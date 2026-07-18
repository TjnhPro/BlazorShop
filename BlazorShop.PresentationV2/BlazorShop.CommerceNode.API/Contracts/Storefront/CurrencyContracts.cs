namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    using Microsoft.AspNetCore.Mvc;

    public sealed record StorefrontCurrencyOptionsResponse(
        string DefaultCurrencyCode,
        IReadOnlyList<string> SupportedCurrencyCodes);

    public sealed class StorefrontCurrencyPreferenceRequest
    {
        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CurrencyCode { get; set; } = string.Empty;
    }

    public sealed record StorefrontCurrencyPreferenceResponse(
        string CurrencyCode,
        string BaseCurrencyCode,
        string? RequestedCurrencyCode,
        bool RequestedCurrencySupported,
        bool CheckoutCurrencyEnabled,
        string Reason);
}
