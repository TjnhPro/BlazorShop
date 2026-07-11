namespace BlazorShop.Web.SharedV2.Models.Pages
{
    public sealed class StorefrontPageSeo
    {
        public string? MetaTitle { get; set; }

        public string? MetaDescription { get; set; }

        public string? CanonicalUrl { get; set; }

        public string? OgTitle { get; set; }

        public string? OgDescription { get; set; }

        public string? OgImage { get; set; }

        public bool RobotsIndex { get; set; } = true;

        public bool RobotsFollow { get; set; } = true;
    }
}
