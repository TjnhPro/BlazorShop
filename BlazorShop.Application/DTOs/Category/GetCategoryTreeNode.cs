namespace BlazorShop.Application.DTOs.Category
{
    public sealed class GetCategoryTreeNode
    {
        public Guid Id { get; set; }

        public Guid? ParentCategoryId { get; set; }

        public string? Name { get; set; }

        public string? Slug { get; set; }

        public string? Image { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsPublished { get; set; }

        public IReadOnlyList<GetCategoryTreeNode> Children { get; set; } = Array.Empty<GetCategoryTreeNode>();
    }
}
