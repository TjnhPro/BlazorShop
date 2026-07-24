namespace BlazorShop.Storefront.Models
{
    public sealed class LoginUser
    {
        public string? Email { get; set; }

        public string? Password { get; set; }

        public string? CaptchaToken { get; set; }
    }

    public sealed class CreateUser
    {
        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? Password { get; set; }

        public string? ConfirmPassword { get; set; }

        public string? CaptchaToken { get; set; }
    }

    public sealed class ResetPassword
    {
        public string? Email { get; set; }

        public string? Token { get; set; }

        public string? Password { get; set; }

        public string? ConfirmPassword { get; set; }
    }

    public sealed class ChangePassword
    {
        public string? CurrentPassword { get; set; }

        public string? NewPassword { get; set; }

        public string? ConfirmPassword { get; set; }
    }
}
