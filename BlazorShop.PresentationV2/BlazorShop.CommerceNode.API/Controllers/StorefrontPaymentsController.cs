namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.Options;
    using BlazorShop.Application.Services.Contracts.Payment;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;

    [ApiController]
    [Route("api/internal/payments")]
    public sealed class StorefrontPaymentsController : StorefrontApiControllerBase
    {
        private readonly ClientAppOptions clientAppOptions;
        private readonly IPaymentMethodService paymentMethodService;
        private readonly IPayPalPaymentService payPalPaymentService;

        public StorefrontPaymentsController(
            IPaymentMethodService paymentMethodService,
            IPayPalPaymentService payPalPaymentService,
            IOptions<ClientAppOptions> clientAppOptions)
        {
            this.paymentMethodService = paymentMethodService;
            this.payPalPaymentService = payPalPaymentService;
            this.clientAppOptions = clientAppOptions.Value;
        }

        [HttpGet("methods")]
        public async Task<IActionResult> GetPaymentMethods()
        {
            var paymentMethods = (await this.paymentMethodService.GetPaymentMethodsAsync()).ToArray();
            return paymentMethods.Length == 0
                ? this.Failure<IEnumerable<GetPaymentMethod>>(
                    ServiceResponseType.NotFound,
                    "No payment methods are currently available.",
                    paymentMethods)
                : this.Success<IEnumerable<GetPaymentMethod>>(paymentMethods, "Payment methods loaded.");
        }

        [HttpGet("paypal/capture")]
        public async Task<IActionResult> CapturePayPal([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return this.Failure<object>(ServiceResponseType.ValidationError, "Missing PayPal token.");
            }

            var captured = await this.payPalPaymentService.CaptureAsync(token);
            return captured
                ? this.Redirect(this.BuildClientUrl("payment-success"))
                : this.Redirect(this.BuildClientUrl("payment-cancel"));
        }

        private string BuildClientUrl(string path)
        {
            if (string.IsNullOrWhiteSpace(this.clientAppOptions.BaseUrl))
            {
                return $"/{path.TrimStart('/')}";
            }

            return $"{this.clientAppOptions.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        }
    }
}
