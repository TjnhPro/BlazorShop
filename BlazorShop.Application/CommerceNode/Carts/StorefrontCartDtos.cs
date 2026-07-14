namespace BlazorShop.Application.CommerceNode.Carts
{
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
        string? CurrencyCodeSnapshot = null);

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
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);
}
