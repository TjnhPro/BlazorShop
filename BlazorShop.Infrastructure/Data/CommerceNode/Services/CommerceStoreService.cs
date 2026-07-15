namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Globalization;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    using BlazorShop.Application.CommerceNode.Settings;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    public sealed partial class CommerceStoreService : ICommerceStoreService
    {
        private const int DefaultTake = 100;
        private const int MaxTake = 200;
        private static readonly HashSet<string> SafeNamedTileColors = new(StringComparer.OrdinalIgnoreCase)
        {
            "black",
            "transparent",
            "white",
        };

        private readonly CommerceNodeDbContext context;
        private readonly IMemoryCache memoryCache;
        private readonly IStorefrontPublicConfigurationCache? publicConfigurationCache;

        public CommerceStoreService(
            CommerceNodeDbContext context,
            IMemoryCache memoryCache,
            IStorefrontPublicConfigurationCache? publicConfigurationCache = null)
        {
            this.context = context;
            this.memoryCache = memoryCache;
            this.publicConfigurationCache = publicConfigurationCache;
        }

        public async Task<CommerceStoreOperationResult<CommerceStoreListResponse>> ListAsync(
            CommerceStoreListQuery query,
            CancellationToken cancellationToken = default)
        {
            var skip = Math.Max(0, query.Skip);
            var take = Math.Clamp(query.Take <= 0 ? DefaultTake : query.Take, 1, MaxTake);

            var stores = this.context.CommerceStores
                .AsNoTracking()
                .Include(store => store.Domains)
                .Where(store => store.ArchivedAt == null);

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = query.Status.Trim().ToLowerInvariant();
                stores = stores.Where(store => store.Status == status);
            }

            var totalCount = await stores.CountAsync(cancellationToken);
            var storeEntities = await stores
                .OrderBy(store => store.DisplayOrder)
                .ThenBy(store => store.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
            var items = storeEntities.Select(MapSummary).ToList();

            return Succeeded(
                "Stores retrieved.",
                new CommerceStoreListResponse(items, totalCount, skip, take));
        }

        public async Task<CommerceStoreOperationResult<CommerceStoreDetail>> GetByPublicIdAsync(
            Guid publicId,
            CancellationToken cancellationToken = default)
        {
            var store = await this.LoadStoreAsync(publicId, asTracking: false, cancellationToken);
            return store is null
                ? Failed<CommerceStoreDetail>(CommerceStoreOperationFailure.NotFound, "Store was not found.")
                : Succeeded("Store retrieved.", MapDetail(store));
        }

        public async Task<CommerceStoreOperationResult<CommerceStoreDetail>> CreateAsync(
            CreateCommerceStoreRequest request,
            CancellationToken cancellationToken = default)
        {
            var validationError = ValidateCreateRequest(request);
            if (validationError is not null)
            {
                return Failed<CommerceStoreDetail>(CommerceStoreOperationFailure.Validation, validationError);
            }

            var storeKey = request.StoreKey.Trim().ToLowerInvariant();
            var exists = await this.context.CommerceStores.AnyAsync(
                store => store.StoreKey == storeKey && store.ArchivedAt == null,
                cancellationToken);
            if (exists)
            {
                return Failed<CommerceStoreDetail>(CommerceStoreOperationFailure.Conflict, "Store key already exists.");
            }

            var now = DateTimeOffset.UtcNow;
            var store = new CommerceStore
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreKey = storeKey,
                Status = CommerceStoreStatuses.Disabled,
                CreatedAt = now,
            };
            ApplyStoreValues(store, request, now);

            if (!string.IsNullOrWhiteSpace(request.PrimaryDomain))
            {
                var domainResult = await this.CreateDomainAsync(store, request.PrimaryDomain, isPrimary: true, now, cancellationToken);
                if (!domainResult.Success)
                {
                    return Failed<CommerceStoreDetail>(domainResult.Failure!.Value, domainResult.Message);
                }
            }

            this.context.CommerceStores.Add(store);
            await this.context.SaveChangesAsync(cancellationToken);
            this.InvalidateStoreCache(store);

            return Succeeded("Store created.", MapDetail(store));
        }

        public async Task<CommerceStoreOperationResult<CommerceStoreDetail>> UpdateAsync(
            Guid publicId,
            UpdateCommerceStoreRequest request,
            CancellationToken cancellationToken = default)
        {
            var validationError = ValidateUpdateRequest(request);
            if (validationError is not null)
            {
                return Failed<CommerceStoreDetail>(CommerceStoreOperationFailure.Validation, validationError);
            }

            var store = await this.LoadStoreAsync(publicId, asTracking: true, cancellationToken);
            if (store is null)
            {
                return Failed<CommerceStoreDetail>(CommerceStoreOperationFailure.NotFound, "Store was not found.");
            }

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                request.Status.Trim().ToLowerInvariant() is not (
                    CommerceStoreStatuses.Active or
                    CommerceStoreStatuses.Disabled or
                    CommerceStoreStatuses.Provisioning))
            {
                return Failed<CommerceStoreDetail>(CommerceStoreOperationFailure.Validation, "Store status is invalid.");
            }

            ApplyStoreValues(store, request, DateTimeOffset.UtcNow);
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                store.Status = request.Status.Trim().ToLowerInvariant();
            }

            await this.context.SaveChangesAsync(cancellationToken);
            this.InvalidateStoreCache(store);
            return Succeeded("Store updated.", MapDetail(store));
        }

        public async Task<CommerceStoreOperationResult<CommerceStoreDetail>> ArchiveAsync(
            Guid publicId,
            CancellationToken cancellationToken = default)
        {
            var store = await this.LoadStoreAsync(publicId, asTracking: true, cancellationToken);
            if (store is null)
            {
                return Failed<CommerceStoreDetail>(CommerceStoreOperationFailure.NotFound, "Store was not found.");
            }

            var now = DateTimeOffset.UtcNow;
            store.Status = CommerceStoreStatuses.Archived;
            store.ArchivedAt = now;
            store.UpdatedAt = now;

            foreach (var domain in store.Domains.Where(domain => domain.DisabledAt == null))
            {
                domain.Status = CommerceStoreDomainStatuses.Disabled;
                domain.DisabledAt = now;
                domain.IsPrimary = false;
                domain.UpdatedAt = now;
            }

            await this.context.SaveChangesAsync(cancellationToken);
            this.InvalidateStoreCache(store);
            return Succeeded("Store archived.", MapDetail(store));
        }

        public async Task<CommerceStoreOperationResult<CommerceStoreDetail>> SetStatusAsync(
            Guid publicId,
            string status,
            CancellationToken cancellationToken = default)
        {
            var normalizedStatus = status?.Trim().ToLowerInvariant();
            if (normalizedStatus is not (CommerceStoreStatuses.Active or CommerceStoreStatuses.Disabled or CommerceStoreStatuses.Provisioning))
            {
                return Failed<CommerceStoreDetail>(CommerceStoreOperationFailure.Validation, "Store status is invalid.");
            }

            var store = await this.LoadStoreAsync(publicId, asTracking: true, cancellationToken);
            if (store is null)
            {
                return Failed<CommerceStoreDetail>(CommerceStoreOperationFailure.NotFound, "Store was not found.");
            }

            store.Status = normalizedStatus;
            store.UpdatedAt = DateTimeOffset.UtcNow;
            await this.context.SaveChangesAsync(cancellationToken);
            this.InvalidateStoreCache(store);
            return Succeeded("Store status updated.", MapDetail(store));
        }

        public async Task<CommerceStoreOperationResult<CommerceStoreDetail>> AddDomainAsync(
            Guid publicId,
            CreateCommerceStoreDomainRequest request,
            CancellationToken cancellationToken = default)
        {
            var store = await this.LoadStoreAsync(publicId, asTracking: true, cancellationToken);
            if (store is null)
            {
                return Failed<CommerceStoreDetail>(CommerceStoreOperationFailure.NotFound, "Store was not found.");
            }

            var result = await this.CreateDomainAsync(store, request.Domain, request.IsPrimary, DateTimeOffset.UtcNow, cancellationToken);
            if (!result.Success)
            {
                return Failed<CommerceStoreDetail>(result.Failure!.Value, result.Message);
            }

            await this.context.SaveChangesAsync(cancellationToken);
            this.InvalidateStoreCache(store);
            return Succeeded("Domain added.", MapDetail(store));
        }

        public async Task<CommerceStoreOperationResult<CommerceStoreDetail>> VerifyDomainAsync(
            Guid publicId,
            Guid domainId,
            CancellationToken cancellationToken = default)
        {
            var result = await this.MutateDomainAsync(
                publicId,
                domainId,
                domain =>
                {
                    domain.Status = CommerceStoreDomainStatuses.Verified;
                    domain.VerifiedAt = DateTimeOffset.UtcNow;
                },
                "Domain verified.",
                cancellationToken);

            return result;
        }

        public async Task<CommerceStoreOperationResult<CommerceStoreDetail>> DisableDomainAsync(
            Guid publicId,
            Guid domainId,
            CancellationToken cancellationToken = default)
        {
            var result = await this.MutateDomainAsync(
                publicId,
                domainId,
                domain =>
                {
                    domain.Status = CommerceStoreDomainStatuses.Disabled;
                    domain.DisabledAt = DateTimeOffset.UtcNow;
                    domain.IsPrimary = false;
                },
                "Domain disabled.",
                cancellationToken);

            return result;
        }

        public async Task<CommerceStoreOperationResult<CommerceStoreDetail>> SetPrimaryDomainAsync(
            Guid publicId,
            Guid domainId,
            CancellationToken cancellationToken = default)
        {
            var store = await this.LoadStoreAsync(publicId, asTracking: true, cancellationToken);
            if (store is null)
            {
                return Failed<CommerceStoreDetail>(CommerceStoreOperationFailure.NotFound, "Store was not found.");
            }

            var domain = store.Domains.FirstOrDefault(item => item.Id == domainId && item.DisabledAt == null);
            if (domain is null)
            {
                return Failed<CommerceStoreDetail>(CommerceStoreOperationFailure.NotFound, "Domain was not found.");
            }

            var now = DateTimeOffset.UtcNow;
            foreach (var existingPrimary in store.Domains.Where(item => item.IsPrimary && item.Id != domain.Id))
            {
                existingPrimary.IsPrimary = false;
                existingPrimary.UpdatedAt = now;
            }

            domain.IsPrimary = true;
            domain.UpdatedAt = now;
            store.UpdatedAt = now;
            await this.context.SaveChangesAsync(cancellationToken);
            this.InvalidateStoreCache(store);

            return Succeeded("Primary domain updated.", MapDetail(store));
        }

        private async Task<CommerceStoreOperationResult<CommerceStoreDomain>> CreateDomainAsync(
            CommerceStore store,
            string? domainValue,
            bool isPrimary,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(domainValue))
            {
                return Failed<CommerceStoreDomain>(CommerceStoreOperationFailure.Validation, "Domain is required.");
            }

            var normalizedDomain = NormalizeDomain(domainValue);
            if (normalizedDomain is null)
            {
                return Failed<CommerceStoreDomain>(CommerceStoreOperationFailure.Validation, "Domain is invalid.");
            }

            var exists = await this.context.CommerceStoreDomains.AnyAsync(
                domain => domain.NormalizedDomain == normalizedDomain && domain.DisabledAt == null,
                cancellationToken);
            if (exists)
            {
                return Failed<CommerceStoreDomain>(CommerceStoreOperationFailure.Conflict, "Domain already exists.");
            }

            if (isPrimary)
            {
                foreach (var existingPrimary in store.Domains.Where(domain => domain.IsPrimary && domain.DisabledAt == null))
                {
                    existingPrimary.IsPrimary = false;
                    existingPrimary.UpdatedAt = now;
                }
            }

            var domain = new CommerceStoreDomain
            {
                Id = Guid.NewGuid(),
                Store = store,
                StoreId = store.Id,
                Domain = domainValue.Trim(),
                NormalizedDomain = normalizedDomain,
                IsPrimary = isPrimary,
                Status = CommerceStoreDomainStatuses.Pending,
                CreatedAt = now,
                UpdatedAt = now,
            };

            store.Domains.Add(domain);
            store.UpdatedAt = now;
            return Succeeded("Domain created.", domain);
        }

        private async Task<CommerceStoreOperationResult<CommerceStoreDetail>> MutateDomainAsync(
            Guid publicId,
            Guid domainId,
            Action<CommerceStoreDomain> mutation,
            string successMessage,
            CancellationToken cancellationToken)
        {
            var store = await this.LoadStoreAsync(publicId, asTracking: true, cancellationToken);
            if (store is null)
            {
                return Failed<CommerceStoreDetail>(CommerceStoreOperationFailure.NotFound, "Store was not found.");
            }

            var domain = store.Domains.FirstOrDefault(item => item.Id == domainId);
            if (domain is null)
            {
                return Failed<CommerceStoreDetail>(CommerceStoreOperationFailure.NotFound, "Domain was not found.");
            }

            mutation(domain);
            domain.UpdatedAt = DateTimeOffset.UtcNow;
            store.UpdatedAt = DateTimeOffset.UtcNow;
            await this.context.SaveChangesAsync(cancellationToken);
            this.InvalidateStoreCache(store);

            return Succeeded(successMessage, MapDetail(store));
        }

        private void InvalidateStoreCache(CommerceStore store)
        {
            this.memoryCache.Remove($"commerce-store:key:{store.StoreKey}");
            this.publicConfigurationCache?.Invalidate(store.StoreKey);
            foreach (var domain in store.Domains)
            {
                this.memoryCache.Remove($"commerce-store:host:{domain.NormalizedDomain}");
            }
        }

        private async Task<CommerceStore?> LoadStoreAsync(
            Guid publicId,
            bool asTracking,
            CancellationToken cancellationToken)
        {
            var query = this.context.CommerceStores
                .Include(store => store.Domains.OrderByDescending(domain => domain.IsPrimary).ThenBy(domain => domain.Domain))
                .Where(store => store.PublicId == publicId && store.ArchivedAt == null);

            if (!asTracking)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        private static string? ValidateCreateRequest(CreateCommerceStoreRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StoreKey) || !StoreKeyRegex().IsMatch(request.StoreKey.Trim()))
            {
                return "Store key is invalid.";
            }

            return ValidateSharedValues(
                request.Name,
                request.BaseUrl,
                request.CdnHost,
                request.LogoUrl,
                request.FaviconUrl,
                request.PngIconUrl,
                request.AppleTouchIconUrl,
                request.MsTileImageUrl,
                request.MsTileColor,
                request.DefaultCurrencyCode,
                request.DefaultCulture,
                request.SupportEmail,
                request.CompanyEmail,
                request.MetadataJson);
        }

        private static string? ValidateUpdateRequest(UpdateCommerceStoreRequest request)
        {
            return ValidateSharedValues(
                request.Name,
                request.BaseUrl,
                request.CdnHost,
                request.LogoUrl,
                request.FaviconUrl,
                request.PngIconUrl,
                request.AppleTouchIconUrl,
                request.MsTileImageUrl,
                request.MsTileColor,
                request.DefaultCurrencyCode,
                request.DefaultCulture,
                request.SupportEmail,
                request.CompanyEmail,
                request.MetadataJson);
        }

        private static string? ValidateSharedValues(
            string name,
            string? baseUrl,
            string? cdnHost,
            string? logoUrl,
            string? faviconUrl,
            string? pngIconUrl,
            string? appleTouchIconUrl,
            string? msTileImageUrl,
            string? msTileColor,
            string defaultCurrencyCode,
            string defaultCulture,
            string? supportEmail,
            string? companyEmail,
            string? metadataJson)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 400)
            {
                return "Store name is required and must be at most 400 characters.";
            }

            if (!string.IsNullOrWhiteSpace(baseUrl) &&
                (!Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var baseUri) ||
                 baseUri.Scheme is not ("http" or "https")))
            {
                return "Base URL must be an absolute HTTP URL.";
            }

            if (!IsValidOptionalHost(cdnHost))
            {
                return "CDN host must be a valid host name, host:port, or absolute HTTP URL.";
            }

            if (!IsValidOptionalPublicAssetUrl(logoUrl))
            {
                return "Logo URL must be an absolute HTTP URL or a safe root-relative path.";
            }

            if (!IsValidOptionalPublicAssetUrl(faviconUrl))
            {
                return "Favicon URL must be an absolute HTTP URL or a safe root-relative path.";
            }

            if (!IsValidOptionalPublicAssetUrl(pngIconUrl))
            {
                return "PNG icon URL must be an absolute HTTP URL or a safe root-relative path.";
            }

            if (!IsValidOptionalPublicAssetUrl(appleTouchIconUrl))
            {
                return "Apple touch icon URL must be an absolute HTTP URL or a safe root-relative path.";
            }

            if (!IsValidOptionalPublicAssetUrl(msTileImageUrl))
            {
                return "MS tile image URL must be an absolute HTTP URL or a safe root-relative path.";
            }

            if (!IsValidOptionalTileColor(msTileColor))
            {
                return "MS tile color must be a hex color or one of: transparent, black, white.";
            }

            if (string.IsNullOrWhiteSpace(defaultCurrencyCode) ||
                !CurrencyRegex().IsMatch(defaultCurrencyCode.Trim().ToUpperInvariant()))
            {
                return "Default currency code must be a 3-letter code.";
            }

            try
            {
                _ = CultureInfo.GetCultureInfo(defaultCulture.Trim());
            }
            catch (CultureNotFoundException)
            {
                return "Default culture is invalid.";
            }

            if (!string.IsNullOrWhiteSpace(supportEmail) && !EmailRegex().IsMatch(supportEmail.Trim()))
            {
                return "Support email is invalid.";
            }

            if (!string.IsNullOrWhiteSpace(companyEmail) && !EmailRegex().IsMatch(companyEmail.Trim()))
            {
                return "Company email is invalid.";
            }

            if (!string.IsNullOrWhiteSpace(metadataJson) && !IsValidJson(metadataJson))
            {
                return "MetadataJson must be valid JSON.";
            }

            return null;
        }

        private static void ApplyStoreValues(CommerceStore store, CreateCommerceStoreRequest request, DateTimeOffset now)
        {
            store.Name = request.Name.Trim();
            store.BaseUrl = NormalizeOptional(request.BaseUrl);
            store.ForceHttps = request.ForceHttps;
            store.SslEnabled = request.SslEnabled;
            store.SslPort = request.SslPort;
            store.DisplayOrder = request.DisplayOrder;
            store.HtmlBodyId = NormalizeOptional(request.HtmlBodyId);
            store.CdnHost = NormalizeOptional(request.CdnHost);
            store.LogoUrl = NormalizeOptional(request.LogoUrl);
            store.CompanyName = NormalizeOptional(request.CompanyName);
            store.CompanyEmail = NormalizeOptional(request.CompanyEmail);
            store.CompanyPhone = NormalizeOptional(request.CompanyPhone);
            store.CompanyAddress = NormalizeOptional(request.CompanyAddress);
            store.FaviconUrl = NormalizeOptional(request.FaviconUrl);
            store.PngIconUrl = NormalizeOptional(request.PngIconUrl);
            store.AppleTouchIconUrl = NormalizeOptional(request.AppleTouchIconUrl);
            store.MsTileImageUrl = NormalizeOptional(request.MsTileImageUrl);
            store.MsTileColor = NormalizeOptional(request.MsTileColor);
            store.DefaultCurrencyCode = request.DefaultCurrencyCode.Trim().ToUpperInvariant();
            store.DefaultCulture = request.DefaultCulture.Trim();
            store.SupportEmail = NormalizeOptional(request.SupportEmail);
            store.SupportPhone = NormalizeOptional(request.SupportPhone);
            store.MaintenanceModeEnabled = request.MaintenanceModeEnabled;
            store.MaintenanceMessage = NormalizeOptional(request.MaintenanceMessage);
            store.MetadataJson = NormalizeOptional(request.MetadataJson);
            store.UpdatedAt = now;
        }

        private static void ApplyStoreValues(CommerceStore store, UpdateCommerceStoreRequest request, DateTimeOffset now)
        {
            store.Name = request.Name.Trim();
            store.BaseUrl = NormalizeOptional(request.BaseUrl);
            store.ForceHttps = request.ForceHttps;
            store.SslEnabled = request.SslEnabled;
            store.SslPort = request.SslPort;
            store.DisplayOrder = request.DisplayOrder;
            store.HtmlBodyId = NormalizeOptional(request.HtmlBodyId);
            store.CdnHost = NormalizeOptional(request.CdnHost);
            store.LogoUrl = NormalizeOptional(request.LogoUrl);
            store.CompanyName = NormalizeOptional(request.CompanyName);
            store.CompanyEmail = NormalizeOptional(request.CompanyEmail);
            store.CompanyPhone = NormalizeOptional(request.CompanyPhone);
            store.CompanyAddress = NormalizeOptional(request.CompanyAddress);
            store.FaviconUrl = NormalizeOptional(request.FaviconUrl);
            store.PngIconUrl = NormalizeOptional(request.PngIconUrl);
            store.AppleTouchIconUrl = NormalizeOptional(request.AppleTouchIconUrl);
            store.MsTileImageUrl = NormalizeOptional(request.MsTileImageUrl);
            store.MsTileColor = NormalizeOptional(request.MsTileColor);
            store.DefaultCurrencyCode = request.DefaultCurrencyCode.Trim().ToUpperInvariant();
            store.DefaultCulture = request.DefaultCulture.Trim();
            store.SupportEmail = NormalizeOptional(request.SupportEmail);
            store.SupportPhone = NormalizeOptional(request.SupportPhone);
            store.MaintenanceModeEnabled = request.MaintenanceModeEnabled;
            store.MaintenanceMessage = NormalizeOptional(request.MaintenanceMessage);
            store.MetadataJson = NormalizeOptional(request.MetadataJson);
            store.UpdatedAt = now;
        }

        private static CommerceStoreSummary MapSummary(CommerceStore store)
        {
            var primaryDomain = store.Domains.FirstOrDefault(domain => domain.IsPrimary && domain.DisabledAt == null);
            return new CommerceStoreSummary(
                store.PublicId,
                store.StoreKey,
                store.Name,
                store.Status,
                store.BaseUrl,
                store.DisplayOrder,
                store.DefaultCurrencyCode,
                store.DefaultCulture,
                store.MaintenanceModeEnabled,
                primaryDomain?.Domain,
                store.CreatedAt,
                store.UpdatedAt);
        }

        private static CommerceStoreDetail MapDetail(CommerceStore store)
        {
            return new CommerceStoreDetail(
                store.PublicId,
                store.ControlPlaneStorePublicId,
                store.StoreKey,
                store.Name,
                store.Status,
                store.BaseUrl,
                store.ForceHttps,
                store.SslEnabled,
                store.SslPort,
                store.DisplayOrder,
                store.HtmlBodyId,
                store.CdnHost,
                store.LogoUrl,
                store.CompanyName,
                store.CompanyEmail,
                store.CompanyPhone,
                store.CompanyAddress,
                store.FaviconUrl,
                store.PngIconUrl,
                store.AppleTouchIconUrl,
                store.MsTileImageUrl,
                store.MsTileColor,
                store.DefaultCurrencyCode,
                store.DefaultCulture,
                store.SupportEmail,
                store.SupportPhone,
                store.MaintenanceModeEnabled,
                store.MaintenanceMessage,
                store.MetadataJson,
                store.CreatedAt,
                store.UpdatedAt,
                store.ArchivedAt,
                store.Domains.Select(MapDomain).ToList());
        }

        private static CommerceStoreDomainDto MapDomain(CommerceStoreDomain domain)
        {
            return new CommerceStoreDomainDto(
                domain.Id,
                domain.Domain,
                domain.NormalizedDomain,
                domain.IsPrimary,
                domain.Status,
                domain.CreatedAt,
                domain.UpdatedAt,
                domain.VerifiedAt,
                domain.DisabledAt);
        }

        private static CommerceStoreOperationResult<TPayload> Succeeded<TPayload>(
            string message,
            TPayload payload)
        {
            return new CommerceStoreOperationResult<TPayload>(true, message, payload);
        }

        private static CommerceStoreOperationResult<TPayload> Failed<TPayload>(
            CommerceStoreOperationFailure failure,
            string message)
        {
            return new CommerceStoreOperationResult<TPayload>(false, message, Failure: failure);
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public static string? NormalizeDomain(string value)
        {
            var host = value.Trim().ToLowerInvariant();
            if (Uri.TryCreate(host, UriKind.Absolute, out var uri))
            {
                host = uri.Host;
            }

            host = host.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? host;
            return HostRegex().IsMatch(host) ? host : null;
        }

        private static bool IsValidOptionalPublicAssetUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            var candidate = value.Trim();
            if (candidate.Any(char.IsControl))
            {
                return false;
            }

            if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            {
                return uri.Scheme is "http" or "https"
                    && !string.IsNullOrWhiteSpace(uri.Host);
            }

            return candidate.StartsWith("/", StringComparison.Ordinal)
                && !candidate.StartsWith("//", StringComparison.Ordinal)
                && !candidate.Contains('\\');
        }

        private static bool IsValidOptionalHost(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            var candidate = value.Trim();
            if (candidate.Any(char.IsControl)
                || candidate.Contains('/')
                || candidate.Contains('\\'))
            {
                return Uri.TryCreate(candidate, UriKind.Absolute, out var absoluteUri)
                    && absoluteUri.Scheme is "http" or "https"
                    && HostRegex().IsMatch(absoluteUri.Host);
            }

            var host = candidate;
            var portSeparatorIndex = candidate.LastIndexOf(':');
            if (portSeparatorIndex > -1)
            {
                host = candidate[..portSeparatorIndex];
                var portText = candidate[(portSeparatorIndex + 1)..];
                if (!int.TryParse(portText, CultureInfo.InvariantCulture, out var port)
                    || port is < 1 or > 65535)
                {
                    return false;
                }
            }

            return HostRegex().IsMatch(host.ToLowerInvariant());
        }

        private static bool IsValidOptionalTileColor(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            var candidate = value.Trim();
            return HexColorRegex().IsMatch(candidate)
                || SafeNamedTileColors.Contains(candidate);
        }

        private static bool IsValidJson(string value)
        {
            try
            {
                using var _ = JsonDocument.Parse(value);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,62}$", RegexOptions.Compiled)]
        private static partial Regex StoreKeyRegex();

        [GeneratedRegex("^[A-Z]{3}$", RegexOptions.Compiled)]
        private static partial Regex CurrencyRegex();

        [GeneratedRegex("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.Compiled)]
        private static partial Regex EmailRegex();

        [GeneratedRegex("^[a-z0-9][a-z0-9.-]*[a-z0-9]$", RegexOptions.Compiled)]
        private static partial Regex HostRegex();

        [GeneratedRegex("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled)]
        private static partial Regex HexColorRegex();
    }
}
