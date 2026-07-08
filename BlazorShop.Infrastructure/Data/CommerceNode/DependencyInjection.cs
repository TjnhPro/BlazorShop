namespace BlazorShop.Infrastructure.Data.CommerceNode
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.Mapping;
    using BlazorShop.Application.Options;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Application.Services.Contracts.Logging;
    using BlazorShop.Application.Services.Contracts.Payment;
    using BlazorShop.Application.Services.Payment;
    using BlazorShop.Application.Validations;
    using BlazorShop.Application.Validations.Seo;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Contracts.CategoryPersistence;
    using BlazorShop.Domain.Contracts.Newsletters;
    using BlazorShop.Domain.Contracts.Payment;
    using BlazorShop.Domain.Contracts.Seo;
    using BlazorShop.Infrastructure.Data.CommerceNode.Repositories;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;
    using BlazorShop.Infrastructure.Services;

    using FluentValidation;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public static class DependencyInjection
    {
        public static IServiceCollection AddCommerceNodeInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var connectionString = configuration.GetConnectionString("CommerceNodeConnection")
                                   ?? "Host=localhost;Port=5434;Database=blazorshop_commerce_node;Username=blazorshop_commerce_node;Password=blazorshop_commerce_node_dev";

            services.AddDbContext<CommerceNodeDbContext>(
                options => options.UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(CommerceNodeDbContext).Assembly.FullName);
                        npgsqlOptions.EnableRetryOnFailure();
                    }));

            services.AddHttpContextAccessor();
            services.AddAutoMapper(cfg => cfg.AddProfile<MappingConfig>());
            services.AddValidatorsFromAssemblyContaining<SeoRedirectDtoValidator>();
            services.AddMemoryCache();
            services.Configure<RecommendationOptions>(configuration.GetSection(RecommendationOptions.SectionName));
            services.AddOptions<ClientAppOptions>()
                .Bind(configuration.GetSection(ClientAppOptions.SectionName));
            services.AddOptions<EmailSettings>()
                .Bind(configuration.GetSection("EmailSettings"));
            services.AddScoped(typeof(IAppLogger<>), typeof(LoggerAdapter<>));
            services.AddTransient<IEmailService, EmailService>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(CommerceNodeGenericRepository<>));
            services.AddScoped<IProductReadRepository, CommerceNodeProductReadRepository>();
            services.AddScoped<IProductRecommendationRepository, CommerceNodeProductRecommendationRepository>();
            services.AddScoped<ICategoryRepository, CommerceNodeCategoryRepository>();
            services.AddScoped<IPaymentMethod, CommerceNodePaymentMethodRepository>();
            services.AddScoped<IOrderRepository, CommerceNodeOrderRepository>();
            services.AddScoped<INewsletterSubscriberRepository, CommerceNodeNewsletterSubscriberRepository>();
            services.AddScoped<ISeoSettingsRepository, CommerceNodeSeoSettingsRepository>();
            services.AddScoped<ISeoRedirectRepository, CommerceNodeSeoRedirectRepository>();
            services.AddScoped<IApplicationTransactionManager, CommerceNodeTransactionManager>();
            services.AddScoped<ICommerceNodeAuditActorAccessor, CommerceNodeAuditActorAccessor>();
            services.AddScoped<IAdminAuditService, CommerceNodeAdminAuditService>();
            services.AddSingleton<ISlugService, SlugService>();
            services.AddScoped<IValidationService, ValidationService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IProductVariantService, ProductVariantService>();
            services.AddScoped<IPublicCatalogService, PublicCatalogService>();
            services.AddScoped<IProductRecommendationService, ProductRecommendationService>();
            services.AddScoped<IPaymentMethodService, PaymentMethodService>();
            services.AddScoped<IPayPalPaymentService, PayPalPaymentService>();
            services.AddScoped<INewsletterService, NewsletterService>();
            services.AddScoped<IAdminInventoryService, CommerceNodeAdminInventoryService>();
            services.AddScoped<IOrderTrackingService, CommerceNodeOrderTrackingService>();
            services.AddScoped<IAdminOrderService, CommerceNodeAdminOrderService>();
            services.AddScoped<IAdminSettingsService, CommerceNodeAdminSettingsService>();
            services.AddScoped<IProductSeoService, ProductSeoService>();
            services.AddScoped<ICategorySeoService, CategorySeoService>();
            services.AddScoped<ISeoSettingsService, SeoSettingsService>();
            services.AddScoped<ISeoRedirectService, SeoRedirectService>();
            services.AddScoped<ISeoRedirectResolutionService, SeoRedirectResolutionService>();
            services.AddScoped<ISeoRedirectAutomationService, SeoRedirectAutomationService>();
            services.AddScoped<IMetricsService, MetricsService>();

            return services;
        }
    }
}
