namespace BlazorShop.Application.CommerceNode.Settings
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Seo;

    public interface IStoreSeoSettingsService
    {
        Task<SeoSettingsDto> ResolveAsync(CancellationToken cancellationToken = default);

        Task<ServiceResponse<SeoSettingsDto>> SaveOverrideAsync(
            UpdateSeoSettingsDto request,
            CancellationToken cancellationToken = default);
    }
}
