namespace BlazorShop.Storefront.Presentation.Services.Checkout
{
    public sealed record StorefrontPaymentResultPageContext(
        bool IsCancelRoute,
        Guid? PaymentAttemptId,
        string? Provider,
        StorefrontPaymentResultAttemptView? Attempt,
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

    public sealed record StorefrontPaymentResultAttemptView(
        Guid Id,
        string State,
        string? FailureMessage);
}
