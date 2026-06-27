namespace WhatsFlow.Application.Services;

public class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>"local" ou "s3"</summary>
    public string Provider { get; set; } = "local";

    public S3StorageSettings S3 { get; set; } = new();
}

public class S3StorageSettings
{
    public string BucketName { get; set; } = "";

    /// <summary>Região AWS (ex.: "sa-east-1"). Ignorado se Endpoint estiver definido.</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>Endpoint customizado para S3-compatible (Cloudflare R2, MinIO, etc.).</summary>
    public string? Endpoint { get; set; }

    public string AccessKeyId { get; set; } = "";
    public string SecretAccessKey { get; set; } = "";

    /// <summary>Validade das URLs pré-assinadas em minutos. Padrão: 60.</summary>
    public int SignedUrlExpiryMinutes { get; set; } = 60;

    /// <summary>
    /// Se o bucket for público (ex.: Cloudflare R2 public bucket), informe a URL base
    /// (ex.: "https://pub-xxx.r2.dev") para retornar URLs diretas sem assinatura.
    /// Se vazio, usa URLs pré-assinadas.
    /// </summary>
    public string? PublicBaseUrl { get; set; }
}
