namespace BlazorShop.Application.CommerceNode.Media
{
    using BlazorShop.Application.Common.Results;

    public interface ICategoryMediaService
    {
        Task<ApplicationResult<CategoryMediaAssignmentDto>> GetPrimaryAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CategoryMediaAssignmentDto>> SetPrimaryAsync(
            Guid categoryId,
            SetCategoryPrimaryMediaRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CategoryMediaAssignmentDto>> ClearPrimaryAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default);
    }
}
