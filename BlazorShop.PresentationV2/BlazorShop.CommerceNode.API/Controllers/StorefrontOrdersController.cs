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
    [Route("api/internal/orders")]
    [Authorize]
    public sealed class StorefrontOrdersController : StorefrontInternalControllerBase
    {
        private readonly ICartService cartService;
        private readonly IOrderQueryService orderQueryService;

        public StorefrontOrdersController(
            ICartService cartService,
            IOrderQueryService orderQueryService)
        {
            this.cartService = cartService;
            this.orderQueryService = orderQueryService;
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmOrder(IEnumerable<ProcessCart> carts, [FromQuery] string? status = null)
        {
            var userId = this.GetCurrentCustomerId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return this.Unauthorized(CommerceNodeApiResponse<object>.Failed("Customer identity was not found."));
            }

            var result = await this.cartService.ConfirmOrderAsync(carts, userId, status);
            return this.FromServiceResponse(result);
        }

        [HttpGet("current-user")]
        public async Task<IActionResult> GetCurrentUserOrders()
        {
            var userId = this.GetCurrentCustomerId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return this.Unauthorized(CommerceNodeApiResponse<object>.Failed("Customer identity was not found."));
            }

            var orders = (await this.orderQueryService.GetOrdersForUserAsync(userId)).ToArray();
            return this.Success<IEnumerable<GetOrder>>(orders, "Current customer orders loaded.");
        }

        [HttpGet("current-user/items")]
        public async Task<IActionResult> GetCurrentUserOrderItems()
        {
            var userId = this.GetCurrentCustomerId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return this.Unauthorized(CommerceNodeApiResponse<object>.Failed("Customer identity was not found."));
            }

            var orderItems = (await this.cartService.GetCheckoutHistoryByUserId(userId)).ToArray();
            return orderItems.Length == 0
                ? this.Failure<IEnumerable<GetOrderItem>>(
                    ServiceResponseType.NotFound,
                    "No orders found for the current customer.",
                    orderItems)
                : this.Success<IEnumerable<GetOrderItem>>(orderItems, "Current customer order items loaded.");
        }

        private string? GetCurrentCustomerId()
        {
            return this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
