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
        GraphQLHttpClient client = new GraphQLHttpClient("https://xmcloudcm.localhost/sitecore/api/graph/edge", (IGraphQLWebsocketJsonSerializer) new SystemTextJsonSerializer());
        client.HttpClient.DefaultRequestHeaders.Add("sc_apikey", "{E2F3D43E-B1FD-495E-B4B1-84579892422A}");
        var graphqlRequest = new GraphQLRequest()
        {
            Query = request.Query,
            OperationName = request.OperationName,
            Variables = request.Variables?.ToString()
        };

        object result = null;

        if (request.Query.Contains("rendered"))
        {
            result = await client.SendQueryAsync<LayoutQueryResponse>(graphqlRequest, new CancellationToken()).ConfigureAwait(false);
            string renderedJson = ((GraphQLResponse<LayoutQueryResponse>)result)?.Data?.Layout?.Item?.Rendered.ToString();

            if (!string.IsNullOrWhiteSpace(renderedJson))
            {
                var jsonObject = JObject.Parse(renderedJson);
                NormalizeFields(jsonObject);

                var layoutContent = _serializer.Deserialize(jsonObject.ToString());
                ApplyFieldUpdates(layoutContent);

                var serializedContent = JsonConvert.SerializeObject(layoutContent, _layoutServiceHelper.CreateSerializerSettings());
                ((GraphQLResponse<LayoutQueryResponse>)result).Data.Layout.Item.Rendered = JsonDocument.Parse(serializedContent).RootElement;
            }
        }
        else
        {
            result = await client.SendQueryAsync<object>(graphqlRequest, new CancellationToken()).ConfigureAwait(false);    
        }
        
        
        var jsonSettings = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        var json = JsonSerializer.Serialize(result, jsonSettings);
        
        
        // var content = result.Data.ToString();
        // if (!string.IsNullOrEmpty(content))
        // {
        //     var response = JsonSerializer.Deserialize<LayoutQueryResponse>(content);    
        // }
        
        
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