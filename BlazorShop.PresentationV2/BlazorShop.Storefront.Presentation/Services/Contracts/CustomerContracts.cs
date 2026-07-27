namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;
    using BlazorShop.Storefront.Options;

    using Microsoft.Extensions.Options;


    public sealed record StorefrontCustomerProfileResponse(
        Guid CustomerPublicId,
        string Email,
        string FullName,
        string? FirstName,
        string? LastName,
        string? Company,
        string? PhoneNumber,
        string? PreferredLanguage,
        string? PreferredCurrencyCode,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? LastActivityAtUtc);

    public sealed class StorefrontCustomerProfileUpdateRequest
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Company { get; set; }

        public string? PhoneNumber { get; set; }

        public string? PreferredLanguage { get; set; }

        public string? PreferredCurrencyCode { get; set; }
    }
}
