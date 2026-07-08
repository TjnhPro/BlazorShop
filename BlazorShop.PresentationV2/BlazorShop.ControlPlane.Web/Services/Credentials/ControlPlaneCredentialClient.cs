namespace BlazorShop.ControlPlane.Web.Services.Credentials
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

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

        public ControlPlaneCredentialClient(IHttpClientHelper httpClientHelper)
        {
            this.httpClientHelper = httpClientHelper;
        }

        public async Task<CredentialListResponse> ListAsync(Guid nodePublicId, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.GetAsync($"api/control-plane/nodes/{nodePublicId}/credentials", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CredentialListResponse>(SerializerOptions, cancellationToken)
                       ?? new CredentialListResponse([]);
            }

            throw new InvalidOperationException(await ResolveErrorMessageAsync(response, "Unable to load credentials."));
        }

        public async Task<CredentialSecretResult> CreateAsync(Guid nodePublicId, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsJsonAsync($"api/control-plane/nodes/{nodePublicId}/credentials", new { }, SerializerOptions, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CredentialSecretResult>(SerializerOptions, cancellationToken)
                       ?? throw new InvalidOperationException("Credential response was empty.");
            }

            throw new InvalidOperationException(await ResolveErrorMessageAsync(response, "Unable to create credential."));
        }

        public async Task<CredentialMutationResult> RevokeAsync(Guid nodePublicId, string keyId, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsync($"api/control-plane/nodes/{nodePublicId}/credentials/{Uri.EscapeDataString(keyId)}/revoke", content: null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var credential = await response.Content.ReadFromJsonAsync<CredentialSummary>(SerializerOptions, cancellationToken);
                return new CredentialMutationResult(true, Credential: credential);
            }

            return new CredentialMutationResult(false, await ResolveErrorMessageAsync(response, "Unable to revoke credential."));
        }

        public async Task<CredentialSecretMutationResult> RotateAsync(Guid nodePublicId, string keyId, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsync($"api/control-plane/nodes/{nodePublicId}/credentials/{Uri.EscapeDataString(keyId)}/rotate", content: null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var credential = await response.Content.ReadFromJsonAsync<CredentialSecretResult>(SerializerOptions, cancellationToken);
                return new CredentialSecretMutationResult(true, Credential: credential);
            }

            return new CredentialSecretMutationResult(false, await ResolveErrorMessageAsync(response, "Unable to rotate credential."));
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
