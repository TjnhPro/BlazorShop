namespace BlazorShop.Storefront.Presentation.Contracts
{
    public interface IStorefrontDisplayContextProvider
    {
        Task<StorefrontDisplayContext> GetAsync(CancellationToken cancellationToken = default);
    }

    public sealed record StorefrontDisplayContext(
        string StoreKey,
        string StoreName,
        string CultureName,
        string LanguageCode,
        string TextDirection,
        string CurrencyCode,
        string? LogoUrl,
        string? FaviconUrl,
        string? PngIconUrl,
        string? AppleTouchIconUrl,
        string? MsTileImageUrl,
        string? MsTileColor,
        string? CompanyName,
        string? CompanyEmail,
        string? CompanyPhone,
        string? CompanyAddress,
        string? SupportEmail,
        string? SupportPhone,
        string DefaultCurrencyCode,
        IReadOnlyList<string> SupportedCurrencyCodes)
    {
        public static StorefrontDisplayContext Fallback { get; } = new(
            "default",
            "BlazorShop",
            "en-US",
            "en",
            "ltr",
            "USD",
            LogoUrl: null,
            FaviconUrl: null,
            PngIconUrl: null,
            AppleTouchIconUrl: null,
            MsTileImageUrl: null,
            MsTileColor: null,
            CompanyName: null,
            CompanyEmail: null,
            CompanyPhone: null,
            CompanyAddress: null,
            SupportEmail: null,
            SupportPhone: null,
            DefaultCurrencyCode: "USD",
            SupportedCurrencyCodes: ["USD"]);
    }
}
