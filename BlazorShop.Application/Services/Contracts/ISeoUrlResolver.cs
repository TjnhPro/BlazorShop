namespace BlazorShop.Application.Services.Contracts
{
    using BlazorShop.Application.DTOs.Seo;

    public interface ISeoUrlResolver
    {
        Task<SeoUrlResolutionDto> ResolvePublicPathAsync(
            string? path,
            CancellationToken cancellationToken = default);

        Task<SeoUrlResolutionDto> ResolveEntityCanonicalAsync(
            string entityType,
            Guid entityId,
            Guid storeId,
            string? languageCode = null,
            CancellationToken cancellationToken = default);
    }
}
