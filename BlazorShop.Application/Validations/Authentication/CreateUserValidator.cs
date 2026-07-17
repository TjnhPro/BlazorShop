namespace BlazorShop.Application.Validations.Authentication
{
    using BlazorShop.Application.DTOs.UserIdentity;

    using FluentValidation;

    public class CreateUserValidator : AbstractValidator<CreateUser>
    {
        public CreateUserValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().WithMessage("Full Name is required.");

            RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email is not valid.");

            RuleFor(x => x.Password).ApplyStrongPasswordRules();

            RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("Passwords do not match.");
        }
    }
}
