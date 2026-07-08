namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using BlazorShop.Application.ControlPlane.Audit;
    using BlazorShop.Application.ControlPlane.Security;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public static class DependencyInjection
    {
        public static IServiceCollection AddControlPlaneInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var connectionString = configuration.GetConnectionString("ControlPlaneConnection")
                                   ?? "Host=localhost;Port=5433;Database=blazorshop_controlplane;Username=blazorshop_controlplane;Password=blazorshop_controlplane_dev";

            services.AddDbContext<ControlPlaneDbContext>(
                options => options.UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(ControlPlaneDbContext).Assembly.FullName);
                        npgsqlOptions.EnableRetryOnFailure();
                    }));

            services.AddHealthChecks()
                .AddCheck<ControlPlaneDbContextHealthCheck>("controlplane_database", tags: ["ready"]);

            services.AddScoped<ControlPlaneDevelopmentSeeder>();
            services.AddScoped<IControlPlaneAuditService, ControlPlaneAuditService>();
            services.AddScoped<IControlPlaneProfileService, ControlPlaneProfileService>();

            return services;
        }
    }
}
