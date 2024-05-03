using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions.Websocket;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.AspNetCore.Mvc;
using Sitecore.LayoutService.Client;
using Sitecore.LayoutService.Client.RequestHandlers.GraphQL;

namespace Mvp.Project.MvpSite.Controllers;

public class GraphController : Controller
{   
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
        
        GraphQLResponse<LayoutQueryResponse> graphQlResponse = await client.SendQueryAsync<LayoutQueryResponse>(graphqlRequest, new CancellationToken()).ConfigureAwait(false);
        
        var jsonSettings = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(graphQlResponse, jsonSettings);

        return Content(json, "application/json");
    }
}