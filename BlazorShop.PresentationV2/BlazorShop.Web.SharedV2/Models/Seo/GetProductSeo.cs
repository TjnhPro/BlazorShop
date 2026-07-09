namespace BlazorShop.Web.SharedV2.Models.Seo
{
    using System.ComponentModel.DataAnnotations;

    public class GetProductSeo : SeoFieldsBase
    {
        [Required]
        public Guid ProductId { get; set; }

        public DateTime? PublishedOn { get; set; }
    }
}