using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.LayoutService.Client.Newtonsoft.Model;

namespace Mvp.Project.MvpSite;

public class NewtonsoftFieldReaderJsonConverter : JsonConverter<NewtonsoftFieldReader>
{
    public override void WriteJson(JsonWriter writer, NewtonsoftFieldReader value, JsonSerializer serializer)
    {
        // Use reflection to access the private _json field
        var jsonFieldInfo = typeof(NewtonsoftFieldReader).GetField("_json", BindingFlags.NonPublic | BindingFlags.Instance);
        if (jsonFieldInfo != null)
        {
            JToken jsonToken = (JToken)jsonFieldInfo.GetValue(value);
            jsonToken.WriteTo(writer);  // Write the JToken directly to the writer
        }
        else
        {
            // If reflection fails to find the field, write some default or error handling logic
            writer.WriteValue("Error: Unable to access the JSON data");
        }
    }

    public override NewtonsoftFieldReader ReadJson(JsonReader reader, Type objectType, NewtonsoftFieldReader existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // Implement this if you need custom deserialization logic
        // For now, you might just revert to the default behavior:
        return serializer.Deserialize<NewtonsoftFieldReader>(reader);
    }
}