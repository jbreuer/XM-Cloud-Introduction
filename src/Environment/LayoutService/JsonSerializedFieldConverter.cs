using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Serialization.Fields;

namespace LayoutService
{
    public class JsonSerializedFieldConverter : JsonConverter<JsonSerializedField>
    {
        public override void Write(Utf8JsonWriter writer, JsonSerializedField value, JsonSerializerOptions options)
        {
            writer.WriteRawValue(value.GetRawValue());
        }

        public override JsonSerializedField Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Deserialize the JSON into a JsonDocument and pass it to the JsonSerializedField constructor
            using var jsonDocument = JsonDocument.ParseValue(ref reader);
            return new JsonSerializedField(jsonDocument);
        }
    }
}