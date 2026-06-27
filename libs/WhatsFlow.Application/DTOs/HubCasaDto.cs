namespace WhatsFlow.Application.DTOs;

public class HubCasaDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int AbertoPorId { get; set; }
    public string AbertoPorNome { get; set; } = string.Empty;
    public int LiderId { get; set; }
    public string LiderNome { get; set; } = string.Empty;
    public int TimoteoId { get; set; }
    public string TimoteoNome { get; set; } = string.Empty;
    public string EnderecoCompleto { get; set; } = string.Empty;
    public string Anfitriao { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
}

public class CriarHubCasaDto
{
    public string Nome { get; set; } = string.Empty;
    public int AbertoPorId { get; set; }
    public int LiderId { get; set; }
    public int TimoteoId { get; set; }
    public string EnderecoCompleto { get; set; } = string.Empty;
    public string Anfitriao { get; set; } = string.Empty;
}

public class AtualizarHubCasaDto
{
    public string Nome { get; set; } = string.Empty;
    public int AbertoPorId { get; set; }
    public int LiderId { get; set; }
    public int TimoteoId { get; set; }
    public string EnderecoCompleto { get; set; } = string.Empty;
    public string Anfitriao { get; set; } = string.Empty;
}
