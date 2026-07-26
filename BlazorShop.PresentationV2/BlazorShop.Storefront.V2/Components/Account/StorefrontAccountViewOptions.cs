namespace BlazorShop.Storefront.Components.Account;

using BlazorShop.Storefront.Components.Headless.Account;

public static class StorefrontAccountViewOptions
{
    public static StorefrontAccountProfileActionDescriptor ProfileActions { get; } = new(
        "/account/profile",
        "/api/account/profile",
        "/api/account/profile");

    public static StorefrontAccountPasswordActionDescriptor PasswordActions { get; } = new(
        "/account/change-password",
        "/api/account/change-password");

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

    public static StorefrontAccountFormClasses FormClasses { get; } = new()
    {
        Root = string.Empty,
        StatusAlert = "mb-5 rounded border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-800",
        ErrorAlert = "mb-5 rounded border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-800",
        MissingProfile = "text-sm text-neutral-700",
        ProfileForm = "grid max-w-3xl gap-5 sm:grid-cols-2",
        PasswordForm = "max-w-xl space-y-5",
        Field = "block text-sm",
        WideField = "block text-sm sm:col-span-2",
        LabelText = "font-semibold text-neutral-800",
        Input = "mt-2 min-h-11 w-full rounded border border-neutral-300 px-3 text-sm",
        CurrencyInput = "mt-2 min-h-11 w-full rounded border border-neutral-300 px-3 text-sm uppercase",
        ActionRow = "sm:col-span-2",
        SubmitButton = "inline-flex rounded bg-neutral-900 px-5 py-3 text-sm font-semibold text-white hover:bg-neutral-800 disabled:cursor-wait disabled:bg-neutral-500"
    };
}
