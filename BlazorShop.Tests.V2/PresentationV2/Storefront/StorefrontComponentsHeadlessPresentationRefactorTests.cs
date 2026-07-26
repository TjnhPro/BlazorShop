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
