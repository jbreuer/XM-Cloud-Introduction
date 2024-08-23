using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL;
using Microsoft.AspNetCore.Mvc;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Request.Handlers.GraphQL;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Response;

namespace LayoutService;

public class GraphController : Controller
{
    private readonly LayoutServiceHelper _layoutServiceHelper;
    private readonly WeatherService _weatherService;

    public GraphController(LayoutServiceHelper layoutServiceHelper, WeatherService weatherService)
    {
        _layoutServiceHelper = layoutServiceHelper;
        _weatherService = weatherService;
    }
    
    public async Task<IActionResult> Index([FromBody] GraphQLRequest graphqlRequest)
    {
        object result;

        if (graphqlRequest.Query.Contains("rendered"))
        {
            result = await _layoutServiceHelper.FetchGraphQLDataAsync<LayoutQueryResponse>(graphqlRequest, Request.Headers);
            var layoutQueryResponse = result as GraphQLResponse<LayoutQueryResponse>;
            var renderedJson = layoutQueryResponse?.Data?.Layout?.Item?.Rendered.ToString();

            if (!string.IsNullOrWhiteSpace(renderedJson))
            {
                var layoutContent = _layoutServiceHelper.ProcessLayoutContentAsync(renderedJson);
                await ApplyFieldUpdates(layoutContent);

                var serializedContent = JsonSerializer.Serialize(layoutContent, _layoutServiceHelper.CreateSerializerSettings());
                layoutQueryResponse.Data.Layout.Item.Rendered = JsonDocument.Parse(serializedContent).RootElement;
            }
        }
        else
        {
            result = await _layoutServiceHelper.FetchGraphQLDataAsync<object>(graphqlRequest, Request.Headers).ConfigureAwait(false);
        }
        
        var jsonSettings = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(result, jsonSettings);
        
        return Content(json, "application/json");
    }
    
    /// <summary>
    /// Applies the necessary field updates to the layout content.
    /// </summary>
    /// <param name="layoutContent">The deserialized layout content.</param>
    private async Task ApplyFieldUpdates(SitecoreLayoutResponseContent layoutContent)
    {
        var itemId = layoutContent?.Sitecore?.Route?.ItemId;
        
        var componentUpdates = new Dictionary<string, Dictionary<string, (object newValue, FieldType fieldType)>>();
        
        if (itemId != null && itemId.Equals("6f42eb7e-7dda-4ccd-b116-a136d10b0e3d", StringComparison.InvariantCultureIgnoreCase))
        {
            componentUpdates.Add("Hero", new Dictionary<string, (object newValue, FieldType fieldType)>
            {
                { "Text", (" updated text from LayoutService", FieldType.TextField) }
            });
            
            componentUpdates.Add("CTA", new Dictionary<string, (object newValue, FieldType fieldType)>
            {
                { "Link", ("- Links can also be updated", FieldType.HyperLinkField) }
            });
        }

        if (itemId != null && itemId.Equals("bf345f94-f106-4d63-b9c6-d79c1cf0abb5", StringComparison.InvariantCultureIgnoreCase))
        {
            var city = "Amsterdam";
            var weatherInfo = await _weatherService.GetCurrentWeatherAsync(city);
            
            componentUpdates.Add("Agenda", new Dictionary<string, (object newValue, FieldType fieldType)>
            {
                { "Title", ("- Update\n\n" + weatherInfo, FieldType.TextField) }
            });
        }

        foreach (var componentName in componentUpdates.Keys)
        {
            _layoutServiceHelper.UpdateFieldsRecursively(layoutContent?.Sitecore?.Route, componentName, componentUpdates[componentName]);
        }
    }
}
