namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class StoreSeoSlugHistoryService : IStoreSeoSlugHistoryService
    {
        private readonly CommerceNodeDbContext context;

        public StoreSeoSlugHistoryService(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<StoreSeoSlugHistoryDto?> GetActiveSlugAsync(
            string entityType,
            Guid entityId,
            Guid storeId,
            string? languageCode = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedEntityType = SeoSlugEntityTypes.Normalize(entityType);
            if (!SeoSlugEntityTypes.IsKnown(normalizedEntityType) || entityId == Guid.Empty || storeId == Guid.Empty)
            {
                return null;
            }

            var normalizedLanguageCode = NormalizeLanguageCode(languageCode);
            var history = await this.FindActiveEntityHistoryAsync(
                normalizedEntityType,
                entityId,
                storeId,
                normalizedLanguageCode,
                asTracking: false,
                cancellationToken);

            return history is null ? null : Map(history);
        }

        public async Task<ServiceResponse<StoreSeoSlugHistoryDto>> RecordInitialActiveSlugAsync(
            string entityType,
            Guid entityId,
            Guid storeId,
            string slug,
            string? languageCode = null,
            CancellationToken cancellationToken = default)
        {
            var validation = ValidateInput(entityType, entityId, storeId, slug);
            if (validation is not null)
            {
                return Failure(validation, ServiceResponseType.ValidationError);
            }

            var normalizedEntityType = SeoSlugEntityTypes.Normalize(entityType);
            var normalizedLanguageCode = NormalizeLanguageCode(languageCode);
            var active = await this.FindActiveEntityHistoryAsync(
                normalizedEntityType,
                entityId,
                storeId,
                normalizedLanguageCode,
                asTracking: false,
                cancellationToken);

            if (active is not null)
            {
                return string.Equals(active.Slug, slug, StringComparison.Ordinal)
                    ? Success(Map(active), "Active slug history already exists.")
                    : Failure("Active slug history already exists for this entity.", ServiceResponseType.Conflict);
            }

            if (await this.ActiveSlugExistsForAnotherEntityAsync(normalizedEntityType, slug, storeId, normalizedLanguageCode, entityId, cancellationToken))
            {
                return Failure("Active slug history already exists for this route family.", ServiceResponseType.Conflict);
            }

            var history = new StoreSeoSlugHistory
            {
                StoreId = storeId,
                EntityType = normalizedEntityType,
                EntityId = entityId,
                Slug = slug,
                LanguageCode = normalizedLanguageCode,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            this.context.StoreSeoSlugHistories.Add(history);
            await this.context.SaveChangesAsync(cancellationToken);

            return Success(Map(history), "Active slug history recorded.");
        }

        public async Task<ServiceResponse<StoreSeoSlugHistoryDto>> ReplaceActiveSlugAsync(
            string entityType,
            Guid entityId,
            Guid storeId,
            string newSlug,
            string? languageCode = null,
            CancellationToken cancellationToken = default)
        {
            var validation = ValidateInput(entityType, entityId, storeId, newSlug);
            if (validation is not null)
            {
                return Failure(validation, ServiceResponseType.ValidationError);
            }

            var normalizedEntityType = SeoSlugEntityTypes.Normalize(entityType);
            var normalizedLanguageCode = NormalizeLanguageCode(languageCode);
            var active = await this.FindActiveEntityHistoryAsync(
                normalizedEntityType,
                entityId,
                storeId,
                normalizedLanguageCode,
                asTracking: true,
                cancellationToken);

            if (active is not null && string.Equals(active.Slug, newSlug, StringComparison.Ordinal))
            {
                return Success(Map(active), "Active slug history already matches.");
            }

            if (await this.ActiveSlugExistsForAnotherEntityAsync(normalizedEntityType, newSlug, storeId, normalizedLanguageCode, entityId, cancellationToken))
            {
                return Failure("Active slug history already exists for this route family.", ServiceResponseType.Conflict);
            }

            var now = DateTimeOffset.UtcNow;
            if (active is not null)
            {
                active.IsActive = false;
                active.ReplacedAt = now;
                active.ReplacedBySlug = newSlug;
            }

            var replacement = new StoreSeoSlugHistory
            {
                StoreId = storeId,
                EntityType = normalizedEntityType,
                EntityId = entityId,
                Slug = newSlug,
                LanguageCode = normalizedLanguageCode,
                IsActive = true,
                CreatedAt = now,
            };

            this.context.StoreSeoSlugHistories.Add(replacement);
            await this.context.SaveChangesAsync(cancellationToken);

            return Success(Map(replacement), "Active slug history replaced.");
        }

        public async Task<IReadOnlyList<StoreSeoSlugHistoryDto>> ListHistoryAsync(
            string entityType,
            Guid entityId,
            Guid storeId,
            string? languageCode = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedEntityType = SeoSlugEntityTypes.Normalize(entityType);
            if (!SeoSlugEntityTypes.IsKnown(normalizedEntityType) || entityId == Guid.Empty || storeId == Guid.Empty)
            {
                return [];
            }

            var normalizedLanguageCode = NormalizeLanguageCode(languageCode);
            return await this.context.StoreSeoSlugHistories
                .AsNoTracking()
                .Where(history =>
                    history.StoreId == storeId &&
                    history.EntityType == normalizedEntityType &&
                    history.EntityId == entityId &&
                    history.LanguageCode == normalizedLanguageCode)
                .OrderByDescending(history => history.IsActive)
                .ThenByDescending(history => history.CreatedAt)
                .Select(history => Map(history))
                .ToListAsync(cancellationToken);
        }

        public async Task<ServiceResponse<StoreSeoSlugBackfillResultDto>> BackfillCurrentSlugsAsync(
            CancellationToken cancellationToken = default)
        {
            var candidates = new List<BackfillCandidate>();

            candidates.AddRange(await this.context.Products
                .AsNoTracking()
                .Where(product => product.StoreId.HasValue && product.ArchivedAt == null && product.Slug != null)
                .Select(product => new BackfillCandidate(product.StoreId!.Value, SeoSlugEntityTypes.Product, product.Id, product.Slug!))
                .ToListAsync(cancellationToken));

            candidates.AddRange(await this.context.Categories
                .AsNoTracking()
                .Where(category => category.StoreId.HasValue && category.ArchivedAt == null && category.Slug != null)
                .Select(category => new BackfillCandidate(category.StoreId!.Value, SeoSlugEntityTypes.Category, category.Id, category.Slug!))
                .ToListAsync(cancellationToken));

            candidates.AddRange(await this.context.StorefrontPages
                .AsNoTracking()
                .Where(page => page.ArchivedAt == null && page.Slug != null)
                .Select(page => new BackfillCandidate(page.StoreId, SeoSlugEntityTypes.Page, page.Id, page.Slug))
                .ToListAsync(cancellationToken));

            var created = 0;
            var skipped = 0;

            foreach (var candidate in candidates)
            {
                var activeExists = await this.FindActiveEntityHistoryAsync(
                    candidate.EntityType,
                    candidate.EntityId,
                    candidate.StoreId,
                    languageCode: null,
                    asTracking: false,
                    cancellationToken) is not null;

                if (activeExists)
                {
                    skipped++;
                    continue;
                }

                this.context.StoreSeoSlugHistories.Add(new StoreSeoSlugHistory
                {
                    StoreId = candidate.StoreId,
                    EntityType = candidate.EntityType,
                    EntityId = candidate.EntityId,
                    Slug = candidate.Slug,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                });
                created++;
            }

            if (created > 0)
            {
                await this.context.SaveChangesAsync(cancellationToken);
            }

            return new ServiceResponse<StoreSeoSlugBackfillResultDto>(true, "SEO slug history backfill completed.")
            {
                Payload = new StoreSeoSlugBackfillResultDto(created, skipped),
                ResponseType = ServiceResponseType.Success,
            };
        }

        private Task<StoreSeoSlugHistory?> FindActiveEntityHistoryAsync(
            string entityType,
            Guid entityId,
            Guid storeId,
            string? languageCode,
            bool asTracking,
            CancellationToken cancellationToken)
        {
            var query = asTracking ? this.context.StoreSeoSlugHistories : this.context.StoreSeoSlugHistories.AsNoTracking();
            return query.FirstOrDefaultAsync(
                history =>
                    history.StoreId == storeId &&
                    history.EntityType == entityType &&
                    history.EntityId == entityId &&
                    history.LanguageCode == languageCode &&
                    history.IsActive,
                cancellationToken);
        }

        private Task<bool> ActiveSlugExistsForAnotherEntityAsync(
            string entityType,
            string slug,
            Guid storeId,
            string? languageCode,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            return this.context.StoreSeoSlugHistories
                .AsNoTracking()
                .AnyAsync(
                    history =>
                        history.StoreId == storeId &&
                        history.EntityType == entityType &&
                        history.Slug == slug &&
                        history.LanguageCode == languageCode &&
                        history.IsActive &&
                        history.EntityId != entityId,
                    cancellationToken);
        }

        private static string? ValidateInput(string entityType, Guid entityId, Guid storeId, string slug)
        {
            if (!SeoSlugEntityTypes.IsKnown(entityType))
            {
                return "SEO slug entity type is not supported.";
            }

            if (entityId == Guid.Empty)
            {
                return "Entity id is required.";
            }

            if (storeId == Guid.Empty)
            {
                return "Store id is required.";
            }

            return string.IsNullOrWhiteSpace(slug) ? "Slug is required." : null;
        }

        private static string? NormalizeLanguageCode(string? languageCode)
        {
            return string.IsNullOrWhiteSpace(languageCode) ? null : languageCode.Trim().ToLowerInvariant();
        }

        private static StoreSeoSlugHistoryDto Map(StoreSeoSlugHistory history)
        {
            return new StoreSeoSlugHistoryDto(
                history.Id,
                history.StoreId,
                history.EntityType,
                history.EntityId,
                history.Slug,
                history.LanguageCode,
                history.IsActive,
                history.CreatedAt,
                history.ReplacedAt,
                history.ReplacedBySlug);
        }

        private static ServiceResponse<StoreSeoSlugHistoryDto> Success(StoreSeoSlugHistoryDto payload, string message)
        {
            return new ServiceResponse<StoreSeoSlugHistoryDto>(true, message, payload.Id)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<StoreSeoSlugHistoryDto> Failure(string message, ServiceResponseType responseType)
        {
            return new ServiceResponse<StoreSeoSlugHistoryDto>(false, message)
            {
                ResponseType = responseType,
            };
        }

        private sealed record BackfillCandidate(Guid StoreId, string EntityType, Guid EntityId, string Slug);
    }
}
