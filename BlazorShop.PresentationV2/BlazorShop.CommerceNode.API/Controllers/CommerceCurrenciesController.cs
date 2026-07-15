namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.Currencies;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/currencies")]
    public sealed class CommerceCurrenciesController : CommerceAdminControllerBase
    {
        private readonly IStoreCurrencyService currencyService;
        private readonly IStoreCurrencyExchangeRateService exchangeRateService;

        public CommerceCurrenciesController(
            IStoreCurrencyService currencyService,
            IStoreCurrencyExchangeRateService exchangeRateService)
        {
            this.currencyService = currencyService;
            this.exchangeRateService = exchangeRateService;
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

        [HttpGet("exchange-rates")]
        public async Task<IActionResult> GetExchangeRates(CancellationToken cancellationToken)
        {
            var rates = await this.exchangeRateService.GetAsync(cancellationToken);
            return this.Success(rates, "Store currency exchange rates loaded.");
        }

        [HttpPut("exchange-rates/{targetCurrencyCode}")]
        public async Task<IActionResult> UpsertExchangeRate(
            string targetCurrencyCode,
            [FromBody] UpsertStoreCurrencyExchangeRateRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.exchangeRateService.UpsertAsync(targetCurrencyCode, request, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPost("exchange-rates/{targetCurrencyCode}/disable")]
        public async Task<IActionResult> DisableExchangeRate(
            string targetCurrencyCode,
            CancellationToken cancellationToken)
        {
            var result = await this.exchangeRateService.DisableAsync(targetCurrencyCode, cancellationToken);
            return this.FromServiceResponse(result);
        }
    }
}
