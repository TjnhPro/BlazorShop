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


    public sealed record StorefrontPublicPaymentMethod(
        Guid Id,
        string Key,
        string Name,
        string? Description,
        string? ShortDisplayText,
        string? IconUrl,
        IReadOnlyList<string> SupportedCurrencyCodes,
        IReadOnlyList<string> SupportedCountryCodes);

    public sealed record StorefrontPaymentNextActionResponse(
        string Type,
        string? Url);

    public sealed record StorefrontPaymentAttemptResponse(
        Guid Id,
        Guid CheckoutSessionId,
        Guid? OrderId,
        string PaymentMethodKey,
        string ProviderKey,
        string State,
        decimal Amount,
        string CurrencyCode,
        string? ProviderReference,
        string? ProviderSessionId,
        StorefrontPaymentNextActionResponse? NextAction,
        string? FailureCode,
        string? FailureMessage,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);
}
