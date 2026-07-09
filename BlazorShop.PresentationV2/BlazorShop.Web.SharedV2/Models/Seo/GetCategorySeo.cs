namespace BlazorShop.Web.SharedV2.Models.Seo
{
    using System.ComponentModel.DataAnnotations;

    public class GetCategorySeo : SeoFieldsBase
    {
        [Required]
        public Guid CategoryId { get; set; }
    }
}