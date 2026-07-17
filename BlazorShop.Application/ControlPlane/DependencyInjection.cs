namespace BlazorShop.Application.ControlPlane
{
    using BlazorShop.Application.Mapping;
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Application.Options;
    using BlazorShop.Application.Services.Authentication;
    using BlazorShop.Application.Services.Contracts.Authentication;
    using BlazorShop.Application.Validations;
    using BlazorShop.Application.Validations.Authentication;
    using BlazorShop.Application.DTOs;

    using FluentValidation;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public static class DependencyInjection
    {
        public static IServiceCollection AddControlPlaneApplication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddAutoMapper(cfg => cfg.AddProfile<MappingConfig>());
            services.AddScoped<IValidator<CreateUser>, CreateUserValidator>();
            services.AddScoped<IValidator<LoginUser>, LoginUserValidator>();
            services.AddScoped<IValidator<ChangePassword>, ChangePasswordValidator>();
            services.AddScoped<IValidator<ResetPassword>, ResetPasswordValidator>();
            services.AddScoped<IValidationService, ValidationService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();

            services.Configure<ClientAppOptions>(configuration.GetSection(ClientAppOptions.SectionName));
            services.Configure<IdentityConfirmationOptions>(configuration.GetSection(IdentityConfirmationOptions.SectionName));

            return services;
        }
    }
}
