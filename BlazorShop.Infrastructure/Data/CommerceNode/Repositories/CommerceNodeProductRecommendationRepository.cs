namespace BlazorShop.Infrastructure.Data.CommerceNode.Repositories
{
    using BlazorShop.Application.Services.Contracts.Logging;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeProductRecommendationRepository : IProductRecommendationRepository
    {
        private readonly CommerceNodeDbContext context;
        private readonly IAppLogger<CommerceNodeProductRecommendationRepository> logger;

        public CommerceNodeProductRecommendationRepository(
            CommerceNodeDbContext context,
            IAppLogger<CommerceNodeProductRecommendationRepository> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<IEnumerable<Product>> GetRelatedProductsByCategoryAsync(
            Guid productId,
            Guid categoryId,
            int count = 4)
        {
            try
            {
                this.logger.LogInformation($"Fetching {count} related products for product {productId} in category {categoryId}");
                var utcNow = DateTime.UtcNow;

                var products = await this.context.Products
                    .AsNoTracking()
                    .Include(product => product.Category)
                    .Include(product => product.Variants)
                    .Where(product =>
                        product.CategoryId == categoryId
                        && product.Id != productId
                        && product.Quantity > 0
                        && product.IsPublished
                        && product.PublishedOn != null
                        && (product.AvailableStartUtc == null || product.AvailableStartUtc <= utcNow)
                        && (product.AvailableEndUtc == null || product.AvailableEndUtc > utcNow)
                        && product.ArchivedAt == null
                        && product.Slug != null
                        && product.Slug != string.Empty
                        && product.Category != null
                        && product.Category.ArchivedAt == null
                        && product.Category.IsPublished)
                    .OrderByDescending(product => product.CreatedOn)
                    .Take(count)
                    .ToListAsync();

                this.logger.LogInformation($"Found {products.Count} related products");
                return products;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error fetching related products");
                return Enumerable.Empty<Product>();
            }
        }

        public async Task<IEnumerable<Product>> GetFrequentlyBoughtTogetherAsync(Guid productId, int count = 4)
        {
            try
            {
                this.logger.LogInformation($"Fetching frequently bought together products for product {productId}");
                var utcNow = DateTime.UtcNow;

                var relatedProductIds = await this.context.OrderLines
                    .AsNoTracking()
                    .Where(orderLine => this.context.OrderLines
                        .Any(otherLine => otherLine.OrderId == orderLine.OrderId && otherLine.ProductId == productId))
                    .Where(orderLine => orderLine.ProductId != productId)
                    .GroupBy(orderLine => orderLine.ProductId)
                    .OrderByDescending(group => group.Count())
                    .Select(group => group.Key)
                    .Take(count)
                    .ToListAsync();

                if (!relatedProductIds.Any())
                {
                    this.logger.LogInformation("No order history found, falling back to category-based recommendations");

                    var product = await this.context.Products
                        .AsNoTracking()
                        .Include(candidate => candidate.Category)
                        .FirstOrDefaultAsync(candidate =>
                            candidate.Id == productId
                            && candidate.IsPublished
                            && candidate.PublishedOn != null
                            && (candidate.AvailableStartUtc == null || candidate.AvailableStartUtc <= utcNow)
                            && (candidate.AvailableEndUtc == null || candidate.AvailableEndUtc > utcNow)
                            && candidate.ArchivedAt == null
                            && candidate.Slug != null
                            && candidate.Slug != string.Empty
                            && candidate.Category != null
                            && candidate.Category.ArchivedAt == null
                            && candidate.Category.IsPublished);

                    if (product is null)
                    {
                        this.logger.LogWarning($"Product {productId} not found");
                        return Enumerable.Empty<Product>();
                    }

                    return product.CategoryId.HasValue
                        ? await this.GetRelatedProductsByCategoryAsync(productId, product.CategoryId.Value, count)
                        : Enumerable.Empty<Product>();
                }

                var products = await this.context.Products
                    .AsNoTracking()
                    .Include(product => product.Category)
                    .Include(product => product.Variants)
                    .Where(product =>
                        relatedProductIds.Contains(product.Id)
                        && product.Quantity > 0
                        && product.IsPublished
                        && product.PublishedOn != null
                        && (product.AvailableStartUtc == null || product.AvailableStartUtc <= utcNow)
                        && (product.AvailableEndUtc == null || product.AvailableEndUtc > utcNow)
                        && product.ArchivedAt == null
                        && product.Slug != null
                        && product.Slug != string.Empty
                        && product.Category != null
                        && product.Category.ArchivedAt == null
                        && product.Category.IsPublished)
                    .ToListAsync();

                this.logger.LogInformation($"Found {products.Count} frequently bought together products");
                return products;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error fetching frequently bought together products");
                return Enumerable.Empty<Product>();
            }
        }

        public async Task<IEnumerable<Product>> GetRecentlyViewedProductsAsync(
            IEnumerable<Guid> productIds,
            int count = 4)
        {
            try
            {
                var productIdsList = productIds?.ToList() ?? [];
                if (productIdsList.Count == 0)
                {
                    this.logger.LogInformation("No product IDs provided for recently viewed");
                    return Enumerable.Empty<Product>();
                }

                this.logger.LogInformation($"Fetching {count} recently viewed products");
                var utcNow = DateTime.UtcNow;

                var products = await this.context.Products
                    .AsNoTracking()
                    .Include(product => product.Category)
                    .Include(product => product.Variants)
                    .Where(product =>
                        productIdsList.Contains(product.Id)
                        && product.Quantity > 0
                        && product.IsPublished
                        && product.PublishedOn != null
                        && (product.AvailableStartUtc == null || product.AvailableStartUtc <= utcNow)
                        && (product.AvailableEndUtc == null || product.AvailableEndUtc > utcNow)
                        && product.ArchivedAt == null
                        && product.Slug != null
                        && product.Slug != string.Empty
                        && product.Category != null
                        && product.Category.ArchivedAt == null
                        && product.Category.IsPublished)
                    .OrderByDescending(product => product.CreatedOn)
                    .Take(count)
                    .ToListAsync();

                this.logger.LogInformation($"Found {products.Count} recently viewed products");
                return products;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error fetching recently viewed products");
                return Enumerable.Empty<Product>();
            }
        }
    }
}
