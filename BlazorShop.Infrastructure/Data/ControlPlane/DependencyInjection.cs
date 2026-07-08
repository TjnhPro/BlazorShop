namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using BlazorShop.Application.ControlPlane.Audit;
    using BlazorShop.Application.ControlPlane.Actions;
    using BlazorShop.Application.ControlPlane.Credentials;
    using BlazorShop.Application.ControlPlane.Dashboard;
    using BlazorShop.Application.ControlPlane.Health;
    using BlazorShop.Application.ControlPlane.Nodes;
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.Application.ControlPlane.Stores;

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
            services.AddScoped<IControlPlaneActionService, ControlPlaneActionService>();
            services.AddScoped<IControlPlaneProfileService, ControlPlaneProfileService>();
            services.AddScoped<IControlPlaneNodeService, ControlPlaneNodeService>();
            services.AddScoped<IControlPlaneCredentialService, ControlPlaneCredentialService>();
            services.AddScoped<IControlPlaneDashboardService, ControlPlaneDashboardService>();
            services.AddScoped<IControlPlaneHealthService, ControlPlaneHealthService>();
            services.AddScoped<IControlPlaneStoreService, ControlPlaneStoreService>();
            services.AddHostedService<ControlPlaneProbeBackgroundService>();
            services.AddHttpClient<ICommerceNodeControlClient, CommerceNodeControlClient>(
                client =>
                {
                    var timeoutSeconds = Math.Clamp(configuration.GetValue("ControlPlane:Probes:TimeoutSeconds", 10), 1, 60);
                    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                });

            return services;
        }
    }
}
