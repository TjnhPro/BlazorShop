namespace BlazorShop.Web.SharedV2.Models.Category
{
    using System.ComponentModel.DataAnnotations;

    public class CategoryBase
    {
        [Required(ErrorMessage = "The Category Name field is required.")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
