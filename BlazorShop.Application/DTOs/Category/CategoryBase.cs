namespace BlazorShop.Application.DTOs.Category
{
    using System.ComponentModel.DataAnnotations;

    public class CategoryBase
    {
        [Required]
        public string? Name { get; set; }

        public Guid? ParentCategoryId { get; set; }

        public string? Image { get; set; }

        public int DisplayOrder { get; set; }
    }
}
