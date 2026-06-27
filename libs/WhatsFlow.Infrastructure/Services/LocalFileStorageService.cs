using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using WhatsFlow.Application.Interfaces;

namespace WhatsFlow.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadsRootPath;

    public bool IsLocalStorage => true;

    public LocalFileStorageService(IHostEnvironment env, IConfiguration config)
    {
        _uploadsRootPath = ResolveUploadsPath(env.ContentRootPath, config);
    }

    public async Task<string> SaveAsync(Stream content, string tenantSlug, string folder, string fileName, string contentType)
    {
        var dirPath = Path.Combine(_uploadsRootPath, "tenants", tenantSlug, folder);
        Directory.CreateDirectory(dirPath);
        var filePath = Path.Combine(dirPath, fileName);
        using var fs = new FileStream(filePath, FileMode.Create);
        await content.CopyToAsync(fs);
        return $"/uploads/tenants/{tenantSlug}/{folder}/{fileName}";
    }

    public Task<string> GetUrlAsync(string storedPath) => Task.FromResult(storedPath);

    public Task DeleteAsync(string storedPath)
    {
        // storedPath = "/uploads/tenants/{slug}/{folder}/{name}"
        // Remove o prefixo "/uploads/" e combina com a raiz física
        var withoutLeadingSlash = storedPath.TrimStart('/');
        var withoutUploadsPrefix = withoutLeadingSlash.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase)
            ? withoutLeadingSlash["uploads/".Length..]
            : withoutLeadingSlash;

        var fullPath = Path.Combine(_uploadsRootPath, withoutUploadsPrefix.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Resolve o caminho raiz dos uploads — mesma lógica do Program.cs GetUploadsPath.
    /// Centralizado aqui para evitar divergência entre o controller e o middleware de arquivos estáticos.
    /// </summary>
    public static string ResolveUploadsPath(string contentRootPath, IConfiguration config)
    {
        var configured = config["Uploads:Path"] ?? Environment.GetEnvironmentVariable("UPLOADS_PATH");
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured.Trim());

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")))
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? @"D:\home";
            return Path.Combine(home, "data", "uploads");
        }

        return Path.Combine(contentRootPath, "uploads");
    }
}
