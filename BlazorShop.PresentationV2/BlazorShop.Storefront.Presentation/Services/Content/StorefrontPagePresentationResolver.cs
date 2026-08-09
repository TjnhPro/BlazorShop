namespace BlazorShop.Storefront.Presentation.Services
{

    using BlazorShop.Storefront.Presentation.Models;
    using BlazorShop.Storefront.Presentation.Contracts;

    public interface IStorefrontPagePresentationResolver
    {
        StorefrontPagePresentation Resolve(GetStorefrontPage page);
    }

    public sealed class StorefrontPagePresentationResolver : IStorefrontPagePresentationResolver
    {
        public StorefrontPagePresentation Resolve(GetStorefrontPage page)
        {
            ArgumentNullException.ThrowIfNull(page);

            var pageKey = Normalize(page.PageKey);
            return pageKey switch
            {
                "shipping_information" or "payment_information" or "return_refund_policy" or "terms_conditions" or "privacy_policy" or "cookie_information"
                    => StorefrontPagePresentation.Policy(pageKey),
                "faq" => StorefrontPagePresentation.Faq(pageKey, []),
                "customer_service" => StorefrontPagePresentation.Support(pageKey),
                _ => StorefrontPagePresentation.Standard(pageKey),
            };
        }

        private static string Normalize(string? value)
        {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? "standard"
                : value.Trim().ToLowerInvariant();

            return StorefrontPageContentRules.IsKnownPageKey(normalized)
                ? normalized
                : "standard";
        }
    }

    public sealed record StorefrontPagePresentation(
        string TemplateKey,
        StorefrontPageLayoutKind LayoutKind,
        StorefrontPageStructuredDataKind StructuredDataKind,
        IReadOnlyList<StorefrontFaqStructuredDataItem> FaqEntries,
        string Eyebrow)
    {
        public static StorefrontPagePresentation Standard(string templateKey)
        {
            return new StorefrontPagePresentation(
                templateKey,
                StorefrontPageLayoutKind.Standard,
                StorefrontPageStructuredDataKind.WebPage,
                [],
                "Store page");
        }

        public static StorefrontPagePresentation Policy(string templateKey)
        {
            return new StorefrontPagePresentation(
                templateKey,
                StorefrontPageLayoutKind.Policy,
                StorefrontPageStructuredDataKind.WebPage,
                [],
                "Policy");
        }

        public static StorefrontPagePresentation Faq(string templateKey, IReadOnlyList<StorefrontFaqStructuredDataItem> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            return new StorefrontPagePresentation(
                templateKey,
                StorefrontPageLayoutKind.Faq,
                entries.Count > 0 ? StorefrontPageStructuredDataKind.FaqPage : StorefrontPageStructuredDataKind.WebPage,
                entries,
                "Help");
        }

        public static StorefrontPagePresentation Support(string templateKey)
        {
            return new StorefrontPagePresentation(
                templateKey,
                StorefrontPageLayoutKind.Support,
                StorefrontPageStructuredDataKind.WebPage,
                [],
                "Support");
        }
    }

    public enum StorefrontPageLayoutKind
    {
        Standard,
        Policy,
        Faq,
        Support,
    }

    public enum StorefrontPageStructuredDataKind
    {
        WebPage,
        FaqPage,
    }
}
