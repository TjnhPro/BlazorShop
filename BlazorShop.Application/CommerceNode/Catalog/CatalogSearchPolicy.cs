namespace BlazorShop.Application.CommerceNode.Catalog
{
    using BlazorShop.Domain.Contracts;

    public static class CatalogSearchPolicy
    {
        public const int MinimumSearchTermLength = 2;
        public const int SuggestionDefaultLimit = 6;
        public const int SuggestionMaxLimit = 10;

        public static IReadOnlyList<int> StorefrontPageSizes { get; } = [12, 24, 48];

        public static string? NormalizeSearchTerm(string? value)
        {
            return ProductCatalogQuery.NormalizeSearchTerm(value);
        }

        public static bool IsSearchTermTooShort(string? normalizedSearchTerm)
        {
            return !string.IsNullOrEmpty(normalizedSearchTerm)
                && normalizedSearchTerm.Length < MinimumSearchTermLength;
        }
    }
}
