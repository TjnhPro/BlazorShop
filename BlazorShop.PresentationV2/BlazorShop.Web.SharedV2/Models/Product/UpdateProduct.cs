namespace BlazorShop.Web.SharedV2.Models.Product
{
    using System.ComponentModel.DataAnnotations;

    public class UpdateProduct : ProductBase
    {
        [Required]
        public Guid Id { get; set; }
    }
}
