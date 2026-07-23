namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    using Xunit;

    public sealed class CommerceNodeDevelopmentSeederTests
    {
        private static readonly Guid ApparelCategoryId = Guid.Parse("8d4830f9-a21f-4f4a-96d7-83d1e6dc0201");
        private static readonly Guid SimpleProductId = Guid.Parse("2b111111-1111-4111-8111-111111111101");
        private static readonly Guid SeoMediaProductId = Guid.Parse("2b111111-1111-4111-8111-111111111113");

        [Fact]
        public async Task SeedAsync_WhenQaSeedAlreadyExists_DoesNotResetStoreRuntimeProfile()
        {
            await using var context = CreateDbContext();
            var storeId = Guid.NewGuid();
            var store = new CommerceStore
            {
                Id = storeId,
                StoreKey = "default",
                Name = "Operator Edited Store",
                Status = CommerceStoreStatuses.Active,
                BaseUrl = "https://edited-store.example",
                LogoUrl = "/operator-logo.png",
                FaviconUrl = "/operator-favicon.ico",
                PngIconUrl = "/operator-icon.png",
                DefaultCurrencyCode = "USD",
                DefaultCulture = "vi-VN",
                MaintenanceModeEnabled = true,
                MaintenanceMessage = "Operator maintenance message.",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            };
            context.CommerceStores.Add(store);
            context.Categories.Add(new Category
            {
                Id = ApparelCategoryId,
                StoreId = storeId,
                Name = "Existing QA Category",
                Slug = "existing-qa-category",
            });
            context.Products.Add(new Product
            {
                Id = SimpleProductId,
                StoreId = storeId,
                CategoryId = ApparelCategoryId,
                Name = "Existing QA Product",
                Slug = "existing-qa-product",
                Price = 12.34m,
                Quantity = 5,
            });
            await context.SaveChangesAsync();
            var mediaRoot = Path.Combine(Path.GetTempPath(), "blazorshop-seeder-media", Guid.NewGuid().ToString("N"));

            var seeder = new CommerceNodeDevelopmentSeeder(
                context,
                userManager: null!,
                new FakeHostEnvironment(mediaRoot),
                new LocalMediaStorageProvider(),
                Options.Create(new CommerceMediaStorageOptions
                {
                    RootPath = "runtime/media/assets",
                }));

            await seeder.SeedAsync();

            var persisted = await context.CommerceStores.SingleAsync(candidate => candidate.StoreKey == "default");
            Assert.Equal("Operator Edited Store", persisted.Name);
            Assert.Equal("https://edited-store.example", persisted.BaseUrl);
            Assert.Equal("/operator-logo.png", persisted.LogoUrl);
            Assert.Equal("/operator-favicon.ico", persisted.FaviconUrl);
            Assert.Equal("/operator-icon.png", persisted.PngIconUrl);
            Assert.Equal("USD", persisted.DefaultCurrencyCode);
            Assert.Equal("vi-VN", persisted.DefaultCulture);
            Assert.True(persisted.MaintenanceModeEnabled);
            Assert.Equal("Operator maintenance message.", persisted.MaintenanceMessage);
            Assert.Empty(context.StorePaymentMethods);

            var qaProduct = await context.Products.SingleAsync(candidate => candidate.Id == SimpleProductId);
            Assert.Equal("QA Simple Product 100", qaProduct.Name);
            Assert.Equal("qa-simple-product-100", qaProduct.Slug);
            Assert.Equal(20, qaProduct.Quantity);

            var faqPage = await context.StorefrontPages.SingleAsync(candidate => candidate.Slug == "faq");
            Assert.Equal("faq", faqPage.PageKey);
            Assert.True(faqPage.IsPublished);
            Assert.True(faqPage.IncludeInSitemap);

            var customerServicePage = await context.StorefrontPages.SingleAsync(candidate => candidate.Slug == "customer-service");
            Assert.Equal("customer_service", customerServicePage.PageKey);
            Assert.True(customerServicePage.IsPublished);
            Assert.True(customerServicePage.IncludeInSitemap);
        }

        [Fact]
        public async Task SeedAsync_WhenQaMediaRowsAlreadyExist_RestoresMissingFixtureFiles()
        {
            await using var context = CreateDbContext();
            var storeId = Guid.NewGuid();
            context.CommerceStores.Add(new CommerceStore
            {
                Id = storeId,
                StoreKey = "default",
                Name = "Existing QA Store",
                Status = CommerceStoreStatuses.Active,
            });
            context.Categories.Add(new Category
            {
                Id = ApparelCategoryId,
                StoreId = storeId,
                Name = "Existing QA Category",
                Slug = "existing-qa-category",
            });
            context.Products.Add(new Product
            {
                Id = SimpleProductId,
                StoreId = storeId,
                CategoryId = ApparelCategoryId,
                Name = "Existing QA Product",
                Slug = "existing-qa-product",
                Price = 12.34m,
                Quantity = 5,
            });
            context.ProductMedia.AddRange(
                CreateStoredProductMedia(storeId, "6f111111-1111-4111-8111-111111111113"),
                CreateStoredProductMedia(storeId, "6f111111-1111-4111-8111-111111111114"),
                CreateStoredProductMedia(storeId, "6f111111-1111-4111-8111-111111111115"));
            await context.SaveChangesAsync();

            var mediaRoot = Path.Combine(Path.GetTempPath(), "blazorshop-seeder-media", Guid.NewGuid().ToString("N"));
            var seeder = new CommerceNodeDevelopmentSeeder(
                context,
                userManager: null!,
                new FakeHostEnvironment(mediaRoot),
                new LocalMediaStorageProvider(),
                Options.Create(new CommerceMediaStorageOptions
                {
                    RootPath = "runtime/media/assets",
                }));

            await seeder.SeedAsync();

            Assert.True(File.Exists(Path.Combine(mediaRoot, "runtime", "media", "qa-fixtures", "default", "seo-media-product.png")));
            Assert.True(File.Exists(Path.Combine(mediaRoot, "runtime", "media", "qa-fixtures", "default", "seo-media-product-alt-1.png")));
            Assert.True(File.Exists(Path.Combine(mediaRoot, "runtime", "media", "qa-fixtures", "default", "seo-media-product-alt-2.png")));
            Assert.True(File.Exists(Path.Combine(mediaRoot, "runtime", "media", "assets", "qa-fixtures", "default", "content-fixture.png")));
            Assert.True(File.Exists(Path.Combine(mediaRoot, "runtime", "media", "stores", "default", "products", "6f111111-1111-4111-8111-111111111113", "original.jpg")));
        }

        private static CommerceNodeDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"commerce-node-development-seeder-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private static ProductMedia CreateStoredProductMedia(Guid storeId, string mediaId)
        {
            return new ProductMedia
            {
                Id = Guid.Parse(mediaId),
                PublicId = Guid.NewGuid(),
                StoreId = storeId,
                ProductId = SeoMediaProductId,
                OriginalStoragePath = $"stores/default/products/{mediaId}/original.jpg",
                MimeType = "image/jpeg",
                Status = ProductMediaStatuses.Stored,
            };
        }

        private sealed class FakeHostEnvironment : IHostEnvironment
        {
            public FakeHostEnvironment(string contentRootPath)
            {
                Directory.CreateDirectory(contentRootPath);
                this.ContentRootPath = contentRootPath;
                this.ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
            }

            public string ApplicationName { get; set; } = "BlazorShop.Tests.V2";

            public IFileProvider ContentRootFileProvider { get; set; }

            public string ContentRootPath { get; set; }

            public string EnvironmentName { get; set; } = Environments.Development;
        }
    }
}
