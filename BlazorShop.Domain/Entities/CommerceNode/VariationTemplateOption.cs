namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class VariationTemplateOption
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public Guid TemplateId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public VariationTemplate? Template { get; set; }

        public ICollection<VariationTemplateValue> Values { get; set; } = new List<VariationTemplateValue>();
    }
}
