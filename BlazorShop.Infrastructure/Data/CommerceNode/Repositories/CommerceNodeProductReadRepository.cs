namespace BlazorShop.Infrastructure.Data.CommerceNode.Repositories
{
    using System.Linq.Expressions;

    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeProductReadRepository : IProductReadRepository
    {
        private const int StorefrontMaxPageCount = 10;
        private const string SearchConfig = "simple";

        private readonly CommerceNodeDbContext context;
        private readonly ISlugService slugService;
        private readonly ICommerceStoreContext storeContext;

        public CommerceNodeProductReadRepository(
            CommerceNodeDbContext context,
            ISlugService slugService,
            ICommerceStoreContext storeContext)
        {
            this.context = context;
            this.slugService = slugService;
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
                    AvailableStartUtc = product.AvailableStartUtc,
                    AvailableEndUtc = product.AvailableEndUtc,
                    ProductType = product.ProductType,
                    VariationTemplateId = product.VariationTemplateId,
                    CategoryId = product.CategoryId,
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetCatalogProductsForCurrentStoreAsync()
        {
            var scopedProducts = await this.GetCurrentStoreProductsAsync();
            return await scopedProducts
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
                    ProductType = product.ProductType,
                    VariationTemplateId = product.VariationTemplateId,
                    CategoryId = product.CategoryId,
                    StoreId = product.StoreId,
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
            items = await this.AttachPrimaryMediaAsync(items);

            return new PagedResult<CatalogProductReadModel>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<PagedResult<CatalogProductReadModel>> GetCatalogPageForCurrentStoreAsync(ProductCatalogQuery query)
        {
            var pageNumber = query.GetNormalizedPageNumber();
            var pageSize = query.GetNormalizedPageSize();
            var scopedProducts = await this.GetCurrentStoreProductsAsync();
            IQueryable<Product> products = BuildCatalogQuery(
                scopedProducts.AsNoTracking().Where(product => product.ArchivedAt == null),
                query);

            var totalCount = await products.CountAsync();
            var items = await products
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(MapCatalogProduct())
                .ToListAsync();
            items = await this.AttachPrimaryMediaAsync(items);

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
            var requestedPageNumber = query.GetNormalizedPageNumber();
            var pageSize = query.GetNormalizedPageSize();

            var storeResult = await this.storeContext.GetCurrentStoreIdAsync();
            if (!storeResult.Success)
            {
                return CreateEmptyPagedResult<CatalogProductReadModel>(1, pageSize);
            }

            var storeId = storeResult.Payload;
            var categoryIds = await this.GetPublishedCategoryIdsForQueryAsync(storeId, query);
            if (categoryIds is not null && categoryIds.Count == 0)
            {
                return CreateEmptyPagedResult<CatalogProductReadModel>(1, pageSize);
            }

            IQueryable<Product> products = ApplyPublicVisibility(
                this.context.Products
                    .AsNoTracking()
                    .Where(product => product.StoreId == storeId),
                DateTime.UtcNow);

            products = BuildPublishedCatalogQuery(products, query, categoryIds);

            var totalCount = await products.CountAsync();
            var cappedTotalCount = Math.Min(totalCount, pageSize * StorefrontMaxPageCount);
            var maxPageNumber = Math.Max(1, (int)Math.Ceiling((double)cappedTotalCount / pageSize));
            var pageNumber = Math.Min(requestedPageNumber, maxPageNumber);

            var items = await products
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(MapCatalogProduct())
                .ToListAsync();
            items = await this.AttachPrimaryMediaAsync(items);

            return new PagedResult<CatalogProductReadModel>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = cappedTotalCount,
            };
        }

        public async Task<IReadOnlyList<PublishedProductSitemapEntryReadModel>> GetPublishedProductSitemapEntriesAsync()
        {
            var scopedProducts = await this.GetCurrentStoreProductsAsync();
            return await ApplyPublicVisibility(scopedProducts.AsNoTracking(), DateTime.UtcNow)
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
                .Include(product => product.VariationTemplate!)
                    .ThenInclude(template => template.Options)
                    .ThenInclude(option => option.Values)
                .FirstOrDefaultAsync(product => product.Id == id);
        }

        public async Task<Product?> GetProductDetailsByIdForCurrentStoreAsync(Guid id)
        {
            var scopedProducts = await this.GetCurrentStoreProductsAsync();
            return await scopedProducts
                .AsNoTracking()
                .Include(product => product.Category)
                .Include(product => product.Variants)
                .Include(product => product.VariationTemplate!)
                    .ThenInclude(template => template.Options)
                    .ThenInclude(option => option.Values)
                .FirstOrDefaultAsync(product => product.Id == id);
        }

        public async Task<Product?> GetPublishedProductDetailsByIdAsync(Guid id)
        {
            var scopedProducts = await this.GetCurrentStoreProductsAsync();
            return await ApplyPublicVisibility(
                scopedProducts
                .AsNoTracking()
                .Include(product => product.Category)
                .Include(product => product.Variants)
                .Include(product => product.VariationTemplate!)
                    .ThenInclude(template => template.Options)
                    .ThenInclude(option => option.Values),
                DateTime.UtcNow)
                .FirstOrDefaultAsync(product => product.Id == id);
        }

        public async Task<Product?> GetPublishedProductBySlugAsync(string slug)
        {
            var scopedProducts = await this.GetCurrentStoreProductsAsync();
            return await ApplyPublicVisibility(
                scopedProducts
                .AsNoTracking()
                .Include(product => product.Category)
                .Include(product => product.Variants)
                .Include(product => product.VariationTemplate!)
                    .ThenInclude(template => template.Options)
                    .ThenInclude(option => option.Values),
                DateTime.UtcNow)
                .FirstOrDefaultAsync(product => product.Slug == slug);
        }

        public async Task<IReadOnlyList<CatalogProductReadModel>> GetPublishedProductsByCategoryAsync(Guid categoryId)
        {
            var scopedProducts = await this.GetCurrentStoreProductsAsync();
            var items = await ApplyPublicVisibility(scopedProducts.AsNoTracking(), DateTime.UtcNow)
                .Where(product => product.CategoryId == categoryId)
                .OrderBy(product => product.DisplayOrder)
                .ThenByDescending(product => product.CreatedOn)
                .ThenBy(product => product.Id)
                .Select(MapCatalogProduct())
                .ToListAsync();
            return await this.AttachPrimaryMediaAsync(items);
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

            var scopedProducts = await this.GetCurrentStoreProductsAsync();
            return await ApplyPublicVisibility(scopedProducts.AsNoTracking(), DateTime.UtcNow)
                .Where(product => product.CategoryId.HasValue
                    && ids.Contains(product.CategoryId.Value)
                    && product.Category != null)
                .CountAsync();
        }

        public async Task<bool> ProductSlugExistsAsync(string slug, Guid? excludedProductId = null)
        {
            return await this.context.Products
                .AsNoTracking()
                .AnyAsync(product => product.Slug == slug
                    && product.ArchivedAt == null
                    && (!excludedProductId.HasValue || product.Id != excludedProductId.Value));
        }

        public async Task<bool> ProductSlugExistsInStoreAsync(string slug, Guid? storeId, Guid? excludedProductId = null)
        {
            return await this.context.Products
                .AsNoTracking()
                .AnyAsync(product => product.Slug == slug
                    && product.StoreId == storeId
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

            if (query.IsPublished.HasValue)
            {
                products = products.Where(product => product.IsPublished == query.IsPublished.Value);
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
                && product.Category.IsPublished
                && product.Category.StoreId == product.StoreId);
        }

        private static IQueryable<Product> BuildPublishedCatalogQuery(
            IQueryable<Product> products,
            ProductCatalogQuery query,
            IReadOnlyCollection<Guid>? categoryIds)
        {
            var searchTerm = query.GetNormalizedSearchTerm();

            if (categoryIds is { Count: > 0 })
            {
                products = products.Where(product => product.CategoryId.HasValue && categoryIds.Contains(product.CategoryId.Value));
            }
            else if (query.CategoryId.HasValue && query.CategoryId.Value != Guid.Empty)
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
                return products
                    .Where(product => EF.Functions
                        .ToTsVector(SearchConfig, product.Name ?? string.Empty)
                        .Matches(EF.Functions.PlainToTsQuery(SearchConfig, searchTerm)))
                    .OrderByDescending(product => EF.Functions
                        .ToTsVector(SearchConfig, product.Name ?? string.Empty)
                        .Rank(EF.Functions.PlainToTsQuery(SearchConfig, searchTerm)))
                    .ThenBy(product => product.Id);
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

        private async Task<IReadOnlyCollection<Guid>?> GetPublishedCategoryIdsForQueryAsync(Guid storeId, ProductCatalogQuery query)
        {
            var categorySlug = query.GetNormalizedCategorySlug();
            var hasCategoryId = query.CategoryId.HasValue && query.CategoryId.Value != Guid.Empty;
            if (string.IsNullOrWhiteSpace(categorySlug) && (!hasCategoryId || !query.IncludeSubcategories))
            {
                return null;
            }

            var categories = await this.context.Categories
                .AsNoTracking()
                .Where(category => category.StoreId == storeId
                    && category.ArchivedAt == null
                    && category.IsPublished
                    && category.Slug != null
                    && category.Slug != string.Empty)
                .Select(category => new CategoryScopeNode(category.Id, category.ParentCategoryId, category.Slug!))
                .ToListAsync();

            CategoryScopeNode? root;
            if (!string.IsNullOrWhiteSpace(categorySlug))
            {
                var normalizedSlug = this.slugService.NormalizeSlug(categorySlug);
                if (string.IsNullOrWhiteSpace(normalizedSlug))
                {
                    return [];
                }

                root = categories.FirstOrDefault(category => category.Slug == normalizedSlug);
            }
            else
            {
                root = categories.FirstOrDefault(category => category.Id == query.CategoryId!.Value);
            }

            if (root is null)
            {
                return [];
            }

            return query.IncludeSubcategories
                ? CollectDescendantCategoryIds(root.Id, categories)
                : [root.Id];
        }

        private static IReadOnlyCollection<Guid> CollectDescendantCategoryIds(Guid rootCategoryId, IReadOnlyList<CategoryScopeNode> categories)
        {
            var categoryIds = new HashSet<Guid> { rootCategoryId };
            var pending = new Queue<Guid>();
            pending.Enqueue(rootCategoryId);

            while (pending.Count > 0)
            {
                var currentId = pending.Dequeue();
                foreach (var child in categories.Where(category => category.ParentCategoryId == currentId))
                {
                    if (categoryIds.Add(child.Id))
                    {
                        pending.Enqueue(child.Id);
                    }
                }
            }

            return categoryIds;
        }

        private static PagedResult<T> CreateEmptyPagedResult<T>(int pageNumber, int pageSize)
        {
            return new PagedResult<T>
            {
                Items = [],
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = 0,
            };
        }

        private static Expression<Func<Product, CatalogProductReadModel>> MapCatalogProduct()
        {
            return product => new CatalogProductReadModel
            {
                Id = product.Id,
                Slug = product.Slug,
                Name = product.Name,
                Description = product.Description,
                Sku = product.Sku,
                Gtin = product.Gtin,
                Barcode = product.Barcode,
                ManufacturerPartNumber = product.ManufacturerPartNumber,
                Condition = product.Condition,
                Weight = product.Weight,
                Length = product.Length,
                Width = product.Width,
                Height = product.Height,
                ShortDescription = product.ShortDescription,
                Price = product.Price,
                ComparePrice = product.ComparePrice,
                Image = product.Image,
                CreatedOn = product.CreatedOn,
                UpdatedAt = product.UpdatedAt,
                DisplayOrder = product.DisplayOrder,
                InStock = product.Quantity > 0 || product.Variants.Any(variant => variant.Stock > 0),
                MinOrderQuantity = product.MinOrderQuantity,
                MaxOrderQuantity = product.MaxOrderQuantity,
                QuantityStep = product.QuantityStep,
                PurchasingDisabled = product.PurchasingDisabled,
                PurchasingDisabledReason = product.PurchasingDisabledReason,
                ManageStock = product.ManageStock,
                HideWhenOutOfStock = product.HideWhenOutOfStock,
                ShippingRequired = product.ShippingRequired,
                FreeShipping = product.FreeShipping,
                DeliveryEstimateText = product.DeliveryEstimateText,
                IsPublished = product.IsPublished,
                PublishedOn = product.PublishedOn,
                AvailableStartUtc = product.AvailableStartUtc,
                AvailableEndUtc = product.AvailableEndUtc,
                CategoryId = product.CategoryId,
                CategoryName = product.Category != null ? product.Category.Name : null,
                CategorySlug = product.Category != null && product.Category.IsPublished ? product.Category.Slug : null,
                HasVariants = product.Variants.Any(),
                ProductType = product.ProductType,
                VariationTemplateId = product.VariationTemplateId,
            };
        }

        private async Task<List<CatalogProductReadModel>> AttachPrimaryMediaAsync(List<CatalogProductReadModel> products)
        {
            var productIds = products.Select(product => product.Id).Distinct().ToArray();
            if (productIds.Length == 0)
            {
                return products;
            }

            var primaryMedia = await this.context.ProductMedia
                .AsNoTracking()
                .Where(media => productIds.Contains(media.ProductId)
                    && media.IsPrimary
                    && media.Status == ProductMediaStatuses.Stored
                    && media.DeletedAt == null)
                .GroupBy(media => media.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    MediaPublicId = group
                        .OrderBy(media => media.SortOrder)
                        .ThenBy(media => media.CreatedAt)
                        .Select(media => media.PublicId)
                        .FirstOrDefault(),
                })
                .ToDictionaryAsync(media => media.ProductId, media => media.MediaPublicId);

            return products
                .Select(product => primaryMedia.TryGetValue(product.Id, out var mediaPublicId) && mediaPublicId != Guid.Empty
                    ? new CatalogProductReadModel
                    {
                        Id = product.Id,
                        Slug = product.Slug,
                        Name = product.Name,
                        Description = product.Description,
                        Sku = product.Sku,
                        Gtin = product.Gtin,
                        Barcode = product.Barcode,
                        ManufacturerPartNumber = product.ManufacturerPartNumber,
                        Condition = product.Condition,
                        Weight = product.Weight,
                        Length = product.Length,
                        Width = product.Width,
                        Height = product.Height,
                        ShortDescription = product.ShortDescription,
                        Price = product.Price,
                        ComparePrice = product.ComparePrice,
                        Image = product.Image,
                        PrimaryMediaPublicId = mediaPublicId,
                        HasPrimaryMedia = true,
                        CreatedOn = product.CreatedOn,
                        UpdatedAt = product.UpdatedAt,
                        DisplayOrder = product.DisplayOrder,
                        InStock = product.InStock,
                        MinOrderQuantity = product.MinOrderQuantity,
                        MaxOrderQuantity = product.MaxOrderQuantity,
                        QuantityStep = product.QuantityStep,
                        PurchasingDisabled = product.PurchasingDisabled,
                        PurchasingDisabledReason = product.PurchasingDisabledReason,
                        ManageStock = product.ManageStock,
                        HideWhenOutOfStock = product.HideWhenOutOfStock,
                        ShippingRequired = product.ShippingRequired,
                        FreeShipping = product.FreeShipping,
                        DeliveryEstimateText = product.DeliveryEstimateText,
                        IsPublished = product.IsPublished,
                        PublishedOn = product.PublishedOn,
                        AvailableStartUtc = product.AvailableStartUtc,
                        AvailableEndUtc = product.AvailableEndUtc,
                        CategoryId = product.CategoryId,
                        CategoryName = product.CategoryName,
                        CategorySlug = product.CategorySlug,
                        HasVariants = product.HasVariants,
                        ProductType = product.ProductType,
                        VariationTemplateId = product.VariationTemplateId,
                    }
                    : product)
                .ToList();
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

        private sealed record CategoryScopeNode(Guid Id, Guid? ParentCategoryId, string Slug);
    }
}
