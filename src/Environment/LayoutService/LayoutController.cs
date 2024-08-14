using System.Net;
using LayoutService;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.LayoutService.Client.Response;

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
    /// <returns>The updated layout data in JSON format.</returns>
    public async Task<IActionResult> Item()
    {
        // Fetch the layout data content from Sitecore
        var (content, json, statusCode) = await _layoutServiceHelper.FetchLayoutDataContentAsync("sitecore/api/layout/render/jss", Request.Query, Request.Headers);
        if (statusCode != HttpStatusCode.OK)
        {
            return StatusCode((int)statusCode);
        }
        if (!string.IsNullOrEmpty(json))
        {
            return Content(json, "application/json");
        }
        var context = JsonConvert.DeserializeObject<JObject>(content?.ContextRawData);

        // Check if the changes should be applied based on the item ID
        if (ShouldApplyChanges(content?.Sitecore?.Route?.ItemId))
        {
            content.ContextRawData = JsonConvert.SerializeObject(context);
            ApplyFieldUpdates(content);
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

        var contentJson = JsonConvert.SerializeObject(result, jsonSettings);
        return Content(contentJson, "application/json");
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
    
    /// <summary>
    /// Applies the necessary field updates to the layout content.
    /// </summary>
    /// <param name="layoutContent">The deserialized layout content.</param>
    private void ApplyFieldUpdates(SitecoreLayoutResponseContent layoutContent)
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
            _layoutServiceHelper.UpdateFieldsRecursively(layoutContent.Sitecore.Route, componentName, componentUpdates[componentName].Updates);
        }
    }
}
