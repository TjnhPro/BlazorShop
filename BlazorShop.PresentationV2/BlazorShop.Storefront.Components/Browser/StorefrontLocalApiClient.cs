using System.Net.Http.Json;
using System.Text.Json;

namespace BlazorShop.Storefront.Components.Browser;

public sealed class StorefrontLocalApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly IStorefrontAntiforgeryTokenReader _antiforgeryTokenReader;

    public StorefrontLocalApiClient(HttpClient httpClient, IStorefrontAntiforgeryTokenReader antiforgeryTokenReader)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _antiforgeryTokenReader = antiforgeryTokenReader ?? throw new ArgumentNullException(nameof(antiforgeryTokenReader));
    }

    public Task<StorefrontLocalApiResult<TResponse>> GetAsync<TResponse>(
        string route,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(HttpMethod.Get, route, body: null, includeAntiforgery: false, cancellationToken);
    }

    public Task<StorefrontLocalApiResult<TResponse>> PostJsonAsync<TRequest, TResponse>(
        string route,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(HttpMethod.Post, route, request, includeAntiforgery: true, cancellationToken);
    }

    public Task<StorefrontLocalApiResult<TResponse>> PutJsonAsync<TRequest, TResponse>(
        string route,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(HttpMethod.Put, route, request, includeAntiforgery: true, cancellationToken);
    }

    public Task<StorefrontLocalApiResult<TResponse>> DeleteAsync<TResponse>(
        string route,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(HttpMethod.Delete, route, body: null, includeAntiforgery: true, cancellationToken);
    }

    private async Task<StorefrontLocalApiResult<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string route,
        object? body,
        bool includeAntiforgery,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, NormalizeLocalRoute(route));
        request.Headers.Accept.ParseAdd("application/json");

        if (includeAntiforgery)
        {
            var token = await _antiforgeryTokenReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (token is not null && !string.IsNullOrWhiteSpace(token.HeaderName) && !string.IsNullOrWhiteSpace(token.Token))
            {
                request.Headers.TryAddWithoutValidation(token.HeaderName, token.Token);
            }
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return StorefrontLocalApiResult<TResponse>.Failed(
                response.StatusCode,
                await ReadErrorMessageAsync(response, cancellationToken).ConfigureAwait(false));
        }

        if (response.Content.Headers.ContentLength == 0)
        {
            return StorefrontLocalApiResult<TResponse>.Succeeded(response.StatusCode, default);
        }

        var data = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
        return StorefrontLocalApiResult<TResponse>.Succeeded(response.StatusCode, data);
    }

    private static string NormalizeLocalRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            throw new ArgumentException("Local API route is required.", nameof(route));
        }

        if (route.StartsWith("//", StringComparison.Ordinal) ||
            route.Contains("://", StringComparison.Ordinal))
        {
            throw new ArgumentException("Storefront WASM local API calls must use same-origin relative routes.", nameof(route));
        }

        return route[0] == '/' ? route : "/" + route;
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content
                .ReadFromJsonAsync<StorefrontLocalApiErrorResponse>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return error.Message;
            }
        }
        catch (JsonException)
        {
        }

        return "Storefront request failed.";
    }
}
