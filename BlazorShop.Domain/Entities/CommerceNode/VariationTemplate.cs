namespace BlazorShop.Domain.Entities.CommerceNode
{
    using BlazorShop.Domain.Entities;

    public sealed class VariationTemplate
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<VariationTemplateOption> Options { get; set; } = new List<VariationTemplateOption>();

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
