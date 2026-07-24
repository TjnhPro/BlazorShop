namespace BlazorShop.Storefront.Endpoints
{
    using System.Globalization;
    using BlazorShop.Storefront.Configuration;
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Web.SharedV2;
    using Microsoft.AspNetCore.Antiforgery;
    internal static partial class StorefrontLocalEndpointSupport
    {
        internal static async Task<IResult> ToLocalCartMutationResultAsync(
        StorefrontCartMutationResult result,
        IStorefrontDisplayContextProvider displayContextProvider,
        IStorefrontPriceFormatter priceFormatter,
        CancellationToken cancellationToken)
    {
        if (result.Success)
        {
            var displayContext = await displayContextProvider.GetAsync(cancellationToken);
            return Results.Ok(ToLocalCartResponse(result.Cart, displayContext, priceFormatter));
        }
    
        return LocalCartValidationError(result.Message);
    }

        internal static StorefrontBrowserCart ToLocalCartResponse(
        StorefrontCartResponse? cart,
        StorefrontDisplayContext displayContext,
        IStorefrontPriceFormatter priceFormatter)
    {
        var lines = ToLocalCartLines(cart?.Lines ?? [], cart?.CurrencyCode, displayContext, priceFormatter);
        var count = cart is not null && cart.SummaryCount > 0
            ? cart.SummaryCount
            : lines.Sum(line => Math.Max(0, line.Quantity));
        var currencyCode = NormalizeCurrencyCode(cart?.CurrencyCode) ?? lines
            .Select(line => line.CurrencyCode)
            .Distinct(StringComparer.Ordinal)
            .SingleOrDefault()
            ?? displayContext.CurrencyCode;
        var subtotal = cart?.Subtotal ?? lines.Sum(line => line.LineTotal);
        var grandTotal = cart?.GrandTotal ?? lines.Sum(line => line.LineTotal);
    
        return new StorefrontBrowserCart(
            count,
            cart?.Version ?? 0,
            lines,
            currencyCode,
            subtotal,
            FormatLocalCartPrice(subtotal, currencyCode, displayContext, priceFormatter),
            grandTotal,
            FormatLocalCartPrice(grandTotal, currencyCode, displayContext, priceFormatter),
            cart?.CheckoutAllowed ?? lines.All(line => !line.IsUnavailable),
            (cart?.Warnings ?? [])
                .Select(warning => new StorefrontBrowserCartWarning(warning.Message))
                .ToArray(),
            (cart?.Adjustments ?? [])
                .Select(adjustment => new StorefrontBrowserCartAdjustment(
                    adjustment.Label,
                    adjustment.Amount,
                    FormatLocalCartPrice(adjustment.Amount, NormalizeCurrencyCode(adjustment.CurrencyCode) ?? currencyCode, displayContext, priceFormatter)))
                .ToArray());
    }

        internal static IReadOnlyList<StorefrontBrowserCartLine> ToLocalCartLines(
        IEnumerable<StorefrontCartLineResponse> cartItems,
        string? cartCurrencyCode,
        StorefrontDisplayContext displayContext,
        IStorefrontPriceFormatter priceFormatter)
    {
        var lines = new List<StorefrontBrowserCartLine>();
        foreach (var cartItem in cartItems)
        {
            var quantity = Math.Max(1, cartItem.Quantity);
            var currencyCode = NormalizeCurrencyCode(cartItem.CurrencyCodeSnapshot) ?? NormalizeCurrencyCode(cartCurrencyCode) ?? displayContext.CurrencyCode;
            var unitPrice = cartItem.UnitPrice ?? cartItem.UnitPriceSnapshot ?? 0m;
            var lineTotal = cartItem.LineTotal ?? cartItem.LineSubtotal ?? (unitPrice * quantity);
            lines.Add(new StorefrontBrowserCartLine(
                cartItem.LineId,
                cartItem.ProductId,
                cartItem.ProductVariantId,
                string.IsNullOrWhiteSpace(cartItem.DisplayName) ? "Cart item" : cartItem.DisplayName,
                ResolveLocalCartProductUrl(cartItem),
                cartItem.ImageUrl,
                quantity,
                unitPrice,
                FormatLocalCartPrice(unitPrice, currencyCode, displayContext, priceFormatter),
                lineTotal,
                FormatLocalCartPrice(lineTotal, currencyCode, displayContext, priceFormatter),
                currencyCode,
                ResolveLocalCartSelectedAttributes(cartItem.SelectedAttributes),
                Math.Max(1, cartItem.QuantityMinimum),
                cartItem.QuantityMaximum,
                Math.Max(1, cartItem.QuantityStep),
                (cartItem.Warnings ?? [])
                    .Select(warning => warning.Message)
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Select(message => new StorefrontBrowserCartWarning(message))
                    .ToArray(),
                !cartItem.Purchasable || (cartItem.Warnings?.Count ?? 0) > 0));
        }
    
        return lines;
    }

        internal static string FormatLocalCartPrice(
        decimal amount,
        string currencyCode,
        StorefrontDisplayContext displayContext,
        IStorefrontPriceFormatter priceFormatter)
    {
        return priceFormatter.Format(amount, displayContext with { CurrencyCode = currencyCode });
    }

        internal static string? ResolveLocalCartProductUrl(StorefrontCartLineResponse cartItem)
    {
        if (!string.IsNullOrWhiteSpace(cartItem.ProductSlug))
        {
            return StorefrontRoutes.Product(cartItem.ProductSlug);
        }
    
        return string.IsNullOrWhiteSpace(cartItem.ProductUrl) ? null : cartItem.ProductUrl;
    }

        internal static string? ResolveLocalCartSelectedAttributes(IReadOnlyList<StorefrontCartSelectedAttributeResponse>? attributes)
    {
        var attributeText = string.Join(
            " / ",
            (attributes ?? [])
                .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Name) || !string.IsNullOrWhiteSpace(attribute.Value))
                .Select(attribute => $"{attribute.Name}: {attribute.Value}"));
        return string.IsNullOrWhiteSpace(attributeText) ? null : attributeText;
    }
    }
}
