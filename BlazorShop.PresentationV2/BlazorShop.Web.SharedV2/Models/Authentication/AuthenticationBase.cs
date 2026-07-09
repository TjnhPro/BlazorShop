namespace BlazorShop.Web.SharedV2V2.Models.Authentication
{
    using System.ComponentModel.DataAnnotations;

    public class AuthenticationBase
    {
        [EmailAddress, Required]
        public string? Email { get; set; }
    }
}
