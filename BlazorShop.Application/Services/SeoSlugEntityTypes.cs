namespace BlazorShop.Application.Services
{
    public static class SeoSlugEntityTypes
    {
        public const string Product = "product";
        public const string Category = "category";
        public const string Page = "page";

        public static bool IsKnown(string? entityType)
        {
            return Normalize(entityType) is Product or Category or Page;
        }

        public static string Normalize(string? entityType)
        {
            return string.IsNullOrWhiteSpace(entityType)
                ? string.Empty
                : entityType.Trim().ToLowerInvariant();
        }
    }
}
