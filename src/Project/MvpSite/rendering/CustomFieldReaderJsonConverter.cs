using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.LayoutService.Client.Newtonsoft.Converters;
using Sitecore.LayoutService.Client.Response.Model;

namespace Mvp.Project.MvpSite;

public class CustomFieldReaderJsonConverter : JsonConverter
{
    private readonly FieldReaderJsonConverter _originalConverter;

    public CustomFieldReaderJsonConverter(FieldReaderJsonConverter originalConverter)
    {
        _originalConverter = originalConverter;
    }

    public override bool CanConvert(Type objectType)
    {
        // Delegate to the original converter or implement your own logic
        return _originalConverter.CanConvert(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // Delegate back to the original converter
        return _originalConverter.ReadJson(reader, objectType, existingValue, serializer);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        // Fallback to default serialization if not handling the type
        serializer.Serialize(writer, value);
    }

    public override bool CanWrite => true;
}