namespace BlazorShop.Storefront.Presentation.Services.Account
{
    using BlazorShop.Storefront.Components.Contracts.Account;
    using BlazorShop.Storefront.Components.Headless.Account;

    public sealed record StorefrontAccountPageContext(
        string? Path,
        int Page,
        string? Error,
        string? Saved,
        string? AntiforgeryFieldName,
        string? AntiforgeryRequestToken)
    {
        public StorefrontAccountProfileActionDescriptor ProfileActions { get; init; } = StorefrontAccountProfileActionDescriptor.Empty;

        public StorefrontAccountPasswordActionDescriptor PasswordActions { get; init; } = StorefrontAccountPasswordActionDescriptor.Empty;

        public StorefrontAccountAddressActionDescriptor AddressActions { get; init; } = StorefrontAccountAddressActionDescriptor.Empty;

        public StorefrontAccountOrderActionDescriptor OrderActions { get; init; } = StorefrontAccountOrderActionDescriptor.Empty;

        public AccountRouteDescriptor RouteDescriptor { get; init; } = AccountRouteDescriptor.Empty;

        public IReadOnlyList<AccountNavigationItem> NavigationItems { get; init; } = [];
    }
}
