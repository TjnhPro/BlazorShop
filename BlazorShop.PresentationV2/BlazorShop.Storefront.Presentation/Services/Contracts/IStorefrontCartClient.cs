namespace BlazorShop.Storefront.Presentation.Contracts
{
    using BlazorShop.Storefront.Presentation.Services;

    public interface IStorefrontCartClient
    {
        Task<StorefrontSubmitResult<StorefrontCartSessionResponse>> CreateOrResumeCartSessionAsync(
                    string? cartToken,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCartResponse>> GetCartAsync(
                    string cartToken,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCartResponse>> AddCartLineAsync(
                    string cartToken,
                    StorefrontCartLineCreateRequest request,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCartResponse>> UpdateCartLineAsync(
                    string cartToken,
                    Guid lineId,
                    StorefrontCartLineUpdateRequest request,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCartResponse>> RemoveCartLineAsync(
                    string cartToken,
                    Guid lineId,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCartResponse>> ClearCartAsync(
                    string cartToken,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCartResponse>> RecalculateCartAsync(
                    string cartToken,
                    StorefrontCartRecalculateRequest request,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCartResponse>> MergeCurrentCustomerCartAsync(
                    string cartToken,
                    string accessToken,
                    CancellationToken cancellationToken = default);
    }
}
