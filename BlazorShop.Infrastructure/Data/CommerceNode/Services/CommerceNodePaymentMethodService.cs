namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.Settings;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Application.Services.Contracts.Payment;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodePaymentMethodService : IPaymentMethodService, IStorePaymentMethodAdminService
    {
        private static readonly StorePaymentMethodSeed[] DefaultMethods =
        [
            new(PaymentMethodKeys.Cod, "Cash on Delivery", "Test checkout payment method for MVP.", true, 10),
            new(PaymentMethodKeys.Stripe, "Stripe", "Card payments through Stripe.", false, 20),
            new(PaymentMethodKeys.PayPal, "PayPal", "PayPal payment skeleton.", false, 30),
        ];

        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly IAdminAuditService auditService;
        private readonly IStorefrontPublicConfigurationCache publicConfigurationCache;
        private readonly IPaymentProviderCapabilityRegistry capabilityRegistry;

        public CommerceNodePaymentMethodService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            IAdminAuditService auditService,
            IStorefrontPublicConfigurationCache publicConfigurationCache,
            IPaymentProviderCapabilityRegistry capabilityRegistry)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.auditService = auditService;
            this.publicConfigurationCache = publicConfigurationCache;
            this.capabilityRegistry = capabilityRegistry;
        }

        public async Task<IEnumerable<GetPaymentMethod>> GetPaymentMethodsAsync()
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync();
            if (!storeResult.Success)
            {
                return [];
            }

            await this.EnsureDefaultsAsync(storeResult.Payload);
            var storeMethods = await this.context.StorePaymentMethods
                .AsNoTracking()
                .Where(method => method.StoreId == storeResult.Payload && method.Enabled)
                .OrderBy(method => method.DisplayOrder)
                .ThenBy(method => method.DisplayName)
                .ToListAsync();

            if (storeMethods.Count == 0)
            {
                return [];
            }

            var keys = storeMethods.Select(method => method.PaymentMethodKey).ToArray();
            var catalog = await this.context.PaymentMethods
                .AsNoTracking()
                .Where(method => keys.Contains(method.Key))
                .ToDictionaryAsync(method => method.Key, StringComparer.OrdinalIgnoreCase);

            return storeMethods.Select(method =>
            {
                catalog.TryGetValue(method.PaymentMethodKey, out var paymentMethod);
                return new GetPaymentMethod
                {
                    Id = paymentMethod?.Id ?? method.Id,
                    Key = method.PaymentMethodKey,
                    Name = method.DisplayName,
                    Description = method.Description ?? paymentMethod?.Description,
                    ShortDisplayText = method.ShortDisplayText,
                    IconUrl = method.IconUrl,
                    SupportedCurrencyCodes = ParseCodes(method.SupportedCurrencyCodesJson),
                    SupportedCountryCodes = ParseCodes(method.SupportedCountryCodesJson),
                };
            });
        }

        public async Task<IReadOnlyList<StorePaymentMethodDto>> GetAsync(CancellationToken cancellationToken = default)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return [];
            }

            await this.EnsureDefaultsAsync(storeResult.Payload, cancellationToken);
            var methods = await this.context.StorePaymentMethods
                .AsNoTracking()
                .Where(method => method.StoreId == storeResult.Payload)
                .OrderBy(method => method.DisplayOrder)
                .ThenBy(method => method.DisplayName)
                .ToListAsync(cancellationToken);

            return methods.Select(this.Map).ToList();
        }

        public async Task<ServiceResponse<StorePaymentMethodDto>> UpdateAsync(
            string paymentMethodKey,
            UpdateStorePaymentMethodRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var key = NormalizeKey(paymentMethodKey);
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return Failure("Current store could not be resolved.", ServiceResponseType.NotFound);
            }

            await this.EnsureDefaultsAsync(storeResult.Payload, cancellationToken);
            var method = await this.context.StorePaymentMethods
                .FirstOrDefaultAsync(
                    candidate => candidate.StoreId == storeResult.Payload && candidate.PaymentMethodKey == key,
                    cancellationToken);

            var capabilityResult = this.capabilityRegistry.Get(key);
            if (!capabilityResult.Success || capabilityResult.Payload is null)
            {
                if (method is null)
                {
                    return Failure("Payment method key is not supported.", ServiceResponseType.ValidationError);
                }

                if (request.Enabled)
                {
                    return Failure("Payment provider is not installed or active.", ServiceResponseType.ValidationError);
                }
            }

            var capability = capabilityResult.Payload ?? CreateUnsupportedCapability(key);
            if (request.Enabled && (!capability.Installed || !capability.Active))
            {
                return Failure("Payment provider is not installed or active.", ServiceResponseType.ValidationError);
            }

            var validationMessage = Validate(request);
            if (validationMessage is not null)
            {
                return Failure(validationMessage, ServiceResponseType.ValidationError);
            }

            if (method is null)
            {
                return Failure("Payment method configuration was not found.", ServiceResponseType.NotFound);
            }

            var effectiveMinOrderTotal = request.MinOrderTotal ?? method.MinOrderTotal;
            var effectiveMaxOrderTotal = request.MaxOrderTotal ?? method.MaxOrderTotal;
            if (effectiveMinOrderTotal.HasValue
                && effectiveMaxOrderTotal.HasValue
                && effectiveMinOrderTotal.Value > effectiveMaxOrderTotal.Value)
            {
                return Failure(
                    "Payment minimum order total must be less than or equal to maximum order total.",
                    ServiceResponseType.ValidationError);
            }

            method.Enabled = request.Enabled;
            method.DisplayName = request.DisplayName.Trim();
            method.Description = NormalizeNullable(request.Description);
            method.DisplayOrder = request.DisplayOrder;
            if (request.ShortDisplayText is not null)
            {
                method.ShortDisplayText = NormalizeNullable(request.ShortDisplayText);
            }

            if (request.IconUrl is not null)
            {
                method.IconUrl = NormalizeNullable(request.IconUrl);
            }

            if (request.SupportedCurrencyCodes is not null)
            {
                method.SupportedCurrencyCodesJson = SerializeCodes(request.SupportedCurrencyCodes);
            }

            if (request.SupportedCountryCodes is not null)
            {
                method.SupportedCountryCodesJson = SerializeCodes(request.SupportedCountryCodes);
            }

            if (request.MinOrderTotal.HasValue)
            {
                method.MinOrderTotal = request.MinOrderTotal.Value;
            }

            if (request.MaxOrderTotal.HasValue)
            {
                method.MaxOrderTotal = request.MaxOrderTotal.Value;
            }

            if (request.ClearSettings)
            {
                method.SettingsJson = null;
            }
            else if (request.SettingsJson is not null)
            {
                method.SettingsJson = NormalizeNullable(request.SettingsJson);
            }

            method.UpdatedAt = DateTime.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken);
            var dto = this.Map(method);

            await this.auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = "PaymentMethod.Updated",
                EntityType = "StorePaymentMethod",
                EntityId = method.Id.ToString(),
                Summary = $"Payment method '{method.PaymentMethodKey}' updated.",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    method.StoreId,
                    method.PaymentMethodKey,
                    method.Enabled,
                    method.DisplayOrder,
                    SettingsConfigured = method.SettingsJson is not null,
                    SettingsChanged = request.ClearSettings || request.SettingsJson is not null,
                    PublicMetadataChanged = request.ShortDisplayText is not null
                        || request.IconUrl is not null
                        || request.SupportedCurrencyCodes is not null
                        || request.SupportedCountryCodes is not null
                        || request.MinOrderTotal.HasValue
                        || request.MaxOrderTotal.HasValue,
                }),
            });
            await this.publicConfigurationCache.InvalidateAsync(storeResult.Payload, cancellationToken);

            return Success(dto, "Payment method updated successfully.");
        }

        private async Task EnsureDefaultsAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            var existingKeys = await this.context.StorePaymentMethods
                .Where(method => method.StoreId == storeId)
                .Select(method => method.PaymentMethodKey)
                .ToArrayAsync(cancellationToken);

            var existing = new HashSet<string>(existingKeys, StringComparer.OrdinalIgnoreCase);
            foreach (var seed in DefaultMethods)
            {
                if (existing.Contains(seed.Key))
                {
                    continue;
                }

                this.context.StorePaymentMethods.Add(new StorePaymentMethod
                {
                    StoreId = storeId,
                    PaymentMethodKey = seed.Key,
                    Enabled = seed.Enabled,
                    DisplayName = seed.DisplayName,
                    Description = seed.Description,
                    DisplayOrder = seed.DisplayOrder,
                });
            }

            await this.context.SaveChangesAsync(cancellationToken);
        }

        private StorePaymentMethodDto Map(StorePaymentMethod method)
        {
            var capability = this.capabilityRegistry.Get(method.PaymentMethodKey).Payload ?? CreateUnsupportedCapability(method.PaymentMethodKey);
            return Map(method, capability);
        }

        private static StorePaymentMethodDto Map(StorePaymentMethod method, PaymentProviderCapabilityDto capability)
        {
            return new StorePaymentMethodDto(
                method.Id,
                method.PaymentMethodKey,
                method.DisplayName,
                method.Description,
                method.Enabled,
                method.DisplayOrder,
                method.ShortDisplayText,
                method.IconUrl,
                ParseCodes(method.SupportedCurrencyCodesJson),
                ParseCodes(method.SupportedCountryCodesJson),
                method.MinOrderTotal,
                method.MaxOrderTotal,
                new StorePaymentMethodCapabilityDto(
                    capability.Installed,
                    capability.Active,
                    capability.MethodType,
                    capability.RecurringCapable,
                    capability.SupportsAuthorize,
                    capability.SupportsCapture,
                    capability.SupportsVoid,
                    capability.SupportsRefund,
                    capability.SupportsPartialRefund,
                    capability.RequiresWebhookSignature),
                new StorePaymentMethodSettingsStatusDto(method.SettingsJson is not null),
                method.CreatedAt,
                method.UpdatedAt);
        }

        private static string? Validate(UpdateStorePaymentMethodRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return "Payment display name is required.";
            }

            if (request.DisplayName.Trim().Length > 160)
            {
                return "Payment display name must be 160 characters or fewer.";
            }

            if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > 500)
            {
                return "Payment description must be 500 characters or fewer.";
            }

            if (request.DisplayOrder < 0 || request.DisplayOrder > 10000)
            {
                return "Payment display order must be between 0 and 10000.";
            }

            if (request.ShortDisplayText?.Trim().Length > 160)
            {
                return "Payment short display text must be 160 characters or fewer.";
            }

            if (request.IconUrl?.Trim().Length > 1024)
            {
                return "Payment icon URL must be 1024 characters or fewer.";
            }

            var currencyValidation = ValidateCodes(request.SupportedCurrencyCodes, "currency", expectedLength: 3);
            if (currencyValidation is not null)
            {
                return currencyValidation;
            }

            var countryValidation = ValidateCodes(request.SupportedCountryCodes, "country", expectedLength: 2);
            if (countryValidation is not null)
            {
                return countryValidation;
            }

            if (request.MinOrderTotal is < 0m || request.MaxOrderTotal is < 0m)
            {
                return "Payment order total limits must be zero or greater.";
            }

            if (request.MinOrderTotal.HasValue
                && request.MaxOrderTotal.HasValue
                && request.MinOrderTotal.Value > request.MaxOrderTotal.Value)
            {
                return "Payment minimum order total must be less than or equal to maximum order total.";
            }

            if (request.ClearSettings && !string.IsNullOrWhiteSpace(request.SettingsJson))
            {
                return "Payment settings cannot be cleared and replaced in the same request.";
            }

            if (request.SettingsJson is not null)
            {
                if (string.IsNullOrWhiteSpace(request.SettingsJson))
                {
                    return "Payment settings JSON must not be blank. Use clearSettings to remove saved settings.";
                }

                try
                {
                    using var _ = JsonDocument.Parse(request.SettingsJson);
                }
                catch (JsonException)
                {
                    return "Payment settings JSON is invalid.";
                }
            }

            return null;
        }

        private static string NormalizeKey(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static IReadOnlyList<string> ParseCodes(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<IReadOnlyList<string>>(json) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static string? SerializeCodes(IReadOnlyList<string> values)
        {
            var codes = values
                .Select(NormalizeNullable)
                .Where(value => value is not null)
                .Select(value => value!.ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            return codes.Length == 0 ? null : JsonSerializer.Serialize(codes);
        }

        private static string? ValidateCodes(IReadOnlyList<string>? values, string label, int expectedLength)
        {
            if (values is null)
            {
                return null;
            }

            if (values.Count > 100)
            {
                return $"Payment {label} restrictions must contain 100 codes or fewer.";
            }

            foreach (var value in values)
            {
                var code = NormalizeNullable(value);
                if (code is null || code.Length != expectedLength || !code.All(char.IsLetter))
                {
                    return $"Payment {label} codes must be {expectedLength} letters.";
                }
            }

            return null;
        }

        private static ServiceResponse<StorePaymentMethodDto> Success(StorePaymentMethodDto payload, string message)
        {
            return new ServiceResponse<StorePaymentMethodDto>(true, message, payload.Id)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<StorePaymentMethodDto> Failure(string message, ServiceResponseType responseType)
        {
            return new ServiceResponse<StorePaymentMethodDto>(false, message)
            {
                ResponseType = responseType,
            };
        }

        private static PaymentProviderCapabilityDto CreateUnsupportedCapability(string systemName)
        {
            return new PaymentProviderCapabilityDto(
                systemName,
                Installed: false,
                Active: false,
                DisplayName: systemName,
                Description: null,
                IconUrl: null,
                DefaultDisplayOrder: 0,
                SupportedStoreIds: [],
                SupportedCurrencyCodes: [],
                SupportedCountryCodes: [],
                MinOrderTotal: null,
                MaxOrderTotal: null,
                MethodType: "unknown",
                RecurringCapable: false,
                SupportsAuthorize: false,
                SupportsCapture: false,
                SupportsVoid: false,
                SupportsRefund: false,
                SupportsPartialRefund: false,
                RequiresWebhookSignature: false);
        }

        private sealed record StorePaymentMethodSeed(
            string Key,
            string DisplayName,
            string Description,
            bool Enabled,
            int DisplayOrder);
    }
}
