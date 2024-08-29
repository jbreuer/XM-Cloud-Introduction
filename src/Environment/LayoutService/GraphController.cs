using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Request.Handlers.GraphQL;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Response;

namespace LayoutService;

public class GraphController : Controller
{
    private readonly LayoutServiceHelper _layoutServiceHelper;
    private readonly WeatherService _weatherService;
    private readonly IMemoryCache _cache;

    public GraphController(LayoutServiceHelper layoutServiceHelper, WeatherService weatherService, IMemoryCache cache)
    {
        _layoutServiceHelper = layoutServiceHelper;
        _weatherService = weatherService;
        _cache = cache;
    }
    
    public async Task<IActionResult> Index([FromBody] GraphQLRequest graphqlRequest)
    {
        object result;

        if (graphqlRequest.Query.Contains("rendered"))
        {
            var cacheKey = GenerateCacheKey(graphqlRequest.Query + graphqlRequest.Variables + graphqlRequest.OperationName);

            if (!_cache.TryGetValue(cacheKey, out object cachedResult))
            {
                cachedResult = await _layoutServiceHelper.FetchGraphQLDataAsync<LayoutQueryResponse>(graphqlRequest, Request.Headers);
                var layoutQueryResponse = cachedResult as GraphQLResponse<LayoutQueryResponse>;
                var renderedJson = layoutQueryResponse?.Data?.Layout?.Item?.Rendered.ToString();

                if (!string.IsNullOrWhiteSpace(renderedJson))
                {
                    var layoutContent = _layoutServiceHelper.ProcessLayoutContentAsync(renderedJson);
                    await ApplyFieldUpdates(layoutContent);

                    var serializedContent = JsonSerializer.Serialize(layoutContent, _layoutServiceHelper.CreateSerializerSettings());
                    layoutQueryResponse.Data.Layout.Item.Rendered = JsonDocument.Parse(serializedContent).RootElement;
                }
                
                // Cache each unique GraphQL query for 60 seconds
                _cache.Set(cacheKey, cachedResult, TimeSpan.FromSeconds(60));
            }
            
            result = cachedResult;
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
    /// Generates a unique cache key based on the GraphQL query string.
    /// </summary>
    private string GenerateCacheKey(string query)
    {
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(query));
            return Convert.ToBase64String(hash);
        }
    }
    
    /// <summary>
    /// Applies the necessary field updates to the layout content.
    /// </summary>
    /// <param name="layoutContent">The deserialized layout content.</param>
    private async Task ApplyFieldUpdates(SitecoreLayoutResponseContent layoutContent)
    {
        var itemId = layoutContent?.Sitecore?.Route?.ItemId;
        
        var componentUpdates = new Dictionary<string, Dictionary<string, (object newValue, FieldType fieldType)>>();
        
        if (itemId != null && itemId.Equals("08762520-fa2d-410a-ba44-f02a0f4b40f9", StringComparison.InvariantCultureIgnoreCase))
        {
            componentUpdates.Add("Hero", new Dictionary<string, (object newValue, FieldType fieldType)>
            {
                { "Headline", (" updated text from LayoutService", FieldType.TextField) }
            });
            
            componentUpdates.Add("ActionBanner", new Dictionary<string, (object newValue, FieldType fieldType)>
            {
                { "CallToAction", ("- Links can also be updated", FieldType.HyperLinkField) }
            });
        }

        if (itemId != null && itemId.Equals("09f61b6b-0ba3-4d94-9c5e-c2e51b7a3951", StringComparison.InvariantCultureIgnoreCase))
        {
            var city = "Amsterdam";
            var weatherInfo = await _weatherService.GetCurrentWeatherAsync(city);
            var date = DateTime.Now.ToString("F", CultureInfo.GetCultureInfo("en-US"));
            
            componentUpdates.Add("Hero", new Dictionary<string, (object newValue, FieldType fieldType)>
            {
                { "Headline", ("- Update\n\n" + weatherInfo + "\n\n" + date, FieldType.TextField) }
            });
        }
        
        if (itemId != null && itemId.Equals("94de9ac3-a9f7-40ab-ae90-acda364b9c40", StringComparison.InvariantCultureIgnoreCase))
        {
            componentUpdates.Add("HeroBig", new Dictionary<string, (object newValue, FieldType fieldType)>
            {
                { "HeroTitle", (" updated from core big", FieldType.TextField) },
                { "HeroDescription", (" With extra text big.", FieldType.RichTextField) }
            });
        }
        
        if (itemId != null && itemId.Equals("0d97b45d-c589-4495-a495-9aaff4fbd2c3", StringComparison.InvariantCultureIgnoreCase))
        {
            componentUpdates.Add("HeroMedium", new Dictionary<string, (object newValue, FieldType fieldType)>
            {
                { "HeroTitle", (" updated from core medium", FieldType.TextField) },
                { "HeroDescription", (" With extra text medium.", FieldType.RichTextField) }
            });
        }

        foreach (var componentName in componentUpdates.Keys)
        {
            _layoutServiceHelper.UpdateFieldsRecursively(layoutContent?.Sitecore?.Route, componentName, componentUpdates[componentName]);
        }
    }
}
