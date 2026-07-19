namespace BlazorShop.Application.ControlPlane.Users
{
    public interface IControlPlaneUserManagementService
    {
        Task<ControlPlaneUserListResponse> ListAsync(
            ControlPlaneUserListQuery query,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneUserDetail>> GetAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CreateControlPlaneUserResponse>> CreateAsync(
            CreateControlPlaneUserRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneUserDetail>> UpdateAsync(
            Guid publicId,
            UpdateControlPlaneUserRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneUserDetail>> DisableAsync(
            Guid publicId,
            ChangeControlPlaneUserStatusRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneUserDetail>> EnableAsync(
            Guid publicId,
            ChangeControlPlaneUserStatusRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneUserDetail>> AssignRoleAsync(
            Guid publicId,
            AssignControlPlaneRoleRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneUserDetail>> RemoveRoleAsync(
            Guid publicId,
            string roleKey,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneUserDetail>> AssignPermissionAsync(
            Guid publicId,
            AssignControlPlanePermissionRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneUserDetail>> RemovePermissionAsync(
            Guid publicId,
            string permissionKey,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneRoleCatalogResponse> GetRoleCatalogAsync(
            CancellationToken cancellationToken = default);

        Task<ControlPlanePermissionCatalogResponse> GetPermissionCatalogAsync(
            CancellationToken cancellationToken = default);
    }
}
