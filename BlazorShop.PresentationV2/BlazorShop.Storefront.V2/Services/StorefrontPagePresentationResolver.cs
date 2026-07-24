namespace BlazorShop.Storefront.Services
{

    using BlazorShop.Storefront.Models;
using BlazorShop.Storefront.Services.Contracts;

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
        string Eyebrow,
        string ArticleClass,
        string BodyContainerClass)
    {
        public static StorefrontPagePresentation Standard(string templateKey)
        {
            return new StorefrontPagePresentation(
                templateKey,
                StorefrontPageLayoutKind.Standard,
                StorefrontPageStructuredDataKind.WebPage,
                [],
                "Store page",
                "bs-storefront-content-page bs-storefront-content-page--standard",
                "rounded-3xl border border-neutral-200/70 bg-white/90 p-6 shadow-lg sm:p-8");
        }

        public static StorefrontPagePresentation Policy(string templateKey)
        {
            return new StorefrontPagePresentation(
                templateKey,
                StorefrontPageLayoutKind.Policy,
                StorefrontPageStructuredDataKind.WebPage,
                [],
                "Policy",
                "bs-storefront-content-page bs-storefront-content-page--policy",
                "rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm sm:p-8");
        }

        public static StorefrontPagePresentation Faq(string templateKey, IReadOnlyList<StorefrontFaqStructuredDataItem> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            return new StorefrontPagePresentation(
                templateKey,
                StorefrontPageLayoutKind.Faq,
                entries.Count > 0 ? StorefrontPageStructuredDataKind.FaqPage : StorefrontPageStructuredDataKind.WebPage,
                entries,
                "Help",
                "bs-storefront-content-page bs-storefront-content-page--faq",
                "rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm sm:p-8");
        }

        public static StorefrontPagePresentation Support(string templateKey)
        {
            return new StorefrontPagePresentation(
                templateKey,
                StorefrontPageLayoutKind.Support,
                StorefrontPageStructuredDataKind.WebPage,
                [],
                "Support",
                "bs-storefront-content-page bs-storefront-content-page--support",
                "rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm sm:p-8");
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
