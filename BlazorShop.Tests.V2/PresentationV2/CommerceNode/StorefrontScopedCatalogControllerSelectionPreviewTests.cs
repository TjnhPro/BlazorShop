extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Domain.Entities;

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using Moq;

    using Xunit;

    using CommerceNodeApi::BlazorShop.CommerceNode.API.Contracts.Storefront;
    using CommerceNodeApi::BlazorShop.CommerceNode.API.Controllers;
    using CommerceNodeApi::BlazorShop.CommerceNode.API.Responses;

    public sealed class StorefrontScopedCatalogControllerSelectionPreviewTests
    {
        [Fact]
        public async Task PreviewProductSelection_WhenSelectionIsValid_ReturnsPreviewPayload()
        {
            var storeId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            var resolver = new Mock<IProductSelectionResolver>();
            var controller = CreateController(storeId, resolver);
            resolver
                .Setup(service => service.ResolveAsync(
                    It.Is<ProductSelectionRequest>(request =>
                        request.StoreId == storeId
                        && request.ProductId == productId
                        && request.ProductVariantId == variantId
                        && request.Quantity == 2
                        && request.Mode == ProductSelectionMode.Preview),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateSelectionResult(productId, variantId, success: true));

            var result = await controller.PreviewProductSelection(
                productId,
                new StorefrontProductSelectionPreviewRequest
                {
                    ProductVariantId = variantId,
                    SelectedAttributes = [new SelectedAttributeDto("Color", "Red")],
                    Quantity = 2,
                    CurrencyCode = "USD",
                },
                CancellationToken.None);

            var response = AssertSuccess(result);
            Assert.True(response.Data!.CanAddToCart);
            Assert.Equal(productId, response.Data.ProductId);
            Assert.Equal(variantId, response.Data.ProductVariantId);
            Assert.Equal("/images/product.png", response.Data.PrimaryImageUrl);
            Assert.Equal("SKU-RED", response.Data.Sku);
            Assert.Equal(19.99m, response.Data.UnitPrice);
            Assert.Equal(24.99m, response.Data.ComparePrice);
            Assert.Equal("USD", response.Data.CurrencyCode);
            Assert.Empty(response.Data.ValidationMessages);
        }

        [Fact]
        public async Task PreviewProductSelection_WhenSelectionIsInvalid_ReturnsCustomerReadablePreviewPayload()
        {
            var storeId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var resolver = new Mock<IProductSelectionResolver>();
            var controller = CreateController(storeId, resolver);
            resolver
                .Setup(service => service.ResolveAsync(It.IsAny<ProductSelectionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateSelectionResult(productId, null, success: false));

            var result = await controller.PreviewProductSelection(
                productId,
                new StorefrontProductSelectionPreviewRequest { Quantity = 1 },
                CancellationToken.None);

            var response = AssertSuccess(result);
            Assert.False(response.Data!.IsValid);
            Assert.False(response.Data.CanAddToCart);
            Assert.Contains("Required selected attribute 'Color' is missing.", response.Data.ValidationMessages);
        }

        [Fact]
        public async Task PreviewProductSelection_WhenStoreCannotBeResolved_ReturnsNotFoundAndSkipsResolver()
        {
            var resolver = new Mock<IProductSelectionResolver>();
            var controller = CreateController(storeId: null, resolver);

            var result = await controller.PreviewProductSelection(
                Guid.NewGuid(),
                new StorefrontProductSelectionPreviewRequest { Quantity = 1 },
                CancellationToken.None);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
            var error = Assert.IsType<CommerceNodeApiErrorResponse>(objectResult.Value);
            Assert.Equal("store.not_found", error.Code);
            resolver.Verify(service => service.ResolveAsync(It.IsAny<ProductSelectionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private static StorefrontScopedCatalogController CreateController(
            Guid? storeId,
            Mock<IProductSelectionResolver> resolver)
        {
            var storeContext = new Mock<ICommerceStoreContext>();
            storeContext
                .Setup(context => context.GetCurrentStoreIdAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(storeId.HasValue
                    ? new ApplicationResult<Guid>(true, "Store resolved.", storeId.Value)
                    : new ApplicationResult<Guid>(false, "Store not found.", Failure: ApplicationErrorKind.NotFound));

            return new StorefrontScopedCatalogController(
                new Mock<IPublicCatalogService>().Object,
                storeContext.Object,
                new Mock<IStorefrontWorkingCurrencyResolver>().Object,
                new Mock<IMoneyConversionService>().Object,
                resolver.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext(),
                },
            };
        }

        private static ProductSelectionResult CreateSelectionResult(Guid productId, Guid? variantId, bool success)
        {
            return new ProductSelectionResult(
                success,
                success ? ServiceResponseType.Success : ServiceResponseType.ValidationError,
                success ? "Product selection resolved." : "Required selected attribute 'Color' is missing.",
                productId,
                variantId,
                success ? [new SelectedAttributeDto("Color", "Red")] : [],
                success ? """[{"name":"Color","value":"Red"}]""" : null,
                success ? "Color=Red" : null,
                success,
                success,
                success,
                success ? [] : ["Required selected attribute 'Color' is missing."],
                [],
                success ? ProductStockStatuses.InStock : null,
                "SKU-RED",
                "Red Shirt",
                success ? 19.99m : 0m,
                success ? 19.99m : 0m,
                "USD",
                "USD",
                success ? 24.99m : null,
                success ? 12 : 0,
                1,
                success ? 12 : 1,
                Product: new Product
                {
                    Id = productId,
                    Image = "/images/product.png",
                });
        }

        private static CommerceNodeApiResponse<StorefrontProductSelectionPreviewResponse> AssertSuccess(IActionResult result)
        {
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<CommerceNodeApiResponse<StorefrontProductSelectionPreviewResponse>>(ok.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            return response;
        }
    }
}
