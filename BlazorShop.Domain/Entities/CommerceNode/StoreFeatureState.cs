namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class StoreFeatureState
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public string FeatureKey { get; set; } = string.Empty;

        public bool Enabled { get; set; } = true;

        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public CommerceStore? Store { get; set; }
    }
}
