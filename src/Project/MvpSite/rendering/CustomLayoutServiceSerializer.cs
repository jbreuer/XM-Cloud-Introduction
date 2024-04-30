using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.LayoutService.Client;
using Sitecore.LayoutService.Client.Newtonsoft.Converters;
using Sitecore.LayoutService.Client.Newtonsoft.Extensions;
using Sitecore.LayoutService.Client.Response;

namespace Mvp.Project.MvpSite;

public class CustomLayoutServiceSerializer : ISitecoreLayoutSerializer
{
    private static readonly Lazy<JsonSerializerSettings> _settings = new Lazy<JsonSerializerSettings>(CreateSerializerSettings);

    public SitecoreLayoutResponseContent Deserialize(string data)
    {
        SitecoreLayoutResponseContent layoutResponseContent = JsonConvert.DeserializeObject<SitecoreLayoutResponseContent>(data, _settings.Value);
        layoutResponseContent.ContextRawData = JObject.Parse(data)["sitecore"]?["context"]?.ToString();
        return layoutResponseContent;
    }

    private static JsonSerializerSettings CreateSerializerSettings()
    {
        var settings = new JsonSerializerSettings();
        // settings.SetDefaults(); // Ensure other default settings are applied
        settings.Converters.Add(new CustomFieldReaderJsonConverter(new FieldReaderJsonConverter())); // Add your custom converter
        settings.Converters.Add(new CustomPlaceholderJsonConverter()); // Add your custom converter
        settings.Converters.Add(new CustomDeviceJsonConverter()); // Add your custom converter
        return settings;
    }
}