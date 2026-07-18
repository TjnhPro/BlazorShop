namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    using Microsoft.AspNetCore.Mvc;

    public sealed class StorefrontRegisterRequest
    {
        [Required]
        [MaxLength(160)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(StorefrontContractValidation.EmailMaxLength)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(StorefrontContractValidation.PasswordMinLength)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [MaxLength(4096)]
        public string? CaptchaToken { get; set; }
    }

    public sealed class StorefrontLoginRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(StorefrontContractValidation.EmailMaxLength)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [MaxLength(4096)]
        public string? CaptchaToken { get; set; }
    }

    public sealed record StorefrontRegistrationPolicyResponse(
        string Mode,
        bool RegistrationAllowed,
        string Message);

    public sealed class StorefrontForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(StorefrontContractValidation.EmailMaxLength)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(4096)]
        public string? CaptchaToken { get; set; }
    }

    public sealed class StorefrontResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(StorefrontContractValidation.EmailMaxLength)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(4096)]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MinLength(StorefrontContractValidation.PasswordMinLength)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public sealed class StorefrontChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(StorefrontContractValidation.PasswordMinLength)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public sealed class StorefrontUpdateProfileRequest
    {
        [Required]
        [MaxLength(160)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(StorefrontContractValidation.EmailMaxLength)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [MaxLength(32)]
        public string? PhoneNumber { get; set; }
    }

    public sealed record StorefrontRegistrationResponse(
        Guid CustomerId);

    public sealed record StorefrontTokenResponse(
        string AccessToken,
        DateTime ExpiresAtUtc);
}
