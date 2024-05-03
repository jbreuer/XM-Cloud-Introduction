using System;
using GraphQL.Client.Abstractions.Websocket;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.LayoutService.Client;
using Sitecore.LayoutService.Client.Extensions;
using Sitecore.LayoutService.Client.Request;

namespace Mvp.Project.MvpSite;

public static class SitecoreLayoutClientBuilderExtensions
{
    public static ILayoutRequestHandlerBuilder<GraphQlLayoutServiceHandler> AddGraphQlHandler(
        this ISitecoreLayoutClientBuilder builder,
        string name,
        string siteName,
        string apiKey,
        Uri uri)
    {
        Assert.ArgumentNotNullOrWhitespace(name, nameof (name));
        Assert.ArgumentNotNullOrWhitespace(siteName, nameof (siteName));
        Assert.ArgumentNotNullOrWhitespace(apiKey, nameof (apiKey));
        Assert.ArgumentNotNull<Uri>(uri, nameof (uri));
        GraphQLHttpClient client = new GraphQLHttpClient(uri, (IGraphQLWebsocketJsonSerializer) new SystemTextJsonSerializer());
        client.HttpClient.DefaultRequestHeaders.Add("sc_apikey", apiKey);
        builder.WithDefaultRequestOptions((Action<SitecoreLayoutRequest>) (request => request.SiteName(siteName).ApiKey(apiKey)));
        return builder.AddHandler<GraphQlLayoutServiceHandler>(name, (Func<IServiceProvider, GraphQlLayoutServiceHandler>) (sp => ActivatorUtilities.CreateInstance<GraphQlLayoutServiceHandler>(sp, (object) client)));
    }
}