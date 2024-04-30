using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.LayoutService.Client.Newtonsoft.Converters;
using Sitecore.LayoutService.Client.Response.Model;

namespace Mvp.Project.MvpSite;

public class CustomPlaceholderJsonConverter : JsonConverter<Placeholder>
{
    public override bool CanWrite => true;

    public override Placeholder ReadJson(
        JsonReader reader, Type objectType, Placeholder existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // Use existing logic or modify as needed
        return new PlaceholderJsonConverter().ReadJson(reader, objectType, existingValue, hasExistingValue, serializer);
    }

    public override void WriteJson(JsonWriter writer, Placeholder value, JsonSerializer serializer)
    {
        // Implement custom serialization logic
        JArray array = new JArray();
        foreach (var feature in value)
        {
            JObject obj = JObject.FromObject(feature, serializer);
            array.Add(obj);
        }
        array.WriteTo(writer);
    }
}
