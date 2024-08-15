using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL;

namespace LayoutService;

public class GraphQLRequestConverter : JsonConverter<GraphQLRequest>
{
    public override GraphQLRequest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        var request = new GraphQLRequest();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return request;

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "query":
                        request.Query = reader.TokenType == JsonTokenType.String
                            ? reader.GetString()
                            : JsonSerializer.Deserialize<JsonElement>(ref reader, options).GetRawText();
                        break;
                    case "operationName":
                        request.OperationName = reader.TokenType == JsonTokenType.String
                            ? reader.GetString()
                            : JsonSerializer.Deserialize<JsonElement>(ref reader, options).GetRawText();
                        break;
                    case "variables":
                        request.Variables = JsonSerializer.Deserialize<JsonElement>(ref reader, options).ToString();
                        break;
                    default:
                        throw new JsonException();
                }
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, GraphQLRequest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.Query != null)
        {
            writer.WriteString("query", value.Query);
        }

        if (value.OperationName != null)
        {
            writer.WriteString("operationName", value.OperationName);
        }

        if (value.Variables != null)
        {
            writer.WritePropertyName("variables");
            writer.WriteRawValue(value.Variables.ToString());
        }

        writer.WriteEndObject();
    }
}
