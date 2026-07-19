namespace BlazorShop.Infrastructure.Data.CommerceNode
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Identity;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    public sealed partial class CommerceNodeDevelopmentSeeder
    {
        private const string DefaultStoreKey = "default";
        private const string IsolationStoreKey = "qa-s2";
        private const string MaintenanceStoreKey = "qa-maintenance";
        private const string DisabledStoreKey = "qa-disabled";
        private const string QaCustomerEmail = "qa.customer@example.local";
        private const string QaCustomerPassword = "QaCustomer123!";
        private const string QaOtherCustomerEmail = "qa.other@example.local";
        private const string QaOtherCustomerPassword = "QaOther123!";
        private const string QaSecondStoreCustomerEmail = "qa.s2.customer@example.local";
        private const string QaSecondStoreCustomerPassword = "QaS2Customer123!";
        private const string StorefrontQaUserRole = "User";
        private const string ProductMediaRootPath = "runtime/media";

        private static readonly Guid ApparelCategoryId = Guid.Parse("8d4830f9-a21f-4f4a-96d7-83d1e6dc0201");
        private static readonly Guid TshirtsCategoryId = Guid.Parse("e0e5e4f8-3f12-4c17-b041-7a8fc62e6b14");
        private static readonly Guid LegalPageId = Guid.Parse("1a111111-1111-4111-8111-111111111111");
        private static readonly Guid CookiePageId = Guid.Parse("1a111111-1111-4111-8111-111111111112");
        private static readonly Guid DraftPageId = Guid.Parse("1a111111-1111-4111-8111-111111111113");
        private static readonly Guid EscapingPageId = Guid.Parse("1a111111-1111-4111-8111-111111111114");
        private static readonly Guid LegalPagePublicId = Guid.Parse("1a111111-1111-4111-8111-111111111211");
        private static readonly Guid CookiePagePublicId = Guid.Parse("1a111111-1111-4111-8111-111111111212");
        private static readonly Guid DraftPagePublicId = Guid.Parse("1a111111-1111-4111-8111-111111111213");
        private static readonly Guid EscapingPagePublicId = Guid.Parse("1a111111-1111-4111-8111-111111111214");
        private static readonly Guid TshirtProductId = Guid.Parse("68ba3d10-4d13-46c4-8c8d-4a53b37cf201");
        private static readonly Guid LowStockProductId = Guid.Parse("e9f21b8f-7b2d-4a08-8971-c0dfe037fc1a");
        private static readonly Guid SimpleProductId = Guid.Parse("2b111111-1111-4111-8111-111111111101");
        private static readonly Guid OutOfStockProductId = Guid.Parse("2b111111-1111-4111-8111-111111111102");
        private static readonly Guid UnmanagedStockProductId = Guid.Parse("2b111111-1111-4111-8111-111111111103");
        private static readonly Guid QuantityRuleProductId = Guid.Parse("2b111111-1111-4111-8111-111111111104");
        private static readonly Guid UnpublishedProductId = Guid.Parse("2b111111-1111-4111-8111-111111111108");
        private static readonly Guid FutureProductId = Guid.Parse("2b111111-1111-4111-8111-111111111109");
        private static readonly Guid ExpiredProductId = Guid.Parse("2b111111-1111-4111-8111-111111111110");
        private static readonly Guid SurchargeProductId = Guid.Parse("2b111111-1111-4111-8111-111111111111");
        private static readonly Guid DigitalProductId = Guid.Parse("2b111111-1111-4111-8111-111111111112");
        private static readonly Guid SeoMediaProductId = Guid.Parse("2b111111-1111-4111-8111-111111111113");
        private static readonly Guid HtmlNameProductId = Guid.Parse("2b111111-1111-4111-8111-111111111115");
        private static readonly Guid MissingImageProductId = Guid.Parse("2b111111-1111-4111-8111-111111111116");
        private static readonly Guid PurchasingDisabledProductId = Guid.Parse("2b111111-1111-4111-8111-111111111117");
        private static readonly Guid QaCustomerId = Guid.Parse("3c111111-1111-4111-8111-111111111101");
        private static readonly Guid QaOtherCustomerId = Guid.Parse("3c111111-1111-4111-8111-111111111102");
        private static readonly Guid QaCustomerShippingAddressId = Guid.Parse("3c111111-1111-4111-8111-111111111201");
        private static readonly Guid QaCustomerBillingAddressId = Guid.Parse("3c111111-1111-4111-8111-111111111202");
        private static readonly Guid QaOtherCustomerShippingAddressId = Guid.Parse("3c111111-1111-4111-8111-111111111203");
        private static readonly Guid QaS2StoreId = Guid.Parse("4d111111-1111-4111-8111-111111111102");
        private static readonly Guid QaMaintenanceStoreId = Guid.Parse("4d111111-1111-4111-8111-111111111103");
        private static readonly Guid QaDisabledStoreId = Guid.Parse("4d111111-1111-4111-8111-111111111104");
        private static readonly Guid QaS2CategoryId = Guid.Parse("5e111111-1111-4111-8111-111111111102");
        private static readonly Guid QaS2ProductId = Guid.Parse("5e111111-1111-4111-8111-111111111202");
        private static readonly Guid QaS2CustomerId = Guid.Parse("5e111111-1111-4111-8111-111111111302");
        private static readonly Guid QaS2CustomerAddressId = Guid.Parse("5e111111-1111-4111-8111-111111111402");
        private static readonly Guid SeoMediaProductMediaId = Guid.Parse("6f111111-1111-4111-8111-111111111113");
        private static readonly Guid SeoMediaProductMediaPublicId = Guid.Parse("6f111111-1111-4111-8111-111111111213");
        private static readonly Guid QaS2ProductMediaId = Guid.Parse("6f111111-1111-4111-8111-111111111202");
        private static readonly Guid QaS2ProductMediaPublicId = Guid.Parse("6f111111-1111-4111-8111-111111111302");
        private static readonly Guid DefaultContentMediaAssetId = Guid.Parse("7a111111-1111-4111-8111-111111111101");
        private static readonly Guid DefaultContentMediaAssetPublicId = Guid.Parse("7a111111-1111-4111-8111-111111111201");
        private static readonly Guid QaS2ContentMediaAssetId = Guid.Parse("7a111111-1111-4111-8111-111111111102");
        private static readonly Guid QaS2ContentMediaAssetPublicId = Guid.Parse("7a111111-1111-4111-8111-111111111202");
        private static readonly Guid TshirtRedMVariantId = Guid.Parse("c34f5a0f-401d-4f58-b3d9-c9349ed6d101");
        private static readonly Guid TshirtRedXlVariantId = Guid.Parse("910cb350-8d44-43a7-b86d-8e38ea0cd102");
        private static readonly Guid TshirtBlackMVariantId = Guid.Parse("6894d9f0-071b-4f77-83a7-3d81d8a3d103");
        private static readonly byte[] QaFixturePngBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");

        private readonly CommerceNodeDbContext dbContext;
        private readonly UserManager<AppUser> userManager;
        private readonly IHostEnvironment hostEnvironment;
        private readonly IMediaStorageProvider mediaStorageProvider;
        private readonly CommerceMediaStorageOptions commerceMediaStorageOptions;

        public CommerceNodeDevelopmentSeeder(
            CommerceNodeDbContext dbContext,
            UserManager<AppUser> userManager,
            IHostEnvironment hostEnvironment,
            IMediaStorageProvider mediaStorageProvider,
            IOptions<CommerceMediaStorageOptions> commerceMediaStorageOptions)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
            this.hostEnvironment = hostEnvironment;
            this.mediaStorageProvider = mediaStorageProvider;
            this.commerceMediaStorageOptions = commerceMediaStorageOptions.Value;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            if (await this.HasRequiredQaSeedDataAsync(cancellationToken))
            {
                return;
            }

            var state = new CommerceNodeDevelopmentSeedState();
            foreach (var step in GetSeedSteps())
            {
                await step.SeedAsync(this, state, cancellationToken);
            }
        }

        private static IReadOnlyList<ICommerceNodeDevelopmentSeedStep> GetSeedSteps()
        {
            return
            [
                new StoreRuntimeSeedStep(),
                new StoreSettingsSeedStep(),
                new CatalogSeedStep(),
                new MediaSeedStep(),
                new ContentNavigationSeedStep(),
                new AccountOrderSeedStep(),
            ];
        }

        private interface ICommerceNodeDevelopmentSeedStep
        {
            Task SeedAsync(
                CommerceNodeDevelopmentSeeder seeder,
                CommerceNodeDevelopmentSeedState state,
                CancellationToken cancellationToken);
        }

        private sealed class CommerceNodeDevelopmentSeedState
        {
            public CommerceStore DefaultStore { get; set; } = null!;

            public CommerceStore IsolationStore { get; set; } = null!;

            public CommerceCustomer DefaultCustomer { get; set; } = null!;

            public CommerceCustomer OtherCustomer { get; set; } = null!;

            public CommerceCustomer IsolationCustomer { get; set; } = null!;
        }

        private sealed class StoreRuntimeSeedStep : ICommerceNodeDevelopmentSeedStep
        {
            public async Task SeedAsync(
                CommerceNodeDevelopmentSeeder seeder,
                CommerceNodeDevelopmentSeedState state,
                CancellationToken cancellationToken)
            {
                state.DefaultStore = await seeder.EnsureStoreAsync(cancellationToken);
                state.IsolationStore = await seeder.EnsureAuxiliaryStoresAsync(cancellationToken);
                await seeder.EnsureStorePaymentMethodsAsync(state.DefaultStore.Id, cancellationToken);
                await seeder.EnsureStorePaymentMethodsAsync(state.IsolationStore.Id, cancellationToken);
            }
        }

        private sealed class StoreSettingsSeedStep : ICommerceNodeDevelopmentSeedStep
        {
            public async Task SeedAsync(
                CommerceNodeDevelopmentSeeder seeder,
                CommerceNodeDevelopmentSeedState state,
                CancellationToken cancellationToken)
            {
                await seeder.EnsureStoreConfigurationAsync(state.DefaultStore.Id, cancellationToken);
                await seeder.EnsureStoreConfigurationAsync(state.IsolationStore.Id, cancellationToken);
                await seeder.EnsureStoreEmailSettingsAsync(state.DefaultStore.Id, "default-sender@example.local", cancellationToken);
                await seeder.EnsureStoreEmailSettingsAsync(state.IsolationStore.Id, "s2-sender@example.local", cancellationToken);
            }
        }

        private sealed class CatalogSeedStep : ICommerceNodeDevelopmentSeedStep
        {
            public async Task SeedAsync(
                CommerceNodeDevelopmentSeeder seeder,
                CommerceNodeDevelopmentSeedState state,
                CancellationToken cancellationToken)
            {
                await seeder.EnsureCategoriesAsync(state.DefaultStore.Id, cancellationToken);
                await seeder.EnsureProductsAsync(state.DefaultStore.Id, cancellationToken);
                await seeder.EnsureIsolationCatalogAsync(state.IsolationStore.Id, cancellationToken);
            }
        }

        private sealed class MediaSeedStep : ICommerceNodeDevelopmentSeedStep
        {
            public async Task SeedAsync(
                CommerceNodeDevelopmentSeeder seeder,
                CommerceNodeDevelopmentSeedState state,
                CancellationToken cancellationToken)
            {
                await seeder.EnsureMediaFixtureFilesAsync(cancellationToken);
                await seeder.EnsureProductMediaFixturesAsync(state.DefaultStore.Id, cancellationToken);
                await seeder.EnsureProductMediaFixturesAsync(state.IsolationStore.Id, cancellationToken);
            }
        }

        private sealed class ContentNavigationSeedStep : ICommerceNodeDevelopmentSeedStep
        {
            public async Task SeedAsync(
                CommerceNodeDevelopmentSeeder seeder,
                CommerceNodeDevelopmentSeedState state,
                CancellationToken cancellationToken)
            {
                await seeder.EnsureStorefrontPagesAsync(state.DefaultStore.Id, cancellationToken);
                await seeder.EnsureNavigationAsync(state.DefaultStore.Id, cancellationToken);
            }
        }

        private sealed class AccountOrderSeedStep : ICommerceNodeDevelopmentSeedStep
        {
            public async Task SeedAsync(
                CommerceNodeDevelopmentSeeder seeder,
                CommerceNodeDevelopmentSeedState state,
                CancellationToken cancellationToken)
            {
                state.DefaultCustomer = await seeder.EnsureCustomerAsync(
                    state.DefaultStore.Id,
                    QaCustomerId,
                    QaCustomerShippingAddressId,
                    QaCustomerBillingAddressId,
                    QaCustomerEmail,
                    QaCustomerPassword,
                    "QA Customer",
                    cancellationToken);
                state.OtherCustomer = await seeder.EnsureCustomerAsync(
                    state.DefaultStore.Id,
                    QaOtherCustomerId,
                    QaOtherCustomerShippingAddressId,
                    null,
                    QaOtherCustomerEmail,
                    QaOtherCustomerPassword,
                    "QA Other Customer",
                    cancellationToken);
                state.IsolationCustomer = await seeder.EnsureCustomerAsync(
                    state.IsolationStore.Id,
                    QaS2CustomerId,
                    QaS2CustomerAddressId,
                    null,
                    QaSecondStoreCustomerEmail,
                    QaSecondStoreCustomerPassword,
                    "QA S2 Customer",
                    cancellationToken);
                await seeder.EnsureSampleOrderAsync(state.DefaultStore, state.DefaultCustomer, cancellationToken);
                await seeder.EnsureSampleOrderAsync(state.DefaultStore, state.OtherCustomer, "QA-OTHER-CUSTOMER-SNAPSHOT", cancellationToken);
                await seeder.EnsureSampleOrderAsync(state.IsolationStore, state.IsolationCustomer, cancellationToken);
            }
        }
    }
}
