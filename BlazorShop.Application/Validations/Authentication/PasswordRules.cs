namespace BlazorShop.Application.Validations.Authentication
{
    using FluentValidation;

    internal static class PasswordRules
    {
        public static IRuleBuilderOptions<T, string> ApplyStrongPasswordRules<T>(
            this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"\d").WithMessage("Password must contain at least one digit.")
                .Matches(@"[^\w]").WithMessage("Password must contain at least one special character.");
        }
    }
}
