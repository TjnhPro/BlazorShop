namespace BlazorShop.ControlPlane.Web.Services.Stores
{
    using System.Net;

    using BlazorShop.ControlPlane.Web.Services.Common;

    public interface IControlPlaneStoreClient
    {
        Task<StoreListResponse> ListAsync(string? search = null, string? status = null, Guid? nodePublicId = null, CancellationToken cancellationToken = default);

        Task<StoreDetail?> GetAsync(Guid publicId, CancellationToken cancellationToken = default);

        Task<StoreMutationResult> CreateAsync(StoreCreateRequest request, CancellationToken cancellationToken = default);

        Task<StoreMutationResult> UpdateAsync(Guid publicId, StoreUpdateRequest request, CancellationToken cancellationToken = default);

        Task<StoreMutationResult> ArchiveAsync(Guid publicId, CancellationToken cancellationToken = default);

        Task<StoreMutationResult> AddDomainAsync(Guid publicId, StoreDomainCreateRequest request, CancellationToken cancellationToken = default);

        Task<StoreMutationResult> VerifyDomainAsync(Guid publicId, long domainId, CancellationToken cancellationToken = default);

        Task<StoreMutationResult> DisableDomainAsync(Guid publicId, long domainId, CancellationToken cancellationToken = default);

        Task<StoreDeploymentResult> DeployAsync(Guid publicId, StoreDeploymentRequest request, CancellationToken cancellationToken = default);

        Task<StoreDeploymentDetailResult> GetDeploymentTaskAsync(Guid publicId, Guid taskPublicId, CancellationToken cancellationToken = default);
    }

    public sealed class ControlPlaneStoreClient : IControlPlaneStoreClient
    {
        private readonly IControlPlaneApiClient apiClient;

        public ControlPlaneStoreClient(IControlPlaneApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public async Task<StoreListResponse> ListAsync(string? search = null, string? status = null, Guid? nodePublicId = null, CancellationToken cancellationToken = default)
        {
            var query = new List<string>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query.Add($"search={Uri.EscapeDataString(search)}");
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query.Add($"status={Uri.EscapeDataString(status)}");
            }

            if (nodePublicId is not null)
            {
                query.Add($"nodePublicId={nodePublicId}");
            }

            var route = query.Count == 0 ? "api/control-plane/stores" : $"api/control-plane/stores?{string.Join("&", query)}";
            var result = await this.apiClient.GetPrivateAsync<StoreListResponse>(
                route,
                "Unable to load stores.",
                cancellationToken);

            if (result.Success)
            {
                return result.Data ?? new StoreListResponse([]);
            }

            throw new InvalidOperationException(result.Message);
        }

        public async Task<StoreDetail?> GetAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.GetPrivateAsync<StoreDetail>(
                $"api/control-plane/stores/{publicId}",
                "Unable to load store detail.",
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

        public async Task<StoreMutationResult> CreateAsync(StoreCreateRequest request, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<StoreCreateRequest, StoreDetail>(
                "api/control-plane/stores",
                request,
                "Unable to create store.",
                cancellationToken);

            return new StoreMutationResult(result.Success, result.Message, result.Data);
        }

        public async Task<StoreMutationResult> UpdateAsync(Guid publicId, StoreUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PutPrivateAsync<StoreUpdateRequest, StoreDetail>(
                $"api/control-plane/stores/{publicId}",
                request,
                "Unable to update store.",
                cancellationToken);

            return new StoreMutationResult(result.Success, result.Message, result.Data);
        }

        public async Task<StoreMutationResult> ArchiveAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<StoreDetail>(
                $"api/control-plane/stores/{publicId}/archive",
                "Unable to archive store.",
                cancellationToken);

            return new StoreMutationResult(result.Success, result.Message, result.Data);
        }

        public async Task<StoreMutationResult> AddDomainAsync(Guid publicId, StoreDomainCreateRequest request, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<StoreDomainCreateRequest, StoreDetail>(
                $"api/control-plane/stores/{publicId}/domains",
                request,
                "Unable to add domain.",
                cancellationToken);

            return new StoreMutationResult(result.Success, result.Message, result.Data);
        }

        public async Task<StoreMutationResult> VerifyDomainAsync(Guid publicId, long domainId, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<StoreDetail>(
                $"api/control-plane/stores/{publicId}/domains/{domainId}/verify",
                "Unable to verify domain.",
                cancellationToken);

            return new StoreMutationResult(result.Success, result.Message, result.Data);
        }

        public async Task<StoreMutationResult> DisableDomainAsync(Guid publicId, long domainId, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<StoreDetail>(
                $"api/control-plane/stores/{publicId}/domains/{domainId}/disable",
                "Unable to disable domain.",
                cancellationToken);

            return new StoreMutationResult(result.Success, result.Message, result.Data);
        }

        public async Task<StoreDeploymentResult> DeployAsync(Guid publicId, StoreDeploymentRequest request, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<StoreDeploymentRequest, StoreDeploymentTaskSummary>(
                $"api/control-plane/stores/{publicId}/deployment-tasks",
                request,
                "Unable to submit deployment task.",
                cancellationToken);

            return new StoreDeploymentResult(result.Success, result.Message, result.Data);
        }

        public async Task<StoreDeploymentDetailResult> GetDeploymentTaskAsync(Guid publicId, Guid taskPublicId, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.GetPrivateAsync<StoreDeploymentTaskDetail>(
                $"api/control-plane/stores/{publicId}/deployment-tasks/{taskPublicId}",
                "Unable to load deployment task.",
                cancellationToken);

            return new StoreDeploymentDetailResult(result.Success, result.Message, result.Data);
        }
    }

    public sealed record StoreListResponse(IReadOnlyList<StoreSummary> Items);

    public sealed record StoreSummary(Guid PublicId, string StoreKey, string Name, string Status, Guid NodePublicId, string NodeKey, string NodeName, string NodeStatus, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? ArchivedAt, int DomainCount);

    public sealed record StoreDetail(Guid PublicId, string StoreKey, string Name, string Status, string? MetadataJson, Guid NodePublicId, string NodeKey, string NodeName, string NodeStatus, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? ArchivedAt, IReadOnlyList<StoreDomain> Domains);

    public sealed record StoreDomain(long Id, string Domain, string NormalizedDomain, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? VerifiedAt, DateTimeOffset? DisabledAt);

    public sealed record StoreCreateRequest(string StoreKey, string Name, Guid NodePublicId, string? MetadataJson);

    public sealed record StoreUpdateRequest(string Name, Guid NodePublicId, string? MetadataJson);

    public sealed record StoreDomainCreateRequest(string Domain);

    public sealed record StoreMutationResult(bool Success, string? Message = null, StoreDetail? Store = null);

    public sealed record StoreDeploymentRequest(
        string StorefrontImage,
        string? PrimaryDomain = null,
        string? BaseUrl = null,
        string DefaultCurrencyCode = "USD",
        string DefaultCulture = "en-US",
        string? NetworkName = null);

    public sealed record StoreDeploymentTaskSummary(
        Guid PublicId,
        string TaskType,
        string Status,
        string? IdempotencyKey,
        string? LockKey,
        string PayloadSchemaVersion,
        string? ErrorCode,
        string? ErrorMessage,
        int AttemptCount,
        int MaxAttempts,
        DateTimeOffset? NextAttemptAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        string? CreatedBy,
        string? CorrelationId,
        DateTimeOffset? CancelRequestedAt,
        string? WorkerId,
        DateTimeOffset? LastHeartbeatAt);

    public sealed record StoreDeploymentTaskDetail(
        Guid PublicId,
        string TaskType,
        string Status,
        string? IdempotencyKey,
        string? LockKey,
        string PayloadSchemaVersion,
        string PayloadJson,
        string? ResultJson,
        string? ErrorCode,
        string? ErrorMessage,
        int AttemptCount,
        int MaxAttempts,
        DateTimeOffset? NextAttemptAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        string? CreatedBy,
        string? CorrelationId,
        DateTimeOffset? CancelRequestedAt,
        string? CancelReason,
        string? WorkerId,
        DateTimeOffset? LastHeartbeatAt,
        IReadOnlyList<StoreDeploymentTaskStep> Steps);

    public sealed record StoreDeploymentTaskStep(
        Guid Id,
        string StepKey,
        string Status,
        int AttemptNumber,
        string? ResultJson,
        string? ErrorCode,
        string? ErrorMessage,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt);

    public sealed record StoreDeploymentResult(bool Success, string? Message = null, StoreDeploymentTaskSummary? Task = null);

    public sealed record StoreDeploymentDetailResult(bool Success, string? Message = null, StoreDeploymentTaskDetail? Task = null);
}
