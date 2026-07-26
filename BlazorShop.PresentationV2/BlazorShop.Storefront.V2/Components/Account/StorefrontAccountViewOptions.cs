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

    public static StorefrontAccountAddressActionDescriptor AddressActions { get; } = new(
        "/account/addresses",
        "/api/account/addresses",
        "/api/account/addresses",
        "/api/account/addresses/{addressId}",
        "/api/account/addresses/{addressId}",
        "/api/account/addresses/{addressId}/default-shipping",
        "/api/account/addresses/{addressId}/default-billing");

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

    public static StorefrontAccountAddressBookClasses AddressClasses { get; } = new()
    {
        Root = string.Empty,
        StatusAlert = "mb-5 rounded border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-800",
        ErrorAlert = "mb-5 rounded border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-800",
        AddSection = "mb-8 rounded border border-neutral-200 p-4",
        AddTitle = "text-lg font-bold text-neutral-900",
        AddForm = "mt-4 grid gap-4 sm:grid-cols-2",
        ActionRow = "sm:col-span-2",
        PrimaryButton = "rounded bg-neutral-900 px-4 py-2 text-sm font-semibold text-white disabled:cursor-wait disabled:bg-neutral-500",
        EmptyState = "text-sm text-neutral-700",
        ListGrid = "grid gap-4 xl:grid-cols-2",
        Card = "rounded border border-neutral-200 p-4",
        BadgeRow = "flex flex-wrap gap-2 text-xs font-semibold uppercase text-neutral-500",
        ShippingBadge = "rounded bg-emerald-50 px-2 py-1 text-emerald-700",
        BillingBadge = "rounded bg-sky-50 px-2 py-1 text-sky-700",
        CardTitle = "mt-3 text-base font-bold text-neutral-900",
        AddressText = "text-sm text-neutral-700",
        AddressTextSpaced = "mt-1 text-sm text-neutral-700",
        EditForm = "mt-4 grid gap-3",
        EditFieldsGrid = "grid gap-3 sm:grid-cols-2",
        EditActions = "flex flex-wrap gap-2",
        SecondaryButton = "rounded border border-neutral-300 px-3 py-2 text-sm font-semibold text-neutral-800",
        DangerButton = "rounded border border-rose-300 px-3 py-2 text-sm font-semibold text-rose-700",
        CompactField = "grid gap-1 text-xs font-semibold uppercase text-neutral-500",
        CompactWideField = "grid gap-1 text-xs font-semibold uppercase text-neutral-500 sm:col-span-2",
        FullField = "grid gap-1 text-sm font-semibold text-neutral-700",
        FullWideField = "grid gap-1 text-sm font-semibold text-neutral-700 sm:col-span-2",
        CompactInput = "rounded border border-neutral-300 px-3 py-2 text-sm font-normal normal-case text-neutral-900",
        FullInput = "rounded border border-neutral-300 px-3 py-2 font-normal"
    };
}
