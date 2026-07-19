namespace BlazorShop.Application.ControlPlane.Nodes
{
    public interface IControlPlaneNodeService
    {
        Task<ControlPlaneNodeListResponse> ListAsync(
            ControlPlaneNodeListQuery query,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneNodeDetail>> GetByPublicIdAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneNodeDetail>> CreateAsync(
            CreateControlPlaneNodeRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneNodeDetail>> UpdateAsync(
            Guid publicId,
            UpdateControlPlaneNodeRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneNodeDetail>> DisableAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);
    }
}
