using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitecore.LayoutService.Client;
using Sitecore.LayoutService.Client.Exceptions;
using Sitecore.LayoutService.Client.Request;
using Sitecore.LayoutService.Client.Response;

namespace Mvp.Project.MvpSite;

public class HttpLayoutRequestHandler : ILayoutRequestHandler
  {
    private readonly ISitecoreLayoutSerializer _serializer;
    private readonly HttpClient _client;
    private readonly IOptionsSnapshot<HttpLayoutRequestHandlerOptions> _options;
    private readonly ILogger<HttpLayoutRequestHandler> _logger;

    public HttpLayoutRequestHandler(
      HttpClient client,
      ISitecoreLayoutSerializer serializer,
      IOptionsSnapshot<HttpLayoutRequestHandlerOptions> options,
      ILogger<HttpLayoutRequestHandler> logger)
    {
      this._client = Assert.ArgumentNotNull<HttpClient>(client, nameof (client));
      this._serializer = Assert.ArgumentNotNull<ISitecoreLayoutSerializer>(serializer, nameof (serializer));
      this._options = Assert.ArgumentNotNull<IOptionsSnapshot<HttpLayoutRequestHandlerOptions>>(options, nameof (options));
      this._logger = Assert.ArgumentNotNull<ILogger<HttpLayoutRequestHandler>>(logger, nameof (logger));
      Assert.ArgumentNotNull<Uri>(client.BaseAddress, "BaseAddress");
    }

    public async Task<SitecoreLayoutResponse> Request(
      SitecoreLayoutRequest request,
      string handlerName)
    {
      Assert.ArgumentNotNull<SitecoreLayoutRequest>(request, nameof (request));
      HttpResponseMessage httpResponse = (HttpResponseMessage) null;
      SitecoreLayoutResponseContent content = (SitecoreLayoutResponseContent) null;
      ILookup<string, string> metadata = (ILookup<string, string>) null;
      List<SitecoreLayoutServiceClientException> errors = new List<SitecoreLayoutServiceClientException>();
      try
      {
        HttpLayoutRequestHandlerOptions options = this._options.Get(handlerName);
        HttpRequestMessage message;
        try
        {
          message = this.BuildMessage(request, options);
          if (this._logger.IsEnabled(LogLevel.Debug))
          {
            ILogger<HttpLayoutRequestHandler> logger = this._logger;
            // DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 1);
            // interpolatedStringHandler.AppendLiteral("Layout Service Http Request Message : ");
            // interpolatedStringHandler.AppendFormatted<HttpRequestMessage>(message);
            // string stringAndClear = interpolatedStringHandler.ToStringAndClear();
            object[] objArray = Array.Empty<object>();
            // logger.LogDebug(stringAndClear, objArray);
          }
        }
        catch (Exception ex)
        {
          errors = HttpLayoutRequestHandler.AddError(errors, (SitecoreLayoutServiceClientException) new SitecoreLayoutServiceMessageConfigurationException(ex));
          if (this._logger.IsEnabled(LogLevel.Debug))
          {
            // ILogger<HttpLayoutRequestHandler> logger = this._logger;
            // DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(41, 1);
            // interpolatedStringHandler.AppendLiteral("An error configuring the HTTP message  : ");
            // interpolatedStringHandler.AppendFormatted<Exception>(ex);
            // string stringAndClear = interpolatedStringHandler.ToStringAndClear();
            object[] objArray = Array.Empty<object>();
            // logger.LogDebug(stringAndClear, objArray);
          }
          return new SitecoreLayoutResponse(request, errors);
        }
        httpResponse = await this.GetResponseAsync(message).ConfigureAwait(false);
        // DefaultInterpolatedStringHandler interpolatedStringHandler1;
        if (this._logger.IsEnabled(LogLevel.Debug))
        {
          ILogger<HttpLayoutRequestHandler> logger = this._logger;
          // interpolatedStringHandler1 = new DefaultInterpolatedStringHandler(31, 1);
          // interpolatedStringHandler1.AppendLiteral("Layout Service Http Response : ");
          // interpolatedStringHandler1.AppendFormatted<HttpResponseMessage>(httpResponse);
          // string stringAndClear = interpolatedStringHandler1.ToStringAndClear();
          object[] objArray = Array.Empty<object>();
          // logger.LogDebug(stringAndClear, objArray);
        }
        int responseStatusCode = (int) httpResponse.StatusCode;
        if (!httpResponse.IsSuccessStatusCode)
        {
          int num1 = responseStatusCode;
          List<SitecoreLayoutServiceClientException> serviceClientExceptionList;
          if (num1 == 404)
          {
            serviceClientExceptionList = HttpLayoutRequestHandler.AddError(errors, (SitecoreLayoutServiceClientException) new ItemNotFoundSitecoreLayoutServiceClientException(), responseStatusCode);
          }
          else
          {
            int num2 = num1;
            serviceClientExceptionList = num2 < 400 || num2 >= 500 ? (num1 < 500 ? HttpLayoutRequestHandler.AddError(errors, new SitecoreLayoutServiceClientException(), responseStatusCode) : HttpLayoutRequestHandler.AddError(errors, (SitecoreLayoutServiceClientException) new InvalidResponseSitecoreLayoutServiceClientException((Exception) new SitecoreLayoutServiceServerException()), responseStatusCode)) : HttpLayoutRequestHandler.AddError(errors, (SitecoreLayoutServiceClientException) new InvalidRequestSitecoreLayoutServiceClientException(), responseStatusCode);
          }
          errors = serviceClientExceptionList;
        }
        if (httpResponse.IsSuccessStatusCode || httpResponse.StatusCode == HttpStatusCode.NotFound)
        {
          try
          {
            string str = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            content = this._serializer.Deserialize(str);
            if (this._logger.IsEnabled(LogLevel.Debug))
            {
              object obj = JsonSerializer.Deserialize<object>(str);
              ILogger<HttpLayoutRequestHandler> logger = this._logger;
              // interpolatedStringHandler1 = new DefaultInterpolatedStringHandler(31, 1);
              // interpolatedStringHandler1.AppendLiteral("Layout Service Response JSON : ");
              // interpolatedStringHandler1.AppendFormatted<object>(obj);
              // string stringAndClear = interpolatedStringHandler1.ToStringAndClear();
              object[] objArray = Array.Empty<object>();
              // logger.LogDebug(stringAndClear, objArray);
            }
          }
          catch (Exception ex)
          {
            errors = HttpLayoutRequestHandler.AddError(errors, (SitecoreLayoutServiceClientException) new InvalidResponseSitecoreLayoutServiceClientException(ex), responseStatusCode);
          }
        }
        try
        {
          metadata = httpResponse.Headers.SelectMany(x => x.Value.Select(y => new
          {
            Key = x.Key,
            Value = y
          })).ToLookup(k => k.Key, v => v.Value);
        }
        catch (Exception ex)
        {
          errors = HttpLayoutRequestHandler.AddError(errors, (SitecoreLayoutServiceClientException) new InvalidResponseSitecoreLayoutServiceClientException(ex), responseStatusCode);
        }
      }
      catch (Exception ex)
      {
        errors.Add((SitecoreLayoutServiceClientException) new CouldNotContactSitecoreLayoutServiceClientException(ex));
      }
      return new SitecoreLayoutResponse(request, errors)
      {
        Content = content,
        Metadata = metadata
      };
    }

    protected virtual HttpRequestMessage BuildMessage(
      SitecoreLayoutRequest request,
      HttpLayoutRequestHandlerOptions options)
    {
      HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, this._client.BaseAddress);
      if (options != null)
      {
        foreach (Action<SitecoreLayoutRequest, HttpRequestMessage> request1 in options.RequestMap)
          request1(request, httpRequestMessage);
      }
      return httpRequestMessage;
    }

    protected virtual async Task<HttpResponseMessage> GetResponseAsync(HttpRequestMessage message) => await this._client.SendAsync(message).ConfigureAwait(false);

    private static List<SitecoreLayoutServiceClientException> AddError(
      List<SitecoreLayoutServiceClientException> errors,
      SitecoreLayoutServiceClientException error,
      int statusCode = 0)
    {
      if (statusCode > 0)
        error.Data.Add((object) Resources.HttpStatusCode_KeyName, (object) statusCode);
      errors.Add(error);
      return errors;
    }
  }
  
internal static class Assert
{
  [DebuggerStepThrough]
  internal static T ArgumentNotNull<T>(T argument, string name)
  {
    return (object) argument != null ? argument : throw new ArgumentNullException(name);
  }

  [DebuggerStepThrough]
  internal static string ArgumentNotNullOrWhitespace(string argument, string name)
  {
    return !string.IsNullOrWhiteSpace(argument) ? argument : throw new ArgumentNullException(name);
  }

  [DebuggerStepThrough]
  internal static T NotNull<T>(T value, string? message = null)
  {
    return (object) value != null ? value : throw new NullReferenceException(message);
  }
}

internal class Resources
{
  private static ResourceManager resourceMan;

  internal Resources()
  {
  }

  [EditorBrowsable(EditorBrowsableState.Advanced)]
  internal static ResourceManager ResourceManager
  {
    get
    {
      if (resourceMan == null)
        resourceMan = new ResourceManager("Sitecore.LayoutService.Client.Resources", typeof (Resources).Assembly);
      return resourceMan;
    }
  }

  [EditorBrowsable(EditorBrowsableState.Advanced)]
  internal static CultureInfo Culture { get; set; }

  internal static string Exception_AbstractRegistrationsMustProvideFactory => Resources.GetString(nameof (Exception_AbstractRegistrationsMustProvideFactory));

  internal static string Exception_HandlerNameIsNull => Resources.GetString(nameof (Exception_HandlerNameIsNull));

  internal static string Exception_HandlerRegistryKeyNotFound(object handlerName) => string.Format((IFormatProvider) Resources.Culture, Resources.GetString(nameof (Exception_HandlerRegistryKeyNotFound)), handlerName);

  internal static string Exception_RegisterTypesOfService(object serviceType) => string.Format((IFormatProvider) Resources.Culture, Resources.GetString(nameof (Exception_RegisterTypesOfService)), serviceType);

  internal static string HttpStatusCode_KeyName => Resources.GetString(nameof (HttpStatusCode_KeyName));

  private static string GetString(string key) => Resources.ResourceManager.GetString(key, Resources.Culture);
}