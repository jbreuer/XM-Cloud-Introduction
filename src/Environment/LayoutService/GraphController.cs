using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions.Websocket;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using LayoutService;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.LayoutService.Client;
using Sitecore.LayoutService.Client.Newtonsoft;
using Sitecore.LayoutService.Client.Newtonsoft.Model;
using Sitecore.LayoutService.Client.RequestHandlers.GraphQL;
using Sitecore.LayoutService.Client.Response;
using Sitecore.LayoutService.Client.Response.Model;
using Sitecore.LayoutService.Client.Response.Model.Fields;
using JsonSerializer = System.Text.Json.JsonSerializer;

public class GraphController : Controller
{
    private readonly ISitecoreLayoutSerializer _serializer;
    private readonly LayoutServiceHelper _layoutServiceHelper;

    public GraphController(ISitecoreLayoutSerializer serializer, LayoutServiceHelper layoutServiceHelper)
    {
        this._serializer = serializer;
        this._layoutServiceHelper = layoutServiceHelper;
    }
    
    public async Task<IActionResult> Index([FromBody] GraphQLRequest request)
    {   
        var graphqlRequest = new GraphQLRequest()
        {
            Query = request.Query,
            OperationName = request.OperationName,
            Variables = request.Variables?.ToString()
        };

        object result = null;

        if (request.Query.Contains("rendered"))
        {
            result = await _layoutServiceHelper.FetchGraphQLDataAsync<LayoutQueryResponse>(graphqlRequest, Request.Headers);
            string renderedJson = ((GraphQLResponse<LayoutQueryResponse>)result)?.Data?.Layout?.Item?.Rendered.ToString();

            if (!string.IsNullOrWhiteSpace(renderedJson))
            {
                var jsonObject = JObject.Parse(renderedJson);
                NormalizeFields(jsonObject);

                var layoutContent = _serializer.Deserialize(jsonObject.ToString());
                if (ShouldApplyChanges(layoutContent.Sitecore.Route.ItemId))
                {
                    ApplyFieldUpdates(layoutContent);
                }

                var serializedContent = JsonConvert.SerializeObject(layoutContent, _layoutServiceHelper.CreateSerializerSettings());
                ((GraphQLResponse<LayoutQueryResponse>)result).Data.Layout.Item.Rendered = JsonDocument.Parse(serializedContent).RootElement;
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
    
    void NormalizeFields(JToken token)
    {
        if (token.Type == JTokenType.Object)
        {
            var obj = (JObject)token;
            foreach (var property in obj.Properties())
            {
                if (property.Name == "fields" && property.Value.Type == JTokenType.Array)
                {
                    // Example logic to convert array to object if necessary
                    var fieldsArray = (JArray)property.Value;
                    var fieldsObject = new JObject();
                    int index = 0;
                    foreach (var item in fieldsArray)
                    {
                        fieldsObject[$"item{index++}"] = item;
                    }
                    obj[property.Name] = fieldsObject;
                }
                else if (property.Name == "fields" && property.Value.Type == JTokenType.Object)
                {
                    // Ensure nested fields are normalized
                    NormalizeFields(property.Value);
                }
                else
                {
                    NormalizeFields(property.Value);
                }
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in (JArray)token)
            {
                NormalizeFields(item);
            }
        }
    }
    
    /// <summary>
    /// Determines if changes should be applied based on the item ID.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <returns>True if changes should be applied, otherwise false.</returns>
    private bool ShouldApplyChanges(string itemId)
    {
        var validItemIds = new HashSet<string>
        {
            "6f42eb7e-7dda-4ccd-b116-a136d10b0e3d",
            "bf345f94-f106-4d63-b9c6-d79c1cf0abb5"
        };

        // Check if the item ID is in the set of valid IDs
        return validItemIds.Contains(itemId.ToLower());
    }
    
    /// <summary>
    /// Applies the necessary field updates to the layout content.
    /// </summary>
    /// <param name="layoutContent">The deserialized layout content.</param>
    private void ApplyFieldUpdates(SitecoreLayoutResponseContent layoutContent)
    {
        var componentUpdates = new Dictionary<string, Dictionary<string, (object newValue, FieldType fieldType)>>
        {
            {
                "Hero",
                new Dictionary<string, (object newValue, FieldType fieldType)>
                {
                    { "Text", (" updated text from LayoutService", FieldType.TextField) }
                }
            }
        };

        foreach (var componentName in componentUpdates.Keys)
        {
            _layoutServiceHelper.UpdateFieldsRecursively(layoutContent.Sitecore.Route, componentName, componentUpdates[componentName]);
        }
    }
}