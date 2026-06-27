using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public class GaleriaFotoService : IGaleriaFotoService
{
    private readonly IGaleriaFotoRepository _repository;
    private readonly IGaleriaFotoItemRepository _itemRepository;
    private readonly ITenantContext _tenantContext;

    public GaleriaFotoService(IGaleriaFotoRepository repository, IGaleriaFotoItemRepository itemRepository, ITenantContext tenantContext)
    {
        _repository = repository;
        _itemRepository = itemRepository;
        _tenantContext = tenantContext;
    }

    public GaleriaFotoService(IGaleriaFotoRepository repository, IGaleriaFotoItemRepository itemRepository)
        : this(repository, itemRepository, new DefaultTenantContext())
    {
    }

    public async Task<IEnumerable<GaleriaFotoDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return entities.Select(MapToDto);
    }

    public async Task<IEnumerable<GaleriaFotoDto>> GetAtivasAsync()
    {
        var entities = await _repository.GetAtivasAsync();
        return entities.Select(MapToDto);
    }

    public async Task<GaleriaFotoDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity != null ? MapToDto(entity) : null;
    }

    public async Task<IEnumerable<GaleriaFotoDto>> GetByEventoIdAsync(int eventoId)
    {
        var entities = await _repository.GetByEventoIdAsync(eventoId);
        return entities.Select(MapToDto);
    }

    public async Task<IEnumerable<GaleriaFotoDto>> GetByCategoriaMidiaIdAsync(int categoriaMidiaId)
    {
        var entities = await _repository.GetByCategoriaMidiaIdAsync(categoriaMidiaId);
        return entities.Select(MapToDto);
    }

    public async Task<GaleriaFotoDto> CreateAsync(CriarGaleriaFotoDto dto)
    {
        // Criar diretório base para a galeria
        var basePath = Path.Combine("uploads", "fotos");
        var galeriaPath = Path.Combine(basePath, Guid.NewGuid().ToString());

        var entity = new GaleriaFoto
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            Data = dto.Data,
            CaminhoDiretorio = galeriaPath,
            EventoId = dto.EventoId,
            CategoriaMidiaId = dto.CategoriaMidiaId,
            Ativo = dto.Ativo,
            QuantidadeFotos = 0,
            DataCriacao = DateTime.Now
        };

        var created = await _repository.CreateAsync(entity);

        // O diretório será criado no momento do upload

        return MapToDto(created);
    }

    public async Task<GaleriaFotoDto> UpdateAsync(int id, AtualizarGaleriaFotoDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Galeria não encontrada");

        entity.Nome = dto.Nome;
        entity.Descricao = dto.Descricao;
        entity.Data = dto.Data;
        entity.EventoId = dto.EventoId;
        entity.CategoriaMidiaId = dto.CategoriaMidiaId;
        entity.Ativo = dto.Ativo;

        var updated = await _repository.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return false;

        // O diretório será deletado no controller que tem acesso ao WebRootPath
        return await _repository.DeleteAsync(id);
    }

    public async Task<bool> UploadFotosAsync(int galeriaId, List<ArquivoUpload> arquivos, string webRootPath)
    {
        var galeria = await _repository.GetByIdAsync(galeriaId);
        if (galeria == null) return false;

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var maxFileSize = 10 * 1024 * 1024; // 10MB

        var basePath = Path.Combine(webRootPath, galeria.CaminhoDiretorio);
        var originalPath = Path.Combine(basePath, "original");
        var thumbnailPath = Path.Combine(basePath, "thumbnail");
        
        Directory.CreateDirectory(originalPath);
        Directory.CreateDirectory(thumbnailPath);

        var uploadedCount = 0;
        var primeiraFoto = true;
        var novosItens = new List<GaleriaFotoItem>();
        const int thumbnailSize = 400; // Tamanho máximo do thumbnail (largura ou altura)

        foreach (var arquivo in arquivos)
        {
            if (arquivo.Conteudo.Length == 0 || arquivo.Conteudo.Length > maxFileSize) continue;

            var extension = Path.GetExtension(arquivo.NomeArquivo).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension)) continue;

            var fileName = $"{Guid.NewGuid()}{extension}";
            var originalFilePath = Path.Combine(originalPath, fileName);
            var thumbnailFilePath = Path.Combine(thumbnailPath, fileName);

            // Salvar foto original
            await File.WriteAllBytesAsync(originalFilePath, arquivo.Conteudo);

            // Gerar thumbnail
            try
            {
                using var image = Image.Load(arquivo.Conteudo);
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(thumbnailSize, thumbnailSize),
                    Mode = ResizeMode.Max // Mantém proporção, ajusta para caber no tamanho máximo
                }));

                await image.SaveAsync(thumbnailFilePath);
            }
            catch
            {
                // Se falhar ao processar imagem, continua sem thumbnail
                // A foto original já foi salva
            }

            uploadedCount++;

            // Definir primeira foto como destaque se não houver
            // Usar o thumbnail como destaque (mais leve para carregar)
            var isDestaque = primeiraFoto && string.IsNullOrEmpty(galeria.ImagemDestaque);
            if (isDestaque)
            {
                galeria.ImagemDestaque = Path.Combine(galeria.CaminhoDiretorio, "thumbnail", fileName).Replace("\\", "/");
                primeiraFoto = false;
            }

            novosItens.Add(new GaleriaFotoItem
            {
                TenantId = galeria.TenantId,
                GaleriaFotoId = galeriaId,
                NomeArquivo = fileName,
                Destaque = isDestaque,
                Ordem = uploadedCount - 1
            });
        }

        if (novosItens.Count > 0)
            await _itemRepository.AddRangeAsync(novosItens);

        // Atualizar quantidade de fotos (contar apenas as originais)
        var fotoFiles = Directory.GetFiles(originalPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Count();

        galeria.QuantidadeFotos = fotoFiles;
        await _repository.UpdateAsync(galeria);

        return uploadedCount > 0;
    }

    public async Task<bool> DefinirImagemDestaqueAsync(int galeriaId, string nomeArquivo)
    {
        var galeria = await _repository.GetByIdAsync(galeriaId);
        if (galeria == null) return false;

        // Usar o thumbnail como destaque (mais leve para carregar)
        var filePath = Path.Combine(galeria.CaminhoDiretorio, "thumbnail", nomeArquivo).Replace("\\", "/");
        galeria.ImagemDestaque = filePath;
        await _repository.UpdateAsync(galeria);
        await _itemRepository.SetDestaqueAsync(galeriaId, nomeArquivo);

        return true;
    }

    public async Task<List<FotoDto>> ListarFotosAsync(int galeriaId, string webRootPath)
    {
        var galeria = await _repository.GetByIdAsync(galeriaId);
        if (galeria == null) return new List<FotoDto>();

        // Preferir listagem a partir do banco (permite mesmo DB em dev com arquivos em produção)
        var itens = await _itemRepository.GetByGaleriaIdAsync(galeriaId);
        if (itens.Count > 0)
        {
            return itens.Select(i => new FotoDto
            {
                NomeArquivo = i.NomeArquivo,
                Destaque = i.Destaque
            }).ToList();
        }

        // Fallback: listar do disco (comportamento anterior; galerias antigas sem itens no banco)
        var fotos = new List<FotoDto>();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var thumbnailPath = Path.Combine(webRootPath, galeria.CaminhoDiretorio, "thumbnail");

        if (!Directory.Exists(thumbnailPath))
            return fotos;

        var arquivos = Directory.GetFiles(thumbnailPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => Path.GetFileName(f))
            .ToList();

        foreach (var arquivo in arquivos)
        {
            var nomeArquivo = Path.GetFileName(arquivo);
            var isDestaque = galeria.ImagemDestaque != null &&
                             galeria.ImagemDestaque.Contains(nomeArquivo);

            fotos.Add(new FotoDto
            {
                NomeArquivo = nomeArquivo,
                Destaque = isDestaque
            });
        }

        return fotos;
    }

    public async Task<int> SyncItensFromDiskAsync(int galeriaId, string webRootPath)
    {
        var galeria = await _repository.GetByIdAsync(galeriaId);
        if (galeria == null) return 0;

        var existing = await _itemRepository.GetByGaleriaIdAsync(galeriaId);
        var existingNames = new HashSet<string>(existing.Select(i => i.NomeArquivo), StringComparer.OrdinalIgnoreCase);

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var thumbnailPath = Path.Combine(webRootPath, galeria.CaminhoDiretorio, "thumbnail");
        if (!Directory.Exists(thumbnailPath)) return 0;

        var arquivos = Directory.GetFiles(thumbnailPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => Path.GetFileName(f))
            .ToList();

        var ordem = existing.Count;
        var novos = new List<GaleriaFotoItem>();
        foreach (var arquivo in arquivos)
        {
            var nomeArquivo = Path.GetFileName(arquivo);
            if (existingNames.Contains(nomeArquivo)) continue;

            var isDestaque = galeria.ImagemDestaque != null && galeria.ImagemDestaque.Contains(nomeArquivo);
            novos.Add(new GaleriaFotoItem
            {
                TenantId = galeria.TenantId,
                GaleriaFotoId = galeriaId,
                NomeArquivo = nomeArquivo,
                Destaque = isDestaque,
                Ordem = ordem++
            });
        }

        if (novos.Count > 0)
            await _itemRepository.AddRangeAsync(novos);

        return novos.Count;
    }

    private static GaleriaFotoDto MapToDto(GaleriaFoto g)
    {
        return new GaleriaFotoDto
        {
            Id = g.Id,
            Nome = g.Nome,
            Descricao = g.Descricao,
            Data = g.Data,
            CaminhoDiretorio = g.CaminhoDiretorio,
            ImagemDestaque = g.ImagemDestaque,
            QuantidadeFotos = g.QuantidadeFotos,
            Ativo = g.Ativo,
            EventoId = g.EventoId,
            EventoTitulo = g.Evento?.Titulo,
            CategoriaMidiaId = g.CategoriaMidiaId,
            CategoriaMidiaNome = g.CategoriaMidia?.Nome,
            DataCriacao = g.DataCriacao
        };
    }
}
