namespace BlazorShop.ControlPlane.Web.Services.Stores
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Web.Shared.Helper.Contracts;

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
    }

    public sealed class ControlPlaneStoreClient : IControlPlaneStoreClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly IHttpClientHelper httpClientHelper;

        public ControlPlaneStoreClient(IHttpClientHelper httpClientHelper)
        {
            this.httpClientHelper = httpClientHelper;
        }

        public async Task<StoreListResponse> ListAsync(string? search = null, string? status = null, Guid? nodePublicId = null, CancellationToken cancellationToken = default)
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

            if (nodePublicId is not null)
            {
                query.Add($"nodePublicId={nodePublicId}");
            }

            var route = query.Count == 0 ? "api/control-plane/stores" : $"api/control-plane/stores?{string.Join("&", query)}";
            using var response = await client.GetAsync(route, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<StoreListResponse>(SerializerOptions, cancellationToken)
                       ?? new StoreListResponse([]);
            }

            throw new InvalidOperationException(await ResolveErrorMessageAsync(response, "Unable to load stores."));
        }

        public async Task<StoreDetail?> GetAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.GetAsync($"api/control-plane/stores/{publicId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<StoreDetail>(SerializerOptions, cancellationToken);
            }

            throw new InvalidOperationException(await ResolveErrorMessageAsync(response, "Unable to load store detail."));
        }

        public async Task<StoreMutationResult> CreateAsync(StoreCreateRequest request, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsJsonAsync("api/control-plane/stores", request, SerializerOptions, cancellationToken);
            return await ToMutationResultAsync(response, "Unable to create store.", cancellationToken);
        }

        public async Task<StoreMutationResult> UpdateAsync(Guid publicId, StoreUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PutAsJsonAsync($"api/control-plane/stores/{publicId}", request, SerializerOptions, cancellationToken);
            return await ToMutationResultAsync(response, "Unable to update store.", cancellationToken);
        }

        public async Task<StoreMutationResult> ArchiveAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsync($"api/control-plane/stores/{publicId}/archive", content: null, cancellationToken);
            return await ToMutationResultAsync(response, "Unable to archive store.", cancellationToken);
        }

        public async Task<StoreMutationResult> AddDomainAsync(Guid publicId, StoreDomainCreateRequest request, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsJsonAsync($"api/control-plane/stores/{publicId}/domains", request, SerializerOptions, cancellationToken);
            return await ToMutationResultAsync(response, "Unable to add domain.", cancellationToken);
        }

        public async Task<StoreMutationResult> VerifyDomainAsync(Guid publicId, long domainId, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsync($"api/control-plane/stores/{publicId}/domains/{domainId}/verify", content: null, cancellationToken);
            return await ToMutationResultAsync(response, "Unable to verify domain.", cancellationToken);
        }

        public async Task<StoreMutationResult> DisableDomainAsync(Guid publicId, long domainId, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsync($"api/control-plane/stores/{publicId}/domains/{domainId}/disable", content: null, cancellationToken);
            return await ToMutationResultAsync(response, "Unable to disable domain.", cancellationToken);
        }

        private static async Task<StoreMutationResult> ToMutationResultAsync(
            HttpResponseMessage response,
            string defaultMessage,
            CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                var store = await response.Content.ReadFromJsonAsync<StoreDetail>(SerializerOptions, cancellationToken);
                return new StoreMutationResult(true, Store: store);
            }

            return new StoreMutationResult(false, await ResolveErrorMessageAsync(response, defaultMessage));
        }

        private static async Task<string> ResolveErrorMessageAsync(HttpResponseMessage response, string defaultMessage)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return "Sign in with a Control Plane account that can manage stores.";
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

    public sealed record StoreListResponse(IReadOnlyList<StoreSummary> Items);

    public sealed record StoreSummary(Guid PublicId, string StoreKey, string Name, string Status, Guid NodePublicId, string NodeKey, string NodeName, string NodeStatus, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? ArchivedAt, int DomainCount);

    public sealed record StoreDetail(Guid PublicId, string StoreKey, string Name, string Status, string? MetadataJson, Guid NodePublicId, string NodeKey, string NodeName, string NodeStatus, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? ArchivedAt, IReadOnlyList<StoreDomain> Domains);

    public sealed record StoreDomain(long Id, string Domain, string NormalizedDomain, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? VerifiedAt, DateTimeOffset? DisabledAt);

    public sealed record StoreCreateRequest(string StoreKey, string Name, Guid NodePublicId, string? MetadataJson);

    public sealed record StoreUpdateRequest(string Name, Guid NodePublicId, string? MetadataJson);

    public sealed record StoreDomainCreateRequest(string Domain);

    public sealed record StoreMutationResult(bool Success, string? Message = null, StoreDetail? Store = null);
}
