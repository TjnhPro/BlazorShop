namespace BlazorShop.Application.CommerceNode.Stores
{
    using BlazorShop.Application.Common.Results;

    public interface ICommerceStoreService
    {
        Task<ApplicationResult<CommerceStoreListResponse>> ListAsync(
            CommerceStoreListQuery query,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceStoreDetail>> GetByPublicIdAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceStoreDetail>> CreateAsync(
            CreateCommerceStoreRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceStoreDetail>> UpdateAsync(
            Guid publicId,
            UpdateCommerceStoreRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceStoreDetail>> SetStatusAsync(
            Guid publicId,
            string status,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceStoreDetail>> ArchiveAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceStoreDetail>> AddDomainAsync(
            Guid publicId,
            CreateCommerceStoreDomainRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceStoreDetail>> VerifyDomainAsync(
            Guid publicId,
            Guid domainId,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceStoreDetail>> DisableDomainAsync(
            Guid publicId,
            Guid domainId,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceStoreDetail>> SetPrimaryDomainAsync(
            Guid publicId,
            Guid domainId,
            CancellationToken cancellationToken = default);
    }
}
