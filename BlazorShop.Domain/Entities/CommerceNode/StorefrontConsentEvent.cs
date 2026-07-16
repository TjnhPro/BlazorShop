namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class StorefrontConsentEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public string ConsentKey { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;

        public string ConsentVersion { get; set; } = string.Empty;

        public string CategoriesJson { get; set; } = "{}";

        public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public CommerceStore? Store { get; set; }
    }
}
