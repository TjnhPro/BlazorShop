namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.Services.Contracts;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/internal/newsletter")]
    public sealed class StorefrontNewsletterController : StorefrontApiControllerBase
    {
        private readonly INewsletterService newsletterService;

        public StorefrontNewsletterController(INewsletterService newsletterService)
        {
            this.newsletterService = newsletterService;
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeEmailRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Email))
            {
                return this.Failure<object>(ServiceResponseType.ValidationError, "Email is required.");
            }

            var result = await this.newsletterService.SubscribeAsync(request.Email);
            return this.FromServiceResponse(result);
        }

        public sealed record SubscribeEmailRequest(string Email);
    }
}
