using BlazorShop.Storefront.Components.Contracts.Account;

namespace BlazorShop.Storefront.V2.WASM.Components.Account;

public static class StorefrontAccountViewOptions
{
    public static StorefrontAccountProfileLabels ProfileLabels { get; } = new()
    {
        MissingProfile = "Profile could not be loaded.",
        EmailAddress = "Email address",
        DisplayName = "Display name",
        FirstName = "First name",
        LastName = "Last name",
        Company = "Company",
        Phone = "Phone",
        Language = "Language",
        Currency = "Currency",
        SaveProfile = "Save profile",
        Saving = "Saving...",
        SavedSuccess = "Profile updated."
    };

    public static StorefrontAccountPasswordLabels PasswordLabels { get; } = new()
    {
        CurrentPassword = "Current password",
        NewPassword = "New password",
        ConfirmNewPassword = "Confirm new password",
        ChangePassword = "Change password",
        Changing = "Changing...",
        SavedSuccess = "Password changed."
    };

    public static AccountNavigationClasses NavigationClasses { get; } = new()
    {
        Nav = "rounded-3xl border border-neutral-200/70 bg-white/95 p-3 text-sm shadow-lg lg:sticky lg:top-24",
        ActiveLink = "mt-2 flex items-center rounded-2xl bg-neutral-950 px-4 py-3 font-semibold text-white shadow-sm first:mt-0",
        InactiveLink = "mt-2 flex items-center rounded-2xl px-4 py-3 font-semibold text-neutral-700 hover:bg-neutral-100 hover:text-neutral-950 first:mt-0"
    };

    public static StorefrontAccountFormClasses FormClasses { get; } = new()
    {
        Root = "space-y-6",
        StatusAlert = "rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-800",
        ErrorAlert = "rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-800",
        MissingProfile = "text-sm text-neutral-700",
        ProfileForm = "grid max-w-3xl gap-5 sm:grid-cols-2",
        PasswordForm = "max-w-xl space-y-5",
        Field = "block text-sm",
        WideField = "block text-sm sm:col-span-2",
        LabelText = "font-semibold text-neutral-800",
        Input = "mt-2 min-h-11 w-full rounded-xl border border-neutral-300 bg-white px-3 text-sm text-neutral-900 outline-none focus:border-neutral-600 focus:ring-2 focus:ring-neutral-100",
        CurrencyInput = "mt-2 min-h-11 w-full rounded-xl border border-neutral-300 bg-white px-3 text-sm uppercase text-neutral-900 outline-none focus:border-neutral-600 focus:ring-2 focus:ring-neutral-100",
        ActionRow = "sm:col-span-2",
        SubmitButton = "inline-flex items-center rounded bg-amber-500 px-5 py-3 text-sm font-semibold text-white hover:bg-amber-600 disabled:cursor-wait disabled:bg-amber-300"
    };

    public static StorefrontAccountAddressBookClasses AddressClasses { get; } = new()
    {
        Root = "space-y-6",
        StatusAlert = "rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-800",
        ErrorAlert = "rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-800",
        AddSection = "rounded-2xl border border-neutral-200 bg-neutral-50/70 p-5",
        AddTitle = "text-lg font-bold text-neutral-900",
        AddForm = "mt-4 grid gap-4 sm:grid-cols-2",
        ActionRow = "sm:col-span-2",
        PrimaryButton = "rounded bg-amber-500 px-4 py-2 text-sm font-semibold text-white hover:bg-amber-600 disabled:cursor-wait disabled:bg-amber-300",
        EmptyState = "text-sm text-neutral-700",
        ListGrid = "grid gap-4 xl:grid-cols-2",
        Card = "rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm",
        BadgeRow = "flex flex-wrap gap-2 text-xs font-semibold uppercase text-neutral-500",
        ShippingBadge = "rounded-full bg-emerald-50 px-2.5 py-1 text-emerald-700",
        BillingBadge = "rounded-full bg-sky-50 px-2.5 py-1 text-sky-700",
        CardTitle = "mt-3 text-base font-bold text-neutral-900",
        AddressText = "text-sm text-neutral-700",
        AddressTextSpaced = "mt-1 text-sm text-neutral-700",
        EditForm = "mt-4 grid gap-3",
        EditFieldsGrid = "grid gap-3 sm:grid-cols-2",
        EditActions = "flex flex-wrap gap-2",
        SecondaryButton = "rounded border border-neutral-300 bg-white px-3 py-2 text-sm font-semibold text-neutral-800 hover:bg-neutral-50",
        DangerButton = "rounded border border-rose-300 bg-white px-3 py-2 text-sm font-semibold text-rose-700 hover:bg-rose-50",
        CompactField = "grid gap-1 text-xs font-semibold uppercase text-neutral-500",
        CompactWideField = "grid gap-1 text-xs font-semibold uppercase text-neutral-500 sm:col-span-2",
        FullField = "grid gap-1 text-sm font-semibold text-neutral-700",
        FullWideField = "grid gap-1 text-sm font-semibold text-neutral-700 sm:col-span-2",
        CompactInput = "rounded-xl border border-neutral-300 bg-white px-3 py-2 text-sm font-normal normal-case text-neutral-900 outline-none focus:border-neutral-600 focus:ring-2 focus:ring-neutral-100",
        FullInput = "rounded-xl border border-neutral-300 bg-white px-3 py-2 font-normal text-neutral-900 outline-none focus:border-neutral-600 focus:ring-2 focus:ring-neutral-100"
    };

    public static StorefrontAccountOrderListClasses OrderListClasses { get; } = new()
    {
        Root = "space-y-5",
        ErrorAlert = "rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-800",
        EmptyState = "rounded-2xl border border-dashed border-neutral-300 bg-neutral-50 px-5 py-8 text-center text-sm text-neutral-700",
        TableWrapper = "overflow-x-auto rounded-2xl border border-neutral-200",
        Table = "min-w-full divide-y divide-neutral-200 text-sm",
        TableHead = "bg-neutral-50 text-left text-xs font-semibold uppercase text-neutral-500",
        HeaderCell = "px-4 py-3",
        TableBody = "divide-y divide-neutral-100",
        ReferenceCell = "px-4 py-4 font-semibold text-neutral-900",
        ReferenceLink = "underline decoration-neutral-300 underline-offset-4 hover:decoration-neutral-900",
        Cell = "px-4 py-4 text-neutral-700",
        StrongCell = "px-4 py-4 text-neutral-900"
    };

    public static StorefrontAccountOrderDetailClasses OrderDetailClasses { get; } = new()
    {
        Root = "space-y-8",
        ErrorAlert = "rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-800",
        MetricsGrid = "grid gap-5 md:grid-cols-3",
        MetricLabel = "text-xs font-semibold uppercase text-neutral-500",
        MetricValue = "mt-1 font-semibold text-neutral-900",
        AddressGrid = "mt-8 grid gap-5 lg:grid-cols-2",
        AddressSection = "rounded-2xl border border-neutral-200 bg-neutral-50/70 p-5",
        AddressTitle = "font-bold text-neutral-900",
        AddressBody = "mt-3 text-sm text-neutral-700",
        AddressStrongLine = "font-semibold text-neutral-900",
        ItemsSection = "mt-8",
        SectionTitle = "font-bold text-neutral-900",
        ItemsList = "mt-3 divide-y divide-neutral-100 rounded-2xl border border-neutral-200",
        LineRow = "grid gap-2 p-4 sm:grid-cols-[minmax(0,1fr)_80px_120px]",
        LineName = "font-semibold text-neutral-900",
        LineSku = "text-sm text-neutral-500",
        LineText = "text-sm text-neutral-700",
        LineTotal = "text-sm font-semibold text-neutral-900",
        TotalsSection = "mt-8 max-w-md rounded-2xl border border-neutral-200 bg-neutral-50/70 p-5",
        TotalsBody = "mt-3 space-y-2 text-sm",
        TotalRow = "flex justify-between gap-4",
        GrandTotalRow = "flex justify-between gap-4 border-t border-neutral-200 pt-3 font-bold text-neutral-900"
    };

    public static StorefrontAccountShellClasses ShellClasses { get; } = new()
    {
        Section = "mx-auto max-w-7xl px-4 pb-12 pt-10 sm:px-6 lg:px-8",
        Layout = "grid gap-6 lg:grid-cols-[240px_minmax(0,1fr)] lg:items-start",
        ContentArticle = "rounded-3xl border border-neutral-200/70 bg-white/95 shadow-lg",
        Header = "border-b border-neutral-200 px-6 py-7 sm:px-8",
        Eyebrow = "text-sm font-semibold uppercase tracking-[0.2em] text-neutral-500",
        Title = "mt-2 text-4xl font-extrabold tracking-tight text-neutral-900",
        Body = "px-6 py-7 sm:px-8",
        UnknownAlert = "rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-800"
    };
}
