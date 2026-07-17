namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;
    using System.Text.RegularExpressions;

    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class StoreShippingSettingsService : IStoreShippingSettingsService
    {
        private const string DefaultSurchargePolicy = StoreShippingSurchargePolicies.Sum;
        private static readonly Regex CountryCodeRegex = new("^[A-Z]{2}$", RegexOptions.Compiled);

        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly ICommerceNodeAuditActorAccessor actorAccessor;
        private readonly IAdminAuditService auditService;

        public StoreShippingSettingsService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            ICommerceNodeAuditActorAccessor actorAccessor,
            IAdminAuditService auditService)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.actorAccessor = actorAccessor;
            this.auditService = auditService;
        }

        public async Task<ServiceResponse<StoreShippingSettingsDto>> GetAsync(
            CancellationToken cancellationToken = default)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return Failure(storeResult.Message);
            }

            var settings = await this.LoadAsync(storeResult.Payload, cancellationToken);
            return Success(Map(settings), "Shipping settings loaded.");
        }

        public async Task<ServiceResponse<StoreShippingSettingsDto>> UpdateAsync(
            UpdateStoreShippingSettingsRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var validation = Validate(request);
            if (validation is not null)
            {
                return Failure(validation, ServiceResponseType.ValidationError);
            }

            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return Failure(storeResult.Message);
            }

            var now = DateTimeOffset.UtcNow;
            var settings = await this.LoadAsync(storeResult.Payload, cancellationToken);
            if (settings is null)
            {
                settings = CreateDefaultEntity(storeResult.Payload, now);
                this.context.StoreShippingSettings.Add(settings);
            }

            Apply(settings, request, now, this.actorAccessor.GetCurrentActor().ActorUserId);
            await this.context.SaveChangesAsync(cancellationToken);
            await this.LogAsync(settings, cancellationToken);

            return Success(Map(settings), "Shipping settings updated.");
        }

        public async Task<StoreShippingRuntimeSettings> ResolveAsync(
            Guid storeId,
            CancellationToken cancellationToken = default)
        {
            if (storeId == Guid.Empty)
            {
                return BuildRuntime(null);
            }

            var settings = await this.LoadAsync(storeId, cancellationToken);
            return BuildRuntime(settings);
        }

        public async Task<StoreShippingRuntimeSettings> ResolveCurrentAsync(
            CancellationToken cancellationToken = default)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return BuildRuntime(null);
            }

            return await this.ResolveAsync(storeResult.Payload, cancellationToken);
        }

        private async Task<StoreShippingSettings?> LoadAsync(Guid storeId, CancellationToken cancellationToken)
        {
            return await this.context.StoreShippingSettings
                .AsTracking()
                .FirstOrDefaultAsync(settings => settings.StoreId == storeId, cancellationToken);
        }

        private static StoreShippingSettings CreateDefaultEntity(Guid storeId, DateTimeOffset now)
        {
            return new StoreShippingSettings
            {
                StoreId = storeId,
                PublicId = Guid.NewGuid(),
                SurchargePolicy = DefaultSurchargePolicy,
                CreatedAt = now,
                UpdatedAt = now,
            };
        }

        private static StoreShippingRuntimeSettings BuildRuntime(StoreShippingSettings? settings)
        {
            return new StoreShippingRuntimeSettings(
                new StoreShippingOriginDto(
                    settings?.OriginFullName,
                    settings?.OriginCompany,
                    settings?.OriginAddress1,
                    settings?.OriginAddress2,
                    settings?.OriginCity,
                    settings?.OriginStateProvinceCode,
                    settings?.OriginPostalCode,
                    NormalizeCountry(settings?.OriginCountryCode)),
                ParseCountryCodes(settings?.EnabledCountryCodesJson),
                settings?.DefaultFlatRate,
                settings?.FreeShippingThreshold,
                NormalizeSurchargePolicy(settings?.SurchargePolicy),
                settings?.DefaultDeliveryEstimateText);
        }

        private static StoreShippingSettingsDto Map(StoreShippingSettings? settings)
        {
            var runtime = BuildRuntime(settings);
            var now = DateTimeOffset.UtcNow;
            return new StoreShippingSettingsDto(
                settings?.PublicId ?? Guid.Empty,
                runtime.Origin,
                runtime.EnabledCountryCodes,
                runtime.DefaultFlatRate,
                runtime.FreeShippingThreshold,
                runtime.SurchargePolicy,
                runtime.DefaultDeliveryEstimateText,
                settings?.CreatedAt ?? now,
                settings?.UpdatedAt ?? now,
                settings?.UpdatedByUserId);
        }

        private static void Apply(StoreShippingSettings settings, UpdateStoreShippingSettingsRequest request, DateTimeOffset now, string? actorUserId)
        {
            var origin = request.Origin;
            settings.OriginFullName = NormalizeNullable(origin.FullName);
            settings.OriginCompany = NormalizeNullable(origin.Company);
            settings.OriginAddress1 = NormalizeNullable(origin.Address1);
            settings.OriginAddress2 = NormalizeNullable(origin.Address2);
            settings.OriginCity = NormalizeNullable(origin.City);
            settings.OriginStateProvinceCode = NormalizeNullable(origin.StateProvinceCode)?.ToUpperInvariant();
            settings.OriginPostalCode = NormalizeNullable(origin.PostalCode);
            settings.OriginCountryCode = NormalizeCountry(origin.CountryCode);
            settings.EnabledCountryCodesJson = SerializeCountryCodes(request.EnabledCountryCodes);
            settings.DefaultFlatRate = request.DefaultFlatRate;
            settings.FreeShippingThreshold = request.FreeShippingThreshold;
            settings.SurchargePolicy = NormalizeSurchargePolicy(request.SurchargePolicy);
            settings.DefaultDeliveryEstimateText = NormalizeNullable(request.DefaultDeliveryEstimateText);
            settings.UpdatedAt = now;
            settings.UpdatedByUserId = actorUserId;
        }

        private async Task LogAsync(StoreShippingSettings settings, CancellationToken cancellationToken)
        {
            var metadata = JsonSerializer.Serialize(new
            {
                settings.StoreId,
                EnabledCountryCodes = ParseCountryCodes(settings.EnabledCountryCodesJson),
                settings.DefaultFlatRate,
                settings.FreeShippingThreshold,
                settings.SurchargePolicy,
                OriginCountryCode = settings.OriginCountryCode,
            });

            await this.auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = "Shipping.SettingsUpdated",
                EntityType = nameof(StoreShippingSettings),
                EntityId = settings.PublicId.ToString(),
                Summary = "Shipping settings updated.",
                MetadataJson = metadata,
            });
        }

        private static string? Validate(UpdateStoreShippingSettingsRequest request)
        {
            if (request.Origin is null)
            {
                return "Shipping origin is required.";
            }

            if (!StoreShippingSurchargePolicies.All.Contains(NormalizeSurchargePolicyCandidate(request.SurchargePolicy) ?? string.Empty))
            {
                return "Shipping surcharge policy is invalid.";
            }

            if (request.DefaultFlatRate is < 0)
            {
                return "Default flat rate must be zero or greater.";
            }

            if (request.FreeShippingThreshold is < 0)
            {
                return "Free shipping threshold must be zero or greater.";
            }

            var countries = NormalizeCountryCodes(request.EnabledCountryCodes);
            if (countries is null)
            {
                return "Enabled shipping country codes must be 2-letter uppercase ISO codes.";
            }

            if ((countries.Count > 0 || request.DefaultFlatRate.HasValue)
                && string.IsNullOrWhiteSpace(NormalizeCountry(request.Origin.CountryCode)))
            {
                return "Shipping origin country is required when shipping country restrictions or rates are configured.";
            }

            return null;
        }

        private static string? SerializeCountryCodes(IReadOnlyList<string>? countryCodes)
        {
            var normalized = NormalizeCountryCodes(countryCodes);
            return normalized is null || normalized.Count == 0
                ? null
                : JsonSerializer.Serialize(normalized);
        }

        private static IReadOnlyList<string> ParseCountryCodes(string? json)
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

        private static IReadOnlyList<string>? NormalizeCountryCodes(IReadOnlyList<string>? countryCodes)
        {
            if (countryCodes is null || countryCodes.Count == 0)
            {
                return [];
            }

            var normalized = new List<string>();
            foreach (var countryCode in countryCodes)
            {
                var value = NormalizeCountry(countryCode);
                if (value is null || !CountryCodeRegex.IsMatch(value))
                {
                    return null;
                }

                if (!normalized.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    normalized.Add(value);
                }
            }

            return normalized;
        }

        private static string NormalizeSurchargePolicy(string? value)
        {
            var normalized = NormalizeSurchargePolicyCandidate(value);
            return StoreShippingSurchargePolicies.All.Contains(normalized ?? string.Empty)
                ? normalized!
                : DefaultSurchargePolicy;
        }

        private static string? NormalizeSurchargePolicyCandidate(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
        }

        private static string? NormalizeCountry(string? value)
        {
            var normalized = NormalizeNullable(value)?.ToUpperInvariant();
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static ServiceResponse<StoreShippingSettingsDto> Success(StoreShippingSettingsDto payload, string message)
        {
            return new ServiceResponse<StoreShippingSettingsDto>(true, message)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<StoreShippingSettingsDto> Failure(
            string? message,
            ServiceResponseType responseType = ServiceResponseType.ValidationError)
        {
            return new ServiceResponse<StoreShippingSettingsDto>(false, message ?? "Shipping settings could not be loaded.")
            {
                ResponseType = responseType,
            };
        }
    }
}
