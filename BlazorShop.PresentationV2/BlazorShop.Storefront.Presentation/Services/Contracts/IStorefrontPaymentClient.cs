namespace BlazorShop.Storefront.Presentation.Contracts
{
    using BlazorShop.Storefront.Presentation.Services;

    public interface IStorefrontPaymentClient
    {
        Task<StorefrontApiResult<IReadOnlyList<StorefrontPublicPaymentMethod>>> GetPaymentMethodsAsync(CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<StorefrontPaymentAttemptResponse>> GetPaymentAttemptAsync(
                    Guid paymentAttemptId,
                    CancellationToken cancellationToken = default);
    }
}
