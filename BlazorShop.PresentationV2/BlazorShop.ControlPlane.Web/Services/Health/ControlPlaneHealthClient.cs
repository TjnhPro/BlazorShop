namespace BlazorShop.ControlPlane.Web.Services.Health
{
    using System.Net;

    using BlazorShop.ControlPlane.Web.Services.Common;

    public interface IControlPlaneHealthClient
    {
        Task<HealthListResponse> ListAsync(
            string? search = null,
            string? status = null,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default);

        Task<HealthDetail?> GetAsync(Guid nodePublicId, CancellationToken cancellationToken = default);

        Task<HealthTimelineResponse> GetTimelineAsync(
            Guid nodePublicId,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default);

        Task<ProbeMutationResult> ProbeAsync(Guid nodePublicId, CancellationToken cancellationToken = default);
    }

    public sealed class ControlPlaneHealthClient : IControlPlaneHealthClient
    {
        private readonly IControlPlaneApiClient apiClient;

        public ControlPlaneHealthClient(IControlPlaneApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public async Task<HealthListResponse> ListAsync(
            string? search = null,
            string? status = null,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            var query = new List<string>
            {
                $"pageNumber={Math.Max(1, pageNumber)}",
                $"pageSize={Math.Clamp(pageSize, 1, 100)}"
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                query.Add($"search={Uri.EscapeDataString(search)}");
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query.Add($"status={Uri.EscapeDataString(status)}");
            }

            var result = await this.apiClient.GetPrivateAsync<HealthListResponse>(
                $"api/control-plane/health/nodes?{string.Join("&", query)}",
                "Unable to load node health.",
                cancellationToken);

            if (result.Success)
            {
                return result.Data ?? new HealthListResponse([], 0, Math.Max(1, pageNumber), Math.Clamp(pageSize, 1, 100), 0);
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

        public async Task<HealthTimelineResponse> GetTimelineAsync(
            Guid nodePublicId,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            var normalizedPageNumber = Math.Max(1, pageNumber);
            var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
            var result = await this.apiClient.GetPrivateAsync<HealthTimelineResponse>(
                $"api/control-plane/health/nodes/{nodePublicId}/timeline?pageNumber={normalizedPageNumber}&pageSize={normalizedPageSize}",
                "Unable to load health timeline.",
                cancellationToken);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return new HealthTimelineResponse([], 0, normalizedPageNumber, normalizedPageSize, 0);
            }

            if (result.Success)
            {
                return result.Data ?? new HealthTimelineResponse([], 0, normalizedPageNumber, normalizedPageSize, 0);
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

    public sealed record HealthListResponse(
        IReadOnlyList<HealthNodeSummary> Items,
        int TotalCount,
        int PageNumber,
        int PageSize,
        int TotalPages);

    public sealed record HealthTimelineResponse(
        IReadOnlyList<HealthSnapshot> Items,
        int TotalCount,
        int PageNumber,
        int PageSize,
        int TotalPages);

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
        HealthSnapshot? LatestHealth,
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
