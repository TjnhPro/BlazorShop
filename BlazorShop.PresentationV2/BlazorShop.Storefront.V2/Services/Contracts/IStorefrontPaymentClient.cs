namespace BlazorShop.Storefront.Services.Contracts
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Web.SharedV2.Models;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Storefront.Options;

    using Microsoft.Extensions.Options;

    using BlazorShop.Storefront.Services;

    public interface IStorefrontPaymentClient
    {
        Task<StorefrontApiResult<IReadOnlyList<GetPaymentMethod>>> GetPaymentMethodsAsync(CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<StorefrontPaymentAttemptResponse>> GetPaymentAttemptAsync(
                    Guid paymentAttemptId,
                    CancellationToken cancellationToken = default);
    }
}
