namespace BlazorShop.Application.ControlPlane.Stores
{
    public interface IControlPlaneStoreService
    {
        Task<ControlPlaneStoreListResponse> ListAsync(
            ControlPlaneStoreListQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>> GetByPublicIdAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>> CreateAsync(
            CreateControlPlaneStoreRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>> UpdateAsync(
            Guid publicId,
            UpdateControlPlaneStoreRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>> ArchiveAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>> AddDomainAsync(
            Guid publicId,
            CreateControlPlaneStoreDomainRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>> VerifyDomainAsync(
            Guid publicId,
            long domainId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>> DisableDomainAsync(
            Guid publicId,
            long domainId,
            CancellationToken cancellationToken = default);
    }
}
