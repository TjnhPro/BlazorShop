namespace BlazorShop.Application.ControlPlane.Users
{
    public interface IControlPlaneUserManagementService
    {
        Task<ControlPlaneUserListResponse> ListAsync(
            ControlPlaneUserListQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> GetAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneRoleCatalogResponse> ListRolesAsync(
            CancellationToken cancellationToken = default);

        Task<ControlPlanePermissionCatalogResponse> ListPermissionsAsync(
            CancellationToken cancellationToken = default);
    }
}
