namespace WhatsFlow.Application.DTOs;

public class IndisponibilidadeVoluntarioDto
{
    public int Id { get; set; }
    public int VoluntarioId { get; set; }
    public string? VoluntarioNome { get; set; }
    public DateTime Data { get; set; }
    public string? Motivo { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class CriarIndisponibilidadeVoluntarioDto
{
    public int VoluntarioId { get; set; }
    public DateTime Data { get; set; }
    public string? Motivo { get; set; }
}
