namespace BlazorShop.Application.ControlPlane.Credentials
{
    public interface IControlPlaneCredentialService
    {
        Task<ControlPlaneCredentialOperationResult<ControlPlaneCredentialListResponse>> ListAsync(
            Guid nodePublicId,
            ControlPlaneCredentialListQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCredentialOperationResult<ControlPlaneCredentialSecretResult>> CreateAsync(
            Guid nodePublicId,
            long? actorAdminUserId = null,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCredentialOperationResult<ControlPlaneCredentialSummary>> RevokeAsync(
            Guid nodePublicId,
            string keyId,
            long? actorAdminUserId = null,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCredentialOperationResult<ControlPlaneCredentialSecretResult>> RotateAsync(
            Guid nodePublicId,
            string keyId,
            long? actorAdminUserId = null,
            CancellationToken cancellationToken = default);

        Task<bool> VerifyAsync(
            string keyId,
            string rawSecret,
            CancellationToken cancellationToken = default);
    }
}
