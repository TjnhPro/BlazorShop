namespace BlazorShop.Web.SharedV2.Models.Category
{
    using System.ComponentModel.DataAnnotations;

    public class UpdateCategory : CategoryBase
    {
        [Required]
        public Guid Id { get; set; }
    }
}
