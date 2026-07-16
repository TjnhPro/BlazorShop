namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class VariationTemplateValue
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public Guid OptionId { get; set; }

        public string Value { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public string? ColorHex { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public VariationTemplateOption? Option { get; set; }
    }
}
