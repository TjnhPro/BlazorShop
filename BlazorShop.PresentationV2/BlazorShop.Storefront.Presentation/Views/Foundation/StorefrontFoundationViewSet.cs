namespace BlazorShop.Storefront.Presentation.Views.Foundation;

public sealed class StorefrontFoundationViewSet
{
    public required Type ApplicationHead { get; init; }

    public required Type ApplicationScripts { get; init; }

    public required Type MainLayout { get; init; }

    public required Type HomePage { get; init; }

    public required Type CategoryPage { get; init; }

    public required Type ProductPage { get; init; }

    public required Type SearchPage { get; init; }

    public required Type DealsPage { get; init; }

    public required Type NewReleasesPage { get; init; }

    public required Type ContentPage { get; init; }

    public required Type CartPage { get; init; }

    public required Type CheckoutPage { get; init; }

    public required Type PaymentResultPage { get; init; }

    public required Type AuthPage { get; init; }

    public required Type AccountPage { get; init; }

    public required Type MaintenanceState { get; init; }

    public required Type NotFoundState { get; init; }

    public required Type ServiceUnavailableState { get; init; }

    public required Type ErrorState { get; init; }

    public static StorefrontFoundationViewSet CreateMinimal(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        return new StorefrontFoundationViewSet
        {
            ApplicationHead = componentType,
            ApplicationScripts = componentType,
            MainLayout = componentType,
            HomePage = componentType,
            CategoryPage = componentType,
            ProductPage = componentType,
            SearchPage = componentType,
            DealsPage = componentType,
            NewReleasesPage = componentType,
            ContentPage = componentType,
            CartPage = componentType,
            CheckoutPage = componentType,
            PaymentResultPage = componentType,
            AuthPage = componentType,
            AccountPage = componentType,
            MaintenanceState = componentType,
            NotFoundState = componentType,
            ServiceUnavailableState = componentType,
            ErrorState = componentType,
        };
    }

    public IReadOnlyList<StorefrontFoundationViewSlot> GetRequiredSlots()
    {
        return
        [
            new(nameof(this.ApplicationHead), this.ApplicationHead),
            new(nameof(this.ApplicationScripts), this.ApplicationScripts),
            new(nameof(this.MainLayout), this.MainLayout),
            new(nameof(this.HomePage), this.HomePage),
            new(nameof(this.CategoryPage), this.CategoryPage),
            new(nameof(this.ProductPage), this.ProductPage),
            new(nameof(this.SearchPage), this.SearchPage),
            new(nameof(this.DealsPage), this.DealsPage),
            new(nameof(this.NewReleasesPage), this.NewReleasesPage),
            new(nameof(this.ContentPage), this.ContentPage),
            new(nameof(this.CartPage), this.CartPage),
            new(nameof(this.CheckoutPage), this.CheckoutPage),
            new(nameof(this.PaymentResultPage), this.PaymentResultPage),
            new(nameof(this.AuthPage), this.AuthPage),
            new(nameof(this.AccountPage), this.AccountPage),
            new(nameof(this.MaintenanceState), this.MaintenanceState),
            new(nameof(this.NotFoundState), this.NotFoundState),
            new(nameof(this.ServiceUnavailableState), this.ServiceUnavailableState),
            new(nameof(this.ErrorState), this.ErrorState),
        ];
    }
}
