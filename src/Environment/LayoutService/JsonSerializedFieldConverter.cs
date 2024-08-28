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
            // Use reflection to get the private _json field
            var jsonFieldInfo = typeof(JsonSerializedField).GetField("_json", BindingFlags.NonPublic | BindingFlags.Instance);
            if (jsonFieldInfo != null)
            {
                var json = jsonFieldInfo.GetValue(value) as string;
                if (json != null)
                {
                    writer.WriteRawValue(json);
                    return;
                }
            }

            // Fallback if reflection fails
            writer.WriteStringValue("Error: Unable to access the JSON data");
        }

        public override JsonSerializedField Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Deserialize the JSON into a JsonDocument and pass it to the JsonSerializedField constructor
            using var jsonDocument = JsonDocument.ParseValue(ref reader);
            return new JsonSerializedField(jsonDocument);
        }
    }
}