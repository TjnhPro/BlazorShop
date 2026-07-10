namespace BlazorShop.Domain.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Product
    {
        [Key]
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        [MaxLength(64)]
        public string? Sku { get; set; }

        public string? ShortDescription { get; set; }

        public string? FullDescription { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ComparePrice { get; set; }

        public string? Image { get; set; }

        public int Quantity { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ArchivedAt { get; set; }

        public int DisplayOrder { get; set; }

        public string? Slug { get; set; }

        public string? MetaTitle { get; set; }

        public string? MetaDescription { get; set; }

        public string? CanonicalUrl { get; set; }

        public string? OgTitle { get; set; }

        public string? OgDescription { get; set; }

        public string? OgImage { get; set; }

        public bool RobotsIndex { get; set; } = true;

        public bool RobotsFollow { get; set; } = true;

        public string? SeoContent { get; set; }

        public bool IsPublished { get; set; } = true;

        public DateTime? PublishedOn { get; set; } = DateTime.UtcNow;

        public Guid? StoreId { get; set; }

        public Guid CategoryId { get; set; }

        public Category? Category { get; set; }

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}
