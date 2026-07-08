namespace BlazorShop.ControlPlane.Web.Services.Actions
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.ControlPlane.Web.Services.Common;
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
        private readonly IControlPlaneApiClient apiClient;

        public ControlPlaneActionClient(IHttpClientHelper httpClientHelper, IControlPlaneApiClient apiClient)
        {
            this.httpClientHelper = httpClientHelper;
            this.apiClient = apiClient;
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

            var result = await this.apiClient.GetPrivateAsync<ActionListResponse>(
                $"api/control-plane/actions?{string.Join("&", query)}",
                "Unable to load control actions.",
                cancellationToken);

            if (result.Success)
            {
                return result.Data ?? new ActionListResponse([], null);
            }

            throw new InvalidOperationException(result.Message);
        }

        public async Task<ActionDetail?> GetAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.GetPrivateAsync<ActionDetail>(
                $"api/control-plane/actions/{publicId}",
                "Unable to load action detail.",
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

        public async Task<ActionMutationResult> EnqueueAsync(ActionEnqueueRequest request, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<ActionEnqueueRequest, ActionDetail>(
                "api/control-plane/actions",
                request,
                "Unable to enqueue control action.",
                cancellationToken);

            return new ActionMutationResult(result.Success, result.Message, result.Data);
        }

        public async Task<ActionMutationResult> CancelAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<ActionDetail>(
                $"api/control-plane/actions/{publicId}/cancel",
                "Unable to cancel control action.",
                cancellationToken);

            return new ActionMutationResult(result.Success, result.Message, result.Data);
        }

        private static async Task<string> ResolveErrorMessageAsync(HttpResponseMessage response, string defaultMessage)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return "Sign in with a Control Plane account that can manage actions.";
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

    public sealed record ActionListResponse(IReadOnlyList<ActionSummary> Items, long? NextBeforeId);

    public sealed record ActionSummary(long Id, Guid PublicId, string ActionType, string Status, string IdempotencyKey, string? CorrelationId, Guid NodePublicId, string NodeKey, string NodeName, Guid? StorePublicId, string? StoreKey, string? StoreName, string? ErrorCode, string? ErrorMessage, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, int AttemptCount);

    public sealed record ActionDetail(long Id, Guid PublicId, string ActionType, string Status, string IdempotencyKey, string? CorrelationId, string? PayloadJson, string? ResultJson, string? ErrorCode, string? ErrorMessage, string? SuggestedFix, Guid NodePublicId, string NodeKey, string NodeName, Guid? StorePublicId, string? StoreKey, string? StoreName, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, IReadOnlyList<ActionAttempt> Attempts);

    public sealed record ActionAttempt(long Id, int AttemptNumber, string Status, int? HttpStatusCode, int DurationMs, string? ResponseJson, string? ErrorCode, string? ErrorMessage, string? SuggestedFix, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt);

    public sealed record ActionEnqueueRequest(Guid NodePublicId, string ActionType, string? IdempotencyKey = null, Guid? StorePublicId = null, string? PayloadJson = null, string? CorrelationId = null);

    public sealed record ActionMutationResult(bool Success, string? Message = null, ActionDetail? Action = null);
}
