namespace BlazorShop.Application.CommerceNode.Features
{
    using BlazorShop.Application.DTOs;

    public sealed record StoreFeatureStateDto(
        string FeatureKey,
        string DisplayName,
        string Description,
        bool Enabled,
        bool DefaultEnabled,
        bool PubliclyVisible,
        string? Reason,
        DateTime? UpdatedAt);

    public sealed record UpdateStoreFeatureStateRequest(
        bool Enabled,
        string? Reason);

    public sealed record StoreFeatureStateSnapshot(
        bool CustomerAccountsEnabled,
        bool CheckoutEnabled,
        bool NewsletterEnabled,
        bool RecommendationsEnabled,
        bool ReviewsEnabled);

    public interface IStoreFeatureStateService
    {
        Task<IReadOnlyList<StoreFeatureStateDto>> GetAsync(CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreFeatureStateDto>> UpdateAsync(
            string featureKey,
            UpdateStoreFeatureStateRequest request,
            CancellationToken cancellationToken = default);

        Task<StoreFeatureStateSnapshot> ResolveAsync(Guid storeId, CancellationToken cancellationToken = default);

        Task<bool> IsEnabledAsync(Guid storeId, string featureKey, CancellationToken cancellationToken = default);
    }
}
