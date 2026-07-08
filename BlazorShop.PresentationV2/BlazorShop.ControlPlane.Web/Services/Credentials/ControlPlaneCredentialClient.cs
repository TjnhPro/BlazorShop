namespace BlazorShop.ControlPlane.Web.Services.Credentials
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.ControlPlane.Web.Services.Common;
    using BlazorShop.Web.Shared.Helper.Contracts;

    public interface IControlPlaneCredentialClient
    {
        Task<CredentialListResponse> ListAsync(Guid nodePublicId, CancellationToken cancellationToken = default);

        Task<CredentialSecretResult> CreateAsync(Guid nodePublicId, CancellationToken cancellationToken = default);

        Task<CredentialMutationResult> RevokeAsync(Guid nodePublicId, string keyId, CancellationToken cancellationToken = default);

        Task<CredentialSecretMutationResult> RotateAsync(Guid nodePublicId, string keyId, CancellationToken cancellationToken = default);
    }

    public sealed class ControlPlaneCredentialClient : IControlPlaneCredentialClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly IHttpClientHelper httpClientHelper;
        private readonly IControlPlaneApiClient apiClient;

        public ControlPlaneCredentialClient(IHttpClientHelper httpClientHelper, IControlPlaneApiClient apiClient)
        {
            this.httpClientHelper = httpClientHelper;
            this.apiClient = apiClient;
        }

        public async Task<CredentialListResponse> ListAsync(Guid nodePublicId, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.GetPrivateAsync<CredentialListResponse>(
                $"api/control-plane/nodes/{nodePublicId}/credentials",
                "Unable to load credentials.",
                cancellationToken);

            if (result.Success)
            {
                return result.Data ?? new CredentialListResponse([]);
            }

            throw new InvalidOperationException(result.Message);
        }

        public async Task<CredentialSecretResult> CreateAsync(Guid nodePublicId, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<object, CredentialSecretResult>(
                $"api/control-plane/nodes/{nodePublicId}/credentials",
                new { },
                "Unable to create credential.",
                cancellationToken);

            if (result.Success && result.Data is not null)
            {
                return result.Data;
            }

            throw new InvalidOperationException(result.Message);
        }

        public async Task<CredentialMutationResult> RevokeAsync(Guid nodePublicId, string keyId, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<CredentialSummary>(
                $"api/control-plane/nodes/{nodePublicId}/credentials/{Uri.EscapeDataString(keyId)}/revoke",
                "Unable to revoke credential.",
                cancellationToken);

            return new CredentialMutationResult(result.Success, result.Message, result.Data);
        }

        public async Task<CredentialSecretMutationResult> RotateAsync(Guid nodePublicId, string keyId, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<CredentialSecretResult>(
                $"api/control-plane/nodes/{nodePublicId}/credentials/{Uri.EscapeDataString(keyId)}/rotate",
                "Unable to rotate credential.",
                cancellationToken);

            return new CredentialSecretMutationResult(result.Success, result.Message, result.Data);
        }

        private static async Task<string> ResolveErrorMessageAsync(HttpResponseMessage response, string defaultMessage)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return "Sign in with a Control Plane account that can rotate credentials.";
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

    public sealed record CredentialListResponse(IReadOnlyList<CredentialSummary> Items);

    public sealed record CredentialSummary(
        string KeyId,
        string Status,
        string HashAlgorithm,
        DateTimeOffset CreatedAt,
        DateTimeOffset? RevealedAt,
        DateTimeOffset? RevokedAt);

    public sealed record CredentialSecretResult(CredentialSummary Credential, string RawSecret);

    public sealed record CredentialMutationResult(bool Success, string? Message = null, CredentialSummary? Credential = null);

    public sealed record CredentialSecretMutationResult(bool Success, string? Message = null, CredentialSecretResult? Credential = null);
}
