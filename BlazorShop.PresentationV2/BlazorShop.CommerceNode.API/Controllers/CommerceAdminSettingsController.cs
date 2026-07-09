namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs.Admin.Settings;
    using BlazorShop.Application.Services.Contracts.Admin;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/settings")]
    public sealed class CommerceAdminSettingsController : CommerceAdminControllerBase
    {
        private readonly IAdminSettingsService adminSettingsService;

        public CommerceAdminSettingsController(IAdminSettingsService adminSettingsService)
        {
            this.adminSettingsService = adminSettingsService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var settings = await this.adminSettingsService.GetAsync();
            return this.Success(settings, "Admin settings retrieved successfully.");
        }

        [HttpPut("store")]
        public async Task<IActionResult> UpdateStore([FromBody] UpdateStoreSettingsDto request)
        {
            var result = await this.adminSettingsService.UpdateStoreAsync(request);
            return this.FromServiceResponse(result);
        }

        [HttpPut("orders")]
        public async Task<IActionResult> UpdateOrders([FromBody] UpdateOrderSettingsDto request)
        {
            var result = await this.adminSettingsService.UpdateOrdersAsync(request);
            return this.FromServiceResponse(result);
        }

        [HttpPut("notifications")]
        public async Task<IActionResult> UpdateNotifications([FromBody] UpdateNotificationSettingsDto request)
        {
            var result = await this.adminSettingsService.UpdateNotificationsAsync(request);
            return this.FromServiceResponse(result);
        }
    }
}
