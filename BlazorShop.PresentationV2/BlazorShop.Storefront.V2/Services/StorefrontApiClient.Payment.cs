namespace BlazorShop.Storefront.Services
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


    public partial class StorefrontApiClient
    {
        public async Task<StorefrontApiResult<IReadOnlyList<GetPaymentMethod>>> GetPaymentMethodsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<GetPaymentMethod>>(
                StorefrontPaymentMethodsRoute,
                cancellationToken,
                [],
                CatalogRequestTimeout);

            return result.IsSuccess && result.Value is not null
                ? StorefrontApiResult<IReadOnlyList<GetPaymentMethod>>.Success(result.Value)
                : result.IsServiceUnavailable
                    ? StorefrontApiResult<IReadOnlyList<GetPaymentMethod>>.ServiceUnavailable()
                    : StorefrontApiResult<IReadOnlyList<GetPaymentMethod>>.Success([]);
        }
        public Task<StorefrontApiResult<StorefrontPaymentAttemptResponse>> GetPaymentAttemptAsync(
            Guid paymentAttemptId,
            CancellationToken cancellationToken = default)
        {
            if (paymentAttemptId == Guid.Empty)
            {
                return Task.FromResult(StorefrontApiResult<StorefrontPaymentAttemptResponse>.NotFound());
            }

            return GetAsync<StorefrontPaymentAttemptResponse>(
                $"{StorefrontPaymentAttemptsRoute}/{paymentAttemptId:D}",
                cancellationToken,
                fallbackValue: null,
                CatalogRequestTimeout);
        }
    }
}
