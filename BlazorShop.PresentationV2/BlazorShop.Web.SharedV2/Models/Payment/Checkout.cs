namespace BlazorShop.Web.SharedV2.Models.Payment
{
    using System.ComponentModel.DataAnnotations;

    public class Checkout
    {
        [Required]
        public Guid PaymentMethodId { get; set; }

        [Required]
        public IEnumerable<ProcessCart> Carts { get; set; } = [];
    }
}
