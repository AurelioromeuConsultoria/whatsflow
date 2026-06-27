using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IVoluntarioService
{
    Task<IEnumerable<VoluntarioDto>> GetAllAsync();
    Task<VoluntarioDto?> GetByIdAsync(int id);
    Task<IEnumerable<VoluntarioDto>> GetVoluntariosPorPessoaAsync(int pessoaId);
    Task<IEnumerable<VoluntarioDto>> GetVoluntariosPorEquipeAsync(int equipeId);
    Task<VoluntarioDto> CreateAsync(CriarVoluntarioDto dto);
    Task<VoluntarioDto> UpdateAsync(int id, AtualizarVoluntarioDto dto);
    Task DeleteAsync(int id);
}

public class VoluntarioService : IVoluntarioService
{
    private readonly IVoluntarioRepository _repository;
    private readonly IEquipeRepository _equipeRepository;
    private readonly ICargoRepository _cargoRepository;
    private readonly IPessoaRepository _pessoaRepository;
    private readonly ITenantContext _tenantContext;

    public VoluntarioService(IVoluntarioRepository repository, IEquipeRepository equipeRepository, ICargoRepository cargoRepository, IPessoaRepository pessoaRepository)
        : this(repository, equipeRepository, cargoRepository, pessoaRepository, new DefaultTenantContext())
    {
    }

    public VoluntarioService(IVoluntarioRepository repository, IEquipeRepository equipeRepository, ICargoRepository cargoRepository, IPessoaRepository pessoaRepository, ITenantContext tenantContext)
    {
        _repository = repository;
        _equipeRepository = equipeRepository;
        _cargoRepository = cargoRepository;
        _pessoaRepository = pessoaRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<VoluntarioDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return entities.Select(MapToDto);
    }

    public async Task<VoluntarioDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity != null ? MapToDto(entity) : null;
    }

    public async Task<IEnumerable<VoluntarioDto>> GetVoluntariosPorPessoaAsync(int pessoaId)
    {
        var entities = await _repository.GetByPessoaIdAsync(pessoaId);
        return entities.Select(MapToDto);
    }

    public async Task<IEnumerable<VoluntarioDto>> GetVoluntariosPorEquipeAsync(int equipeId)
    {
        var entities = await _repository.GetByEquipeAsync(equipeId);
        return entities.Select(MapToDto);
    }

    public async Task<VoluntarioDto> CreateAsync(CriarVoluntarioDto dto)
    {
        // Garantir referencias válidas
        var equipe = await _equipeRepository.GetByIdAsync(dto.EquipeId) ?? throw new ArgumentException("Equipe inválida");
        var cargo = await _cargoRepository.GetByIdAsync(dto.CargoId) ?? throw new ArgumentException("Cargo inválido");

        if (dto.PessoaId <= 0) throw new ArgumentException("Pessoa inválida");
        var pessoa = await _pessoaRepository.GetByIdAsync(dto.PessoaId) ?? throw new ArgumentException("Pessoa não encontrada");

        // Evitar duplicar o mesmo vínculo (Pessoa + Equipe + Cargo)
        if (await _repository.ExistsByPessoaEquipeCargoAsync(pessoa.Id, dto.EquipeId, dto.CargoId))
            throw new ArgumentException("Esta pessoa já está cadastrada como voluntária para esta equipe e cargo");

        // Opcional: atualizar dados de contato da pessoa a partir do cadastro de voluntário
        await AtualizarContatoPessoaSeNecessarioAsync(pessoa, dto.Email, dto.Telefone, dto.WhatsApp, dto.DataNascimento);

        var entity = new Voluntario
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            PessoaId = pessoa.Id,
            EquipeId = dto.EquipeId,
            CargoId = dto.CargoId,
            DataCadastro = DateTime.Now
        };

        var created = await _repository.CreateAsync(entity);
        // Recarregar para obter navegações
        var loaded = await _repository.GetByIdAsync(created.Id) ?? created;
        loaded.Equipe = equipe;
        loaded.Cargo = cargo;
        return MapToDto(loaded);
    }

    public async Task<VoluntarioDto> UpdateAsync(int id, AtualizarVoluntarioDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Voluntário não encontrado");

        // Validar novas referencias
        var equipe = await _equipeRepository.GetByIdAsync(dto.EquipeId) ?? throw new ArgumentException("Equipe inválida");
        var cargo = await _cargoRepository.GetByIdAsync(dto.CargoId) ?? throw new ArgumentException("Cargo inválido");

        if (dto.PessoaId <= 0) throw new ArgumentException("Pessoa inválida");
        var pessoa = await _pessoaRepository.GetByIdAsync(dto.PessoaId) ?? throw new ArgumentException("Pessoa não encontrada");

        // Evitar duplicar o mesmo vínculo (Pessoa + Equipe + Cargo)
        if (await _repository.ExistsByPessoaEquipeCargoAsync(pessoa.Id, dto.EquipeId, dto.CargoId, ignoreVoluntarioId: entity.Id))
            throw new ArgumentException("Esta pessoa já está cadastrada como voluntária para esta equipe e cargo");

        // Atualizar vínculo (permite corrigir pessoa selecionada)
        entity.PessoaId = pessoa.Id;

        // Opcional: atualizar dados de contato da pessoa a partir do cadastro de voluntário
        await AtualizarContatoPessoaSeNecessarioAsync(pessoa, dto.Email, dto.Telefone, dto.WhatsApp, dto.DataNascimento);

        // Atualizar voluntário
        entity.EquipeId = dto.EquipeId;
        entity.CargoId = dto.CargoId;

        var updated = await _repository.UpdateAsync(entity);
        updated.Equipe = equipe;
        updated.Cargo = cargo;
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new ArgumentException("Voluntário não encontrado");
        }

        await _repository.DeleteAsync(id);
    }

    private static VoluntarioDto MapToDto(Voluntario v)
    {
        return new VoluntarioDto
        {
            Id = v.Id,
            PessoaId = v.PessoaId,
            Nome = v.Pessoa?.Nome ?? string.Empty,
            WhatsApp = v.Pessoa?.WhatsApp,
            Email = v.Pessoa?.Email,
            Telefone = v.Pessoa?.Telefone,
            EquipeId = v.EquipeId,
            NomeEquipe = v.Equipe?.Nome ?? string.Empty,
            CargoId = v.CargoId,
            NomeCargo = v.Cargo?.Nome ?? string.Empty,
            DataCadastro = v.DataCadastro
        };
    }

    private async Task AtualizarContatoPessoaSeNecessarioAsync(
        Pessoa pessoa,
        string? email,
        string? telefone,
        string? whatsApp,
        DateTime? dataNascimento)
    {
        var mudou = false;

        if (!string.IsNullOrWhiteSpace(email) && !string.Equals(email, pessoa.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existePessoa = await _pessoaRepository.GetByEmailAsync(email);
            if (existePessoa != null && existePessoa.Id != pessoa.Id)
                throw new ArgumentException("Email já cadastrado para outra pessoa");

            pessoa.Email = email;
            mudou = true;
        }

        if (!string.IsNullOrWhiteSpace(telefone) && telefone != pessoa.Telefone)
        {
            pessoa.Telefone = telefone;
            mudou = true;
        }

        if (!string.IsNullOrWhiteSpace(whatsApp) && whatsApp != pessoa.WhatsApp)
        {
            pessoa.WhatsApp = whatsApp;
            mudou = true;
        }

        if (dataNascimento.HasValue && dataNascimento.Value != pessoa.DataNascimento)
        {
            pessoa.DataNascimento = dataNascimento;
            mudou = true;
        }

        if (mudou)
        {
            await _pessoaRepository.UpdateAsync(pessoa);
        }
    }
}
