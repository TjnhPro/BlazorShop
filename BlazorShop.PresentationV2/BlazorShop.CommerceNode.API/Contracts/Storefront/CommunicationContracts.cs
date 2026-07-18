namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    using Microsoft.AspNetCore.Mvc;

    public sealed class StorefrontNewsletterSubscribeRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(StorefrontContractValidation.EmailMaxLength)]
        public string Email { get; set; } = string.Empty;

        public bool MarketingConsentAccepted { get; set; }

        [MaxLength(4096)]
        public string? CaptchaToken { get; set; }
    }

    public sealed class StorefrontContactRequest
    {
        [Required]
        [MaxLength(160)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(StorefrontContractValidation.EmailMaxLength)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(4096)]
        public string? CaptchaToken { get; set; }
    }

    public sealed record StorefrontContactResponse(
        bool Accepted,
        string Message);
}
