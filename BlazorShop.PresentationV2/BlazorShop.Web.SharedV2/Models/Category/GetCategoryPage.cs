namespace BlazorShop.Web.SharedV2.Models.Category
{
    using BlazorShop.Web.SharedV2.Models.Product;

    public sealed class GetCategoryPage
    {
        public GetCategory Category { get; set; } = new();

        public IReadOnlyList<GetCategoryBreadcrumbItem> Breadcrumbs { get; set; } = Array.Empty<GetCategoryBreadcrumbItem>();

        public IReadOnlyList<GetCatalogProduct> Products { get; set; } = Array.Empty<GetCatalogProduct>();

        public int DirectProductCount { get; set; }

        public int DescendantProductCount { get; set; }
    }
}
