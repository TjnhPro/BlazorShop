namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class StoreSeoSettings
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public string? SiteName { get; set; }

        public string? DefaultTitleSuffix { get; set; }

        public string? DefaultMetaDescription { get; set; }

        public string? DefaultOgImage { get; set; }

        public string? BaseCanonicalUrl { get; set; }

        public string? CompanyName { get; set; }

        public string? CompanyLogoUrl { get; set; }

        public string? CompanyPhone { get; set; }

        public string? CompanyEmail { get; set; }

        public string? CompanyAddress { get; set; }

        public string? FacebookUrl { get; set; }

        public string? InstagramUrl { get; set; }

        public string? XUrl { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public CommerceStore? Store { get; set; }
    }
}
