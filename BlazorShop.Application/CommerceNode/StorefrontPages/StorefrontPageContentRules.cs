namespace BlazorShop.Application.CommerceNode.StorefrontPages
{
    public static class StorefrontPageContentRules
    {
        public const string FooterCompany = "footer_company";
        public const string FooterSupport = "footer_support";
        public const string FooterLegal = "footer_legal";
        public const string Header = "header";

        public static readonly IReadOnlySet<string> PageKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "about",
            "faq",
            "customer_service",
            "shipping_information",
            "payment_information",
            "terms_conditions",
            "privacy_policy",
            "return_refund_policy",
            "cookie_information",
            "home_content",
            "store_closed_content",
        };

        public static readonly IReadOnlySet<string> NavigationLocations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Header,
            FooterCompany,
            FooterSupport,
            FooterLegal,
        };

        public static bool IsKnownPageKey(string value)
        {
            return PageKeys.Contains(value);
        }

        public static bool IsKnownNavigationLocation(string value)
        {
            return NavigationLocations.Contains(value);
        }
    }
}
