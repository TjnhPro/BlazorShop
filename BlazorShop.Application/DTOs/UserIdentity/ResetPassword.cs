namespace BlazorShop.Application.DTOs.UserIdentity
{
    public class ResetPassword
    {
        public string Email { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
