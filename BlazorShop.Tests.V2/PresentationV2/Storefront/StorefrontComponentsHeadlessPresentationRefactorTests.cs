namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontComponentsHeadlessPresentationRefactorTests
    {
        private static readonly string[] ExpectedContractModelAndEnumFiles =
        [
            "Account/AccountLabels.cs",
            "Account/AccountRouteDescriptor.cs",
            "Brand/StorefrontBrandLogoClasses.cs",
            "Brand/StorefrontBrandLogoContext.cs",
            "Cart/CartLabels.cs",
            "Catalog/CatalogFilterCategoryOption.cs",
            "Catalog/ProductSummaryItem.cs",
            "Catalog/ProductSummaryLabels.cs",
            "Catalog/StorefrontDiscountedProductRailActionDescriptor.cs",
            "Catalog/StorefrontDiscountedProductRailClasses.cs",
            "Catalog/StorefrontDiscountedProductRailLabels.cs",
            "Catalog/StorefrontDiscountedProductRailRequest.cs",
            "Catalog/StorefrontDiscountedProductRailResponse.cs",
            "Catalog/StorefrontDiscountedProductRailState.cs",
            "Checkout/CheckoutLabels.cs",
            "Components/StorefrontComponentCategory.cs",
            "Components/StorefrontComponentDescriptor.cs",
            "Components/StorefrontComponentDescriptorValidationResult.cs",
            "Components/StorefrontComponentDescriptorValidator.cs",
            "Components/StorefrontComponentMode.cs",
            "Contact/StorefrontContactFormActionDescriptor.cs",
            "Contact/StorefrontContactFormClasses.cs",
            "Contact/StorefrontContactFormLabels.cs",
            "Contact/StorefrontContactFormState.cs",
            "Contact/StorefrontContactFormSubmitRequest.cs",
            "Contact/StorefrontContactFormSubmitResult.cs",
            "Deals/DealsPlacement.cs",
            "Product/ProductGalleryItem.cs",
            "Product/ProductGalleryLabels.cs",
            "Product/ProductPurchaseLabels.cs",
            "Product/ProductPurchaseOptionItem.cs",
            "Product/ProductPurchaseOptionValueItem.cs",
            "Product/ProductPurchasePanelModel.cs",
            "Product/ProductPurchaseVariantItem.cs",
            "System/StorefrontHybridRuntimeProbeClasses.cs",
            "System/StorefrontHybridRuntimeProbeLabels.cs"
        ];

        private static readonly string[] ExpectedBrowserContractFiles =
        [
            "StorefrontBrowserAccountModels.cs",
            "StorefrontBrowserCartModels.cs",
            "StorefrontBrowserCheckoutModels.cs",
            "StorefrontFeatureDataMode.cs",
            "StorefrontLocalApiErrorResponse.cs"
        ];

        [Fact]
        public void StorefrontComponents_HasNoFeaturesFolder()
        {
            Assert.False(Directory.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features")));
        }

        [Fact]
        public void StorefrontComponents_HasNoRazorFiles()
        {
            var componentRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components");
            var razorFiles = Directory
                .EnumerateFiles(componentRoot, "*.razor", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Select(path => Path.GetRelativePath(componentRoot, path).Replace('\\', '/'))
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(razorFiles);
        }

        [Fact]
        public void StorefrontComponents_UsesClassLibrarySdk()
        {
            var project = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj");
            var v2Interop = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontWasmInterop.js");
            var sharedInterop = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/wwwroot/js/storefrontWasmInterop.js");
            var browserInterop = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/wwwroot/js/storefrontWasmInterop.js");
            var browserRuntimeSource = ReadSourceTree(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Browser"));
            var cartView = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartView.razor");

            Assert.Contains("<Project Sdk=\"Microsoft.NET.Sdk\">", project, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.NET.Sdk.Razor", project, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.JSInterop", project, StringComparison.Ordinal);
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/_Imports.razor")));
            Assert.False(File.Exists(sharedInterop));
            Assert.True(File.Exists(browserInterop));
            Assert.False(File.Exists(v2Interop));
            Assert.Contains("./_content/BlazorShop.Storefront.Browser/js/storefrontWasmInterop.js", browserRuntimeSource, StringComparison.Ordinal);
            Assert.Contains("IStorefrontBrowserCartController", cartView, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontV2_DoesNotImportComponentsFeatures()
        {
            var v2Root = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2");
            var source = ReadSourceTree(v2Root);

            Assert.DoesNotContain("BlazorShop.Storefront.Components.Features", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Components.Features", source, StringComparison.Ordinal);
        }

        [Fact]
        public void StarterAndGeneratedTemplates_DoNotImportComponentsFeatures()
        {
            var roots = new[]
            {
                RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter"),
                RepositoryPath("tools/BlazorShop.AI.StorefrontBuilder")
            };
            var source = string.Join(Environment.NewLine, roots.Select(ReadSourceTree));

            Assert.Contains("Features/feature-manifest.json", source.Replace('\\', '/'), StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.Components.Features", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Components.Features", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Storefront.Components/Features", source.Replace('\\', '/'), StringComparison.Ordinal);
        }

        [Fact]
        public void ContractModelInventory_RecordsReusableProductAndCatalogContracts()
        {
            var actual = EnumerateComponentContractFiles("*.cs");

            Assert.Equal(ExpectedContractModelAndEnumFiles, actual);
            Assert.Equal(36, actual.Length);
        }

        [Fact]
        public void BrowserContractInventory_RecordsCurrentSharedBffContracts()
        {
            var browserRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Browser");
            var actual = Directory
                .EnumerateFiles(browserRoot, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(ExpectedBrowserContractFiles, actual);
        }

        [Fact]
        public void NeutralityGuardDesign_IsDocumentedButNotStrictBeforeMigration()
        {
            var plan = ReadRepositoryFile(
                "docs/refactor-control-Commerce-storefront/Storefront Component Mode Foundation.todo.md");

            foreach (var token in new[]
            {
                "class=\"rounded",
                "class=\"bg-",
                "class=\"text-",
                "class=\"shadow",
                "class=\"grid",
                "class=\"flex",
                "class=\"px-",
                "class=\"mx-",
                "responsive Tailwind prefixes"
            })
            {
                Assert.Contains(token, plan, StringComparison.Ordinal);
            }

            Assert.Contains("Allow `class=\"@...\"", plan, StringComparison.Ordinal);
            Assert.Contains("Allow `data-storefront-*`", plan, StringComparison.Ordinal);
            Assert.Contains("Assert no V2 CSS classes or V2 asset paths are referenced", plan, StringComparison.Ordinal);
        }

        [Fact]
        public void TargetContractAndHeadlessFolders_ExistWithOwnershipReadmes()
        {
            var expectedFolders = new[]
            {
                "Contracts/Catalog",
                "Contracts/Deals",
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

            Assert.False(Directory.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features")));
        }

        [Fact]
        public void ComponentsDependencyDirection_KeepsContractsAndHeadlessBelowBrowserAndHostMarkup()
        {
            var contractSource = ReadComponentLayerSource("Contracts");
            var headlessSource = ReadComponentLayerSource("Headless");

            foreach (var forbiddenContractDependency in new[]
            {
                "BlazorShop.Storefront.Components.Headless",
                "BlazorShop.Storefront.Components.Browser",
                "BlazorShop.Storefront.Components.Features",
                ".Headless.",
                ".Browser.",
                ".Features."
            })
            {
                Assert.DoesNotContain(forbiddenContractDependency, contractSource, StringComparison.Ordinal);
            }

            Assert.Contains("BlazorShop.Storefront.Components.Contracts", headlessSource, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.Components.Features", headlessSource, StringComparison.Ordinal);
            Assert.DoesNotContain(".Features.", headlessSource, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontComponents_HeadlessHasNoVisualClassBags()
        {
            var headlessSource = ReadComponentLayerSource("Headless");
            var v2ClassSource =
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountViewClasses.cs")
                + ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartViewClasses.cs")
                + ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutViewClasses.cs");

            foreach (var classBag in new[]
            {
                "StorefrontCartViewClasses",
                "StorefrontCheckoutViewClasses",
                "AccountNavigationClasses",
                "StorefrontAccountFormClasses",
                "StorefrontAccountAddressBookClasses",
                "StorefrontAccountOrderListClasses",
                "StorefrontAccountOrderDetailClasses",
                "StorefrontAccountShellClasses"
            })
            {
                Assert.DoesNotContain(classBag, headlessSource, StringComparison.Ordinal);
                Assert.Contains(classBag, v2ClassSource, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void SharedProductSummaryCard_RemainsSemanticAfterHpr2Migration()
        {
            Assert.False(File.Exists(RepositoryPath(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Catalog/ProductSummaryCard.razor")));
            var productSummaryContract = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Catalog/ProductSummaryItem.cs");
            var v2Card = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/StorefrontProductSummaryCard.razor");

            Assert.Contains("public sealed record ProductSummaryItem", productSummaryContract, StringComparison.Ordinal);
            Assert.Contains("data-storefront-product-summary-card", v2Card, StringComparison.Ordinal);
            Assert.Contains("data-storefront-product-purchase", v2Card, StringComparison.Ordinal);
            Assert.Contains("data-storefront-command=\"cart.add-line\"", v2Card, StringComparison.Ordinal);
            Assert.Contains("data-storefront-product-purchase-submit", v2Card, StringComparison.Ordinal);
            Assert.Contains("data-currency-code=\"@Item.CurrencyCode\"", v2Card, StringComparison.Ordinal);
            Assert.Contains("rounded-2xl", v2Card, StringComparison.Ordinal);
            Assert.Contains("bg-white/95", v2Card, StringComparison.Ordinal);
            Assert.Contains("hover:shadow-2xl", v2Card, StringComparison.Ordinal);
        }

        [Fact]
        public void SharedProductSummaryGrid_RemainsSemanticAfterHpr3Migration()
        {
            Assert.False(File.Exists(RepositoryPath(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Catalog/ProductSummaryGrid.razor")));
            var v2Grid = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/StorefrontProductSummaryGrid.razor");
            var categoryPage = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/CategoryPage.razor");
            var searchPage = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/SearchPage.razor");

            Assert.Contains("data-storefront-product-summary-grid", v2Grid, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductSummaryCard Item=\"item\" />", v2Grid, StringComparison.Ordinal);
            Assert.Contains("grid gap-8 sm:grid-cols-2 lg:grid-cols-3", v2Grid, StringComparison.Ordinal);
            Assert.Contains("data-storefront-product-summary-empty", v2Grid, StringComparison.Ordinal);

            Assert.Contains("<StorefrontProductSummaryGrid Items=\"Context.ProductSummaries\"", categoryPage, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductSummaryGrid Items=\"Context.ProductSummaries\"", searchPage, StringComparison.Ordinal);
        }

        [Fact]
        public void SharedDealsBlock_RemainsSemanticAfterHpr4Migration()
        {
            Assert.False(File.Exists(RepositoryPath(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Deals/DealsBlock.razor")));
            var v2Deals = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/StorefrontDealsSection.razor");
            var dealsPlacement = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Deals/DealsPlacement.cs");
            var home = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/Home.razor");

            Assert.Contains("data-storefront-deals-block", v2Deals, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductSummaryGrid Items=\"Items\"", v2Deals, StringComparison.Ordinal);
            Assert.Contains("mx-auto max-w-7xl", v2Deals, StringComparison.Ordinal);
            Assert.Contains("bg-amber-500", v2Deals, StringComparison.Ordinal);
            Assert.DoesNotContain("DedicatedPage", dealsPlacement, StringComparison.Ordinal);
            Assert.DoesNotContain("DedicatedPage", v2Deals, StringComparison.Ordinal);

            Assert.Contains("<StorefrontDealsSection Placement=\"DealsPlacement.Home\"", home, StringComparison.Ordinal);
            Assert.DoesNotContain("<DealsBlock", home, StringComparison.Ordinal);
        }

        [Fact]
        public void ProductGallery_UsesHeadlessStateAndV2VisualTemplateAfterHpr5Migration()
        {
            Assert.False(File.Exists(RepositoryPath(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Product/ProductGallery.razor")));
            var galleryState = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Product/ProductGalleryState.cs");
            var v2Gallery = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductGallery.razor");
            var productPage = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Product/V2ProductPageView.razor");

            foreach (var expected in new[]
            {
                "SelectedIndex",
                "SelectedItem",
                "CanSelectPrevious",
                "CanSelectNext",
                "Select(int index)",
                "SelectPrevious()",
                "SelectNext()",
                "data-storefront-gallery-thumbnail"
            })
            {
                Assert.Contains(expected, galleryState, StringComparison.Ordinal);
            }

            Assert.Contains("ProductGalleryState.Create(Items, ProductName)", v2Gallery, StringComparison.Ordinal);
            Assert.Contains("data-storefront-product-gallery", v2Gallery, StringComparison.Ordinal);
            Assert.Contains("data-storefront-gallery-main-image", v2Gallery, StringComparison.Ordinal);
            Assert.Contains("aspect-square", v2Gallery, StringComparison.Ordinal);
            Assert.Contains("bs-product-gallery__main", v2Gallery, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductGallery Items=\"_galleryItems\" ProductName=\"@_product.Name\" />", productPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<ProductGallery Items=\"_galleryItems\"", productPage, StringComparison.Ordinal);
        }

        [Fact]
        public void ProductPurchasePanel_UsesHostActionDescriptorAfterHpr6Migration()
        {
            Assert.False(File.Exists(RepositoryPath(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Product/ProductPurchasePanel.razor")));
            var purchaseBehavior = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Product/ProductPurchaseBehavior.cs");
            var v2Panel = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductPurchasePanel.razor");
            var productContext = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Product/StorefrontProductPageContext.cs");
            var productMapper = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Product/StorefrontProductPageMapper.cs");
            var productPage = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Product/V2ProductPageView.razor");

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

            Assert.Contains("ProductPurchaseActionDescriptor.Empty", v2Panel, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontV2Default", purchaseBehavior, StringComparison.Ordinal);
            Assert.DoesNotContain("/api/product-selection-preview", purchaseBehavior, StringComparison.Ordinal);
            Assert.Contains("ProductPurchaseActionDescriptor PurchaseActions", productContext, StringComparison.Ordinal);
            Assert.Contains("StorefrontRoutes.ProductSelectionPreview", productMapper, StringComparison.Ordinal);
            Assert.Contains("Actions=\"Context.PurchaseActions\"", productPage, StringComparison.Ordinal);
            Assert.Contains("id=\"@Actions.PanelId\"", v2Panel, StringComparison.Ordinal);
            Assert.Contains("data-selection-preview-route=\"@Actions.SelectionPreviewRoute\"", v2Panel, StringComparison.Ordinal);
            Assert.Contains("data-storefront-product-purchase-submit", v2Panel, StringComparison.Ordinal);
            Assert.DoesNotContain("data-preview-container", v2Panel, StringComparison.Ordinal);
            Assert.Contains("data-feedback-target=\"@Actions.FeedbackTargetSelector\"", v2Panel, StringComparison.Ordinal);
            Assert.Contains("disabled=\"@(!Model.CanSubmitInitialPurchase)\"", v2Panel, StringComparison.Ordinal);
            Assert.Contains("rounded-2xl", v2Panel, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductPurchasePanel Model=\"_purchasePanel\" Actions=\"Context.PurchaseActions\" />", productPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<ProductPurchasePanel Model=\"_purchasePanel\"", productPage, StringComparison.Ordinal);
        }

        [Fact]
        public void CartView_UsesHostActionsAndClassesAfterHpr7Migration()
        {
            var cartView = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartView.razor");
            var cartController = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/Cart/StorefrontBrowserCartController.cs");
            var cartBehavior = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Cart/StorefrontCartBehavior.cs");
            var cartOptions = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartViewOptions.cs");
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
                "CheckoutAllowed"
            })
            {
                Assert.Contains(expected, cartBehavior, StringComparison.Ordinal);
            }

            Assert.Contains("_actions.CurrentCartRoute", cartController, StringComparison.Ordinal);
            Assert.Contains("_actions.UpdateLineRoute(lineId)", cartController, StringComparison.Ordinal);
            Assert.Contains("_actions.RemoveLineRoute(lineId)", cartController, StringComparison.Ordinal);
            Assert.Contains("_actions.ClearCartRoute", cartController, StringComparison.Ordinal);
            Assert.Contains("CartController.UpdateQuantityAsync(line.LineId, value)", cartView, StringComparison.Ordinal);
            Assert.Contains("CartController.RemoveLineAsync(line.LineId)", cartView, StringComparison.Ordinal);
            Assert.Contains("CartController.ClearAsync()", cartView, StringComparison.Ordinal);
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

            Assert.DoesNotContain("\"/api/cart", cartOptions, StringComparison.Ordinal);
            Assert.Contains("rounded-3xl", cartOptions, StringComparison.Ordinal);
            Assert.Contains("max-w-7xl", cartOptions, StringComparison.Ordinal);
            Assert.Contains("Actions=\"@Context.CartActions\"", cartPage, StringComparison.Ordinal);
            Assert.Contains("Classes=\"StorefrontCartViewOptions.Classes\"", cartPage, StringComparison.Ordinal);
            Assert.Contains("<StorefrontCartView", cartPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<CartView", cartPage, StringComparison.Ordinal);
        }

        [Fact]
        public void CheckoutShell_UsesHostActionsAndClassesAfterHpr8Migration()
        {
            var checkoutShell = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutShell.razor");
            var checkoutController = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/Checkout/StorefrontBrowserCheckoutController.cs");
            var checkoutBehavior = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Checkout/StorefrontCheckoutBehavior.cs");
            var checkoutOptions = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutShellOptions.cs");
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
                "PlaceOrderAllowed"
            })
            {
                Assert.Contains(expected, checkoutBehavior, StringComparison.Ordinal);
            }

            Assert.Contains("_actions.CurrentCheckoutRoute", checkoutController, StringComparison.Ordinal);
            Assert.Contains("_actions.ShippingMethodRoute", checkoutController, StringComparison.Ordinal);
            Assert.Contains("_actions.PaymentMethodRoute", checkoutController, StringComparison.Ordinal);
            Assert.Contains("_actions.ReviewRoute", checkoutController, StringComparison.Ordinal);
            Assert.Contains("_actions.PlaceOrderRoute", checkoutController, StringComparison.Ordinal);
            Assert.Contains("CheckoutController.SelectShippingAsync(key)", checkoutShell, StringComparison.Ordinal);
            Assert.Contains("CheckoutController.SelectPaymentAsync(key)", checkoutShell, StringComparison.Ordinal);
            Assert.Contains("CheckoutController.ReviewAsync()", checkoutShell, StringComparison.Ordinal);
            Assert.Contains("CheckoutController.PlaceOrderAsync()", checkoutShell, StringComparison.Ordinal);
            Assert.Contains("class=\"@Classes.", checkoutShell, StringComparison.Ordinal);
            Assert.Contains("data-storefront-checkout-shell", checkoutShell, StringComparison.Ordinal);
            Assert.Contains("data-storefront-checkout-cart-version", checkoutShell, StringComparison.Ordinal);
            Assert.DoesNotContain("\"/api/checkout", checkoutShell, StringComparison.Ordinal);
            Assert.DoesNotContain("rounded-", checkoutShell, StringComparison.Ordinal);
            Assert.DoesNotContain("bg-neutral-", checkoutShell, StringComparison.Ordinal);
            Assert.DoesNotContain("lg:", checkoutShell, StringComparison.Ordinal);

            Assert.DoesNotContain("\"/api/checkout", checkoutOptions, StringComparison.Ordinal);
            Assert.Contains("rounded", checkoutOptions, StringComparison.Ordinal);
            Assert.Contains("Actions=\"@Context.CheckoutActions\"", checkoutPage, StringComparison.Ordinal);
            Assert.Contains("Classes=\"StorefrontCheckoutShellOptions.Classes\"", checkoutPage, StringComparison.Ordinal);
            Assert.Contains("<StorefrontCheckoutShell", checkoutPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<CheckoutShell", checkoutPage, StringComparison.Ordinal);
            Assert.Equal(2, CountOccurrences(checkoutPage, "ShowPanel=\"false\""));
        }

        [Fact]
        public void AccountNavigation_UsesHostItemsAndClassesAfterHpr9Migration()
        {
            var navigation = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountNavigation.razor");
            var contracts = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Account/AccountNavigationContracts.cs");
            var options = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountViewOptions.cs");
            var host = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor");
            var app = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor");

            foreach (var expected in new[]
            {
                "AccountNavigationItem",
                "RouteKey",
                "Label",
                "Href"
            })
            {
                Assert.Contains(expected, contracts, StringComparison.Ordinal);
            }
            Assert.Contains("AccountNavigationClasses", options, StringComparison.Ordinal);
            Assert.Contains("ActiveLink", options, StringComparison.Ordinal);
            Assert.Contains("InactiveLink", options, StringComparison.Ordinal);

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

            Assert.DoesNotContain("/account/profile", options, StringComparison.Ordinal);
            Assert.DoesNotContain("/account/orders", options, StringComparison.Ordinal);
            Assert.DoesNotContain("/account/addresses", options, StringComparison.Ordinal);
            Assert.DoesNotContain("/account/change-password", options, StringComparison.Ordinal);
            Assert.Contains("rounded-3xl border border-neutral-200/70 bg-white/95", options, StringComparison.Ordinal);
            Assert.Contains("hover:bg-neutral-100", options, StringComparison.Ordinal);

            Assert.Contains("NavigationItems=\"@Context.NavigationItems\"", host, StringComparison.Ordinal);
            Assert.Contains("NavigationClasses=\"StorefrontAccountViewOptions.NavigationClasses\"", host, StringComparison.Ordinal);
            Assert.Contains("Items=\"NavigationItems\"", app, StringComparison.Ordinal);
            Assert.Contains("Classes=\"NavigationClasses\"", app, StringComparison.Ordinal);
        }

        [Fact]
        public void AccountProfileAndPasswordForms_UseHostActionsAndClassesAfterHpr10Migration()
        {
            var profile = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountProfileEditor.razor");
            var password = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountChangePasswordForm.razor");
            var accountController = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/Account/StorefrontBrowserAccountController.cs");
            var behavior = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Account/StorefrontAccountFormBehavior.cs");
            var options = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountViewOptions.cs");
            var host = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor");
            var app = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor");

            foreach (var expected in new[]
            {
                "StorefrontAccountProfileActionDescriptor",
                "LoadProfileRoute",
                "SaveProfileRoute",
                "StorefrontAccountPasswordActionDescriptor",
                "ChangePasswordRoute"
            })
            {
                Assert.Contains(expected, behavior, StringComparison.Ordinal);
            }
            Assert.Contains("StorefrontAccountFormClasses", options, StringComparison.Ordinal);
            Assert.Contains("ProfileForm", options, StringComparison.Ordinal);
            Assert.Contains("PasswordForm", options, StringComparison.Ordinal);
            Assert.Contains("SubmitButton", options, StringComparison.Ordinal);

            Assert.Contains("GetAsync<StorefrontBrowserCustomerProfile>(_profileActions.LoadProfileRoute", accountController, StringComparison.Ordinal);
            Assert.Contains("PutJsonAsync<StorefrontBrowserCustomerProfileUpdateRequest, StorefrontBrowserCustomerProfile>", accountController, StringComparison.Ordinal);
            Assert.Contains("PostJsonAsync<object, StorefrontBrowserAccountCommandResult>", accountController, StringComparison.Ordinal);
            Assert.Contains("_passwordActions.ChangePasswordRoute", accountController, StringComparison.Ordinal);
            Assert.Contains("class=\"@Classes.", profile + password, StringComparison.Ordinal);
            Assert.Contains("Passwords do not match.", accountController, StringComparison.Ordinal);
            Assert.Contains("AccountController.SaveProfileAsync()", profile, StringComparison.Ordinal);
            Assert.Contains("AccountController.ChangePasswordAsync()", password, StringComparison.Ordinal);

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

            Assert.DoesNotContain("StorefrontAccountProfileActionDescriptor", options, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontAccountPasswordActionDescriptor", options, StringComparison.Ordinal);
            Assert.DoesNotContain("\"/api/account", options, StringComparison.Ordinal);
            Assert.Contains("ProfileForm = \"grid max-w-3xl gap-5 sm:grid-cols-2\"", options, StringComparison.Ordinal);
            Assert.Contains("SubmitButton = \"inline-flex items-center rounded bg-amber-500", options, StringComparison.Ordinal);

            Assert.Contains("ProfileActions=\"@Context.ProfileActions\"", host, StringComparison.Ordinal);
            Assert.Contains("PasswordActions=\"@Context.PasswordActions\"", host, StringComparison.Ordinal);
            Assert.Contains("AccountFormClasses=\"StorefrontAccountViewOptions.FormClasses\"", host, StringComparison.Ordinal);
            Assert.Contains("nameof(StorefrontAccountProfileEditor.Actions)", app, StringComparison.Ordinal);
            Assert.Contains("nameof(StorefrontAccountChangePasswordForm.Actions)", app, StringComparison.Ordinal);
            Assert.Contains("nameof(StorefrontAccountProfileEditor.Classes)", app, StringComparison.Ordinal);
            Assert.Contains("nameof(StorefrontAccountChangePasswordForm.Classes)", app, StringComparison.Ordinal);
        }

        [Fact]
        public void AccountAddressBook_UsesHostActionsAndClassesAfterHpr11Migration()
        {
            var addresses = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountAddressBook.razor");
            var accountController = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/Account/StorefrontBrowserAccountController.cs");
            var behavior = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Account/StorefrontAccountFormBehavior.cs");
            var options = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountViewOptions.cs");
            var host = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor");
            var app = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor");

            foreach (var expected in new[]
            {
                "StorefrontAccountAddressActionDescriptor",
                "CurrentAddressesRoute",
                "CreateAddressRoute",
                "UpdateAddressRouteTemplate",
                "DeleteAddressRouteTemplate",
                "DefaultShippingRouteTemplate",
                "DefaultBillingRouteTemplate",
                "UpdateAddressRoute(Guid addressId)"
            })
            {
                Assert.Contains(expected, behavior, StringComparison.Ordinal);
            }
            Assert.Contains("StorefrontAccountAddressBookClasses", options, StringComparison.Ordinal);
            Assert.Contains("CompactWideField", options, StringComparison.Ordinal);
            Assert.Contains("FullWideField", options, StringComparison.Ordinal);

            Assert.Contains("GetAsync<IReadOnlyList<StorefrontBrowserCustomerAddress>>(_addressActions.CurrentAddressesRoute", accountController, StringComparison.Ordinal);
            Assert.Contains("PostJsonAsync<StorefrontBrowserCustomerAddressRequest, StorefrontBrowserCustomerAddress>", accountController, StringComparison.Ordinal);
            Assert.Contains("_addressActions.CreateAddressRoute", accountController, StringComparison.Ordinal);
            Assert.Contains("_addressActions.UpdateAddressRoute(addressId)", accountController, StringComparison.Ordinal);
            Assert.Contains("_addressActions.DeleteAddressRoute(addressId)", accountController, StringComparison.Ordinal);
            Assert.Contains("_addressActions.DefaultShippingRoute(addressId)", accountController, StringComparison.Ordinal);
            Assert.Contains("_addressActions.DefaultBillingRoute(addressId)", accountController, StringComparison.Ordinal);
            Assert.Contains("AccountController.CreateAddressAsync()", addresses, StringComparison.Ordinal);
            Assert.Contains("AccountController.UpdateAddressAsync(addressId)", addresses, StringComparison.Ordinal);
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

            Assert.DoesNotContain("StorefrontAccountAddressActionDescriptor", options, StringComparison.Ordinal);
            Assert.Contains("AddressClasses", options, StringComparison.Ordinal);
            Assert.DoesNotContain("\"/api/account", options, StringComparison.Ordinal);
            Assert.Contains("ListGrid = \"grid gap-4 xl:grid-cols-2\"", options, StringComparison.Ordinal);

            Assert.Contains("AddressActions=\"@Context.AddressActions\"", host, StringComparison.Ordinal);
            Assert.Contains("AddressClasses=\"StorefrontAccountViewOptions.AddressClasses\"", host, StringComparison.Ordinal);
            Assert.Contains("nameof(StorefrontAccountAddressBook.Actions)", app, StringComparison.Ordinal);
            Assert.Contains("nameof(StorefrontAccountAddressBook.Classes)", app, StringComparison.Ordinal);
        }

        [Fact]
        public void AccountOrders_UseHostActionsAndClassesAfterHpr12Migration()
        {
            var orderList = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountOrderList.razor");
            var orderDetail = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountOrderDetail.razor");
            var accountController = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/Account/StorefrontBrowserAccountController.cs");
            var behavior = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Account/StorefrontAccountFormBehavior.cs");
            var options = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountViewOptions.cs");
            var host = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor");
            var app = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor");

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
                "OrderDetailHref(string orderReference)"
            })
            {
                Assert.Contains(expected, behavior, StringComparison.Ordinal);
            }
            Assert.Contains("StorefrontAccountOrderListClasses", options, StringComparison.Ordinal);
            Assert.Contains("StorefrontAccountOrderDetailClasses", options, StringComparison.Ordinal);

            Assert.Contains("GetAsync<StorefrontBrowserAccountOrderList>(_orderActions.OrderListRoute(_ordersPageNumber)", accountController, StringComparison.Ordinal);
            Assert.Contains("href=\"@Actions.OrderDetailHref(order.Reference)\"", orderList, StringComparison.Ordinal);
            Assert.Contains("_orderActions.ReceiptRoute(_orderReference)", accountController, StringComparison.Ordinal);
            Assert.Contains("_orderActions.OrderDetailRoute(_orderReference)", accountController, StringComparison.Ordinal);
            Assert.Contains("GetAsync<StorefrontBrowserAccountOrderDetail>(route", accountController, StringComparison.Ordinal);
            Assert.Contains("AccountController.HydrateOrdersAsync()", orderList, StringComparison.Ordinal);
            Assert.Contains("AccountController.HydrateOrderDetailAsync()", orderDetail, StringComparison.Ordinal);
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

            Assert.DoesNotContain("StorefrontAccountOrderActionDescriptor", options, StringComparison.Ordinal);
            Assert.DoesNotContain("\"/api/account", options, StringComparison.Ordinal);
            Assert.DoesNotContain("\"/account/orders", options, StringComparison.Ordinal);
            Assert.Contains("OrderListClasses", options, StringComparison.Ordinal);
            Assert.Contains("OrderDetailClasses", options, StringComparison.Ordinal);

            Assert.Contains("OrderActions=\"@Context.OrderActions\"", host, StringComparison.Ordinal);
            Assert.Contains("OrderListClasses=\"StorefrontAccountViewOptions.OrderListClasses\"", host, StringComparison.Ordinal);
            Assert.Contains("OrderDetailClasses=\"StorefrontAccountViewOptions.OrderDetailClasses\"", host, StringComparison.Ordinal);
            Assert.Contains("nameof(StorefrontAccountOrderList.Actions)", app, StringComparison.Ordinal);
            Assert.Contains("nameof(StorefrontAccountOrderDetail.Actions)", app, StringComparison.Ordinal);
            Assert.Contains("nameof(StorefrontAccountOrderList.Classes)", app, StringComparison.Ordinal);
            Assert.Contains("nameof(StorefrontAccountOrderDetail.Classes)", app, StringComparison.Ordinal);
        }

        [Fact]
        public void AccountNavigationItems_AreProvidedByPresentationContextForWasmHydration()
        {
            var options = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountViewOptions.cs");
            var host = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor");
            var app = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor");

            Assert.DoesNotContain("NavigationItems", options, StringComparison.Ordinal);
            Assert.DoesNotContain("public static IReadOnlyList<AccountNavigationItem> NavigationItems", options, StringComparison.Ordinal);
            Assert.Contains("NavigationItems=\"@Context.NavigationItems\"", host, StringComparison.Ordinal);
            Assert.Contains("IReadOnlyList<AccountNavigationItem> NavigationItems", app, StringComparison.Ordinal);
        }

        [Fact]
        public void AccountApp_UsesHostShellClassesAfterHpr13Migration()
        {
            var app = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor");
            var options = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountViewOptions.cs");
            var host = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor");

            Assert.Contains("StorefrontAccountShellClasses", options, StringComparison.Ordinal);
            Assert.Contains("ShellClasses.Section", app, StringComparison.Ordinal);
            Assert.Contains("ShellClasses.Layout", app, StringComparison.Ordinal);
            Assert.Contains("ShellClasses.ContentArticle", app, StringComparison.Ordinal);
            Assert.Contains("ShellClasses.UnknownAlert", app, StringComparison.Ordinal);
            Assert.Contains("ShellClasses=\"StorefrontAccountViewOptions.ShellClasses\"", host, StringComparison.Ordinal);
            Assert.Contains("Section = \"mx-auto max-w-7xl px-4 pb-12 pt-10 sm:px-6 lg:px-8\"", options, StringComparison.Ordinal);
            Assert.Contains("ContentArticle = \"rounded-3xl border border-neutral-200/70 bg-white/95 shadow-lg\"", options, StringComparison.Ordinal);
            Assert.Contains("UnknownAlert = \"rounded-2xl border border-rose-200", options, StringComparison.Ordinal);

            var accountRouteContract = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Account/AccountRouteDescriptor.cs");

            foreach (var expectedRoute in new[]
            {
                "AccountRouteParser.Resolve(Path, RouteDescriptor)",
                "RouteDescriptor=\"@Context.RouteDescriptor\""
            })
            {
                Assert.Contains(expectedRoute, app + host, StringComparison.Ordinal);
            }

            foreach (var expectedRouteContract in new[]
            {
                "ProfileSegment",
                "AddressesSegment",
                "OrdersSegment",
                "ChangePasswordSegment",
                "ReceiptSegment",
                "Uri.UnescapeDataString(segments[1])",
                "AccountRouteKind.OrderDetail"
            })
            {
                Assert.Contains(expectedRouteContract, accountRouteContract, StringComparison.Ordinal);
            }

            Assert.DoesNotContain("string.Equals(normalized, \"profile\"", app, StringComparison.Ordinal);

            Assert.Contains("StorefrontAccountProfileEditor", app, StringComparison.Ordinal);
            Assert.Contains("StorefrontAccountAddressBook", app, StringComparison.Ordinal);
            Assert.Contains("StorefrontAccountOrderList", app, StringComparison.Ordinal);
            Assert.Contains("StorefrontAccountOrderDetail", app, StringComparison.Ordinal);
            Assert.Contains("StorefrontAccountChangePasswordForm", app, StringComparison.Ordinal);
            Assert.DoesNotContain("mx-auto", app, StringComparison.Ordinal);
            Assert.DoesNotContain("max-w-", app, StringComparison.Ordinal);
            Assert.DoesNotContain("rounded", app, StringComparison.Ordinal);
            Assert.DoesNotContain("bg-white", app, StringComparison.Ordinal);
            Assert.DoesNotContain("bg-rose-", app, StringComparison.Ordinal);
            Assert.DoesNotContain("text-neutral-", app, StringComparison.Ordinal);
            Assert.DoesNotContain("sm:", app, StringComparison.Ordinal);
            Assert.DoesNotContain("lg:", app, StringComparison.Ordinal);
        }

        [Fact]
        public void BrowserContracts_RemainDataOnlyAfterRuntimeCutover()
        {
            var browserRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Browser");
            var browserSource = string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(browserRoot, "*.cs", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
            var browserReadme = ReadRepositoryFile(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Browser/README.md");
            Assert.Contains("same-origin BFF endpoints", browserReadme, StringComparison.Ordinal);
            Assert.Contains("Visual ownership stays with the host storefront project", browserReadme, StringComparison.Ordinal);
            Assert.Contains("API clients, antiforgery readers, mutation orchestration", browserReadme, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontLocalApiClient", browserSource, StringComparison.Ordinal);
            Assert.DoesNotContain("IStorefrontAntiforgeryTokenReader", browserSource, StringComparison.Ordinal);

            foreach (var visualToken in new[]
            {
                "bg-",
                "text-neutral-",
                "text-rose-",
                "rounded",
                "shadow-",
                "max-w-",
                "grid-cols-",
                "hover:"
            })
            {
                Assert.DoesNotContain(visualToken, browserSource, StringComparison.Ordinal);
            }

            foreach (var forbiddenBrowserDependency in new[]
            {
                "https://",
                "http://",
                "CommerceNode",
                "ControlPlane",
                "NodeSecret",
                "accessToken",
                "refreshToken",
                "api/storefront/stores"
            })
            {
                Assert.DoesNotContain(forbiddenBrowserDependency, browserSource, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void StarterGeneratedGuidance_DefinesVisualOwnershipAfterHpr15Alignment()
        {
            var builderArchitecture = ReadRepositoryFile("docs/architecture/11-storefront-builder.md");
            var folderGuide = ReadRepositoryFile("docs/architecture/05-project-and-folder-guide.md");
            var visualReference = ReadRepositoryFile("docs/visual-reverse-engineering-skill/reference.md");
            var howTo = ReadRepositoryFile("docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md");
            var combinedDocs = string.Join(Environment.NewLine, builderArchitecture, folderGuide, visualReference, howTo);

            foreach (var expected in new[]
            {
                "Use `BlazorShop.Storefront.Components` only for reusable browser-safe contracts/headless behavior",
                "Starter owns its neutral visual templates",
                "must not copy Storefront V2 visual components",
                "`BlazorShop.Storefront.{Name}` owns generated markup",
                "generated CSS",
                "must not use Storefront V2 visual markup as their presentation source",
                "StorefrontBuilder may replace product card, grid, gallery, purchase, cart, checkout, and account visual templates",
                "without changing shared behavior contracts"
            })
            {
                Assert.Contains(expected, combinedDocs, StringComparison.Ordinal);
            }
        }

        private static string[] EnumerateComponentContractFiles(string searchPattern)
        {
            var contractRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts");

            return Directory
                .EnumerateFiles(contractRoot, searchPattern, SearchOption.AllDirectories)
                .Where(path => !string.Equals(Path.GetFileName(path), "README.md", StringComparison.OrdinalIgnoreCase))
                .Select(path => Path.GetRelativePath(contractRoot, path).Replace('\\', '/'))
                .Order(StringComparer.Ordinal)
                .ToArray();
        }

        private static string ReadComponentLayerSource(string layer)
        {
            var root = RepositoryPath($"BlazorShop.PresentationV2/BlazorShop.Storefront.Components/{layer}");
            if (!Directory.Exists(root))
            {
                return string.Empty;
            }

            return string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                    .Where(path => Path.GetExtension(path) is ".cs" or ".razor")
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
        }

        private static string ReadSourceTree(string root)
        {
            if (!Directory.Exists(root))
            {
                return string.Empty;
            }

            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".cs",
                ".csproj",
                ".json",
                ".md",
                ".mjs",
                ".props",
                ".ps1",
                ".razor",
                ".yaml",
                ".yml"
            };

            return string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                    .Where(path => allowedExtensions.Contains(Path.GetExtension(path)))
                    .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(RepositoryPath(relativePath));
        }

        private static int CountOccurrences(string value, string search)
        {
            var count = 0;
            var index = 0;
            while ((index = value.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += search.Length;
            }

            return count;
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
