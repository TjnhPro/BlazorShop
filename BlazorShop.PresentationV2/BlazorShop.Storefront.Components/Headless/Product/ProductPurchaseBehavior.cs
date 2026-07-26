namespace BlazorShop.Storefront.Components.Headless.Product;

using BlazorShop.Storefront.Components.Contracts.Product;

public sealed record ProductPurchaseSnapshot(
    ProductPurchasePanelModel Model,
    ProductPurchaseActionDescriptor Actions)
{
    public bool HasVariationTemplate => Model.VariationOptions.Count > 0;

    public bool UsesVariantSelect => Model.Variants.Count > 0 && !HasVariationTemplate;

    public string InitialSelectionMessage => Model.InitialValidationMessages.Count > 0
        ? Model.InitialValidationMessages[0]
        : Model.CanSubmitInitialPurchase ? "Selection ready." : Model.PurchaseBlockMessage;

    public static ProductPurchaseSnapshot Create(
        ProductPurchasePanelModel model,
        ProductPurchaseActionDescriptor actions)
    {
        return new ProductPurchaseSnapshot(model, actions);
    }
}

public sealed record ProductPurchaseSelectionState(
    Guid? SelectedVariantId,
    IReadOnlyDictionary<string, string> SelectedAttributes,
    int Quantity,
    IReadOnlyList<string> ValidationMessages,
    bool CanAddToCart,
    bool IsPreviewPending,
    string? PreviewError,
    bool IsAddToCartPending,
    string? AddToCartError,
    bool AddToCartSuccess)
{
    public static ProductPurchaseSelectionState FromSnapshot(ProductPurchasePanelModel model)
    {
        return new ProductPurchaseSelectionState(
            model.ResolvedVariantId,
            new Dictionary<string, string>(StringComparer.Ordinal),
            model.MinOrderQuantity,
            model.InitialValidationMessages,
            model.CanSubmitInitialPurchase,
            false,
            null,
            false,
            null,
            false);
    }
}

public sealed record ProductPurchaseActionDescriptor(
    string PanelId,
    string SelectionPreviewRoute,
    string FeedbackElementId,
    string? VariantSelectId,
    string QuantityInputId)
{
    public string PreviewContainerSelector => $"#{PanelId}";

    public string FeedbackTargetSelector => $"#{FeedbackElementId}";

    public string? VariantSelectSelector => string.IsNullOrWhiteSpace(VariantSelectId) ? null : $"#{VariantSelectId}";

    public static ProductPurchaseActionDescriptor Empty { get; } = new(
        "product-purchase",
        string.Empty,
        "product-purchase-feedback",
        "product-variant-select",
        "product-selection-quantity");

    public static ProductPurchaseActionDescriptor StorefrontV2Default { get; } = new(
        "purchase",
        "/api/product-selection-preview",
        "product-cart-feedback",
        "product-variant-select",
        "product-selection-quantity");
}
