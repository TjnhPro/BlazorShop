namespace BlazorShop.CommerceNode.API.Responses
{
    public static class StorefrontErrorCodes
    {
        public const string AuthUnauthenticated = "auth.unauthenticated";
        public const string AuthRefreshCookieMissing = "auth.refresh_cookie_missing";
        public const string AccountRegistrationDisabled = "account.registration_disabled";
        public const string AccountEmailMissing = "account.email_missing";
        public const string CartVersionStale = "cart.version_stale";
        public const string CartValidationFailed = "cart.validation_failed";
        public const string CheckoutValidationFailed = "checkout.validation_failed";
        public const string PaymentOperationNotSupported = "payment.operation_not_supported";
        public const string CatalogNotFound = "catalog.not_found";
        public const string ContentNotFound = "content.not_found";
        public const string ResourceNotFound = "resource.not_found";
        public const string StoreNotFound = "store.not_found";
        public const string StoreUnavailable = "store.unavailable";
        public const string StoreMaintenance = "store.maintenance";
        public const string ValidationFailed = "validation.failed";
        public const string InternalError = "internal.error";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
        {
            AuthUnauthenticated,
            AuthRefreshCookieMissing,
            AccountRegistrationDisabled,
            AccountEmailMissing,
            CartVersionStale,
            CartValidationFailed,
            CheckoutValidationFailed,
            PaymentOperationNotSupported,
            CatalogNotFound,
            ContentNotFound,
            ResourceNotFound,
            StoreNotFound,
            StoreUnavailable,
            StoreMaintenance,
            ValidationFailed,
            InternalError,
        };
    }
}
