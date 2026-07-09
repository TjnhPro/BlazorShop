namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class CommerceStoreDomain
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public CommerceStore? Store { get; set; }

        public string Domain { get; set; } = string.Empty;

        public string NormalizedDomain { get; set; } = string.Empty;

        public bool IsPrimary { get; set; }

        public string Status { get; set; } = CommerceStoreDomainStatuses.Pending;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? VerifiedAt { get; set; }

        public DateTimeOffset? DisabledAt { get; set; }
    }
}
