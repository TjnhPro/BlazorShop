namespace BlazorShop.CommerceNode.API.Controllers
{
    using System.Security.Claims;

    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.Services.Contracts.Payment;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/internal/cart")]
    [Authorize]
    public sealed class StorefrontCartController : StorefrontInternalControllerBase
    {
        private readonly ICartService cartService;

        public StorefrontCartController(ICartService cartService)
        {
            this.cartService = cartService;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout(Checkout checkout)
        {
            var userId = this.GetCurrentCustomerId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return this.Unauthorized(CommerceNodeApiResponse<object>.Failed("Customer identity was not found."));
            }

            var result = await this.cartService.CheckoutAsync(checkout, userId);
            return this.FromServiceResponse(result);
        }

        [HttpPost("save-checkout")]
        public async Task<IActionResult> SaveCheckout(IEnumerable<CreateOrderItem> orderItems)
        {
            var userId = this.GetCurrentCustomerId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return this.Unauthorized(CommerceNodeApiResponse<object>.Failed("Customer identity was not found."));
            }

            var result = await this.cartService.SaveCheckoutHistoryAsync(userId, orderItems);
            return this.FromServiceResponse(result);
        }

        private string? GetCurrentCustomerId()
        {
            return this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
