using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;

namespace WhatsFlow.Infrastructure.Services;

public class S3FileStorageService : IFileStorageService, IDisposable
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;
    private readonly string? _publicBaseUrl;
    private readonly TimeSpan _signedUrlExpiry;

    public bool IsLocalStorage => false;

    public S3FileStorageService(IOptions<StorageSettings> options)
    {
        var s3 = options.Value.S3;

        if (string.IsNullOrWhiteSpace(s3.BucketName))
            throw new InvalidOperationException("Storage:S3:BucketName não configurado.");

        _bucketName = s3.BucketName;
        _publicBaseUrl = string.IsNullOrWhiteSpace(s3.PublicBaseUrl) ? null : s3.PublicBaseUrl.TrimEnd('/');
        _signedUrlExpiry = TimeSpan.FromMinutes(s3.SignedUrlExpiryMinutes > 0 ? s3.SignedUrlExpiryMinutes : 60);

        var credentials = new BasicAWSCredentials(s3.AccessKeyId, s3.SecretAccessKey);
        var config = new AmazonS3Config { ForcePathStyle = true };

        if (!string.IsNullOrWhiteSpace(s3.Endpoint))
            config.ServiceURL = s3.Endpoint;
        else
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(s3.Region);

        _s3 = new AmazonS3Client(credentials, config);
    }

    public async Task<string> SaveAsync(Stream content, string tenantSlug, string folder, string fileName, string contentType)
    {
        var key = $"tenants/{tenantSlug}/{folder}/{fileName}";
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
        });
        // Retorna a chave S3 — GetUrlAsync converte para URL acessível quando necessário
        return key;
    }

    public Task<string> GetUrlAsync(string storedPath)
    {
        var key = NormalizeKey(storedPath);

        if (_publicBaseUrl != null)
            return Task.FromResult($"{_publicBaseUrl}/{key}");

        var url = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Expires = DateTime.UtcNow.Add(_signedUrlExpiry),
            Verb = HttpVerb.GET,
        });
        return Task.FromResult(url);
    }

    public async Task DeleteAsync(string storedPath)
    {
        await _s3.DeleteObjectAsync(_bucketName, NormalizeKey(storedPath));
    }

    public void Dispose() => _s3.Dispose();

    /// <summary>
    /// Converte tanto caminhos locais legados ("/uploads/tenants/...")
    /// quanto chaves S3 puras ("tenants/...") para o formato de chave S3.
    /// </summary>
    private static string NormalizeKey(string storedPath)
    {
        var path = storedPath.TrimStart('/');
        if (path.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            path = path["uploads/".Length..];
        return path;
    }
}
