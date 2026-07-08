namespace BlazorShop.ControlPlane.API.Authorization
{
    using BlazorShop.Application.ControlPlane.Security;

    using Microsoft.AspNetCore.Authorization;

    public static class ControlPlaneAuthorizationServiceCollectionExtensions
    {
        public static IServiceCollection AddControlPlaneAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(
                options =>
                {
                    foreach (var policy in ControlPlanePolicyNames.PermissionByPolicy)
                    {
                        options.AddPolicy(
                            policy.Key,
                            builder => builder
                                .RequireAuthenticatedUser()
                                .AddRequirements(new ControlPlanePermissionRequirement(policy.Value)));
                    }
                });

            services.AddScoped<IAuthorizationHandler, ControlPlanePermissionAuthorizationHandler>();

            return services;
        }
    }
}
