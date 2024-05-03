using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Sitecore.LayoutService.Client;
using Sitecore.LayoutService.Client.Exceptions;
using Sitecore.LayoutService.Client.Request;
using Sitecore.LayoutService.Client.RequestHandlers.GraphQL;
using Sitecore.LayoutService.Client.Response;

namespace Mvp.Project.MvpSite;

public class GraphQlLayoutServiceHandler : ILayoutRequestHandler, IDisposable
  {
    private readonly ISitecoreLayoutSerializer _serializer;
    private readonly ILogger<GraphQlLayoutServiceHandler> _logger;
    private readonly IGraphQLClient _client;

    public GraphQlLayoutServiceHandler(
      IGraphQLClient client,
      ISitecoreLayoutSerializer serializer,
      ILogger<GraphQlLayoutServiceHandler> logger)
    {
      this._logger = Assert.ArgumentNotNull<ILogger<GraphQlLayoutServiceHandler>>(logger, nameof (logger));
      this._client = Assert.ArgumentNotNull<IGraphQLClient>(client, nameof (client));
      this._serializer = Assert.ArgumentNotNull<ISitecoreLayoutSerializer>(serializer, nameof (serializer));
    }

    public async Task<SitecoreLayoutResponse> Request(
      SitecoreLayoutRequest request,
      string handlerName)
    {
      List<SitecoreLayoutServiceClientException> errors = new List<SitecoreLayoutServiceClientException>();
      SitecoreLayoutResponseContent content = (SitecoreLayoutResponseContent) null;
      GraphQLResponse<LayoutQueryResponse> graphQlResponse = await this._client.SendQueryAsync<LayoutQueryResponse>(new GraphQLRequest()
      {
        Query = "\r\n                query LayoutQuery($path: String!, $language: String!, $site: String!) {\r\n                    layout(routePath: $path, language: $language, site: $site) {\r\n                        item {\r\n                            rendered\r\n                        }\r\n                    }\r\n                }",
        OperationName = "LayoutQuery",
        Variables = (object) new
        {
          path = request.Path(),
          language = request.Language(),
          site = request.SiteName()
        }
      }, new CancellationToken()).ConfigureAwait(false);
      
      // DefaultInterpolatedStringHandler interpolatedStringHandler;
      
      if (this._logger.IsEnabled(LogLevel.Debug))
      {
        ILogger<GraphQlLayoutServiceHandler> logger = this._logger;
        // interpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 1);
        // interpolatedStringHandler.AppendLiteral("Layout Service GraphQL Response : ");
        // interpolatedStringHandler.AppendFormatted<LayoutModel>(graphQlResponse.Data.Layout);
        // string stringAndClear = interpolatedStringHandler.ToStringAndClear();
        object[] objArray = Array.Empty<object>();
        // logger.LogDebug(stringAndClear, objArray);
      }
      string str = graphQlResponse?.Data?.Layout?.Item?.Rendered.ToString();
      if (str == null)
      {
        errors.Add((SitecoreLayoutServiceClientException) new ItemNotFoundSitecoreLayoutServiceClientException());
      }
      else
      {
        content = this._serializer.Deserialize(str);
        if (this._logger.IsEnabled(LogLevel.Debug))
        {
          object obj = JsonSerializer.Deserialize<object>(str);
          ILogger<GraphQlLayoutServiceHandler> logger = this._logger;
          // interpolatedStringHandler = new DefaultInterpolatedStringHandler(31, 1);
          // interpolatedStringHandler.AppendLiteral("Layout Service Response JSON : ");
          // interpolatedStringHandler.AppendFormatted<object>(obj);
          // string stringAndClear = interpolatedStringHandler.ToStringAndClear();
          object[] objArray = Array.Empty<object>();
          // logger.LogDebug(stringAndClear, objArray);
        }
      }
      if (graphQlResponse?.Errors != null)
        errors.AddRange(((IEnumerable<GraphQLError>) graphQlResponse.Errors).Select<GraphQLError, SitecoreLayoutServiceClientException>((Func<GraphQLError, SitecoreLayoutServiceClientException>) (e => new SitecoreLayoutServiceClientException((Exception) new LayoutServiceGraphQlException(e)))));
      SitecoreLayoutResponse sitecoreLayoutResponse = new SitecoreLayoutResponse(request, errors)
      {
        Content = content,
        Metadata = new Dictionary<string, string>().ToLookup<KeyValuePair<string, string>, string, string>((Func<KeyValuePair<string, string>, string>) (k => k.Key), (Func<KeyValuePair<string, string>, string>) (v => v.Value))
      };
      errors = (List<SitecoreLayoutServiceClientException>) null;
      content = (SitecoreLayoutResponseContent) null;
      return sitecoreLayoutResponse;
    }

    public void Dispose() => ((IDisposable) this._client).Dispose();
  }