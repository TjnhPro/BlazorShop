namespace BlazorShop.Web.SharedV2.Models.Pages
{
    public sealed class GetStorefrontPage
    {
        public string Slug { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Intro { get; set; }

        public string BodyHtml { get; set; } = string.Empty;

        public StorefrontPageSeo Seo { get; set; } = new();

        public DateTimeOffset UpdatedAt { get; set; }

        public string? PageKey { get; set; }
    }
}
