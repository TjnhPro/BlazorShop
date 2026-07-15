namespace BlazorShop.Application.DTOs.Seo
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Domain.Constants;

    public class SeoRedirectDto
    {
        public Guid Id { get; set; }

        public Guid? StoreId { get; set; }

        public string? EntityType { get; set; }

        public Guid? EntityId { get; set; }

        public string? LanguageCode { get; set; }

        [Required]
        [MaxLength(SeoConstraints.UrlMaxLength)]
        public string? OldPath { get; set; }

        [Required]
        [MaxLength(SeoConstraints.UrlMaxLength)]
        public string? NewPath { get; set; }

        public int StatusCode { get; set; } = SeoConstraints.PermanentRedirectStatusCode;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; }
    }
}
