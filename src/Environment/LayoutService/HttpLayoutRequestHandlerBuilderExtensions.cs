using System.Net;
using Sitecore.LayoutService.Client.Request;

namespace LayoutService;

public static class HttpLayoutRequestHandlerBuilderExtensions
{
    public static Uri BuildDefaultSitecoreLayoutRequestUri(
        this SitecoreLayoutRequest request,
        Uri baseUri)
    {
        return request.BuildUri(baseUri, (IEnumerable<string>) _defaultSitecoreRequestKeys);
    }
    
    public static Uri BuildUri(
        this SitecoreLayoutRequest request,
        Uri baseUri,
        IEnumerable<string> queryParameters)
    {
        string[] array = request.Where<KeyValuePair<string, object>>((Func<KeyValuePair<string, object>, bool>) (entry => queryParameters.Contains<string>(entry.Key))).ToList<KeyValuePair<string, object>>().Where<KeyValuePair<string, object>>((Func<KeyValuePair<string, object>, bool>) (entry => entry.Value is string && !string.IsNullOrWhiteSpace(entry.Value.ToString()))).Select<KeyValuePair<string, object>, string>((Func<KeyValuePair<string, object>, string>) (kvp => WebUtility.UrlEncode(kvp.Key) + "=" + WebUtility.UrlEncode(kvp.Value.ToString()))).ToArray<string>();
        if (!((IEnumerable<string>) array).Any<string>())
            return baseUri;
        string str = "?" + string.Join("&", array);
        return new UriBuilder(baseUri) { Query = str }.Uri;
    }
    
    private static readonly List<string> _defaultSitecoreRequestKeys = new List<string>()
    {
        "sc_site",
        "item",
        "placeholderName",
        "hybridLocation",
        "isHybridPlaceholder",
        "hasHybridSsr",
        "sc_lang",
        "sc_apikey",
        "sc_mode",
        "sc_date"
    };
}