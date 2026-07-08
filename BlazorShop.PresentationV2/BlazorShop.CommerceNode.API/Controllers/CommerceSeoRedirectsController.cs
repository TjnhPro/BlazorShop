namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services.Contracts;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/seo/redirects")]
    public sealed class CommerceSeoRedirectsController : CommerceAdminControllerBase
    {
        private readonly ISeoRedirectService seoRedirectService;

        public CommerceSeoRedirectsController(ISeoRedirectService seoRedirectService)
        {
            this.seoRedirectService = seoRedirectService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var redirects = await this.seoRedirectService.GetAllAsync();
            return this.Success(redirects, "SEO redirects retrieved successfully.");
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await this.seoRedirectService.GetByIdAsync(id);
            return this.FromServiceResponse(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(UpsertSeoRedirectDto request)
        {
            var result = await this.seoRedirectService.CreateAsync(request);
            return this.FromServiceResponse(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpsertSeoRedirectDto request)
        {
            var result = await this.seoRedirectService.UpdateAsync(id, request);
            return this.FromServiceResponse(result);
        }

        [HttpPost("{id:guid}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var result = await this.seoRedirectService.DeactivateAsync(id);
            return this.FromServiceResponse(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await this.seoRedirectService.DeleteAsync(id);
            return this.FromServiceResponse(result);
        }
    }
}
