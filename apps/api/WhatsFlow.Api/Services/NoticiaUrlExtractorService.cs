using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WhatsFlow.API.Services;

/// <summary>
/// Extrai título, data, descrição, imagem e texto de uma URL de notícia (qualquer portal).
/// Funciona de forma genérica: tenta várias fontes em ordem (meta tags, &lt;article&gt;, classes comuns de CMS, etc.),
/// sem depender de um site específico. Quanto mais padrões forem adicionados nas listas (classes, ids, meta),
/// mais portais diferentes passarão a funcionar.
/// </summary>
public class NoticiaUrlExtractorService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public NoticiaUrlExtractorService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<NoticiaExtraidaDto?> ExtrairAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        url = url.Trim();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        client.Timeout = TimeSpan.FromSeconds(15);

        string html;
        try
        {
            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            html = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(html))
            return null;

        // Preferir o título mais longo quando og:title e <title> existem (muitos sites truncam og:title para redes sociais)
        var ogTitle = ExtrairMetaContent(html, "og:title")?.Trim();
        var twitterTitle = ExtrairMetaContent(html, "twitter:title")?.Trim();
        var titleTag = ExtrairTitle(html)?.Trim();
        var titulo = ogTitle ?? twitterTitle ?? titleTag;
        if (!string.IsNullOrEmpty(titleTag) && titleTag.Length > (titulo?.Length ?? 0))
            titulo = titleTag;

        var dataStr = ExtrairMetaContent(html, "article:published_time")
            ?? ExtrairMetaContent(html, "date")
            ?? ExtrairMetaContent(html, "publish_date");
        DateTime? data = null;
        if (!string.IsNullOrWhiteSpace(dataStr) && DateTime.TryParse(dataStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            data = parsed;

        var texto = ExtrairCorpoTexto(html);
        texto = LimparInicioDoTexto(texto, titulo);

        // Descrição: 1) trecho abaixo do título no HTML  2) primeira frase/parágrafo do corpo  3) meta (se não for autor/site e tiver tamanho razoável)  4) início do texto
        var descricao = ExtrairTrechoAbaixoDoTitulo(html)
            ?? (string.IsNullOrWhiteSpace(texto) ? null : ExtrairPrimeiraFraseOuParagrafo(texto));

        if (string.IsNullOrWhiteSpace(descricao))
        {
            var metaDesc = ExtrairMetaContent(html, "og:description")
                ?? ExtrairMetaContent(html, "twitter:description")
                ?? ExtrairMetaContent(html, "description", byProperty: false);
            if (!DescricaoPareceAutorOuSite(metaDesc) && !string.IsNullOrWhiteSpace(metaDesc) && metaDesc.Trim().Length >= 40)
                descricao = metaDesc?.Trim();
        }

        // Quando o portal não traz descrição (ou meta é curta/ruim), usar início do texto
        if (string.IsNullOrWhiteSpace(descricao) && !string.IsNullOrWhiteSpace(texto))
        {
            var inicio = texto!.Trim();
            if (inicio.Length > 320)
            {
                var corte = inicio.Substring(0, 320);
                var ultimoEspaco = corte.LastIndexOf(' ');
                descricao = ultimoEspaco > 150 ? corte.Substring(0, ultimoEspaco) + "…" : corte.TrimEnd() + "…";
            }
            else
                descricao = inicio;
        }

        var imagemUrl = ExtrairMetaContent(html, "og:image")
            ?? ExtrairMetaContent(html, "twitter:image");

        return new NoticiaExtraidaDto
        {
            Titulo = titulo?.Trim() ?? "",
            Descricao = descricao?.Trim() ?? "",
            Texto = texto?.Trim() ?? "",
            Data = data,
            Url = url,
            ImagemUrl = imagemUrl?.Trim()
        };
    }

    /// <summary>
    /// Extrai o trecho que aparece logo abaixo do título (subtítulo/lead) em páginas de notícia.
    /// </summary>
    private static string? ExtrairTrechoAbaixoDoTitulo(string html)
    {
        const int maxLength = 600;
        // sub-title é usado pelo CPAD News para o trecho logo abaixo do título
        var classes = new[] { "sub-title", "subtitle", "lead", "excerpt", "summary", "resumo", "chapeu", "article-lead", "entry-summary", "post-excerpt", "single-excerpt", "article-description", "post-description", "description", "intro", "article-intro", "entry-excerpt", "tdb-block-inner" };
        foreach (var className in classes)
        {
            // div ou p com a classe (pode ter outras classes junto)
            var pattern = $@"<(?:div|p)[^>]*class=[""'][^""']*{Regex.Escape(className)}[^""']*[""'][^>]*>([\s\S]*?)</(?:div|p)>";
            var m = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
            if (!m.Success) continue;
            var inner = m.Groups[1].Value;
            var texto = StripTags(inner);
            if (string.IsNullOrWhiteSpace(texto)) continue;
            texto = texto.Trim();
            if (texto.Length < 10) continue; // descarte trechos mínimos
            return texto.Length > maxLength ? texto.Substring(0, maxLength).TrimEnd() + "…" : texto;
        }
        // Alguns sites usam o primeiro <p> dentro de article ou de um bloco de conteúdo
        var articleMatch = Regex.Match(html, @"<article[^>]*>[\s\S]*?<(?:p|div)[^>]*>([\s\S]*?)</(?:p|div)>", RegexOptions.IgnoreCase);
        if (articleMatch.Success)
        {
            var firstBlock = StripTags(articleMatch.Groups[1].Value).Trim();
            if (firstBlock.Length >= 20 && firstBlock.Length <= maxLength)
                return firstBlock;
            if (firstBlock.Length > maxLength)
                return firstBlock.Substring(0, maxLength).TrimEnd() + "…";
        }
        // CPAD e similares: procurar qualquer <p> ou <div> cujo texto pareça lead (ex: "Segundo a Portas Abertas...")
        var allBlocks = Regex.Matches(html, @"<(?:p|div)[^>]*>([\s\S]*?)</(?:p|div)>", RegexOptions.IgnoreCase);
        foreach (Match b in allBlocks)
        {
            var inner = StripTags(b.Groups[1].Value).Trim();
            if (inner.Length < 30) continue;
            var isLead = inner.StartsWith("Segundo ", StringComparison.OrdinalIgnoreCase) ||
                inner.StartsWith("Segundo a ", StringComparison.OrdinalIgnoreCase) ||
                inner.StartsWith("De acordo com ", StringComparison.OrdinalIgnoreCase) ||
                inner.StartsWith("Conforme ", StringComparison.OrdinalIgnoreCase);
            if (!isLead) continue;
            // Se o bloco for curto, usar inteiro; senão usar só a primeira frase
            if (inner.Length <= 400)
                return inner.Length > maxLength ? inner.Substring(0, maxLength).TrimEnd() + "…" : inner;
            var primeiraFrase = Regex.Match(inner, @"^([^.]{10,400})\.\s");
            if (primeiraFrase.Success)
                return primeiraFrase.Groups[1].Value.Trim() + ".";
        }
        return null;
    }

    /// <summary>
    /// Extrai a primeira frase ou primeiro parágrafo curto do texto do artigo (ótimo para lead).
    /// </summary>
    private static string? ExtrairPrimeiraFraseOuParagrafo(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        texto = texto.Trim();
        // Parágrafos (separados por quebra dupla)
        var paragrafos = Regex.Split(texto, @"\n\s*\n|\r\n\s*\r\n");
        foreach (var p in paragrafos)
        {
            var t = p.Trim();
            if (t.Length >= 50 && t.Length <= 450) return t;
        }
        // Primeira frase (até ponto seguido de espaço)
        var match = Regex.Match(texto, @"^([^.]{40,450})\.\s", RegexOptions.Singleline);
        if (match.Success) return match.Groups[1].Value.Trim() + ".";
        return null;
    }

    /// <summary>
    /// Rejeita meta description quando for claramente autor/redação/site (ex: "Redação CPAD News Website").
    /// </summary>
    private static bool DescricaoPareceAutorOuSite(string? meta)
    {
        if (string.IsNullOrWhiteSpace(meta) || meta.Length < 25) return true;
        var m = meta.Trim();
        if (m.Length > 200) return false; // descrições longas são válidas
        if (m.Contains("Website", StringComparison.OrdinalIgnoreCase)) return true;
        if (m.Contains("Redação", StringComparison.OrdinalIgnoreCase) && m.Length < 80) return true;
        if (Regex.IsMatch(m, @"^By\s+\w+", RegexOptions.IgnoreCase)) return true;
        return false;
    }

    private static string? ExtrairMetaContent(string html, string propertyOrName, bool byProperty = true)
    {
        var attr = byProperty ? "property" : "name";
        var pattern = $@"<meta\s+[^>]*{attr}=[""'](?:[^""']*){Regex.Escape(propertyOrName)}[^""']*[""'][^>]*content=[""']([^""']*)[""']";
        var m = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (m.Success)
            return DecodeHtml(m.Groups[1].Value);
        pattern = $@"<meta\s+[^>]*content=[""']([^""']*)[""'][^>]*{attr}=[""'](?:[^""']*){Regex.Escape(propertyOrName)}[^""']*[""']";
        m = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return m.Success ? DecodeHtml(m.Groups[1].Value) : null;
    }

    private static string? ExtrairTitle(string html)
    {
        var m = Regex.Match(html, @"<title[^>]*>([^<]*)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return m.Success ? DecodeHtml(m.Groups[1].Value).Trim() : null;
    }

    /// <summary>
    /// Extrai o corpo do texto do artigo. Tenta vários padrões em ordem (tags semânticas, classes e ids comuns em vários portais/CMS).
    /// Novos portais podem ser cobertos adicionando mais itens nas listas estáticas.
    /// </summary>
    private static string? ExtrairCorpoTexto(string html)
    {
        const int minLength = 100;
        const int maxLength = 5000;

        // 1) Tags semânticas (funcionam em muitos sites)
        foreach (var tag in new[] { "article", "main", "section" })
        {
            var texto = ExtrairConteudoPorTag(html, tag, minLength, maxLength);
            if (texto != null) return texto;
        }

        // 2) Divs por classe (WordPress, Joomla, CPAD, Guiame, temas genéricos)
        var classesConteudo = new[]
        {
            "post-content", "entry-content", "content-body", "article-body", "single-content", "post-body",
            "conteudo", "content", "article-content", "post-entry", "entry-body", "noticia-corpo",
            "materia-corpo", "texto-noticia", "corpo-materia", "itemFullText", "story-body", "article__body",
            "news-content", "single-post-content", "tdb-block-inner", "elementor-widget-theme-post-content",
            "wpb_wrapper", "the-content", "blog-post-content", "news-body", "noticia-conteudo"
        };
        foreach (var className in classesConteudo)
        {
            var texto = ExtrairConteudoPorClasse(html, className, minLength, maxLength);
            if (texto != null) return texto;
        }

        // 3) Por id (comum em layouts antigos ou custom)
        foreach (var id in new[] { "content", "main-content", "article-content", "conteudo", "post-content", "article-body", "corpo" })
        {
            var texto = ExtrairConteudoPorId(html, id, minLength, maxLength);
            if (texto != null) return texto;
        }

        // 4) Fallback: maior bloco de texto que pareça artigo (remove scripts e pega body)
        var full = StripTagsPreservandoParagrafos(html);
        if (!string.IsNullOrWhiteSpace(full) && full.Length > minLength)
            return full.Length > maxLength ? full.Substring(0, maxLength) + "…" : full;
        return null;
    }

    private static string? ExtrairConteudoPorTag(string html, string tagName, int minLength, int maxLength)
    {
        var pattern = $@"<{Regex.Escape(tagName)}[^>]*>([\s\S]*?)</{tagName}>";
        var m = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        var texto = StripTagsPreservandoParagrafos(m.Groups[1].Value);
        return string.IsNullOrWhiteSpace(texto) || texto.Length < minLength ? null
            : texto.Length > maxLength ? texto.Substring(0, maxLength) + "…" : texto;
    }

    private static string? ExtrairConteudoPorClasse(string html, string className, int minLength, int maxLength)
    {
        // div ou section com a classe (pode ter outras classes)
        var pattern = $@"<(?:div|section|article)[^>]*class=[""'][^""']*{Regex.Escape(className)}[^""']*[""'][^>]*>([\s\S]*?)</(?:div|section|article)>";
        var m = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        var texto = StripTagsPreservandoParagrafos(m.Groups[1].Value);
        return string.IsNullOrWhiteSpace(texto) || texto.Length < minLength ? null
            : texto.Length > maxLength ? texto.Substring(0, maxLength) + "…" : texto;
    }

    private static string? ExtrairConteudoPorId(string html, string id, int minLength, int maxLength)
    {
        // Captura div/section com esse id; \1 garante fechar com a mesma tag (evita parar em div interno)
        var pattern = $@"<(div|section)[^>]*id=[""'][^""']*{Regex.Escape(id)}[^""']*[""'][^>]*>([\s\S]*?)</\1>";
        var m = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        var texto = StripTagsPreservandoParagrafos(m.Groups[2].Value);
        return string.IsNullOrWhiteSpace(texto) || texto.Length < minLength ? null
            : texto.Length > maxLength ? texto.Substring(0, maxLength) + "…" : texto;
    }

    /// <summary>
    /// Remove tags HTML preservando quebras de parágrafo (espaçamento entre blocos).
    /// </summary>
    private static string StripTagsPreservandoParagrafos(string html)
    {
        html = Regex.Replace(html, @"<script[\s\S]*?</script>", " ", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<style[\s\S]*?</style>", " ", RegexOptions.IgnoreCase);
        // Marcar limites de bloco com quebra de linha dupla antes de remover tags
        html = Regex.Replace(html, @"</p>", "\n\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<p[^>]*>", "\n\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<br\s*/?>", "\n\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</div>", "\n\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</li>", "\n\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<[^>]+>", " ", RegexOptions.IgnoreCase);
        html = DecodeHtml(html);
        // Colapsar apenas espaços e tabs (não quebras de linha) dentro da mesma linha
        html = Regex.Replace(html, @"[ \t]+", " ");
        // Colapsar múltiplas quebras em no máximo duas (parágrafo)
        html = Regex.Replace(html, @"\n[\s\n]*\n", "\n\n");
        return html.Trim();
    }

    /// <summary>
    /// Remove do início do texto metadados/UI até o primeiro parágrafo de corpo (lead). O texto deve começar no primeiro trecho longo de prosa.
    /// </summary>
    private static string? LimparInicioDoTexto(string? texto, string? titulo)
    {
        if (string.IsNullOrWhiteSpace(texto)) return texto;
        var linhas = texto!.Split(new[] { "\n\n", "\r\n\r\n", "\n", "\r\n" }, StringSplitOptions.None);
        var sb = new StringBuilder();
        var comecouConteudo = false;
        foreach (var linha in linhas)
        {
            var t = linha.Trim();
            if (string.IsNullOrEmpty(t)) { if (!comecouConteudo) continue; sb.AppendLine(); sb.AppendLine(); continue; }
            // Se ainda não começou o conteúdo: descarta lixo explícito OU trechos curtos (título/metadata têm menos que um parágrafo de lead)
            if (!comecouConteudo)
            {
                if (LinhaEhLixoInicial(t, titulo)) continue;
                // Parágrafo de lead costuma ter pelo menos ~100 caracteres; ignora blocos curtos no início
                if (t.Length < 100) continue;
            }
            comecouConteudo = true;
            // Se o bloco começa com lixo mas tem conteúdo depois (ex.: "ADVERTISEMENT\n\nA confirmação..."), usa só a partir do conteúdo
            var parteUtil = CortarPrefixoLixo(t, titulo);
            if (string.IsNullOrWhiteSpace(parteUtil)) continue;
            if (sb.Length > 0) sb.AppendLine(); sb.AppendLine();
            sb.Append(parteUtil.Trim());
        }
        return sb.ToString().Trim();
    }

    /// <summary>Se o texto começa com uma linha de lixo seguida de parágrafo, retorna só o parágrafo.</summary>
    private static string CortarPrefixoLixo(string bloco, string? titulo)
    {
        var idx = bloco.IndexOf("\n\n", StringComparison.Ordinal);
        if (idx <= 0) return bloco;
        var primeiraLinha = bloco.Substring(0, idx).Trim();
        if (!LinhaEhLixoInicial(primeiraLinha, titulo)) return bloco;
        var resto = bloco.Substring(idx + 2).Trim();
        return string.IsNullOrWhiteSpace(resto) ? bloco : resto;
    }

    private static bool LinhaEhLixoInicial(string linha, string? titulo)
    {
        if (string.IsNullOrWhiteSpace(linha)) return true;
        if (linha.Equals("capa", StringComparison.OrdinalIgnoreCase)) return true;
        if (linha.Equals("em", StringComparison.OrdinalIgnoreCase)) return true;
        if (!string.IsNullOrEmpty(titulo) && linha.Equals(titulo.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
        if (Regex.IsMatch(linha, @"^\d+\s*(hora|horas|dia|dias|minuto|minutos)\s+atrás$", RegexOptions.IgnoreCase)) return true;
        if (Regex.IsMatch(linha, @"^em\s+\d{1,2}\s+de\s+\w+\s+de\s+\d{4}$", RegexOptions.IgnoreCase)) return true;
        if (Regex.IsMatch(linha, @"^\d{1,2}\s+de\s+\w+\s+de\s+\d{4}$", RegexOptions.IgnoreCase)) return true;
        if (Regex.IsMatch(linha, @"^Por\s+.+$", RegexOptions.IgnoreCase) && linha.Length < 80) return true;
        if (linha.Equals("ADVERTISEMENT", StringComparison.OrdinalIgnoreCase)) return true;
        if (linha.Contains("Ver essa foto no Instagram", StringComparison.OrdinalIgnoreCase)) return true;
        if (Regex.IsMatch(linha, @"^Um post compartilhado por\s+.+$", RegexOptions.IgnoreCase)) return true;
        if (linha.Contains("post compartilhado por", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static string StripTags(string html)
    {
        html = Regex.Replace(html, @"<script[\s\S]*?</script>", " ", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<style[\s\S]*?</style>", " ", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<[^>]+>", " ", RegexOptions.IgnoreCase);
        html = DecodeHtml(html);
        html = Regex.Replace(html, @"\s+", " ").Trim();
        return html;
    }

    private static string DecodeHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return System.Net.WebUtility.HtmlDecode(text);
    }
}

public class NoticiaExtraidaDto
{
    public string Titulo { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string Texto { get; set; } = "";
    public DateTime? Data { get; set; }
    public string? Url { get; set; }
    /// <summary>URL da imagem de destaque (og:image) para baixar e usar na notícia.</summary>
    public string? ImagemUrl { get; set; }
}

public class ExtrairNoticiaUrlRequest
{
    public string Url { get; set; } = "";
}
