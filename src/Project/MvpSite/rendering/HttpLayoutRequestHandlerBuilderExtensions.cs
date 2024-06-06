using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.LayoutService.Client.Extensions;
using Sitecore.LayoutService.Client.Request;

namespace Mvp.Project.MvpSite;

public static class HttpLayoutRequestHandlerBuilderExtensions
{
    public static Sitecore.LayoutService.Client.ILayoutRequestHandlerBuilder<HttpLayoutRequestHandler> AddHttpHandler(
        this Sitecore.LayoutService.Client.ISitecoreLayoutClientBuilder builder,
        string handlerName,
        Func<IServiceProvider, HttpClient> resolveClient)
    {
        Assert.ArgumentNotNull<Sitecore.LayoutService.Client.ISitecoreLayoutClientBuilder>(builder, nameof (builder));
        Assert.ArgumentNotNull<string>(handlerName, nameof (handlerName));
        Assert.ArgumentNotNull<Func<IServiceProvider, HttpClient>>(resolveClient, nameof (resolveClient));
        Sitecore.LayoutService.Client.ILayoutRequestHandlerBuilder<HttpLayoutRequestHandler> httpHandlerBuilder = builder.AddHandler<HttpLayoutRequestHandler>(handlerName, (Func<IServiceProvider, HttpLayoutRequestHandler>) (sp =>
        {
            HttpClient httpClient = resolveClient(sp);
            return ActivatorUtilities.CreateInstance<HttpLayoutRequestHandler>(sp, (object) httpClient);
        }));
        //httpHandlerBuilder.ConfigureRequest(Array.Empty<string>());
        ConfigureRequest(httpHandlerBuilder, Array.Empty<string>());
        return httpHandlerBuilder;
    }
    
    public static Sitecore.LayoutService.Client.ILayoutRequestHandlerBuilder<HttpLayoutRequestHandler> MapFromRequest(
        this Sitecore.LayoutService.Client.ILayoutRequestHandlerBuilder<HttpLayoutRequestHandler> builder,
        Action<SitecoreLayoutRequest, HttpRequestMessage> configureHttpRequestMessage)
    {
        Assert.ArgumentNotNull<Sitecore.LayoutService.Client.ILayoutRequestHandlerBuilder<HttpLayoutRequestHandler>>(builder, nameof (builder));
        Assert.ArgumentNotNull<Action<SitecoreLayoutRequest, HttpRequestMessage>>(configureHttpRequestMessage, nameof (configureHttpRequestMessage));
        builder.Services.Configure<Sitecore.LayoutService.Client.HttpLayoutRequestHandlerOptions>(builder.HandlerName, (Action<Sitecore.LayoutService.Client.HttpLayoutRequestHandlerOptions>) (options => options.RequestMap.Add(configureHttpRequestMessage)));
        return builder;
    }
    
    public static Sitecore.LayoutService.Client.ILayoutRequestHandlerBuilder<HttpLayoutRequestHandler> ConfigureRequest(
        this Sitecore.LayoutService.Client.ILayoutRequestHandlerBuilder<HttpLayoutRequestHandler> httpHandlerBuilder,
        string[] nonValidatedHeaders)
    {
        Assert.ArgumentNotNull<Sitecore.LayoutService.Client.ILayoutRequestHandlerBuilder<HttpLayoutRequestHandler>>(httpHandlerBuilder, nameof (httpHandlerBuilder));
        Assert.ArgumentNotNull<string[]>(nonValidatedHeaders, nameof (nonValidatedHeaders));
        httpHandlerBuilder.MapFromRequest((Action<SitecoreLayoutRequest, HttpRequestMessage>) ((request, message) =>
        {
            message.RequestUri = request.BuildDefaultSitecoreLayoutRequestUri(message.RequestUri);
            string parameter;
            if (request.TryReadValue<string>("sc_auth_header_key", out parameter))
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", parameter);
            Dictionary<string, string[]> headers;
            if (!request.TryGetHeadersCollection(out headers))
                return;
            foreach (KeyValuePair<string, string[]> keyValuePair in headers)
            {
                if (((IEnumerable<string>) nonValidatedHeaders).Contains<string>(keyValuePair.Key))
                    message.Headers.TryAddWithoutValidation(keyValuePair.Key, (IEnumerable<string>) keyValuePair.Value);
                else
                    message.Headers.Add(keyValuePair.Key, (IEnumerable<string>) keyValuePair.Value);
            }
        }));
        return httpHandlerBuilder;
    }
    
    public static Uri BuildDefaultSitecoreLayoutRequestUri(
        this SitecoreLayoutRequest request,
        Uri baseUri)
    {
        Assert.ArgumentNotNull<SitecoreLayoutRequest>(request, nameof (request));
        Assert.ArgumentNotNull<Uri>(baseUri, nameof (baseUri));
        return request.BuildUri(baseUri, (IEnumerable<string>) _defaultSitecoreRequestKeys);
    }
    
    public static Uri BuildUri(
        this SitecoreLayoutRequest request,
        Uri baseUri,
        IEnumerable<string> queryParameters)
    {
        Assert.ArgumentNotNull<SitecoreLayoutRequest>(request, nameof (request));
        Assert.ArgumentNotNull<Uri>(baseUri, nameof (baseUri));
        Assert.ArgumentNotNull<IEnumerable<string>>(queryParameters, nameof (queryParameters));
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
        "sc_lang",
        "sc_apikey",
        "sc_mode",
        "sc_date"
    };
}