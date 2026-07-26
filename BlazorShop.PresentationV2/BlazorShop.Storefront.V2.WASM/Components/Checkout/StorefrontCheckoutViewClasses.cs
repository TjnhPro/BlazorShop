namespace BlazorShop.Storefront.V2.WASM.Components.Checkout;

public sealed record StorefrontCheckoutViewClasses
{
    public static StorefrontCheckoutViewClasses Empty { get; } = new();

    public string Shell { get; init; } = string.Empty;
    public string HeaderLayout { get; init; } = string.Empty;
    public string Eyebrow { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string BodyText { get; init; } = string.Empty;
    public string RefreshButton { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public string MetricsGrid { get; init; } = string.Empty;
    public string MetricCard { get; init; } = string.Empty;
    public string MetricValue { get; init; } = string.Empty;
    public string IssuePanel { get; init; } = string.Empty;
    public string OptionGrid { get; init; } = string.Empty;
    public string OptionPanel { get; init; } = string.Empty;
    public string OptionList { get; init; } = string.Empty;
    public string OptionLabel { get; init; } = string.Empty;
    public string PrimaryButton { get; init; } = string.Empty;
    public string SecondaryButton { get; init; } = string.Empty;
}
