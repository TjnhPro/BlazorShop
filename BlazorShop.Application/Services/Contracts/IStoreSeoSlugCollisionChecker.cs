namespace BlazorShop.Application.Services.Contracts
{
    public interface IStoreSeoSlugCollisionChecker
    {
        Task<bool> SlugExistsAsync(
            string entityType,
            string slug,
            Guid? storeId,
            string? languageCode = null,
            Guid? excludedEntityId = null,
            CancellationToken cancellationToken = default);
    }
}
