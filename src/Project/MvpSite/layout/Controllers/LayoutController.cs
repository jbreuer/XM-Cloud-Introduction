using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.LayoutService.Client;
using Sitecore.LayoutService.Client.Newtonsoft;
using Sitecore.LayoutService.Client.Newtonsoft.Model;
using Sitecore.LayoutService.Client.Request;
using Sitecore.LayoutService.Client.Response.Model;
using Sitecore.LayoutService.Client.Response.Model.Fields;

namespace Mvp.Project.MvpSite.Controllers;

public class LayoutController : Controller
{
    private readonly ISitecoreLayoutClient _layoutClient;
    private readonly IOptionsSnapshot<HttpLayoutRequestHandlerOptions> _options;

    public LayoutController(ISitecoreLayoutClient layoutClient, IOptionsSnapshot<HttpLayoutRequestHandlerOptions> options)
    {
        _layoutClient = layoutClient;
        _options = options;
    }
    
    public async Task<IActionResult> Index(string item, string sc_apikey, string sc_site, string sc_lang)
    {
        var sitecoreLayoutRequest = new SitecoreLayoutRequest
        {
            { "sc_site", sc_site },
            { "sc_apikey", sc_apikey },
            { "item", item },
            { "sc_lang", sc_lang }
        };
        var test = await _layoutClient.Request(sitecoreLayoutRequest);
        
        HttpClient client = new HttpClient
        {
            //BaseAddress = new Uri("https://xmcloudcm.localhost/sitecore/api/layout/render/jss")
            BaseAddress = new Uri("https://xmcloudcm.localhost")
        };
        
        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, client.BaseAddress);

        HttpLayoutRequestHandlerOptions options = this._options.Get("default");
        
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