namespace BlazorShop.Infrastructure.Repositories
{
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Infrastructure.Data;

    using Microsoft.EntityFrameworkCore;

    public class ProductReadRepository : IProductReadRepository
    {
        private readonly AppDbContext _context;

        public ProductReadRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetCatalogProductsAsync()
        {
            return await _context.Products
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
                    AvailableStartUtc = product.AvailableStartUtc,
                    AvailableEndUtc = product.AvailableEndUtc,
                    CategoryId = product.CategoryId,
                })
                .ToListAsync();
        }

        public Task<IEnumerable<Product>> GetCatalogProductsForCurrentStoreAsync()
        {
            return GetCatalogProductsAsync();
        }

        public async Task<PagedResult<CatalogProductReadModel>> GetCatalogPageAsync(ProductCatalogQuery query)
        {
            var pageNumber = query.GetNormalizedPageNumber();
            var pageSize = query.GetNormalizedPageSize();
            IQueryable<Product> products = BuildCatalogQuery(
                _context.Products.AsNoTracking().Where(product => product.ArchivedAt == null),
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

        public Task<PagedResult<CatalogProductReadModel>> GetCatalogPageForCurrentStoreAsync(ProductCatalogQuery query)
        {
            return GetCatalogPageAsync(query);
        }

        public async Task<PagedResult<CatalogProductReadModel>> GetPublishedCatalogPageAsync(ProductCatalogQuery query)
        {
            var pageNumber = query.GetNormalizedPageNumber();
            var pageSize = query.GetNormalizedPageSize();

            IQueryable<Product> products = BuildCatalogQuery(
                ApplyPublicVisibility(_context.Products.AsNoTracking(), DateTime.UtcNow),
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
            return await ApplyPublicVisibility(_context.Products.AsNoTracking(), DateTime.UtcNow)
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
            return await _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .Include(product => product.Variants)
                .FirstOrDefaultAsync(product => product.Id == id);
        }

        public Task<Product?> GetProductDetailsByIdForCurrentStoreAsync(Guid id)
        {
            return GetProductDetailsByIdAsync(id);
        }

        public async Task<Product?> GetPublishedProductDetailsByIdAsync(Guid id)
        {
            return await ApplyPublicVisibility(
                _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .Include(product => product.Variants),
                DateTime.UtcNow)
                .FirstOrDefaultAsync(product => product.Id == id);
        }

        public async Task<Product?> GetPublishedProductBySlugAsync(string slug)
        {
            return await ApplyPublicVisibility(
                _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .Include(product => product.Variants),
                DateTime.UtcNow)
                .FirstOrDefaultAsync(product => product.Slug == slug);
        }

        public async Task<IReadOnlyList<CatalogProductReadModel>> GetPublishedProductsByCategoryAsync(Guid categoryId)
        {
            return await ApplyPublicVisibility(_context.Products.AsNoTracking(), DateTime.UtcNow)
                .Where(product => product.CategoryId == categoryId)
                .OrderBy(product => product.DisplayOrder)
                .ThenByDescending(product => product.CreatedOn)
                .ThenBy(product => product.Id)
                .Select(MapCatalogProduct())
                .ToListAsync();
        }

        public async Task<int> CountPublishedProductsByCategoryIdsAsync(IReadOnlyCollection<Guid> categoryIds)
        {
            var ids = categoryIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();
            if (ids.Length == 0)
            {
                return 0;
            }

            return await ApplyPublicVisibility(_context.Products.AsNoTracking(), DateTime.UtcNow)
                .Where(product => product.CategoryId.HasValue
                    && ids.Contains(product.CategoryId.Value)
                    && product.Category != null)
                .CountAsync();
        }

        public async Task<bool> ProductSlugExistsAsync(string slug, Guid? excludedProductId = null)
        {
            return await _context.Products
                .AsNoTracking()
                .AnyAsync(product => product.Slug == slug
                    && product.ArchivedAt == null
                    && (!excludedProductId.HasValue || product.Id != excludedProductId.Value));
        }

        public async Task<bool> ProductSlugExistsInStoreAsync(string slug, Guid? storeId, Guid? excludedProductId = null)
        {
            return await _context.Products
                .AsNoTracking()
                .AnyAsync(product => product.Slug == slug
                    && product.StoreId == storeId
                    && product.ArchivedAt == null
                    && (!excludedProductId.HasValue || product.Id != excludedProductId.Value));
        }

        public async Task<bool> ProductSkuExistsAsync(string sku, Guid? storeId, Guid? excludedProductId = null)
        {
            return await _context.Products
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

            return await _context.Products
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

        private static IQueryable<Product> ApplyPublicVisibility(IQueryable<Product> products, DateTime utcNow)
        {
            return products.Where(product => product.IsPublished
                && product.ArchivedAt == null
                && product.PublishedOn != null
                && (product.AvailableStartUtc == null || product.AvailableStartUtc <= utcNow)
                && (product.AvailableEndUtc == null || product.AvailableEndUtc > utcNow)
                && product.Slug != null
                && product.Slug != string.Empty
                && product.Category != null
                && product.Category.ArchivedAt == null
                && product.Category.IsPublished);
        }

        private static System.Linq.Expressions.Expression<Func<Product, CatalogProductReadModel>> MapCatalogProduct()
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
                IsPublished = product.IsPublished,
                PublishedOn = product.PublishedOn,
                AvailableStartUtc = product.AvailableStartUtc,
                AvailableEndUtc = product.AvailableEndUtc,
                CategoryId = product.CategoryId,
                CategoryName = product.Category != null ? product.Category.Name : null,
                CategorySlug = product.Category != null && product.Category.IsPublished ? product.Category.Slug : null,
                HasVariants = product.Variants.Any(),
            };
        }
    }
}
