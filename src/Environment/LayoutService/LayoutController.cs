using LayoutService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.LayoutService.Client.Newtonsoft.Model;
using Sitecore.LayoutService.Client.Request;
using Sitecore.LayoutService.Client.Response.Model;

public class LayoutController : Controller
{
    private readonly LayoutServiceHelper _layoutServiceHelper;

    public LayoutController(LayoutServiceHelper layoutServiceHelper)
    {
        _layoutServiceHelper = layoutServiceHelper;
    }

    /// <summary>
    /// Fetches the layout data for a given item and updates fields based on specific criteria.
    /// </summary>
    /// <param name="item">The item identifier.</param>
    /// <param name="sc_apikey">The Sitecore API key.</param>
    /// <param name="sc_site">The Sitecore site.</param>
    /// <param name="sc_lang">The Sitecore language.</param>
    /// <returns>The updated layout data in JSON format.</returns>
    public async Task<IActionResult> Item(string item, string sc_apikey, string sc_site, string sc_lang)
    {
        var sitecoreLayoutRequest = new SitecoreLayoutRequest
        {
            { "sc_site", sc_site },
            { "sc_apikey", sc_apikey },
            { "item", item },
            { "sc_lang", sc_lang }
        };

        // Fetch the layout data content from Sitecore
        var content = await _layoutServiceHelper.FetchLayoutDataContentAsync("sitecore/api/layout/render/jss", sitecoreLayoutRequest);
        var context = JsonConvert.DeserializeObject<JObject>(content?.ContextRawData);

        // Check if the changes should be applied based on the item ID
        if (ShouldApplyChanges(content?.Sitecore?.Route?.ItemId))
        {
            var componentUpdates = new Dictionary<string, ComponentConfig>
            {
                { 
                    "HeroBig", 
                    new ComponentConfig
                    {
                        Updates = new Dictionary<string, (object newValue, FieldType fieldType)>
                        {
                            { "HeroTitle", (" updated from core big", FieldType.TextField) },
                            { "HeroDescription", (" With extra text big.", FieldType.RichTextField) }
                        }
                    }
                },
                { 
                    "HeroMedium", 
                    new ComponentConfig
                    {
                        Updates = new Dictionary<string, (object newValue, FieldType fieldType)>
                        {
                            { "HeroTitle", (" updated from core medium", FieldType.TextField) },
                            { "HeroDescription", (" With extra text medium.", FieldType.RichTextField) }
                        }
                    }
                }
            };

            // Update fields in the components recursively
            foreach (var componentName in componentUpdates.Keys)
            {
                _layoutServiceHelper.UpdateFieldsRecursively(content.Sitecore.Route, componentName, componentUpdates[componentName].Updates);
            }

            content.ContextRawData = JsonConvert.SerializeObject(context);
        }

        var jsonSettings = _layoutServiceHelper.CreateSerializerSettings();

        // Construct the final JSON result
        var result = new
        {
            sitecore = new
            {
                context = context,
                route = content?.Sitecore?.Route
            }
        };

        var json = JsonConvert.SerializeObject(result, jsonSettings);
        return Content(json, "application/json");
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
            "c91b1c4b-c37b-4709-b6b7-3c83053b9f0d",
            "c7dc292c-9faf-473a-a9c6-6a2bc3765e04",
            "94de9ac3-a9f7-40ab-ae90-acda364b9c40",
            "0d97b45d-c589-4495-a495-9aaff4fbd2c3"
        };

        // Check if the item ID is in the set of valid IDs
        return validItemIds.Contains(itemId.ToLower());
    }
}
