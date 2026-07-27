namespace BlazorShop.Storefront.Services.Contracts;

using Microsoft.AspNetCore.Http;

public interface IStorefrontCartMergeService
{
    Task MergeCurrentCustomerAsync(HttpContext httpContext, string accessToken, CancellationToken cancellationToken = default);
}
