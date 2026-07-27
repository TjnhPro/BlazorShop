namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontBrandingMarkupTests
    {
        [Fact]
        public void StorefrontHeader_ConsumesDisplayContextAndRendersLogo()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontHeader.razor");

            Assert.Contains("@inject IStorefrontDisplayContextProvider DisplayContextProvider", markup);
            Assert.Contains("@inject IStorefrontPageNavigationProvider PageNavigationProvider", markup);
            Assert.Contains("BrandLogoUrl", markup);
            Assert.Contains("bs-storefront-header__brand-logo", markup);
            Assert.Contains("DisplayContextProvider.GetAsync()", markup);
            Assert.Contains("StorefrontPageContentRules.Header", markup);
            Assert.DoesNotContain("StorefrontRoutes.About", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontRoutes.CustomerService", markup, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontBrandHead_RendersStoreSpecificIconsAndLanguage()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Seo/StorefrontBrandHead.razor");

            Assert.Contains("@inject IStorefrontDisplayContextProvider DisplayContextProvider", markup);
            Assert.Contains("<link rel=\"icon\" href=\"@_displayContext.FaviconUrl\" />", markup);
            Assert.Contains("<link rel=\"icon\" type=\"image/png\" href=\"@_displayContext.PngIconUrl\" />", markup);
            Assert.Contains("<link rel=\"apple-touch-icon\" href=\"@_displayContext.AppleTouchIconUrl\" />", markup);
            Assert.Contains("msapplication-TileImage", markup);
            Assert.Contains("document.documentElement.lang", markup);
            Assert.DoesNotContain("<HeadContent>", markup, StringComparison.Ordinal);
        }

        [Fact]
        public void AppHead_IncludesStorefrontBrandHeadBeforeHeadOutlet()
        {
            var appMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontApp.razor");
            var applicationHead = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontApplicationHead.razor");
            var layoutMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor");

            Assert.Contains("<StorefrontBrandHead />", applicationHead);
            Assert.Contains("<HeadOutlet />", appMarkup);
            Assert.True(
                appMarkup.IndexOf("ComponentType=\"@ViewSet.ApplicationHead\"", StringComparison.Ordinal) <
                appMarkup.IndexOf("<HeadOutlet />", StringComparison.Ordinal));
            Assert.DoesNotContain("<StorefrontBrandHead />", layoutMarkup, StringComparison.Ordinal);
            Assert.Contains("<StorefrontHeader />", layoutMarkup);
            Assert.Contains("<StorefrontFooter />", layoutMarkup);
        }

        [Fact]
        public void StorefrontCss_DefinesStableBrandLogoDimensions()
        {
            var styles = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/css/storefront.css");

            Assert.Contains(".bs-storefront-header__brand-logo", styles);
            Assert.Contains("height: 2rem;", styles);
            Assert.Contains("object-fit: contain;", styles);
        }

        [Fact]
        public void StorefrontFooter_ConsumesDisplayContextAndContactFields()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontFooter.razor");

            Assert.Contains("@inject IStorefrontDisplayContextProvider DisplayContextProvider", markup);
            Assert.Contains("@inject IStorefrontPageNavigationProvider PageNavigationProvider", markup);
            Assert.Contains("DisplayContextProvider.GetAsync()", markup);
            Assert.Contains("StorefrontPageContentRules.FooterCompany", markup);
            Assert.Contains("StorefrontPageContentRules.FooterSupport", markup);
            Assert.Contains("StorefrontPageContentRules.FooterLegal", markup);
            Assert.Contains("ContactEmail", markup);
            Assert.Contains("ContactPhone", markup);
            Assert.Contains("_displayContext.CompanyAddress", markup);
            Assert.Contains("mailto:@ContactEmail", markup);
            Assert.DoesNotContain("BLAZORSHOP", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontRoutes.About", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontRoutes.Privacy", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontRoutes.Terms", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontRoutes.CustomerService", markup, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontPricingMarkup_UsesStoreCurrencyContext()
        {
            var files = new[]
            {
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/ProductCard.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Theme/Pages/Product/V2ProductPageView.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor",
            };

            foreach (var relativePath in files)
            {
                var markup = ReadRepositoryFile(relativePath);
                Assert.DoesNotContain("EUR @", markup, StringComparison.Ordinal);
                Assert.DoesNotContain("€ @", markup, StringComparison.Ordinal);
                Assert.DoesNotContain("€ {", markup, StringComparison.Ordinal);
            }

            Assert.Contains("data-currency-code", ReadRepositoryFile(files[0]));
            Assert.Contains(
                "data-currency-code",
                ReadRepositoryFile(files[1])
                    + ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductPurchasePanel.razor"),
                StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontLocalCart_PostsCurrencyCode()
        {
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");
            var cartEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontPresentationCartEndpoints.cs");
            var cartLocalContracts = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/Contracts/StorefrontCartLocalContracts.cs");

            Assert.Contains("CurrencyCode: (button.dataset.currencyCode", script);
            Assert.Contains("CurrencyCode: payload.CurrencyCode || null", script);
            Assert.Contains("CurrencyCode = request.CurrencyCode", cartEndpoints);
            Assert.Contains("public string? CurrencyCode { get; set; }", cartLocalContracts);
        }

        [Fact]
        public void ProductPage_UsesBackendSelectionPreviewForVariantAttributes()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Theme/Pages/Product/V2ProductPageView.razor");
            var mapper = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Product/StorefrontProductPageMapper.cs");
            var purchasePanel = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductPurchasePanel.razor");
            var purchaseModels = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Product/ProductPurchasePanelModel.cs");
            var purchaseBehavior = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Product/ProductPurchaseBehavior.cs");
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");
            var cartEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontPresentationCartEndpoints.cs");
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Product/ProductPurchasePanel.razor")));

            Assert.Contains("<StorefrontProductPurchasePanel Model=\"_purchasePanel\" />", markup);
            Assert.Contains("BuildPurchasePanel", mapper, StringComparison.Ordinal);
            Assert.Contains("ProductPurchasePanelModel", purchaseModels, StringComparison.Ordinal);
            Assert.Contains("ProductPurchaseActionDescriptor", purchaseBehavior, StringComparison.Ordinal);
            Assert.Contains("ProductPurchaseSelectionState", purchaseBehavior, StringComparison.Ordinal);
            Assert.DoesNotContain("GetProduct", purchasePanel + purchaseModels, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontRoutes", purchasePanel + purchaseModels, StringComparison.Ordinal);
            Assert.Contains("data-storefront-selection-preview", purchasePanel);
            Assert.Contains("data-preview-route=\"@Actions.SelectionPreviewRoute\"", purchasePanel);
            Assert.DoesNotContain("StorefrontV2Default", purchaseBehavior, StringComparison.Ordinal);
            Assert.Contains("data-resolved-variant-id=\"@Model.ResolvedVariantId\"", purchasePanel);
            Assert.Contains("data-main-image-url=\"@Model.InitialMainImageUrl\"", purchasePanel);
            Assert.Contains("data-sku=\"@Model.InitialSku\"", purchasePanel);
            Assert.Contains("data-gtin=\"@Model.InitialGtin\"", purchasePanel);
            Assert.Contains("data-storefront-attribute-control", purchasePanel);
            Assert.Contains("data-storefront-selection-quantity", purchasePanel);
            Assert.Contains("data-storefront-selection-price", markup);
            Assert.Contains("data-storefront-selection-stock", markup);
            Assert.Contains("data-storefront-selection-sku", markup);
            Assert.Contains("data-storefront-selection-gtin", markup);
            Assert.Contains("InitialValidationMessages", purchaseModels, StringComparison.Ordinal);

            Assert.Contains("const selectionPreviewSelector", script);
            Assert.Contains("collectSelectedAttributes", script);
            Assert.Contains("SelectedAttributes: payload.SelectedAttributes || null", script);
            Assert.Contains("/api/product-selection-preview", script);
            Assert.Contains("syncGalleryMainImage(container, preview.primaryImageUrl)", script);
            Assert.Contains("container.dataset.mainImageUrl = preview.primaryImageUrl", script);

            Assert.Contains("app.MapPost(\"/api/product-selection-preview\"", cartEndpoints);
            Assert.Contains("PreviewProductSelectionAsync", cartEndpoints);
            Assert.Contains("StorefrontLocalProductSelectionPreviewResponse", cartEndpoints);
        }

        [Fact]
        public void ProductCard_RendersSellabilitySafeActions()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/ProductCard.razor");

            Assert.Contains("Product.Purchasable && QuantityOneAllowed", markup);
            Assert.Contains("Product.MinOrderQuantity <= 1", markup);
            Assert.Contains("Product.QuantityStep <= 1", markup);
            Assert.Contains("Product.ManageStock ? Math.Max(0, Product.AvailableQuantity ?? Product.Quantity) : 999999", markup);
            Assert.Contains("IsPurchasePaused", markup);
            Assert.Contains("\"purchase_disabled\" => \"Purchasing is paused.\"", markup);
            Assert.Contains("\"below_min_quantity\" => $\"Minimum order quantity is {Product.MinOrderQuantity}.\"", markup);
            Assert.Contains("View Product", markup);
            Assert.Contains("BrokenImageFallbackScript", markup);
            Assert.Contains("data:image/svg+xml", markup);
        }

        [Fact]
        public void ProductPage_RendersSellabilityAndQuantityMetadata()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Theme/Pages/Product/V2ProductPageView.razor");
            var mapper = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Product/StorefrontProductPageMapper.cs");
            var purchasePanel = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductPurchasePanel.razor");

            Assert.Contains("min=\"@Model.MinOrderQuantity\"", purchasePanel);
            Assert.Contains("max=\"@Model.MaxOrderQuantity\"", purchasePanel);
            Assert.Contains("step=\"@Model.QuantityStep\"", purchasePanel);
            Assert.Contains("value=\"@Model.MinOrderQuantity\"", purchasePanel);
            Assert.Contains("disabled=\"@(!Model.CanSubmitInitialPurchase)\"", purchasePanel);
            Assert.Contains("data-stock=\"@Model.InitialStockValue\"", purchasePanel);
            Assert.Contains("Free shipping", purchasePanel);
            Assert.Contains("@Model.DeliveryEstimateText", purchasePanel);
            Assert.Contains("BuildPurchasePanel", mapper);
            Assert.Contains("IsInitialPurchaseHardBlock", mapper);
            Assert.Contains("or \"purchase_disabled\"", mapper);
            Assert.Contains("or \"out_of_stock\"", mapper);
            Assert.Contains("product.ManageStock == false ? 999999", mapper);
        }

        [Fact]
        public void ProductPage_RendersProductImageGalleryComponent()
        {
            var page = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Theme/Pages/Product/V2ProductPageView.razor");
            var mapper = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Product/StorefrontProductPageMapper.cs");
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Product/ProductGallery.razor")));
            var galleryState = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Product/ProductGalleryState.cs");
            var v2Gallery = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductGallery.razor");
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");

            Assert.Contains("<StorefrontProductGallery Items=\"_galleryItems\" ProductName=\"@_product.Name\" />", page);
            Assert.DoesNotContain("aspect-[4/3]", page, StringComparison.Ordinal);
            Assert.Contains("BuildGalleryItems", mapper, StringComparison.Ordinal);
            Assert.Contains("product.MediaGallery", mapper, StringComparison.Ordinal);
            Assert.Contains("ProductGalleryItem", mapper, StringComparison.Ordinal);
            Assert.Contains("ProductGalleryState", galleryState);
            Assert.Contains("bs-product-gallery__main", v2Gallery);
            Assert.Contains("bs-product-gallery__thumb", v2Gallery);
            Assert.Contains("aspect-square", v2Gallery);
            Assert.Contains("data-storefront-product-gallery", v2Gallery);
            Assert.Contains("data-storefront-gallery-main-image", v2Gallery);
            Assert.Contains("data-storefront-gallery-controls", v2Gallery);
            Assert.Contains("data-storefront-gallery-prev", v2Gallery);
            Assert.Contains("data-storefront-gallery-next", v2Gallery);
            Assert.Contains("bs-product-gallery__nav-icon", v2Gallery);
            Assert.Contains("viewBox=\"0 0 24 24\"", v2Gallery);
            Assert.DoesNotContain("data-storefront-gallery-status", v2Gallery, StringComparison.Ordinal);
            Assert.DoesNotContain("Image 1 of", v2Gallery, StringComparison.Ordinal);
            Assert.Contains("data-storefront-gallery-thumb-viewport", v2Gallery);
            Assert.Contains("data-storefront-gallery-thumbnail", v2Gallery);
            Assert.Contains("bs-product-gallery__thumb-fallback", v2Gallery);
            Assert.Contains("data-storefront-gallery-thumb-fallback", v2Gallery);
            Assert.Contains("data-gallery-index=\"@index\"", v2Gallery);
            Assert.Contains("aria-selected=\"@(index == Gallery.SelectedIndex ? \"true\" : \"false\")\"", v2Gallery);
            Assert.Contains("data-storefront-gallery-prev", v2Gallery);
            Assert.Contains("disabled>", v2Gallery);
            Assert.Contains("data-[selected=true]:ring-2", v2Gallery);
            Assert.Contains("product.Image", mapper);
            Assert.Contains("Image unavailable", v2Gallery);
            Assert.Contains("BrokenImageFallbackScript", v2Gallery);
            Assert.Contains("onerror=\"@BrokenImageFallbackScript\"", v2Gallery);
            Assert.Contains("selectGalleryThumbnail", script);
            Assert.Contains("selectGalleryIndex", script);
            Assert.Contains("resolveSelectedGalleryIndex", script);
            Assert.Contains("galleryPreviousSelector", script);
            Assert.Contains("galleryNextSelector", script);
            Assert.DoesNotContain("galleryStatusSelector", script, StringComparison.Ordinal);
            Assert.DoesNotContain("Image ${selectedIndex + 1} of", script, StringComparison.Ordinal);
            Assert.Contains("galleryPlaceholderSelector", script);
            Assert.Contains("galleryThumbnailSelector", script);
            Assert.Contains("mainImage.hidden = false", script);
            Assert.Contains("placeholder.hidden = true", script);
            Assert.Contains("mainImage.src = imageUrl", script);
            Assert.Contains("mainImage.alt = selectedThumbnail.dataset.alt", script);
            Assert.Contains("thumbnail.setAttribute(\"aria-selected\"", script);
            Assert.Contains("setGalleryButtonState", script);
            Assert.Contains("showGalleryImageFallback", script);
            Assert.Contains("handleGalleryImageError", script);
            Assert.Contains("document.addEventListener(\"error\", handleGalleryImageError, true)", script);
            Assert.Contains("selectedThumbnail.scrollIntoView({ block: \"nearest\", inline: \"nearest\" })", script);
            Assert.Contains("event.key === \"ArrowLeft\"", script);
            Assert.Contains("event.key === \"ArrowRight\"", script);
            Assert.Contains("document.addEventListener(\"keydown\", handleKeyDown)", script);
        }

        [Fact]
        public void ProductGalleryCss_EnforcesSquareImageFrames()
        {
            var styles = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/css/storefront.css");

            Assert.Contains(".bs-storefront-shell [hidden]:not([hidden=\"until-found\"])", styles);
            Assert.Contains("display: none !important;", styles);
            Assert.Contains(".bs-product-gallery__main", styles);
            Assert.Contains(".bs-product-gallery__controls", styles);
            Assert.Contains(".bs-product-gallery__nav", styles);
            Assert.Contains(".bs-product-gallery__nav--prev", styles);
            Assert.Contains(".bs-product-gallery__nav--next", styles);
            Assert.Contains(".bs-product-gallery__nav-icon", styles);
            Assert.Contains(".bs-product-gallery__nav:focus-visible", styles);
            Assert.Contains(".bs-product-gallery__thumb", styles);
            Assert.Contains(".bs-product-gallery__thumb-fallback", styles);
            Assert.Contains("top: 50%;", styles);
            Assert.Contains("transform: translateY(-50%);", styles);
            Assert.Contains("left: 0.75rem;", styles);
            Assert.Contains("right: 0.75rem;", styles);
            Assert.Contains("aspect-ratio: 1 / 1;", styles);
            Assert.Contains("width: 5rem;", styles);
            Assert.Contains("height: 5rem;", styles);
            Assert.Contains("flex: 0 0 5rem;", styles);
            Assert.Contains("max-width: 5rem;", styles);
            Assert.DoesNotContain("flex: initial;", styles, StringComparison.Ordinal);
            Assert.Contains("object-fit: contain;", styles);
            Assert.Contains("overscroll-behavior-x: contain;", styles);
            Assert.Contains(".bs-product-gallery__thumb[data-selected=\"true\"]", styles);
            Assert.Contains("@media (prefers-reduced-motion: reduce)", styles);
        }

        [Fact]
        public void DealsAndNewReleases_ComposePortableFeatureComponents()
        {
            var home = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/Home.razor");
            var categoryPage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/CategoryPage.razor");
            var searchPage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/SearchPage.razor");
            var dealsPage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/TodaysDeals.razor");
            var newReleasesPage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/NewReleases.razor");
            var dealsBlock = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/StorefrontDealsSection.razor");
            var productGrid = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/StorefrontProductSummaryGrid.razor");
            var productCard = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/StorefrontProductSummaryCard.razor");
            var dealsPlacement = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Deals/DealsPlacement.cs");
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Deals/DealsBlock.razor")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Catalog/ProductSummaryGrid.razor")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Catalog/ProductSummaryCard.razor")));

            Assert.Contains("<StorefrontDealsSection Placement=\"DealsPlacement.Home\"", home, StringComparison.Ordinal);
            Assert.Contains("<StorefrontDealsSection Placement=\"DealsPlacement.DedicatedPage\"", dealsPage, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductSummaryGrid Items=\"Context.ProductSummaries\"", newReleasesPage, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductSummaryGrid Items=\"Context.ProductSummaries\"", categoryPage, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductSummaryGrid Items=\"Context.ProductSummaries\"", searchPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<ProductGrid Products=\"_products\"", categoryPage + searchPage + dealsPage + newReleasesPage, StringComparison.Ordinal);
            var catalogServices = string.Concat(
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Catalog/StorefrontHomePageService.cs"),
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Catalog/StorefrontCategoryPageService.cs"),
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Catalog/StorefrontSearchPageService.cs"),
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Catalog/StorefrontDealsPageService.cs"),
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Catalog/StorefrontNewReleasesPageService.cs"));
            Assert.Contains("StorefrontProductSummaryMapper.ToProductSummary", catalogServices, StringComparison.Ordinal);

            Assert.Contains("data-storefront-deals-block", dealsBlock, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductSummaryGrid", dealsBlock, StringComparison.Ordinal);
            Assert.Contains("data-storefront-product-summary-grid", productGrid, StringComparison.Ordinal);
            Assert.Contains("data-storefront-product-summary-card", productCard, StringComparison.Ordinal);
            Assert.Contains("data-storefront-add-to-cart", productCard, StringComparison.Ordinal);
            Assert.Contains("data-unit-price=\"@Item.UnitPriceValue\"", productCard, StringComparison.Ordinal);
            Assert.Contains("data-currency-code=\"@Item.CurrencyCode\"", productCard, StringComparison.Ordinal);
            Assert.Contains("ProductDetailFooter", dealsPlacement, StringComparison.Ordinal);
            Assert.DoesNotContain("IStorefront", dealsBlock + productGrid + productCard, StringComparison.Ordinal);
        }

        [Fact]
        public void AccountOrderDetailPage_PassesRouteReferenceToBrowserComponent()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor");

            Assert.Contains("nameof(StorefrontAccountOrderDetail.OrderReference)", markup);
            Assert.Contains("ActiveRoute.OrderReference", markup);
            Assert.DoesNotContain("OrderReference=\"OrderReference\"", markup, StringComparison.Ordinal);
        }

        [Fact]
        public void CheckoutPage_RendersAddressLookupAndSavedAddressSelection()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor");
            var codeBehind = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor.cs");
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");
            var apiRoutes = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiRoutes.cs");

            Assert.Contains("data-storefront-address-select", markup);
            Assert.Contains("data-storefront-manual-address", markup);
            Assert.Contains("data-storefront-manual-address-field", markup);
            Assert.Contains("GetAddressCountriesAsync", codeBehind);
            Assert.Contains("GetAddressStatesAsync", codeBehind);
            Assert.Contains("GetCustomerAddressesAsync", codeBehind);
            Assert.Contains("GetAddressConfigurationAsync", codeBehind);
            Assert.Contains("StorefrontAddressCountriesRoute", apiRoutes);
            Assert.Contains("StorefrontAddressConfigurationRoute", apiRoutes);
            Assert.Contains("customer/addresses", apiRoutes);
            Assert.Contains("syncManualAddressFields", script);
            Assert.Contains("field.disabled = useSavedAddress", script);
        }

        private static string ReadStorefrontLocalEndpointSupportSource()
        {
            var root = FindStorefrontSupportRepositoryRoot();
            var endpointDirectory = Path.Combine(root, "BlazorShop.PresentationV2", "BlazorShop.Storefront.V2", "Endpoints");
            return string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(endpointDirectory, "StorefrontLocalEndpointSupport*.cs")
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
        }
        private static string FindStorefrontSupportRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Unable to locate BlazorShop.sln from the test output directory.");
        }
        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));
        }

        private static string RepositoryPath(string relativePath)
        {
            return Path.Combine(FindRepositoryRoot(), relativePath);
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Unable to locate BlazorShop.sln from the test output directory.");
        }
    }
}
