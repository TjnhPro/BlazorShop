namespace BlazorShop.Storefront.Presentation.Views.Foundation;

public sealed class StorefrontFoundationViewSet
{
    private Type? visualScripts;
    private Type? VisualScriptsOrNull => this.visualScripts;

    public required Type ApplicationHead { get; init; }

    public Type VisualScripts
    {
        get => this.visualScripts
            ?? throw new InvalidOperationException("A visual script component type must be registered.");
        init => this.visualScripts = value;
    }

    public required Type MainLayout { get; init; }

    public required Type ConsentBanner { get; init; }

    public required Type HomePage { get; init; }

    public required Type CategoryPage { get; init; }

    public required Type ProductPage { get; init; }

    public required Type SearchPage { get; init; }

    public required Type ContentPage { get; init; }

    public required Type CartPage { get; init; }

    public required Type CheckoutPage { get; init; }

    public required Type PaymentResultPage { get; init; }

    public required Type AuthPage { get; init; }

    public required Type AccountPage { get; init; }

    public Type? ComponentMvpLab { get; init; }

    public required Type MaintenanceState { get; init; }

    public required Type NotFoundState { get; init; }

    public required Type ServiceUnavailableState { get; init; }

    public required Type ErrorState { get; init; }

    public IReadOnlyList<StorefrontFoundationViewSlot> GetRequiredSlots()
    {
        return
        [
            new(nameof(this.ApplicationHead), this.ApplicationHead),
            new(nameof(this.VisualScripts), this.VisualScriptsOrNull),
            new(nameof(this.MainLayout), this.MainLayout),
            new(nameof(this.ConsentBanner), this.ConsentBanner),
            new(nameof(this.HomePage), this.HomePage),
            new(nameof(this.CategoryPage), this.CategoryPage),
            new(nameof(this.ProductPage), this.ProductPage),
            new(nameof(this.SearchPage), this.SearchPage),
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

    public IReadOnlyList<StorefrontFoundationViewSlot> GetOptionalSlots()
    {
        return this.ComponentMvpLab is null
            ? []
            : [new(nameof(this.ComponentMvpLab), this.ComponentMvpLab)];
    }
}
