namespace BlazorShop.ControlPlane.Web.Services.Nodes
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Web.Shared.Helper.Contracts;

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
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly IHttpClientHelper httpClientHelper;

        public ControlPlaneNodeClient(IHttpClientHelper httpClientHelper)
        {
            this.httpClientHelper = httpClientHelper;
        }

        public async Task<NodeListResponse> ListAsync(string? search, string? status, string? cursor, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
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
            using var response = await client.GetAsync(route, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<NodeListResponse>(SerializerOptions, cancellationToken)
                       ?? new NodeListResponse([], null);
            }

            throw new InvalidOperationException(await ResolveErrorMessageAsync(response, "Unable to load nodes."));
        }

        public async Task<NodeDetail?> GetAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.GetAsync($"api/control-plane/nodes/{publicId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<NodeDetail>(SerializerOptions, cancellationToken);
            }

            throw new InvalidOperationException(await ResolveErrorMessageAsync(response, "Unable to load node details."));
        }

        public async Task<NodeMutationResult> CreateAsync(NodeCreateRequest request, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsJsonAsync("api/control-plane/nodes", request, SerializerOptions, cancellationToken);
            return await ToMutationResultAsync(response, "Unable to create node.", cancellationToken);
        }

        public async Task<NodeMutationResult> UpdateAsync(Guid publicId, NodeUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PutAsJsonAsync($"api/control-plane/nodes/{publicId}", request, SerializerOptions, cancellationToken);
            return await ToMutationResultAsync(response, "Unable to update node.", cancellationToken);
        }

        public async Task<NodeMutationResult> DisableAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsync($"api/control-plane/nodes/{publicId}/disable", content: null, cancellationToken);
            return await ToMutationResultAsync(response, "Unable to disable node.", cancellationToken);
        }

        private static async Task<NodeMutationResult> ToMutationResultAsync(
            HttpResponseMessage response,
            string defaultErrorMessage,
            CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                var node = await response.Content.ReadFromJsonAsync<NodeDetail>(SerializerOptions, cancellationToken);
                return new NodeMutationResult(true, Node: node);
            }

            return new NodeMutationResult(false, await ResolveErrorMessageAsync(response, defaultErrorMessage));
        }

        private static async Task<string> ResolveErrorMessageAsync(HttpResponseMessage response, string defaultMessage)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return "Sign in with a Control Plane account that has access to node registry.";
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

    public sealed record NodeListResponse(IReadOnlyList<NodeSummary> Items, string? NextCursor);

    public sealed record NodeSummary(
        Guid PublicId,
        string NodeKey,
        string Name,
        string Status,
        string? Description,
        string? ControlApiUrl,
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
        DateTimeOffset? LastSeenAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? DisabledAt,
        IReadOnlyList<NodeEndpoint> Endpoints);

    public sealed record NodeEndpoint(long Id, string Kind, string Url, bool IsPrimary, DateTimeOffset? DisabledAt);

    public sealed record NodeCreateRequest(string NodeKey, string Name, string? Description, string ControlApiUrl);

    public sealed record NodeUpdateRequest(string Name, string? Description, string ControlApiUrl);

    public sealed record NodeMutationResult(bool Success, string? Message = null, NodeDetail? Node = null);
}
