namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.Services.Contracts.Admin;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/orders")]
    public sealed class OrdersController : CommerceAdminControllerBase
    {
        private readonly IAdminOrderService adminOrderService;

        public OrdersController(IAdminOrderService adminOrderService)
        {
            this.adminOrderService = adminOrderService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] AdminOrderQueryDto query)
        {
            var orders = await this.adminOrderService.GetAsync(query);
            return this.Success(orders, "Orders retrieved successfully.");
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await this.adminOrderService.GetByIdAsync(id);
            return this.FromServiceResponse(result);
        }

        [HttpPut("{id:guid}/tracking")]
        public async Task<IActionResult> UpdateTracking(Guid id, [FromBody] UpdateTrackingRequest request)
        {
            var result = await this.adminOrderService.UpdateTrackingAsync(id, request);
            return this.FromServiceResponse(result);
        }

        [HttpPut("{id:guid}/shipping-status")]
        public async Task<IActionResult> UpdateShippingStatus(Guid id, [FromBody] UpdateShippingStatusRequest request)
        {
            var result = await this.adminOrderService.UpdateShippingStatusAsync(id, request);
            return this.FromServiceResponse(result);
        }

        [HttpPut("{id:guid}/admin-note")]
        public async Task<IActionResult> UpdateAdminNote(Guid id, [FromBody] UpdateOrderAdminNoteRequest request)
        {
            var result = await this.adminOrderService.UpdateAdminNoteAsync(id, request);
            return this.FromServiceResponse(result);
        }
    }
}
