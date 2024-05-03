using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.LayoutService.Client;
using Sitecore.LayoutService.Client.Newtonsoft;
using Sitecore.LayoutService.Client.Newtonsoft.Model;
using Sitecore.LayoutService.Client.Request;
using Sitecore.LayoutService.Client.Response.Model;
using Sitecore.LayoutService.Client.Response.Model.Fields;

namespace Mvp.Project.MvpSite.Controllers;

public class GraphController : Controller
{
    private readonly ISitecoreLayoutClient _layoutClient;

    public GraphController(ISitecoreLayoutClient layoutClient)
    {
        _layoutClient = layoutClient;
    }
    
    public async Task<IActionResult> Index([FromBody] GraphQLRequest request)
    {
        var sitecoreLayoutRequest = new SitecoreLayoutRequest
        {
            { "sc_site", "mvp-site" },
            { "sc_apikey", "{E2F3D43E-B1FD-495E-B4B1-84579892422A}" },
            { "item", "/About" },
            { "sc_lang", "en" }
        };
        var test = await _layoutClient.Request(sitecoreLayoutRequest);
        
        test.Content.Sitecore.Route.Placeholders.TryGetValue("main", out var mainAbout);
        
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
        
        var json = JsonConvert.SerializeObject(test.Content, jsonSettings);
        return Content(json, "application/json");
    }
    
}