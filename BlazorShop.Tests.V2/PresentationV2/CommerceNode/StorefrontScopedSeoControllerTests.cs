extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Settings;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using Moq;

    using Xunit;

    using CommerceNodeApi::BlazorShop.CommerceNode.API.Controllers;
    using CommerceNodeApi::BlazorShop.CommerceNode.API.Responses;

    public sealed class StorefrontScopedSeoControllerTests
    {
        [Fact]
        public async Task ResolveRedirect_WhenExplicitRedirectExists_DoesNotCallUrlResolver()
        {
            var redirectService = new Mock<ISeoRedirectResolutionService>();
            var urlResolver = new Mock<ISeoUrlResolver>();
            var controller = CreateController(redirectService.Object, urlResolver.Object);
            redirectService
                .Setup(service => service.ResolvePublicPathAsync("/legacy-sale"))
                .ReturnsAsync(new SeoRedirectResolutionDto
                {
                    NewPath = "/todays-deals",
                    StatusCode = 301,
                });

            var result = await controller.ResolveRedirect("/legacy-sale", CancellationToken.None);

            var response = AssertSuccess(result);
            Assert.Equal("/todays-deals", response.Data!.NewPath);
            Assert.Equal(301, response.Data.StatusCode);
            urlResolver.Verify(
                service => service.ResolvePublicPathAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ResolveRedirect_WhenSlugHistoryRequiresCanonicalRedirect_ReturnsLegacyRedirectDto()
        {
            var redirectService = new Mock<ISeoRedirectResolutionService>();
            var urlResolver = new Mock<ISeoUrlResolver>();
            var controller = CreateController(redirectService.Object, urlResolver.Object);
            redirectService
                .Setup(service => service.ResolvePublicPathAsync("/product/old-shoes"))
                .ReturnsAsync((SeoRedirectResolutionDto?)null);
            urlResolver
                .Setup(service => service.ResolvePublicPathAsync("/product/old-shoes", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SeoUrlResolutionDto(
                    SeoUrlResolutionStatuses.RedirectToCanonical,
                    StatusCodes.Status301MovedPermanently,
                    RequiresRedirect: true,
                    RequestedPath: "/product/old-shoes",
                    CanonicalPath: "/product/new-shoes",
                    EntityType: SeoSlugEntityTypes.Product,
                    EntityId: Guid.NewGuid(),
                    RequestedSlug: "old-shoes",
                    CanonicalSlug: "new-shoes",
                    LanguageCode: null));

            var result = await controller.ResolveRedirect("/product/old-shoes", CancellationToken.None);

            var response = AssertSuccess(result);
            Assert.Equal("/product/new-shoes", response.Data!.NewPath);
            Assert.Equal(301, response.Data.StatusCode);
        }

        [Fact]
        public async Task ResolveRedirect_WhenPathIsAlreadyCanonical_ReturnsNotFound()
        {
            var redirectService = new Mock<ISeoRedirectResolutionService>();
            var urlResolver = new Mock<ISeoUrlResolver>();
            var controller = CreateController(redirectService.Object, urlResolver.Object);
            redirectService
                .Setup(service => service.ResolvePublicPathAsync("/product/new-shoes"))
                .ReturnsAsync((SeoRedirectResolutionDto?)null);
            urlResolver
                .Setup(service => service.ResolvePublicPathAsync("/product/new-shoes", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SeoUrlResolutionDto(
                    SeoUrlResolutionStatuses.Resolved,
                    StatusCodes.Status200OK,
                    RequiresRedirect: false,
                    RequestedPath: "/product/new-shoes",
                    CanonicalPath: "/product/new-shoes",
                    EntityType: SeoSlugEntityTypes.Product,
                    EntityId: Guid.NewGuid(),
                    RequestedSlug: "new-shoes",
                    CanonicalSlug: "new-shoes",
                    LanguageCode: null));

            var result = await controller.ResolveRedirect("/product/new-shoes", CancellationToken.None);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        }

        private static StorefrontScopedSeoController CreateController(
            ISeoRedirectResolutionService redirectService,
            ISeoUrlResolver urlResolver)
        {
            var settings = new Mock<IStoreSeoSettingsService>();
            return new StorefrontScopedSeoController(redirectService, urlResolver, settings.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext(),
                },
            };
        }

        private static CommerceNodeApiResponse<SeoRedirectResolutionDto> AssertSuccess(IActionResult result)
        {
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<CommerceNodeApiResponse<SeoRedirectResolutionDto>>(ok.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            return response;
        }
    }
}
