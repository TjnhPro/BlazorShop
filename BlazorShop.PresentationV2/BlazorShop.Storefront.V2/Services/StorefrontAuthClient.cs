namespace BlazorShop.Storefront.Services
{
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Storefront.Services.Contracts;

    public sealed class StorefrontAuthClient : IStorefrontAuthClient
    {
        private const string RegisterRoute = "internal/auth/create";
        private const string LoginRoute = "internal/auth/login";
        private const string LogoutRoute = "internal/auth/logout";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient httpClient;

        public StorefrontAuthClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public Task<StorefrontAuthResult<LoginResponse>> LoginAsync(LoginUser user, CancellationToken cancellationToken = default)
        {
            return this.PostAsync<LoginUser, LoginResponse>(
                LoginRoute,
                user,
                "Unable to sign in right now.",
                cancellationToken);
        }

        public Task<StorefrontAuthResult<object>> RegisterAsync(CreateUser user, CancellationToken cancellationToken = default)
        {
            return this.PostAsync<CreateUser, object>(
                RegisterRoute,
                user,
                "Unable to create your account right now.",
                cancellationToken);
        }

        public async Task<StorefrontAuthResult<object>> LogoutAsync(
            string? cookieHeader,
            string? userAgent,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, LogoutRoute);
            if (!string.IsNullOrWhiteSpace(cookieHeader))
            {
                request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
            }

            if (!string.IsNullOrWhiteSpace(userAgent))
            {
                request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
            }

            return await this.SendAsync<object>(request, "Unable to sign out right now.", cancellationToken);
        }

        private async Task<StorefrontAuthResult<TData>> PostAsync<TRequest, TData>(
            string route,
            TRequest request,
            string unavailableMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                using var response = await this.httpClient.PostAsJsonAsync(route, request, JsonOptions, cancellationToken);
                return await CreateResultAsync<TData>(response, unavailableMessage, cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException or TaskCanceledException)
            {
                return StorefrontAuthResult<TData>.Failed(unavailableMessage);
            }
        }

        private async Task<StorefrontAuthResult<TData>> SendAsync<TData>(
            HttpRequestMessage request,
            string unavailableMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                using var response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                return await CreateResultAsync<TData>(response, unavailableMessage, cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException or TaskCanceledException)
            {
                return StorefrontAuthResult<TData>.Failed(unavailableMessage);
            }
        }

        private static async Task<StorefrontAuthResult<TData>> CreateResultAsync<TData>(
            HttpResponseMessage response,
            string unavailableMessage,
            CancellationToken cancellationToken)
        {
            var setCookieHeaders = ReadSetCookieHeaders(response);
            var envelope = await ReadEnvelopeAsync<TData>(response, cancellationToken);

            if (envelope is not null)
            {
                return envelope.Success
                    ? StorefrontAuthResult<TData>.Succeeded(envelope.Data, envelope.Message, setCookieHeaders)
                    : StorefrontAuthResult<TData>.Failed(envelope.Message, setCookieHeaders);
            }

            return response.IsSuccessStatusCode
                ? StorefrontAuthResult<TData>.Succeeded(default, "Authentication request completed.", setCookieHeaders)
                : StorefrontAuthResult<TData>.Failed(unavailableMessage, setCookieHeaders);
        }

        private static IReadOnlyList<string> ReadSetCookieHeaders(HttpResponseMessage response)
        {
            return response.Headers.TryGetValues("Set-Cookie", out var values)
                ? values.ToArray()
                : [];
        }

        private static async Task<StorefrontAuthEnvelope<TData>?> ReadEnvelopeAsync<TData>(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            return JsonSerializer.Deserialize<StorefrontAuthEnvelope<TData>>(payload, JsonOptions);
        }

        private sealed record StorefrontAuthEnvelope<TData>(bool Success, string? Message, TData? Data);
    }
}
