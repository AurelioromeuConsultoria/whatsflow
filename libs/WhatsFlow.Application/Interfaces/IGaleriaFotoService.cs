using WhatsFlow.Application.DTOs;

namespace WhatsFlow.Application.Interfaces;

public class ArquivoUpload
{
    public string NomeArquivo { get; set; } = string.Empty;
    public byte[] Conteudo { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
}

public interface IGaleriaFotoService
{
    Task<IEnumerable<GaleriaFotoDto>> GetAllAsync();
    Task<IEnumerable<GaleriaFotoDto>> GetAtivasAsync();
    Task<GaleriaFotoDto?> GetByIdAsync(int id);
    Task<IEnumerable<GaleriaFotoDto>> GetByEventoIdAsync(int eventoId);
    Task<IEnumerable<GaleriaFotoDto>> GetByCategoriaMidiaIdAsync(int categoriaMidiaId);
    Task<GaleriaFotoDto> CreateAsync(CriarGaleriaFotoDto dto);
    Task<GaleriaFotoDto> UpdateAsync(int id, AtualizarGaleriaFotoDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> UploadFotosAsync(int galeriaId, List<ArquivoUpload> arquivos, string webRootPath);
    Task<bool> DefinirImagemDestaqueAsync(int galeriaId, string nomeArquivo);
    Task<List<FotoDto>> ListarFotosAsync(int galeriaId, string webRootPath);

    /// <summary>
    /// Sincroniza itens da galeria a partir dos arquivos no disco (para popular a tabela em galerias já existentes).
    /// Chamar uma vez em produção após o deploy para que a listagem funcione localmente com o mesmo DB.
    /// </summary>
    Task<int> SyncItensFromDiskAsync(int galeriaId, string webRootPath);
}

