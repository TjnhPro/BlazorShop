namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.IdentityModel.Tokens.Jwt;

    using BlazorShop.Application.CommerceNode.Addresses;
    using BlazorShop.Application.CommerceNode.Captcha;
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Consent;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Domain.Contracts;
    public static partial class StorefrontContractMappings
    {
        public static ProcessCart ToProcessCart(this StorefrontCartItemRequest request)
        {
            return new ProcessCart
            {
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                SelectedAttributes = request.SelectedAttributes,
                Quantity = request.Quantity,
            };
        }
        public static StorefrontCartItemRequest ToStorefrontContract(this ProcessCart request)
        {
            return new StorefrontCartItemRequest
            {
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                SelectedAttributes = request.SelectedAttributes,
                Quantity = request.Quantity,
            };
        }
        public static StorefrontCartAddLineRequest ToApplicationRequest(
            this StorefrontCartLineCreateRequest request,
            Guid storeId,
            string cartToken)
        {
            return new StorefrontCartAddLineRequest(
                storeId,
                cartToken,
                request.ProductId,
                request.ProductVariantId,
                request.SelectedAttributes,
                request.PersonalizationHash,
                request.PersonalizationJson,
                request.ArtworkAssetId,
                request.ArtworkVersion,
                request.FulfillmentProviderKey,
                request.Quantity,
                request.CurrencyCode);
        }
        public static StorefrontCartUpdateLineRequest ToApplicationRequest(
            this StorefrontCartLineUpdateRequest request,
            Guid storeId,
            string cartToken,
            Guid lineId)
        {
            return new StorefrontCartUpdateLineRequest(
                storeId,
                cartToken,
                lineId,
                request.Quantity);
        }
        public static StorefrontCartSessionResponse ToSessionContract(this StorefrontCartResult result, string? fallbackCartToken = null)
        {
            return new StorefrontCartSessionResponse(
                result.Cart.PublicId,
                result.Token ?? fallbackCartToken ?? string.Empty,
                result.Cart.State,
                result.Cart.Version,
                result.Cart.ExpiresAtUtc);
        }
        public static StorefrontCartResponse ToStorefrontContract(this StorefrontCartSessionDto cart)
        {
            return new StorefrontCartResponse(
                cart.PublicId,
                cart.State,
                cart.Version,
                cart.LastActivityAtUtc,
                cart.ExpiresAtUtc,
                cart.Lines.Select(line => line.ToStorefrontContract()).ToArray(),
                cart.CurrencyCode,
                cart.SummaryCount,
                cart.Subtotal,
                cart.DiscountTotal,
                cart.ShippingEstimate,
                cart.TaxEstimate,
                cart.GrandTotal,
                cart.CheckoutAllowed,
                cart.Warnings?.Select(warning => warning.ToStorefrontContract()).ToArray() ?? [],
                cart.Adjustments?.Select(adjustment => adjustment.ToStorefrontContract()).ToArray() ?? []);
        }
        public static StorefrontCartLineResponse ToStorefrontContract(this StorefrontCartLineDto line)
        {
            return new StorefrontCartLineResponse(
                line.Id,
                line.ProductId,
                line.ProductVariantId,
                line.SelectedAttributesJson,
                line.PersonalizationHash,
                line.PersonalizationJson,
                line.ArtworkAssetId,
                line.ArtworkVersion,
                line.FulfillmentProviderKey,
                line.Quantity,
                line.UnitPriceSnapshot,
                line.CurrencyCodeSnapshot,
                line.DisplayName,
                line.ProductSlug,
                line.ProductUrl,
                line.ImageUrl,
                line.SelectedAttributes?
                    .Select(attribute => new StorefrontCartSelectedAttributeResponse(attribute.Name, attribute.Value))
                    .ToArray() ?? [],
                line.UnitPrice,
                line.LineSubtotal,
                line.LineTotal,
                line.QuantityMinimum,
                line.QuantityMaximum,
                line.QuantityStep,
                line.AllowedQuantities,
                line.Purchasable,
                line.Warnings?.Select(warning => warning.ToStorefrontContract()).ToArray() ?? []);
        }
        public static StorefrontCartWarningResponse ToStorefrontContract(this StorefrontCartWarningDto warning)
        {
            return new StorefrontCartWarningResponse(
                warning.Code,
                warning.Message,
                warning.LineId,
                warning.ProductId);
        }
        public static StorefrontCartAdjustmentResponse ToStorefrontContract(this StorefrontCartAdjustmentDto adjustment)
        {
            return new StorefrontCartAdjustmentResponse(
                adjustment.Code,
                adjustment.Label,
                adjustment.Amount,
                adjustment.CurrencyCode);
        }
        public static StorefrontCartValidationResponse ToStorefrontContract(this StorefrontCartValidationResult validation)
        {
            return new StorefrontCartValidationResponse(
                validation.CartPublicId,
                validation.Version,
                validation.IsValid,
                validation.TotalAmount,
                validation.CurrencyCode,
                validation.Issues.Select(issue => issue.ToStorefrontContract()).ToArray());
        }
        public static StorefrontCartValidationIssueResponse ToStorefrontContract(this StorefrontCartValidationIssueDto issue)
        {
            return new StorefrontCartValidationIssueResponse(
                issue.LineId,
                issue.ProductId,
                issue.Code,
                issue.Message);
        }
    }
}
