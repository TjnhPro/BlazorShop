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
                using var healthResponse = await this.httpClient.GetAsync(AppendPath(baseUri, "health"), cancellationToken);
                var healthBody = await healthResponse.Content.ReadAsStringAsync(cancellationToken);

                if (!healthResponse.IsSuccessStatusCode)
                {
                    stopwatch.Stop();
                    return new CommerceNodeControlProbeResponse(
                        "down",
                        (int)healthResponse.StatusCode,
                        (int)stopwatch.ElapsedMilliseconds,
                        null,
                        "http_status",
                        $"Health endpoint returned {(int)healthResponse.StatusCode}.",
                        null,
                        null);
                }

                using var healthJson = JsonDocument.Parse(healthBody);
                var status = ReadString(healthJson.RootElement, "status") ?? "unknown";
                var dependenciesJson = ReadRawProperty(healthJson.RootElement, "dependencies");
                var capabilityResponse = await this.ReadCapabilitiesAsync(baseUri, cancellationToken);
                stopwatch.Stop();

                return capabilityResponse with
                {
                    HealthStatus = NormalizeStatus(status),
                    HttpStatusCode = (int)healthResponse.StatusCode,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    DependencyStatusJson = dependenciesJson
                };
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

        private async Task<CommerceNodeControlProbeResponse> ReadCapabilitiesAsync(
            Uri baseUri,
            CancellationToken cancellationToken)
        {
            using var response = await this.httpClient.GetAsync(AppendPath(baseUri, "capabilities"), cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new CommerceNodeControlProbeResponse(
                    "warning",
                    (int)response.StatusCode,
                    0,
                    null,
                    "capabilities_http_status",
                    $"Capabilities endpoint returned {(int)response.StatusCode}.",
                    null,
                    null);
            }

            using var document = JsonDocument.Parse(body);
            var schemaVersion = ReadString(document.RootElement, "schemaVersion")
                                ?? ReadString(document.RootElement, "schema_version")
                                ?? "unknown";

            return new CommerceNodeControlProbeResponse(
                "healthy",
                (int)response.StatusCode,
                0,
                null,
                null,
                null,
                schemaVersion,
                body);
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
    }
}
