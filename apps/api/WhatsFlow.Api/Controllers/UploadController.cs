using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/admin/upload")]
public class UploadController : ControllerBase
{
    private readonly IFileStorageService _fileStorage;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UploadController> _logger;
    private const long MaxFileSize = 500 * 1024 * 1024; // 500MB
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public UploadController(
        IFileStorageService fileStorage,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ITenantContext tenantContext,
        ILogger<UploadController> logger)
    {
        _fileStorage = fileStorage;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    [Authorize]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        return await HandleUpload(file, "images", new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" });
    }

    [HttpPost("videos")]
    [Consumes("multipart/form-data")]
    [Authorize]
    public async Task<IActionResult> UploadVideo(IFormFile file)
    {
        return await HandleUpload(file, "videos", new[] { ".mp4", ".webm", ".ogg", ".mov", ".avi" });
    }

    [HttpPost("audios")]
    [Consumes("multipart/form-data")]
    [Authorize]
    public async Task<IActionResult> UploadAudio(IFormFile file)
    {
        return await HandleUpload(file, "audios", new[] { ".mp3", ".wav", ".ogg", ".m4a", ".aac" });
    }

    [HttpPost("files")]
    [Consumes("multipart/form-data")]
    [Authorize]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        return await HandleUpload(file, "files", null);
    }

    /// <summary>
    /// Recebe imagem enviada por outro backend (sync server-to-server). Exige header X-Sync-Api-Key.
    /// Usado somente com storage local para sincronizar dev → produção.
    /// Com S3 este endpoint é desnecessário (ambos os ambientes usam o mesmo bucket).
    /// </summary>
    [HttpPost("sync-image")]
    [Consumes("multipart/form-data")]
    [AllowAnonymous]
    public async Task<IActionResult> SyncImage(IFormFile file, [FromForm] string? fileName, [FromForm] string? tenantSlug = null)
    {
        var key = Request.Headers["X-Sync-Api-Key"].FirstOrDefault();
        var expectedKey = _configuration["ProductionUploadSync:ApiKey"];
        if (string.IsNullOrEmpty(expectedKey) || key != expectedKey)
            return Unauthorized(new { message = "Chave de sync inválida" });

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Nenhum arquivo enviado" });

        var name = !string.IsNullOrWhiteSpace(fileName) ? fileName.Trim() : $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        if (name.Contains(Path.DirectorySeparatorChar) || name.Contains('/'))
            name = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        var resolvedTenantSlug = ResolveTenantSlug(tenantSlug);
        using var stream = file.OpenReadStream();
        var storedPath = await _fileStorage.SaveAsync(stream, resolvedTenantSlug, "images", name, file.ContentType);
        return Ok(new { url = storedPath, path = storedPath, fileName = name });
    }

    /// <summary>
    /// Baixa uma imagem de URL externa e salva no storage.
    /// </summary>
    [HttpPost("image-from-url")]
    [Authorize]
    public async Task<IActionResult> UploadImageFromUrl([FromBody] UploadImageFromUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Url))
            return BadRequest(new { message = "URL da imagem é obrigatória" });

        var url = request.Url.Trim();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url;

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            using var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            var extension = GetExtensionFromContentType(contentType) ?? GetExtensionFromUrl(url) ?? ".jpg";
            if (!AllowedImageExtensions.Contains(extension.ToLowerInvariant()))
                extension = ".jpg";

            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (bytes.Length == 0)
                return BadRequest(new { message = "Imagem vazia" });
            if (bytes.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "Imagem muito grande (máx. 5MB)" });

            var resolvedTenantSlug = ResolveTenantSlug();
            var fileName = $"{Guid.NewGuid()}{extension}";
            using var stream = new MemoryStream(bytes);
            var storedPath = await _fileStorage.SaveAsync(stream, resolvedTenantSlug, "images", fileName, $"image/{extension.TrimStart('.')}");

            // Sync dev→prod apenas para storage local
            if (_fileStorage.IsLocalStorage)
                storedPath = await TentarSincronizarParaProducaoAsync(storedPath, bytes, resolvedTenantSlug, fileName) ?? storedPath;

            return Ok(new { url = storedPath, path = storedPath, fileName });
        }
        catch (HttpRequestException ex)
        {
            return BadRequest(new { message = "Não foi possível baixar a imagem.", error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao salvar imagem", error = ex.Message });
        }
    }

    private async Task<IActionResult> HandleUpload(IFormFile file, string folder, string[]? allowedExtensions)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Nenhum arquivo enviado" });

        if (file.Length > MaxFileSize)
            return BadRequest(new { message = $"Arquivo muito grande. Máximo: {MaxFileSize / (1024 * 1024)}MB" });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (allowedExtensions != null && !allowedExtensions.Contains(extension))
            return BadRequest(new { message = $"Tipo de arquivo não permitido. Extensões permitidas: {string.Join(", ", allowedExtensions)}" });

        try
        {
            var resolvedTenantSlug = ResolveTenantSlug();
            var fileName = $"{Guid.NewGuid()}{extension}";

            using var stream = file.OpenReadStream();
            var storedPath = await _fileStorage.SaveAsync(stream, resolvedTenantSlug, folder, fileName, file.ContentType);

            // Sync dev→prod apenas para imagens no storage local
            if (_fileStorage.IsLocalStorage && string.Equals(folder, "images", StringComparison.OrdinalIgnoreCase))
            {
                using var ms = new MemoryStream();
                stream.Position = 0;
                // Re-read: stream foi consumido; precisamos ler o arquivo salvo
                storedPath = await TentarSincronizarParaProducaoAsync(storedPath, null, resolvedTenantSlug, fileName) ?? storedPath;
            }

            return Ok(new { url = storedPath, path = storedPath, fileName, size = file.Length });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao fazer upload", error = ex.Message });
        }
    }

    /// <summary>
    /// Tenta sincronizar a imagem para a API de produção (mecanismo dev→prod para storage local).
    /// Quando bytes é null, lê o arquivo do disco a partir do storedPath.
    /// </summary>
    private async Task<string?> TentarSincronizarParaProducaoAsync(string storedPath, byte[]? bytes, string tenantSlug, string fileName)
    {
        var syncBaseUrl = _configuration["ProductionUploadSync:BaseUrl"]?.Trim();
        var syncApiKey = _configuration["ProductionUploadSync:ApiKey"]?.Trim();
        if (string.IsNullOrEmpty(syncBaseUrl) || string.IsNullOrEmpty(syncApiKey))
            return null;

        try
        {
            // Se bytes não foi passado, lê do disco via LocalFileStorageService path
            if (bytes == null)
            {
                var withoutLeadingSlash = storedPath.TrimStart('/');
                var relativePart = withoutLeadingSlash.StartsWith("uploads/") ? withoutLeadingSlash["uploads/".Length..] : withoutLeadingSlash;
                // Não temos a raiz aqui — o sync via IFormFile stream aconteceu antes do save;
                // para este cenário retornamos null e deixamos o path local
                return null;
            }

            var syncClient = _httpClientFactory.CreateClient();
            syncClient.Timeout = TimeSpan.FromSeconds(30);

            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(bytes), "file", fileName);
            content.Add(new StringContent(fileName), "fileName");
            content.Add(new StringContent(tenantSlug), "tenantSlug");

            var syncRequest = new HttpRequestMessage(HttpMethod.Post, $"{syncBaseUrl.TrimEnd('/')}/api/admin/upload/sync-image");
            syncRequest.Headers.Add("X-Sync-Api-Key", syncApiKey);
            syncRequest.Content = content;

            using var syncResponse = await syncClient.SendAsync(syncRequest);
            if (!syncResponse.IsSuccessStatusCode)
                throw new InvalidOperationException($"Sync produção retornou {(int)syncResponse.StatusCode}");

            var json = await syncResponse.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(json))
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("url", out var urlEl))
                {
                    var resolvedUrl = urlEl.GetString();
                    if (!string.IsNullOrWhiteSpace(resolvedUrl))
                    {
                        _logger.LogInformation("Imagem sincronizada para produção. Tenant={Tenant} File={File} Url={Url}", tenantSlug, fileName, resolvedUrl);
                        return resolvedUrl;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao sincronizar imagem para produção.");
        }

        return null;
    }

    private static string? GetExtensionFromContentType(string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => null
        };

    private static string? GetExtensionFromUrl(string url)
    {
        try
        {
            var ext = Path.GetExtension(new Uri(url).AbsolutePath);
            return !string.IsNullOrEmpty(ext) && AllowedImageExtensions.Contains(ext.ToLowerInvariant()) ? ext : null;
        }
        catch { return null; }
    }

    private string ResolveTenantSlug(string? explicitTenantSlug = null)
    {
        var candidate = string.IsNullOrWhiteSpace(explicitTenantSlug) ? _tenantContext.TenantSlug : explicitTenantSlug;
        if (string.IsNullOrWhiteSpace(candidate))
            return Tenant.InitialTenantSlug;

        var sanitized = candidate.Trim().ToLowerInvariant();
        foreach (var invalid in Path.GetInvalidFileNameChars())
            sanitized = sanitized.Replace(invalid, '-');
        sanitized = sanitized.Replace("/", "-").Replace("\\", "-");
        return string.IsNullOrWhiteSpace(sanitized) ? Tenant.InitialTenantSlug : sanitized;
    }
}

public class UploadImageFromUrlRequest
{
    public string Url { get; set; } = "";
}
