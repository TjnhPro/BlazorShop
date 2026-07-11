namespace BlazorShop.Application.Services.Contracts
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Domain.Contracts;

    public interface IProductVariantService
    {
        Task<IEnumerable<GetProductVariant>> GetByProductIdAsync(Guid productId);

        Task<PagedResult<GetProductVariant>> QueryByProductIdAsync(Guid productId, int pageNumber = 1, int pageSize = 25);

        Task<ServiceResponse> AddAsync(CreateProductVariant variant);

        Task<ServiceResponse> UpdateAsync(UpdateProductVariant variant);

        Task<ServiceResponse> DeleteAsync(Guid variantId);
    }
}
