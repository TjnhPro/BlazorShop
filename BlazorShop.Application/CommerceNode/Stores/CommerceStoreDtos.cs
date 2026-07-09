namespace BlazorShop.Application.CommerceNode.Stores
{
    public sealed record CommerceStoreListQuery(
        string? Status = null,
        int Skip = 0,
        int Take = 100);

    public sealed record CreateCommerceStoreRequest(
        string StoreKey,
        string Name,
        string? BaseUrl = null,
        bool ForceHttps = true,
        bool SslEnabled = true,
        int? SslPort = null,
        int DisplayOrder = 0,
        string? HtmlBodyId = null,
        string? CdnHost = null,
        string? LogoUrl = null,
        string? FaviconUrl = null,
        string? PngIconUrl = null,
        string? AppleTouchIconUrl = null,
        string? MsTileImageUrl = null,
        string? MsTileColor = null,
        string DefaultCurrencyCode = "USD",
        string DefaultCulture = "en-US",
        string? SupportEmail = null,
        string? SupportPhone = null,
        bool MaintenanceModeEnabled = false,
        string? MaintenanceMessage = null,
        string? MetadataJson = null,
        string? PrimaryDomain = null);

    public sealed record UpdateCommerceStoreRequest(
        string Name,
        string? BaseUrl = null,
        bool ForceHttps = true,
        bool SslEnabled = true,
        int? SslPort = null,
        int DisplayOrder = 0,
        string? HtmlBodyId = null,
        string? CdnHost = null,
        string? LogoUrl = null,
        string? FaviconUrl = null,
        string? PngIconUrl = null,
        string? AppleTouchIconUrl = null,
        string? MsTileImageUrl = null,
        string? MsTileColor = null,
        string DefaultCurrencyCode = "USD",
        string DefaultCulture = "en-US",
        string? SupportEmail = null,
        string? SupportPhone = null,
        bool MaintenanceModeEnabled = false,
        string? MaintenanceMessage = null,
        string? MetadataJson = null,
        string? Status = null);

    public sealed record CreateCommerceStoreDomainRequest(
        string Domain,
        bool IsPrimary = false);

    public sealed record CommerceStoreListResponse(
        IReadOnlyList<CommerceStoreSummary> Items,
        int TotalCount,
        int Skip,
        int Take);

    public sealed record CommerceStoreSummary(
        Guid PublicId,
        string StoreKey,
        string Name,
        string Status,
        string? BaseUrl,
        int DisplayOrder,
        string DefaultCurrencyCode,
        string DefaultCulture,
        bool MaintenanceModeEnabled,
        string? PrimaryDomain,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public sealed record CommerceStoreDetail(
        Guid PublicId,
        Guid? ControlPlaneStorePublicId,
        string StoreKey,
        string Name,
        string Status,
        string? BaseUrl,
        bool ForceHttps,
        bool SslEnabled,
        int? SslPort,
        int DisplayOrder,
        string? HtmlBodyId,
        string? CdnHost,
        string? LogoUrl,
        string? FaviconUrl,
        string? PngIconUrl,
        string? AppleTouchIconUrl,
        string? MsTileImageUrl,
        string? MsTileColor,
        string DefaultCurrencyCode,
        string DefaultCulture,
        string? SupportEmail,
        string? SupportPhone,
        bool MaintenanceModeEnabled,
        string? MaintenanceMessage,
        string? MetadataJson,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? ArchivedAt,
        IReadOnlyList<CommerceStoreDomainDto> Domains);

    public sealed record CommerceStoreDomainDto(
        Guid Id,
        string Domain,
        string NormalizedDomain,
        bool IsPrimary,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? VerifiedAt,
        DateTimeOffset? DisabledAt);

    public sealed record CommerceCurrentStore(
        Guid PublicId,
        string StoreKey,
        string Name,
        string Status,
        string? BaseUrl,
        string? PrimaryDomain,
        bool ForceHttps,
        string? CdnHost,
        string? LogoUrl,
        string? FaviconUrl,
        string? PngIconUrl,
        string? AppleTouchIconUrl,
        string? MsTileImageUrl,
        string? MsTileColor,
        string DefaultCurrencyCode,
        string DefaultCulture,
        string? SupportEmail,
        string? SupportPhone,
        bool MaintenanceModeEnabled,
        string? MaintenanceMessage,
        string? HtmlBodyId);

    public sealed record CommerceStoreOperationResult<TPayload>(
        bool Success,
        string Message,
        TPayload? Payload = default,
        CommerceStoreOperationFailure? Failure = null);

    public enum CommerceStoreOperationFailure
    {
        Validation,
        NotFound,
        Conflict
    }
}
