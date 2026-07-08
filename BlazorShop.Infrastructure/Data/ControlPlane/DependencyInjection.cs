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
    using BlazorShop.Application.ControlPlane.Users;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.Services.Contracts.Logging;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Contracts.Authentication;
    using BlazorShop.Domain.Entities.Identity;
    using BlazorShop.Infrastructure.Data.ControlPlane.Authentication;
    using BlazorShop.Infrastructure.Repositories.Authentication;
    using BlazorShop.Infrastructure.Services;

    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Tokens;

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

            services.AddControlPlaneAuthenticationInfrastructure(configuration);

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
            services.AddScoped<IControlPlaneUserManagementService, ControlPlaneUserManagementService>();
            services.AddHostedService<ControlPlaneProbeBackgroundService>();
            services.AddHttpClient<ICommerceNodeControlClient, CommerceNodeControlClient>(
                client =>
                {
                    var timeoutSeconds = Math.Clamp(configuration.GetValue("ControlPlane:Probes:TimeoutSeconds", 10), 1, 60);
                    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                });

            return services;
        }

        private static IServiceCollection AddControlPlaneAuthenticationInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddScoped(typeof(IAppLogger<>), typeof(LoggerAdapter<>));

            services.AddDefaultIdentity<AppUser>(
                    options =>
                    {
                        options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
                        options.Lockout.AllowedForNewUsers = true;
                        options.Lockout.MaxFailedAccessAttempts = 5;
                        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                        options.Password.RequireDigit = true;
                        options.Password.RequireNonAlphanumeric = true;
                        options.Password.RequiredLength = 8;
                        options.Password.RequireLowercase = true;
                        options.Password.RequireUppercase = true;
                        options.Password.RequiredUniqueChars = 1;
                    })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ControlPlaneDbContext>();

            services.AddAuthentication(
                    options =>
                    {
                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                .AddJwtBearer(
                    options =>
                    {
                        options.SaveToken = true;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            RequireExpirationTime = true,
                            ValidateIssuerSigningKey = true,
                            ValidAudience = configuration["JWT:Audience"],
                            ValidIssuer = configuration["JWT:Issuer"],
                            ClockSkew = TimeSpan.Zero,
                            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(configuration["JWT:Key"]!)),
                        };
                    });

            services.AddScoped<IAppUserManager, ControlPlaneAppUserManager>();
            services.AddScoped<IAppTokenManager, ControlPlaneTokenManager>();
            services.AddScoped<IAppRoleManager, AppRoleManager>();
            services.AddHttpContextAccessor();
            services.AddOptions<EmailSettings>()
                .Bind(configuration.GetSection("EmailSettings"));
            services.AddTransient<IEmailService, EmailService>();

            return services;
        }
    }
}
