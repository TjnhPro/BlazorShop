namespace BlazorShop.ControlPlane.Web.Services.Common
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Web.Shared.Helper.Contracts;

    public sealed class ControlPlaneApiClient : IControlPlaneApiClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly IHttpClientHelper httpClientHelper;

        public ControlPlaneApiClient(IHttpClientHelper httpClientHelper)
        {
            this.httpClientHelper = httpClientHelper;
        }

        public async Task<ControlPlaneClientResult<TData>> GetPrivateAsync<TData>(
            string route,
            string fallbackMessage,
            CancellationToken cancellationToken = default)
        {
            return await this.SendPrivateAsync<TData>(
                client => client.GetAsync(route, cancellationToken),
                fallbackMessage);
        }

        public async Task<ControlPlaneClientResult<TData>> GetPublicAsync<TData>(
            string route,
            string fallbackMessage,
            CancellationToken cancellationToken = default)
        {
            return await this.SendPublicAsync<TData>(
                client => client.GetAsync(route, cancellationToken),
                fallbackMessage);
        }

        public async Task<ControlPlaneClientResult<TData>> PostPrivateAsync<TRequest, TData>(
            string route,
            TRequest request,
            string fallbackMessage,
            CancellationToken cancellationToken = default)
        {
            return await this.SendPrivateAsync<TData>(
                client => client.PostAsJsonAsync(route, request, SerializerOptions, cancellationToken),
                fallbackMessage);
        }

        public async Task<ControlPlaneClientResult<TData>> PostPrivateAsync<TData>(
            string route,
            string fallbackMessage,
            CancellationToken cancellationToken = default)
        {
            return await this.SendPrivateAsync<TData>(
                client => client.PostAsync(route, content: null, cancellationToken),
                fallbackMessage);
        }

        public async Task<ControlPlaneClientResult<TData>> PostPublicAsync<TRequest, TData>(
            string route,
            TRequest request,
            string fallbackMessage,
            CancellationToken cancellationToken = default)
        {
            return await this.SendPublicAsync<TData>(
                client => client.PostAsJsonAsync(route, request, SerializerOptions, cancellationToken),
                fallbackMessage);
        }

        public async Task<ControlPlaneClientResult<TData>> PostPublicAsync<TData>(
            string route,
            string fallbackMessage,
            CancellationToken cancellationToken = default)
        {
            return await this.SendPublicAsync<TData>(
                client => client.PostAsync(route, content: null, cancellationToken),
                fallbackMessage);
        }

        public async Task<ControlPlaneClientResult<TData>> PutPrivateAsync<TRequest, TData>(
            string route,
            TRequest request,
            string fallbackMessage,
            CancellationToken cancellationToken = default)
        {
            return await this.SendPrivateAsync<TData>(
                client => client.PutAsJsonAsync(route, request, SerializerOptions, cancellationToken),
                fallbackMessage);
        }

        public async Task<ControlPlaneClientResult<TData>> DeletePrivateAsync<TData>(
            string route,
            string fallbackMessage,
            CancellationToken cancellationToken = default)
        {
            return await this.SendPrivateAsync<TData>(
                client => client.DeleteAsync(route, cancellationToken),
                fallbackMessage);
        }

        private async Task<ControlPlaneClientResult<TData>> SendPrivateAsync<TData>(
            Func<HttpClient, Task<HttpResponseMessage>> send,
            string fallbackMessage)
        {
            try
            {
                var client = await this.httpClientHelper.GetPrivateClientAsync();
                using var response = await send(client);
                return await ReadEnvelopeAsync<TData>(response, fallbackMessage);
            }
            catch (HttpRequestException)
            {
                return ControlPlaneClientResult<TData>.Failed("Unable to reach the Control Plane API.");
            }
            catch (OperationCanceledException)
            {
                return ControlPlaneClientResult<TData>.Failed("The Control Plane request timed out.");
            }
        }

        private async Task<ControlPlaneClientResult<TData>> SendPublicAsync<TData>(
            Func<HttpClient, Task<HttpResponseMessage>> send,
            string fallbackMessage)
        {
            try
            {
                var client = this.httpClientHelper.GetPublicClient();
                using var response = await send(client);
                return await ReadEnvelopeAsync<TData>(response, fallbackMessage);
            }
            catch (HttpRequestException)
            {
                return ControlPlaneClientResult<TData>.Failed("Unable to reach the Control Plane API.");
            }
            catch (OperationCanceledException)
            {
                return ControlPlaneClientResult<TData>.Failed("The Control Plane request timed out.");
            }
        }

        private static async Task<ControlPlaneClientResult<TData>> ReadEnvelopeAsync<TData>(
            HttpResponseMessage response,
            string fallbackMessage)
        {
            var payload = response.Content is null
                ? null
                : await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(payload) && TryReadEnvelope(payload, out ControlPlaneApiEnvelope<TData>? envelope))
            {
                var apiEnvelope = envelope!;
                return new ControlPlaneClientResult<TData>(
                    apiEnvelope.Success,
                    ResolveMessage(apiEnvelope.Message, response.StatusCode, fallbackMessage),
                    apiEnvelope.Data,
                    response.StatusCode);
            }

            if (response.IsSuccessStatusCode)
            {
                var data = string.IsNullOrWhiteSpace(payload)
                    ? default
                    : JsonSerializer.Deserialize<TData>(payload, SerializerOptions);

                return ControlPlaneClientResult<TData>.Succeeded(data, string.Empty, response.StatusCode);
            }

            return ControlPlaneClientResult<TData>.Failed(
                ResolveFailureMessage(payload, response.StatusCode, fallbackMessage),
                response.StatusCode);
        }

        private static bool TryReadEnvelope<TData>(string payload, out ControlPlaneApiEnvelope<TData>? envelope)
        {
            envelope = null;

            try
            {
                using var document = JsonDocument.Parse(payload);
                if (document.RootElement.ValueKind != JsonValueKind.Object
                    || !document.RootElement.TryGetProperty("success", out _)
                    || !document.RootElement.TryGetProperty("message", out _)
                    || !document.RootElement.TryGetProperty("data", out _))
                {
                    return false;
                }

                envelope = JsonSerializer.Deserialize<ControlPlaneApiEnvelope<TData>>(payload, SerializerOptions);
                return envelope is not null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static string ResolveFailureMessage(
            string? payload,
            HttpStatusCode statusCode,
            string fallbackMessage)
        {
            var message = TryReadMessage(payload);
            if (!string.IsNullOrWhiteSpace(message))
            {
                return message;
            }

            return statusCode switch
            {
                HttpStatusCode.Unauthorized => "Sign in with a Control Plane account to continue.",
                HttpStatusCode.Forbidden => "Your Control Plane account does not have permission for this action.",
                HttpStatusCode.NotFound => "The requested Control Plane resource was not found.",
                HttpStatusCode.Conflict => "The Control Plane request conflicts with the current state.",
                HttpStatusCode.TooManyRequests => "Too many Control Plane requests. Try again shortly.",
                _ => fallbackMessage
            };
        }

        private static string ResolveMessage(
            string? message,
            HttpStatusCode statusCode,
            string fallbackMessage)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                return message;
            }

            return (int)statusCode >= 400
                ? ResolveFailureMessage(null, statusCode, fallbackMessage)
                : string.Empty;
        }

        private static string? TryReadMessage(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(payload);
                if (TryGetJsonString(document.RootElement, "message", out var message))
                {
                    return message;
                }

                if (TryGetJsonString(document.RootElement, "detail", out var detail))
                {
                    return detail;
                }

                if (TryGetJsonString(document.RootElement, "title", out var title))
                {
                    return title;
                }
            }
            catch (JsonException)
            {
            }

            return null;
        }

        private static bool TryGetJsonString(JsonElement element, string propertyName, out string? value)
        {
            if (element.ValueKind == JsonValueKind.Object
                && element.TryGetProperty(propertyName, out var property)
                && property.ValueKind == JsonValueKind.String)
            {
                value = property.GetString();
                return true;
            }

            value = null;
            return false;
        }
    }
}
