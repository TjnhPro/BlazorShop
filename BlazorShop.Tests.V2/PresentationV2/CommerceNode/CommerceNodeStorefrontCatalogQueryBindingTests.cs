extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using System.Net;

    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Discovery;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    using Xunit;

    using CommerceNodeProgram = CommerceNodeApi::Program;

    [Collection("V2 serial host and process tests")]
    public sealed class CommerceNodeStorefrontCatalogQueryBindingTests : IClassFixture<WebApplicationFactory<CommerceNodeProgram>>
    {
        private static readonly Guid StoreId = Guid.NewGuid();
        private readonly WebApplicationFactory<CommerceNodeProgram> factory;

        public CommerceNodeStorefrontCatalogQueryBindingTests(WebApplicationFactory<CommerceNodeProgram> factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task GetProducts_BindsCreatedAfterUtcOffsetQueryAsUtcCatalogFilter()
        {
            var catalogService = new CapturingPublicCatalogService();
            using var client = this.CreateClient(catalogService);

            using var response = await client.GetAsync(
                "/api/storefront/stores/test-store/catalog/products?PageNumber=1&PageSize=24&SortBy=newest&CreatedAfterUtc=2026-07-15T06%3A59%3A30%2B07%3A00");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(catalogService.LastQuery);
            Assert.Equal(new DateTime(2026, 7, 14, 23, 59, 30, DateTimeKind.Utc), catalogService.LastQuery!.CreatedAfterUtc);
            Assert.Equal(ProductCatalogSortBy.Newest, catalogService.LastQuery.SortBy);
        }

        [Fact]
        public async Task GetProducts_WithoutCreatedAfterUtcKeepsNewestSortAndNoDateFilter()
        {
            var catalogService = new CapturingPublicCatalogService();
            using var client = this.CreateClient(catalogService);

            using var response = await client.GetAsync(
                "/api/storefront/stores/test-store/catalog/products?PageNumber=1&PageSize=24&SortBy=newest");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(catalogService.LastQuery);
            Assert.Null(catalogService.LastQuery!.CreatedAfterUtc);
            Assert.Equal(ProductCatalogSortBy.Newest, catalogService.LastQuery.SortBy);
        }

        private HttpClient CreateClient(CapturingPublicCatalogService catalogService)
        {
            var configuredFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("CommerceNode:Database:MigrateOnStartup", "false");
                builder.UseSetting("CommerceTaskWorker:Enabled", "false");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ICommerceStoreDomainResolver>();
                    services.RemoveAll<ICommerceStoreContext>();
                    services.RemoveAll<IPublicCatalogService>();
                    services.RemoveAll<IStorefrontWorkingCurrencyResolver>();
                    services.RemoveAll<IMoneyConversionService>();
                    services.AddSingleton<ICommerceStoreDomainResolver, StubCommerceStoreDomainResolver>();
                    services.AddSingleton<ICommerceStoreContext, StubCommerceStoreContext>();
                    services.AddSingleton<IPublicCatalogService>(catalogService);
                    services.AddSingleton<IStorefrontWorkingCurrencyResolver, StubStorefrontWorkingCurrencyResolver>();
                    services.AddSingleton<IMoneyConversionService, StubMoneyConversionService>();
                });
            });

            return configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }

        private static CommerceCurrentStore CreateCurrentStore()
        {
            return new CommerceCurrentStore(
                StoreId,
                "test-store",
                "Test Store",
                CommerceStoreStatuses.Active,
                "https://test-store.example",
                "test-store.example",
                true,
                null,
                null,
                "Test Store",
                "support@test-store.example",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "USD",
                "en-US",
                "support@test-store.example",
                null,
                false,
                null,
                null);
        }

        private sealed class StubCommerceStoreDomainResolver : ICommerceStoreDomainResolver
        {
            public Task<ApplicationResult<CommerceCurrentStore>> ResolveAsync(
                string? storeKey = null,
                string? host = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(ApplicationResult<CommerceCurrentStore>.Succeeded(CreateCurrentStore()));
            }

            public Task<ApplicationResult<CommerceCurrentStore>> ResolveForReadinessAsync(
                string? storeKey = null,
                string? host = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(ApplicationResult<CommerceCurrentStore>.Succeeded(CreateCurrentStore()));
            }

            public Task<ApplicationResult<StoreExecutionContext>> ResolveExecutionContextAsync(
                string? storeKey = null,
                string? host = null,
                string source = StoreExecutionContextSources.Unknown,
                CancellationToken cancellationToken = default)
            {
                var currentStore = CreateCurrentStore();
                return Task.FromResult(ApplicationResult<StoreExecutionContext>.Succeeded(
                    new StoreExecutionContext(
                        StoreId,
                        currentStore.StoreKey,
                        host,
                        source,
                        currentStore.Status,
                        true,
                        currentStore)));
            }

            public Task<ApplicationResult<Guid>> ResolveStoreIdAsync(
                string? storeKey = null,
                string? host = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(ApplicationResult<Guid>.Succeeded(StoreId));
            }
        }

        private sealed class StubCommerceStoreContext : ICommerceStoreContext
        {
            public Task<ApplicationResult<CommerceCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(ApplicationResult<CommerceCurrentStore>.Succeeded(CreateCurrentStore()));
            }

            public Task<ApplicationResult<Guid>> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(ApplicationResult<Guid>.Succeeded(StoreId));
            }
        }

        private sealed class CapturingPublicCatalogService : IPublicCatalogService
        {
            public ProductCatalogQuery? LastQuery { get; private set; }

            public Task<PagedResult<GetCatalogProduct>> GetPublishedCatalogPageAsync(ProductCatalogQuery query)
            {
                this.LastQuery = query;
                return Task.FromResult(new PagedResult<GetCatalogProduct>
                {
                    Items = [],
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,
                    TotalCount = 0,
                });
            }

            public Task<IEnumerable<GetCategory>> GetPublishedCategoriesAsync() => throw new NotSupportedException();

            public Task<IReadOnlyList<GetCategoryTreeNode>> GetPublishedCategoryTreeAsync() => throw new NotSupportedException();

            public Task<GetPublicCatalogSitemap> GetPublishedSitemapAsync() => throw new NotSupportedException();

            public Task<ProductFilterMetadataReadModel> GetPublishedProductFilterMetadataAsync(ProductCatalogQuery query) => throw new NotSupportedException();

            public Task<IReadOnlyList<GetCatalogProduct>> GetPublishedSearchSuggestionsAsync(ProductCatalogQuery query, int limit) => throw new NotSupportedException();

            public Task<GetProduct?> GetPublishedProductByIdAsync(Guid id) => throw new NotSupportedException();

            public Task<GetProduct?> GetPublishedProductBySlugAsync(string slug) => throw new NotSupportedException();

            public Task<GetCategory?> GetPublishedCategoryByIdAsync(Guid id) => throw new NotSupportedException();

            public Task<IReadOnlyList<GetCatalogProduct>> GetPublishedProductsByCategoryAsync(Guid categoryId) => throw new NotSupportedException();

            public Task<GetCategoryPage?> GetPublishedCategoryPageBySlugAsync(string slug) => throw new NotSupportedException();
        }

        private sealed class StubStorefrontWorkingCurrencyResolver : IStorefrontWorkingCurrencyResolver
        {
            public Task<StorefrontWorkingCurrencyResolution> ResolveAsync(
                Guid storeId,
                string? requestedCurrencyCode,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new StorefrontWorkingCurrencyResolution(
                    "USD",
                    "USD",
                    requestedCurrencyCode,
                    true,
                    true,
                    "default"));
            }
        }

        private sealed class StubMoneyConversionService : IMoneyConversionService
        {
            public Task<ServiceResponse<MoneyConversionResult>> ConvertFromBaseAsync(
                Guid storeId,
                decimal amount,
                string targetCurrencyCode,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException("The catalog query binding tests return no products.");
            }
        }
    }
}
