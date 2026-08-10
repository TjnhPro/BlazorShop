using BlazorShop.Storefront.Components.Contracts.Catalog;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorShop.Storefront.Browser.Catalog;

public sealed class StorefrontBrowserProductRailController : IStorefrontBrowserProductRailController
{
    public const string DefaultDiscountedProductsPath = "/api/catalog/discounted-products";

    private readonly IServiceProvider services;

    public StorefrontBrowserProductRailController(IServiceProvider services)
    {
        this.services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public async Task<StorefrontDiscountedProductRailResponse> GetDiscountedProductRailAsync(
        int limit,
        StorefrontDiscountedProductRailActionDescriptor? actionDescriptor = null,
        CancellationToken cancellationToken = default)
    {
        var apiClient = this.ResolveApiClient();
        if (apiClient is null)
        {
            return Failure(
                "service_unavailable",
                "Product rail data is unavailable.",
                traceId: null,
                retryable: true);
        }

        var route = string.IsNullOrWhiteSpace(actionDescriptor?.LoadPath)
            ? DefaultDiscountedProductsPath
            : actionDescriptor.LoadPath;
        var result = await apiClient
            .GetAsync<StorefrontDiscountedProductRailResponse>(
                AppendLimit(route, limit),
                cancellationToken)
            .ConfigureAwait(false);

        if (result.Success && result.Data is not null)
        {
            return result.Data;
        }

        if (result.Error is not null)
        {
            return Failure(
                result.Error.Code,
                result.Error.DisplayMessage,
                result.Error.TraceId,
                result.Error.Retryable);
        }

        return Failure(
            "service_unavailable",
            string.IsNullOrWhiteSpace(result.Message)
                ? "Product rail data could not be loaded."
                : result.Message,
            traceId: null,
            retryable: true);
    }

    private StorefrontLocalApiClient? ResolveApiClient()
    {
        return this.services.GetService<StorefrontLocalApiClient>();
    }

    private static string AppendLimit(string route, int limit)
    {
        var separator = route.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return string.Concat(route, separator, "limit=", limit.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private static StorefrontDiscountedProductRailResponse Failure(
        string code,
        string defaultMessage,
        string? traceId,
        bool retryable)
    {
        return new StorefrontDiscountedProductRailResponse(
            Array.Empty<ProductSummaryItem>(),
            Success: false,
            Code: string.IsNullOrWhiteSpace(code) ? "service_unavailable" : code,
            DefaultMessage: defaultMessage,
            TraceId: traceId,
            Retryable: retryable);
    }
}
