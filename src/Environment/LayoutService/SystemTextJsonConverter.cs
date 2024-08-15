using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Serialization.Fields;

namespace LayoutService;

public class SystemTextJsonConverter : JsonConverter<JsonSerializedField>
{
    public override void Write(Utf8JsonWriter writer, JsonSerializedField value, JsonSerializerOptions options)
    {
        // Use reflection to access the private _json field
        var jsonFieldInfo = typeof(JsonSerializedField).GetField("_json", BindingFlags.NonPublic | BindingFlags.Instance);
        if (jsonFieldInfo != null)
        {
            var jsonFieldValue = jsonFieldInfo.GetValue(value);

            if (jsonFieldValue is JsonElement jsonElement)
            {
                jsonElement.WriteTo(writer);  // Write the JsonElement directly to the writer
            }
            else if (jsonFieldValue is string jsonString)
            {
                // If the _json field is a string, attempt to parse it as JSON
                using (var jsonDocument = JsonDocument.Parse(jsonString))
                {
                    jsonDocument.RootElement.WriteTo(writer);
                }
            }
            else
            {
                // If the _json field is neither a JsonElement nor a string, handle the error
                writer.WriteStringValue("Error: Unsupported JSON data type");
            }
        }
        else
        {
            // If reflection fails to find the field, write some default or error handling logic
            writer.WriteStringValue("Error: Unable to access the JSON data");
        }
    }

    public override JsonSerializedField Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Directly deserialize the JSON to JsonSerializedField
        return JsonSerializer.Deserialize<JsonSerializedField>(ref reader, options);
    }
}