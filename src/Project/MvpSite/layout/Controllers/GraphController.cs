using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.LayoutService.Client;
using Sitecore.LayoutService.Client.Exceptions;
using Sitecore.LayoutService.Client.Newtonsoft;
using Sitecore.LayoutService.Client.Newtonsoft.Model;
using Sitecore.LayoutService.Client.Request;
using Sitecore.LayoutService.Client.RequestHandlers.GraphQL;
using Sitecore.LayoutService.Client.Response;
using Sitecore.LayoutService.Client.Response.Model;
using Sitecore.LayoutService.Client.Response.Model.Fields;

namespace Mvp.Project.MvpSite.Controllers;

public class GraphController : Controller
{
    private readonly IGraphQLClient _client;
    private readonly ISitecoreLayoutSerializer _serializer;

    public GraphController(IGraphQLClient client, ISitecoreLayoutSerializer serializer)
    {
        this._client = Assert.ArgumentNotNull<IGraphQLClient>(client, nameof (client));
        this._serializer = Assert.ArgumentNotNull<ISitecoreLayoutSerializer>(serializer, nameof (serializer));
    }
    
    public async Task<IActionResult> Index([FromBody] GraphQLRequest request)
    {
        GraphQLResponse<LayoutQueryResponse> graphQlResponse = await this._client.SendQueryAsync<LayoutQueryResponse>(request, new CancellationToken()).ConfigureAwait(false);
        string str = graphQlResponse?.Data?.Layout?.Item?.Rendered.ToString();
        var content = this._serializer.Deserialize(str);

        var sitecoreLayoutRequest = new SitecoreLayoutRequest();
        List<SitecoreLayoutServiceClientException> errors = new List<SitecoreLayoutServiceClientException>();
        SitecoreLayoutResponse sitecoreLayoutResponse = new SitecoreLayoutResponse(sitecoreLayoutRequest, errors)
        {
            Content = content,
            Metadata = new Dictionary<string, string>().ToLookup<KeyValuePair<string, string>, string, string>((Func<KeyValuePair<string, string>, string>) (k => k.Key), (Func<KeyValuePair<string, string>, string>) (v => v.Value))
        };
        
        sitecoreLayoutResponse.Content.Sitecore.Route.Placeholders.TryGetValue("main", out var mainAbout);
        
        Placeholder mainPlaceholderAbout = mainAbout as Placeholder;
        if (mainPlaceholderAbout != null) 
        {
            foreach (Component component in mainPlaceholderAbout)
            {
                var fields = component.Fields;
                
                // Ensure the component has a HeroSubtitle field
                if (component.Fields.ContainsKey("HeroSubtitle"))
                {   
                    if (component.Fields.TryGetValue("HeroSubtitle", out var fieldReaderHeroSubtitle))
                    {
                        var heroSubtitle = fieldReaderHeroSubtitle.Read<TextField>();
                        if (heroSubtitle != null)
                        {
                            // Create a new JToken with the updated subtitle
                            JToken subtitleToken = JToken.FromObject(new { value = heroSubtitle.Value + " updated text" });
        
                            // Use the existing serializer or create a new one if necessary
                            JsonSerializer serializer = new JsonSerializer();
        
                            // Create a new NewtonsoftFieldReader with the new JToken
                            NewtonsoftFieldReader newFieldReader = new NewtonsoftFieldReader(serializer, subtitleToken);
        
                            // Update the component's Fields dictionary
                            component.Fields["HeroSubtitle"] = newFieldReader;
                        }
                    }
                }
            }
        }
        
        var jsonSettings = new JsonSerializerSettings {
            Formatting = Formatting.Indented,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ContractResolver = CustomDataContractResolver.Instance,
            Converters = new List<JsonConverter> {
                new NewtonsoftFieldReaderJsonConverter()
            }
        };
        
        var json = JsonConvert.SerializeObject(sitecoreLayoutResponse.Content, jsonSettings);
        return Content(json, "application/json");
    }
    
}