namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;
    using BlazorShop.Storefront.Options;

    using Microsoft.Extensions.Options;


    public partial class StorefrontApiClient
    {
        private async Task<StorefrontApiResult<T>> GetAsyncWithFallback<T>(
            string route,
            string fallbackRoute,
            CancellationToken cancellationToken,
            T? fallbackValue = default,
            TimeSpan? requestTimeout = null)
        {
            var result = await GetAsync<T>(route, cancellationToken, requestTimeout: requestTimeout);
            if (result.IsSuccess)
            {
                return result;
            }

            if (!_enableLegacyFallback)
            {
                return result;
            }

            try
            {
                return await GetAsync(fallbackRoute, cancellationToken, fallbackValue, requestTimeout);
            }
            catch (ObjectDisposedException)
            {
                return result;
            }
        }
        private async Task<StorefrontApiResult<T>> GetMaybeNotFoundWithFallbackAsync<T>(
            string route,
            string fallbackRoute,
            CancellationToken cancellationToken,
            TimeSpan requestTimeout)
        {
            var result = await GetMaybeNotFoundAsync<T>(route, cancellationToken, requestTimeout);
            if (result.IsSuccess)
            {
                return result;
            }

            if (!_enableLegacyFallback)
            {
                return result;
            }

            try
            {
                return await GetMaybeNotFoundAsync<T>(fallbackRoute, cancellationToken, requestTimeout);
            }
            catch (ObjectDisposedException)
            {
                return result;
            }
        }
        private async Task<StorefrontApiResult<T>> GetAsync<T>(string route, CancellationToken cancellationToken, T? fallbackValue = default, TimeSpan? requestTimeout = null)
        {
            using var requestTimeoutToken = CreateRequestTimeoutToken(cancellationToken, requestTimeout ?? CatalogRequestTimeout);

            try
            {
                using var response = await _httpClient.GetAsync(route, requestTimeoutToken.Token);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return fallbackValue is not null
                        ? StorefrontApiResult<T>.Success(fallbackValue)
                        : StorefrontApiResult<T>.NotFound();
                }

                response.EnsureSuccessStatusCode();

                var payload = await ReadPayloadAsync<T>(response, requestTimeoutToken.Token);
                if (payload is not null)
                {
                    return StorefrontApiResult<T>.Success(payload);
                }

                return fallbackValue is not null
                    ? StorefrontApiResult<T>.Success(fallbackValue)
                    : StorefrontApiResult<T>.NotFound();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return StorefrontApiResult<T>.ServiceUnavailable();
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException)
            {
                return StorefrontApiResult<T>.ServiceUnavailable();
            }
        }
        private async Task<StorefrontApiResult<T>> GetMaybeNotFoundAsync<T>(string route, CancellationToken cancellationToken, TimeSpan requestTimeout)
        {
            using var requestTimeoutToken = CreateRequestTimeoutToken(cancellationToken, requestTimeout);

            try
            {
                using var response = await _httpClient.GetAsync(route, requestTimeoutToken.Token);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return StorefrontApiResult<T>.NotFound();
                }

                response.EnsureSuccessStatusCode();

                var payload = await ReadPayloadAsync<T>(response, cancellationToken);
                return payload is not null
                    ? StorefrontApiResult<T>.Success(payload)
                    : StorefrontApiResult<T>.NotFound();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return StorefrontApiResult<T>.ServiceUnavailable();
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException)
            {
                return StorefrontApiResult<T>.ServiceUnavailable();
            }
        }
        private static CancellationTokenSource CreateRequestTimeoutToken(CancellationToken cancellationToken, TimeSpan requestTimeout)
        {
            var requestTimeoutToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestTimeoutToken.CancelAfter(requestTimeout);
            return requestTimeoutToken;
        }
        private static async Task<T?> ReadPayloadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("success", out var successProperty)
                && document.RootElement.TryGetProperty("data", out var dataProperty))
            {
                if (successProperty.ValueKind == JsonValueKind.False)
                {
                    return default;
                }

                return dataProperty.ValueKind == JsonValueKind.Null
                    ? default
                    : dataProperty.Deserialize<T>(JsonOptions);
            }

            return document.RootElement.Deserialize<T>(JsonOptions);
        }
        private async Task<StorefrontSubmitResult<TData>> PostAsync<TRequest, TData>(
            string route,
            TRequest request,
            string unavailableMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync(route, request, JsonOptions, cancellationToken);
                var envelope = await ReadEnvelopeAsync<TData>(response, cancellationToken);
                if (envelope is not null)
                {
                    return envelope.Success
                        ? StorefrontSubmitResult<TData>.Succeeded(envelope.Data, envelope.Message)
                        : StorefrontSubmitResult<TData>.Failed(envelope.Message);
                }

                return response.IsSuccessStatusCode
                    ? StorefrontSubmitResult<TData>.Succeeded(default, "Request completed.")
                    : StorefrontSubmitResult<TData>.Failed(unavailableMessage);
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException or TaskCanceledException)
            {
                return StorefrontSubmitResult<TData>.Failed(unavailableMessage);
            }
        }
        private async Task<StorefrontSubmitResult<TData>> SendCartAsync<TData>(
            HttpMethod method,
            string route,
            string cartToken,
            object? request,
            string unavailableMessage,
            CancellationToken cancellationToken,
            string? bearerToken = null)
        {
            if (string.IsNullOrWhiteSpace(cartToken))
            {
                return StorefrontSubmitResult<TData>.Failed("Cart token is required.");
            }

            try
            {
                using var message = new HttpRequestMessage(method, route);
                message.Headers.TryAddWithoutValidation(CartTokenHeaderName, cartToken);
                if (!string.IsNullOrWhiteSpace(bearerToken))
                {
                    message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
                }

                if (request is not null)
                {
                    message.Content = JsonContent.Create(request, options: JsonOptions);
                }

                using var response = await _httpClient.SendAsync(message, cancellationToken);
                var envelope = await ReadEnvelopeAsync<TData>(response, cancellationToken);
                if (envelope is not null)
                {
                    return envelope.Success
                        ? StorefrontSubmitResult<TData>.Succeeded(envelope.Data, envelope.Message)
                        : StorefrontSubmitResult<TData>.Failed(envelope.Message);
                }

                return response.IsSuccessStatusCode
                    ? StorefrontSubmitResult<TData>.Succeeded(default, "Request completed.")
                    : StorefrontSubmitResult<TData>.Failed(unavailableMessage);
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException or TaskCanceledException)
            {
                return StorefrontSubmitResult<TData>.Failed(unavailableMessage);
            }
        }
        private async Task<StorefrontSubmitResult<TData>> SendAuthorizedAsync<TData>(
            HttpMethod method,
            string route,
            string bearerToken,
            object? request,
            string unavailableMessage,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(bearerToken))
            {
                return StorefrontSubmitResult<TData>.Failed("Customer identity is required.");
            }

            try
            {
                using var message = new HttpRequestMessage(method, route);
                message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

                if (request is not null)
                {
                    message.Content = JsonContent.Create(request, options: JsonOptions);
                }

                using var response = await _httpClient.SendAsync(message, cancellationToken);
                var envelope = await ReadEnvelopeAsync<TData>(response, cancellationToken);
                if (envelope is not null)
                {
                    return envelope.Success
                        ? StorefrontSubmitResult<TData>.Succeeded(envelope.Data, envelope.Message)
                        : StorefrontSubmitResult<TData>.Failed(envelope.Message);
                }

                return response.IsSuccessStatusCode
                    ? StorefrontSubmitResult<TData>.Succeeded(default, "Request completed.")
                    : StorefrontSubmitResult<TData>.Failed(unavailableMessage);
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException or TaskCanceledException)
            {
                return StorefrontSubmitResult<TData>.Failed(unavailableMessage);
            }
        }
        private async Task<StorefrontSubmitResult<TData>> SendConsentAsync<TData>(
            HttpMethod method,
            string route,
            string? visitorKey,
            object? request,
            string unavailableMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                using var message = new HttpRequestMessage(method, route);
                if (!string.IsNullOrWhiteSpace(visitorKey))
                {
                    message.Headers.TryAddWithoutValidation(ConsentVisitorHeaderName, visitorKey);
                }

                if (request is not null)
                {
                    message.Content = JsonContent.Create(request, options: JsonOptions);
                }

                using var response = await _httpClient.SendAsync(message, cancellationToken);
                var envelope = await ReadEnvelopeAsync<TData>(response, cancellationToken);
                if (envelope is not null)
                {
                    return envelope.Success
                        ? StorefrontSubmitResult<TData>.Succeeded(envelope.Data, envelope.Message)
                        : StorefrontSubmitResult<TData>.Failed(envelope.Message);
                }

                return response.IsSuccessStatusCode
                    ? StorefrontSubmitResult<TData>.Succeeded(default, "Request completed.")
                    : StorefrontSubmitResult<TData>.Failed(unavailableMessage);
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException or TaskCanceledException)
            {
                return StorefrontSubmitResult<TData>.Failed(unavailableMessage);
            }
        }
        private static async Task<StorefrontApiEnvelope<TData>?> ReadEnvelopeAsync<TData>(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            return JsonSerializer.Deserialize<StorefrontApiEnvelope<TData>>(payload, JsonOptions);
        }
        private sealed record StorefrontApiEnvelope<TData>(bool Success, string? Message, TData? Data);
    }
}
