using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions.Websocket;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Sitecore.LayoutService.Client;
using Sitecore.LayoutService.Client.Newtonsoft;
using Sitecore.LayoutService.Client.Newtonsoft.Model;
using Sitecore.LayoutService.Client.RequestHandlers.GraphQL;
using Sitecore.LayoutService.Client.Response.Model;
using Sitecore.LayoutService.Client.Response.Model.Fields;

namespace Mvp.Project.MvpSite.Controllers;

public class GraphController : Controller
{
    private readonly ISitecoreLayoutSerializer _serializer;

    public GraphController(ISitecoreLayoutSerializer serializer)
    {
        this._serializer = Assert.ArgumentNotNull<ISitecoreLayoutSerializer>(serializer, nameof (serializer));
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
}