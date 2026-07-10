namespace BlazorShop.Infrastructure.Data.CommerceNode.Repositories
{
    using System.Linq.Expressions;

    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeProductReadRepository : IProductReadRepository
    {
        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;

        public CommerceNodeProductReadRepository(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext)
        {
            this.context = context;
            this.storeContext = storeContext;
        }

        public async Task<IEnumerable<Product>> GetCatalogProductsAsync()
        {
            return await this.context.Products
                .AsNoTracking()
                .OrderByDescending(product => product.CreatedOn)
                .Select(product => new Product
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Image = product.Image,
                    Quantity = product.Quantity,
                    CreatedOn = product.CreatedOn,
                    UpdatedAt = product.UpdatedAt,
                    Sku = product.Sku,
                    ShortDescription = product.ShortDescription,
                    FullDescription = product.FullDescription,
                    ComparePrice = product.ComparePrice,
                    DisplayOrder = product.DisplayOrder,
                    Slug = product.Slug,
                    MetaTitle = product.MetaTitle,
                    MetaDescription = product.MetaDescription,
                    CanonicalUrl = product.CanonicalUrl,
                    OgTitle = product.OgTitle,
                    OgDescription = product.OgDescription,
                    OgImage = product.OgImage,
                    RobotsIndex = product.RobotsIndex,
                    RobotsFollow = product.RobotsFollow,
                    SeoContent = product.SeoContent,
                    IsPublished = product.IsPublished,
                    PublishedOn = product.PublishedOn,
                    CategoryId = product.CategoryId,
                })
                .ToListAsync();
        }

        public async Task<PagedResult<CatalogProductReadModel>> GetCatalogPageAsync(ProductCatalogQuery query)
        {
            var pageNumber = query.GetNormalizedPageNumber();
            var pageSize = query.GetNormalizedPageSize();
            IQueryable<Product> products = BuildCatalogQuery(
                this.context.Products.AsNoTracking().Where(product => product.ArchivedAt == null),
                query);

            var totalCount = await products.CountAsync();
            var items = await products
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(MapCatalogProduct())
                .ToListAsync();

            return new PagedResult<CatalogProductReadModel>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<PagedResult<CatalogProductReadModel>> GetPublishedCatalogPageAsync(ProductCatalogQuery query)
        {
            var pageNumber = query.GetNormalizedPageNumber();
            var pageSize = query.GetNormalizedPageSize();

            var scopedProducts = await this.GetCurrentStoreProductsAsync();
            IQueryable<Product> products = BuildCatalogQuery(scopedProducts
                .Where(product => product.IsPublished
                    && product.ArchivedAt == null
                    && product.PublishedOn != null
                    && product.Slug != null
                    && product.Slug != string.Empty
                    && product.Category != null
                    && product.Category.ArchivedAt == null
                    && product.Category.IsPublished
                    && product.Category.StoreId == product.StoreId),
                query);

            var totalCount = await products.CountAsync();
            var items = await products
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(MapCatalogProduct())
                .ToListAsync();

            return new PagedResult<CatalogProductReadModel>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<IReadOnlyList<PublishedProductSitemapEntryReadModel>> GetPublishedProductSitemapEntriesAsync()
        {
            var scopedProducts = await this.GetCurrentStoreProductsAsync();
            return await scopedProducts
                .AsNoTracking()
                .Where(product => product.IsPublished
                    && product.ArchivedAt == null
                    && product.PublishedOn != null
                    && product.Slug != null
                    && product.Slug != string.Empty
                    && product.Category != null
                    && product.Category.ArchivedAt == null
                    && product.Category.IsPublished
                    && product.Category.StoreId == product.StoreId)
                .OrderBy(product => product.UpdatedAt)
                .ThenBy(product => product.Id)
                .Select(product => new PublishedProductSitemapEntryReadModel
                {
                    Slug = product.Slug!,
                    LastModifiedUtc = product.UpdatedAt != default ? product.UpdatedAt : product.PublishedOn ?? product.CreatedOn,
                })
                .ToListAsync();
        }

        public async Task<Product?> GetProductDetailsByIdAsync(Guid id)
        {
            return await this.context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .Include(product => product.Variants)
                .FirstOrDefaultAsync(product => product.Id == id);
        }

        public async Task<Product?> GetPublishedProductDetailsByIdAsync(Guid id)
        {
            var scopedProducts = await this.GetCurrentStoreProductsAsync();
            return await scopedProducts
                .AsNoTracking()
                .Include(product => product.Category)
                .Include(product => product.Variants)
                .FirstOrDefaultAsync(product => product.Id == id
                    && product.ArchivedAt == null
                    && product.IsPublished
                    && product.PublishedOn != null
                    && product.Slug != null
                    && product.Slug != string.Empty
                    && product.Category != null
                    && product.Category.ArchivedAt == null
                    && product.Category.IsPublished
                    && product.Category.StoreId == product.StoreId);
        }

        public async Task<Product?> GetPublishedProductBySlugAsync(string slug)
        {
            var scopedProducts = await this.GetCurrentStoreProductsAsync();
            return await scopedProducts
                .AsNoTracking()
                .Include(product => product.Category)
                .Include(product => product.Variants)
                .FirstOrDefaultAsync(product => product.IsPublished
                    && product.ArchivedAt == null
                    && product.PublishedOn != null
                    && product.Slug == slug
                    && product.Category != null
                    && product.Category.ArchivedAt == null
                    && product.Category.IsPublished
                    && product.Category.StoreId == product.StoreId);
        }

        public async Task<IReadOnlyList<CatalogProductReadModel>> GetPublishedProductsByCategoryAsync(Guid categoryId)
        {
            var scopedProducts = await this.GetCurrentStoreProductsAsync();
            return await scopedProducts
                .AsNoTracking()
                .Where(product => product.CategoryId == categoryId
                    && product.ArchivedAt == null
                    && product.IsPublished
                    && product.PublishedOn != null
                    && product.Slug != null
                    && product.Slug != string.Empty
                    && product.Category != null
                    && product.Category.ArchivedAt == null
                    && product.Category.IsPublished
                    && product.Category.StoreId == product.StoreId)
                .OrderBy(product => product.DisplayOrder)
                .ThenByDescending(product => product.CreatedOn)
                .ThenBy(product => product.Id)
                .Select(MapCatalogProduct())
                .ToListAsync();
        }

        public async Task<bool> ProductSlugExistsAsync(string slug, Guid? excludedProductId = null)
        {
            return await this.context.Products
                .AsNoTracking()
                .AnyAsync(product => product.Slug == slug
                    && product.ArchivedAt == null
                    && (!excludedProductId.HasValue || product.Id != excludedProductId.Value));
        }

        public async Task<bool> ProductSkuExistsAsync(string sku, Guid? storeId, Guid? excludedProductId = null)
        {
            return await this.context.Products
                .AsNoTracking()
                .AnyAsync(product => product.Sku == sku
                    && product.StoreId == storeId
                    && product.ArchivedAt == null
                    && (!excludedProductId.HasValue || product.Id != excludedProductId.Value));
        }

        public async Task<IReadOnlyDictionary<Guid, Product>> GetProductsByIdsAsync(IEnumerable<Guid> productIds)
        {
            var ids = productIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();

            if (ids.Length == 0)
            {
                return new Dictionary<Guid, Product>();
            }

            return await this.context.Products
                .AsNoTracking()
                .Include(product => product.Variants)
                .Where(product => ids.Contains(product.Id))
                .ToDictionaryAsync(product => product.Id);
        }

        private static IQueryable<Product> BuildCatalogQuery(IQueryable<Product> products, ProductCatalogQuery query)
        {
            var searchTerm = query.GetNormalizedSearchTerm();

            if (query.CategoryId.HasValue && query.CategoryId.Value != Guid.Empty)
            {
                products = products.Where(product => product.CategoryId == query.CategoryId.Value);
            }

            if (query.CreatedAfterUtc.HasValue)
            {
                products = products.Where(product => product.CreatedOn >= query.CreatedAfterUtc.Value);
            }

            if (query.MinPrice.HasValue)
            {
                products = products.Where(product => product.Price >= query.MinPrice.Value);
            }

            if (query.MaxPrice.HasValue)
            {
                products = products.Where(product => product.Price <= query.MaxPrice.Value);
            }

            if (query.InStock.HasValue)
            {
                products = query.InStock.Value
                    ? products.Where(product => product.Quantity > 0 || product.Variants.Any(variant => variant.Stock > 0))
                    : products.Where(product => product.Quantity <= 0 && !product.Variants.Any(variant => variant.Stock > 0));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalizedSearchTerm = searchTerm.ToLower();
                products = products.Where(product =>
                    (product.Sku != null && product.Sku.ToLower().Contains(normalizedSearchTerm)) ||
                    (product.Name != null && product.Name.ToLower().Contains(normalizedSearchTerm)) ||
                    (product.Description != null && product.Description.ToLower().Contains(normalizedSearchTerm)));
            }

            return query.SortBy switch
            {
                ProductCatalogSortBy.Oldest => products.OrderBy(product => product.CreatedOn).ThenBy(product => product.Id),
                ProductCatalogSortBy.PriceLowToHigh => products.OrderBy(product => product.Price).ThenBy(product => product.Id),
                ProductCatalogSortBy.PriceHighToLow => products.OrderByDescending(product => product.Price).ThenBy(product => product.Id),
                ProductCatalogSortBy.NameAscending => products.OrderBy(product => product.Name).ThenBy(product => product.Id),
                ProductCatalogSortBy.NameDescending => products.OrderByDescending(product => product.Name).ThenBy(product => product.Id),
                ProductCatalogSortBy.DisplayOrder => products.OrderBy(product => product.DisplayOrder).ThenByDescending(product => product.CreatedOn).ThenBy(product => product.Id),
                ProductCatalogSortBy.Updated => products.OrderByDescending(product => product.UpdatedAt).ThenBy(product => product.Id),
                _ => products.OrderByDescending(product => product.CreatedOn).ThenBy(product => product.Id),
            };
        }

        private static Expression<Func<Product, CatalogProductReadModel>> MapCatalogProduct()
        {
            return product => new CatalogProductReadModel
            {
                Id = product.Id,
                Slug = product.IsPublished ? product.Slug : null,
                Name = product.Name,
                Description = product.Description,
                Sku = product.Sku,
                ShortDescription = product.ShortDescription,
                Price = product.Price,
                ComparePrice = product.ComparePrice,
                Image = product.Image,
                CreatedOn = product.CreatedOn,
                UpdatedAt = product.UpdatedAt,
                DisplayOrder = product.DisplayOrder,
                InStock = product.Quantity > 0 || product.Variants.Any(variant => variant.Stock > 0),
                CategoryId = product.CategoryId,
                CategoryName = product.Category != null ? product.Category.Name : null,
                CategorySlug = product.Category != null && product.Category.IsPublished ? product.Category.Slug : null,
                HasVariants = product.Variants.Any(),
            };
        }

        private async Task<IQueryable<Product>> GetCurrentStoreProductsAsync()
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync();
            if (!storeResult.Success)
            {
                return this.context.Products.Where(product => false);
            }

            var storeId = storeResult.Payload;
            return this.context.Products
                .AsNoTracking()
                .Where(product => product.StoreId == storeId);
        }
    }
}
