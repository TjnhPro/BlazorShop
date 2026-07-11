namespace BlazorShop.Application.Services.Contracts
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Domain.Contracts;

    public interface ICategoryService
    {
        Task<IEnumerable<GetCategory>> GetAllAsync();

        Task<PagedResult<GetCategory>> QueryAsync(int pageNumber = 1, int pageSize = 25);

        Task<IReadOnlyList<GetCategoryTreeNode>> GetTreeAsync();

        Task<GetCategory> GetByIdAsync(Guid id);

        Task<ServiceResponse> AddAsync(CreateCategory category);

        Task<ServiceResponse> UpdateAsync(UpdateCategory category);

        Task<ServiceResponse> DeleteAsync(Guid id);

        Task<IEnumerable<GetProduct>> GetProductsByCategoryAsync(Guid id);
    }
}
