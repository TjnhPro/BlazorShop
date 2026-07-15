namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.Currencies;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/currencies")]
    public sealed class CommerceCurrenciesController : CommerceAdminControllerBase
    {
        private readonly IStoreCurrencyService currencyService;

        public CommerceCurrenciesController(IStoreCurrencyService currencyService)
        {
            this.currencyService = currencyService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var currencies = await this.currencyService.GetAsync(cancellationToken);
            return this.Success(currencies, "Store currencies loaded.");
        }

        [HttpPut("{currencyCode}")]
        public async Task<IActionResult> Update(
            string currencyCode,
            [FromBody] UpdateStoreCurrencyRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.currencyService.UpdateAsync(currencyCode, request, cancellationToken);
            return this.FromServiceResponse(result);
        }
    }
}
