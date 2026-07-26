namespace BlazorShop.Storefront.Components.Account;

public sealed record AccountNavigationClasses
{
    public static AccountNavigationClasses Empty { get; } = new();

    public string Nav { get; init; } = string.Empty;

    public string ActiveLink { get; init; } = string.Empty;

    public string InactiveLink { get; init; } = string.Empty;
}

public sealed record StorefrontAccountFormClasses
{
    public static StorefrontAccountFormClasses Empty { get; } = new();

    public string Root { get; init; } = string.Empty;
    public string StatusAlert { get; init; } = string.Empty;
    public string ErrorAlert { get; init; } = string.Empty;
    public string MissingProfile { get; init; } = string.Empty;
    public string ProfileForm { get; init; } = string.Empty;
    public string PasswordForm { get; init; } = string.Empty;
    public string Field { get; init; } = string.Empty;
    public string WideField { get; init; } = string.Empty;
    public string LabelText { get; init; } = string.Empty;
    public string Input { get; init; } = string.Empty;
    public string CurrencyInput { get; init; } = string.Empty;
    public string ActionRow { get; init; } = string.Empty;
    public string SubmitButton { get; init; } = string.Empty;
}

public sealed record StorefrontAccountAddressBookClasses
{
    public static StorefrontAccountAddressBookClasses Empty { get; } = new();

    public string Root { get; init; } = string.Empty;
    public string StatusAlert { get; init; } = string.Empty;
    public string ErrorAlert { get; init; } = string.Empty;
    public string AddSection { get; init; } = string.Empty;
    public string AddTitle { get; init; } = string.Empty;
    public string AddForm { get; init; } = string.Empty;
    public string ActionRow { get; init; } = string.Empty;
    public string PrimaryButton { get; init; } = string.Empty;
    public string EmptyState { get; init; } = string.Empty;
    public string ListGrid { get; init; } = string.Empty;
    public string Card { get; init; } = string.Empty;
    public string BadgeRow { get; init; } = string.Empty;
    public string ShippingBadge { get; init; } = string.Empty;
    public string BillingBadge { get; init; } = string.Empty;
    public string CardTitle { get; init; } = string.Empty;
    public string AddressText { get; init; } = string.Empty;
    public string AddressTextSpaced { get; init; } = string.Empty;
    public string EditForm { get; init; } = string.Empty;
    public string EditFieldsGrid { get; init; } = string.Empty;
    public string EditActions { get; init; } = string.Empty;
    public string SecondaryButton { get; init; } = string.Empty;
    public string DangerButton { get; init; } = string.Empty;
    public string CompactField { get; init; } = string.Empty;
    public string CompactWideField { get; init; } = string.Empty;
    public string FullField { get; init; } = string.Empty;
    public string FullWideField { get; init; } = string.Empty;
    public string CompactInput { get; init; } = string.Empty;
    public string FullInput { get; init; } = string.Empty;
}

public sealed record StorefrontAccountOrderListClasses
{
    public static StorefrontAccountOrderListClasses Empty { get; } = new();

    public string Root { get; init; } = string.Empty;
    public string ErrorAlert { get; init; } = string.Empty;
    public string EmptyState { get; init; } = string.Empty;
    public string TableWrapper { get; init; } = string.Empty;
    public string Table { get; init; } = string.Empty;
    public string TableHead { get; init; } = string.Empty;
    public string HeaderCell { get; init; } = string.Empty;
    public string TableBody { get; init; } = string.Empty;
    public string ReferenceCell { get; init; } = string.Empty;
    public string ReferenceLink { get; init; } = string.Empty;
    public string Cell { get; init; } = string.Empty;
    public string StrongCell { get; init; } = string.Empty;
}

public sealed record StorefrontAccountOrderDetailClasses
{
    public static StorefrontAccountOrderDetailClasses Empty { get; } = new();

    public string Root { get; init; } = string.Empty;
    public string ErrorAlert { get; init; } = string.Empty;
    public string MetricsGrid { get; init; } = string.Empty;
    public string MetricLabel { get; init; } = string.Empty;
    public string MetricValue { get; init; } = string.Empty;
    public string AddressGrid { get; init; } = string.Empty;
    public string AddressSection { get; init; } = string.Empty;
    public string AddressTitle { get; init; } = string.Empty;
    public string AddressBody { get; init; } = string.Empty;
    public string AddressStrongLine { get; init; } = string.Empty;
    public string ItemsSection { get; init; } = string.Empty;
    public string SectionTitle { get; init; } = string.Empty;
    public string ItemsList { get; init; } = string.Empty;
    public string LineRow { get; init; } = string.Empty;
    public string LineName { get; init; } = string.Empty;
    public string LineSku { get; init; } = string.Empty;
    public string LineText { get; init; } = string.Empty;
    public string LineTotal { get; init; } = string.Empty;
    public string TotalsSection { get; init; } = string.Empty;
    public string TotalsBody { get; init; } = string.Empty;
    public string TotalRow { get; init; } = string.Empty;
    public string GrandTotalRow { get; init; } = string.Empty;
}

public sealed record StorefrontAccountShellClasses
{
    public static StorefrontAccountShellClasses Empty { get; } = new();

    public string Section { get; init; } = string.Empty;
    public string Layout { get; init; } = string.Empty;
    public string ContentArticle { get; init; } = string.Empty;
    public string Header { get; init; } = string.Empty;
    public string Eyebrow { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string UnknownAlert { get; init; } = string.Empty;
}
