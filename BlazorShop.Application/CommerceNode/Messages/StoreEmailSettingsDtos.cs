namespace BlazorShop.Application.CommerceNode.Messages
{
    using System.ComponentModel.DataAnnotations;
    using System.Net.Mail;

    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities.CommerceNode;

    public sealed record StoreEmailSettingsResponse(
        Guid PublicId,
        Guid StoreId,
        bool Enabled,
        string? SmtpHost,
        int SmtpPort,
        bool UseSsl,
        string? Username,
        string? FromEmail,
        string? FromDisplayName,
        string? ReplyToEmail,
        string DeliveryMode,
        string? CaptureRedirectToEmail,
        bool SecretsConfigured,
        DateTimeOffset? PasswordUpdatedAtUtc,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc,
        string? UpdatedByUserId);

    public sealed class UpdateStoreEmailSettingsRequest
    {
        public bool Enabled { get; set; }

        [MaxLength(253)]
        public string? SmtpHost { get; set; }

        [Range(1, 65535)]
        public int SmtpPort { get; set; } = 587;

        public bool UseSsl { get; set; } = true;

        [MaxLength(320)]
        public string? Username { get; set; }

        [MinLength(8)]
        [MaxLength(1024)]
        public string? Password { get; set; }

        public bool ClearPassword { get; set; }

        public bool UseExistingPassword { get; set; } = true;

        [EmailAddress]
        [MaxLength(254)]
        public string? FromEmail { get; set; }

        [MaxLength(160)]
        public string? FromDisplayName { get; set; }

        [EmailAddress]
        [MaxLength(254)]
        public string? ReplyToEmail { get; set; }

        [Required]
        [MaxLength(32)]
        public string DeliveryMode { get; set; } = StoreEmailDeliveryModes.Smtp;

        [EmailAddress]
        [MaxLength(254)]
        public string? CaptureRedirectToEmail { get; set; }
    }

    public sealed record StoreEmailSettingsValidationContext(
        bool ExistingSecretConfigured = false,
        bool CaptureModeAllowed = false);

    public sealed record StoreEmailSettingsValidationResult(
        bool Success,
        IReadOnlyList<string> Errors)
    {
        public static StoreEmailSettingsValidationResult Valid { get; } = new(true, Array.Empty<string>());
    }

    public static class StoreEmailSettingsRequestValidator
    {
        public static StoreEmailSettingsValidationResult Validate(
            UpdateStoreEmailSettingsRequest request,
            StoreEmailSettingsValidationContext context)
        {
            ArgumentNullException.ThrowIfNull(request);

            var errors = new List<string>();
            var annotationResults = new List<ValidationResult>();
            Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                annotationResults,
                validateAllProperties: true);
            errors.AddRange(annotationResults.Select(result => result.ErrorMessage ?? "Store email settings are invalid."));

            var deliveryMode = Normalize(request.DeliveryMode);
            if (deliveryMode is null || !StoreEmailDeliveryModes.All.Contains(deliveryMode))
            {
                errors.Add("Delivery mode must be smtp or capture.");
            }

            if (deliveryMode == StoreEmailDeliveryModes.Capture && !context.CaptureModeAllowed)
            {
                errors.Add("Capture delivery mode is not allowed in this environment.");
            }

            if (request.ClearPassword && !string.IsNullOrWhiteSpace(request.Password))
            {
                errors.Add("Password cannot be provided when ClearPassword is true.");
            }

            if (request.Enabled)
            {
                if (deliveryMode == StoreEmailDeliveryModes.Smtp)
                {
                    Require(request.SmtpHost, "SMTP host is required when store email is enabled.", errors);
                    Require(request.FromEmail, "From email is required when store email is enabled.", errors);

                    var hasUsableSecret = !request.ClearPassword
                        && (!string.IsNullOrWhiteSpace(request.Password)
                            || (request.UseExistingPassword && context.ExistingSecretConfigured));
                    if (!hasUsableSecret)
                    {
                        errors.Add("SMTP password is required when SMTP delivery is enabled.");
                    }
                }
                else if (deliveryMode == StoreEmailDeliveryModes.Capture)
                {
                    Require(request.FromEmail, "From email is required when capture delivery is enabled.", errors);
                }
            }

            ValidateEmail(request.FromEmail, "From email is not valid.", errors);
            ValidateEmail(request.ReplyToEmail, "Reply-to email is not valid.", errors);
            ValidateEmail(request.CaptureRedirectToEmail, "Capture redirect email is not valid.", errors);

            return errors.Count == 0
                ? StoreEmailSettingsValidationResult.Valid
                : new StoreEmailSettingsValidationResult(false, errors.Distinct(StringComparer.Ordinal).ToArray());
        }

        private static void Require(string? value, string message, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(message);
            }
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
        }

        private static void ValidateEmail(string? value, string message, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            try
            {
                _ = new MailAddress(value.Trim());
            }
            catch (FormatException)
            {
                errors.Add(message);
            }
        }
    }

    public interface IStoreEmailSecretProtector
    {
        string Protect(string secret);

        string Unprotect(string protectedSecret);
    }

    public interface IStoreEmailSettingsService
    {
        Task<ServiceResponse<StoreEmailSettingsResponse>> GetAsync(
            Guid storeId,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreEmailSettingsResponse>> UpdateAsync(
            Guid storeId,
            UpdateStoreEmailSettingsRequest request,
            string? updatedByUserId,
            bool captureModeAllowed,
            CancellationToken cancellationToken = default);
    }
}
