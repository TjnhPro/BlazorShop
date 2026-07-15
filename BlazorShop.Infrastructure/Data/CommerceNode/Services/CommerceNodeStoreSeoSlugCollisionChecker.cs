namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeStoreSeoSlugCollisionChecker : IStoreSeoSlugCollisionChecker
    {
        private readonly CommerceNodeDbContext context;

        public CommerceNodeStoreSeoSlugCollisionChecker(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public Task<bool> SlugExistsAsync(
            string entityType,
            string slug,
            Guid? storeId,
            string? languageCode = null,
            Guid? excludedEntityId = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedEntityType = SeoSlugEntityTypes.Normalize(entityType);
            return normalizedEntityType switch
            {
                SeoSlugEntityTypes.Product => this.ProductSlugExistsAsync(slug, storeId, excludedEntityId, cancellationToken),
                SeoSlugEntityTypes.Category => this.CategorySlugExistsAsync(slug, storeId, excludedEntityId, cancellationToken),
                SeoSlugEntityTypes.Page => this.PageSlugExistsAsync(slug, storeId, excludedEntityId, cancellationToken),
                _ => Task.FromResult(false),
            };
        }

        private Task<bool> ProductSlugExistsAsync(
            string slug,
            Guid? storeId,
            Guid? excludedEntityId,
            CancellationToken cancellationToken)
        {
            return this.context.Products
                .AsNoTracking()
                .AnyAsync(
                    product =>
                        product.StoreId == storeId &&
                        product.Slug == slug &&
                        product.ArchivedAt == null &&
                        (!excludedEntityId.HasValue || product.Id != excludedEntityId.Value),
                    cancellationToken);
        }

        private Task<bool> CategorySlugExistsAsync(
            string slug,
            Guid? storeId,
            Guid? excludedEntityId,
            CancellationToken cancellationToken)
        {
            return this.context.Categories
                .AsNoTracking()
                .AnyAsync(
                    category =>
                        category.StoreId == storeId &&
                        category.Slug == slug &&
                        category.ArchivedAt == null &&
                        (!excludedEntityId.HasValue || category.Id != excludedEntityId.Value),
                    cancellationToken);
        }

        private Task<bool> PageSlugExistsAsync(
            string slug,
            Guid? storeId,
            Guid? excludedEntityId,
            CancellationToken cancellationToken)
        {
            if (!storeId.HasValue)
            {
                return Task.FromResult(false);
            }

            return this.context.StorefrontPages
                .AsNoTracking()
                .AnyAsync(
                    page =>
                        page.StoreId == storeId.Value &&
                        page.Slug == slug &&
                        page.ArchivedAt == null &&
                        (!excludedEntityId.HasValue || page.Id != excludedEntityId.Value),
                    cancellationToken);
        }
    }
}
