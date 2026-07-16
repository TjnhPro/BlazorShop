namespace BlazorShop.Application.CommerceNode.ProductSelections
{
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities;

    public enum ProductSelectionMode
    {
        Preview = 0,
        Cart = 1,
    }

    public sealed record ProductSelectionRequest(
        Guid StoreId,
        Guid ProductId,
        Guid? ProductVariantId = null,
        IReadOnlyList<SelectedAttributeDto>? SelectedAttributes = null,
        string? SelectedAttributesJson = null,
        int Quantity = 1,
        string? CurrencyCode = null,
        ProductSelectionMode Mode = ProductSelectionMode.Cart);

    public sealed record ProductSelectionResult(
        bool Success,
        ServiceResponseType ResponseType,
        string Message,
        Guid ProductId,
        Guid? ProductVariantId,
        IReadOnlyList<SelectedAttributeDto> SelectedAttributes,
        string? SelectedAttributesJson,
        string? AttributeSignature,
        bool IsValid,
        bool IsAvailable,
        bool CanAddToCart,
        IReadOnlyList<string> ValidationMessages,
        string? Sku,
        string? DisplayName,
        decimal UnitPrice,
        decimal BaseUnitPrice,
        string CurrencyCode,
        string BaseCurrencyCode,
        decimal? ComparePrice,
        int StockQuantity,
        int MinQuantity,
        int MaxQuantity,
        decimal? ExchangeRate = null,
        string? ExchangeRateProviderKey = null,
        string? ExchangeRateSource = null,
        DateTimeOffset? ExchangeRateEffectiveAtUtc = null,
        DateTimeOffset? ExchangeRateExpiresAtUtc = null,
        Product? Product = null,
        ProductVariant? Variant = null);

    public interface IProductSelectionResolver
    {
        Task<ProductSelectionResult> ResolveAsync(
            ProductSelectionRequest request,
            CancellationToken cancellationToken = default);
    }
}
