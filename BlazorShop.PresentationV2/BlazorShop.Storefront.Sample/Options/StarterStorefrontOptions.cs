namespace BlazorShop.Storefront.Sample.Options
{
    using System.ComponentModel.DataAnnotations;

    public sealed class StarterStorefrontOptions
    {
        public const string SectionName = "Storefront";

        [Required]
        [Url]
        public string CommerceNodeBaseUrl { get; set; } = "http://localhost:5180";

        [Required]
        [MinLength(1)]
        public string StoreKey { get; set; } = "default";

        [Url]
        public string? PublicBaseUrl { get; set; }
    }
}

