namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Globalization;
    using System.Net.Mail;
    using System.Runtime.InteropServices;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.DTOs.Admin.Settings;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    public sealed class CommerceNodeAdminSettingsService : IAdminSettingsService
    {
        private static readonly Regex CurrencyRegex = new("^[A-Z]{3}$", RegexOptions.Compiled);
        private static readonly Regex PrefixRegex = new("^[A-Za-z0-9-]{1,16}$", RegexOptions.Compiled);
        private static readonly HashSet<string> ShippingStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "PendingShipment",
            "Shipped",
            "InTransit",
            "OutForDelivery",
            "Delivered",
        };

        private readonly CommerceNodeDbContext context;
        private readonly EmailSettings emailSettings;
        private readonly IHostEnvironment environment;
        private readonly ICommerceNodeAuditActorAccessor actorAccessor;
        private readonly IAdminAuditService auditService;
        private readonly IMemoryCache memoryCache;

        public CommerceNodeAdminSettingsService(
            CommerceNodeDbContext context,
            IOptions<EmailSettings> emailSettings,
            IHostEnvironment environment,
            ICommerceNodeAuditActorAccessor actorAccessor,
            IAdminAuditService auditService,
            IMemoryCache memoryCache)
        {
            this.context = context;
            this.emailSettings = emailSettings.Value;
            this.environment = environment;
            this.actorAccessor = actorAccessor;
            this.auditService = auditService;
            this.memoryCache = memoryCache;
        }

        public async Task<AdminSettingsDto> GetAsync()
        {
            var settings = await this.GetOrCreateAsync();
            var store = await this.GetSettingsStoreAsync();
            return this.Map(settings, store);
        }

        public async Task<ServiceResponse<StoreSettingsDto>> UpdateStoreAsync(UpdateStoreSettingsDto request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var validationMessage = ValidateStore(request);
            if (validationMessage is not null)
            {
                return Failure<StoreSettingsDto>(validationMessage, ServiceResponseType.ValidationError);
            }

            var settings = await this.GetOrCreateAsync();
            var store = await this.GetOrCreateSettingsStoreAsync(settings);
            store.Name = request.StoreName.Trim();
            store.SupportEmail = NormalizeNullable(request.StoreSupportEmail);
            store.SupportPhone = NormalizeNullable(request.StoreSupportPhone);
            store.DefaultCurrencyCode = request.DefaultCurrency.Trim().ToUpperInvariant();
            store.DefaultCulture = request.DefaultCulture.Trim();
            store.MaintenanceModeEnabled = request.MaintenanceModeEnabled;
            store.MaintenanceMessage = NormalizeNullable(request.MaintenanceMessage);
            store.UpdatedAt = DateTimeOffset.UtcNow;
            this.Touch(settings);

            await this.context.SaveChangesAsync();
            this.InvalidateStoreCache(store);
            await this.LogAsync("CommerceStore.SettingsUpdated", "Store settings updated.", settings, store);

            return Success(this.MapStore(settings, store), "Store settings updated successfully.");
        }

        public async Task<ServiceResponse<OrderSettingsDto>> UpdateOrdersAsync(UpdateOrderSettingsDto request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var validationMessage = ValidateOrders(request);
            if (validationMessage is not null)
            {
                return Failure<OrderSettingsDto>(validationMessage, ServiceResponseType.ValidationError);
            }

            var settings = await this.GetOrCreateAsync();
            settings.AllowGuestCheckout = false;
            settings.DefaultShippingStatus = request.DefaultShippingStatus.Trim();
            settings.AutoConfirmPaidOrders = request.AutoConfirmPaidOrders;
            settings.OrderReferencePrefix = request.OrderReferencePrefix.Trim().ToUpperInvariant();
            this.Touch(settings);

            await this.context.SaveChangesAsync();
            await this.LogAsync("AdminSettings.OrdersUpdated", "Order settings updated.", settings);

            return Success(this.MapOrders(settings), "Order settings updated successfully.");
        }

        public async Task<ServiceResponse<NotificationSettingsDto>> UpdateNotificationsAsync(UpdateNotificationSettingsDto request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var validationMessage = ValidateNotifications(request);
            if (validationMessage is not null)
            {
                return Failure<NotificationSettingsDto>(validationMessage, ServiceResponseType.ValidationError);
            }

            var settings = await this.GetOrCreateAsync();
            settings.SmtpHost = Normalize(request.SmtpHost);
            settings.SmtpFromEmail = Normalize(request.SmtpFromEmail);
            settings.SmtpFromDisplayName = Normalize(request.SmtpFromDisplayName);
            this.Touch(settings);

            await this.context.SaveChangesAsync();
            await this.LogAsync("AdminSettings.NotificationsUpdated", "Notification settings updated.", settings);

            return Success(this.MapNotifications(settings), "Notification settings updated successfully.");
        }

        private async Task<AdminSettings> GetOrCreateAsync()
        {
            var settings = await this.context.AdminSettings.FirstOrDefaultAsync();
            if (settings is not null)
            {
                return settings;
            }

            settings = new AdminSettings
            {
                SmtpHost = this.emailSettings.SmtpServer,
                SmtpFromEmail = this.emailSettings.From,
                SmtpFromDisplayName = this.emailSettings.DisplayName,
                UpdatedOn = DateTime.UtcNow,
            };

            this.context.AdminSettings.Add(settings);
            await this.context.SaveChangesAsync();

            return settings;
        }

        private async Task<CommerceStore?> GetSettingsStoreAsync()
        {
            var defaultStore = await this.context.CommerceStores
                .AsNoTracking()
                .Include(store => store.Domains)
                .Where(store => store.ArchivedAt == null && store.StoreKey == "default")
                .FirstOrDefaultAsync();
            if (defaultStore is not null)
            {
                return defaultStore;
            }

            var activeStores = await this.context.CommerceStores
                .AsNoTracking()
                .Include(store => store.Domains)
                .Where(store => store.ArchivedAt == null && store.Status == CommerceStoreStatuses.Active)
                .Take(2)
                .ToListAsync();

            return activeStores.Count == 1 ? activeStores[0] : null;
        }

        private async Task<CommerceStore> GetOrCreateSettingsStoreAsync(AdminSettings settings)
        {
            var store = await this.context.CommerceStores
                .Include(item => item.Domains)
                .Where(item => item.ArchivedAt == null && item.StoreKey == "default")
                .FirstOrDefaultAsync();
            if (store is not null)
            {
                return store;
            }

            var now = DateTimeOffset.UtcNow;
            store = new CommerceStore
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreKey = "default",
                Name = string.IsNullOrWhiteSpace(settings.StoreName) ? "BlazorShop" : settings.StoreName.Trim(),
                Status = CommerceStoreStatuses.Active,
                DefaultCurrencyCode = string.IsNullOrWhiteSpace(settings.DefaultCurrency) ? "USD" : settings.DefaultCurrency.Trim().ToUpperInvariant(),
                DefaultCulture = string.IsNullOrWhiteSpace(settings.DefaultCulture) ? "en-US" : settings.DefaultCulture.Trim(),
                SupportEmail = NormalizeNullable(settings.StoreSupportEmail),
                SupportPhone = NormalizeNullable(settings.StoreSupportPhone),
                MaintenanceModeEnabled = settings.MaintenanceModeEnabled,
                MaintenanceMessage = NormalizeNullable(settings.MaintenanceMessage),
                CreatedAt = now,
                UpdatedAt = now,
            };

            this.context.CommerceStores.Add(store);
            return store;
        }

        private AdminSettingsDto Map(AdminSettings settings, CommerceStore? store)
        {
            return new AdminSettingsDto
            {
                Store = this.MapStore(settings, store),
                Orders = this.MapOrders(settings),
                Notifications = this.MapNotifications(settings),
                System = new SystemSettingsDto
                {
                    UpdatedOn = settings.UpdatedOn,
                    UpdatedByUserId = settings.UpdatedByUserId,
                    RuntimeEnvironment = this.environment.EnvironmentName,
                    FrameworkDescription = RuntimeInformation.FrameworkDescription,
                },
            };
        }

        private StoreSettingsDto MapStore(AdminSettings settings, CommerceStore? store)
        {
            if (store is not null)
            {
                return new StoreSettingsDto
                {
                    StoreName = store.Name,
                    StoreSupportEmail = store.SupportEmail ?? string.Empty,
                    StoreSupportPhone = store.SupportPhone ?? string.Empty,
                    DefaultCurrency = store.DefaultCurrencyCode,
                    DefaultCulture = store.DefaultCulture,
                    MaintenanceModeEnabled = store.MaintenanceModeEnabled,
                    MaintenanceMessage = store.MaintenanceMessage ?? string.Empty,
                };
            }

            return new StoreSettingsDto
            {
                StoreName = settings.StoreName,
                StoreSupportEmail = settings.StoreSupportEmail,
                StoreSupportPhone = settings.StoreSupportPhone,
                DefaultCurrency = settings.DefaultCurrency,
                DefaultCulture = settings.DefaultCulture,
                MaintenanceModeEnabled = settings.MaintenanceModeEnabled,
                MaintenanceMessage = settings.MaintenanceMessage,
            };
        }

        private OrderSettingsDto MapOrders(AdminSettings settings)
        {
            return new OrderSettingsDto
            {
                AllowGuestCheckout = false,
                GuestCheckoutSupported = false,
                DefaultShippingStatus = settings.DefaultShippingStatus,
                AutoConfirmPaidOrders = settings.AutoConfirmPaidOrders,
                OrderReferencePrefix = settings.OrderReferencePrefix,
            };
        }

        private NotificationSettingsDto MapNotifications(AdminSettings settings)
        {
            return new NotificationSettingsDto
            {
                SmtpHost = string.IsNullOrWhiteSpace(settings.SmtpHost) ? this.emailSettings.SmtpServer : settings.SmtpHost,
                SmtpFromEmail = string.IsNullOrWhiteSpace(settings.SmtpFromEmail) ? this.emailSettings.From : settings.SmtpFromEmail,
                SmtpFromDisplayName = string.IsNullOrWhiteSpace(settings.SmtpFromDisplayName) ? this.emailSettings.DisplayName : settings.SmtpFromDisplayName,
                SecretsConfigured = !string.IsNullOrWhiteSpace(this.emailSettings.Password) || !string.IsNullOrWhiteSpace(this.emailSettings.Username),
            };
        }

        private void Touch(AdminSettings settings)
        {
            settings.UpdatedOn = DateTime.UtcNow;
            settings.UpdatedByUserId = this.actorAccessor.GetCurrentActor().ActorUserId;
        }

        private void InvalidateStoreCache(CommerceStore store)
        {
            this.memoryCache.Remove($"commerce-store:key:{store.StoreKey}");
            foreach (var domain in store.Domains)
            {
                this.memoryCache.Remove($"commerce-store:host:{domain.NormalizedDomain}");
            }
        }

        private async Task LogAsync(string action, string summary, AdminSettings settings, CommerceStore? store = null)
        {
            var metadata = JsonSerializer.Serialize(new
            {
                StoreKey = store?.StoreKey,
                StoreName = store?.Name ?? settings.StoreName,
                DefaultCurrency = store?.DefaultCurrencyCode ?? settings.DefaultCurrency,
                DefaultCulture = store?.DefaultCulture ?? settings.DefaultCulture,
                settings.DefaultShippingStatus,
                settings.OrderReferencePrefix,
                MaintenanceModeEnabled = store?.MaintenanceModeEnabled ?? settings.MaintenanceModeEnabled,
            });

            await this.auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = action,
                EntityType = "AdminSettings",
                EntityId = settings.Id.ToString(),
                Summary = summary,
                MetadataJson = metadata,
            });
        }

        private static string? ValidateStore(UpdateStoreSettingsDto request)
        {
            if (string.IsNullOrWhiteSpace(request.StoreName))
            {
                return "Store name is required.";
            }

            if (!string.IsNullOrWhiteSpace(request.StoreSupportEmail) && !IsEmail(request.StoreSupportEmail))
            {
                return "Store support email is invalid.";
            }

            if (string.IsNullOrWhiteSpace(request.DefaultCurrency) || !CurrencyRegex.IsMatch(request.DefaultCurrency.Trim().ToUpperInvariant()))
            {
                return "Default currency must be a three-letter ISO currency code.";
            }

            if (string.IsNullOrWhiteSpace(request.DefaultCulture))
            {
                return "Default culture is required.";
            }

            try
            {
                CultureInfo.GetCultureInfo(request.DefaultCulture.Trim());
            }
            catch (CultureNotFoundException)
            {
                return "Default culture is invalid.";
            }

            return null;
        }

        private static string? ValidateOrders(UpdateOrderSettingsDto request)
        {
            if (request.AllowGuestCheckout)
            {
                return "Guest checkout is not currently supported by this storefront.";
            }

            if (string.IsNullOrWhiteSpace(request.DefaultShippingStatus) || !ShippingStatuses.Contains(request.DefaultShippingStatus.Trim()))
            {
                return "Default shipping status is invalid.";
            }

            if (string.IsNullOrWhiteSpace(request.OrderReferencePrefix) || !PrefixRegex.IsMatch(request.OrderReferencePrefix.Trim()))
            {
                return "Order reference prefix must contain only letters, numbers, or hyphens.";
            }

            return null;
        }

        private static string? ValidateNotifications(UpdateNotificationSettingsDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.SmtpFromEmail) && !IsEmail(request.SmtpFromEmail))
            {
                return "SMTP from email is invalid.";
            }

            return null;
        }

        private static bool IsEmail(string email)
        {
            try
            {
                _ = new MailAddress(email.Trim());
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static ServiceResponse<TPayload> Success<TPayload>(TPayload payload, string message)
        {
            return new ServiceResponse<TPayload>(true, message)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<TPayload> Failure<TPayload>(string message, ServiceResponseType responseType)
        {
            return new ServiceResponse<TPayload>(false, message)
            {
                ResponseType = responseType,
            };
        }
    }
}
