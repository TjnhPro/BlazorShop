namespace BlazorShop.Storefront.Endpoints
{
    using System.Globalization;

    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Storefront.Configuration;
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Web.SharedV2;
    using BlazorShop.Web.SharedV2.Models;

    using Microsoft.AspNetCore.Antiforgery;

    internal static partial class StorefrontLocalEndpointSupport
    {
        private const string StorefrontConsentVisitorCookieName = "bs-consent-visitor";
    
    
    
    
    
    
    
    
    
    
    
    
    
        internal static string? NormalizeCurrencyCode(string? currencyCode)
    {
        var normalized = currencyCode?.Trim().ToUpperInvariant();
        return normalized is { Length: 3 } && normalized.All(char.IsLetter)
            ? normalized
            : null;
    }
    
        internal static bool IsValidEmail(string? email)
    {
        return !string.IsNullOrWhiteSpace(email)
            && email.Length <= 254
            && new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email);
    }
    
        internal static string? NormalizeOptionalFormValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    
        internal static string FormatMoney(decimal amount, string? currencyCode)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{amount:0.00} {currencyCode ?? string.Empty}").Trim();
    }
    
    
        internal static async Task<IResult?> ValidateLocalCartAntiforgeryAsync(HttpContext httpContext, IAntiforgery antiforgery)
    {
        StorefrontResponseHeaders.ApplyPrivatePage(httpContext);
    
        try
        {
            await antiforgery.ValidateRequestAsync(httpContext);
            return null;
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Json(
                new StorefrontLocalCartErrorResponse("Security validation failed. Refresh the page and try again."),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
    
        internal static string ResolveConsentVisitorKey(HttpContext httpContext, bool createIfMissing)
    {
        if (httpContext.Request.Cookies.TryGetValue(StorefrontConsentVisitorCookieName, out var existing)
            && !string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }
    
        if (!createIfMissing)
        {
            return string.Empty;
        }
    
        var visitorKey = Guid.NewGuid().ToString("N");
        httpContext.Response.Cookies.Append(
            StorefrontConsentVisitorCookieName,
            visitorKey,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = httpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(180),
            });
        return visitorKey;
    }
    
    
    
    
    
    
    }

    public sealed class StorefrontLocalCartLineRequest
    {
        public Guid ProductId { get; set; }
    
        public Guid? ProductVariantId { get; set; }
    
        public string? CurrencyCode { get; set; }
    
        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }
    
        public int Quantity { get; set; } = 1;
    }
    
    public sealed class StorefrontLocalProductSelectionPreviewRequest
    {
        public Guid ProductId { get; set; }
    
        public Guid? ProductVariantId { get; set; }
    
        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }
    
        public int Quantity { get; set; } = 1;
    
        public string? CurrencyCode { get; set; }
    }
    
    public sealed record StorefrontLocalProductSelectionPreviewResponse(
        Guid ProductId,
        Guid? ProductVariantId,
        bool IsValid,
        bool IsAvailable,
        bool CanAddToCart,
        IReadOnlyList<string> ValidationMessages,
        IReadOnlyList<SelectedAttributeDto> SelectedAttributes,
        string? AttributeSignature,
        string? Sku,
        string? DisplayName,
        decimal UnitPrice,
        decimal? ComparePrice,
        string CurrencyCode,
        string FormattedUnitPrice,
        string? FormattedComparePrice,
        int StockQuantity,
        int MinQuantity,
        int MaxQuantity,
        string? PrimaryImageUrl);
    
    public sealed class StorefrontLocalCartQuantityRequest
    {
        public int Quantity { get; set; }
    }
    
    public sealed class StorefrontCurrencyPreferenceForm
    {
        public string? CurrencyCode { get; set; }
    
        public string? ReturnUrl { get; set; }
    }
    
    
}

