namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    using Microsoft.AspNetCore.Mvc;

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
        [Required]
        [MaxLength(160)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(StorefrontContractValidation.EmailMaxLength)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? FirstName { get; set; }

        [MaxLength(120)]
        public string? LastName { get; set; }

        [MaxLength(200)]
        public string? Company { get; set; }

        [Phone]
        [MaxLength(32)]
        public string? PhoneNumber { get; set; }

        [StringLength(16, MinimumLength = 2)]
        public string? PreferredLanguage { get; set; }

        [StringLength(3, MinimumLength = 3)]
        public string? PreferredCurrencyCode { get; set; }
    }
}
