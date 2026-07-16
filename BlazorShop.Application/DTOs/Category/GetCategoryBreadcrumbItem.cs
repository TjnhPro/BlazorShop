namespace BlazorShop.Application.DTOs.Category
{
    public sealed class GetCategoryBreadcrumbItem
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? Slug { get; set; }
    }
}
