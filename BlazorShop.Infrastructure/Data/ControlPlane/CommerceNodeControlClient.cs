namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Diagnostics;
    using System.Net;
    using System.Text.Json;

    using BlazorShop.Application.ControlPlane.Health;

    public sealed class CommerceNodeControlClient : ICommerceNodeControlClient
    {
        private readonly HttpClient httpClient;

        public CommerceNodeControlClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<CommerceNodeControlProbeResponse> ProbeAsync(
            string controlApiBaseUrl,
            string nodeKey,
            string nodeSecret,
            CancellationToken cancellationToken = default)
        {
            if (!Uri.TryCreate(controlApiBaseUrl, UriKind.Absolute, out var baseUri))
            {
                return new CommerceNodeControlProbeResponse(
                    "malformed",
                    null,
                    0,
                    null,
                    "invalid_url",
                    "Control API URL is not an absolute URL.",
                    null,
                    null);
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, AppendPath(baseUri, "api/commerce/healthz"));
                request.Headers.TryAddWithoutValidation("X-Node-Key", nodeKey);
                request.Headers.TryAddWithoutValidation("X-Node-Secret", nodeSecret);

                using var healthResponse = await this.httpClient.SendAsync(request, cancellationToken);
                var healthBody = await healthResponse.Content.ReadAsStringAsync(cancellationToken);

                if (!healthResponse.IsSuccessStatusCode)
                {
                    stopwatch.Stop();
                    return new CommerceNodeControlProbeResponse(
                        "down",
                        (int)healthResponse.StatusCode,
                        (int)stopwatch.ElapsedMilliseconds,
                        null,
                        ResolveHttpErrorCode(healthResponse.StatusCode),
                        ResolveEnvelopeMessage(healthBody) ?? $"Commerce health endpoint returned {(int)healthResponse.StatusCode}.",
                        null,
                        null);
                }

                using var healthJson = JsonDocument.Parse(healthBody);
                if (!TryReadEnvelope(healthJson.RootElement, out var success, out var message, out var data)
                    || data is null)
                {
                    stopwatch.Stop();
                    return new CommerceNodeControlProbeResponse(
                        "malformed",
                        (int)healthResponse.StatusCode,
                        (int)stopwatch.ElapsedMilliseconds,
                        null,
                        "malformed_payload",
                        "Commerce health endpoint returned malformed envelope.",
                        null,
                        null);
                }

                if (!success)
                {
                    stopwatch.Stop();
                    return new CommerceNodeControlProbeResponse(
                        "down",
                        (int)healthResponse.StatusCode,
                        (int)stopwatch.ElapsedMilliseconds,
                        null,
                        "commerce_node_error",
                        string.IsNullOrWhiteSpace(message) ? "Commerce Node reported an unsuccessful health check." : message,
                        null,
                        null);
                }

                var status = ReadString(data.Value, "status") ?? "unknown";
                var dependenciesJson = ReadRawProperty(data.Value, "dependencies");
                stopwatch.Stop();

                return new CommerceNodeControlProbeResponse(
                    NormalizeStatus(status),
                    (int)healthResponse.StatusCode,
                    (int)stopwatch.ElapsedMilliseconds,
                    dependenciesJson,
                    null,
                    null,
                    null,
                    null);
            }
            catch (TaskCanceledException)
            {
                stopwatch.Stop();
                return new CommerceNodeControlProbeResponse(
                    "timeout",
                    null,
                    (int)stopwatch.ElapsedMilliseconds,
                    null,
                    "timeout",
                    "Health probe timed out.",
                    null,
                    null);
            }
            catch (JsonException)
            {
                stopwatch.Stop();
                return new CommerceNodeControlProbeResponse(
                    "malformed",
                    HttpStatusCode: (int)System.Net.HttpStatusCode.OK,
                    (int)stopwatch.ElapsedMilliseconds,
                    null,
                    "malformed_payload",
                    "Health endpoint returned malformed JSON.",
                    null,
                    null);
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                return new CommerceNodeControlProbeResponse(
                    "down",
                    null,
                    (int)stopwatch.ElapsedMilliseconds,
                    null,
                    "request_failed",
                    ex.Message,
                    null,
                    null);
            }
        }

        private static Uri AppendPath(Uri baseUri, string path)
        {
            var value = baseUri.ToString().TrimEnd('/') + "/" + path.TrimStart('/');
            return new Uri(value, UriKind.Absolute);
        }

        private static string NormalizeStatus(string status)
        {
            return status.Trim().ToLowerInvariant() switch
            {
                "healthy" => "healthy",
                "ok" => "healthy",
                "warning" => "warning",
                "degraded" => "warning",
                "unhealthy" => "down",
                "down" => "down",
                _ => "unknown"
            };
        }

        private static string? ReadString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
        }

        private static string? ReadRawProperty(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property) ? property.GetRawText() : null;
        }

        private static bool TryReadEnvelope(
            JsonElement root,
            out bool success,
            out string? message,
            out JsonElement? data)
        {
            success = false;
            message = null;
            data = null;

            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("success", out var successProperty)
                || successProperty.ValueKind is not JsonValueKind.True and not JsonValueKind.False
                || !root.TryGetProperty("message", out var messageProperty)
                || !root.TryGetProperty("data", out var dataProperty))
            {
                return false;
            }

            success = successProperty.GetBoolean();
            message = messageProperty.ValueKind == JsonValueKind.String ? messageProperty.GetString() : null;
            data = dataProperty.ValueKind == JsonValueKind.Null ? null : dataProperty;
            return true;
        }

        private static string ResolveHttpErrorCode(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.Unauthorized => "invalid_credentials",
                HttpStatusCode.Forbidden => "ip_not_allowed",
                _ => "http_status"
            };
        }

        private static string? ResolveEnvelopeMessage(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(payload);
                return ReadString(document.RootElement, "message");
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
