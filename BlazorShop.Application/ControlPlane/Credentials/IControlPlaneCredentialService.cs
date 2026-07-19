namespace BlazorShop.Application.ControlPlane.Credentials
{
    public interface IControlPlaneCredentialService
    {
        Task<ApplicationResult<ControlPlaneCredentialListResponse>> ListAsync(
            Guid nodePublicId,
            ControlPlaneCredentialListQuery query,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneCredentialSecretResult>> CreateAsync(
            Guid nodePublicId,
            long? actorAdminUserId = null,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneCredentialSummary>> RevokeAsync(
            Guid nodePublicId,
            string keyId,
            long? actorAdminUserId = null,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneCredentialSecretResult>> RotateAsync(
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
