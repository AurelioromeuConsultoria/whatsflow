# 📸 Endpoint para Listagem de Fotos da Galeria

## Status
✅ **Implementado** — O endpoint existe e está em uso. A listagem de fotos está funcional.

## Descrição
O backend expõe um endpoint que retorna a lista de todas as fotos de uma galeria.

## Especificação do Endpoint

### Rota
```
GET /api/galeriasFotos/{id}/fotos
```

### Headers
```
Authorization: Bearer {token}  // Se autenticação for necessária
```

### Resposta Esperada

**Status:** `200 OK`

**Body (JSON):**
```json
[
  {
    "nomeArquivo": "foto1.jpg",
    "destaque": true
  },
  {
    "nomeArquivo": "foto2.jpg",
    "destaque": false
  },
  {
    "nomeArquivo": "foto3.jpg",
    "destaque": false
  }
]
```

### Estrutura dos Dados

Cada item do array deve ter:
- `nomeArquivo` (string, obrigatório): Nome do arquivo da foto (ex: "foto1.jpg")
- `destaque` (boolean, opcional): Indica se é a foto de destaque

## Exemplo de Implementação (C# / ASP.NET Core)

```csharp
[HttpGet("{id}/fotos")]
public async Task<ActionResult<List<FotoDto>>> ListarFotos(int id)
{
    var galeria = await _context.GaleriasFotos
        .FirstOrDefaultAsync(g => g.Id == id);
    
    if (galeria == null)
    {
        return NotFound();
    }

    var fotos = new List<FotoDto>();
    var diretorioThumbnail = Path.Combine(
        _webHostEnvironment.ContentRootPath,
        "uploads",
        "fotos",
        galeria.CaminhoDiretorio,
        "thumbnail"
    );

    if (Directory.Exists(diretorioThumbnail))
    {
        var arquivos = Directory.GetFiles(diretorioThumbnail, "*.jpg")
            .Concat(Directory.GetFiles(diretorioThumbnail, "*.jpeg"))
            .Concat(Directory.GetFiles(diretorioThumbnail, "*.png"))
            .Concat(Directory.GetFiles(diretorioThumbnail, "*.gif"))
            .Concat(Directory.GetFiles(diretorioThumbnail, "*.webp"));

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
    }

    return Ok(fotos);
}

public class FotoDto
{
    public string NomeArquivo { get; set; }
    public bool Destaque { get; set; }
}
```

## Exemplo Alternativo (mais simples)

Se preferir, o endpoint pode retornar apenas um array de strings com os nomes dos arquivos:

```json
[
  "foto1.jpg",
  "foto2.jpg",
  "foto3.jpg"
]
```

Nesse caso, o frontend identificará qual é a foto de destaque comparando com o campo `imagemDestaque` da galeria.

## Comportamento Atual

- **Com endpoint:** Todas as fotos são listadas em um grid
- **Sem endpoint (404):** Apenas a foto de destaque é exibida

## Teste do Endpoint

Após implementar, teste com:

```bash
GET http://localhost:7000/api/galeriasFotos/1/fotos
Authorization: Bearer {seu-token}
```

## Observações

1. O endpoint deve listar apenas arquivos de imagem válidos (JPG, PNG, GIF, WEBP)
2. Deve verificar se o diretório existe antes de tentar ler os arquivos
3. Deve retornar `404` se a galeria não existir
4. Pode ser útil ordenar os arquivos por nome ou data de criação



