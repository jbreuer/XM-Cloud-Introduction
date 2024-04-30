using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.LayoutService.Client.Newtonsoft.Converters;
using Sitecore.LayoutService.Client.Response.Model.Presentation;

namespace Mvp.Project.MvpSite;

public class CustomDeviceJsonConverter : JsonConverter<List<Device>>
{
    public override bool CanWrite => true;

    public override List<Device> ReadJson(
        JsonReader reader, Type objectType, List<Device> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // Use existing logic or modify as needed
        return new DeviceJsonConverter().ReadJson(reader, objectType, existingValue, hasExistingValue, serializer);
    }

    public override void WriteJson(JsonWriter writer, List<Device> value, JsonSerializer serializer)
    {
        // Implement custom serialization logic
        JArray array = new JArray();
        foreach (Device device in value)
        {
            JObject obj = JObject.FromObject(device, serializer);
            array.Add(obj);
        }
        array.WriteTo(writer);
    }
}
