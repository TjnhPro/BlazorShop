namespace BlazorShop.Storefront.Services.Contracts
{
    using BlazorShop.Storefront.Services;

    public interface IStorefrontConsentClient
    {
        Task<StorefrontSubmitResult<StorefrontConsentState>> GetConsentAsync(
                    string? visitorKey,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontConsentState>> SaveConsentAsync(
                    string visitorKey,
                    StorefrontConsentSaveRequest request,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontConsentState>> RevokeConsentAsync(
                    string visitorKey,
                    CancellationToken cancellationToken = default);
    }
}
