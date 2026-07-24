namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Web.SharedV2.Models;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Storefront.Options;

    using Microsoft.Extensions.Options;


    public sealed record StorefrontCurrencyOptions(
        string DefaultCurrencyCode,
        IReadOnlyList<string> SupportedCurrencyCodes);

    public sealed class StorefrontCurrencyPreferenceRequest
    {
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
