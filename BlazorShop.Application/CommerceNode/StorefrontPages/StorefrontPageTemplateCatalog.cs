namespace BlazorShop.Application.CommerceNode.StorefrontPages
{
    public static class StorefrontPageTemplateCatalog
    {
        private static readonly StorefrontPageTemplateDefinitionDto[] Definitions =
        [
            new("about", "About us", "about-us", "About us", true, StorefrontPageContentRules.FooterCompany, 100, "/about-us"),
            new("shipping_information", "Shipping information", "shipping", "Shipping information", true, StorefrontPageContentRules.FooterSupport, 200),
            new("payment_information", "Payment information", "payment", "Payment information", true, StorefrontPageContentRules.FooterSupport, 210),
            new("return_refund_policy", "Return and refund policy", "returns", "Return and refund policy", true, StorefrontPageContentRules.FooterSupport, 220),
            new("faq", "FAQ", "faq", "FAQ", true, StorefrontPageContentRules.FooterSupport, 230, "/faq"),
            new("customer_service", "Customer service", "customer-service", "Customer service", true, StorefrontPageContentRules.FooterSupport, 240, "/customer-service"),
            new("terms_conditions", "Terms and conditions", "terms", "Terms and conditions", true, StorefrontPageContentRules.FooterLegal, 300, "/terms"),
            new("privacy_policy", "Privacy policy", "privacy", "Privacy policy", true, StorefrontPageContentRules.FooterLegal, 310, "/privacy"),
            new("cookie_information", "Cookie information", "cookies", "Cookie information", true, StorefrontPageContentRules.FooterLegal, 320),
            new("home_content", "Home content", "home", "Home content", false, null, 10),
            new("store_closed_content", "Store closed", "store-closed", "Store closed", false, null, 900),
        ];

        public static IReadOnlyList<StorefrontPageTemplateDefinitionDto> ListDefinitions()
        {
            return Definitions;
        }

        public static StorefrontPageTemplateDefinitionDto? Find(string? pageKey)
        {
            if (string.IsNullOrWhiteSpace(pageKey))
            {
                return null;
            }

            return Definitions.FirstOrDefault(definition =>
                string.Equals(definition.PageKey, pageKey.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public static string? FindLegacyPath(string? pageKey)
        {
            return Find(pageKey)?.LegacyPath;
        }
    }
}
