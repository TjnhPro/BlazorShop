namespace BlazorShop.Application.CommerceNode.Consent
{
    using BlazorShop.Application.DTOs;

    public interface IStorefrontConsentService
    {
        Task<ServiceResponse<StorefrontConsentSnapshot>> GetCurrentAsync(
            Guid storeId,
            string? visitorKey,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontConsentSnapshot>> SaveAsync(
            Guid storeId,
            string visitorKey,
            StorefrontConsentSaveRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontConsentSnapshot>> RevokeAsync(
            Guid storeId,
            string visitorKey,
            CancellationToken cancellationToken = default);
    }
}
