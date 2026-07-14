namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services.Contracts;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/internal/seo")]
    public sealed class StorefrontSeoController : StorefrontApiControllerBase
    {
        private readonly ISeoRedirectResolutionService seoRedirectResolutionService;
        private readonly ISeoSettingsService seoSettingsService;

        public StorefrontSeoController(
            ISeoRedirectResolutionService seoRedirectResolutionService,
            ISeoSettingsService seoSettingsService)
        {
            this.seoRedirectResolutionService = seoRedirectResolutionService;
            this.seoSettingsService = seoSettingsService;
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await this.seoSettingsService.GetCurrentAsync();
            return this.Success(settings, "SEO settings loaded.");
        }

        [HttpGet("redirects/resolve")]
        public async Task<IActionResult> ResolveRedirect([FromQuery] string path)
        {
            var redirect = await this.seoRedirectResolutionService.ResolvePublicPathAsync(path);
            return redirect is null
                ? this.Failure<SeoRedirectResolutionDto>(ServiceResponseType.NotFound, "SEO redirect was not found.")
                : this.Success(redirect, "SEO redirect resolved.");
        }
    }
}
