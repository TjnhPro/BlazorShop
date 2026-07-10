namespace BlazorShop.Infrastructure.Data.CommerceNode
{
    using System.Text.Json;

    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeDevelopmentSeeder
    {
        private const string DefaultStoreKey = "default";

        private static readonly Guid ApparelCategoryId = Guid.Parse("8d4830f9-a21f-4f4a-96d7-83d1e6dc0201");
        private static readonly Guid TshirtsCategoryId = Guid.Parse("e0e5e4f8-3f12-4c17-b041-7a8fc62e6b14");
        private static readonly Guid TshirtProductId = Guid.Parse("68ba3d10-4d13-46c4-8c8d-4a53b37cf201");
        private static readonly Guid LowStockProductId = Guid.Parse("e9f21b8f-7b2d-4a08-8971-c0dfe037fc1a");
        private static readonly Guid TshirtRedMVariantId = Guid.Parse("c34f5a0f-401d-4f58-b3d9-c9349ed6d101");
        private static readonly Guid TshirtRedXlVariantId = Guid.Parse("910cb350-8d44-43a7-b86d-8e38ea0cd102");
        private static readonly Guid TshirtBlackMVariantId = Guid.Parse("6894d9f0-071b-4f77-83a7-3d81d8a3d103");

        private readonly CommerceNodeDbContext dbContext;

        public CommerceNodeDevelopmentSeeder(CommerceNodeDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            var store = await this.EnsureStoreAsync(cancellationToken);
            await this.EnsureCategoriesAsync(store.Id, cancellationToken);
            await this.EnsureProductsAsync(store.Id, cancellationToken);
            await this.EnsureSampleOrderAsync(store.Id, cancellationToken);
        }

        private async Task<CommerceStore> EnsureStoreAsync(CancellationToken cancellationToken)
        {
            var store = await this.dbContext.CommerceStores
                .FirstOrDefaultAsync(candidate => candidate.StoreKey == DefaultStoreKey, cancellationToken);

            if (store is not null)
            {
                return store;
            }

            var now = DateTimeOffset.UtcNow;
            store = new CommerceStore
            {
                StoreKey = DefaultStoreKey,
                Name = "Default QA Store",
                Status = CommerceStoreStatuses.Active,
                BaseUrl = "http://localhost:18598",
                DefaultCurrencyCode = "EUR",
                DefaultCulture = "en-US",
                SupportEmail = "support@example.local",
                CreatedAt = now,
                UpdatedAt = now,
            };

            store.Domains.Add(new CommerceStoreDomain
            {
                Domain = "localhost",
                NormalizedDomain = "localhost",
                IsPrimary = true,
                Status = "verified",
                CreatedAt = now,
                UpdatedAt = now,
                VerifiedAt = now,
            });

            this.dbContext.CommerceStores.Add(store);
            await this.dbContext.SaveChangesAsync(cancellationToken);
            return store;
        }

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
                    DisplayOrder = 20,
                    IsPublished = true,
                    CreatedOn = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow,
                    PublishedOn = DateTime.UtcNow.AddDays(-1),
                });
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);

            await this.EnsureVariantAsync(
                TshirtRedMVariantId,
                TshirtProductId,
                "QA-TSHIRT-RED-M",
                [new("Color", "Red"), new("Size", "M")],
                19.99m,
                8,
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
                cancellationToken);
            await this.EnsureVariantAsync(
                TshirtBlackMVariantId,
                TshirtProductId,
                "QA-TSHIRT-BLACK-M",
                [new("Color", "Black"), new("Size", "M")],
                null,
                0,
                false,
                cancellationToken);
        }

        private async Task EnsureVariantAsync(
            Guid id,
            Guid productId,
            string sku,
            IReadOnlyList<VariantAttributeSeed> attributes,
            decimal? price,
            int stock,
            bool isDefault,
            CancellationToken cancellationToken)
        {
            if (await this.dbContext.ProductVariants.AnyAsync(variant => variant.Id == id, cancellationToken))
            {
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
            });

            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureSampleOrderAsync(Guid storeId, CancellationToken cancellationToken)
        {
            const string reference = "QA-CATALOG-SNAPSHOT";
            if (await this.dbContext.Orders.AnyAsync(order => order.Reference == reference, cancellationToken))
            {
                return;
            }

            this.dbContext.Orders.Add(new Order
            {
                StoreId = storeId,
                UserId = "qa-seed-user",
                Reference = reference,
                Status = "Completed",
                CurrencyCode = "EUR",
                TotalAmount = 19.99m,
                CreatedOn = DateTime.UtcNow,
                Lines =
                [
                    new OrderLine
                    {
                        ProductId = TshirtProductId,
                        ProductName = "Catalog QA T-Shirt",
                        Sku = "QA-TSHIRT-RED-M",
                        Image = "/images/banner-bg.jpg",
                        ProductVariantId = TshirtRedMVariantId,
                        VariantAttributesJson = JsonSerializer.Serialize(
                            new[]
                            {
                                new VariantAttributeSeed("Color", "Red"),
                                new VariantAttributeSeed("Size", "M"),
                            }),
                        Quantity = 1,
                        UnitPrice = 19.99m,
                    },
                ],
            });

            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private sealed record VariantAttributeSeed(string Name, string Value);
    }
}
