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

        Task<ControlPlaneUserOperationResult<CreateControlPlaneUserResponse>> CreateAsync(
            CreateControlPlaneUserRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> UpdateAsync(
            Guid publicId,
            UpdateControlPlaneUserRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> DisableAsync(
            Guid publicId,
            ChangeControlPlaneUserStatusRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> EnableAsync(
            Guid publicId,
            ChangeControlPlaneUserStatusRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> AssignRoleAsync(
            Guid publicId,
            AssignControlPlaneRoleRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> RemoveRoleAsync(
            Guid publicId,
            string roleKey,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> AssignPermissionAsync(
            Guid publicId,
            AssignControlPlanePermissionRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> RemovePermissionAsync(
            Guid publicId,
            string permissionKey,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneRoleCatalogResponse> ListRolesAsync(
            CancellationToken cancellationToken = default);

        Task<ControlPlanePermissionCatalogResponse> ListPermissionsAsync(
            CancellationToken cancellationToken = default);
    }
}
