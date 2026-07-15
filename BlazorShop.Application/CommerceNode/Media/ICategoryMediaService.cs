namespace BlazorShop.Application.CommerceNode.Media
{
    public interface ICategoryMediaService
    {
        Task<CategoryMediaOperationResult<CategoryMediaAssignmentDto>> GetPrimaryAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default);

        Task<CategoryMediaOperationResult<CategoryMediaAssignmentDto>> SetPrimaryAsync(
            Guid categoryId,
            SetCategoryPrimaryMediaRequest request,
            CancellationToken cancellationToken = default);

        Task<CategoryMediaOperationResult<CategoryMediaAssignmentDto>> ClearPrimaryAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default);
    }
}
