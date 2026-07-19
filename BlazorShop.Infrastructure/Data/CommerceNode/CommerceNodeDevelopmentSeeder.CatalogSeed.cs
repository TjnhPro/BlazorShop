namespace BlazorShop.Infrastructure.Data.CommerceNode
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Identity;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    public sealed partial class CommerceNodeDevelopmentSeeder
    {
        private async Task EnsureCategoriesAsync(Guid storeId, CancellationToken cancellationToken)
        {
            if (!await this.dbContext.Categories.AnyAsync(category => category.Id == ApparelCategoryId, cancellationToken))
            {
                this.dbContext.Categories.Add(new Category
                {
                    Id = ApparelCategoryId,
                    StoreId = storeId,
                    Name = "Apparel",
                    Slug = "apparel",
                    Image = "/images/banner-bg.jpg",
                    DisplayOrder = 10,
                    IsPublished = true,
                    UpdatedAt = DateTime.UtcNow,
                    MetaTitle = "Apparel",
                    MetaDescription = "QA apparel category for catalog expansion.",
                });
            }

            if (!await this.dbContext.Categories.AnyAsync(category => category.Id == TshirtsCategoryId, cancellationToken))
            {
                this.dbContext.Categories.Add(new Category
                {
                    Id = TshirtsCategoryId,
                    StoreId = storeId,
                    ParentCategoryId = ApparelCategoryId,
                    Name = "T-Shirts",
                    Slug = "t-shirts",
                    Image = "/images/banner-bg.jpg",
                    DisplayOrder = 20,
                    IsPublished = true,
                    UpdatedAt = DateTime.UtcNow,
                    MetaTitle = "T-Shirts",
                    MetaDescription = "QA t-shirt category with variant products.",
                });
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureProductsAsync(Guid storeId, CancellationToken cancellationToken)
        {
            if (!await this.dbContext.Products.AnyAsync(product => product.Id == TshirtProductId, cancellationToken))
            {
                this.dbContext.Products.Add(new Product
                {
                    Id = TshirtProductId,
                    StoreId = storeId,
                    CategoryId = TshirtsCategoryId,
                    Name = "Catalog QA T-Shirt",
                    Slug = "catalog-qa-t-shirt",
                    Sku = "QA-TSHIRT",
                    Description = "A catalog QA t-shirt with color and size variants.",
                    ShortDescription = "QA t-shirt with variants.",
                    FullDescription = "A catalog expansion QA product used to verify variant selection, stock filtering, and order snapshots.",
                    Price = 19.99m,
                    ComparePrice = 24.99m,
                    Image = "/images/banner-bg.jpg",
                    Quantity = 30,
                    ProductType = ProductTypes.VariantInventory,
                    ManageStock = true,
                    MinOrderQuantity = 1,
                    QuantityStep = 1,
                    ShippingRequired = true,
                    DisplayOrder = 10,
                    IsPublished = true,
                    CreatedOn = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow,
                    PublishedOn = DateTime.UtcNow.AddDays(-2),
                    MetaTitle = "Catalog QA T-Shirt",
                    MetaDescription = "Catalog expansion QA t-shirt.",
                });
            }

            if (!await this.dbContext.Products.AnyAsync(product => product.Id == LowStockProductId, cancellationToken))
            {
                this.dbContext.Products.Add(new Product
                {
                    Id = LowStockProductId,
                    StoreId = storeId,
                    CategoryId = TshirtsCategoryId,
                    Name = "Catalog QA Low Stock Tee",
                    Slug = "catalog-qa-low-stock-tee",
                    Sku = "QA-LOW-STOCK",
                    Description = "A low-stock QA product.",
                    ShortDescription = "Low-stock QA product.",
                    FullDescription = "A catalog expansion QA product used to verify low-stock and in-stock filtering.",
                    Price = 15.99m,
                    Image = "/images/banner-bg.jpg",
                    Quantity = 1,
                    ManageStock = true,
                    MinOrderQuantity = 1,
                    QuantityStep = 1,
                    DisplayOrder = 20,
                    IsPublished = true,
                    CreatedOn = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow,
                    PublishedOn = DateTime.UtcNow.AddDays(-1),
                });
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);

            await this.EnsureQaProductAsync(
                new ProductSeed(
                    SimpleProductId,
                    "QA Simple Product 100",
                    "qa-simple-product-100",
                    "QA-P1-SIMPLE",
                    "Published simple QA product with managed stock 20 and price 100.",
                    100.00m,
                    20,
                    DisplayOrder: 30),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    OutOfStockProductId,
                    "QA Out Of Stock Product",
                    "qa-out-of-stock-product",
                    "QA-P2-OOS",
                    "Published simple QA product with stock 0 and visible unavailable state.",
                    45.00m,
                    0,
                    DisplayOrder: 40),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    UnmanagedStockProductId,
                    "QA Unmanaged Stock Product",
                    "qa-unmanaged-stock-product",
                    "QA-P3-UNMANAGED",
                    "Published QA product with unmanaged stock and quantity 0.",
                    55.00m,
                    0,
                    DisplayOrder: 50,
                    ManageStock: false),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    QuantityRuleProductId,
                    "QA Quantity Rule Product",
                    "qa-quantity-rule-product",
                    "QA-P4-QTY",
                    "Published QA product with min quantity 2, max 10, step 2.",
                    25.00m,
                    40,
                    DisplayOrder: 60,
                    MinOrderQuantity: 2,
                    MaxOrderQuantity: 10,
                    QuantityStep: 2),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    PurchasingDisabledProductId,
                    "QA Purchasing Disabled Product",
                    "qa-purchasing-disabled-product",
                    "QA-P7-DISABLED",
                    "Published QA product with purchasing disabled and a visible reason.",
                    65.00m,
                    20,
                    DisplayOrder: 70,
                    PurchasingDisabled: true,
                    PurchasingDisabledReason: "QA fixture: purchasing is disabled for this product."),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    UnpublishedProductId,
                    "QA Unpublished Product",
                    "qa-unpublished-product",
                    "QA-P8-UNPUBLISHED",
                    "Unpublished QA product for listing and direct-route not-found checks.",
                    31.00m,
                    10,
                    DisplayOrder: 80,
                    IsPublished: false,
                    PublishedOn: null),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    FutureProductId,
                    "QA Future Product",
                    "qa-future-product",
                    "QA-P9-FUTURE",
                    "QA product scheduled for the future.",
                    32.00m,
                    10,
                    DisplayOrder: 90,
                    AvailableStartUtc: DateTime.UtcNow.AddDays(14)),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    ExpiredProductId,
                    "QA Expired Product",
                    "qa-expired-product",
                    "QA-P10-EXPIRED",
                    "QA product whose availability window has ended.",
                    33.00m,
                    10,
                    DisplayOrder: 100,
                    AvailableEndUtc: DateTime.UtcNow.AddDays(-1)),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    SurchargeProductId,
                    "QA Shipping Surcharge Product",
                    "qa-shipping-surcharge-product",
                    "QA-P11-SURCHARGE",
                    "Physical QA product with shipping surcharge.",
                    120.00m,
                    20,
                    DisplayOrder: 110,
                    ShippingSurcharge: 12.50m),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    DigitalProductId,
                    "QA Digital No Shipping Product",
                    "qa-digital-no-shipping-product",
                    "QA-P12-DIGITAL",
                    "QA product that does not require shipping.",
                    35.00m,
                    50,
                    DisplayOrder: 120,
                    ShippingRequired: false),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    SeoMediaProductId,
                    "QA SEO Media Product",
                    "qa-seo-media-product",
                    "QA-P13-SEO-MEDIA",
                    "QA product with compare price and SEO metadata.",
                    88.00m,
                    25,
                    DisplayOrder: 130,
                    ComparePrice: 120.00m,
                    MetaTitle: "QA SEO Media Product",
                    MetaDescription: "Product fixture for SEO, media, compare price, and JSON-LD QA.",
                    OgTitle: "QA SEO Media Product",
                    OgDescription: "SEO product fixture with safe media.",
                    OgImage: "/images/banner-bg.jpg"),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    HtmlNameProductId,
                    "QA Unicode Safe <Tag> Tee",
                    "qa-unicode-safe-tee",
                    "QA-P15-UNICODE",
                    "QA product with Unicode and HTML-like text for escaping checks.",
                    42.00m,
                    12,
                    DisplayOrder: 150),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    MissingImageProductId,
                    "QA Missing Image Product",
                    "qa-missing-image-product",
                    "QA-P16-NOIMAGE",
                    "Published QA product with no image to verify fallback rendering.",
                    18.00m,
                    10,
                    DisplayOrder: 160,
                    Image: null),
                storeId,
                TshirtsCategoryId,
                cancellationToken);

            var variantProduct = await this.dbContext.Products.FirstAsync(product => product.Id == TshirtProductId, cancellationToken);
            variantProduct.ProductType = ProductTypes.VariantInventory;
            variantProduct.ManageStock = true;
            variantProduct.Quantity = 30;
            variantProduct.ShippingRequired = true;
            variantProduct.UpdatedAt = DateTime.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken);

            await this.EnsureVariantAsync(
                TshirtRedMVariantId,
                TshirtProductId,
                "QA-TSHIRT-RED-M",
                [new("Color", "Red"), new("Size", "M")],
                19.99m,
                8,
                true,
                true,
                cancellationToken);
            await this.EnsureVariantAsync(
                TshirtRedXlVariantId,
                TshirtProductId,
                "QA-TSHIRT-RED-XL",
                [new("Color", "Red"), new("Size", "XL")],
                21.99m,
                3,
                false,
                true,
                cancellationToken);
            await this.EnsureVariantAsync(
                TshirtBlackMVariantId,
                TshirtProductId,
                "QA-TSHIRT-BLACK-M",
                [new("Color", "Black"), new("Size", "M")],
                null,
                0,
                false,
                false,
                cancellationToken);
        }

        private async Task EnsureQaProductAsync(
            ProductSeed seed,
            Guid storeId,
            Guid categoryId,
            CancellationToken cancellationToken)
        {
            var product = await this.dbContext.Products.FirstOrDefaultAsync(item => item.Id == seed.Id, cancellationToken);
            if (product is null)
            {
                product = new Product { Id = seed.Id };
                this.dbContext.Products.Add(product);
            }

            product.StoreId = storeId;
            product.CategoryId = categoryId;
            product.Name = seed.Name;
            product.Slug = seed.Slug;
            product.Sku = seed.Sku;
            product.Description = seed.Description;
            product.ShortDescription = seed.Description;
            product.FullDescription = seed.Description;
            product.Price = seed.Price;
            product.ComparePrice = seed.ComparePrice;
            product.Image = seed.Image;
            product.Quantity = seed.Quantity;
            product.ProductType = ProductTypes.Simple;
            product.ManageStock = seed.ManageStock;
            product.MinOrderQuantity = seed.MinOrderQuantity;
            product.MaxOrderQuantity = seed.MaxOrderQuantity;
            product.QuantityStep = seed.QuantityStep;
            product.PurchasingDisabled = seed.PurchasingDisabled;
            product.PurchasingDisabledReason = seed.PurchasingDisabledReason;
            product.HideWhenOutOfStock = seed.HideWhenOutOfStock;
            product.ShippingRequired = seed.ShippingRequired;
            product.FreeShipping = seed.FreeShipping;
            product.ShippingSurcharge = seed.ShippingSurcharge;
            product.DeliveryEstimateText = seed.DeliveryEstimateText;
            product.DisplayOrder = seed.DisplayOrder;
            product.IsPublished = seed.IsPublished;
            product.PublishedOn = seed.PublishedOn ?? (seed.IsPublished ? DateTime.UtcNow.AddDays(-3) : null);
            product.AvailableStartUtc = seed.AvailableStartUtc;
            product.AvailableEndUtc = seed.AvailableEndUtc;
            product.MetaTitle = seed.MetaTitle ?? seed.Name;
            product.MetaDescription = seed.MetaDescription ?? seed.Description;
            product.OgTitle = seed.OgTitle ?? seed.Name;
            product.OgDescription = seed.OgDescription ?? seed.Description;
            product.OgImage = seed.OgImage ?? seed.Image;
            product.RobotsIndex = seed.RobotsIndex;
            product.RobotsFollow = seed.RobotsFollow;
            product.SeoContent = seed.SeoContent;
            product.CreatedOn = seed.CreatedOn ?? DateTime.UtcNow.AddDays(-3);
            product.UpdatedAt = DateTime.UtcNow;

            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureVariantAsync(
            Guid id,
            Guid productId,
            string sku,
            IReadOnlyList<VariantAttributeSeed> attributes,
            decimal? price,
            int stock,
            bool isDefault,
            bool isActive,
            CancellationToken cancellationToken)
        {
            var existing = await this.dbContext.ProductVariants.FirstOrDefaultAsync(variant => variant.Id == id, cancellationToken);
            if (existing is not null)
            {
                existing.Sku = sku;
                existing.Price = price;
                existing.Stock = stock;
                existing.IsDefault = isDefault;
                existing.IsActive = isActive;
                await this.dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            var signature = string.Join(
                "|",
                attributes
                    .OrderBy(attribute => attribute.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(attribute => $"{attribute.Name.Trim().ToLowerInvariant()}={attribute.Value.Trim().ToLowerInvariant()}"));

            this.dbContext.ProductVariants.Add(new ProductVariant
            {
                Id = id,
                ProductId = productId,
                Sku = sku,
                AttributesJson = JsonSerializer.Serialize(attributes),
                AttributeSignature = signature,
                DisplayName = string.Join(" / ", attributes.Select(attribute => attribute.Value)),
                SizeScale = SizeScale.ClothingAlpha,
                SizeValue = attributes.FirstOrDefault(attribute => attribute.Name == "Size")?.Value ?? string.Empty,
                Color = attributes.FirstOrDefault(attribute => attribute.Name == "Color")?.Value,
                Price = price,
                Stock = stock,
                IsDefault = isDefault,
                IsActive = isActive,
            });

            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureIsolationCatalogAsync(Guid storeId, CancellationToken cancellationToken)
        {
            if (!await this.dbContext.Categories.AnyAsync(category => category.Id == QaS2CategoryId, cancellationToken))
            {
                this.dbContext.Categories.Add(new Category
                {
                    Id = QaS2CategoryId,
                    StoreId = storeId,
                    Name = "S2 Apparel",
                    Slug = "apparel",
                    Image = "/images/banner-bg.jpg",
                    DisplayOrder = 10,
                    IsPublished = true,
                    UpdatedAt = DateTime.UtcNow,
                    MetaTitle = "S2 Apparel",
                    MetaDescription = "Second-store category for isolation QA.",
                });
                await this.dbContext.SaveChangesAsync(cancellationToken);
            }

            await this.EnsureQaProductAsync(
                new ProductSeed(
                    QaS2ProductId,
                    "S2 Isolation Product",
                    "qa-simple-product-100",
                    "QA-S2-P1",
                    "Same slug as S1 product but owned by S2 for isolation QA.",
                    100.00m,
                    20,
                    DisplayOrder: 10),
                storeId,
                QaS2CategoryId,
                cancellationToken);
        }

        private sealed record ProductSeed(
            Guid Id,
            string Name,
            string Slug,
            string Sku,
            string Description,
            decimal Price,
            int Quantity,
            int DisplayOrder,
            string? Image = "/images/banner-bg.jpg",
            bool ManageStock = true,
            int MinOrderQuantity = 1,
            int? MaxOrderQuantity = null,
            int QuantityStep = 1,
            bool PurchasingDisabled = false,
            string? PurchasingDisabledReason = null,
            bool HideWhenOutOfStock = false,
            bool ShippingRequired = true,
            bool FreeShipping = false,
            decimal? ShippingSurcharge = null,
            string? DeliveryEstimateText = null,
            bool IsPublished = true,
            DateTime? PublishedOn = null,
            DateTime? AvailableStartUtc = null,
            DateTime? AvailableEndUtc = null,
            decimal? ComparePrice = null,
            string? MetaTitle = null,
            string? MetaDescription = null,
            string? OgTitle = null,
            string? OgDescription = null,
            string? OgImage = null,
            bool RobotsIndex = true,
            bool RobotsFollow = true,
            string? SeoContent = null,
            DateTime? CreatedOn = null);

        private sealed record VariantAttributeSeed(string Name, string Value);
    }
}
