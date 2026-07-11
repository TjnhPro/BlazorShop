namespace BlazorShop.Application.CommerceNode.ProductMedia
{
    public interface IProductMediaService
    {
        Task<ProductMediaOperationResult<ProductMediaListResponse>> ListAsync(
            Guid productId,
            ProductMediaListQuery query,
            CancellationToken cancellationToken = default);

        Task<ProductMediaOperationResult<ImportProductMediaResponse>> ImportAsync(
            Guid productId,
            ImportProductMediaRequest request,
            string? createdBy = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default);

        Task<ProductMediaOperationResult<ImportProductMediaResponse>> ImportForStoreAsync(
            Guid storeId,
            Guid productId,
            ImportProductMediaRequest request,
            string? createdBy = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default);

        Task<ProductMediaOperationResult<ProductMediaDto>> SetPrimaryAsync(
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default);

        Task<ProductMediaOperationResult<ProductMediaListResponse>> UpdateOrderAsync(
            Guid productId,
            UpdateProductMediaOrderRequest request,
            CancellationToken cancellationToken = default);

        Task<ProductMediaOperationResult<ProductMediaListResponse>> DeleteAsync(
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default);

        Task<ProductMediaOperationResult<ImportProductMediaResponse>> RetryAsync(
            Guid productId,
            Guid mediaPublicId,
            string? createdBy = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default);
    }
}
