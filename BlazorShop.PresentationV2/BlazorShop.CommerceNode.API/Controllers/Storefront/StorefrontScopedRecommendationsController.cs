namespace BlazorShop.CommerceNode.API.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.Security.Claims;

    using BlazorShop.Application.CommerceNode.Addresses;
    using BlazorShop.Application.CommerceNode.Captcha;
    using ApplicationStorefrontCheckoutResult = BlazorShop.Application.DTOs.Payment.StorefrontCheckoutResult;
    using ApplicationStorefrontCheckoutPreviewResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutPreviewResult;
    using ApplicationStorefrontCheckoutReviewResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutReviewResult;
    using ApplicationStorefrontCheckoutSessionRequest = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutSessionRequest;
    using ApplicationStorefrontCheckoutSessionResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutSessionResult;
    using ApplicationStorefrontCheckoutStartRequest = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutStartRequest;
    using ApplicationStorefrontPlaceOrderResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontPlaceOrderResult;
    using IStorefrontCheckoutService = BlazorShop.Application.CommerceNode.Checkout.IStorefrontCheckoutService;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Consent;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Customers;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Orders;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Settings;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Discovery;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Application.Options;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Services.Contracts.Authentication;
    using BlazorShop.Application.Services.Contracts.Payment;
    using BlazorShop.CommerceNode.API.Configuration;
    using BlazorShop.CommerceNode.API.Contracts.Storefront;
    using BlazorShop.CommerceNode.API.Responses;
    using BlazorShop.Domain.Contracts;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.Extensions.Options;

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/recommendations")]
    public sealed class StorefrontScopedRecommendationsController : StorefrontApiControllerBase
    {
        private readonly IProductRecommendationService recommendationService;
        private readonly ILogger<StorefrontScopedRecommendationsController> logger;

        public StorefrontScopedRecommendationsController(
            IProductRecommendationService recommendationService,
            ILogger<StorefrontScopedRecommendationsController> logger)
        {
            this.recommendationService = recommendationService;
            this.logger = logger;
        }

        [HttpGet("products/{productId:guid}")]
        public async Task<IActionResult> GetRecommendations(Guid productId)
        {
            try
            {
                if (productId == Guid.Empty)
                {
                    return this.Failure<IReadOnlyList<StorefrontProductRecommendationResponse>>(
                        ServiceResponseType.ValidationError,
                        "Invalid product ID.");
                }

                var recommendations = (await this.recommendationService.GetRecommendationsForProductAsync(productId)).ToArray();
                if (recommendations.Length == 0)
                {
                    return this.Failure<IReadOnlyList<StorefrontProductRecommendationResponse>>(
                        ServiceResponseType.NotFound,
                        "No recommendations found for this product.",
                        []);
                }

                return this.Success(
                    recommendations.Select(recommendation => recommendation.ToStorefrontContract()).ToArray(),
                    "Product recommendations loaded.");
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error occurred while fetching recommendations for product {ProductId}.", productId);
                return this.Failure<IReadOnlyList<StorefrontProductRecommendationResponse>>(
                    ServiceResponseType.Failure,
                    "An error occurred while processing product recommendations.");
            }
        }
    }

}
