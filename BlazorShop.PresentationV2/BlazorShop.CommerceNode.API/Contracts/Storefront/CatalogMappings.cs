namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.IdentityModel.Tokens.Jwt;

    using BlazorShop.Application.CommerceNode.Addresses;
    using BlazorShop.Application.CommerceNode.Captcha;
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Consent;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Domain.Contracts;
    public static partial class StorefrontContractMappings
    {
        public static ProductCatalogQuery ToApplicationQuery(this StorefrontProductCatalogQuery query)
        {
            return new ProductCatalogQuery
            {
                PageNumber = query.PageNumber,
                PageSize = Math.Clamp(query.PageSize, 1, StorefrontContractValidation.MaxPageSize),
                CategoryId = query.CategoryId,
                CategorySlug = query.CategorySlug,
                IncludeSubcategories = query.IncludeSubcategories,
                SearchTerm = query.SearchTerm,
                MinPrice = query.MinPrice,
                MaxPrice = query.MaxPrice,
                InStock = query.InStock,
                SortBy = ToApplicationSortBy(query.SortBy),
                CreatedAfterUtc = query.CreatedAfterUtc,
            };
        }
        public static StorefrontCategoryResponse ToStorefrontContract(this GetCategory category)
        {
            return new StorefrontCategoryResponse(
                category.Id,
                category.ParentCategoryId,
                category.Name,
                category.Description,
                category.Slug,
                category.Image,
                category.DisplayOrder,
                category.UpdatedAt,
                category.MetaTitle,
                category.MetaDescription,
                category.CanonicalUrl,
                category.OgTitle,
                category.OgDescription,
                category.OgImage,
                category.SeoContent,
                category.RobotsIndex,
                category.RobotsFollow);
        }
        public static StorefrontCategoryTreeNodeResponse ToStorefrontContract(this GetCategoryTreeNode category)
        {
            return new StorefrontCategoryTreeNodeResponse(
                category.Id,
                category.ParentCategoryId,
                category.Name,
                category.Slug,
                category.Image,
                category.DisplayOrder,
                category.Children.Select(child => child.ToStorefrontContract()).ToArray());
        }
        public static StorefrontCategoryPageResponse ToStorefrontContract(this GetCategoryPage page)
        {
            return new StorefrontCategoryPageResponse(
                page.Category.ToStorefrontContract(),
                page.Breadcrumbs.Select(crumb => crumb.ToStorefrontContract()).ToArray(),
                page.Products.Select(product => product.ToStorefrontContract()).ToArray(),
                page.DirectProductCount,
                page.DescendantProductCount);
        }
        public static StorefrontCategoryPageResponse ToStorefrontContract(
            this GetCategoryPage page,
            Func<GetCatalogProduct, StorefrontCatalogProductResponse> mapProduct)
        {
            return new StorefrontCategoryPageResponse(
                page.Category.ToStorefrontContract(),
                page.Breadcrumbs.Select(crumb => crumb.ToStorefrontContract()).ToArray(),
                page.Products.Select(mapProduct).ToArray(),
                page.DirectProductCount,
                page.DescendantProductCount);
        }
        public static StorefrontCategoryBreadcrumbItemResponse ToStorefrontContract(this GetCategoryBreadcrumbItem crumb)
        {
            return new StorefrontCategoryBreadcrumbItemResponse(
                crumb.Id,
                crumb.Name,
                crumb.Slug);
        }
        public static StorefrontCatalogProductResponse ToStorefrontContract(
            this GetCatalogProduct product,
            StorefrontDisplayMoney? displayMoney = null)
        {
            return new StorefrontCatalogProductResponse(
                product.Id,
                product.Slug,
                product.Name,
                product.Description,
                product.Sku,
                product.ShortDescription,
                product.Price,
                product.ComparePrice,
                product.Image,
                product.PrimaryMediaPublicId,
                product.HasPrimaryMedia,
                product.Quantity,
                product.CreatedOn,
                product.UpdatedAt,
                product.DisplayOrder,
                product.InStock,
                IsCatalogProductPurchasable(product),
                ResolveCatalogProductPurchaseBlockReasons(product),
                ResolveCatalogProductStockStatus(product),
                ResolveCatalogProductAvailableQuantity(product),
                product.MinOrderQuantity,
                product.MaxOrderQuantity,
                product.QuantityStep,
                product.ManageStock,
                product.ShippingRequired,
                product.FreeShipping,
                product.DeliveryEstimateText,
                product.PublishedOn,
                product.CategoryId,
                product.CategoryName,
                product.CategorySlug,
                product.HasVariants,
                product.ProductType,
                product.VariationTemplateId,
                displayMoney?.Price,
                displayMoney?.ComparePrice,
                displayMoney?.CurrencyCode);
        }
        public static StorefrontProductResponse ToStorefrontContract(
            this GetProduct product,
            StorefrontDisplayMoney? displayMoney = null,
            Func<GetProductVariant, GetProduct, StorefrontProductVariantResponse>? mapVariant = null)
        {
            var purchaseBlockReasons = ResolveProductPurchaseBlockReasons(product);
            return new StorefrontProductResponse(
                product.Id,
                product.Slug,
                product.Name,
                product.Description,
                product.Sku,
                product.ShortDescription,
                product.FullDescription,
                product.Price,
                product.ComparePrice,
                product.Weight,
                product.Length,
                product.Width,
                product.Height,
                product.Image,
                product.Quantity,
                purchaseBlockReasons.Count == 0,
                purchaseBlockReasons,
                ResolveProductStockStatus(product),
                ResolveProductAvailableQuantity(product),
                product.MinOrderQuantity,
                product.MaxOrderQuantity,
                product.QuantityStep,
                product.ManageStock,
                product.ShippingRequired,
                product.FreeShipping,
                product.DeliveryEstimateText,
                product.DisplayOrder,
                IsProductInStock(product),
                product.PublishedOn,
                product.ProductType,
                product.VariationTemplateId,
                product.CategoryId,
                product.MetaTitle,
                product.MetaDescription,
                product.CanonicalUrl,
                product.OgTitle,
                product.OgDescription,
                product.OgImage,
                product.SeoContent,
                product.RobotsIndex,
                product.RobotsFollow,
                product.Category?.ToStorefrontContract(),
                product.VariationTemplate,
                product.CreatedOn,
                product.UpdatedAt,
                product.Variants.Select(variant => mapVariant is null
                    ? variant.ToStorefrontContract(product: product)
                    : mapVariant(variant, product)).ToArray(),
                displayMoney?.Price,
                displayMoney?.ComparePrice,
                displayMoney?.CurrencyCode);
        }
        public static StorefrontProductVariantResponse ToStorefrontContract(
            this GetProductVariant variant,
            StorefrontDisplayMoney? displayMoney = null,
            GetProduct? product = null)
        {
            var purchaseBlockReasons = ResolveVariantPurchaseBlockReasons(variant, product);
            return new StorefrontProductVariantResponse(
                variant.Id,
                variant.ProductId,
                variant.Sku,
                variant.Attributes.Select(attribute => attribute.ToStorefrontContract()).ToArray(),
                variant.AttributeSignature,
                variant.DisplayName,
                variant.SizeScale,
                variant.SizeValue,
                variant.Price,
                variant.EffectivePrice,
                variant.Stock,
                variant.IsActive,
                purchaseBlockReasons.Count == 0,
                purchaseBlockReasons,
                ResolveVariantStockStatus(variant, product),
                product?.ManageStock == false ? null : variant.Stock,
                variant.Color,
                variant.IsDefault,
                displayMoney?.Price,
                displayMoney?.CurrencyCode);
        }
        public static StorefrontProductVariantAttributeResponse ToStorefrontContract(
            this ProductVariantAttributeDto attribute)
        {
            return new StorefrontProductVariantAttributeResponse(attribute.Name, attribute.Value);
        }
        public static ProductSelectionRequest ToApplicationRequest(
            this StorefrontProductSelectionPreviewRequest request,
            Guid storeId,
            Guid productId)
        {
            return new ProductSelectionRequest(
                storeId,
                productId,
                request.ProductVariantId,
                request.SelectedAttributes,
                SelectedAttributesJson: null,
                request.Quantity,
                request.CurrencyCode,
                ProductSelectionMode.Preview);
        }
        public static StorefrontProductSelectionPreviewResponse ToStorefrontContract(
            this ProductSelectionResult result)
        {
            return new StorefrontProductSelectionPreviewResponse(
                result.ProductId,
                result.ProductVariantId,
                result.IsValid,
                result.IsAvailable,
                result.CanAddToCart,
                result.ValidationMessages,
                result.SelectedAttributes
                    .Select(attribute => new StorefrontProductVariantAttributeResponse(attribute.Name, attribute.Value))
                    .ToArray(),
                result.AttributeSignature,
                result.Sku,
                result.DisplayName,
                result.UnitPrice,
                result.ComparePrice,
                result.CurrencyCode,
                result.StockQuantity,
                result.MinQuantity,
                result.MaxQuantity,
                result.Product?.Image);
        }
        private static IReadOnlyList<string> ResolveCatalogProductPurchaseBlockReasons(GetCatalogProduct product)
        {
            var reasons = new List<string>();
            AddCommonPurchaseReasons(
                reasons,
                product.PurchasingDisabled,
                product.ManageStock,
                product.HasVariants,
                product.InStock,
                product.Quantity);
            return reasons;
        }
        private static bool IsCatalogProductPurchasable(GetCatalogProduct product)
        {
            return ResolveCatalogProductPurchaseBlockReasons(product).Count == 0;
        }
        private static string ResolveCatalogProductStockStatus(GetCatalogProduct product)
        {
            if (!product.ManageStock)
            {
                return ProductStockStatuses.NotManaged;
            }

            if (product.HasVariants)
            {
                return ProductStockStatuses.VariantRequired;
            }

            return product.InStock ? ProductStockStatuses.InStock : ProductStockStatuses.OutOfStock;
        }
        private static int? ResolveCatalogProductAvailableQuantity(GetCatalogProduct product)
        {
            return product.ManageStock && !product.HasVariants ? product.Quantity : null;
        }
        private static IReadOnlyList<string> ResolveProductPurchaseBlockReasons(GetProduct product)
        {
            var reasons = new List<string>();
            AddCommonPurchaseReasons(
                reasons,
                product.PurchasingDisabled,
                product.ManageStock,
                product.Variants.Any(),
                IsProductInStock(product),
                product.Quantity);
            return reasons;
        }
        private static bool IsProductInStock(GetProduct product)
        {
            return product.Quantity > 0 || product.Variants.Any(variant => variant.Stock > 0);
        }
        private static string ResolveProductStockStatus(GetProduct product)
        {
            if (!product.ManageStock)
            {
                return ProductStockStatuses.NotManaged;
            }

            if (product.Variants.Any())
            {
                return ProductStockStatuses.VariantRequired;
            }

            return product.Quantity > 0 ? ProductStockStatuses.InStock : ProductStockStatuses.OutOfStock;
        }
        private static int? ResolveProductAvailableQuantity(GetProduct product)
        {
            return product.ManageStock && !product.Variants.Any() ? product.Quantity : null;
        }
        private static IReadOnlyList<string> ResolveVariantPurchaseBlockReasons(GetProductVariant variant, GetProduct? product)
        {
            var reasons = new List<string>();
            if (product?.PurchasingDisabled == true)
            {
                reasons.Add(ProductPurchaseBlockReasons.PurchaseDisabled);
            }

            if (!variant.IsActive)
            {
                reasons.Add(ProductPurchaseBlockReasons.VariantInactive);
            }

            if (product?.ManageStock != false && variant.Stock <= 0)
            {
                reasons.Add(ProductPurchaseBlockReasons.OutOfStock);
            }

            return reasons;
        }
        private static string ResolveVariantStockStatus(GetProductVariant variant, GetProduct? product)
        {
            if (product?.ManageStock == false)
            {
                return ProductStockStatuses.NotManaged;
            }

            return variant.Stock > 0 ? ProductStockStatuses.InStock : ProductStockStatuses.OutOfStock;
        }
        private static void AddCommonPurchaseReasons(
            List<string> reasons,
            bool purchasingDisabled,
            bool manageStock,
            bool requiresVariant,
            bool inStock,
            int quantity)
        {
            if (purchasingDisabled)
            {
                reasons.Add(ProductPurchaseBlockReasons.PurchaseDisabled);
            }

            if (requiresVariant)
            {
                reasons.Add(ProductPurchaseBlockReasons.VariantRequired);
                return;
            }

            if (manageStock && (!inStock || quantity <= 0))
            {
                reasons.Add(ProductPurchaseBlockReasons.OutOfStock);
            }
        }
        public static StorefrontProductRecommendationResponse ToStorefrontContract(
            this GetProductRecommendation recommendation)
        {
            return new StorefrontProductRecommendationResponse(
                recommendation.Id,
                recommendation.Name,
                recommendation.Image,
                recommendation.Price,
                recommendation.CategoryName);
        }
        private static ProductCatalogSortBy ToApplicationSortBy(string? sortBy)
        {
            return sortBy switch
            {
                StorefrontProductCatalogSortValues.Oldest => ProductCatalogSortBy.Oldest,
                StorefrontProductCatalogSortValues.PriceLowToHigh => ProductCatalogSortBy.PriceLowToHigh,
                StorefrontProductCatalogSortValues.PriceHighToLow => ProductCatalogSortBy.PriceHighToLow,
                StorefrontProductCatalogSortValues.NameAscending => ProductCatalogSortBy.NameAscending,
                StorefrontProductCatalogSortValues.NameDescending => ProductCatalogSortBy.NameDescending,
                StorefrontProductCatalogSortValues.DisplayOrder => ProductCatalogSortBy.DisplayOrder,
                StorefrontProductCatalogSortValues.Updated => ProductCatalogSortBy.Updated,
                _ => ProductCatalogSortBy.Newest,
            };
        }
    }

    public sealed record StorefrontDisplayMoney(
        decimal Price,
        decimal? ComparePrice,
        string CurrencyCode);
}
