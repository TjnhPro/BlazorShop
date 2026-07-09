namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class CommerceStore
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public Guid? ControlPlaneStorePublicId { get; set; }

        public string StoreKey { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Status { get; set; } = CommerceStoreStatuses.Disabled;

        public string? BaseUrl { get; set; }

        public bool ForceHttps { get; set; } = true;

        public bool SslEnabled { get; set; } = true;

        public int? SslPort { get; set; }

        public int DisplayOrder { get; set; }

        public string? HtmlBodyId { get; set; }

        public string? CdnHost { get; set; }

        public string? LogoUrl { get; set; }

        public string? FaviconUrl { get; set; }

        public string? PngIconUrl { get; set; }

        public string? AppleTouchIconUrl { get; set; }

        public string? MsTileImageUrl { get; set; }

        public string? MsTileColor { get; set; }

        public string DefaultCurrencyCode { get; set; } = "USD";

        public string DefaultCulture { get; set; } = "en-US";

        public string? SupportEmail { get; set; }

        public string? SupportPhone { get; set; }

        public bool MaintenanceModeEnabled { get; set; }

        public string? MaintenanceMessage { get; set; }

        public string? MetadataJson { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ArchivedAt { get; set; }

        public ICollection<CommerceStoreDomain> Domains { get; set; } = new List<CommerceStoreDomain>();
    }
}
