namespace BlazorShop.Infrastructure.Data.CommerceNode
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Customers;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.Settings;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.Mapping;
    using BlazorShop.Application.Options;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Authentication;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Services.Contracts.Authentication;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Application.Services.Contracts.Logging;
    using BlazorShop.Application.Services.Contracts.Payment;
    using BlazorShop.Application.Services.Payment;
    using BlazorShop.Application.Validations;
    using BlazorShop.Application.Validations.Authentication;
    using BlazorShop.Application.Validations.Seo;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Contracts.Authentication;
    using BlazorShop.Domain.Contracts.CategoryPersistence;
    using BlazorShop.Domain.Contracts.Newsletters;
    using BlazorShop.Domain.Contracts.Payment;
    using BlazorShop.Domain.Contracts.Seo;
    using BlazorShop.Domain.Entities.Identity;
    using BlazorShop.Infrastructure.Data.CommerceNode.Repositories;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;
    using BlazorShop.Infrastructure.Repositories.Authentication;
    using BlazorShop.Infrastructure.Services;

    using FluentValidation;

    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Tokens;

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
            services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();
            services.AddMemoryCache();
            services.Configure<RecommendationOptions>(configuration.GetSection(RecommendationOptions.SectionName));
            services.Configure<IdentityConfirmationOptions>(configuration.GetSection(IdentityConfirmationOptions.SectionName));
            services.Configure<BankTransferSettings>(configuration.GetSection("BankTransfer"));
            services.AddOptions<ClientAppOptions>()
                .Bind(configuration.GetSection(ClientAppOptions.SectionName));
            services.AddOptions<EmailSettings>()
                .Bind(configuration.GetSection("EmailSettings"));
            services.AddScoped<CommerceNodeDevelopmentSeeder>();
            services.AddScoped(typeof(IAppLogger<>), typeof(LoggerAdapter<>));
            services.AddTransient<IEmailService, EmailService>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(CommerceNodeGenericRepository<>));
            services.AddScoped<IProductReadRepository, CommerceNodeProductReadRepository>();
            services.AddScoped<IAppUserManager, CommerceNodeAppUserManager>();
            services.AddScoped<IAppTokenManager, CommerceNodeAppTokenManager>();
            services.AddScoped<IAppRoleManager, AppRoleManager>();
            services.AddScoped<IProductRecommendationRepository, CommerceNodeProductRecommendationRepository>();
            services.AddScoped<ICategoryRepository, CommerceNodeCategoryRepository>();
            services.AddScoped<IPaymentMethod, CommerceNodePaymentMethodRepository>();
            services.AddScoped<ICart, CommerceNodeCartRepository>();
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
            services.AddScoped<CommerceNodePaymentMethodService>();
            services.AddScoped<IPaymentMethodService>(provider => provider.GetRequiredService<CommerceNodePaymentMethodService>());
            services.AddScoped<IStorePaymentMethodAdminService>(provider => provider.GetRequiredService<CommerceNodePaymentMethodService>());
            services.AddScoped<IPaymentHandler, CodPaymentHandler>();
            services.AddScoped<IPaymentHandler, StripePaymentHandler>();
            services.AddScoped<IPaymentHandler, PayPalPaymentHandler>();
            services.AddScoped<IPaymentHandlerResolver, PaymentHandlerResolver>();
            services.AddScoped<IPaymentAttemptService, PaymentAttemptService>();
            services.AddScoped<IStorefrontPaymentProvider, StripeStorefrontPaymentProvider>();
            services.AddScoped<IStorefrontPaymentProviderResolver, StorefrontPaymentProviderResolver>();
            services.AddScoped<IStripeCheckoutSessionService, StripeCheckoutSessionService>();
            services.AddScoped<IPaymentService, StripePaymentService>();
            services.AddScoped<IPayPalPaymentService, PayPalPaymentService>();
            services.AddScoped<INewsletterService, NewsletterService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<IOrderQueryService, OrderQueryService>();
            services.AddScoped<IAdminInventoryService, CommerceNodeAdminInventoryService>();
            services.AddScoped<IOrderTrackingService, CommerceNodeOrderTrackingService>();
            services.AddScoped<IAdminOrderService, CommerceNodeAdminOrderService>();
            services.AddScoped<IAdminShipmentService, CommerceNodeAdminShipmentService>();
            services.AddScoped<IAdminSettingsService, CommerceNodeAdminSettingsService>();
            services.AddScoped<ICommerceTaskService, CommerceTaskService>();
            services.AddScoped<IProductMediaService, ProductMediaService>();
            services.AddScoped<ICommerceMediaAssetService, CommerceMediaAssetService>();
            services.AddScoped<IProductMediaUrlBuilder, ProductMediaUrlBuilder>();
            services.AddScoped<IProductImportCsvParser, ProductImportCsvParser>();
            services.AddScoped<IProductImportService, ProductImportService>();
            services.AddScoped<IVariationTemplateService, VariationTemplateService>();
            services.AddScoped<IVariationTemplateLookupService, VariationTemplateLookupService>();
            services.AddScoped<IStorefrontPageService, StorefrontPageService>();
            services.AddSingleton<ICatalogQueryCache, MemoryCatalogQueryCache>();
            services.AddScoped<ICommerceStoreService, CommerceStoreService>();
            services.AddScoped<ICommerceStoreContext, CommerceStoreContext>();
            services.AddScoped<ICommerceStoreDomainResolver, CommerceStoreDomainResolver>();
            services.AddScoped<IStoreCurrencyResolver, StoreCurrencyResolver>();
            services.AddScoped<IStoreCurrencyService, StoreCurrencyService>();
            services.AddSingleton<ICurrencyMetadataService, CurrencyMetadataService>();
            services.AddSingleton<IMoneyRoundingService, MoneyRoundingService>();
            services.AddSingleton<IPaymentMinorUnitConverter, PaymentMinorUnitConverter>();
            services.AddScoped<IStorefrontCustomerService, StorefrontCustomerService>();
            services.AddScoped<IStorefrontCartSessionService, StorefrontCartSessionService>();
            services.AddScoped<IStorefrontCartService, StorefrontCartService>();
            services.AddScoped<IStorefrontCheckoutService, StorefrontCheckoutService>();
            services.AddScoped<IProductSeoService, ProductSeoService>();
            services.AddScoped<ICategorySeoService, CategorySeoService>();
            services.AddScoped<ISeoSettingsService, SeoSettingsService>();
            services.AddScoped<IStoreSeoSettingsService, StoreSeoSettingsService>();
            services.AddScoped<IStoreFeatureStateService, StoreFeatureStateService>();
            services.AddScoped<IStorefrontPublicConfigurationCache, StorefrontPublicConfigurationCache>();
            services.AddScoped<ISeoRedirectService, SeoRedirectService>();
            services.AddScoped<ISeoRedirectResolutionService, SeoRedirectResolutionService>();
            services.AddScoped<ISeoRedirectAutomationService, SeoRedirectAutomationService>();
            services.AddScoped<IMetricsService, MetricsService>();
            Stripe.StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
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
                .AddEntityFrameworkStores<CommerceNodeDbContext>();

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
                            IssuerSigningKey = new SymmetricSecurityKey(
                                System.Text.Encoding.UTF8.GetBytes(configuration["JWT:Key"]!)),
                        };
                    });

            return services;
        }
    }
}
