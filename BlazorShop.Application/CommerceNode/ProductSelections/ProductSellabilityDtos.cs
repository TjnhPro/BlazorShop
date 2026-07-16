namespace BlazorShop.Application.CommerceNode.ProductSelections
{
    using BlazorShop.Domain.Entities;

    public static class ProductPurchaseBlockReasons
    {
        public const string NotVisible = "not_visible";
        public const string NotPublished = "not_published";
        public const string NotStarted = "not_started";
        public const string Expired = "expired";
        public const string PurchaseDisabled = "purchase_disabled";
        public const string VariantRequired = "variant_required";
        public const string VariantInactive = "variant_inactive";
        public const string OutOfStock = "out_of_stock";
        public const string BelowMinQuantity = "below_min_quantity";
        public const string AboveMaxQuantity = "above_max_quantity";
        public const string InvalidQuantityStep = "invalid_quantity_step";
        public const string NotEnoughStock = "not_enough_stock";
    }

    public static class ProductStockStatuses
    {
        public const string InStock = "in_stock";
        public const string OutOfStock = "out_of_stock";
        public const string NotManaged = "not_managed";
        public const string VariantRequired = "variant_required";
    }

    public enum ProductSellabilityMode
    {
        Storefront = 0,
        Internal = 1,
    }

    public sealed record ProductSellabilityRequest(
        Guid StoreId,
        Product Product,
        ProductVariant? Variant = null,
        int RequestedQuantity = 1,
        DateTimeOffset? NowUtc = null,
        ProductSellabilityMode Mode = ProductSellabilityMode.Storefront);

    public sealed record ProductSellabilityResult(
        Guid ProductId,
        Guid? ProductVariantId,
        bool Purchasable,
        IReadOnlyList<string> PurchaseBlockReasons,
        IReadOnlyList<string> PurchaseBlockMessages,
        string StockStatus,
        int? AvailableQuantity,
        int MinOrderQuantity,
        int? MaxOrderQuantity,
        int QuantityStep,
        bool ManageStock,
        bool ShippingRequired,
        bool FreeShipping,
        string? DeliveryEstimateText);

    public interface IProductSellabilityResolver
    {
        ProductSellabilityResult Resolve(ProductSellabilityRequest request);
    }
}
