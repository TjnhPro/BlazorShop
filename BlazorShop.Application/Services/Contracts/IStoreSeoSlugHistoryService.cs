namespace BlazorShop.Application.Services.Contracts
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Seo;

    public interface IStoreSeoSlugHistoryService
    {
        Task<StoreSeoSlugHistoryDto?> GetActiveSlugAsync(
            string entityType,
            Guid entityId,
            Guid storeId,
            string? languageCode = null,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreSeoSlugHistoryDto>> RecordInitialActiveSlugAsync(
            string entityType,
            Guid entityId,
            Guid storeId,
            string slug,
            string? languageCode = null,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreSeoSlugHistoryDto>> ReplaceActiveSlugAsync(
            string entityType,
            Guid entityId,
            Guid storeId,
            string newSlug,
            string? languageCode = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StoreSeoSlugHistoryDto>> ListHistoryAsync(
            string entityType,
            Guid entityId,
            Guid storeId,
            string? languageCode = null,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreSeoSlugBackfillResultDto>> BackfillCurrentSlugsAsync(
            CancellationToken cancellationToken = default);
    }
}
