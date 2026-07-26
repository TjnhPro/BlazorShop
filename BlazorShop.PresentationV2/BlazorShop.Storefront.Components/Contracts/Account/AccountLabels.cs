namespace BlazorShop.Storefront.Components.Contracts.Account;

public sealed record AccountLabels(
    string Profile,
    string Addresses,
    string Orders,
    string Password,
    string Save,
    string Saving,
    string Delete,
    string Loading)
{
    public static AccountLabels Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);
}
