using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Configuration;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Utils;

namespace WhatsFlow.Application.Services;

public class EvolutionApiService : IEvolutionApiService
{
    private readonly HttpClient _httpClient;
    private readonly EvolutionApiSettings _settings;
    private readonly ILogger<EvolutionApiService> _logger;

    public EvolutionApiService(
        HttpClient httpClient,
        IOptions<EvolutionApiSettings> settings,
        ILogger<EvolutionApiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        if (!string.IsNullOrEmpty(_settings.BaseUrl))
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");

        if (!string.IsNullOrEmpty(_settings.ApiKey))
            _httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);

        _logger.LogInformation(
            "Evolution API configurada. BaseUrl: {BaseUrl}, InstanceName: {InstanceName}, Timeout: {TimeoutSeconds}s, MaxRetries: {MaxRetries}",
            _httpClient.BaseAddress?.ToString() ?? "(não definida)",
            string.IsNullOrWhiteSpace(_settings.InstanceName) ? "(não definida)" : _settings.InstanceName,
            _settings.TimeoutSeconds,
            _settings.MaxRetries);
    }

    public async Task<EvolutionApiResponse> EnviarMensagemTextoAsync(
        string numero,
        string mensagem,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(numero))
        {
            return new EvolutionApiResponse
            {
                Sucesso = false,
                MensagemErro = "Número de telefone não pode ser vazio",
                StatusCode = 400
            };
        }

        if (string.IsNullOrWhiteSpace(mensagem))
        {
            return new EvolutionApiResponse
            {
                Sucesso = false,
                MensagemErro = "Mensagem não pode ser vazia",
                StatusCode = 400
            };
        }

        string numeroFormatado;
        try
        {
            numeroFormatado = TelefoneUtils.FormatarParaEvolutionApi(numero, _settings.CodigoPaisPadrao);
            _logger.LogDebug("Número formatado: {NumeroOriginal} -> {NumeroFormatado}", numero, numeroFormatado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao formatar número {Numero}", numero);
            return new EvolutionApiResponse
            {
                Sucesso = false,
                MensagemErro = $"Erro ao formatar número: {ex.Message}",
                StatusCode = 400
            };
        }

        var request = new EvolutionApiSendTextRequest
        {
            Number = numeroFormatado,
            Text = mensagem,
            Delay = Math.Max(0, _settings.DelayMs),
            LinkPreview = false
        };

        return await EnviarComRetryAsync(
            $"message/sendText/{_settings.InstanceName}",
            request,
            numeroFormatado,
            mensagem,
            cancellationToken);
    }

    public async Task<EvolutionApiResponse> EnviarMensagemImagemAsync(
        string numero,
        string imageUrl,
        string legenda,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(numero))
        {
            return new EvolutionApiResponse
            {
                Sucesso = false,
                MensagemErro = "Número de telefone não pode ser vazio",
                StatusCode = 400
            };
        }

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return new EvolutionApiResponse
            {
                Sucesso = false,
                MensagemErro = "URL da imagem não pode ser vazia",
                StatusCode = 400
            };
        }

        string numeroFormatado;
        try
        {
            numeroFormatado = TelefoneUtils.FormatarParaEvolutionApi(numero, _settings.CodigoPaisPadrao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao formatar número {Numero}", numero);
            return new EvolutionApiResponse
            {
                Sucesso = false,
                MensagemErro = $"Erro ao formatar número: {ex.Message}",
                StatusCode = 400
            };
        }

        var mediaContent = await ResolverConteudoMediaAsync(imageUrl, cancellationToken);
        var mimeType = ObterMimeType(imageUrl);
        var fileName = ObterNomeArquivo(imageUrl, "feliz-aniversario.png");
        var endpoint = $"message/sendMedia/{_settings.InstanceName}";

        foreach (var variante in CriarVariantesMedia(mediaContent, imageUrl))
        {
            _logger.LogInformation(
                "Tentando envio de mídia via Evolution API. Numero={Numero} Variante={Variante} Prefixo={Prefixo}",
                numeroFormatado,
                variante.Descricao,
                variante.Conteudo.Length > 40 ? variante.Conteudo[..40] : variante.Conteudo);

            var request = new EvolutionApiSendMediaRequest
            {
                Number = numeroFormatado,
                Media = variante.Conteudo,
                FileName = fileName,
                Mimetype = mimeType,
                Caption = legenda,
                Mediatype = "image",
                Delay = Math.Max(0, _settings.DelayMs)
            };

            var response = await EnviarComRetryAsync(
                endpoint,
                request,
                numeroFormatado,
                legenda,
                cancellationToken);

            if (response.Sucesso)
            {
                return response;
            }

            if (PodeTentarFallbackMedia(response))
            {
                _logger.LogWarning(
                    "Evolution sendMedia falhou na variante {Variante}. Tentando payload compatível com v1 para o número {Numero}.",
                    variante.Descricao,
                    numeroFormatado);

                var fallbackRequest = new EvolutionApiSendMediaV1Request
                {
                    Number = numeroFormatado,
                    Mediatype = "image",
                    FileName = fileName,
                    Mimetype = mimeType,
                    Caption = legenda,
                    Media = variante.Conteudo,
                    MediaMessage = new EvolutionApiMediaMessageV1
                    {
                        MediaType = "image",
                        FileName = fileName,
                        Caption = legenda,
                        Media = variante.Conteudo
                    },
                    Options = new EvolutionApiMediaOptionsV1
                    {
                        Delay = Math.Max(0, _settings.DelayMs),
                        Presence = "composing"
                    }
                };

                var fallbackResponse = await EnviarComRetryAsync(
                    endpoint,
                    fallbackRequest,
                    numeroFormatado,
                    legenda,
                    cancellationToken);

                if (fallbackResponse.Sucesso)
                {
                    return fallbackResponse;
                }

                if (!PodeTentarFallbackMedia(fallbackResponse))
                {
                    return fallbackResponse;
                }
            }
            else
            {
                return response;
            }
        }

        return new EvolutionApiResponse
        {
            Sucesso = false,
            MensagemErro = "Nao foi possivel enviar a mídia em nenhum formato compatível com a Evolution API.",
            StatusCode = 500
        };
    }

    public async Task<bool> ValidarInstanciaAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = "instance/fetchInstances";
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var requestUri = response.RequestMessage?.RequestUri?.ToString()
                                ?? (_httpClient.BaseAddress is null ? endpoint : new Uri(_httpClient.BaseAddress, endpoint).ToString());

                _logger.LogWarning(
                    "Falha ao validar instância na Evolution API - Status: {StatusCode}. RequestUri: {RequestUri}. Response: {Response}",
                    response.StatusCode,
                    requestUri,
                    Truncate(body, 600));
                return false;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Resposta validação instância: {Resposta}", content);

            try
            {
                if (string.IsNullOrWhiteSpace(_settings.InstanceName))
                {
                    _logger.LogWarning("InstanceName não configurado para Evolution API (EvolutionApi:InstanceName)");
                    return false;
                }

                var jsonDoc = JsonDocument.Parse(content);
                if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var nomes = jsonDoc.RootElement.EnumerateArray()
                        .Select(ExtractInstanceNameFromFetchInstancesItem)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var encontrada = nomes.Any(n =>
                        string.Equals(n, _settings.InstanceName, StringComparison.OrdinalIgnoreCase));
                    if (encontrada)
                    {
                        _logger.LogInformation("Instância {InstanceName} encontrada e válida", _settings.InstanceName);
                        return true;
                    }

                    if (nomes.Count > 0)
                    {
                        _logger.LogWarning(
                            "Instância {InstanceName} não encontrada. Instâncias disponíveis: {Disponiveis}",
                            _settings.InstanceName,
                            string.Join(", ", nomes));
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Não foi possível parsear resposta de validação");
            }

            _logger.LogWarning("Instância {InstanceName} não encontrada na lista", _settings.InstanceName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar instância");
            return false;
        }
    }

    private async Task<EvolutionApiResponse> EnviarComRetryAsync(
        string endpoint,
        object request,
        string numeroFormatado,
        string previewMensagem,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Enviando mensagem via Evolution API - Endpoint: {Endpoint}, Número: {Numero}, Mensagem: {MensagemPreview}",
            endpoint,
            numeroFormatado,
            previewMensagem.Length > 50 ? previewMensagem[..50] + "..." : previewMensagem);

        for (int tentativa = 1; tentativa <= _settings.MaxRetries; tentativa++)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var requestUri = response.RequestMessage?.RequestUri?.ToString()
                                ?? (_httpClient.BaseAddress is null ? endpoint : new Uri(_httpClient.BaseAddress, endpoint).ToString());

                _logger.LogDebug(
                    "Resposta Evolution API - Status: {StatusCode}, Tentativa: {Tentativa}/{MaxRetries}, Resposta: {Resposta}",
                    response.StatusCode,
                    tentativa,
                    _settings.MaxRetries,
                    responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(responseContent);
                        var messageId = jsonDoc.RootElement.TryGetProperty("key", out var keyElement)
                            && keyElement.TryGetProperty("id", out var idElement)
                            ? idElement.GetString()
                            : null;

                        return new EvolutionApiResponse
                        {
                            Sucesso = true,
                            StatusCode = (int)response.StatusCode,
                            MessageId = messageId,
                            RespostaCompleta = responseContent
                        };
                    }
                    catch (JsonException)
                    {
                        return new EvolutionApiResponse
                        {
                            Sucesso = true,
                            StatusCode = (int)response.StatusCode,
                            RespostaCompleta = responseContent
                        };
                    }
                }

                var errorResponse = TratarErroResponse(responseContent, (int)response.StatusCode);

                if ((int)response.StatusCode == 404)
                {
                    _logger.LogError(
                        "Evolution API retornou 404. RequestUri: {RequestUri}. BaseUrl: {BaseUrl}. InstanceName: {InstanceName}. Response: {Response}",
                        requestUri,
                        _httpClient.BaseAddress?.ToString() ?? "(não definida)",
                        string.IsNullOrWhiteSpace(_settings.InstanceName) ? "(não definida)" : _settings.InstanceName,
                        Truncate(responseContent, 600));
                }
                else
                {
                    _logger.LogWarning(
                        "Falha ao enviar mensagem na Evolution API. Status: {StatusCode}. RequestUri: {RequestUri}. Erro: {Erro}. Response: {Response}",
                        response.StatusCode,
                        requestUri,
                        errorResponse.MensagemErro,
                        Truncate(responseContent, 600));
                }

                if (!IsTransientFailure((HttpStatusCode)response.StatusCode))
                {
                    return errorResponse;
                }

                if (tentativa < _settings.MaxRetries)
                {
                    await Task.Delay(ObterBackoffExponencial(tentativa), cancellationToken);
                    continue;
                }

                return errorResponse;
            }
            catch (TaskCanceledException)
            {
                if (tentativa < _settings.MaxRetries)
                {
                    await Task.Delay(ObterBackoffExponencial(tentativa), cancellationToken);
                    continue;
                }

                return new EvolutionApiResponse
                {
                    Sucesso = false,
                    MensagemErro = $"Timeout após {_settings.MaxRetries} tentativas",
                    StatusCode = 408
                };
            }
            catch (HttpRequestException ex)
            {
                if (tentativa < _settings.MaxRetries)
                {
                    await Task.Delay(ObterBackoffExponencial(tentativa), cancellationToken);
                    continue;
                }

                return new EvolutionApiResponse
                {
                    Sucesso = false,
                    MensagemErro = $"Erro de conexão: {ex.Message}",
                    StatusCode = 0
                };
            }
            catch (Exception ex)
            {
                if (tentativa < _settings.MaxRetries)
                {
                    await Task.Delay(ObterBackoffExponencial(tentativa), cancellationToken);
                    continue;
                }

                return new EvolutionApiResponse
                {
                    Sucesso = false,
                    MensagemErro = $"Erro inesperado: {ex.Message}",
                    StatusCode = 500
                };
            }
        }

        return new EvolutionApiResponse
        {
            Sucesso = false,
            MensagemErro = "Falha ao enviar mensagem após todas as tentativas",
            StatusCode = 500
        };
    }

    /// <summary>
    /// Retry apenas para falhas transitórias: timeout, 429, 5xx. Não retry para 4xx (exceto 429).
    /// </summary>
    private static bool IsTransientFailure(HttpStatusCode status)
    {
        var code = (int)status;
        if (code is >= 500 and < 600) return true;
        if (status == (HttpStatusCode)429) return true; // Too Many Requests
        return false;
    }

    /// <summary>
    /// Backoff exponencial: base * 2^(tentativa-1), limitado ao máximo configurável.
    /// </summary>
    private TimeSpan ObterBackoffExponencial(int tentativa)
    {
        var segundos = _settings.RetryDelaySeconds * Math.Pow(2, tentativa - 1);
        var cap = Math.Min(60, Math.Max(1, _settings.RetryDelaySeconds * 8));
        segundos = Math.Min(segundos, cap);
        return TimeSpan.FromSeconds(segundos);
    }

    private static EvolutionApiResponse TratarErroResponse(string responseContent, int statusCode)
    {
        try
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsed = JsonSerializer.Deserialize<EvolutionApiErrorResponse>(responseContent, opts);
            var msg = parsed?.Message ?? parsed?.Error;

            // Alguns erros da Evolution v2 vêm em: { response: { message: [[ "..."]] } }
            if (string.IsNullOrWhiteSpace(msg))
            {
                var detailed = TryExtractEvolutionValidationMessage(responseContent);
                if (!string.IsNullOrWhiteSpace(detailed))
                {
                    msg = detailed;
                }
            }

            msg ??= responseContent;
            return new EvolutionApiResponse
            {
                Sucesso = false,
                StatusCode = statusCode,
                MensagemErro = msg,
                RespostaCompleta = responseContent
            };
        }
        catch
        {
            return new EvolutionApiResponse
            {
                Sucesso = false,
                StatusCode = statusCode,
                MensagemErro = responseContent,
                RespostaCompleta = responseContent
            };
        }
    }

    private static string Truncate(string? s, int maxLen)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        if (s.Length <= maxLen) return s;
        return s[..maxLen] + "...(truncado)";
    }

    private static string? ExtractInstanceNameFromFetchInstancesItem(JsonElement item)
    {
        // v2 comum:
        // [
        //   { "instance": { "instanceName": "kingdom", ... } }
        // ]
        if (item.ValueKind == JsonValueKind.Object)
        {
            if (item.TryGetProperty("instanceName", out var directInstanceName) &&
                directInstanceName.ValueKind == JsonValueKind.String)
            {
                return directInstanceName.GetString();
            }

            if (item.TryGetProperty("name", out var directName) &&
                directName.ValueKind == JsonValueKind.String)
            {
                return directName.GetString();
            }

            if (item.TryGetProperty("instance", out var instanceProp))
            {
                if (instanceProp.ValueKind == JsonValueKind.String)
                {
                    return instanceProp.GetString();
                }

                if (instanceProp.ValueKind == JsonValueKind.Object)
                {
                    if (instanceProp.TryGetProperty("instanceName", out var nestedInstanceName) &&
                        nestedInstanceName.ValueKind == JsonValueKind.String)
                    {
                        return nestedInstanceName.GetString();
                    }

                    if (instanceProp.TryGetProperty("name", out var nestedName) &&
                        nestedName.ValueKind == JsonValueKind.String)
                    {
                        return nestedName.GetString();
                    }
                }
            }
        }

        return null;
    }

    private static string? TryExtractEvolutionValidationMessage(string responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent)) return null;

        try
        {
            using var doc = JsonDocument.Parse(responseContent);
            if (!doc.RootElement.TryGetProperty("response", out var resp)) return null;
            if (!resp.TryGetProperty("message", out var msgEl)) return null;

            if (msgEl.ValueKind == JsonValueKind.String) return msgEl.GetString();

            // Pode vir como array de arrays: [[ "Enter a value..." ]]
            if (msgEl.ValueKind == JsonValueKind.Array)
            {
                var parts = new List<string>();
                foreach (var item in msgEl.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var s = item.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) parts.Add(s);
                        continue;
                    }

                    if (item.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var inner in item.EnumerateArray())
                        {
                            if (inner.ValueKind != JsonValueKind.String) continue;
                            var s = inner.GetString();
                            if (!string.IsNullOrWhiteSpace(s)) parts.Add(s);
                        }
                    }
                }

                return parts.Count > 0 ? string.Join("; ", parts) : null;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string ObterNomeArquivo(string imageUrl, string fallback)
    {
        try
        {
            var uri = new Uri(imageUrl, UriKind.RelativeOrAbsolute);
            var path = uri.IsAbsoluteUri ? uri.AbsolutePath : imageUrl;
            var fileName = Path.GetFileName(path);
            return string.IsNullOrWhiteSpace(fileName) ? fallback : fileName;
        }
        catch
        {
            return fallback;
        }
    }

    private static string ObterMimeType(string imageUrl)
    {
        var extension = Path.GetExtension(imageUrl)?.ToLowerInvariant();
        return extension switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/png"
        };
    }

    private async Task<string> ResolverConteudoMediaAsync(string imageUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new FileNotFoundException("Nao foi possivel localizar o arquivo da midia para envio. Caminho informado esta vazio.");
        }

        if (EhDataUri(imageUrl))
        {
            return imageUrl;
        }

        if (EhUrlHttp(imageUrl))
        {
            try
            {
                using var mediaClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(Math.Min(_settings.TimeoutSeconds, 15))
                };

                using var response = await mediaClient.GetAsync(imageUrl, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "Falha ao baixar midia remota para envio na Evolution API. Url: {Url}. Status: {StatusCode}. Response: {Response}",
                        imageUrl,
                        response.StatusCode,
                        Truncate(responseContent, 300));
                }
                else
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    if (bytes.Length > 0)
                    {
                        var mimeType = response.Content.Headers.ContentType?.MediaType ?? ObterMimeType(imageUrl);
                        _logger.LogInformation(
                            "Midia remota baixada com sucesso para envio inline na Evolution API. Url: {Url}. Bytes: {Bytes}",
                            imageUrl,
                            bytes.Length);
                        return ConstruirDataUri(mimeType, bytes);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Nao foi possivel baixar midia remota para envio inline na Evolution API. Url: {Url}. Sera mantido o envio por URL.",
                    imageUrl);
            }

            return imageUrl;
        }

        var caminhoNormalizado = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var baseDirectory = AppContext.BaseDirectory;
        var contentRoot = Directory.GetCurrentDirectory();
        var projectRoot = Directory.GetParent(baseDirectory)?.Parent?.Parent?.Parent?.FullName;

        var candidatos = new[]
        {
            Path.Combine(contentRoot, caminhoNormalizado),
            Path.Combine(contentRoot, "wwwroot", caminhoNormalizado),
            Path.Combine(contentRoot, "uploads", caminhoNormalizado.Replace($"uploads{Path.DirectorySeparatorChar}", string.Empty)),
            Path.Combine(baseDirectory, caminhoNormalizado),
            Path.Combine(baseDirectory, "wwwroot", caminhoNormalizado),
            Path.Combine(baseDirectory, "uploads", caminhoNormalizado.Replace($"uploads{Path.DirectorySeparatorChar}", string.Empty)),
            projectRoot is null ? string.Empty : Path.Combine(projectRoot, caminhoNormalizado),
            projectRoot is null ? string.Empty : Path.Combine(projectRoot, "wwwroot", caminhoNormalizado),
            projectRoot is null ? string.Empty : Path.Combine(projectRoot, "uploads", caminhoNormalizado.Replace($"uploads{Path.DirectorySeparatorChar}", string.Empty))
        };

        foreach (var candidato in candidatos)
        {
            if (string.IsNullOrWhiteSpace(candidato) || !File.Exists(candidato))
                continue;

            var bytes = File.ReadAllBytes(candidato);
            return ConstruirDataUri(ObterMimeType(candidato), bytes);
        }

        var fileName = Path.GetFileName(imageUrl);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var diretoriosBusca = new[]
            {
                contentRoot,
                baseDirectory,
                projectRoot
            }
            .Where(d => !string.IsNullOrWhiteSpace(d) && Directory.Exists(d))
            .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var diretorio in diretoriosBusca)
            {
                try
                {
                    var encontrado = Directory
                        .EnumerateFiles(diretorio!, fileName, SearchOption.AllDirectories)
                        .FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(encontrado) || !File.Exists(encontrado))
                        continue;

                    var bytes = File.ReadAllBytes(encontrado);
                    return ConstruirDataUri(ObterMimeType(encontrado), bytes);
                }
                catch
                {
                    // Ignora falhas de acesso em diretórios específicos e segue para o próximo.
                }
            }
        }

        throw new FileNotFoundException(
            $"Nao foi possivel localizar o arquivo da midia para envio. Caminho informado: {imageUrl}");
    }

    private static bool EhUrlHttp(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        if (!Uri.TryCreate(valor, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
               uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EhDataUri(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        if (!Uri.TryCreate(valor, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme.Equals("data", StringComparison.OrdinalIgnoreCase);
    }

    private static bool PareceBase64(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return false;
        if (valor.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            valor.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return valor.Length > 100 && !valor.Contains('/');
    }

    private static IEnumerable<(string Descricao, string Conteudo)> CriarVariantesMedia(string mediaContent, string imageUrl)
    {
        var variantes = new List<(string Descricao, string Conteudo)>();

        if (!string.IsNullOrWhiteSpace(mediaContent))
        {
            variantes.Add(("inline", mediaContent));

            if (EhDataUri(mediaContent))
            {
                var plainBase64 = ExtrairBase64DeDataUri(mediaContent);
                if (!string.IsNullOrWhiteSpace(plainBase64))
                {
                    variantes.Add(("base64", plainBase64));
                }
            }
        }

        if (EhUrlHttp(imageUrl))
        {
            variantes.Add(("url", imageUrl));
        }

        return variantes
            .Where(v => !string.IsNullOrWhiteSpace(v.Conteudo))
            .DistinctBy(v => v.Conteudo);
    }

    private static string? ExtrairBase64DeDataUri(string valor)
    {
        if (!EhDataUri(valor))
        {
            return null;
        }

        var indice = valor.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
        if (indice < 0)
        {
            return null;
        }

        return valor[(indice + "base64,".Length)..];
    }

    private static bool PodeTentarFallbackMedia(EvolutionApiResponse response)
    {
        if (response.Sucesso)
        {
            return false;
        }

        if (response.StatusCode is 400 or 404)
        {
            return true;
        }

        if (response.StatusCode == 500)
        {
            var conteudo = $"{response.MensagemErro} {response.RespostaCompleta}";
            return conteudo.Contains("404", StringComparison.OrdinalIgnoreCase) ||
                   conteudo.Contains("sendMedia", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static string ConstruirDataUri(string mimeType, byte[] bytes)
    {
        var mime = string.IsNullOrWhiteSpace(mimeType) ? "image/png" : mimeType;
        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }
}
