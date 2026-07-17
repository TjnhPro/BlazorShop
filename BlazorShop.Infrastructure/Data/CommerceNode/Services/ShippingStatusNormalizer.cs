namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Domain.Constants;

    internal static class ShippingStatusNormalizer
    {
        private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["PendingShipment"] = ShippingStatuses.NotYetShipped,
            ["NotYetShipped"] = ShippingStatuses.NotYetShipped,
            [ShippingStatuses.NotYetShipped] = ShippingStatuses.NotYetShipped,
            ["Shipped"] = ShippingStatuses.Shipped,
            [ShippingStatuses.Shipped] = ShippingStatuses.Shipped,
            ["InTransit"] = "in_transit",
            ["in_transit"] = "in_transit",
            ["OutForDelivery"] = "out_for_delivery",
            ["out_for_delivery"] = "out_for_delivery",
            ["Delivered"] = ShippingStatuses.Delivered,
            [ShippingStatuses.Delivered] = ShippingStatuses.Delivered,
            ["ShippingNotRequired"] = ShippingStatuses.ShippingNotRequired,
            [ShippingStatuses.ShippingNotRequired] = ShippingStatuses.ShippingNotRequired,
        };

        public static bool TryNormalize(string? value, out string normalized)
        {
            normalized = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return Aliases.TryGetValue(value.Trim(), out normalized!);
        }

        public static string NormalizeOrOriginal(string? value)
        {
            return TryNormalize(value, out var normalized)
                ? normalized
                : value?.Trim() ?? string.Empty;
        }

        public static bool IsCompleteAllowed(string? value)
        {
            return TryNormalize(value, out var normalized)
                && (normalized is ShippingStatuses.Shipped
                    or ShippingStatuses.Delivered
                    or ShippingStatuses.ShippingNotRequired);
        }

        public static IReadOnlyList<string> GetLookupAliases(string normalized)
        {
            return Aliases
                .Where(item => string.Equals(item.Value, normalized, StringComparison.OrdinalIgnoreCase))
                .SelectMany(item => new[] { item.Key, item.Value })
                .Append(normalized)
                .Select(item => item.ToLowerInvariant())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
    }
}
