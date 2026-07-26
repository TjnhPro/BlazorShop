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

        [Fact]
        public void ProductPurchasePanel_UsesHostActionDescriptorAfterHpr6Migration()
        {
            var sharedPanel = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Product/ProductPurchasePanel.razor");
            var purchaseBehavior = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Product/ProductPurchaseBehavior.cs");
            var v2Panel = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductPurchasePanel.razor");
            var productPage = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/ProductPage.razor");

            foreach (var expected in new[]
            {
                "ProductPurchaseSnapshot",
                "ProductPurchaseSelectionState",
                "SelectedVariantId",
                "SelectedAttributes",
                "Quantity",
                "ValidationMessages",
                "CanAddToCart",
                "IsPreviewPending",
                "PreviewError",
                "IsAddToCartPending",
                "AddToCartError",
                "AddToCartSuccess",
                "ProductPurchaseActionDescriptor",
                "SelectionPreviewRoute",
                "PreviewContainerSelector",
                "FeedbackTargetSelector"
            })
            {
                Assert.Contains(expected, purchaseBehavior, StringComparison.Ordinal);
            }

            Assert.Contains("ProductPurchaseActionDescriptor.Empty", sharedPanel, StringComparison.Ordinal);
            Assert.Contains("data-preview-route=\"@Actions.SelectionPreviewRoute\"", sharedPanel, StringComparison.Ordinal);
            Assert.Contains("data-preview-container=\"@Actions.PreviewContainerSelector\"", sharedPanel, StringComparison.Ordinal);
            Assert.Contains("data-feedback-target=\"@Actions.FeedbackTargetSelector\"", sharedPanel, StringComparison.Ordinal);
            Assert.DoesNotContain("/api/", sharedPanel, StringComparison.Ordinal);
            Assert.DoesNotContain("#purchase", sharedPanel, StringComparison.Ordinal);
            Assert.DoesNotContain("#product-cart-feedback", sharedPanel, StringComparison.Ordinal);
            Assert.DoesNotContain("class=\"", sharedPanel, StringComparison.Ordinal);
            Assert.DoesNotContain("rounded-", sharedPanel, StringComparison.Ordinal);
            Assert.DoesNotContain("bg-neutral-", sharedPanel, StringComparison.Ordinal);
            Assert.DoesNotContain("bg-amber-", sharedPanel, StringComparison.Ordinal);

            Assert.Contains("ProductPurchaseActionDescriptor.StorefrontV2Default", v2Panel, StringComparison.Ordinal);
            Assert.Contains("/api/product-selection-preview", purchaseBehavior, StringComparison.Ordinal);
            Assert.Contains("id=\"@Actions.PanelId\"", v2Panel, StringComparison.Ordinal);
            Assert.Contains("data-feedback-target=\"@Actions.FeedbackTargetSelector\"", v2Panel, StringComparison.Ordinal);
            Assert.Contains("rounded-2xl", v2Panel, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductPurchasePanel Model=\"_purchasePanel\" />", productPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<ProductPurchasePanel Model=\"_purchasePanel\"", productPage, StringComparison.Ordinal);
        }

        [Fact]
        public void CartView_UsesHostActionsAndClassesAfterHpr7Migration()
        {
            var cartView = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Cart/CartView.razor");
            var cartBehavior = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Cart/StorefrontCartBehavior.cs");
            var cartOptions = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Cart/StorefrontCartViewOptions.cs");
            var cartPage = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor");

            foreach (var expected in new[]
            {
                "StorefrontCartActionDescriptor",
                "CurrentCartRoute",
                "UpdateLineRouteTemplate",
                "RemoveLineRouteTemplate",
                "ClearCartRoute",
                "StorefrontCartViewState",
                "Loading",
                "Empty",
                "HasError",
                "CheckoutAllowed",
                "StorefrontCartViewClasses"
            })
            {
                Assert.Contains(expected, cartBehavior, StringComparison.Ordinal);
            }

            Assert.Contains("Actions.CurrentCartRoute", cartView, StringComparison.Ordinal);
            Assert.Contains("Actions.UpdateLineRoute(line.LineId)", cartView, StringComparison.Ordinal);
            Assert.Contains("Actions.RemoveLineRoute(line.LineId)", cartView, StringComparison.Ordinal);
            Assert.Contains("Actions.ClearCartRoute", cartView, StringComparison.Ordinal);
            Assert.Contains("class=\"@Classes.", cartView, StringComparison.Ordinal);
            Assert.Contains("data-storefront-cart-quantity", cartView, StringComparison.Ordinal);
            Assert.Contains("data-storefront-cart-remove", cartView, StringComparison.Ordinal);
            Assert.Contains("data-storefront-cart-clear", cartView, StringComparison.Ordinal);
            Assert.DoesNotContain("\"/api/cart", cartView, StringComparison.Ordinal);
            Assert.DoesNotContain("rounded-", cartView, StringComparison.Ordinal);
            Assert.DoesNotContain("bg-neutral-", cartView, StringComparison.Ordinal);
            Assert.DoesNotContain("max-w-", cartView, StringComparison.Ordinal);
            Assert.DoesNotContain("sm:", cartView, StringComparison.Ordinal);
            Assert.DoesNotContain("lg:", cartView, StringComparison.Ordinal);

            Assert.Contains("\"/api/cart\"", cartOptions, StringComparison.Ordinal);
            Assert.Contains("rounded-3xl", cartOptions, StringComparison.Ordinal);
            Assert.Contains("max-w-7xl", cartOptions, StringComparison.Ordinal);
            Assert.Contains("Actions=\"StorefrontCartViewOptions.Actions\"", cartPage, StringComparison.Ordinal);
            Assert.Contains("Classes=\"StorefrontCartViewOptions.Classes\"", cartPage, StringComparison.Ordinal);
        }

        [Fact]
        public void CheckoutShell_UsesHostActionsAndClassesAfterHpr8Migration()
        {
            var checkoutShell = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Checkout/CheckoutShell.razor");
            var checkoutBehavior = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Checkout/StorefrontCheckoutBehavior.cs");
            var checkoutOptions = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Checkout/StorefrontCheckoutShellOptions.cs");
            var checkoutPage = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor");

            foreach (var expected in new[]
            {
                "StorefrontCheckoutActionDescriptor",
                "CurrentCheckoutRoute",
                "ShippingMethodRoute",
                "PaymentMethodRoute",
                "ReviewRoute",
                "PlaceOrderRoute",
                "StorefrontCheckoutViewState",
                "Loading",
                "PlaceOrderAllowed",
                "StorefrontCheckoutViewClasses"
            })
            {
                Assert.Contains(expected, checkoutBehavior, StringComparison.Ordinal);
            }

            Assert.Contains("Actions.CurrentCheckoutRoute", checkoutShell, StringComparison.Ordinal);
            Assert.Contains("Actions.ShippingMethodRoute", checkoutShell, StringComparison.Ordinal);
            Assert.Contains("Actions.PaymentMethodRoute", checkoutShell, StringComparison.Ordinal);
            Assert.Contains("Actions.ReviewRoute", checkoutShell, StringComparison.Ordinal);
            Assert.Contains("Actions.PlaceOrderRoute", checkoutShell, StringComparison.Ordinal);
            Assert.Contains("class=\"@Classes.", checkoutShell, StringComparison.Ordinal);
            Assert.Contains("data-storefront-checkout-shell", checkoutShell, StringComparison.Ordinal);
            Assert.Contains("data-storefront-checkout-cart-version", checkoutShell, StringComparison.Ordinal);
            Assert.DoesNotContain("\"/api/checkout", checkoutShell, StringComparison.Ordinal);
            Assert.DoesNotContain("rounded-", checkoutShell, StringComparison.Ordinal);
            Assert.DoesNotContain("bg-neutral-", checkoutShell, StringComparison.Ordinal);
            Assert.DoesNotContain("lg:", checkoutShell, StringComparison.Ordinal);

            Assert.Contains("\"/api/checkout\"", checkoutOptions, StringComparison.Ordinal);
            Assert.Contains("\"/api/checkout/place-order\"", checkoutOptions, StringComparison.Ordinal);
            Assert.Contains("rounded", checkoutOptions, StringComparison.Ordinal);
            Assert.Contains("Actions=\"StorefrontCheckoutShellOptions.Actions\"", checkoutPage, StringComparison.Ordinal);
            Assert.Contains("Classes=\"StorefrontCheckoutShellOptions.Classes\"", checkoutPage, StringComparison.Ordinal);
        }

        [Fact]
        public void AccountNavigation_UsesHostItemsAndClassesAfterHpr9Migration()
        {
            var navigation = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Account/AccountNavigation.razor");
            var contracts = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Account/AccountNavigationContracts.cs");
            var options = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Account/StorefrontAccountViewOptions.cs");
            var host = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor");
            var app = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Account/AccountApp.razor");

            foreach (var expected in new[]
            {
                "AccountNavigationItem",
                "RouteKey",
                "Label",
                "Href",
                "AccountNavigationClasses",
                "ActiveLink",
                "InactiveLink"
            })
            {
                Assert.Contains(expected, contracts, StringComparison.Ordinal);
            }

            Assert.Contains("data-storefront-account-navigation", navigation, StringComparison.Ordinal);
            Assert.Contains("data-storefront-account-nav-item=\"@item.RouteKey\"", navigation, StringComparison.Ordinal);
            Assert.Contains("class=\"@Classes.Nav\"", navigation, StringComparison.Ordinal);
            Assert.Contains("class=\"@LinkClass(item.Href)\"", navigation, StringComparison.Ordinal);
            Assert.Contains("Items { get; set; } = []", navigation, StringComparison.Ordinal);
            Assert.DoesNotContain("/account/profile", navigation, StringComparison.Ordinal);
            Assert.DoesNotContain("/account/orders", navigation, StringComparison.Ordinal);
            Assert.DoesNotContain("/account/addresses", navigation, StringComparison.Ordinal);
            Assert.DoesNotContain("/account/change-password", navigation, StringComparison.Ordinal);
            Assert.DoesNotContain("rounded", navigation, StringComparison.Ordinal);
            Assert.DoesNotContain("bg-neutral-", navigation, StringComparison.Ordinal);
            Assert.DoesNotContain("text-neutral-", navigation, StringComparison.Ordinal);
            Assert.DoesNotContain("hover:", navigation, StringComparison.Ordinal);

            Assert.Contains("new(\"profile\", \"Profile\", \"/account/profile\")", options, StringComparison.Ordinal);
            Assert.Contains("new(\"orders\", \"Orders\", \"/account/orders\")", options, StringComparison.Ordinal);
            Assert.Contains("new(\"addresses\", \"Addresses\", \"/account/addresses\")", options, StringComparison.Ordinal);
            Assert.Contains("new(\"change-password\", \"Password\", \"/account/change-password\")", options, StringComparison.Ordinal);
            Assert.Contains("rounded border border-neutral-200 bg-white", options, StringComparison.Ordinal);
            Assert.Contains("hover:bg-neutral-100", options, StringComparison.Ordinal);

            Assert.Contains("NavigationItems=\"StorefrontAccountViewOptions.NavigationItems\"", host, StringComparison.Ordinal);
            Assert.Contains("NavigationClasses=\"StorefrontAccountViewOptions.NavigationClasses\"", host, StringComparison.Ordinal);
            Assert.Contains("Items=\"NavigationItems\"", app, StringComparison.Ordinal);
            Assert.Contains("Classes=\"NavigationClasses\"", app, StringComparison.Ordinal);
        }

        [Fact]
        public void AccountProfileAndPasswordForms_UseHostActionsAndClassesAfterHpr10Migration()
        {
            var profile = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Account/AccountProfileEditor.razor");
            var password = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Account/AccountChangePasswordForm.razor");
            var behavior = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Account/StorefrontAccountFormBehavior.cs");
            var options = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Account/StorefrontAccountViewOptions.cs");
            var host = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor");
            var app = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Account/AccountApp.razor");

            foreach (var expected in new[]
            {
                "StorefrontAccountProfileActionDescriptor",
                "LoadProfileRoute",
                "SaveProfileRoute",
                "StorefrontAccountPasswordActionDescriptor",
                "ChangePasswordRoute",
                "StorefrontAccountFormClasses",
                "ProfileForm",
                "PasswordForm",
                "SubmitButton"
            })
            {
                Assert.Contains(expected, behavior, StringComparison.Ordinal);
            }

            Assert.Contains("GetAsync<StorefrontBrowserCustomerProfile>(Actions.LoadProfileRoute)", profile, StringComparison.Ordinal);
            Assert.Contains("PutJsonAsync<StorefrontBrowserCustomerProfileUpdateRequest, StorefrontBrowserCustomerProfile>(Actions.SaveProfileRoute", profile, StringComparison.Ordinal);
            Assert.Contains("PostJsonAsync<object, StorefrontBrowserAccountCommandResult>", password, StringComparison.Ordinal);
            Assert.Contains("Actions.ChangePasswordRoute", password, StringComparison.Ordinal);
            Assert.Contains("class=\"@Classes.", profile + password, StringComparison.Ordinal);
            Assert.Contains("Passwords do not match.", password, StringComparison.Ordinal);

            foreach (var sharedSource in new[] { profile, password })
            {
                Assert.DoesNotContain("/api/account/profile", sharedSource, StringComparison.Ordinal);
                Assert.DoesNotContain("/api/account/change-password", sharedSource, StringComparison.Ordinal);
                Assert.DoesNotContain("rounded", sharedSource, StringComparison.Ordinal);
                Assert.DoesNotContain("bg-neutral-", sharedSource, StringComparison.Ordinal);
                Assert.DoesNotContain("text-neutral-", sharedSource, StringComparison.Ordinal);
                Assert.DoesNotContain("hover:", sharedSource, StringComparison.Ordinal);
                Assert.DoesNotContain("sm:", sharedSource, StringComparison.Ordinal);
            }

            Assert.Contains("ProfileActions", options, StringComparison.Ordinal);
            Assert.Contains("PasswordActions", options, StringComparison.Ordinal);
            Assert.Contains("\"/api/account/profile\"", options, StringComparison.Ordinal);
            Assert.Contains("\"/api/account/change-password\"", options, StringComparison.Ordinal);
            Assert.Contains("ProfileForm = \"grid max-w-3xl gap-5 sm:grid-cols-2\"", options, StringComparison.Ordinal);
            Assert.Contains("SubmitButton = \"inline-flex rounded bg-neutral-900", options, StringComparison.Ordinal);

            Assert.Contains("ProfileActions=\"StorefrontAccountViewOptions.ProfileActions\"", host, StringComparison.Ordinal);
            Assert.Contains("PasswordActions=\"StorefrontAccountViewOptions.PasswordActions\"", host, StringComparison.Ordinal);
            Assert.Contains("AccountFormClasses=\"StorefrontAccountViewOptions.FormClasses\"", host, StringComparison.Ordinal);
            Assert.Contains("nameof(AccountProfileEditor.Actions)", app, StringComparison.Ordinal);
            Assert.Contains("nameof(AccountChangePasswordForm.Actions)", app, StringComparison.Ordinal);
            Assert.Contains("nameof(AccountProfileEditor.Classes)", app, StringComparison.Ordinal);
            Assert.Contains("nameof(AccountChangePasswordForm.Classes)", app, StringComparison.Ordinal);
        }

        [Fact]
        public void AccountAddressBook_UsesHostActionsAndClassesAfterHpr11Migration()
        {
            var addresses = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Account/AccountAddressBook.razor");
            var behavior = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Account/StorefrontAccountFormBehavior.cs");
            var options = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Account/StorefrontAccountViewOptions.cs");
            var host = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor");
            var app = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Account/AccountApp.razor");

            foreach (var expected in new[]
            {
                "StorefrontAccountAddressActionDescriptor",
                "CurrentAddressesRoute",
                "CreateAddressRoute",
                "UpdateAddressRouteTemplate",
                "DeleteAddressRouteTemplate",
                "DefaultShippingRouteTemplate",
                "DefaultBillingRouteTemplate",
                "UpdateAddressRoute(Guid addressId)",
                "StorefrontAccountAddressBookClasses",
                "CompactWideField",
                "FullWideField"
            })
            {
                Assert.Contains(expected, behavior, StringComparison.Ordinal);
            }

            Assert.Contains("GetAsync<IReadOnlyList<StorefrontBrowserCustomerAddress>>(Actions.CurrentAddressesRoute)", addresses, StringComparison.Ordinal);
            Assert.Contains("PostJsonAsync<StorefrontBrowserCustomerAddressRequest, StorefrontBrowserCustomerAddress>", addresses, StringComparison.Ordinal);
            Assert.Contains("Actions.CreateAddressRoute", addresses, StringComparison.Ordinal);
            Assert.Contains("Actions.UpdateAddressRoute(addressId)", addresses, StringComparison.Ordinal);
            Assert.Contains("Actions.DeleteAddressRoute(addressId)", addresses, StringComparison.Ordinal);
            Assert.Contains("Actions.DefaultShippingRoute(addressId)", addresses, StringComparison.Ordinal);
            Assert.Contains("Actions.DefaultBillingRoute(addressId)", addresses, StringComparison.Ordinal);
            Assert.Contains("class=\"@Classes.", addresses, StringComparison.Ordinal);
            Assert.Contains("Classes.CompactInput", addresses, StringComparison.Ordinal);
            Assert.Contains("Classes.FullInput", addresses, StringComparison.Ordinal);
            Assert.DoesNotContain("/api/account/addresses", addresses, StringComparison.Ordinal);
            Assert.DoesNotContain("rounded", addresses, StringComparison.Ordinal);
            Assert.DoesNotContain("bg-neutral-", addresses, StringComparison.Ordinal);
            Assert.DoesNotContain("text-neutral-", addresses, StringComparison.Ordinal);
            Assert.DoesNotContain("text-rose-", addresses, StringComparison.Ordinal);
            Assert.DoesNotContain("text-sky-", addresses, StringComparison.Ordinal);
            Assert.DoesNotContain("sm:", addresses, StringComparison.Ordinal);
            Assert.DoesNotContain("xl:", addresses, StringComparison.Ordinal);

            Assert.Contains("AddressActions", options, StringComparison.Ordinal);
            Assert.Contains("AddressClasses", options, StringComparison.Ordinal);
            Assert.Contains("\"/api/account/addresses\"", options, StringComparison.Ordinal);
            Assert.Contains("\"/api/account/addresses/{addressId}/default-shipping\"", options, StringComparison.Ordinal);
            Assert.Contains("ListGrid = \"grid gap-4 xl:grid-cols-2\"", options, StringComparison.Ordinal);

            Assert.Contains("AddressActions=\"StorefrontAccountViewOptions.AddressActions\"", host, StringComparison.Ordinal);
            Assert.Contains("AddressClasses=\"StorefrontAccountViewOptions.AddressClasses\"", host, StringComparison.Ordinal);
            Assert.Contains("nameof(AccountAddressBook.Actions)", app, StringComparison.Ordinal);
            Assert.Contains("nameof(AccountAddressBook.Classes)", app, StringComparison.Ordinal);
        }

        [Fact]
        public void AccountOrders_UseHostActionsAndClassesAfterHpr12Migration()
        {
            var orderList = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Account/AccountOrderList.razor");
            var orderDetail = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Account/AccountOrderDetail.razor");
            var behavior = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Account/StorefrontAccountFormBehavior.cs");
            var options = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Account/StorefrontAccountViewOptions.cs");
            var host = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor");
            var app = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Account/AccountApp.razor");

            foreach (var expected in new[]
            {
                "StorefrontAccountOrderActionDescriptor",
                "OrderListRouteTemplate",
                "OrderDetailRouteTemplate",
                "ReceiptRouteTemplate",
                "OrderDetailHrefTemplate",
                "OrderListRoute(int pageNumber)",
                "OrderDetailRoute(string orderReference)",
                "ReceiptRoute(string orderReference)",
                "OrderDetailHref(string orderReference)",
                "StorefrontAccountOrderListClasses",
                "StorefrontAccountOrderDetailClasses"
            })
            {
                Assert.Contains(expected, behavior, StringComparison.Ordinal);
            }

            Assert.Contains("GetAsync<StorefrontBrowserAccountOrderList>(Actions.OrderListRoute(PageNumber))", orderList, StringComparison.Ordinal);
            Assert.Contains("href=\"@Actions.OrderDetailHref(order.Reference)\"", orderList, StringComparison.Ordinal);
            Assert.Contains("Actions.ReceiptRoute(OrderReference)", orderDetail, StringComparison.Ordinal);
            Assert.Contains("Actions.OrderDetailRoute(OrderReference)", orderDetail, StringComparison.Ordinal);
            Assert.Contains("GetAsync<StorefrontBrowserAccountOrderDetail>(route)", orderDetail, StringComparison.Ordinal);
            Assert.Contains("class=\"@Classes.", orderList + orderDetail, StringComparison.Ordinal);
            Assert.Contains("Classes.AddressSection", orderDetail, StringComparison.Ordinal);
            Assert.Contains("Classes.AddressStrongLine", orderDetail, StringComparison.Ordinal);

            foreach (var sharedSource in new[] { orderList, orderDetail })
            {
                Assert.DoesNotContain("/api/account/orders", sharedSource, StringComparison.Ordinal);
                Assert.DoesNotContain("/account/orders/", sharedSource, StringComparison.Ordinal);
                Assert.DoesNotContain("rounded", sharedSource, StringComparison.Ordinal);
                Assert.DoesNotContain("bg-neutral-", sharedSource, StringComparison.Ordinal);
                Assert.DoesNotContain("text-neutral-", sharedSource, StringComparison.Ordinal);
                Assert.DoesNotContain("text-rose-", sharedSource, StringComparison.Ordinal);
                Assert.DoesNotContain("hover:", sharedSource, StringComparison.Ordinal);
                Assert.DoesNotContain("sm:", sharedSource, StringComparison.Ordinal);
                Assert.DoesNotContain("md:", sharedSource, StringComparison.Ordinal);
                Assert.DoesNotContain("lg:", sharedSource, StringComparison.Ordinal);
            }

            Assert.Contains("OrderActions", options, StringComparison.Ordinal);
            Assert.Contains("\"/api/account/orders?page={pageNumber}\"", options, StringComparison.Ordinal);
            Assert.Contains("\"/api/account/orders/{orderReference}/receipt\"", options, StringComparison.Ordinal);
            Assert.Contains("\"/account/orders/{orderReference}\"", options, StringComparison.Ordinal);
            Assert.Contains("OrderListClasses", options, StringComparison.Ordinal);
            Assert.Contains("OrderDetailClasses", options, StringComparison.Ordinal);

            Assert.Contains("OrderActions=\"StorefrontAccountViewOptions.OrderActions\"", host, StringComparison.Ordinal);
            Assert.Contains("OrderListClasses=\"StorefrontAccountViewOptions.OrderListClasses\"", host, StringComparison.Ordinal);
            Assert.Contains("OrderDetailClasses=\"StorefrontAccountViewOptions.OrderDetailClasses\"", host, StringComparison.Ordinal);
            Assert.Contains("nameof(AccountOrderList.Actions)", app, StringComparison.Ordinal);
            Assert.Contains("nameof(AccountOrderDetail.Actions)", app, StringComparison.Ordinal);
            Assert.Contains("nameof(AccountOrderList.Classes)", app, StringComparison.Ordinal);
            Assert.Contains("nameof(AccountOrderDetail.Classes)", app, StringComparison.Ordinal);
        }

        [Fact]
        public void AccountApp_UsesHostShellClassesAfterHpr13Migration()
        {
            var app = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Account/AccountApp.razor");
            var behavior = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Account/StorefrontAccountFormBehavior.cs");
            var options = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Account/StorefrontAccountViewOptions.cs");
            var host = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor");

            Assert.Contains("StorefrontAccountShellClasses", behavior, StringComparison.Ordinal);
            Assert.Contains("ShellClasses.Section", app, StringComparison.Ordinal);
            Assert.Contains("ShellClasses.Layout", app, StringComparison.Ordinal);
            Assert.Contains("ShellClasses.ContentArticle", app, StringComparison.Ordinal);
            Assert.Contains("ShellClasses.UnknownAlert", app, StringComparison.Ordinal);
            Assert.Contains("ShellClasses=\"StorefrontAccountViewOptions.ShellClasses\"", host, StringComparison.Ordinal);
            Assert.Contains("Section = \"mx-auto max-w-7xl px-4 pb-12 pt-10 sm:px-6 lg:px-8\"", options, StringComparison.Ordinal);
            Assert.Contains("UnknownAlert = \"rounded border border-rose-200", options, StringComparison.Ordinal);

            foreach (var expectedRoute in new[]
            {
                "string.Equals(normalized, \"profile\"",
                "string.Equals(normalized, \"addresses\"",
                "string.Equals(normalized, \"orders\"",
                "string.Equals(normalized, \"change-password\"",
                "Uri.UnescapeDataString(segments[1])",
                "AccountRouteKind.OrderDetail"
            })
            {
                Assert.Contains(expectedRoute, app, StringComparison.Ordinal);
            }

            Assert.Contains("AccountProfileEditor", app, StringComparison.Ordinal);
            Assert.Contains("AccountAddressBook", app, StringComparison.Ordinal);
            Assert.Contains("AccountOrderList", app, StringComparison.Ordinal);
            Assert.Contains("AccountOrderDetail", app, StringComparison.Ordinal);
            Assert.Contains("AccountChangePasswordForm", app, StringComparison.Ordinal);
            Assert.DoesNotContain("mx-auto", app, StringComparison.Ordinal);
            Assert.DoesNotContain("max-w-", app, StringComparison.Ordinal);
            Assert.DoesNotContain("rounded", app, StringComparison.Ordinal);
            Assert.DoesNotContain("bg-white", app, StringComparison.Ordinal);
            Assert.DoesNotContain("bg-rose-", app, StringComparison.Ordinal);
            Assert.DoesNotContain("text-neutral-", app, StringComparison.Ordinal);
            Assert.DoesNotContain("sm:", app, StringComparison.Ordinal);
            Assert.DoesNotContain("lg:", app, StringComparison.Ordinal);
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
