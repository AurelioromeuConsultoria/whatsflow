using System.Text.Json;
using System.Text.Json.Serialization;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.JsonConverters;

public sealed class TipoPessoaJsonConverter : JsonConverter<TipoPessoa>
{
    public override TipoPessoa Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s))
            {
                return TipoPessoa.Adulto;
            }

            if (Enum.TryParse<TipoPessoa>(s, ignoreCase: true, out var parsed))
            {
                return parsed;
            }

            throw new JsonException($"Valor inválido para TipoPessoa: '{s}'. Use 'Adulto' ou 'Crianca'.");
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var i))
        {
            if (Enum.IsDefined(typeof(TipoPessoa), i))
            {
                return (TipoPessoa)i;
            }

            throw new JsonException($"Valor inválido para TipoPessoa: {i}.");
        }

        throw new JsonException($"Token inválido para TipoPessoa: {reader.TokenType}.");
    }

    public override void Write(Utf8JsonWriter writer, TipoPessoa value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

