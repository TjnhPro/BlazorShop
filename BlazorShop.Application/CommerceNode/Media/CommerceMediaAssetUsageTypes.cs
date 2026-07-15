namespace BlazorShop.Application.CommerceNode.Media
{
    public static class CommerceMediaAssetUsageTypes
    {
        public const string Content = "content";

        public const string Branding = "branding";

        public const string Theme = "theme";

        public const string Category = "category";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(
            [Content, Branding, Theme, Category],
            StringComparer.OrdinalIgnoreCase);

        public static string NormalizeOrDefault(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? Content
                : value.Trim().ToLowerInvariant();
        }

        public static bool IsValid(string? value)
        {
            return All.Contains(NormalizeOrDefault(value));
        }
    }
}
