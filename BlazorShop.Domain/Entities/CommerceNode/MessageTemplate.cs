namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class MessageTemplate
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public string SystemName { get; set; } = string.Empty;

        public Guid? StoreId { get; set; }

        public string? LanguageCode { get; set; }

        public string SubjectTemplate { get; set; } = string.Empty;

        public string BodyHtmlTemplate { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public string? Description { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public CommerceStore? Store { get; set; }

        public ICollection<QueuedMessage> QueuedMessages { get; set; } = new List<QueuedMessage>();
    }
}
