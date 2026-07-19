namespace BlazorShop.Application.ControlPlane.Stores
{
    public interface IControlPlaneStoreService
    {
        Task<ControlPlaneStoreListResponse> ListAsync(
            ControlPlaneStoreListQuery query,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneStoreDetail>> GetByPublicIdAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneStoreDetail>> CreateAsync(
            CreateControlPlaneStoreRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneStoreDetail>> UpdateAsync(
            Guid publicId,
            UpdateControlPlaneStoreRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneStoreDetail>> ArchiveAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneStoreDetail>> AddDomainAsync(
            Guid publicId,
            CreateControlPlaneStoreDomainRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneStoreDetail>> VerifyDomainAsync(
            Guid publicId,
            long domainId,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneStoreDetail>> DisableDomainAsync(
            Guid publicId,
            long domainId,
            CancellationToken cancellationToken = default);
    }
}
