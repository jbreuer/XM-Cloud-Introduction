using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Sitecore.LayoutService.Client;
using Sitecore.LayoutService.Client.Newtonsoft;
using Sitecore.LayoutService.Client.Request;

namespace Mvp.Project.MvpSite.Controllers;

public class LayoutController : Controller
{
    private readonly ISitecoreLayoutClient _layoutClient;

    public LayoutController(ISitecoreLayoutClient layoutClient)
    {
        _layoutClient = layoutClient;
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