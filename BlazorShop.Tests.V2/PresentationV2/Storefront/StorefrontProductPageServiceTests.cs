namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Text.Json.Nodes;

    using BlazorShop.Storefront.Presentation.Models;
    using BlazorShop.Storefront.Presentation.PagePatterns;
    using BlazorShop.Storefront.Presentation.Services.Product;
    using BlazorShop.Storefront.Presentation.Services;
    using BlazorShop.Storefront.Presentation.Contracts;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using Xunit;

    public sealed class StorefrontProductPageServiceTests
    {
        [Fact]
        public async Task ResolveAsync_ReturnsReadyProductContextWithMappedPurchaseAndSeo()
        {
            var product = CreateProduct();
            var service = CreateService(catalog =>
            {
                catalog
                    .Setup(client => client.GetPublishedProductBySlugAsync("test-product", "USD", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(StorefrontApiResult<GetProduct>.Success(product));
                catalog
                    .Setup(client => client.GetPublishedCategoryBySlugAsync("shirts", "USD", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(StorefrontApiResult<GetCategoryPage>.Success(new GetCategoryPage
                    {
                        Products =
                        [
                            new GetCatalogProduct { Id = Guid.NewGuid(), Slug = "related-product", Name = "Related product" },
                            new GetCatalogProduct { Id = product.Id, Slug = "test-product", Name = "Current product" },
                        ],
                    }));
            });

            var result = await service.ResolveAsync("test-product");

            var ready = Assert.IsType<StorefrontPageState.Ready<StorefrontProductPageContext>>(result.State);
            Assert.Equal(StorefrontPageKind.Product, ready.Kind);
            Assert.Equal("SEO Test Product", result.Metadata.Title);
            Assert.False(result.StructuredData.IsEmpty);
            Assert.Equal(product.Id, ready.Context.Product.Id);
            Assert.Single(ready.Context.GalleryItems);
            Assert.Equal(product.Id, ready.Context.PurchasePanel.ProductId);
            Assert.True(ready.Context.PurchasePanel.CanSubmitInitialPurchase);
            Assert.Single(ready.Context.RelatedProductSummaries);
        }

        [Fact]
        public async Task ResolveAsync_ReturnsNotFoundStateAndNotFoundSeo()
        {
            var service = CreateService(catalog =>
            {
                catalog
                    .Setup(client => client.GetPublishedProductBySlugAsync("missing", "USD", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(StorefrontApiResult<GetProduct>.NotFound());
            });

            var result = await service.ResolveAsync("missing");

            Assert.IsType<StorefrontPageState.NotFoundState>(result.State);
            Assert.Equal("Product not found", result.Metadata.Title);
            Assert.True(result.StructuredData.IsEmpty);
        }

        [Fact]
        public async Task ResolveAsync_ReturnsServiceUnavailableStateAndNoIndexSeo()
        {
            var service = CreateService(catalog =>
            {
                catalog
                    .Setup(client => client.GetPublishedProductBySlugAsync("offline", "USD", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(StorefrontApiResult<GetProduct>.ServiceUnavailable());
            });

            var result = await service.ResolveAsync("offline");

            Assert.IsType<StorefrontPageState.ServiceUnavailableState>(result.State);
            Assert.Equal("Product temporarily unavailable", result.Metadata.Title);
            Assert.False(result.Metadata.RobotsIndex);
            Assert.True(result.StructuredData.IsEmpty);
        }

        private static StorefrontProductPageService CreateService(Action<Mock<IStorefrontCatalogClient>> configureCatalog)
        {
            var catalog = new Mock<IStorefrontCatalogClient>();
            configureCatalog(catalog);

            var seo = new Mock<IStorefrontSeoComposer>();
            seo
                .Setup(composer => composer.ComposeProductPageAsync(It.IsAny<GetProduct>()))
                .ReturnsAsync(new SeoMetadataDto
                {
                    Title = "SEO Test Product",
                    MetaDescription = "SEO product description",
                    CanonicalUrl = "https://shop.example/product/test-product",
                    RobotsIndex = true,
                    RobotsFollow = true,
                });
            seo
                .Setup(composer => composer.ComposeNotFoundPageAsync("Product not found", "/product/missing", It.IsAny<string>()))
                .ReturnsAsync(new SeoMetadataDto { Title = "Product not found", RobotsIndex = false, RobotsFollow = false });
            seo
                .Setup(composer => composer.ComposeServiceUnavailablePageAsync("Product temporarily unavailable", "/product/offline", It.IsAny<string>()))
                .ReturnsAsync(new SeoMetadataDto { Title = "Product temporarily unavailable", RobotsIndex = false, RobotsFollow = false });

            var structuredData = new Mock<IStorefrontStructuredDataComposer>();
            structuredData
                .Setup(composer => composer.ComposeProductPageAsync(It.IsAny<GetProduct>()))
                .ReturnsAsync(StorefrontStructuredDataDocument.CreateGraph([new JsonObject { ["@type"] = "Product" }]));

            var displayContext = new Mock<IStorefrontDisplayContextProvider>();
            displayContext
                .Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(StorefrontDisplayContext.Fallback);

            var priceFormatter = new Mock<IStorefrontPriceFormatter>();
            priceFormatter
                .Setup(formatter => formatter.Format(It.IsAny<decimal>(), It.IsAny<StorefrontDisplayContext>()))
                .Returns<decimal, StorefrontDisplayContext>((amount, context) => $"{context.CurrencyCode} {amount:0.00}");

            return new StorefrontProductPageService(
                catalog.Object,
                seo.Object,
                structuredData.Object,
                displayContext.Object,
                priceFormatter.Object,
                NullLogger<StorefrontProductPageService>.Instance);
        }

        private static GetProduct CreateProduct()
        {
            var productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            return new GetProduct
            {
                Id = productId,
                Name = "Test Product",
                Slug = "test-product",
                Description = "Test product description",
                Price = 10m,
                DisplayPrice = 10m,
                DisplayCurrencyCode = "USD",
                CreatedOn = DateTime.UtcNow,
                ManageStock = false,
                Purchasable = true,
                MinOrderQuantity = 1,
                QuantityStep = 1,
                Category = new GetCategory { Name = "Shirts", Slug = "shirts" },
                Image = "/media/products/test-product.png",
                MediaGallery =
                [
                    new ProductGalleryImageDto(
                        Guid.NewGuid(),
                        "/media/products/test-product.png",
                        "/media/products/test-product-thumb.png",
                        null,
                        "Test product image",
                        0,
                        true,
                        null,
                        null,
                        1),
                ],
                Variants =
                [
                    new GetProductVariant
                    {
                        Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                        ProductId = productId,
                        DisplayName = "Default",
                        Price = 10m,
                        DisplayPrice = 10m,
                        DisplayCurrencyCode = "USD",
                        Stock = 5,
                        IsDefault = true,
                        Purchasable = true,
                    },
                ],
            };
        }
    }
}
