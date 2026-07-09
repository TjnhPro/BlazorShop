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

        public string DefaultCurrencyCode { get; set; } = "USD";

        public string DefaultCulture { get; set; } = "en-US";

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ArchivedAt { get; set; }

        public ICollection<CommerceStoreDomain> Domains { get; set; } = new List<CommerceStoreDomain>();
    }
}
