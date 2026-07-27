namespace BlazorShop.Storefront.Presentation.Services.Checkout
{
    using BlazorShop.Storefront.Client;

    public sealed record StorefrontPaymentResultPageContext(
        bool IsCancelRoute,
        Guid? PaymentAttemptId,
        string? Provider,
        StorefrontPaymentAttemptResponse? Attempt,
        string LoadError,
        string Eyebrow,
        string Heading,
        string Body,
        string PanelClass,
        string EyebrowClass,
        string HeadingClass,
        string BodyClass,
        string MutedClass,
        bool IsPending,
        bool IsSuccess,
        bool ShowRetry)
    {
        public bool HasAttempt => Attempt is not null;
    }
}
