using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GaleriasFotosController : ControllerBase
{
    private readonly IGaleriaFotoService _service;
    private readonly IWebHostEnvironment _environment;

    public GaleriasFotosController(IGaleriaFotoService service, IWebHostEnvironment environment)
    {
        _service = service;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GaleriaFotoDto>>> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("ativas")]
    public async Task<ActionResult<IEnumerable<GaleriaFotoDto>>> GetAtivas()
    {
        var items = await _service.GetAtivasAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GaleriaFotoDto>> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpGet("evento/{eventoId}")]
    public async Task<ActionResult<IEnumerable<GaleriaFotoDto>>> GetByEvento(int eventoId)
    {
        var items = await _service.GetByEventoIdAsync(eventoId);
        return Ok(items);
    }

    [HttpGet("categoria/{categoriaMidiaId}")]
    public async Task<ActionResult<IEnumerable<GaleriaFotoDto>>> GetByCategoria(int categoriaMidiaId)
    {
        var items = await _service.GetByCategoriaMidiaIdAsync(categoriaMidiaId);
        return Ok(items);
    }

    [HttpGet("{id}/fotos")]
    public async Task<ActionResult<List<FotoDto>>> ListarFotos(int id)
    {
        var basePath = GetUploadsBasePath();
        var fotos = await _service.ListarFotosAsync(id, basePath);
        
        if (fotos == null || fotos.Count == 0)
        {
            // Verificar se a galeria existe
            var galeria = await _service.GetByIdAsync(id);
            if (galeria == null) return NotFound("Galeria não encontrada");
        }

        return Ok(fotos);
    }

    [HttpPost]
    public async Task<ActionResult<GaleriaFotoDto>> Create(CriarGaleriaFotoDto dto)
    {
        try
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<GaleriaFotoDto>> Update(int id, AtualizarGaleriaFotoDto dto)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, dto);
            return Ok(updated);
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var galeria = await _service.GetByIdAsync(id);
        if (galeria == null) return NotFound();

        // Deletar diretório físico (mesmo path usado no upload)
        var basePath = GetUploadsBasePath();
        var fullPath = Path.Combine(basePath, galeria.CaminhoDiretorio.TrimStart('/', '\\'));
        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, true);
        }

        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFotos(int id, List<IFormFile> arquivos)
    {
        if (arquivos == null || arquivos.Count == 0)
            return BadRequest("Nenhum arquivo enviado");

        var arquivosUpload = new List<ArquivoUpload>();
        foreach (var arquivo in arquivos)
        {
            if (arquivo.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await arquivo.CopyToAsync(memoryStream);
                arquivosUpload.Add(new ArquivoUpload
                {
                    NomeArquivo = arquivo.FileName,
                    Conteudo = memoryStream.ToArray(),
                    ContentType = arquivo.ContentType
                });
            }
        }

        var basePath = GetUploadsBasePath();
        var success = await _service.UploadFotosAsync(id, arquivosUpload, basePath);
        if (!success) return NotFound("Galeria não encontrada");

        return Ok(new { message = "Fotos enviadas com sucesso" });
    }

    [HttpPut("{id}/destaque")]
    public async Task<IActionResult> DefinirDestaque(int id, [FromBody] string nomeArquivo)
    {
        var success = await _service.DefinirImagemDestaqueAsync(id, nomeArquivo);
        if (!success) return NotFound("Galeria ou arquivo não encontrado");

        return Ok(new { message = "Imagem de destaque definida com sucesso" });
    }

    /// <summary>
    /// Sincroniza a lista de fotos da galeria a partir dos arquivos no disco (backfill para GaleriaFotoItem).
    /// Chamar uma vez em produção por galeria existente para que a listagem funcione localmente com o mesmo DB.
    /// </summary>
    [HttpPost("{id}/sync-itens")]
    public async Task<ActionResult<object>> SyncItens(int id)
    {
        var galeria = await _service.GetByIdAsync(id);
        if (galeria == null) return NotFound("Galeria não encontrada");

        var basePath = GetUploadsBasePath();
        var added = await _service.SyncItensFromDiskAsync(id, basePath);
        return Ok(new { message = "Sincronização concluída", itensAdicionados = added });
    }

    /// <summary>
    /// Retorna o caminho base para uploads (alinhado com Program.cs / UseStaticFiles).
    /// Local: ContentRootPath. Azure: HOME/data (CaminhoDiretorio será montado sob uploads).
    /// </summary>
    private string GetUploadsBasePath()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")))
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? @"D:\home";
            return Path.Combine(home, "data");
        }
        return _environment.ContentRootPath;
    }
}

