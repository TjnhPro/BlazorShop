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
        public static BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutPreviewRequest ToApplicationRequest(
            this Contracts.Storefront.StorefrontCheckoutPreviewRequest request,
            Guid storeId,
            string cartToken,
            string? customerAppUserId = null)
        {
            return new BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutPreviewRequest(
                storeId,
                cartToken,
                request.ExpectedCartVersion,
                request.CustomerEmail,
                request.CustomerName,
                request.PaymentMethodKey,
                request.ShippingAddress?.ToPreviewShippingAddress(),
                request.ShippingAddressId,
                request.BillingAddressId,
                request.UseShippingAddressAsBillingAddress,
                customerAppUserId);
        }
        public static StorefrontCheckoutPreviewResponse ToStorefrontContract(this StorefrontCheckoutPreviewResult result)
        {
            return new StorefrontCheckoutPreviewResponse(
                result.CheckoutSessionId,
                result.CartId,
                result.CheckoutVersion,
                result.CartVersion,
                result.LastValidatedCartVersion,
                result.State,
                result.CurrentStep,
                result.CompletedSteps,
                result.IsValid,
                result.NextAction,
                result.CustomerEmail,
                result.CustomerName,
                result.PaymentMethodKey,
                result.Subtotal,
                result.ShippingTotal,
                result.TaxTotal,
                result.DiscountTotal,
                result.GrandTotal,
                result.CurrencyCode,
                result.ExpiresAtUtc,
                result.Lines.Select(line => new StorefrontCheckoutLineSummaryResponse(
                    line.LineId,
                    line.ProductId,
                    line.ProductVariantId,
                    line.Quantity,
                    line.UnitPrice,
                    line.LineTotal,
                    line.CurrencyCode)).ToArray(),
                result.Issues.Select(issue => new StorefrontCheckoutValidationIssueResponse(
                    issue.Code,
                    issue.Message,
                    issue.Field,
                    issue.LineId,
                    issue.ProductId)).ToArray());
        }
        public static StorefrontCheckoutSessionResponse ToStorefrontContract(this StorefrontCheckoutSessionResult result)
        {
            return new StorefrontCheckoutSessionResponse(
                result.CheckoutSessionId,
                result.CartId,
                result.CheckoutVersion,
                result.CartVersion,
                result.LastValidatedCartVersion,
                result.State,
                result.CurrentStep,
                result.CompletedSteps,
                result.IsActive,
                result.NextAction,
                result.CustomerEmail,
                result.CustomerName,
                result.PaymentMethodKey,
                result.Subtotal,
                result.ShippingTotal,
                result.TaxTotal,
                result.DiscountTotal,
                result.GrandTotal,
                result.CurrencyCode,
                result.ExpiresAtUtc,
                result.ShippingRequired,
                result.SelectedShippingOption?.ToStorefrontContract(),
                result.ShippingOptions.Select(option => option.ToStorefrontContract()).ToArray(),
                result.SelectedPaymentMethod?.ToStorefrontContract(),
                result.PaymentMethods.Select(method => method.ToStorefrontContract()).ToArray(),
                result.Lines.Select(line => new StorefrontCheckoutLineSummaryResponse(
                    line.LineId,
                    line.ProductId,
                    line.ProductVariantId,
                    line.Quantity,
                    line.UnitPrice,
                    line.LineTotal,
                    line.CurrencyCode)).ToArray(),
                result.Issues.Select(issue => new StorefrontCheckoutValidationIssueResponse(
                    issue.Code,
                    issue.Message,
                    issue.Field,
                    issue.LineId,
                    issue.ProductId)).ToArray());
        }
        public static StorefrontCheckoutReviewResponse ToStorefrontContract(this StorefrontCheckoutReviewResult result)
        {
            return new StorefrontCheckoutReviewResponse(
                result.CheckoutSessionId,
                result.CartId,
                result.CheckoutVersion,
                result.CartVersion,
                result.LastValidatedCartVersion,
                result.State,
                result.CurrentStep,
                result.CompletedSteps,
                result.IsActive,
                result.NextAction,
                result.CustomerEmail,
                result.CustomerName,
                result.BillingAddress?.ToStorefrontContract(),
                result.ShippingAddress?.ToStorefrontContract(),
                result.SelectedShippingOption?.ToStorefrontContract(),
                result.SelectedPaymentMethod?.ToStorefrontContract(),
                result.Lines.Select(line => new StorefrontCheckoutLineSummaryResponse(
                    line.LineId,
                    line.ProductId,
                    line.ProductVariantId,
                    line.Quantity,
                    line.UnitPrice,
                    line.LineTotal,
                    line.CurrencyCode)).ToArray(),
                result.Subtotal,
                result.ShippingTotal,
                result.TaxTotal,
                result.DiscountTotal,
                result.GrandTotal,
                result.CurrencyCode,
                result.TermsRequired,
                result.TermsAccepted,
                result.TermsVersion,
                result.TermsAcceptedAtUtc,
                result.PlaceOrderAllowed,
                result.NextRequiredStep,
                result.Issues.Select(issue => new StorefrontCheckoutValidationIssueResponse(
                    issue.Code,
                    issue.Message,
                    issue.Field,
                    issue.LineId,
                    issue.ProductId)).ToArray());
        }
        public static BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutAddressStepRequest ToApplicationRequest(
            this Contracts.Storefront.StorefrontCheckoutAddressStepRequest request,
            Guid storeId,
            Guid checkoutSessionId,
            string cartToken,
            string? customerAppUserId = null)
        {
            return new BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutAddressStepRequest(
                storeId,
                checkoutSessionId,
                cartToken,
                request.BillingAddress?.ToPreviewShippingAddress(),
                request.ShippingAddress?.ToPreviewShippingAddress(),
                request.BillingAddressId,
                request.ShippingAddressId,
                request.UseBillingAddressAsShippingAddress,
                customerAppUserId);
        }
        public static BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutShippingMethodRequest ToApplicationRequest(
            this Contracts.Storefront.StorefrontCheckoutShippingMethodRequest request,
            Guid storeId,
            Guid checkoutSessionId,
            string cartToken)
        {
            return new BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutShippingMethodRequest(
                storeId,
                checkoutSessionId,
                cartToken,
                request.ShippingOptionKey);
        }
        public static BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutPaymentMethodRequest ToApplicationRequest(
            this Contracts.Storefront.StorefrontCheckoutPaymentMethodRequest request,
            Guid storeId,
            Guid checkoutSessionId,
            string cartToken)
        {
            return new BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutPaymentMethodRequest(
                storeId,
                checkoutSessionId,
                cartToken,
                request.PaymentMethodKey);
        }
        public static BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutReviewRequest ToApplicationRequest(
            this Contracts.Storefront.StorefrontCheckoutReviewRequest request,
            Guid storeId,
            Guid checkoutSessionId,
            string cartToken)
        {
            return new BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutReviewRequest(
                storeId,
                checkoutSessionId,
                cartToken,
                request.TermsAccepted,
                request.TermsVersion);
        }
        private static StorefrontCheckoutShippingOptionResponse ToStorefrontContract(
            this StorefrontCheckoutShippingOption option)
        {
            return new StorefrontCheckoutShippingOptionResponse(
                option.Key,
                option.ProviderSystemName,
                option.MethodCode,
                option.DisplayName,
                option.Description,
                option.Price,
                option.CurrencyCode,
                option.DeliveryEstimateText,
                option.Selected);
        }
        private static StorefrontCheckoutPaymentMethodOptionResponse ToStorefrontContract(
            this StorefrontCheckoutPaymentMethodOption option)
        {
            return new StorefrontCheckoutPaymentMethodOptionResponse(
                option.Key,
                option.DisplayName,
                option.Description,
                option.ShortDisplayText,
                option.IconUrl,
                option.ProviderKey,
                option.NextActionKind,
                option.Selected);
        }
        public static BlazorShop.Application.CommerceNode.Checkout.StorefrontPlaceOrderRequest ToApplicationRequest(
            this Contracts.Storefront.StorefrontPlaceOrderRequest request,
            Guid storeId)
        {
            return new BlazorShop.Application.CommerceNode.Checkout.StorefrontPlaceOrderRequest(
                storeId,
                request.CheckoutSessionId,
                request.ExpectedCheckoutVersion,
                request.ExpectedCartVersion,
                request.IdempotencyKey);
        }
        public static StorefrontPlaceOrderResponse ToStorefrontContract(this StorefrontPlaceOrderResult result)
        {
            var nextAction = string.IsNullOrWhiteSpace(result.NextActionType)
                ? null
                : new StorefrontPaymentNextActionResponse(result.NextActionType, result.NextActionUrl);

            return new StorefrontPlaceOrderResponse(
                result.CheckoutSessionId,
                result.PaymentAttemptId,
                result.OrderId,
                result.Reference,
                result.OrderStatus,
                result.PaymentStatus,
                result.PaymentMethodKey,
                result.TotalAmount,
                result.CurrencyCode,
                result.IdempotencyKey,
                result.CreatedOn,
                result.GuestAccessToken,
                nextAction);
        }
    }
}
