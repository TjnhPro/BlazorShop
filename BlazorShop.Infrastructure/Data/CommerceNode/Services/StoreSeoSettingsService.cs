namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Settings;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Validations;
    using BlazorShop.Domain.Entities.CommerceNode;

    using FluentValidation;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    public sealed class StoreSeoSettingsService : IStoreSeoSettingsService
    {
        private const string CacheKeyPrefix = "store-settings";
        private const string DomainName = "seo-defaults";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly ISeoSettingsService globalSeoSettingsService;
        private readonly IValidationService validationService;
        private readonly IValidator<UpdateSeoSettingsDto> validator;
        private readonly IMemoryCache memoryCache;

        public StoreSeoSettingsService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            ISeoSettingsService globalSeoSettingsService,
            IValidationService validationService,
            IValidator<UpdateSeoSettingsDto> validator,
            IMemoryCache memoryCache)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.globalSeoSettingsService = globalSeoSettingsService;
            this.validationService = validationService;
            this.validator = validator;
            this.memoryCache = memoryCache;
        }

        public async Task<SeoSettingsDto> ResolveAsync(CancellationToken cancellationToken = default)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return await this.globalSeoSettingsService.GetCurrentAsync();
            }

            var cacheKey = BuildCacheKey(storeResult.Payload);
            if (this.memoryCache.TryGetValue(cacheKey, out SeoSettingsDto? cached) && cached is not null)
            {
                return cached;
            }

            var overrideSettings = await this.context.StoreSeoSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(settings => settings.StoreId == storeResult.Payload, cancellationToken);

            var resolved = overrideSettings is null
                ? await this.globalSeoSettingsService.GetCurrentAsync()
                : Map(overrideSettings);

            this.memoryCache.Set(cacheKey, resolved, CacheDuration);
            return resolved;
        }

        public async Task<ServiceResponse<SeoSettingsDto>> SaveOverrideAsync(
            UpdateSeoSettingsDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var validationResult = await this.validationService.ValidateAsync(request, this.validator);
            if (!validationResult.Success)
            {
                return new ServiceResponse<SeoSettingsDto>(false, validationResult.Message)
                {
                    ResponseType = ServiceResponseType.ValidationError,
                };
            }

            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return new ServiceResponse<SeoSettingsDto>(false, "Current store could not be resolved.")
                {
                    ResponseType = ServiceResponseType.NotFound,
                };
            }

            var settings = await this.context.StoreSeoSettings
                .FirstOrDefaultAsync(candidate => candidate.StoreId == storeResult.Payload, cancellationToken);

            if (settings is null)
            {
                settings = new StoreSeoSettings
                {
                    StoreId = storeResult.Payload,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                this.context.StoreSeoSettings.Add(settings);
            }

            Apply(request, settings);
            settings.UpdatedAt = DateTimeOffset.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken);
            this.memoryCache.Remove(BuildCacheKey(storeResult.Payload));

            return new ServiceResponse<SeoSettingsDto>(true, "Store SEO settings saved successfully.", settings.Id)
            {
                Payload = Map(settings),
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static string BuildCacheKey(Guid storeId)
        {
            return $"{CacheKeyPrefix}:{storeId}:{DomainName}";
        }

        private static SeoSettingsDto Map(StoreSeoSettings settings)
        {
            return new SeoSettingsDto
            {
                Id = settings.Id,
                SiteName = settings.SiteName,
                DefaultTitleSuffix = settings.DefaultTitleSuffix,
                DefaultMetaDescription = settings.DefaultMetaDescription,
                DefaultOgImage = settings.DefaultOgImage,
                BaseCanonicalUrl = settings.BaseCanonicalUrl,
                CompanyName = settings.CompanyName,
                CompanyLogoUrl = settings.CompanyLogoUrl,
                CompanyPhone = settings.CompanyPhone,
                CompanyEmail = settings.CompanyEmail,
                CompanyAddress = settings.CompanyAddress,
                FacebookUrl = settings.FacebookUrl,
                InstagramUrl = settings.InstagramUrl,
                XUrl = settings.XUrl,
            };
        }

        private static void Apply(UpdateSeoSettingsDto request, StoreSeoSettings settings)
        {
            settings.SiteName = Normalize(request.SiteName);
            settings.DefaultTitleSuffix = Normalize(request.DefaultTitleSuffix);
            settings.DefaultMetaDescription = Normalize(request.DefaultMetaDescription);
            settings.DefaultOgImage = Normalize(request.DefaultOgImage);
            settings.BaseCanonicalUrl = Normalize(request.BaseCanonicalUrl);
            settings.CompanyName = Normalize(request.CompanyName);
            settings.CompanyLogoUrl = Normalize(request.CompanyLogoUrl);
            settings.CompanyPhone = Normalize(request.CompanyPhone);
            settings.CompanyEmail = Normalize(request.CompanyEmail);
            settings.CompanyAddress = Normalize(request.CompanyAddress);
            settings.FacebookUrl = Normalize(request.FacebookUrl);
            settings.InstagramUrl = Normalize(request.InstagramUrl);
            settings.XUrl = Normalize(request.XUrl);
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
