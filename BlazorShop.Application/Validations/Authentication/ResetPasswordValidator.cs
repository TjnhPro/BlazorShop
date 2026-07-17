namespace BlazorShop.Application.Validations.Authentication
{
    using BlazorShop.Application.DTOs.UserIdentity;

    using FluentValidation;

    public class ResetPasswordValidator : AbstractValidator<ResetPassword>
    {
        public ResetPasswordValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email is not valid.");
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Reset token is required.");
            RuleFor(x => x.Password).ApplyStrongPasswordRules();
            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password)
                .WithMessage("Passwords do not match.");
        }
    }
}
