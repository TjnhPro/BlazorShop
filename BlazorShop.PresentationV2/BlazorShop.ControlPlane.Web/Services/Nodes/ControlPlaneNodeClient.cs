namespace BlazorShop.ControlPlane.Web.Services.Nodes
{
    using System.Net;

    using BlazorShop.ControlPlane.Web.Services.Common;

    public interface IControlPlaneNodeClient
    {
        Task<NodeListResponse> ListAsync(string? search, string? status, string? cursor, CancellationToken cancellationToken = default);

        Task<NodeDetail?> GetAsync(Guid publicId, CancellationToken cancellationToken = default);

        Task<NodeMutationResult> CreateAsync(NodeCreateRequest request, CancellationToken cancellationToken = default);

        Task<NodeMutationResult> UpdateAsync(Guid publicId, NodeUpdateRequest request, CancellationToken cancellationToken = default);

        Task<NodeMutationResult> DisableAsync(Guid publicId, CancellationToken cancellationToken = default);
    }

    public sealed class ControlPlaneNodeClient : IControlPlaneNodeClient
    {
        private readonly IControlPlaneApiClient apiClient;

        public ControlPlaneNodeClient(IControlPlaneApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public async Task<NodeListResponse> ListAsync(string? search, string? status, string? cursor, CancellationToken cancellationToken = default)
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

            if (!string.IsNullOrWhiteSpace(cursor))
            {
                query.Add($"cursor={Uri.EscapeDataString(cursor)}");
            }

            query.Add("limit=25");
            var route = $"api/control-plane/nodes?{string.Join("&", query)}";
            var result = await this.apiClient.GetPrivateAsync<NodeListResponse>(
                route,
                "Unable to load nodes.",
                cancellationToken);

            if (result.Success)
            {
                return result.Data ?? new NodeListResponse([], null);
            }

            throw new InvalidOperationException(result.Message);
        }

        public async Task<NodeDetail?> GetAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.GetPrivateAsync<NodeDetail>(
                $"api/control-plane/nodes/{publicId}",
                "Unable to load node details.",
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

        public async Task<NodeMutationResult> CreateAsync(NodeCreateRequest request, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<NodeCreateRequest, NodeDetail>(
                "api/control-plane/nodes",
                request,
                "Unable to create node.",
                cancellationToken);

            return new NodeMutationResult(result.Success, result.Message, result.Data);
        }

        public async Task<NodeMutationResult> UpdateAsync(Guid publicId, NodeUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PutPrivateAsync<NodeUpdateRequest, NodeDetail>(
                $"api/control-plane/nodes/{publicId}",
                request,
                "Unable to update node.",
                cancellationToken);

            return new NodeMutationResult(result.Success, result.Message, result.Data);
        }

        public async Task<NodeMutationResult> DisableAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<NodeDetail>(
                $"api/control-plane/nodes/{publicId}/disable",
                "Unable to disable node.",
                cancellationToken);

            return new NodeMutationResult(result.Success, result.Message, result.Data);
        }
    }

    public sealed record NodeListResponse(IReadOnlyList<NodeSummary> Items, string? NextCursor);

    public sealed record NodeSummary(
        Guid PublicId,
        string NodeKey,
        string Name,
        string Status,
        string? Description,
        string? ControlApiUrl,
        bool HasNodeSecret,
        DateTimeOffset? NodeSecretUpdatedAt,
        DateTimeOffset? LastSeenAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? DisabledAt);

    public sealed record NodeDetail(
        Guid PublicId,
        string NodeKey,
        string Name,
        string Status,
        string? Description,
        string? ControlApiUrl,
        bool HasNodeSecret,
        DateTimeOffset? NodeSecretUpdatedAt,
        DateTimeOffset? LastSeenAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? DisabledAt,
        IReadOnlyList<NodeEndpoint> Endpoints);

    public sealed record NodeEndpoint(long Id, string Kind, string Url, bool IsPrimary, DateTimeOffset? DisabledAt);

    public sealed record NodeCreateRequest(string NodeKey, string NodeSecret, string Name, string? Description, string ControlApiUrl);

    public sealed record NodeUpdateRequest(string Name, string? Description, string ControlApiUrl, string? NodeSecret = null);

    public sealed record NodeMutationResult(bool Success, string? Message = null, NodeDetail? Node = null);
}
