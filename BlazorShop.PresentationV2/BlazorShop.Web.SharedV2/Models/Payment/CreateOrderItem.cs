namespace BlazorShop.Web.SharedV2.Models.Payment
{
    using System.ComponentModel.DataAnnotations;

    public class CreateOrderItem : ProcessCart
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
    }
}
