namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Text.RegularExpressions;

    using BlazorShop.Application.ControlPlane.Common;
    using BlazorShop.Application.ControlPlane.Stores;
    using BlazorShop.Domain.Entities.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    public sealed partial class ControlPlaneStoreService : IControlPlaneStoreService
    {
        private readonly ControlPlaneDbContext dbContext;

        public ControlPlaneStoreService(ControlPlaneDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<ControlPlaneStoreListResponse> ListAsync(
            ControlPlaneStoreListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var stores = this.dbContext.Stores
                .AsNoTracking()
                .Include(store => store.Node)
                .Include(store => store.Domains)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLowerInvariant();
                stores = stores.Where(store =>
                    store.StoreKey.ToLower().Contains(search)
                    || store.Name.ToLower().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = query.Status.Trim().ToLowerInvariant();
                stores = stores.Where(store => store.Status == status);
            }

            if (query.NodePublicId is not null)
            {
                stores = stores.Where(store => store.Node != null && store.Node.PublicId == query.NodePublicId);
            }

            var page = ControlPlanePaging.Normalize(query.PageNumber, query.PageSize);
            var totalCount = await stores.CountAsync(cancellationToken);
            var items = await stores
                .OrderBy(store => store.StoreKey)
                .Skip(page.Skip)
                .Take(page.PageSize)
                .ToListAsync(cancellationToken);

            return new ControlPlaneStoreListResponse(
                items.Select(MapSummary).ToArray(),
                totalCount,
                page.PageNumber,
                page.PageSize,
                ControlPlanePaging.GetTotalPages(totalCount, page.PageSize));
        }

        public async Task<ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>> GetByPublicIdAsync(
            Guid publicId,
            CancellationToken cancellationToken = default)
        {
            var store = await this.LoadStoreAsync(publicId, cancellationToken);
            return store is null ? NotFound("Store was not found.") : Succeeded(MapDetail(store));
        }

        public async Task<ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>> CreateAsync(
            CreateControlPlaneStoreRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await this.ValidateCreateRequestAsync(request, cancellationToken);
            if (validation is not null)
            {
                return validation;
            }

            var node = await this.LoadWritableNodeAsync(request.NodePublicId, cancellationToken);
            if (node is null)
            {
                return ValidationFailed("Target node was not found or is disabled.");
            }

            var now = DateTimeOffset.UtcNow;
            var store = new StoreRegistry
            {
                StoreKey = request.StoreKey.Trim().ToLowerInvariant(),
                Name = request.Name.Trim(),
                Status = ControlPlaneStoreStatuses.Disabled,
                MetadataJson = NormalizeOptionalJson(request.MetadataJson),
                NodeId = node.Id,
                CreatedAt = now,
                UpdatedAt = now
            };

            this.dbContext.Stores.Add(store);
            await this.dbContext.SaveChangesAsync(cancellationToken);

            return Succeeded(MapDetail((await this.LoadStoreAsync(store.PublicId, cancellationToken))!));
        }

        public async Task<ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>> UpdateAsync(
            Guid publicId,
            UpdateControlPlaneStoreRequest request,
            CancellationToken cancellationToken = default)
        {
            var store = await this.LoadStoreAsync(publicId, cancellationToken);
            if (store is null)
            {
                return NotFound("Store was not found.");
            }

            if (store.Status == ControlPlaneStoreStatuses.Archived)
            {
                return ValidationFailed("Archived stores cannot be updated.");
            }

            var validation = ValidateCommonFields(request?.Name, request?.MetadataJson);
            if (validation is not null)
            {
                return ValidationFailed(validation);
            }

            var node = await this.LoadWritableNodeAsync(request!.NodePublicId, cancellationToken);
            if (node is null)
            {
                return ValidationFailed("Target node was not found or is disabled.");
            }

            store.Name = request.Name.Trim();
            store.NodeId = node.Id;
            store.Node = node;
            store.MetadataJson = NormalizeOptionalJson(request.MetadataJson);
            store.UpdatedAt = DateTimeOffset.UtcNow;

            await this.dbContext.SaveChangesAsync(cancellationToken);
            return Succeeded(MapDetail(store));
        }

        public async Task<ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>> ArchiveAsync(
            Guid publicId,
            CancellationToken cancellationToken = default)
        {
            var store = await this.LoadStoreAsync(publicId, cancellationToken);
            if (store is null)
            {
                return NotFound("Store was not found.");
            }

            if (store.Status != ControlPlaneStoreStatuses.Archived)
            {
                var now = DateTimeOffset.UtcNow;
                store.Status = ControlPlaneStoreStatuses.Archived;
                store.ArchivedAt = now;
                store.UpdatedAt = now;
                await this.dbContext.SaveChangesAsync(cancellationToken);
            }

            return Succeeded(MapDetail(store));
        }

        public async Task<ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>> AddDomainAsync(
            Guid publicId,
            CreateControlPlaneStoreDomainRequest request,
            CancellationToken cancellationToken = default)
        {
            var store = await this.LoadStoreAsync(publicId, cancellationToken);
            if (store is null)
            {
                return NotFound("Store was not found.");
            }

            if (store.Status == ControlPlaneStoreStatuses.Archived)
            {
                return ValidationFailed("Domains cannot be added to archived stores.");
            }

            var normalizedDomain = NormalizeDomain(request?.Domain);
            if (normalizedDomain is null)
            {
                return ValidationFailed("Domain must be a valid host name.");
            }

            var domainExists = await this.dbContext.StoreDomains.AnyAsync(
                domain => domain.NormalizedDomain == normalizedDomain && domain.DisabledAt == null,
                cancellationToken);

            if (domainExists)
            {
                return Conflict("Another active store domain already uses this domain.");
            }

            var now = DateTimeOffset.UtcNow;
            store.Domains.Add(new StoreDomainRegistry
            {
                Domain = request!.Domain.Trim(),
                NormalizedDomain = normalizedDomain,
                Status = "pending",
                CreatedAt = now,
                UpdatedAt = now
            });
            store.UpdatedAt = now;

            await this.dbContext.SaveChangesAsync(cancellationToken);
            return Succeeded(MapDetail(store));
        }

        public async Task<ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>> VerifyDomainAsync(
            Guid publicId,
            long domainId,
            CancellationToken cancellationToken = default)
        {
            return await this.UpdateDomainAsync(publicId, domainId, verify: true, cancellationToken);
        }

        public async Task<ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>> DisableDomainAsync(
            Guid publicId,
            long domainId,
            CancellationToken cancellationToken = default)
        {
            return await this.UpdateDomainAsync(publicId, domainId, verify: false, cancellationToken);
        }

        private async Task<ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>> UpdateDomainAsync(
            Guid publicId,
            long domainId,
            bool verify,
            CancellationToken cancellationToken)
        {
            var store = await this.LoadStoreAsync(publicId, cancellationToken);
            if (store is null)
            {
                return NotFound("Store was not found.");
            }

            var domain = store.Domains.FirstOrDefault(candidate => candidate.Id == domainId);
            if (domain is null)
            {
                return NotFound("Store domain was not found.");
            }

            var now = DateTimeOffset.UtcNow;
            domain.Status = verify ? "verified" : "disabled";
            domain.UpdatedAt = now;
            domain.VerifiedAt = verify ? now : domain.VerifiedAt;
            domain.DisabledAt = verify ? null : now;
            store.UpdatedAt = now;

            await this.dbContext.SaveChangesAsync(cancellationToken);
            return Succeeded(MapDetail(store));
        }

        private async Task<ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>?> ValidateCreateRequestAsync(
            CreateControlPlaneStoreRequest? request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return ValidationFailed("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.StoreKey) || !StoreKeyRegex().IsMatch(request.StoreKey.Trim()))
            {
                return ValidationFailed("Store key must be 3-64 characters and contain only lowercase letters, digits, and hyphens.");
            }

            var commonValidation = ValidateCommonFields(request.Name, request.MetadataJson);
            if (commonValidation is not null)
            {
                return ValidationFailed(commonValidation);
            }

            var storeKey = request.StoreKey.Trim().ToLowerInvariant();
            var duplicateExists = await this.dbContext.Stores.AnyAsync(
                store => store.StoreKey == storeKey && store.ArchivedAt == null,
                cancellationToken);

            return duplicateExists ? Conflict("An active store with this key already exists.") : null;
        }

        private static string? ValidateCommonFields(string? name, string? metadataJson)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Store name is required.";
            }

            if (name.Trim().Length > 120)
            {
                return "Store name must be 120 characters or fewer.";
            }

            if (!string.IsNullOrWhiteSpace(metadataJson))
            {
                try
                {
                    using var _ = System.Text.Json.JsonDocument.Parse(metadataJson);
                }
                catch (System.Text.Json.JsonException)
                {
                    return "Metadata must be valid JSON.";
                }
            }

            return null;
        }

        private async Task<CommerceNode?> LoadWritableNodeAsync(Guid publicId, CancellationToken cancellationToken)
        {
            return await this.dbContext.Nodes.FirstOrDefaultAsync(
                node => node.PublicId == publicId && node.Status != "disabled",
                cancellationToken);
        }

        private async Task<StoreRegistry?> LoadStoreAsync(Guid publicId, CancellationToken cancellationToken)
        {
            return await this.dbContext.Stores
                .Include(store => store.Node)
                .Include(store => store.Domains)
                .FirstOrDefaultAsync(store => store.PublicId == publicId, cancellationToken);
        }

        private static string? NormalizeOptionalJson(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? NormalizeDomain(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var candidate = value.Trim().ToLowerInvariant();
            if (!candidate.Contains("://", StringComparison.Ordinal))
            {
                candidate = "https://" + candidate;
            }

            return Uri.TryCreate(candidate, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host)
                ? uri.Host.Trim('.')
                : null;
        }

        private static ControlPlaneStoreSummary MapSummary(StoreRegistry store)
        {
            return new ControlPlaneStoreSummary(
                store.PublicId,
                store.StoreKey,
                store.Name,
                store.Status,
                store.Node?.PublicId ?? Guid.Empty,
                store.Node?.NodeKey ?? string.Empty,
                store.Node?.Name ?? string.Empty,
                store.Node?.Status ?? "unknown",
                store.CreatedAt,
                store.UpdatedAt,
                store.ArchivedAt,
                store.Domains.Count(domain => domain.DisabledAt is null));
        }

        private static ControlPlaneStoreDetail MapDetail(StoreRegistry store)
        {
            return new ControlPlaneStoreDetail(
                store.PublicId,
                store.StoreKey,
                store.Name,
                store.Status,
                store.MetadataJson,
                store.Node?.PublicId ?? Guid.Empty,
                store.Node?.NodeKey ?? string.Empty,
                store.Node?.Name ?? string.Empty,
                store.Node?.Status ?? "unknown",
                store.CreatedAt,
                store.UpdatedAt,
                store.ArchivedAt,
                store.Domains
                    .OrderBy(domain => domain.NormalizedDomain, StringComparer.Ordinal)
                    .Select(domain => new ControlPlaneStoreDomainDto(
                        domain.Id,
                        domain.Domain,
                        domain.NormalizedDomain,
                        domain.Status,
                        domain.CreatedAt,
                        domain.UpdatedAt,
                        domain.VerifiedAt,
                        domain.DisabledAt))
                    .ToArray());
        }

        private static ControlPlaneStoreOperationResult<ControlPlaneStoreDetail> Succeeded(ControlPlaneStoreDetail payload)
        {
            return new ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>(true, Payload: payload);
        }

        private static ControlPlaneStoreOperationResult<ControlPlaneStoreDetail> ValidationFailed(string message)
        {
            return new ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>(false, message, Failure: ControlPlaneStoreOperationFailure.Validation);
        }

        private static ControlPlaneStoreOperationResult<ControlPlaneStoreDetail> Conflict(string message)
        {
            return new ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>(false, message, Failure: ControlPlaneStoreOperationFailure.Conflict);
        }

        private static ControlPlaneStoreOperationResult<ControlPlaneStoreDetail> NotFound(string message)
        {
            return new ControlPlaneStoreOperationResult<ControlPlaneStoreDetail>(false, message, Failure: ControlPlaneStoreOperationFailure.NotFound);
        }

        [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{1,62}[a-z0-9])$")]
        private static partial Regex StoreKeyRegex();
    }
}
