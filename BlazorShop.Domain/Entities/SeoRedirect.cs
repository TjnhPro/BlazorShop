namespace BlazorShop.Domain.Entities
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Domain.Constants;

    public class SeoRedirect
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? StoreId { get; set; }

        public string? EntityType { get; set; }

        public Guid? EntityId { get; set; }

        public string? LanguageCode { get; set; }

        public string? OldPath { get; set; }

        public string? NewPath { get; set; }

        public int StatusCode { get; set; } = SeoConstraints.PermanentRedirectStatusCode;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
