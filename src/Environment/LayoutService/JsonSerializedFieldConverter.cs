using System.Text.Json;
using System.Text.Json.Serialization;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Serialization.Fields;

namespace LayoutService
{
    public class JsonSerializedFieldConverter : JsonConverter<JsonSerializedField>
    {
        public override void Write(Utf8JsonWriter writer, JsonSerializedField value, JsonSerializerOptions options)
        {
            // Use the ToString method to get the raw JSON string and write it directly
            writer.WriteRawValue(value.ToString());
        }

        public override JsonSerializedField Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Deserialize the JSON into a JsonDocument and pass it to the JsonSerializedField constructor
            using var jsonDocument = JsonDocument.ParseValue(ref reader);
            return new JsonSerializedField(jsonDocument);
        }
    }
}