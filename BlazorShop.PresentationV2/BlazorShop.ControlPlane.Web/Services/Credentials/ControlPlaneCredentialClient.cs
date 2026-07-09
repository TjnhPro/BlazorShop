namespace BlazorShop.ControlPlane.Web.Services.Credentials
{
    using BlazorShop.ControlPlane.Web.Services.Common;

    public interface IControlPlaneCredentialClient
    {
        Task<CredentialListResponse> ListAsync(Guid nodePublicId, CancellationToken cancellationToken = default);

        Task<CredentialSecretResult> CreateAsync(Guid nodePublicId, CancellationToken cancellationToken = default);

        Task<CredentialMutationResult> RevokeAsync(Guid nodePublicId, string keyId, CancellationToken cancellationToken = default);

        Task<CredentialSecretMutationResult> RotateAsync(Guid nodePublicId, string keyId, CancellationToken cancellationToken = default);
    }

    public sealed class ControlPlaneCredentialClient : IControlPlaneCredentialClient
    {
        private readonly IControlPlaneApiClient apiClient;

        public ControlPlaneCredentialClient(IControlPlaneApiClient apiClient)
        {
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
