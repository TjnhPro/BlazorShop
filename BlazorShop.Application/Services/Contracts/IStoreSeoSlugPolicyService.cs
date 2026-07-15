namespace BlazorShop.Application.Services.Contracts
{
    using BlazorShop.Application.DTOs.Seo;

    public interface IStoreSeoSlugPolicyService
    {
        Task<StoreSeoSlugPolicyResult> GenerateSlugAsync(
            string entityType,
            string? sourceName,
            Guid? storeId,
            string? languageCode = null,
            Guid? excludedEntityId = null,
            CancellationToken cancellationToken = default);

        Task<StoreSeoSlugPolicyResult> ValidateSlugAsync(
            string entityType,
            string? slug,
            Guid? storeId,
            string? languageCode = null,
            Guid? excludedEntityId = null,
            CancellationToken cancellationToken = default);
    }
}
