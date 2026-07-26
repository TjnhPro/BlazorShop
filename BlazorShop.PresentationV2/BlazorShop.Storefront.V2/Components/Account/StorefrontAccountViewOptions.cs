namespace BlazorShop.Storefront.Components.Account;

using BlazorShop.Storefront.Components.Headless.Account;

public static class StorefrontAccountViewOptions
{
    public static IReadOnlyList<AccountNavigationItem> NavigationItems { get; } =
    [
        new("profile", "Profile", "/account/profile"),
        new("orders", "Orders", "/account/orders"),
        new("addresses", "Addresses", "/account/addresses"),
        new("change-password", "Password", "/account/change-password")
    ];

    public static AccountNavigationClasses NavigationClasses { get; } = new()
    {
        Nav = "rounded border border-neutral-200 bg-white p-4 text-sm",
        ActiveLink = "mt-2 block rounded bg-neutral-900 px-3 py-2 font-semibold text-white first:mt-0",
        InactiveLink = "mt-2 block rounded px-3 py-2 font-semibold text-neutral-700 hover:bg-neutral-100 first:mt-0"
    };
}
