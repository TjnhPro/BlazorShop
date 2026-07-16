namespace BlazorShop.Domain.Contracts
{
    public sealed class ProductCatalogQuery
    {
        private const int DefaultPageNumber = 1;
        private const int DefaultPageSize = 24;
        private const int MaxPageSize = 100;

        public int PageNumber { get; init; } = DefaultPageNumber;

        public int PageSize { get; init; } = DefaultPageSize;

        public Guid? CategoryId { get; init; }

        public string? CategorySlug { get; init; }

        public bool IncludeSubcategories { get; init; }

        public string? SearchTerm { get; init; }

        public decimal? MinPrice { get; init; }

        public decimal? MaxPrice { get; init; }

        public bool? InStock { get; init; }

        public bool? IsPublished { get; init; }

        public ProductCatalogSortBy SortBy { get; init; } = ProductCatalogSortBy.Newest;

        public DateTime? CreatedAfterUtc { get; init; }

        public int GetNormalizedPageNumber() => this.PageNumber < 1 ? DefaultPageNumber : this.PageNumber;

        public int GetNormalizedPageSize() => Math.Clamp(this.PageSize, 1, MaxPageSize);

        public string? GetNormalizedSearchTerm() => NormalizeSearchTerm(this.SearchTerm);

        public string? GetNormalizedCategorySlug() => string.IsNullOrWhiteSpace(this.CategorySlug)
            ? null
            : this.CategorySlug.Trim();

        public static string? NormalizeSearchTerm(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var builder = new System.Text.StringBuilder(value.Length);
            var previousWasWhiteSpace = true;
            foreach (var character in value)
            {
                if (char.IsWhiteSpace(character))
                {
                    if (!previousWasWhiteSpace)
                    {
                        builder.Append(' ');
                        previousWasWhiteSpace = true;
                    }

                    continue;
                }

                builder.Append(character);
                previousWasWhiteSpace = false;
            }

            if (previousWasWhiteSpace && builder.Length > 0)
            {
                builder.Length--;
            }

            return builder.Length == 0 ? null : builder.ToString();
        }
    }
}
