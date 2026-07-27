namespace BlazorShop.Storefront.Services.Contracts
{
    using BlazorShop.Storefront.Services;

    public interface IStorefrontPaymentClient
    {
        Task<StorefrontApiResult<IReadOnlyList<StorefrontPublicPaymentMethod>>> GetPaymentMethodsAsync(CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<StorefrontPaymentAttemptResponse>> GetPaymentAttemptAsync(
                    Guid paymentAttemptId,
                    CancellationToken cancellationToken = default);
    }
}
