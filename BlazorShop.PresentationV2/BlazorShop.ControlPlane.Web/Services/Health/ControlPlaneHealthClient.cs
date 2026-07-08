namespace BlazorShop.ControlPlane.Web.Services.Health
{
    using System.Net;

    using BlazorShop.ControlPlane.Web.Services.Common;

    public interface IControlPlaneHealthClient
    {
        Task<HealthListResponse> ListAsync(CancellationToken cancellationToken = default);

        Task<HealthDetail?> GetAsync(Guid nodePublicId, CancellationToken cancellationToken = default);

        Task<ProbeMutationResult> ProbeAsync(Guid nodePublicId, CancellationToken cancellationToken = default);
    }

    public sealed class ControlPlaneHealthClient : IControlPlaneHealthClient
    {
        private readonly IControlPlaneApiClient apiClient;

        public ControlPlaneHealthClient(IControlPlaneApiClient apiClient)
        {
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
