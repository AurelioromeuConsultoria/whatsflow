namespace WhatsFlow.Application.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Salva o stream no storage e retorna o caminho armazenável (path local ou chave S3).
    /// </summary>
    Task<string> SaveAsync(Stream content, string tenantSlug, string folder, string fileName, string contentType);

    /// <summary>
    /// Resolve o caminho armazenado para uma URL acessível.
    /// Local: retorna o path como está. S3: retorna URL pública ou pré-assinada.
    /// </summary>
    Task<string> GetUrlAsync(string storedPath);

    /// <summary>
    /// Exclui o arquivo do storage.
    /// </summary>
    Task DeleteAsync(string storedPath);

    /// <summary>
    /// Indica se o storage é local (disco). false = nuvem (S3-compatible).
    /// Usado para decidir se a sincronização dev→prod ainda é necessária.
    /// </summary>
    bool IsLocalStorage { get; }
}
