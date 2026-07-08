namespace BlazorShop.ControlPlane.Web.Services.Health
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.ControlPlane.Web.Services.Common;
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
        private readonly IControlPlaneApiClient apiClient;

        public ControlPlaneHealthClient(IHttpClientHelper httpClientHelper, IControlPlaneApiClient apiClient)
        {
            this.httpClientHelper = httpClientHelper;
            this.apiClient = apiClient;
        }

        public async Task<HealthListResponse> ListAsync(CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.GetPrivateAsync<HealthListResponse>(
                "api/control-plane/health/nodes",
                "Unable to load node health.",
                cancellationToken);

            if (result.Success)
            {
                return result.Data ?? new HealthListResponse([]);
            }

            throw new InvalidOperationException(result.Message);
        }

        public async Task<HealthDetail?> GetAsync(Guid nodePublicId, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.GetPrivateAsync<HealthDetail>(
                $"api/control-plane/health/nodes/{nodePublicId}",
                "Unable to load health detail.",
                cancellationToken);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (result.Success)
            {
                return result.Data;
            }

            throw new InvalidOperationException(result.Message);
        }

        public async Task<ProbeMutationResult> ProbeAsync(Guid nodePublicId, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<ProbeResult>(
                $"api/control-plane/health/nodes/{nodePublicId}/probe",
                "Unable to run probe.",
                cancellationToken);

            return new ProbeMutationResult(result.Success, result.Message, result.Data);
        }

        private static async Task<string> ResolveErrorMessageAsync(HttpResponseMessage response, string defaultMessage)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return "Sign in with a Control Plane account that can inspect node health.";
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return "Your Control Plane account does not have permission for this action.";
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
