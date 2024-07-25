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
                    "HybridPlaceholderExampleSection", 
                    new ComponentConfig
                    {
                        UseSsr = true,
                        Updates = new Dictionary<string, (object newValue, FieldType fieldType)>
                        {
                            { "title", ("", FieldType.TextField) },
                            { "text", (new { value = "<br/>From core." }, FieldType.RichTextField) }
                        }
                    }
                },
                { 
                    "HybridPlaceholderExampleNoSsrSection", 
                    new ComponentConfig
                    {
                        UseSsr = false,
                        Updates = new Dictionary<string, (object newValue, FieldType fieldType)>
                        {
                            { "title", ("", FieldType.TextField) },
                            { "text", (new { value = "<br/>From core no SSR." }, FieldType.RichTextField) }
                        }
                    }
                }
            };

            // Update fields in the components recursively
            foreach (var componentName in componentUpdates.Keys)
            {
                _layoutServiceHelper.UpdateFieldsRecursively(content.Sitecore.Route, componentName, componentUpdates[componentName].Updates);
            }

            // Build hybrid placeholder data and update the context
            var hybridPlaceholderData = new JObject();
            _layoutServiceHelper.BuildHybridPlaceholderData(content?.Sitecore?.Route, hybridPlaceholderData, componentUpdates);
            context["hybridPlaceholderData"] = hybridPlaceholderData;
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
    /// Fetches and updates the placeholder data for a given item.
    /// </summary>
    /// <param name="placeholderName">The placeholder name.</param>
    /// <param name="item">The item identifier.</param>
    /// <param name="sc_apikey">The Sitecore API key.</param>
    /// <param name="sc_site">The Sitecore site.</param>
    /// <param name="sc_lang">The Sitecore language.</param>
    /// <param name="hybridLocation">The hybrid location.</param>
    /// <param name="isHybridPlaceholder">Indicator if it is a hybrid placeholder.</param>
    /// <param name="hasHybridSsr">Indicator if it has hybrid SSR.</param>
    /// <returns>The updated placeholder data in JSON format.</returns>
    public async Task<IActionResult> Placeholder(string placeholderName, string item, string sc_apikey, string sc_site, string sc_lang, string hybridLocation, string isHybridPlaceholder, string hasHybridSsr)
    {
        var sitecoreLayoutRequest = new SitecoreLayoutRequest
        {
            { "placeholderName", placeholderName },
            { "sc_site", sc_site },
            { "sc_apikey", sc_apikey },
            { "item", item },
            { "sc_lang", sc_lang },
            { "hybridLocation", hybridLocation },
            { "isHybridPlaceholder", isHybridPlaceholder },
            { "hasHybridSsr", hasHybridSsr }
        };

        // Fetch the placeholder layout data
        var str = await _layoutServiceHelper.FetchLayoutDataAsync("sitecore/api/layout/placeholder/jss", sitecoreLayoutRequest);
        var jsonObject = JObject.Parse(str);
        var components = jsonObject["elements"]?.ToObject<List<Component>>(JsonSerializer.Create(_layoutServiceHelper.CreateSerializerSettings()));

        // Define updates for specific components
        var componentUpdates = new Dictionary<string, Dictionary<string, (object newValue, FieldType fieldType)>>()
        {
            {
                "HybridPlaceholderExampleSection", new Dictionary<string, (object newValue, FieldType fieldType)>
                {
                    { "date", (_layoutServiceHelper.GetDate(), FieldType.DateField) }
                }
            },
            {
                "HybridPlaceholderExampleNoSsrSection", new Dictionary<string, (object newValue, FieldType fieldType)>
                {
                    { "date", (_layoutServiceHelper.GetDate(), FieldType.DateField) }
                }
            }
        };

        // Update fields in the components
        if (components != null)
        {
            foreach (var component in components.Where(component => componentUpdates.ContainsKey(component.Name)))
            {
                _layoutServiceHelper.UpdateFields(component, componentUpdates[component.Name]);
            }
        }

        // Update the JSON object with the updated components
        if (components != null)
        {
            jsonObject["elements"] = JArray.FromObject(components, JsonSerializer.Create(_layoutServiceHelper.CreateSerializerSettings()));
        }

        var json = JsonConvert.SerializeObject(jsonObject, _layoutServiceHelper.CreateSerializerSettings());
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
            "c7dc292c-9faf-473a-a9c6-6a2bc3765e04"
        };

        // Check if the item ID is in the set of valid IDs
        return validItemIds.Contains(itemId.ToLower());
    }
}
