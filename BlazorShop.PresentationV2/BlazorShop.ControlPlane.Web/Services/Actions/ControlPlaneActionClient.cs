namespace BlazorShop.ControlPlane.Web.Services.Actions
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Web.Shared.Helper.Contracts;

    public interface IControlPlaneActionClient
    {
        Task<ActionListResponse> ListAsync(
            string? status = null,
            string? actionType = null,
            Guid? nodePublicId = null,
            Guid? storePublicId = null,
            long? beforeId = null,
            int limit = 100,
            CancellationToken cancellationToken = default);

        Task<ActionDetail?> GetAsync(Guid publicId, CancellationToken cancellationToken = default);

        Task<ActionMutationResult> EnqueueAsync(ActionEnqueueRequest request, CancellationToken cancellationToken = default);

        Task<ActionMutationResult> CancelAsync(Guid publicId, CancellationToken cancellationToken = default);
    }

    public sealed class ControlPlaneActionClient : IControlPlaneActionClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly IHttpClientHelper httpClientHelper;

        public ControlPlaneActionClient(IHttpClientHelper httpClientHelper)
        {
            this.httpClientHelper = httpClientHelper;
        }

        public async Task<ActionListResponse> ListAsync(
            string? status = null,
            string? actionType = null,
            Guid? nodePublicId = null,
            Guid? storePublicId = null,
            long? beforeId = null,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            var query = new List<string> { $"limit={limit}" };

            if (!string.IsNullOrWhiteSpace(status))
            {
                query.Add($"status={Uri.EscapeDataString(status)}");
            }

            if (!string.IsNullOrWhiteSpace(actionType))
            {
                query.Add($"actionType={Uri.EscapeDataString(actionType)}");
            }

            if (nodePublicId is not null)
            {
                query.Add($"nodePublicId={nodePublicId}");
            }

            if (storePublicId is not null)
            {
                query.Add($"storePublicId={storePublicId}");
            }

            if (beforeId is not null)
            {
                query.Add($"beforeId={beforeId}");
            }

            using var response = await client.GetAsync($"api/control-plane/actions?{string.Join("&", query)}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ActionListResponse>(SerializerOptions, cancellationToken)
                       ?? new ActionListResponse([], null);
            }

            throw new InvalidOperationException(await ResolveErrorMessageAsync(response, "Unable to load control actions."));
        }

        public async Task<ActionDetail?> GetAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.GetAsync($"api/control-plane/actions/{publicId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ActionDetail>(SerializerOptions, cancellationToken);
            }

            throw new InvalidOperationException(await ResolveErrorMessageAsync(response, "Unable to load action detail."));
        }

        public async Task<ActionMutationResult> EnqueueAsync(ActionEnqueueRequest request, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsJsonAsync("api/control-plane/actions", request, SerializerOptions, cancellationToken);
            return await ToMutationResultAsync(response, "Unable to enqueue control action.", cancellationToken);
        }

        public async Task<ActionMutationResult> CancelAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsync($"api/control-plane/actions/{publicId}/cancel", content: null, cancellationToken);
            return await ToMutationResultAsync(response, "Unable to cancel control action.", cancellationToken);
        }

        private static async Task<ActionMutationResult> ToMutationResultAsync(
            HttpResponseMessage response,
            string defaultMessage,
            CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                var action = await response.Content.ReadFromJsonAsync<ActionDetail>(SerializerOptions, cancellationToken);
                return new ActionMutationResult(true, Action: action);
            }

            return new ActionMutationResult(false, await ResolveErrorMessageAsync(response, defaultMessage));
        }

        private static async Task<string> ResolveErrorMessageAsync(HttpResponseMessage response, string defaultMessage)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return "Sign in with a Control Plane account that can manage actions.";
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

    public sealed record ActionListResponse(IReadOnlyList<ActionSummary> Items, long? NextBeforeId);

    public sealed record ActionSummary(long Id, Guid PublicId, string ActionType, string Status, string IdempotencyKey, string? CorrelationId, Guid NodePublicId, string NodeKey, string NodeName, Guid? StorePublicId, string? StoreKey, string? StoreName, string? ErrorCode, string? ErrorMessage, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, int AttemptCount);

    public sealed record ActionDetail(long Id, Guid PublicId, string ActionType, string Status, string IdempotencyKey, string? CorrelationId, string? PayloadJson, string? ResultJson, string? ErrorCode, string? ErrorMessage, string? SuggestedFix, Guid NodePublicId, string NodeKey, string NodeName, Guid? StorePublicId, string? StoreKey, string? StoreName, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, IReadOnlyList<ActionAttempt> Attempts);

    public sealed record ActionAttempt(long Id, int AttemptNumber, string Status, int? HttpStatusCode, int DurationMs, string? ResponseJson, string? ErrorCode, string? ErrorMessage, string? SuggestedFix, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt);

    public sealed record ActionEnqueueRequest(Guid NodePublicId, string ActionType, string? IdempotencyKey = null, Guid? StorePublicId = null, string? PayloadJson = null, string? CorrelationId = null);

    public sealed record ActionMutationResult(bool Success, string? Message = null, ActionDetail? Action = null);
}
