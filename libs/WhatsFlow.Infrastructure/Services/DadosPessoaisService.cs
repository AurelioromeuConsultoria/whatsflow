using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Services;

/// <summary>
/// Implementa os direitos do titular (LGPD). Usa o DbContext diretamente: o filtro
/// global de tenant garante que só dados do tenant corrente sejam lidos/alterados.
/// </summary>
public class DadosPessoaisService : IDadosPessoaisService
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUser;

    public DadosPessoaisService(
        WhatsFlowDbContext context,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser)
    {
        _context = context;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<DadosPessoaisExportDto?> ExportarAsync(int pessoaId)
    {
        var pessoa = await _context.Pessoas.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pessoaId);
        if (pessoa == null)
        {
            return null;
        }

        var export = new DadosPessoaisExportDto
        {
            GeradoEm = DateTime.UtcNow,
            Pessoa = new PessoaDadosDto
            {
                Id = pessoa.Id,
                Nome = pessoa.Nome,
                Email = pessoa.Email,
                Telefone = pessoa.Telefone,
                WhatsApp = pessoa.WhatsApp,
                DataNascimento = pessoa.DataNascimento,
                TipoPessoa = pessoa.TipoPessoa.ToString(),
                Ativo = pessoa.Ativo,
                DataCriacao = pessoa.DataCriacao
            }
        };

        export.Perfis = await _context.PessoasPerfis.AsNoTracking()
            .Where(p => p.PessoaId == pessoaId)
            .Select(p => new PerfilDadosDto { Perfil = p.Perfil.ToString(), DataInicio = p.DataInicio, DataFim = p.DataFim })
            .ToListAsync();

        export.Visitas = await _context.Visitantes.AsNoTracking()
            .Where(v => v.PessoaId == pessoaId)
            .Select(v => new VisitaDadosDto { DataVisita = v.DataVisita, Observacoes = v.Observacoes, DataCadastro = v.DataCadastro })
            .ToListAsync();

        export.Voluntariados = await _context.Voluntarios.AsNoTracking()
            .Where(v => v.PessoaId == pessoaId)
            .Select(v => new VoluntariadoDadosDto { EquipeId = v.EquipeId, CargoId = v.CargoId, DataCadastro = v.DataCadastro })
            .ToListAsync();

        var detalhe = await _context.CriancasDetalhes.AsNoTracking().FirstOrDefaultAsync(c => c.PessoaId == pessoaId);
        if (detalhe != null)
        {
            export.DetalheCrianca = new DetalheCriancaDadosDto
            {
                Alergias = detalhe.Alergias,
                RestricoesAlimentares = detalhe.RestricoesAlimentares,
                Observacoes = detalhe.Observacoes,
                SalaId = detalhe.SalaId,
                TurmaId = detalhe.TurmaId
            };
        }

        export.VinculosResponsaveis = await _context.ResponsaveisCriancas.AsNoTracking()
            .Where(r => r.CriancaPessoaId == pessoaId || r.ResponsavelPessoaId == pessoaId)
            .Select(r => new VinculoResponsavelDadosDto
            {
                Papel = r.CriancaPessoaId == pessoaId ? "crianca" : "responsavel",
                CriancaPessoaId = r.CriancaPessoaId,
                ResponsavelPessoaId = r.ResponsavelPessoaId,
                Parentesco = r.Parentesco,
                PodeRetirar = r.PodeRetirar
            })
            .ToListAsync();

        export.CheckinsKids = await _context.KidsCheckins.AsNoTracking()
            .Where(k => k.CriancaPessoaId == pessoaId)
            .Select(k => new CheckinDadosDto { CheckinTime = k.CheckinTime, CheckoutTime = k.CheckoutTime, Status = k.Status, Metodo = k.Metodo })
            .ToListAsync();

        export.OcorrenciasKids = await _context.KidsOcorrencias.AsNoTracking()
            .Where(o => o.CriancaPessoaId == pessoaId)
            .Select(o => new OcorrenciaDadosDto { Tipo = o.Tipo, Titulo = o.Titulo, Descricao = o.Descricao, Status = o.Status, DataCriacao = o.DataCriacao })
            .ToListAsync();

        export.Doacoes = await _context.DoacoesOnline.AsNoTracking()
            .Where(d => d.PessoaId == pessoaId)
            .Select(d => new DoacaoDadosDto
            {
                NomeDoador = d.NomeDoador,
                Valor = d.Valor,
                MetodoPagamento = d.MetodoPagamento.ToString(),
                Status = d.Status.ToString(),
                DataCriacao = d.DataCriacao,
                DataConfirmacao = d.DataConfirmacao
            })
            .ToListAsync();

        export.PreferenciasComunicacao = await _context.ComunicacaoPreferencias.AsNoTracking()
            .Where(p => p.PessoaId == pessoaId)
            .Select(p => new PreferenciaComunicacaoDadosDto { Canal = p.Canal.ToString(), Status = p.Status.ToString(), OrigemConsentimento = p.OrigemConsentimento })
            .ToListAsync();

        export.Consentimentos = await _context.Set<ConsentimentoRegistro>().AsNoTracking()
            .Where(c => c.PessoaId == pessoaId)
            .Select(c => new ConsentimentoDadosDto { Tipo = c.Tipo.ToString(), VersaoDocumento = c.VersaoDocumento, AceitoEm = c.AceitoEm, Origem = c.Origem, RevogadoEm = c.RevogadoEm })
            .ToListAsync();

        return export;
    }

    public async Task<AnonimizacaoResultadoDto?> AnonimizarAsync(int pessoaId)
    {
        var pessoa = await _context.Pessoas.FirstOrDefaultAsync(p => p.Id == pessoaId);
        if (pessoa == null)
        {
            return null;
        }

        var agora = DateTime.UtcNow;
        var marcador = $"Titular removido #{pessoa.Id}";
        var afetados = 1;

        // Dados cadastrais identificáveis.
        pessoa.Nome = marcador;
        pessoa.Email = null;
        pessoa.Telefone = null;
        pessoa.WhatsApp = null;
        pessoa.DataNascimento = null;
        pessoa.Ativo = false;

        // Dados sensíveis de saúde (módulo infantil).
        var detalhe = await _context.CriancasDetalhes.FirstOrDefaultAsync(c => c.PessoaId == pessoaId);
        if (detalhe != null)
        {
            detalhe.Alergias = null;
            detalhe.RestricoesAlimentares = null;
            detalhe.Observacoes = null;
            afetados++;
        }

        // Texto livre em visitas pode conter dados identificáveis.
        var visitas = await _context.Visitantes.Where(v => v.PessoaId == pessoaId).ToListAsync();
        foreach (var visita in visitas)
        {
            visita.Observacoes = null;
            afetados++;
        }

        // Dados de doador são anonimizados, mas o valor/registro contábil é preservado.
        var doacoes = await _context.DoacoesOnline.Where(d => d.PessoaId == pessoaId).ToListAsync();
        foreach (var doacao in doacoes)
        {
            doacao.NomeDoador = marcador;
            doacao.Email = null;
            doacao.WhatsApp = null;
            doacao.Documento = null;
            afetados++;
        }

        // Revoga consentimentos ativos (a anonimização encerra o tratamento baseado em consentimento).
        var consentimentos = await _context.Set<ConsentimentoRegistro>()
            .Where(c => c.PessoaId == pessoaId && c.RevogadoEm == null)
            .ToListAsync();
        foreach (var consentimento in consentimentos)
        {
            consentimento.RevogadoEm = agora;
            afetados++;
        }

        // Marcador explícito de auditoria (além do log automático por entidade do interceptor).
        _context.Set<AuditLog>().Add(new AuditLog
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            EntityName = nameof(Pessoa),
            EntityId = pessoa.Id.ToString(),
            Action = "Anonimizacao",
            UserId = _currentUser.UserId,
            UserName = _currentUser.UserName,
            UserEmail = _currentUser.UserEmail,
            IpAddress = _currentUser.IpAddress,
            CreatedAt = agora,
            ChangesJson = "{\"motivo\":\"LGPD - direito ao esquecimento (anonimizacao)\"}"
        });

        await _context.SaveChangesAsync();

        return new AnonimizacaoResultadoDto
        {
            PessoaId = pessoa.Id,
            NomeAnonimizado = marcador,
            AnonimizadoEm = agora,
            RegistrosAfetados = afetados
        };
    }
}
