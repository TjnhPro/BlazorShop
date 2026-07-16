namespace BlazorShop.Application.DTOs.Category
{
    using BlazorShop.Application.DTOs.Product;

    public sealed class GetCategoryPage
    {
        public GetCategory Category { get; set; } = new();

        public IReadOnlyList<GetCategoryBreadcrumbItem> Breadcrumbs { get; set; } = Array.Empty<GetCategoryBreadcrumbItem>();

        public IReadOnlyList<GetCatalogProduct> Products { get; set; } = Array.Empty<GetCatalogProduct>();

        public int DirectProductCount { get; set; }

        public int DescendantProductCount { get; set; }
    }
}
