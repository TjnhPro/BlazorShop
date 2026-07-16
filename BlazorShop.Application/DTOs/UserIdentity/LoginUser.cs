namespace BlazorShop.Application.DTOs.UserIdentity
{
    public class LoginUser : BaseModel
    {
        public string? CaptchaToken { get; set; }
    }
}
