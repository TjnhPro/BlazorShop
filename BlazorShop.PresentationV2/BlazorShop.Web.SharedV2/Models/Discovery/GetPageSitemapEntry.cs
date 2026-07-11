namespace BlazorShop.Web.SharedV2.Models.Discovery
{
    public sealed class GetPageSitemapEntry
    {
        public string Slug { get; set; } = string.Empty;

        public DateTime? LastModifiedUtc { get; set; }
    }
}
