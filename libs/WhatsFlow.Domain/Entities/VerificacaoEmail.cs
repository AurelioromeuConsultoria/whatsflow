using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

/// <summary>
/// Token de verificação de e-mail emitido no signup self-service. Enquanto não
/// confirmado, o tenant e o usuário admin ficam inativos. Nível plataforma — NÃO é
/// ITenantEntity (o endpoint de confirmação é público, sem contexto de tenant).
/// </summary>
public class VerificacaoEmail
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [Required]
    [MaxLength(80)]
    public string Token { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiraEm { get; set; }

    public DateTime? ConfirmadoEm { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
