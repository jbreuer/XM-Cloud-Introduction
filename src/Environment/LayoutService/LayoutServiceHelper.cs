using System.Globalization;
using LayoutService;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.LayoutService.Client;
using Sitecore.LayoutService.Client.Newtonsoft;
using Sitecore.LayoutService.Client.Newtonsoft.Converters;
using Sitecore.LayoutService.Client.Newtonsoft.Model;
using Sitecore.LayoutService.Client.Request;
using Sitecore.LayoutService.Client.Response;
using Sitecore.LayoutService.Client.Response.Model;
using Sitecore.LayoutService.Client.Response.Model.Fields;
using Route = Sitecore.LayoutService.Client.Response.Model.Route;

public class LayoutServiceHelper
{
    private readonly HttpClient _client;
    private readonly ISitecoreLayoutSerializer _serializer;
    private readonly IOptionsSnapshot<HttpLayoutRequestHandlerOptions> _options;

    public LayoutServiceHelper(IHttpClientFactory httpClientFactory, ISitecoreLayoutSerializer serializer, IOptionsSnapshot<HttpLayoutRequestHandlerOptions> options)
    {
        _client = httpClientFactory.CreateClient("httpClient");
        _serializer = serializer;
        _options = options;
    }

    /// <summary>
    /// Fetches the layout data as a string from the specified endpoint.
    /// </summary>
    /// <param name="endpoint">The endpoint to fetch data from.</param>
    /// <param name="request">The Sitecore layout request.</param>
    /// <returns>The layout data as a string.</returns>
    public async Task<string> FetchLayoutDataAsync(string endpoint, SitecoreLayoutRequest request)
    {
        var options = _options.Get("httpClient");
        var message = BuildMessage(endpoint, request, options);
        var response = await _client.SendAsync(message);
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Fetches the layout data and deserializes it into a SitecoreLayoutResponseContent object.
    /// </summary>
    /// <param name="endpoint">The endpoint to fetch data from.</param>
    /// <param name="request">The Sitecore layout request.</param>
    /// <returns>The deserialized SitecoreLayoutResponseContent object.</returns>
    public async Task<SitecoreLayoutResponseContent> FetchLayoutDataContentAsync(string endpoint, SitecoreLayoutRequest request)
    {
        var str = await FetchLayoutDataAsync(endpoint, request);
        return _serializer.Deserialize(str);
    }

    /// <summary>
    /// Builds the hybrid placeholder data.
    /// </summary>
    /// <param name="route">The route containing placeholders and components.</param>
    /// <param name="hybridPlaceholderData">The JObject to store hybrid placeholder data.</param>
    /// <param name="componentConfigurations">The component configurations for updates.</param>
    public void BuildHybridPlaceholderData(Route route, JObject hybridPlaceholderData, Dictionary<string, ComponentConfig> componentConfigurations)
    {
        foreach (var (placeholderName, value) in route.Placeholders)
        {
            AddComponentsToHybridPlaceholderData(placeholderName, value.OfType<Component>(), hybridPlaceholderData, componentConfigurations);
        }
    }

    /// <summary>
    /// Adds components to the hybrid placeholder data.
    /// </summary>
    /// <param name="placeholderName">The placeholder name.</param>
    /// <param name="components">The components to add.</param>
    /// <param name="hybridPlaceholderData">The JObject to store hybrid placeholder data.</param>
    /// <param name="componentConfigurations">The component configurations for updates.</param>
    public void AddComponentsToHybridPlaceholderData(string placeholderName, IEnumerable<Component> components, JObject hybridPlaceholderData, Dictionary<string, ComponentConfig> componentConfigurations)
    {
        foreach (var component in components)
        {
            if (componentConfigurations.TryGetValue(component.Name, out var value))
            {
                var componentObject = new JObject
                {
                    ["placeholderName"] = $"{placeholderName}",
                };

                if (value.UseSsr)
                {
                    componentObject["useSsr"] = true;
                }

                hybridPlaceholderData[component.Id] = componentObject;
            }

            foreach (var childPlaceholder in component.Placeholders)
            {
                var childPlaceholderName = $"/{placeholderName}/{childPlaceholder.Key}-{{{component.Id.ToUpper()}}}-0";
                AddComponentsToHybridPlaceholderData(childPlaceholderName, childPlaceholder.Value.OfType<Component>(), hybridPlaceholderData, componentConfigurations);
            }
        }
    }

    /// <summary>
    /// Updates fields of a component based on the provided updates.
    /// </summary>
    /// <param name="component">The component to update.</param>
    /// <param name="updates">The updates to apply.</param>
    public void UpdateFields(Component component, Dictionary<string, (object newValue, FieldType fieldType)> updates)
    {
        var fieldsToKeep = updates.Keys.Select(k => k.ToLower()).ToHashSet();

        var fieldsToRemove = component.Fields.Keys.Where(k => !fieldsToKeep.Contains(k.ToLower())).ToList();
        foreach (var field in fieldsToRemove)
        {
            component.Fields.Remove(field);
        }

        foreach (var update in updates)
        {
            var fieldName = update.Key.ToLower();
            var (newValue, fieldType) = update.Value;

            if (component.Fields.TryGetValue(fieldName, out var fieldReader))
            {
                object? originalValue = fieldType switch
                {
                    FieldType.TextField => fieldReader.Read<TextField>()?.Value,
                    FieldType.RichTextField => fieldReader.Read<RichTextField>()?.Value,
                    _ => null
                };

                if (originalValue != null)
                {
                    newValue = fieldType switch
                    {
                        FieldType.TextField => $"{originalValue} {newValue}",
                        FieldType.RichTextField => new { value = $"{originalValue} {((dynamic)newValue).value}" },
                        _ => newValue
                    };
                }
            }

            var newValueToken = JToken.FromObject(newValue);
            var serializer = new JsonSerializer();
            var newFieldReader = new NewtonsoftFieldReader(serializer, newValueToken);

            component.Fields.Remove(fieldName);
            component.Fields[fieldName] = newFieldReader;
        }
    }

    /// <summary>
    /// Recursively updates fields of components within a route.
    /// </summary>
    /// <param name="route">The route containing components.</param>
    /// <param name="componentName">The name of the component to update.</param>
    /// <param name="updates">The updates to apply.</param>
    public void UpdateFieldsRecursively(Route? route, string componentName, Dictionary<string, (object newValue, FieldType fieldType)> updates)
    {
        if (route == null) return;

        foreach (var component in route.Placeholders.Values.SelectMany(placeholder => placeholder.OfType<Component>()))
        {
            UpdateFieldsRecursively(component, componentName, updates);
        }
    }

    /// <summary>
    /// Recursively updates fields of a component and its child components.
    /// </summary>
    /// <param name="component">The component to update.</param>
    /// <param name="componentName">The name of the component to update.</param>
    /// <param name="updates">The updates to apply.</param>
    public void UpdateFieldsRecursively(Component? component, string componentName, Dictionary<string, (object newValue, FieldType fieldType)> updates)
    {
        if (component == null) return;

        if (component.Name == componentName)
        {
            UpdateFields(component, updates);
        }

        foreach (var childComponent in component.Placeholders.Values.SelectMany(placeholder => placeholder.OfType<Component>()))
        {
            UpdateFieldsRecursively(childComponent, componentName, updates);
        }
    }

    /// <summary>
    /// Gets the current date formatted as a string.
    /// </summary>
    /// <returns>The formatted date string.</returns>
    public string GetDate()
    {
        Thread.Sleep(1500); // Simulate delay
        return DateTime.Now.ToString("f", CultureInfo.GetCultureInfo("en-US"));
    }

    /// <summary>
    /// Builds the HTTP request message for the given path and request.
    /// </summary>
    /// <param name="path">The path to the endpoint.</param>
    /// <param name="request">The Sitecore layout request.</param>
    /// <param name="options">The options for the request.</param>
    /// <returns>The constructed HttpRequestMessage.</returns>
    public HttpRequestMessage BuildMessage(string path, SitecoreLayoutRequest request, HttpLayoutRequestHandlerOptions? options)
    {
        var builder = new UriBuilder(_client.BaseAddress)
        {
            Path = path
        };

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
        if (options == null)
        {
            return httpRequestMessage;
        }

        foreach (var request1 in options.RequestMap)
        {
            request1(request, httpRequestMessage);
        }

        return httpRequestMessage;
    }

    /// <summary>
    /// Creates the JSON serializer settings.
    /// </summary>
    /// <returns>The configured JsonSerializerSettings.</returns>
    public JsonSerializerSettings CreateSerializerSettings()
    {
        return new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ContractResolver = CustomDataContractResolver.Instance,
            Converters = new List<JsonConverter>
            {
                new NewtonsoftFieldReaderJsonConverter(),
                new FieldReaderJsonConverter()
            }
        };
    }
}
