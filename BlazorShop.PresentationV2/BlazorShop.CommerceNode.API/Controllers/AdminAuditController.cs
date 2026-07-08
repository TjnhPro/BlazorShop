namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts.Admin;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/audit")]
    public sealed class AdminAuditController : CommerceAdminControllerBase
    {
        private readonly IAdminAuditService adminAuditService;

        public AdminAuditController(IAdminAuditService adminAuditService)
        {
            this.adminAuditService = adminAuditService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] AdminAuditQueryDto query)
        {
            var logs = await this.adminAuditService.GetAsync(query);
            return this.Success(logs, "Audit logs retrieved successfully.");
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await this.adminAuditService.GetByIdAsync(id);
            return this.FromServiceResponse(result);
        }
    }
}
