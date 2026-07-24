using BlazorShop.Storefront.Components.Features.Catalog;
using BlazorShop.Storefront.Models;
using BlazorShop.Storefront.Services.Contracts;

namespace BlazorShop.Storefront.Services;

public static class StorefrontProductSummaryMapper
{
    public static ProductSummaryItem ToProductSummary(
        GetCatalogProduct product,
        StorefrontDisplayContext displayContext,
        IStorefrontPriceFormatter priceFormatter)
    {
        var productUrl = string.IsNullOrWhiteSpace(product.Slug) ? null : StorefrontRoutes.Product(product.Slug);
        var categoryUrl = string.IsNullOrWhiteSpace(product.CategorySlug) ? null : StorefrontRoutes.Category(product.CategorySlug);
        var displayCurrencyCode = NormalizeCurrencyCode(product.DisplayCurrencyCode) ?? displayContext.DefaultCurrencyCode;
        var displayPrice = product.DisplayPrice ?? product.Price;
        var comparePrice = product.DisplayComparePrice ?? product.ComparePrice;
        var canAddDirectly = !product.HasVariants && product.Purchasable && QuantityOneAllowed(product);
        var directAddStockValue = product.ManageStock ? Math.Max(0, product.AvailableQuantity ?? product.Quantity) : 999999;

        return new ProductSummaryItem(
            product.Id,
            string.IsNullOrWhiteSpace(product.Name) ? "Product" : product.Name,
            productUrl,
            product.CategoryName,
            categoryUrl,
            product.Image,
            Truncate(string.IsNullOrWhiteSpace(product.ShortDescription) ? product.Description ?? string.Empty : product.ShortDescription, 110),
            priceFormatter.Format(displayPrice, displayContext with { CurrencyCode = displayCurrencyCode }),
            comparePrice is not null && comparePrice > displayPrice
                ? priceFormatter.Format(comparePrice.Value, displayContext with { CurrencyCode = displayCurrencyCode })
                : null,
            product.HasVariants,
            product.InStock,
            DateTime.UtcNow.Subtract(product.CreatedOn).TotalDays <= 7,
            product.Purchasable,
            productUrl is null ? null : $"{productUrl}#purchase",
            canAddDirectly,
            displayPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            displayCurrencyCode,
            directAddStockValue,
            PurchaseBlockMessage(product));
    }

    private static bool QuantityOneAllowed(GetCatalogProduct product)
    {
        return product.MinOrderQuantity <= 1
            && (!product.MaxOrderQuantity.HasValue || product.MaxOrderQuantity.Value >= 1)
            && product.QuantityStep <= 1;
    }

    private static string PurchaseBlockMessage(GetCatalogProduct product)
    {
        return product.PurchaseBlockReasons.FirstOrDefault() switch
        {
            "purchase_disabled" => "Purchasing is paused.",
            "below_min_quantity" => $"Minimum order quantity is {product.MinOrderQuantity}.",
            "out_of_stock" => "Currently out of stock.",
            _ => "Currently unavailable.",
        };
    }

    private static string? NormalizeCurrencyCode(string? currencyCode)
    {
        var normalized = currencyCode?.Trim().ToUpperInvariant();
        return normalized is { Length: 3 } && normalized.All(char.IsLetter) ? normalized : null;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : string.Concat(value[..maxLength].TrimEnd(), "...");
    }
}
