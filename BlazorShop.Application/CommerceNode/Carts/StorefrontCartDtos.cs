namespace BlazorShop.Application.CommerceNode.Carts
{
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    public sealed record StorefrontCartCreateOrResumeRequest(
        Guid StoreId,
        string? Token = null,
        Guid? CustomerId = null,
        string? AppUserId = null);

    public sealed record StorefrontCartResult(
        StorefrontCartSessionDto Cart,
        string? Token = null,
        IReadOnlyList<StorefrontCartValidationIssueDto>? Issues = null);

    public sealed record StorefrontCartAddLineRequest(
        Guid StoreId,
        string Token,
        Guid ProductId,
        Guid? ProductVariantId = null,
        IReadOnlyList<SelectedAttributeDto>? SelectedAttributes = null,
        string? PersonalizationHash = null,
        string? PersonalizationJson = null,
        Guid? ArtworkAssetId = null,
        int? ArtworkVersion = null,
        string? FulfillmentProviderKey = null,
        int Quantity = 1,
        string? CurrencyCode = null);

    public sealed record StorefrontCartUpdateLineRequest(
        Guid StoreId,
        string Token,
        Guid LineId,
        int Quantity);

    public sealed record StorefrontCartValidationResult(
        Guid CartPublicId,
        int Version,
        bool IsValid,
        decimal TotalAmount,
        string CurrencyCode,
        IReadOnlyList<StorefrontCartValidationIssueDto> Issues);

    public sealed record StorefrontCartValidationIssueDto(
        Guid? LineId,
        Guid? ProductId,
        string Code,
        string Message);

    public sealed record StorefrontCartSessionCreateRequest(
        Guid StoreId,
        Guid? CustomerId = null,
        string? AppUserId = null,
        DateTimeOffset? ExpiresAtUtc = null);

    public sealed record StorefrontCartSessionCreated(
        Guid Id,
        Guid PublicId,
        Guid StoreId,
        string Token,
        string State,
        int Version,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset CreatedAtUtc);

    public sealed record StorefrontCartSessionDto(
        Guid Id,
        Guid PublicId,
        Guid StoreId,
        Guid? CustomerId,
        string? AppUserId,
        string State,
        int Version,
        DateTimeOffset LastActivityAtUtc,
        DateTimeOffset ExpiresAtUtc,
        IReadOnlyList<StorefrontCartLineDto> Lines);

    public sealed record StorefrontCartLineMutationRequest(
        Guid StoreId,
        string Token,
        Guid ProductId,
        Guid? ProductVariantId = null,
        string? SelectedAttributesJson = null,
        string? PersonalizationHash = null,
        string? PersonalizationJson = null,
        Guid? ArtworkAssetId = null,
        int? ArtworkVersion = null,
        string? FulfillmentProviderKey = null,
        int Quantity = 1,
        decimal? UnitPriceSnapshot = null,
        string? CurrencyCodeSnapshot = null,
        decimal? BaseUnitPriceSnapshot = null,
        string? BaseCurrencyCodeSnapshot = null,
        decimal? ExchangeRateSnapshot = null,
        string? ExchangeRateProviderKey = null,
        string? ExchangeRateSource = null,
        DateTimeOffset? ExchangeRateEffectiveAtUtc = null,
        DateTimeOffset? ExchangeRateExpiresAtUtc = null);

    public sealed record StorefrontCartLineDto(
        Guid Id,
        Guid ProductId,
        Guid? ProductVariantId,
        string LineKey,
        string? SelectedAttributesJson,
        string? PersonalizationHash,
        string? PersonalizationJson,
        Guid? ArtworkAssetId,
        int? ArtworkVersion,
        string? FulfillmentProviderKey,
        int Quantity,
        decimal? UnitPriceSnapshot,
        string? CurrencyCodeSnapshot,
        decimal? BaseUnitPriceSnapshot,
        string? BaseCurrencyCodeSnapshot,
        decimal? ExchangeRateSnapshot,
        string? ExchangeRateProviderKey,
        string? ExchangeRateSource,
        DateTimeOffset? ExchangeRateEffectiveAtUtc,
        DateTimeOffset? ExchangeRateExpiresAtUtc,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);
}
