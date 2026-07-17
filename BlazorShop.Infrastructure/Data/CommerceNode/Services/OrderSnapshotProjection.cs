namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Domain.Entities.Payment;

    internal static class OrderSnapshotProjection
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public static GetOrderStoreSnapshot? ToStoreSnapshot(Order order)
        {
            return order.StorePublicId.HasValue
                || !string.IsNullOrWhiteSpace(order.StoreKeySnapshot)
                || !string.IsNullOrWhiteSpace(order.StoreNameSnapshot)
                || !string.IsNullOrWhiteSpace(order.StoreBaseUrlSnapshot)
                || !string.IsNullOrWhiteSpace(order.StoreCompanyNameSnapshot)
                || !string.IsNullOrWhiteSpace(order.StoreCompanyEmailSnapshot)
                || !string.IsNullOrWhiteSpace(order.StoreCompanyPhoneSnapshot)
                || !string.IsNullOrWhiteSpace(order.StoreCompanyAddressSnapshot)
                    ? new GetOrderStoreSnapshot
                    {
                        PublicId = order.StorePublicId,
                        StoreKey = order.StoreKeySnapshot,
                        Name = order.StoreNameSnapshot,
                        BaseUrl = order.StoreBaseUrlSnapshot,
                        CompanyName = order.StoreCompanyNameSnapshot,
                        CompanyEmail = order.StoreCompanyEmailSnapshot,
                        CompanyPhone = order.StoreCompanyPhoneSnapshot,
                        CompanyAddress = order.StoreCompanyAddressSnapshot,
                    }
                    : null;
        }

        public static GetOrderTotalBreakdown? ToTotalBreakdown(
            decimal? subtotal,
            decimal? shippingTotal,
            decimal? taxTotal,
            decimal? discountTotal,
            decimal? grandTotal)
        {
            return subtotal.HasValue
                || shippingTotal.HasValue
                || taxTotal.HasValue
                || discountTotal.HasValue
                || grandTotal.HasValue
                    ? new GetOrderTotalBreakdown
                    {
                        Subtotal = subtotal,
                        ShippingTotal = shippingTotal,
                        TaxTotal = taxTotal,
                        DiscountTotal = discountTotal,
                        GrandTotal = grandTotal,
                    }
                    : null;
        }

        public static GetOrderAddress? ToAddress(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var address = JsonSerializer.Deserialize<StorefrontCheckoutShippingAddressDto>(json, JsonOptions);
                return address is null
                    ? null
                    : new GetOrderAddress
                    {
                        FullName = address.FullName,
                        Email = address.Email,
                        Phone = address.Phone,
                        Address1 = address.Address1,
                        Address2 = address.Address2,
                        City = address.City,
                        State = address.State,
                        PostalCode = address.PostalCode,
                        CountryCode = address.CountryCode,
                    };
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static GetOrderAddress? ToShippingAddressSnapshot(Order order)
        {
            return ToAddress(order.ShippingAddressSnapshotJson)
                ?? new GetOrderAddress
                {
                    FullName = order.ShippingFullName,
                    Email = order.ShippingEmail,
                    Phone = order.ShippingPhone,
                    Address1 = order.ShippingAddress1,
                    Address2 = order.ShippingAddress2,
                    City = order.ShippingCity,
                    State = order.ShippingState,
                    PostalCode = order.ShippingPostalCode,
                    CountryCode = order.ShippingCountryCode,
                };
        }

        public static GetOrderShippingMethodSnapshot? ToShippingMethod(Order order)
        {
            return !string.IsNullOrWhiteSpace(order.ShippingMethodKey)
                || !string.IsNullOrWhiteSpace(order.ShippingProviderSystemName)
                || !string.IsNullOrWhiteSpace(order.ShippingMethodCode)
                || !string.IsNullOrWhiteSpace(order.ShippingMethodName)
                || order.ShippingTotal != 0m
                || !string.IsNullOrWhiteSpace(order.ShippingCurrencyCode)
                || !string.IsNullOrWhiteSpace(order.ShippingDeliveryEstimateText)
                    ? new GetOrderShippingMethodSnapshot
                    {
                        Key = order.ShippingMethodKey,
                        ProviderSystemName = order.ShippingProviderSystemName,
                        MethodCode = order.ShippingMethodCode,
                        Name = order.ShippingMethodName,
                        Total = order.ShippingTotal,
                        CurrencyCode = order.ShippingCurrencyCode,
                        DeliveryEstimateText = order.ShippingDeliveryEstimateText,
                    }
                    : null;
        }
    }
}
