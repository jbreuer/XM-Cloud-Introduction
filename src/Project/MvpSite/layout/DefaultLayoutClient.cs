using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitecore.LayoutService.Client;
using Sitecore.LayoutService.Client.Request;
using Sitecore.LayoutService.Client.Response;

namespace Mvp.Project.MvpSite;

public class DefaultLayoutClient : ISitecoreLayoutClient, ILayoutRequestHandler
  {
    private readonly IServiceProvider _services;
    private readonly IOptions<SitecoreLayoutClientOptions> _layoutClientOptions;
    private readonly IOptionsSnapshot<SitecoreLayoutRequestOptions> _layoutRequestOptions;
    private readonly ILogger<DefaultLayoutClient> _logger;

    public DefaultLayoutClient(
      IServiceProvider services,
      IOptions<SitecoreLayoutClientOptions> layoutClientOptions,
      IOptionsSnapshot<SitecoreLayoutRequestOptions> layoutRequestOptions,
      ILogger<DefaultLayoutClient> logger)
    {
      this._services = Assert.ArgumentNotNull<IServiceProvider>(services, nameof (services));
      this._layoutClientOptions = Assert.ArgumentNotNull<IOptions<SitecoreLayoutClientOptions>>(layoutClientOptions, nameof (layoutClientOptions));
      this._layoutRequestOptions = Assert.ArgumentNotNull<IOptionsSnapshot<SitecoreLayoutRequestOptions>>(layoutRequestOptions, nameof (layoutRequestOptions));
      this._logger = Assert.ArgumentNotNull<ILogger<DefaultLayoutClient>>(logger, nameof (logger));
    }

    public async Task<SitecoreLayoutResponse> Request(SitecoreLayoutRequest request)
    {
      Assert.ArgumentNotNull<SitecoreLayoutRequest>(request, nameof (request));
      return await this.Request(request, string.Empty).ConfigureAwait(false);
    }

    public async Task<SitecoreLayoutResponse> Request(
      SitecoreLayoutRequest request,
      string handlerName)
    {
      Assert.ArgumentNotNull<SitecoreLayoutRequest>(request, nameof (request));
      string str = !string.IsNullOrWhiteSpace(handlerName) ? handlerName : this._layoutClientOptions.Value.DefaultHandler;
      if (string.IsNullOrWhiteSpace(str))
        throw new ArgumentNullException(str, Resources.Exception_HandlerNameIsNull);
      if (!this._layoutClientOptions.Value.HandlerRegistry.ContainsKey(str))
      {
        // ISSUE: reference to a compiler-generated method
        throw new KeyNotFoundException(Resources.Exception_HandlerRegistryKeyNotFound((object) str));
      }
      SitecoreLayoutRequest request1 = request.UpdateRequest((Dictionary<string, object>) this.MergeLayoutRequestOptions(str).RequestDefaults);
      Func<IServiceProvider, ILayoutRequestHandler> func = this._layoutClientOptions.Value.HandlerRegistry[str];
      if (this._logger.IsEnabled(LogLevel.Trace))
        this._logger.LogTrace("Sitecore Layout Request " + JsonSerializer.Serialize<SitecoreLayoutRequest>(request1));
      IServiceProvider services = this._services;
      return await func(services).Request(request1, str).ConfigureAwait(false);
    }

    private SitecoreLayoutRequestOptions MergeLayoutRequestOptions(string handlerName)
    {
      SitecoreLayoutRequestOptions layoutRequestOptions1 = this._layoutRequestOptions.Value;
      SitecoreLayoutRequestOptions layoutRequestOptions2 = this._layoutRequestOptions.Get(handlerName);
      if (DefaultLayoutClient.AreEqual((IDictionary<string, object>) layoutRequestOptions1.RequestDefaults, (IDictionary<string, object>) layoutRequestOptions2.RequestDefaults))
        return layoutRequestOptions1;
      SitecoreLayoutRequestOptions layoutRequestOptions3 = layoutRequestOptions1;
      SitecoreLayoutRequest requestDefaults1 = layoutRequestOptions1.RequestDefaults;
      SitecoreLayoutRequest requestDefaults2 = layoutRequestOptions2.RequestDefaults;
      foreach (KeyValuePair<string, object> keyValuePair in (Dictionary<string, object>) requestDefaults2)
      {
        if (requestDefaults1.ContainsKey(keyValuePair.Key))
          requestDefaults1[keyValuePair.Key] = requestDefaults2[keyValuePair.Key];
        else
          requestDefaults1.Add(keyValuePair.Key, requestDefaults2[keyValuePair.Key]);
      }
      layoutRequestOptions3.RequestDefaults = requestDefaults1;
      return layoutRequestOptions3;
    }

    private static bool AreEqual(
      IDictionary<string, object?> dictionary1,
      IDictionary<string, object?> dictionary2)
    {
      if (dictionary1.Count != dictionary2.Count)
        return false;
      foreach (string key in (IEnumerable<string>) dictionary1.Keys)
      {
        object obj;
        if (!dictionary2.TryGetValue(key, out obj) || dictionary1[key] != obj)
          return false;
      }
      return true;
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