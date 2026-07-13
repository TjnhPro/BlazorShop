namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.Payments;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/payment-methods")]
    public sealed class CommercePaymentMethodsController : CommerceAdminControllerBase
    {
        private readonly IStorePaymentMethodAdminService paymentMethodService;

        public CommercePaymentMethodsController(IStorePaymentMethodAdminService paymentMethodService)
        {
            this.paymentMethodService = paymentMethodService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var methods = await this.paymentMethodService.GetAsync(cancellationToken);
            return this.Success(methods, "Payment methods loaded.");
        }

        [HttpPut("{paymentMethodKey}")]
        public async Task<IActionResult> Update(
            string paymentMethodKey,
            [FromBody] UpdateStorePaymentMethodRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.paymentMethodService.UpdateAsync(paymentMethodKey, request, cancellationToken);
            return this.FromServiceResponse(result);
        }
    }
}
