namespace BlazorShop.Storefront.Components.Contracts.Cart;

public sealed record StorefrontCartViewClasses
{
    public static StorefrontCartViewClasses Empty { get; } = new();

    public string PageSection { get; init; } = string.Empty;
    public string Layout { get; init; } = string.Empty;
    public string ContentColumn { get; init; } = string.Empty;
    public string HeaderCard { get; init; } = string.Empty;
    public string HeaderLayout { get; init; } = string.Empty;
    public string Eyebrow { get; init; } = string.Empty;
    public string HeaderTitle { get; init; } = string.Empty;
    public string BodyText { get; init; } = string.Empty;
    public string CountBadge { get; init; } = string.Empty;
    public string Alert { get; init; } = string.Empty;
    public string ErrorAlert { get; init; } = string.Empty;
    public string WarningAlert { get; init; } = string.Empty;
    public string EmptyState { get; init; } = string.Empty;
    public string EmptyTitle { get; init; } = string.Empty;
    public string EmptyActions { get; init; } = string.Empty;
    public string PrimaryLink { get; init; } = string.Empty;
    public string SecondaryLink { get; init; } = string.Empty;
    public string LineList { get; init; } = string.Empty;
    public string LineCard { get; init; } = string.Empty;
    public string LineLayout { get; init; } = string.Empty;
    public string LineImageFrame { get; init; } = string.Empty;
    public string LineImage { get; init; } = string.Empty;
    public string LineTitle { get; init; } = string.Empty;
    public string LineMeta { get; init; } = string.Empty;
    public string LineWarning { get; init; } = string.Empty;
    public string LineControls { get; init; } = string.Empty;
    public string LineMetrics { get; init; } = string.Empty;
    public string MetricLabel { get; init; } = string.Empty;
    public string MetricValue { get; init; } = string.Empty;
    public string QuantityInput { get; init; } = string.Empty;
    public string RemoveButton { get; init; } = string.Empty;
    public string SummaryAside { get; init; } = string.Empty;
    public string SummaryCard { get; init; } = string.Empty;
    public string SummaryRows { get; init; } = string.Empty;
    public string SummaryRow { get; init; } = string.Empty;
    public string CheckoutButton { get; init; } = string.Empty;
    public string DisabledCheckoutButton { get; init; } = string.Empty;
    public string ClearButton { get; init; } = string.Empty;
}
