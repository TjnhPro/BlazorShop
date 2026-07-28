namespace BlazorShop.Storefront.Presentation.Services.Product;

using global::System.Globalization;
using BlazorShop.Storefront.Components.Contracts.Catalog;
using BlazorShop.Storefront.Components.Contracts.Product;
using BlazorShop.Storefront.Models;
using BlazorShop.Storefront.Services;
using BlazorShop.Storefront.Services.Contracts;

public static class StorefrontProductPageMapper
{
    public static StorefrontProductPageContext Map(
        GetProduct product,
        IReadOnlyList<GetCatalogProduct> relatedProducts,
        StorefrontDisplayContext displayContext,
        IStorefrontPriceFormatter priceFormatter)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(displayContext);
        ArgumentNullException.ThrowIfNull(priceFormatter);

        var galleryItems = BuildGalleryItems(product);
        var purchasePanel = BuildPurchasePanel(product, galleryItems, displayContext, priceFormatter);
        var displayCurrencyCode = ResolveDisplayCurrencyCode(product, displayContext);
        var displayPriceAmount = ResolveDisplayPriceAmount(product);
        var canSubmitInitialPurchase = CanSubmitInitialPurchase(product);

        return new StorefrontProductPageContext(
            product,
            BuildBreadcrumbs(product),
            galleryItems,
            purchasePanel,
            BuildPricing(product, displayContext, priceFormatter, displayCurrencyCode, displayPriceAmount),
            BuildAvailability(product),
            BuildPurchase(product, canSubmitInitialPurchase),
            BuildVariants(product, displayContext, priceFormatter, displayCurrencyCode),
            new StorefrontProductBadgeView(IsFreshArrival(product.CreatedOn)),
            BuildNavigation(product),
            relatedProducts
                .Select(relatedProduct => StorefrontProductSummaryMapper.ToProductSummary(relatedProduct, displayContext, priceFormatter))
                .ToArray(),
            displayContext);
    }

    private static IReadOnlyList<StorefrontBreadcrumbItem> BuildBreadcrumbs(GetProduct product)
    {
        var breadcrumbs = new List<StorefrontBreadcrumbItem>
        {
            new("Home", StorefrontRoutes.Home),
        };

        if (!string.IsNullOrWhiteSpace(product.Category?.Slug) && !string.IsNullOrWhiteSpace(product.Category?.Name))
        {
            breadcrumbs.Add(new StorefrontBreadcrumbItem(product.Category.Name, StorefrontRoutes.Category(product.Category.Slug)));
        }

        breadcrumbs.Add(new StorefrontBreadcrumbItem(product.Name ?? "Product"));

        return breadcrumbs;
    }

    private static IReadOnlyList<ProductGalleryItem> BuildGalleryItems(GetProduct product)
    {
        var gallery = product.MediaGallery
            .Where(item => !string.IsNullOrWhiteSpace(item.ImageUrl))
            .Select(item => new ProductGalleryItem(
                item.ImageUrl!,
                string.IsNullOrWhiteSpace(item.ThumbnailUrl) ? item.ImageUrl! : item.ThumbnailUrl!,
                ResolveGalleryAltText(item.AltText, product.Name)))
            .ToArray();

        if (gallery.Length > 0)
        {
            return gallery;
        }

        return string.IsNullOrWhiteSpace(product.Image)
            ? []
            :
            [
                new ProductGalleryItem(
                    product.Image!,
                    product.Image!,
                    ResolveGalleryAltText(null, product.Name)),
            ];
    }

    private static string ResolveGalleryAltText(string? altText, string? productName)
    {
        if (!string.IsNullOrWhiteSpace(altText))
        {
            return altText.Trim();
        }

        return string.IsNullOrWhiteSpace(productName) ? "Product image" : productName.Trim();
    }

    private static ProductPurchasePanelModel BuildPurchasePanel(
        GetProduct product,
        IReadOnlyList<ProductGalleryItem> galleryItems,
        StorefrontDisplayContext displayContext,
        IStorefrontPriceFormatter priceFormatter)
    {
        var displayCurrencyCode = ResolveDisplayCurrencyCode(product, displayContext);
        var displayPriceAmount = ResolveDisplayPriceAmount(product);
        var activeVariationOptions = ActiveVariationOptions(product);
        var canSubmitInitialPurchase = CanSubmitInitialPurchase(product);

        return new ProductPurchasePanelModel(
            product.Id,
            string.IsNullOrWhiteSpace(product.Name) ? "Product" : product.Name,
            displayCurrencyCode,
            displayPriceAmount.ToString("0.00", CultureInfo.InvariantCulture),
            product.Variants.FirstOrDefault(variant => variant.IsDefault)?.Id,
            product.Variants.FirstOrDefault(variant => variant.IsDefault)?.Sku ?? product.Sku,
            product.Gtin,
            galleryItems.FirstOrDefault()?.ImageUrl,
            ResolveInitialStockValue(product),
            product.MinOrderQuantity,
            product.MaxOrderQuantity,
            product.QuantityStep,
            product.FreeShipping,
            product.DeliveryEstimateText,
            canSubmitInitialPurchase,
            product.Variants.Any()
                ? "Choose a size, add it to your cart, and review it in the storefront cart before checkout."
                : product.Purchasable
                    ? "Add this item now and review it in the storefront cart before checkout."
                    : "This product cannot be added to cart right now.",
            PurchaseBlockMessage(product),
            BuildInitialValidationMessages(product, canSubmitInitialPurchase),
            activeVariationOptions
                .Select(option => new ProductPurchaseOptionItem(
                    option.Name!,
                    option.IsRequired,
                    option.ControlType,
                    ActiveVariationValues(option)
                        .Select(value => new ProductPurchaseOptionValueItem(value.Value!, value.ColorHex))
                        .ToArray()))
                .ToArray(),
            product.Variants
                .Select(variant => new ProductPurchaseVariantItem(
                    variant.Id,
                    VariantDisplayName(variant),
                    VariantAttributeText(variant),
                    VariantOptionLabel(variant, product, displayCurrencyCode, displayContext, priceFormatter),
                    variant.SizeValue,
                    variant.Sku,
                    variant.Stock,
                    variant.IsDefault,
                    VariantPriceValue(variant, product),
                    VariantCurrencyCode(variant, displayCurrencyCode),
                    FormatPrice(GetVariantDisplayPrice(variant, product), VariantCurrencyCode(variant, displayCurrencyCode), displayContext, priceFormatter)))
                .ToArray(),
            StorefrontRoutes.Cart);
    }

    private static StorefrontProductPricingView BuildPricing(
        GetProduct product,
        StorefrontDisplayContext displayContext,
        IStorefrontPriceFormatter priceFormatter,
        string displayCurrencyCode,
        decimal displayPriceAmount)
    {
        var comparePriceAmount = product.DisplayComparePrice ?? product.ComparePrice;

        return new StorefrontProductPricingView(
            product.Variants.Any() ? "From" : "Price",
            FormatPrice(displayPriceAmount, displayCurrencyCode, displayContext, priceFormatter),
            comparePriceAmount is not null && comparePriceAmount > displayPriceAmount
                ? FormatPrice(comparePriceAmount.Value, displayCurrencyCode, displayContext, priceFormatter)
                : null,
            displayCurrencyCode);
    }

    private static StorefrontProductAvailabilityView BuildAvailability(GetProduct product)
    {
        var variantCount = product.Variants.Count();
        if (variantCount > 0)
        {
            var availableVariantCount = product.Variants.Count(variant => variant.Stock > 0);
            var state = availableVariantCount > 0 ? "available" : "out-of-stock";
            return new StorefrontProductAvailabilityView(
                state,
                availableVariantCount > 0 ? "Available" : "Out of stock",
                $"{availableVariantCount} options in stock",
                $"{variantCount} options");
        }

        if (product.ManageStock == false)
        {
            return new StorefrontProductAvailabilityView("available", "Available", "Available", "Single option");
        }

        var stock = ResolveInitialStockValue(product);
        return stock > 0
            ? new StorefrontProductAvailabilityView("available", "Available", $"{stock} in stock", "Single option")
            : new StorefrontProductAvailabilityView("out-of-stock", "Out of stock", "Out of stock", "Single option");
    }

    private static StorefrontProductPurchaseView BuildPurchase(GetProduct product, bool canSubmitInitialPurchase)
    {
        var defaultSku = product.Variants.FirstOrDefault(variant => variant.IsDefault)?.Sku ?? product.Sku;

        return new StorefrontProductPurchaseView(
            canSubmitInitialPurchase,
            product.Variants.Any()
                ? "Choose a size, add it to your cart, and review it in the storefront cart before checkout."
                : product.Purchasable
                    ? "Add this item now and review it in the storefront cart before checkout."
                    : "This product cannot be added to cart right now.",
            PurchaseBlockMessage(product),
            string.IsNullOrWhiteSpace(defaultSku) ? string.Empty : $"SKU {defaultSku}",
            string.IsNullOrWhiteSpace(product.Gtin) ? string.Empty : $"GTIN {product.Gtin}",
            product.MinOrderQuantity,
            product.MaxOrderQuantity,
            ResolveInitialStockValue(product));
    }

    private static IReadOnlyList<StorefrontProductVariantView> BuildVariants(
        GetProduct product,
        StorefrontDisplayContext displayContext,
        IStorefrontPriceFormatter priceFormatter,
        string displayCurrencyCode)
    {
        return product.Variants
            .Select(variant => new StorefrontProductVariantView(
                variant.Id,
                VariantDisplayName(variant),
                VariantAttributeText(variant),
                FormatPrice(GetVariantDisplayPrice(variant, product), VariantCurrencyCode(variant, displayCurrencyCode), displayContext, priceFormatter),
                VariantStockLabel(variant),
                variant.Stock > 0 ? "available" : "out-of-stock",
                variant.IsDefault))
            .ToArray();
    }

    private static StorefrontProductNavigationView BuildNavigation(GetProduct product)
    {
        return new StorefrontProductNavigationView(
            product.Category?.Name,
            !string.IsNullOrWhiteSpace(product.Category?.Slug) ? StorefrontRoutes.Category(product.Category.Slug) : null,
            !string.IsNullOrWhiteSpace(product.FullDescription) ? product.FullDescription! : product.Description ?? string.Empty,
            product.Name is { Length: > 0 } productName ? $"More about {productName}" : string.Empty);
    }

    private static IReadOnlyList<string> BuildInitialValidationMessages(GetProduct product, bool canSubmitInitialPurchase)
    {
        if (canSubmitInitialPurchase)
        {
            return [];
        }

        return product.PurchaseBlockReasons.Count > 0
            ? product.PurchaseBlockReasons
                .Select(reason => FormatPurchaseBlockReason(product, reason))
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .ToArray()
            : [PurchaseBlockMessage(product)];
    }

    private static bool CanSubmitInitialPurchase(GetProduct product)
    {
        return !product.PurchaseBlockReasons.Any(IsInitialPurchaseHardBlock);
    }

    private static bool IsInitialPurchaseHardBlock(string reason) =>
        reason is "not_visible"
            or "not_published"
            or "not_started"
            or "expired"
            or "purchase_disabled"
            or "variant_inactive"
            or "out_of_stock"
            or "not_enough_stock"
            or "above_max_quantity";

    private static string PurchaseBlockMessage(GetProduct product) => product.PurchaseBlockReasons.FirstOrDefault() switch
    {
        "purchase_disabled" => "Purchasing is paused.",
        "out_of_stock" => "Currently out of stock.",
        "below_min_quantity" => $"Minimum order quantity is {product.MinOrderQuantity}.",
        "above_max_quantity" => $"Maximum order quantity is {product.MaxOrderQuantity}.",
        _ => "Currently unavailable.",
    };

    private static int ResolveInitialStockValue(GetProduct product)
    {
        return product.ManageStock == false
            ? 999999
            : Math.Max(0, product.AvailableQuantity > 0 ? product.AvailableQuantity.Value : product.Quantity);
    }

    private static decimal ResolveDisplayPriceAmount(GetProduct product)
    {
        return product.DisplayPrice ?? product.Price;
    }

    private static string ResolveDisplayCurrencyCode(GetProduct product, StorefrontDisplayContext displayContext)
    {
        return NormalizeCurrencyCode(product.DisplayCurrencyCode) ?? displayContext.DefaultCurrencyCode;
    }

    private static bool IsFreshArrival(DateTime createdOn)
    {
        return DateTime.UtcNow.Subtract(createdOn).TotalDays <= 7;
    }

    private static string VariantStockLabel(GetProductVariant variant)
    {
        return variant.Stock > 0 ? $"{variant.Stock} in stock" : "Out of stock";
    }

    private static string FormatPurchaseBlockReason(GetProduct product, string reason)
    {
        return reason switch
        {
            "purchase_disabled" => "Purchasing is paused.",
            "out_of_stock" => "Currently out of stock.",
            "below_min_quantity" => $"Minimum order quantity is {product.MinOrderQuantity}.",
            "above_max_quantity" => $"Maximum order quantity is {product.MaxOrderQuantity}.",
            _ => PurchaseBlockMessage(product),
        };
    }

    private static string VariantOptionLabel(
        GetProductVariant variant,
        GetProduct product,
        string displayCurrencyCode,
        StorefrontDisplayContext displayContext,
        IStorefrontPriceFormatter priceFormatter)
    {
        var defaultLabel = variant.IsDefault ? " - Default" : string.Empty;
        var stockLabel = variant.Stock > 0 ? $"{variant.Stock} left" : "Out of stock";
        return $"{VariantDisplayName(variant)} - {FormatPrice(GetVariantDisplayPrice(variant, product), VariantCurrencyCode(variant, displayCurrencyCode), displayContext, priceFormatter)} - {stockLabel}{defaultLabel}";
    }

    private static string VariantPriceValue(GetProductVariant variant, GetProduct product)
    {
        return GetVariantDisplayPrice(variant, product).ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static decimal GetVariantPrice(GetProductVariant variant, GetProduct product)
    {
        return variant.EffectivePrice > 0 ? variant.EffectivePrice : variant.Price ?? product.Price;
    }

    private static decimal GetVariantDisplayPrice(GetProductVariant variant, GetProduct product)
    {
        return variant.DisplayPrice ?? GetVariantPrice(variant, product);
    }

    private static string VariantCurrencyCode(GetProductVariant variant, string displayCurrencyCode)
    {
        return NormalizeCurrencyCode(variant.DisplayCurrencyCode) ?? displayCurrencyCode;
    }

    private static string FormatPrice(
        decimal amount,
        string currencyCode,
        StorefrontDisplayContext displayContext,
        IStorefrontPriceFormatter priceFormatter)
    {
        return priceFormatter.Format(amount, displayContext with { CurrencyCode = currencyCode });
    }

    private static IReadOnlyList<StorefrontVariationOptionDto> ActiveVariationOptions(GetProduct product)
    {
        return product.VariationTemplate?.Options
            .Where(option => !string.IsNullOrWhiteSpace(option.Name))
            .Where(option => ActiveVariationValues(option).Count > 0)
            .ToArray()
        ?? [];
    }

    private static IReadOnlyList<StorefrontVariationValueDto> ActiveVariationValues(StorefrontVariationOptionDto option)
    {
        return option.Values
            .Where(value => !string.IsNullOrWhiteSpace(value.Value))
            .ToArray();
    }

    private static string? NormalizeCurrencyCode(string? currencyCode)
    {
        var normalized = currencyCode?.Trim().ToUpperInvariant();
        return normalized is { Length: 3 } && normalized.All(char.IsLetter)
            ? normalized
            : null;
    }

    private static string ScaleLabel(int scale)
    {
        return scale switch
        {
            1 => "Clothing",
            2 => "Clothing EU",
            10 => "Shoes EU",
            11 => "Shoes US",
            12 => "Shoes UK",
            _ => "Variant",
        };
    }

    private static string VariantDisplayName(GetProductVariant variant)
    {
        if (!string.IsNullOrWhiteSpace(variant.DisplayName))
        {
            return variant.DisplayName;
        }

        var attributeText = VariantAttributeText(variant);
        return string.IsNullOrWhiteSpace(attributeText) ? variant.SizeValue : attributeText;
    }

    private static string VariantAttributeText(GetProductVariant variant)
    {
        var attributeText = string.Join(" / ", variant.Attributes.Select(attribute => $"{attribute.Name}: {attribute.Value}"));
        return string.IsNullOrWhiteSpace(attributeText)
            ? ScaleLabel(variant.SizeScale)
            : attributeText;
    }
}
