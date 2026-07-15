namespace BlazorShop.Application.CommerceNode.Stores
{
    public interface ICommerceStoreService
    {
        Task<CommerceStoreOperationResult<CommerceStoreListResponse>> ListAsync(
            CommerceStoreListQuery query,
            CancellationToken cancellationToken = default);

        Task<CommerceStoreOperationResult<CommerceStoreDetail>> GetByPublicIdAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<CommerceStoreOperationResult<CommerceStoreDetail>> CreateAsync(
            CreateCommerceStoreRequest request,
            CancellationToken cancellationToken = default);

        Task<CommerceStoreOperationResult<CommerceStoreDetail>> UpdateAsync(
            Guid publicId,
            UpdateCommerceStoreRequest request,
            CancellationToken cancellationToken = default);

        Task<CommerceStoreOperationResult<CommerceStoreDetail>> SetStatusAsync(
            Guid publicId,
            string status,
            CancellationToken cancellationToken = default);

        Task<CommerceStoreOperationResult<CommerceStoreDetail>> ArchiveAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<CommerceStoreOperationResult<CommerceStoreDetail>> AddDomainAsync(
            Guid publicId,
            CreateCommerceStoreDomainRequest request,
            CancellationToken cancellationToken = default);

        Task<CommerceStoreOperationResult<CommerceStoreDetail>> VerifyDomainAsync(
            Guid publicId,
            Guid domainId,
            CancellationToken cancellationToken = default);

        Task<CommerceStoreOperationResult<CommerceStoreDetail>> DisableDomainAsync(
            Guid publicId,
            Guid domainId,
            CancellationToken cancellationToken = default);

        Task<CommerceStoreOperationResult<CommerceStoreDetail>> SetPrimaryDomainAsync(
            Guid publicId,
            Guid domainId,
            CancellationToken cancellationToken = default);
    }
}
