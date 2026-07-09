namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services.Contracts;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/seo/settings")]
    public sealed class CommerceSeoSettingsController : CommerceAdminControllerBase
    {
        private readonly ISeoSettingsService seoSettingsService;

        public CommerceSeoSettingsController(ISeoSettingsService seoSettingsService)
        {
            this.seoSettingsService = seoSettingsService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var settings = await this.seoSettingsService.GetCurrentAsync();
            return this.Success(settings, "SEO settings retrieved successfully.");
        }

        [HttpPut]
        public async Task<IActionResult> Update(UpdateSeoSettingsDto request)
        {
            var result = await this.seoSettingsService.UpdateAsync(request);
            return this.FromServiceResponse(result);
        }
    }
}
