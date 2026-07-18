namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class StoreEmailSettingsService : IStoreEmailSettingsService
    {
        private readonly CommerceNodeDbContext context;
        private readonly IStoreEmailSecretProtector secretProtector;

        public StoreEmailSettingsService(
            CommerceNodeDbContext context,
            IStoreEmailSecretProtector secretProtector)
        {
            this.context = context;
            this.secretProtector = secretProtector;
        }

        public async Task<ServiceResponse<StoreEmailSettingsResponse>> GetAsync(
            Guid storeId,
            CancellationToken cancellationToken = default)
        {
            if (storeId == Guid.Empty)
            {
                return new ServiceResponse<StoreEmailSettingsResponse>(
                    false,
                    "Store is required.");
            }

            var storeExists = await this.context.CommerceStores
                .AsNoTracking()
                .AnyAsync(store => store.Id == storeId, cancellationToken);
            if (!storeExists)
            {
                return new ServiceResponse<StoreEmailSettingsResponse>(
                    false,
                    "Store was not found.");
            }

            var settings = await this.context.StoreEmailSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.StoreId == storeId, cancellationToken);
            return new ServiceResponse<StoreEmailSettingsResponse>(
                true,
                "Store email settings loaded.")
            {
                Payload = settings is null ? CreateDefaultResponse(storeId) : ToResponse(settings),
            };
        }

        public async Task<ServiceResponse<StoreEmailSettingsResponse>> UpdateAsync(
            Guid storeId,
            UpdateStoreEmailSettingsRequest request,
            string? updatedByUserId,
            bool captureModeAllowed,
            CancellationToken cancellationToken = default)
        {
            if (storeId == Guid.Empty)
            {
                return new ServiceResponse<StoreEmailSettingsResponse>(
                    false,
                    "Store is required.");
            }

            var storeExists = await this.context.CommerceStores
                .AnyAsync(store => store.Id == storeId, cancellationToken);
            if (!storeExists)
            {
                return new ServiceResponse<StoreEmailSettingsResponse>(
                    false,
                    "Store was not found.");
            }

            var settings = await this.context.StoreEmailSettings
                .FirstOrDefaultAsync(item => item.StoreId == storeId, cancellationToken);
            var existingSecretConfigured = !string.IsNullOrWhiteSpace(settings?.ProtectedPassword);
            var validation = StoreEmailSettingsRequestValidator.Validate(
                request,
                new StoreEmailSettingsValidationContext(existingSecretConfigured, captureModeAllowed));
            if (!validation.Success)
            {
                return new ServiceResponse<StoreEmailSettingsResponse>(
                    false,
                    string.Join(" ", validation.Errors));
            }

            var now = DateTimeOffset.UtcNow;
            if (settings is null)
            {
                settings = new StoreEmailSettings
                {
                    StoreId = storeId,
                    CreatedAtUtc = now,
                };
                this.context.StoreEmailSettings.Add(settings);
            }

            settings.Enabled = request.Enabled;
            settings.SmtpHost = NormalizeOptional(request.SmtpHost);
            settings.SmtpPort = request.SmtpPort;
            settings.UseSsl = request.UseSsl;
            settings.Username = NormalizeOptional(request.Username);
            settings.FromEmail = NormalizeOptional(request.FromEmail);
            settings.FromDisplayName = NormalizeOptional(request.FromDisplayName);
            settings.ReplyToEmail = NormalizeOptional(request.ReplyToEmail);
            settings.DeliveryMode = NormalizeDeliveryMode(request.DeliveryMode);
            settings.CaptureRedirectToEmail = NormalizeOptional(request.CaptureRedirectToEmail);
            settings.UpdatedByUserId = NormalizeOptional(updatedByUserId);
            settings.UpdatedAtUtc = now;

            if (request.ClearPassword)
            {
                settings.ProtectedPassword = null;
                settings.PasswordUpdatedAtUtc = null;
            }
            else if (!string.IsNullOrWhiteSpace(request.Password))
            {
                settings.ProtectedPassword = this.secretProtector.Protect(request.Password.Trim());
                settings.PasswordUpdatedAtUtc = now;
            }

            await this.context.SaveChangesAsync(cancellationToken);

            return new ServiceResponse<StoreEmailSettingsResponse>(
                true,
                "Store email settings updated.")
            {
                Payload = ToResponse(settings),
            };
        }

        private static StoreEmailSettingsResponse CreateDefaultResponse(Guid storeId)
        {
            var now = DateTimeOffset.UtcNow;
            return new StoreEmailSettingsResponse(
                Guid.Empty,
                storeId,
                false,
                null,
                587,
                true,
                null,
                null,
                null,
                null,
                StoreEmailDeliveryModes.Smtp,
                null,
                false,
                null,
                now,
                now,
                null);
        }

        private static StoreEmailSettingsResponse ToResponse(StoreEmailSettings settings)
        {
            return new StoreEmailSettingsResponse(
                settings.PublicId,
                settings.StoreId,
                settings.Enabled,
                settings.SmtpHost,
                settings.SmtpPort,
                settings.UseSsl,
                settings.Username,
                settings.FromEmail,
                settings.FromDisplayName,
                settings.ReplyToEmail,
                settings.DeliveryMode,
                settings.CaptureRedirectToEmail,
                !string.IsNullOrWhiteSpace(settings.ProtectedPassword),
                settings.PasswordUpdatedAtUtc,
                settings.CreatedAtUtc,
                settings.UpdatedAtUtc,
                settings.UpdatedByUserId);
        }

        private static string NormalizeDeliveryMode(string? value)
        {
            var normalized = NormalizeOptional(value)?.ToLowerInvariant();
            return StoreEmailDeliveryModes.All.Contains(normalized ?? string.Empty)
                ? normalized!
                : StoreEmailDeliveryModes.Smtp;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
