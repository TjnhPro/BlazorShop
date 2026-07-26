namespace BlazorShop.Storefront.Components.Contracts.Account;

public sealed record AccountRouteDescriptor(
    string ProfileRoute,
    string AddressesRoute,
    string OrdersRoute,
    string ChangePasswordRoute,
    string ProfileSegment,
    string AddressesSegment,
    string OrdersSegment,
    string ChangePasswordSegment,
    string ReceiptSegment)
{
    public static AccountRouteDescriptor Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);
}

public enum AccountRouteKind
{
    Unknown,
    Profile,
    Addresses,
    Orders,
    OrderDetail,
    ChangePassword
}

public sealed record AccountRouteMatch(
    AccountRouteKind Kind,
    string NavRoute,
    string? OrderReference,
    bool ReceiptMode);

public static class AccountRouteParser
{
    public static AccountRouteMatch Resolve(string? path, AccountRouteDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var normalized = Normalize(path);
        if (normalized.Length == 0 || Matches(normalized, descriptor.ProfileSegment))
        {
            return new AccountRouteMatch(AccountRouteKind.Profile, descriptor.ProfileRoute, null, false);
        }

        if (Matches(normalized, descriptor.AddressesSegment))
        {
            return new AccountRouteMatch(AccountRouteKind.Addresses, descriptor.AddressesRoute, null, false);
        }

        if (Matches(normalized, descriptor.OrdersSegment))
        {
            return new AccountRouteMatch(AccountRouteKind.Orders, descriptor.OrdersRoute, null, false);
        }

        if (Matches(normalized, descriptor.ChangePasswordSegment))
        {
            return new AccountRouteMatch(AccountRouteKind.ChangePassword, descriptor.ChangePasswordRoute, null, false);
        }

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length is 2 or 3 && Matches(segments[0], descriptor.OrdersSegment))
        {
            var receiptMode = segments.Length == 3 && Matches(segments[2], descriptor.ReceiptSegment);
            if (segments.Length == 2 || receiptMode)
            {
                var reference = Uri.UnescapeDataString(segments[1]);
                if (!string.IsNullOrWhiteSpace(reference))
                {
                    return new AccountRouteMatch(AccountRouteKind.OrderDetail, descriptor.OrdersRoute, reference, receiptMode);
                }
            }
        }

        return new AccountRouteMatch(AccountRouteKind.Unknown, descriptor.ProfileRoute, null, false);
    }

    private static string Normalize(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : path.Trim().Trim('/');
    }

    private static bool Matches(string value, string expected)
    {
        return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
    }
}
