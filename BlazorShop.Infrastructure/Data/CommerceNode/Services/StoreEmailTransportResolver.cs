namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;

    public sealed class StoreEmailTransportResolver : IStoreEmailTransportResolver
    {
        private readonly CommerceNodeDbContext context;
        private readonly IStoreEmailSecretProtector secretProtector;
        private readonly EmailSettings globalEmailSettings;
        private readonly StoreEmailTransportOptions options;

        public StoreEmailTransportResolver(
            CommerceNodeDbContext context,
            IStoreEmailSecretProtector secretProtector,
            IOptions<EmailSettings> globalEmailSettings,
            IOptions<StoreEmailTransportOptions> options)
        {
            this.context = context;
            this.secretProtector = secretProtector;
            this.globalEmailSettings = globalEmailSettings.Value;
            this.options = options.Value;
        }

        public async Task<StoreEmailSenderProfile> ResolveSenderProfileAsync(
            Guid storeId,
            CancellationToken cancellationToken = default)
        {
            var settings = await this.context.StoreEmailSettings
                .AsNoTracking()
                .Where(item => item.StoreId == storeId)
                .Select(item => new
                {
                    item.FromEmail,
                    item.FromDisplayName,
                    item.ReplyToEmail,
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(settings?.FromEmail))
            {
                return new StoreEmailSenderProfile(
                    settings.FromEmail.Trim(),
                    NormalizeOptional(settings.FromDisplayName),
                    NormalizeOptional(settings.ReplyToEmail),
                    FromStoreSettings: true);
            }

            if (this.options.AllowGlobalEmailSettingsFallback
                && !string.IsNullOrWhiteSpace(this.globalEmailSettings.From))
            {
                return new StoreEmailSenderProfile(
                    this.globalEmailSettings.From.Trim(),
                    NormalizeOptional(this.globalEmailSettings.DisplayName),
                    null,
                    FromStoreSettings: false);
            }

            var storeSupportEmail = await this.context.CommerceStores
                .AsNoTracking()
                .Where(store => store.Id == storeId)
                .Select(store => store.SupportEmail ?? store.CompanyEmail)
                .FirstOrDefaultAsync(cancellationToken);

            return new StoreEmailSenderProfile(
                string.IsNullOrWhiteSpace(storeSupportEmail) ? "no-reply@invalid.local" : storeSupportEmail.Trim(),
                null,
                null,
                FromStoreSettings: false);
        }

        public async Task<StoreEmailTransportResolutionResult> ResolveTransportAsync(
            Guid storeId,
            CancellationToken cancellationToken = default)
        {
            if (storeId == Guid.Empty)
            {
                return NotConfigured("Store is required.");
            }

            var settings = await this.context.StoreEmailSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.StoreId == storeId, cancellationToken);
            if (settings is not null && settings.Enabled)
            {
                var transport = TryCreateStoreTransport(settings);
                if (transport is not null)
                {
                    return new StoreEmailTransportResolutionResult(true, transport);
                }
            }

            if (this.options.AllowGlobalEmailSettingsFallback
                && HasGlobalTransport())
            {
                return new StoreEmailTransportResolutionResult(
                    true,
                    new StoreEmailTransportSettings(
                        storeId,
                        StoreEmailDeliveryModes.Smtp,
                        this.globalEmailSettings.From.Trim(),
                        NormalizeOptional(this.globalEmailSettings.DisplayName),
                        null,
                        this.globalEmailSettings.SmtpServer.Trim(),
                        this.globalEmailSettings.Port,
                        this.globalEmailSettings.UseSsl,
                        NormalizeOptional(this.globalEmailSettings.Username),
                        NormalizeOptional(this.globalEmailSettings.Password)));
            }

            return NotConfigured("Store SMTP transport is not configured.");
        }

        private StoreEmailTransportSettings? TryCreateStoreTransport(StoreEmailSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.FromEmail)
                || string.IsNullOrWhiteSpace(settings.SmtpHost)
                || settings.SmtpPort is < 1 or > 65535
                || !StoreEmailDeliveryModes.All.Contains(settings.DeliveryMode))
            {
                return null;
            }

            string? password = null;
            if (!string.IsNullOrWhiteSpace(settings.ProtectedPassword))
            {
                password = this.secretProtector.Unprotect(settings.ProtectedPassword);
            }

            if (settings.DeliveryMode == StoreEmailDeliveryModes.Smtp
                && string.IsNullOrWhiteSpace(password)
                && !string.IsNullOrWhiteSpace(settings.Username))
            {
                return null;
            }

            return new StoreEmailTransportSettings(
                settings.StoreId,
                settings.DeliveryMode,
                settings.FromEmail.Trim(),
                NormalizeOptional(settings.FromDisplayName),
                NormalizeOptional(settings.ReplyToEmail),
                settings.SmtpHost.Trim(),
                settings.SmtpPort,
                settings.UseSsl,
                NormalizeOptional(settings.Username),
                password);
        }

        private bool HasGlobalTransport()
        {
            return !string.IsNullOrWhiteSpace(this.globalEmailSettings.From)
                && !string.IsNullOrWhiteSpace(this.globalEmailSettings.SmtpServer)
                && this.globalEmailSettings.Port is >= 1 and <= 65535;
        }

        private static StoreEmailTransportResolutionResult NotConfigured(string message)
        {
            return new StoreEmailTransportResolutionResult(
                false,
                ErrorCode: "message_delivery.smtp_not_configured",
                Message: message);
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
