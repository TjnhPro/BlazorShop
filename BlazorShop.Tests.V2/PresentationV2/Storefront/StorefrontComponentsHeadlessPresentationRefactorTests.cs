namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontComponentsHeadlessPresentationRefactorTests
    {
        private static readonly string[] ExpectedFeatureRazorComponents =
        [
            "Account/AccountAddressBook.razor",
            "Account/AccountApp.razor",
            "Account/AccountChangePasswordForm.razor",
            "Account/AccountNavigation.razor",
            "Account/AccountOrderDetail.razor",
            "Account/AccountOrderList.razor",
            "Account/AccountProfileEditor.razor",
            "Cart/CartView.razor",
            "Catalog/ProductSummaryCard.razor",
            "Catalog/ProductSummaryGrid.razor",
            "Checkout/CheckoutShell.razor",
            "Deals/DealsBlock.razor",
            "Product/ProductGallery.razor",
            "Product/ProductPurchasePanel.razor"
        ];

        private static readonly string[] ExpectedFeatureModelAndEnumFiles =
        [
            "Catalog/ProductSummaryItem.cs",
            "Deals/DealsPlacement.cs",
            "Product/ProductGalleryItem.cs",
            "Product/ProductPurchasePanelModels.cs"
        ];

        private static readonly string[] ExpectedBrowserSupportFiles =
        [
            "IStorefrontAntiforgeryTokenReader.cs",
            "StorefrontAntiforgeryToken.cs",
            "StorefrontAntiforgeryTokenReader.cs",
            "StorefrontBrowserAccountModels.cs",
            "StorefrontBrowserCartModels.cs",
            "StorefrontBrowserCheckoutModels.cs",
            "StorefrontFeatureDataMode.cs",
            "StorefrontLocalApiClient.cs",
            "StorefrontLocalApiResult.cs"
        ];

        [Fact]
        public void FeatureRazorInventory_RecordsAllCurrentComponentsBeforeHeadlessMigration()
        {
            var actual = EnumerateComponentFeatureFiles("*.razor");

            Assert.Equal(ExpectedFeatureRazorComponents, actual);
            Assert.Equal(14, actual.Length);
        }

        [Fact]
        public void FeatureModelInventory_RecordsAllCurrentFeatureModelsBeforeHeadlessMigration()
        {
            var actual = EnumerateComponentFeatureFiles("*.cs");

            Assert.Equal(ExpectedFeatureModelAndEnumFiles, actual);
            Assert.Equal(4, actual.Length);
        }

        [Fact]
        public void BrowserSupportInventory_RecordsCurrentSameOriginSupportPrimitives()
        {
            var browserRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Browser");
            var actual = Directory
                .EnumerateFiles(browserRoot, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(ExpectedBrowserSupportFiles, actual);
        }

        [Fact]
        public void NeutralityGuardDesign_IsDocumentedButNotStrictBeforeMigration()
        {
            var plan = ReadRepositoryFile(
                "docs/refactor-control-Commerce-storefront/Storefront Components Headless Presentation Refactor.todo.md");

            foreach (var token in new[]
            {
                "bg-*",
                "text-neutral-*",
                "text-rose-*",
                "text-amber-*",
                "text-emerald-*",
                "rounded-*",
                "shadow-*",
                "max-w-*",
                "grid-cols-*",
                "sm:*",
                "md:*",
                "lg:*",
                "hover:*"
            })
            {
                Assert.Contains(token, plan, StringComparison.Ordinal);
            }

            Assert.Contains("Allowed: `sr-only`, `hidden`, ARIA attributes, `data-storefront-*`, semantic `bs-*`", plan, StringComparison.Ordinal);
            Assert.Contains("Route strings such as `/api/*`, `#purchase`, `#product-cart-feedback` must be parameterized or host-owned.", plan, StringComparison.Ordinal);
            Assert.Contains("Do not enable strict failure until a component group has been migrated", plan, StringComparison.Ordinal);
        }

        [Fact]
        public void TargetContractAndHeadlessFolders_ExistWithOwnershipReadmes()
        {
            var expectedFolders = new[]
            {
                "Contracts/Catalog",
                "Contracts/Product",
                "Contracts/Cart",
                "Contracts/Checkout",
                "Contracts/Account",
                "Headless/Product",
                "Headless/Cart",
                "Headless/Checkout",
                "Headless/Account"
            };

            foreach (var folder in expectedFolders)
            {
                var readmePath = RepositoryPath($"BlazorShop.PresentationV2/BlazorShop.Storefront.Components/{folder}/README.md");

                Assert.True(File.Exists(readmePath), $"{folder} must have a README documenting ownership.");
                Assert.NotEmpty(File.ReadAllText(readmePath));
            }

            var featuresReadme = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/README.md");
            Assert.Contains("temporary compatibility area", featuresReadme, StringComparison.Ordinal);
            Assert.Contains("Contracts/{Capability}", featuresReadme, StringComparison.Ordinal);
            Assert.Contains("Headless/{Capability}", featuresReadme, StringComparison.Ordinal);
            Assert.Contains("Store-owned visual templates belong", featuresReadme, StringComparison.Ordinal);
        }

        [Fact]
        public void SharedProductSummaryCard_RemainsSemanticAfterHpr2Migration()
        {
            var sharedCard = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Catalog/ProductSummaryCard.razor");
            var v2Card = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/StorefrontProductSummaryCard.razor");

            Assert.Contains("data-storefront-product-summary-card", sharedCard, StringComparison.Ordinal);
            Assert.Contains("data-storefront-add-to-cart", sharedCard, StringComparison.Ordinal);
            Assert.Contains("data-unit-price=\"@Item.UnitPriceValue\"", sharedCard, StringComparison.Ordinal);
            Assert.Contains("data-currency-code=\"@Item.CurrencyCode\"", sharedCard, StringComparison.Ordinal);

            Assert.DoesNotContain("class=\"", sharedCard, StringComparison.Ordinal);
            Assert.DoesNotContain("rounded-", sharedCard, StringComparison.Ordinal);
            Assert.DoesNotContain("bg-neutral-", sharedCard, StringComparison.Ordinal);
            Assert.DoesNotContain("bg-amber-", sharedCard, StringComparison.Ordinal);
            Assert.DoesNotContain("text-neutral-", sharedCard, StringComparison.Ordinal);
            Assert.DoesNotContain("hover:", sharedCard, StringComparison.Ordinal);
            Assert.DoesNotContain("sm:", sharedCard, StringComparison.Ordinal);
            Assert.DoesNotContain("lg:", sharedCard, StringComparison.Ordinal);

            Assert.Contains("data-storefront-product-summary-card", v2Card, StringComparison.Ordinal);
            Assert.Contains("rounded-2xl", v2Card, StringComparison.Ordinal);
            Assert.Contains("bg-white/95", v2Card, StringComparison.Ordinal);
            Assert.Contains("hover:shadow-2xl", v2Card, StringComparison.Ordinal);
        }

        [Fact]
        public void SharedProductSummaryGrid_RemainsSemanticAfterHpr3Migration()
        {
            var sharedGrid = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Catalog/ProductSummaryGrid.razor");
            var v2Grid = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/StorefrontProductSummaryGrid.razor");
            var categoryPage = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/CategoryPage.razor");
            var searchPage = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/SearchPage.razor");
            var newReleasesPage = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/NewReleases.razor");

            Assert.Contains("data-storefront-product-summary-grid", sharedGrid, StringComparison.Ordinal);
            Assert.Contains("<ProductSummaryCard Item=\"item\" />", sharedGrid, StringComparison.Ordinal);
            Assert.DoesNotContain("class=\"", sharedGrid, StringComparison.Ordinal);
            Assert.DoesNotContain("grid-cols-", sharedGrid, StringComparison.Ordinal);
            Assert.DoesNotContain("sm:", sharedGrid, StringComparison.Ordinal);
            Assert.DoesNotContain("lg:", sharedGrid, StringComparison.Ordinal);
            Assert.DoesNotContain("rounded-", sharedGrid, StringComparison.Ordinal);
            Assert.DoesNotContain("bg-blue-", sharedGrid, StringComparison.Ordinal);

            Assert.Contains("<StorefrontProductSummaryCard Item=\"item\" />", v2Grid, StringComparison.Ordinal);
            Assert.Contains("grid gap-8 sm:grid-cols-2 lg:grid-cols-3", v2Grid, StringComparison.Ordinal);
            Assert.Contains("data-storefront-product-summary-empty", v2Grid, StringComparison.Ordinal);

            Assert.Contains("<StorefrontProductSummaryGrid Items=\"_productSummaries\"", categoryPage, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductSummaryGrid Items=\"_productSummaries\"", searchPage, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductSummaryGrid Items=\"_productSummaries\"", newReleasesPage, StringComparison.Ordinal);
        }

        [Fact]
        public void SharedDealsBlock_RemainsSemanticAfterHpr4Migration()
        {
            var sharedDeals = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Deals/DealsBlock.razor");
            var v2Deals = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/StorefrontDealsSection.razor");
            var home = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/Home.razor");
            var todaysDeals = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/TodaysDeals.razor");

            Assert.Contains("data-storefront-deals-block", sharedDeals, StringComparison.Ordinal);
            Assert.Contains("data-storefront-deals-items", sharedDeals, StringComparison.Ordinal);
            Assert.Contains("<ProductSummaryCard Item=\"item\" />", sharedDeals, StringComparison.Ordinal);
            Assert.DoesNotContain("<ProductSummaryGrid", sharedDeals, StringComparison.Ordinal);
            Assert.DoesNotContain("class=\"", sharedDeals, StringComparison.Ordinal);
            Assert.DoesNotContain("max-w-", sharedDeals, StringComparison.Ordinal);
            Assert.DoesNotContain("bg-amber-", sharedDeals, StringComparison.Ordinal);
            Assert.DoesNotContain("sm:", sharedDeals, StringComparison.Ordinal);
            Assert.DoesNotContain("lg:", sharedDeals, StringComparison.Ordinal);

            Assert.Contains("<StorefrontProductSummaryGrid Items=\"Items\"", v2Deals, StringComparison.Ordinal);
            Assert.Contains("mx-auto max-w-7xl", v2Deals, StringComparison.Ordinal);
            Assert.Contains("bg-amber-500", v2Deals, StringComparison.Ordinal);

            Assert.Contains("<StorefrontDealsSection Placement=\"DealsPlacement.Home\"", home, StringComparison.Ordinal);
            Assert.Contains("<StorefrontDealsSection Placement=\"DealsPlacement.DedicatedPage\"", todaysDeals, StringComparison.Ordinal);
            Assert.DoesNotContain("<DealsBlock", home + todaysDeals, StringComparison.Ordinal);
        }

        [Fact]
        public void ProductGallery_UsesHeadlessStateAndV2VisualTemplateAfterHpr5Migration()
        {
            var sharedGallery = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Product/ProductGallery.razor");
            var galleryState = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Product/ProductGalleryState.cs");
            var v2Gallery = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductGallery.razor");
            var productPage = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/ProductPage.razor");

            foreach (var expected in new[]
            {
                "SelectedIndex",
                "SelectedItem",
                "CanSelectPrevious",
                "CanSelectNext",
                "Select(int index)",
                "SelectPrevious()",
                "SelectNext()",
                "FallbackAltText",
                "data-storefront-gallery-thumbnail"
            })
            {
                Assert.Contains(expected, galleryState, StringComparison.Ordinal);
            }

            Assert.Contains("ProductGalleryState.Create(Items, ProductName)", sharedGallery, StringComparison.Ordinal);
            Assert.Contains("data-storefront-product-gallery", sharedGallery, StringComparison.Ordinal);
            Assert.Contains("data-storefront-gallery-main-image", sharedGallery, StringComparison.Ordinal);
            Assert.DoesNotContain("class=\"", sharedGallery, StringComparison.Ordinal);
            Assert.DoesNotContain("aspect-square", sharedGallery, StringComparison.Ordinal);
            Assert.DoesNotContain("rounded-", sharedGallery, StringComparison.Ordinal);
            Assert.DoesNotContain("bg-neutral-", sharedGallery, StringComparison.Ordinal);
            Assert.DoesNotContain("sm:grid", sharedGallery, StringComparison.Ordinal);

            Assert.Contains("ProductGalleryState.Create(Items, ProductName)", v2Gallery, StringComparison.Ordinal);
            Assert.Contains("aspect-square", v2Gallery, StringComparison.Ordinal);
            Assert.Contains("bs-product-gallery__main", v2Gallery, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductGallery Items=\"_galleryItems\" ProductName=\"@_product.Name\" />", productPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<ProductGallery Items=\"_galleryItems\"", productPage, StringComparison.Ordinal);
        }

        private static string[] EnumerateComponentFeatureFiles(string searchPattern)
        {
            var featureRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features");

            return Directory
                .EnumerateFiles(featureRoot, searchPattern, SearchOption.AllDirectories)
                .Where(path => !string.Equals(Path.GetFileName(path), "README.md", StringComparison.OrdinalIgnoreCase))
                .Select(path => Path.GetRelativePath(featureRoot, path).Replace('\\', '/'))
                .Order(StringComparer.Ordinal)
                .ToArray();
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(RepositoryPath(relativePath));
        }

        private static string RepositoryPath(string relativePath)
        {
            return Path.Combine(RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln"))
                    && File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate repository root.");
        }
    }
}
