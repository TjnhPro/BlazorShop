namespace BlazorShop.Application.CommerceNode.ProductMedia
{
    using BlazorShop.Application.Common.Results;

    public interface IProductMediaService
    {
        Task<ApplicationResult<ProductMediaListResponse>> ListAsync(
            Guid productId,
            ProductMediaListQuery query,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ImportProductMediaResponse>> ImportAsync(
            Guid productId,
            ImportProductMediaRequest request,
            string? createdBy = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ImportProductMediaResponse>> ImportForStoreAsync(
            Guid storeId,
            Guid productId,
            ImportProductMediaRequest request,
            string? createdBy = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ProductMediaDto>> SetPrimaryAsync(
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ProductMediaListResponse>> UpdateOrderAsync(
            Guid productId,
            UpdateProductMediaOrderRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ProductMediaListResponse>> DeleteAsync(
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ImportProductMediaResponse>> RetryAsync(
            Guid productId,
            Guid mediaPublicId,
            string? createdBy = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default);
    }
}
