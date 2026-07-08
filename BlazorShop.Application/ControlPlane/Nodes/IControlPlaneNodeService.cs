namespace BlazorShop.Application.ControlPlane.Nodes
{
    public interface IControlPlaneNodeService
    {
        Task<ControlPlaneNodeListResponse> ListAsync(
            ControlPlaneNodeListQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneNodeOperationResult<ControlPlaneNodeDetail>> GetByPublicIdAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneNodeOperationResult<ControlPlaneNodeDetail>> CreateAsync(
            CreateControlPlaneNodeRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneNodeOperationResult<ControlPlaneNodeDetail>> UpdateAsync(
            Guid publicId,
            UpdateControlPlaneNodeRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneNodeOperationResult<ControlPlaneNodeDetail>> DisableAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);
    }
}
