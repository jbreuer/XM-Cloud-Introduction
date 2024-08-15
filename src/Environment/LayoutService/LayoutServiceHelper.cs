using System.Net;
using System.Text.Json;
using System.Web;
using GraphQL;
using GraphQL.Client.Http;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Response;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Response.Model;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Response.Model.Fields;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Serialization;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Serialization.Fields;
using Route = Sitecore.AspNetCore.SDK.LayoutService.Client.Response.Model.Route;

namespace LayoutService;

public class LayoutServiceHelper
{
    private readonly HttpClient _client;
    private readonly ISitecoreLayoutSerializer _serializer;
    private readonly GraphQLHttpClient _graphQLClient;

    public LayoutServiceHelper(IHttpClientFactory httpClientFactory, ISitecoreLayoutSerializer serializer, GraphQLHttpClient graphQLClient)
    {
        _client = httpClientFactory.CreateClient("httpClient");
        _serializer = serializer;
        _graphQLClient = graphQLClient;
    }

    /// <summary>
    /// Fetches the layout data as a string from the specified endpoint.
    /// </summary>
    /// <param name="endpoint">The endpoint to fetch data from.</param>
    /// <param name="incomingQuery">The querystring from the incoming request.</param>
    /// <param name="incomingHeaders">The headers from the incoming request.</param>
    /// <returns>A tuple containing the layout data as a string and the HTTP status code.</returns>
    public async Task<(string, HttpStatusCode)> FetchLayoutDataAsync(string endpoint, IQueryCollection incomingQuery, IHeaderDictionary incomingHeaders)
    {
        var builder = new UriBuilder(_client.BaseAddress)
        {
            Path = endpoint
        };

        // Build the query string from incomingQuery
        var query = HttpUtility.ParseQueryString(string.Empty);
        foreach (var kvp in incomingQuery)
        {
            query[kvp.Key] = kvp.Value.ToString();
        }
        builder.Query = query.ToString();

        var message = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
    
        // List of headers to forward
        var headersToForward = new List<string> { "Authorization", "Cookie", "User-Agent", "Referer" };

        foreach (var header in incomingHeaders)
        {
            if (headersToForward.Contains(header.Key))
            {
                if (!message.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value))
                {
                    message.Content?.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
                }
            }
        }
    
        var response = await _client.SendAsync(message);

        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return (responseContent, response.StatusCode);
    }

    /// <summary>
    /// Fetches the layout data and deserializes it into a SitecoreLayoutResponseContent object.
    /// </summary>
    /// <param name="endpoint">The endpoint to fetch data from.</param>
    /// <param name="request">The Sitecore layout request.</param>
    /// <param name="incomingHeaders">The headers from the incoming request.</param>
    /// <returns>A tuple containing the deserialized SitecoreLayoutResponseContent object (if applicable), the raw response content, and the HTTP status code.</returns>
    public async Task<(SitecoreLayoutResponseContent, string, HttpStatusCode)> FetchLayoutDataContentAsync(string endpoint, IQueryCollection incomingQuery, IHeaderDictionary incomingHeaders)
    {
        var (responseContent, statusCode) = await FetchLayoutDataAsync(endpoint, incomingQuery, incomingHeaders);
        if (statusCode == HttpStatusCode.OK)
        {
            if (responseContent.TrimStart().StartsWith("{\"sitecore\":") && responseContent.Contains("\"route\":"))
            {
                return (_serializer.Deserialize(responseContent), null, statusCode)!;    
            }
            else
            {
                return (null, responseContent, statusCode)!;
            }
        }

        return (null, null, statusCode)!;
    }
    
    /// <summary>
    /// Fetches the layout data using GraphQLHttpClient, forwarding specific headers.
    /// </summary>
    public async Task<GraphQLResponse<T>> FetchGraphQLDataAsync<T>(GraphQLRequest request, IHeaderDictionary incomingHeaders)
    {
        // Forward specific headers
        var headersToForward = new List<string> { "sc_apikey", "Authorization", "Cookie", "User-Agent", "Referer" };

        foreach (var header in incomingHeaders)
        {
            if (headersToForward.Contains(header.Key) && !_graphQLClient.HttpClient.DefaultRequestHeaders.Contains(header.Key))
            {
                _graphQLClient.HttpClient.DefaultRequestHeaders.Add(header.Key, header.Value.ToArray());
            }
        }

        // Send the GraphQL request and return the response
        var result = await _graphQLClient.SendQueryAsync<T>(request, new CancellationToken()).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Updates fields of a component based on the provided updates.
    /// </summary>
    /// <param name="component">The component to update.</param>
    /// <param name="updates">The updates to apply.</param>
    public void UpdateFields(Component component, Dictionary<string, (object newValue, FieldType fieldType)> updates)
    {
        foreach (var update in updates)
        {
            var fieldName = update.Key;
            var (newValue, fieldType) = update.Value;

            if (component.Fields.TryGetValue(fieldName, out var fieldReader))
            {
                object? originalValue = fieldType switch
                {
                    FieldType.TextField => fieldReader.TryRead<TextField>(out var textField) ? textField.Value : null,
                    FieldType.RichTextField => fieldReader.TryRead<RichTextField>(out var richTextField) ? richTextField.Value : null,
                    _ => null
                };

                if (originalValue != null)
                {
                    newValue = fieldType switch
                    {
                        FieldType.TextField => new { value = $"{originalValue} {newValue}" },
                        FieldType.RichTextField => new { value = $"{originalValue} {newValue}" },
                        _ => newValue
                    };
                }
            }
            
            var newFieldReader = new JsonSerializedField(JsonDocument.Parse(newValue.ToString()));

            component.Fields.Remove(fieldName);
            component.Fields[fieldName] = newFieldReader;
        }
    }

    /// <summary>
    /// Recursively updates fields of components within a route.
    /// </summary>
    /// <param name="route">The route containing components.</param>
    /// <param name="componentName">The name of the component to update.</param>
    /// <param name="updates">The updates to apply.</param>
    public void UpdateFieldsRecursively(Route? route, string componentName, Dictionary<string, (object newValue, FieldType fieldType)> updates)
    {
        if (route == null) return;

        foreach (var component in route.Placeholders.Values.SelectMany(placeholder => placeholder.OfType<Component>()))
        {
            UpdateFieldsRecursively(component, componentName, updates);
        }
    }

    /// <summary>
    /// Recursively updates fields of a component and its child components.
    /// </summary>
    /// <param name="component">The component to update.</param>
    /// <param name="componentName">The name of the component to update.</param>
    /// <param name="updates">The updates to apply.</param>
    public void UpdateFieldsRecursively(Component? component, string componentName, Dictionary<string, (object newValue, FieldType fieldType)> updates)
    {
        if (component == null) return;

        if (component.Name == componentName)
        {
            UpdateFields(component, updates);
        }

        foreach (var childComponent in component.Placeholders.Values.SelectMany(placeholder => placeholder.OfType<Component>()))
        {
            UpdateFieldsRecursively(childComponent, componentName, updates);
        }
    }

    public JsonSerializerOptions CreateSerializerSettings()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                // TODO custom converter.
            }
        };
    }
    
    public SitecoreLayoutResponseContent ProcessLayoutContentAsync(string renderedJson)
    {
        var layoutContent = _serializer.Deserialize(renderedJson);
        return layoutContent;
    }
}