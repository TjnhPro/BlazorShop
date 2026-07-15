namespace BlazorShop.Domain.Constants
{
    public static class StoreFeatureKeys
    {
        public const string Checkout = "checkout";

        public const string CustomerAccounts = "customerAccounts";

        public const string Newsletter = "newsletter";

        public const string Recommendations = "recommendations";

        public const string Reviews = "reviews";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
        {
            Checkout,
            CustomerAccounts,
            Newsletter,
            Recommendations,
            Reviews,
        };
    }
}
