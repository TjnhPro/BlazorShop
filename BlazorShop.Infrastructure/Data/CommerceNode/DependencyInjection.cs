namespace BlazorShop.Infrastructure.Data.CommerceNode
{
    using BlazorShop.Application.Mapping;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Contracts.CategoryPersistence;
    using BlazorShop.Infrastructure.Data.CommerceNode.Repositories;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

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
                                   ?? "Host=localhost;Port=5433;Database=blazorshop_commerce_node;Username=blazorshop_commerce_node;Password=blazorshop_commerce_node_dev";

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
            services.AddScoped(typeof(IGenericRepository<>), typeof(CommerceNodeGenericRepository<>));
            services.AddScoped<IProductReadRepository, CommerceNodeProductReadRepository>();
            services.AddScoped<ICategoryRepository, CommerceNodeCategoryRepository>();
            services.AddScoped<IApplicationTransactionManager, CommerceNodeTransactionManager>();
            services.AddScoped<ICommerceNodeAuditActorAccessor, CommerceNodeAuditActorAccessor>();
            services.AddScoped<IAdminAuditService, CommerceNodeAdminAuditService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IProductVariantService, ProductVariantService>();
            services.AddScoped<IAdminInventoryService, CommerceNodeAdminInventoryService>();

            return services;
        }
    }
}
