namespace BlazorShop.ControlPlane.Web.Services.Health
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Web.Shared.Helper.Contracts;

    public interface IControlPlaneHealthClient
    {
        Task<HealthListResponse> ListAsync(CancellationToken cancellationToken = default);

        Task<HealthDetail?> GetAsync(Guid nodePublicId, CancellationToken cancellationToken = default);

        Task<ProbeMutationResult> ProbeAsync(Guid nodePublicId, CancellationToken cancellationToken = default);
    }

    public sealed class ControlPlaneHealthClient : IControlPlaneHealthClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly IHttpClientHelper httpClientHelper;

        public ControlPlaneHealthClient(IHttpClientHelper httpClientHelper)
        {
            this.httpClientHelper = httpClientHelper;
        }

        public async Task<HealthListResponse> ListAsync(CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.GetAsync("api/control-plane/health/nodes", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<HealthListResponse>(SerializerOptions, cancellationToken)
                       ?? new HealthListResponse([]);
            }

            throw new InvalidOperationException(await ResolveErrorMessageAsync(response, "Unable to load node health."));
        }

        public async Task<HealthDetail?> GetAsync(Guid nodePublicId, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.GetAsync($"api/control-plane/health/nodes/{nodePublicId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<HealthDetail>(SerializerOptions, cancellationToken);
            }

            throw new InvalidOperationException(await ResolveErrorMessageAsync(response, "Unable to load health detail."));
        }

        public async Task<ProbeMutationResult> ProbeAsync(Guid nodePublicId, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsync($"api/control-plane/health/nodes/{nodePublicId}/probe", content: null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ProbeResult>(SerializerOptions, cancellationToken);
                return new ProbeMutationResult(true, Probe: result);
            }

            return new ProbeMutationResult(false, await ResolveErrorMessageAsync(response, "Unable to run probe."));
        }

        private static async Task<string> ResolveErrorMessageAsync(HttpResponseMessage response, string defaultMessage)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return "Sign in with a Control Plane account that can inspect node health.";
            }

            if (response.Content is null)
            {
                return defaultMessage;
            }

            try
            {
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (document.RootElement.TryGetProperty("message", out var messageElement)
                    && messageElement.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(messageElement.GetString()))
                {
                    return messageElement.GetString()!;
                }
            }
            catch (JsonException)
            {
            }

            return defaultMessage;
        }
    }

    public sealed record HealthListResponse(IReadOnlyList<HealthNodeSummary> Items);

    public sealed record HealthNodeSummary(
        Guid PublicId,
        string NodeKey,
        string Name,
        string Status,
        DateTimeOffset? LastSeenAt,
        HealthSnapshot? LatestHealth,
        CapabilitySnapshot? CurrentCapability);

    public sealed record HealthDetail(
        Guid PublicId,
        string NodeKey,
        string Name,
        string Status,
        DateTimeOffset? LastSeenAt,
        IReadOnlyList<HealthSnapshot> RecentHealth,
        CapabilitySnapshot? CurrentCapability);

    public sealed record HealthSnapshot(
        Guid PublicId,
        string Status,
        int? HttpStatusCode,
        int DurationMs,
        string? DependencyStatusJson,
        string? ErrorCode,
        string? ErrorMessage,
        DateTimeOffset CheckedAt);

    public sealed record CapabilitySnapshot(
        Guid PublicId,
        string SchemaVersion,
        string Checksum,
        string CapabilitiesJson,
        bool IsCurrent,
        DateTimeOffset CapturedAt);

    public sealed record ProbeResult(HealthSnapshot Health, CapabilitySnapshot? Capability, bool CapabilityChanged);

    public sealed record ProbeMutationResult(bool Success, string? Message = null, ProbeResult? Probe = null);
}
