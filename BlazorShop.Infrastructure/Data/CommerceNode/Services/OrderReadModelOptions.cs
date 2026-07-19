namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    public sealed record OrderReadModelOptions(
        OrderReadVisibility Visibility,
        bool IncludeTrackingEvents,
        bool IncludePaymentAttemptPublicReference,
        bool IncludeAdminNote,
        bool IncludeAllHistory,
        bool UseProductNameFallback,
        bool IncludeLineMoneyDetails)
    {
        public static OrderReadModelOptions Admin()
        {
            return new OrderReadModelOptions(
                OrderReadVisibility.Admin,
                IncludeTrackingEvents: false,
                IncludePaymentAttemptPublicReference: true,
                IncludeAdminNote: true,
                IncludeAllHistory: true,
                UseProductNameFallback: true,
                IncludeLineMoneyDetails: false);
        }

        public static OrderReadModelOptions Customer()
        {
            return new OrderReadModelOptions(
                OrderReadVisibility.Customer,
                IncludeTrackingEvents: true,
                IncludePaymentAttemptPublicReference: false,
                IncludeAdminNote: false,
                IncludeAllHistory: false,
                UseProductNameFallback: true,
                IncludeLineMoneyDetails: true);
        }

        public static OrderReadModelOptions Guest()
        {
            return new OrderReadModelOptions(
                OrderReadVisibility.Guest,
                IncludeTrackingEvents: false,
                IncludePaymentAttemptPublicReference: true,
                IncludeAdminNote: false,
                IncludeAllHistory: false,
                UseProductNameFallback: false,
                IncludeLineMoneyDetails: true);
        }

        public static OrderReadModelOptions Internal()
        {
            return new OrderReadModelOptions(
                OrderReadVisibility.Internal,
                IncludeTrackingEvents: true,
                IncludePaymentAttemptPublicReference: true,
                IncludeAdminNote: true,
                IncludeAllHistory: false,
                UseProductNameFallback: true,
                IncludeLineMoneyDetails: true);
        }
    }
}
