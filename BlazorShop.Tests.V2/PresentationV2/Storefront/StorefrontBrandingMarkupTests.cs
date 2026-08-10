namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontBrandingMarkupTests
    {
        [Fact]
        public void StorefrontHeader_ConsumesDisplayContextAndRendersLogo()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontHeader.razor");

            Assert.DoesNotContain("@inject", markup, StringComparison.Ordinal);
            Assert.Contains("public StorefrontHeaderContext Context", markup);
            Assert.Contains("<StorefrontBrandLogo Context=\"BrandLogoContext\" Classes=\"BrandLogoClasses\" />", markup);
            Assert.Contains("private StorefrontBrandLogoContext BrandLogoContext", markup);
            Assert.Contains("private static StorefrontBrandLogoClasses BrandLogoClasses", markup);
            Assert.Contains("Context.Brand.LogoUrl", markup);
            Assert.Contains("bs-storefront-header__brand-logo", markup);
            Assert.Contains("Context.Navigation.HeaderLinks", markup);
            Assert.Contains("Context.Search.Categories", markup);
            Assert.Contains("Context.Currency.ShowSelector", markup);
            Assert.Contains("<StorefrontCurrencyPreferenceForm Context=\"Context.Currency\"", markup);
            Assert.Contains("<StorefrontAccountMenu Context=\"Context.AccountMenu\" />", markup);
            Assert.DoesNotContain("OnInitializedAsync", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontRoutes.About", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontRoutes.CustomerService", markup, StringComparison.Ordinal);
            Assert.Equal(2, CountOccurrences(markup, "<StorefrontBrandLogo Context=\"BrandLogoContext\" Classes=\"BrandLogoClasses\" />"));
        }

        [Fact]
        public void StorefrontBrandHead_RendersNonIconStorefrontMetadataOnly()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Seo/StorefrontBrandHead.razor");
            var iconHead = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Head/StorefrontIconHead.razor");

            Assert.DoesNotContain("@inject", markup, StringComparison.Ordinal);
            Assert.Contains("public StorefrontDisplayContext DisplayContext", markup);
            Assert.Contains("<meta name=\"bs-storefront-language\" content=\"@DisplayContext.LanguageCode\" />", markup);
            Assert.DoesNotContain("rel=\"icon\"", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("apple-touch-icon", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("msapplication-TileImage", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("msapplication-TileColor", markup, StringComparison.Ordinal);
            Assert.Contains("<link rel=\"icon\" href=\"@DisplayContext.FaviconUrl\" />", iconHead, StringComparison.Ordinal);
            Assert.Contains("<link rel=\"apple-touch-icon\" href=\"@DisplayContext.AppleTouchIconUrl\" />", iconHead, StringComparison.Ordinal);
            Assert.Contains("msapplication-TileImage", iconHead, StringComparison.Ordinal);
            Assert.DoesNotContain("document.documentElement.lang", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("<HeadContent>", markup, StringComparison.Ordinal);
        }

        [Fact]
        public void AppHead_IncludesStorefrontBrandHeadBeforeHeadOutlet()
        {
            var appMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontApp.razor");
            var applicationHead = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontApplicationHead.razor");
            var layoutMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor");

            Assert.Contains("<StorefrontBrandHead DisplayContext=\"Context.Display\" />", applicationHead);
            Assert.Contains("<StorefrontIconHead DisplayContext=\"Context.Display\" />", applicationHead);
            Assert.Contains("<HeadOutlet />", appMarkup);
            Assert.True(
                appMarkup.IndexOf("<StorefrontFoundationApplicationHead />", StringComparison.Ordinal) <
                appMarkup.IndexOf("<HeadOutlet />", StringComparison.Ordinal));
            Assert.DoesNotContain("<StorefrontBrandHead", layoutMarkup, StringComparison.Ordinal);
            Assert.Contains("<StorefrontHeader Context=\"Context.Header\" />", layoutMarkup);
            Assert.Contains("<StorefrontFooter Context=\"Context.Footer\" />", layoutMarkup);
        }

        [Fact]
        public void StorefrontV2_DoesNotOwnHardCodedOrBrandIconLinks()
        {
            var applicationHead = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontApplicationHead.razor");
            var brandHead = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Seo/StorefrontBrandHead.razor");

            Assert.DoesNotContain("<link rel=\"icon\" type=\"image/png\" href=\"icon-192.png\" />", applicationHead, StringComparison.Ordinal);
            Assert.DoesNotContain("rel=\"icon\"", brandHead, StringComparison.Ordinal);
            Assert.Contains("<StorefrontIconHead DisplayContext=\"Context.Display\" />", applicationHead, StringComparison.Ordinal);
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

            Assert.DoesNotContain("@inject", markup, StringComparison.Ordinal);
            Assert.Contains("public StorefrontFooterContext Context", markup);
            Assert.Contains("Context.Navigation.FooterCompanyLinks", markup);
            Assert.Contains("Context.Navigation.FooterSupportLinks", markup);
            Assert.Contains("Context.Navigation.FooterLegalLinks", markup);
            Assert.Contains("Context.ContactEmail", markup);
            Assert.Contains("Context.ContactPhone", markup);
            Assert.Contains("Context.CompanyAddress", markup);
            Assert.Contains("mailto:@Context.ContactEmail", markup);
            Assert.DoesNotContain("OnInitializedAsync", markup, StringComparison.Ordinal);
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
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Product/V2ProductPageView.razor",
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

            Assert.Contains(
                "data-currency-code",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/Catalog/StorefrontProductSummaryPurchaseActions.razor"),
                StringComparison.Ordinal);
            Assert.Contains(
                "data-currency-code",
                ReadRepositoryFile(files[1])
                    + ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductPurchasePanel.razor"),
                StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontLocalCart_PostsCurrencyCode()
        {
            var applicationScript = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js");
            var cartEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontPresentationCartEndpoints.cs");
            var cartLocalContracts = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/Contracts/StorefrontCartLocalContracts.cs");

            Assert.Contains("CurrencyCode: descriptor.currencyCode || null", applicationScript);
            Assert.Contains("CurrencyCode: selection.currencyCode || null", applicationScript);
            Assert.Contains("cartApiRoute = \"/api/cart\"", applicationScript);
            Assert.Contains("CurrencyCode = request.CurrencyCode", cartEndpoints);
            Assert.Contains("public string? CurrencyCode { get; set; }", cartLocalContracts);
        }

        [Fact]
        public void ProductPage_UsesBackendSelectionPreviewForVariantAttributes()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Product/V2ProductPageView.razor");
            var mapper = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Product/StorefrontProductPageMapper.cs");
            var purchasePanel = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductPurchasePanel.razor");
            var pricingDisplay = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Product/StorefrontProductPricing.razor");
            var availabilityDisplay = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Product/StorefrontProductAvailability.razor");
            var purchaseModels = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Product/ProductPurchasePanelModel.cs");
            var purchaseBehavior = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Product/ProductPurchaseBehavior.cs");
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");
            var cartEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontPresentationCartEndpoints.cs");
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Product/ProductPurchasePanel.razor")));

            Assert.Contains("<StorefrontProductPurchasePanel Model=\"_purchasePanel\" Actions=\"Context.PurchaseActions\" />", markup);
            Assert.Contains("BuildPurchasePanel", mapper, StringComparison.Ordinal);
            Assert.Contains("ProductPurchasePanelModel", purchaseModels, StringComparison.Ordinal);
            Assert.Contains("ProductPurchaseActionDescriptor", purchaseBehavior, StringComparison.Ordinal);
            Assert.Contains("ProductPurchaseSelectionState", purchaseBehavior, StringComparison.Ordinal);
            Assert.DoesNotContain("GetProduct", purchasePanel + purchaseModels, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontRoutes", purchasePanel + purchaseModels, StringComparison.Ordinal);
            Assert.Contains("data-storefront-product-purchase", purchasePanel);
            Assert.Contains("data-selection-preview-route=\"@Actions.SelectionPreviewRoute\"", purchasePanel);
            Assert.DoesNotContain("StorefrontV2Default", purchaseBehavior, StringComparison.Ordinal);
            Assert.Contains("data-resolved-variant-id=\"@Model.ResolvedVariantId\"", purchasePanel);
            Assert.Contains("data-main-image-url=\"@Model.InitialMainImageUrl\"", purchasePanel);
            Assert.Contains("data-sku=\"@Model.InitialSku\"", purchasePanel);
            Assert.Contains("data-gtin=\"@Model.InitialGtin\"", purchasePanel);
            Assert.Contains("data-storefront-purchase-attribute", purchasePanel);
            Assert.Contains("checked=\"@value.IsSelected\"", purchasePanel);
            Assert.Contains("selected=\"@value.IsSelected\"", purchasePanel);
            Assert.DoesNotContain("ShouldSelectOptionValue", purchasePanel, StringComparison.Ordinal);
            Assert.DoesNotContain("values[0]", purchasePanel, StringComparison.Ordinal);
            Assert.Contains("bool IsSelected", ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Product/ProductPurchaseOptionValueItem.cs"), StringComparison.Ordinal);
            Assert.Contains("data-storefront-purchase-quantity", purchasePanel);
            Assert.Contains("<StorefrontProductPricing", markup, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductAvailability", markup, StringComparison.Ordinal);
            Assert.Contains("data-storefront-selection-price", pricingDisplay);
            Assert.Contains("data-storefront-selection-stock", availabilityDisplay);
            Assert.Contains("data-storefront-selection-sku", availabilityDisplay);
            Assert.Contains("data-storefront-selection-gtin", availabilityDisplay);
            Assert.Contains("InitialValidationMessages", purchaseModels, StringComparison.Ordinal);

            Assert.Contains("storefront:product-purchase:selection-changed", script);
            Assert.Contains("applySelectionVisual", script);
            Assert.Contains("readSelectedAttributes", ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js"));
            Assert.Contains("SelectedAttributes: descriptor.selectedAttributes.length > 0 ? descriptor.selectedAttributes : null", ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js"));
            Assert.Contains("productSelection.preview(descriptor.previewRoute, payload)", ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js"));
            Assert.Contains("/api/product-selection-preview", ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js"));
            Assert.Contains("syncGalleryMainImage(rootElement, selection.mainImageUrl)", script);
            Assert.Contains("rootElement.dataset.mainImageUrl = selection.mainImageUrl", script);

            Assert.Contains("app.MapPost(\"/api/product-selection-preview\"", cartEndpoints);
            Assert.Contains("PreviewProductSelectionAsync", cartEndpoints);
            Assert.Contains("StorefrontLocalProductSelectionPreviewResponse", cartEndpoints);
        }

        [Fact]
        public void ProductCard_RendersSellabilitySafeActions()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/ProductCard.razor");
            var summaryCard = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/Catalog/StorefrontProductSummaryCard.razor");
            var summaryImage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/Catalog/StorefrontProductSummaryImage.razor");
            var summaryActions = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/Catalog/StorefrontProductSummaryPurchaseActions.razor");
            var visuals = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/ProductSummaryCardVisuals.cs");
            var mapper = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Catalog/StorefrontProductSummaryMapper.cs");

            Assert.Contains("<StorefrontProductSummaryCard", markup);
            Assert.Contains("Labels=\"ProductSummaryCardVisuals.Labels\"", markup);
            Assert.Contains("Classes=\"ProductSummaryCardVisuals.Classes\"", markup);
            Assert.DoesNotContain("@inject", markup, StringComparison.Ordinal);
            Assert.Contains("product.Purchasable && !product.PurchaseBlockReasons.Any(IsDirectAddHardBlock) && QuantityOneAllowed(product)", mapper);
            Assert.Contains("product.MinOrderQuantity <= 1", mapper);
            Assert.Contains("product.QuantityStep <= 1", mapper);
            Assert.Contains("product.ManageStock ? Math.Max(0, product.AvailableQuantity ?? product.Quantity) : 999999", mapper);
            Assert.Contains("\"purchase_disabled\" => \"Purchasing is paused.\"", mapper);
            Assert.Contains("\"below_min_quantity\" => $\"Minimum order quantity is {product.MinOrderQuantity}.\"", mapper);
            Assert.Contains("ViewProduct", summaryCard + summaryActions);
            Assert.Contains("BrokenImageFallbackScript", summaryImage);
            Assert.Contains("data:image/svg+xml", summaryImage);
            Assert.Contains("ViewProduct: \"View Product\"", visuals);
        }

        [Fact]
        public void ProductPage_RendersSellabilityAndQuantityMetadata()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Product/V2ProductPageView.razor");
            var mapper = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Product/StorefrontProductPageMapper.cs");
            var purchasePanel = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductPurchasePanel.razor");

            Assert.Contains("min=\"@Model.MinOrderQuantity\"", purchasePanel);
            Assert.Contains("max=\"@Model.MaxOrderQuantity\"", purchasePanel);
            Assert.Contains("step=\"@Model.QuantityStep\"", purchasePanel);
            Assert.Contains("value=\"@Model.MinOrderQuantity\"", purchasePanel);
            Assert.Contains("disabled=\"@(!Model.CanSubmitInitialPurchase)\"", purchasePanel);
            Assert.DoesNotContain("data-stock=", purchasePanel, StringComparison.Ordinal);
            Assert.Contains("Free shipping", purchasePanel);
            Assert.Contains("@Model.DeliveryEstimateText", purchasePanel);
            Assert.Contains("BuildPurchasePanel", mapper);
            Assert.Contains("IsInitialPurchaseHardBlock", mapper);
            Assert.Contains("or \"purchase_disabled\"", mapper);
            Assert.Contains("or \"out_of_stock\"", mapper);
            Assert.Contains("return product.ManageStock == false", mapper);
            Assert.Contains("? 999999", mapper);
        }

        [Fact]
        public void ProductPage_RendersProductImageGalleryComponent()
        {
            var page = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Product/V2ProductPageView.razor");
            var mapper = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Product/StorefrontProductPageMapper.cs");
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Product/ProductGallery.razor")));
            var galleryState = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Product/ProductGalleryState.cs");
            var primitiveGallery = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/Product/StorefrontProductGallery.razor");
            var v2GalleryVisuals = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/ProductGalleryVisuals.cs");
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");

            Assert.Contains("<StorefrontProductGallery", page, StringComparison.Ordinal);
            Assert.Contains("Labels=\"ProductGalleryVisuals.Labels\"", page, StringComparison.Ordinal);
            Assert.Contains("Classes=\"ProductGalleryVisuals.Classes\"", page, StringComparison.Ordinal);
            Assert.DoesNotContain("aspect-[4/3]", page, StringComparison.Ordinal);
            Assert.Contains("BuildGalleryItems", mapper, StringComparison.Ordinal);
            Assert.Contains("product.MediaGallery", mapper, StringComparison.Ordinal);
            Assert.Contains("ProductGalleryItem", mapper, StringComparison.Ordinal);
            Assert.Contains("ProductGalleryState", galleryState);
            Assert.Contains("bs-product-gallery__main", v2GalleryVisuals);
            Assert.Contains("bs-product-gallery__thumb", v2GalleryVisuals);
            Assert.Contains("aspect-square", v2GalleryVisuals);
            Assert.Contains("data-storefront-product-gallery", primitiveGallery);
            Assert.Contains("data-storefront-gallery-main-image", primitiveGallery);
            Assert.Contains("data-storefront-gallery-controls", primitiveGallery);
            Assert.Contains("data-storefront-gallery-prev", primitiveGallery);
            Assert.Contains("data-storefront-gallery-next", primitiveGallery);
            Assert.Contains("bs-product-gallery__nav-icon", v2GalleryVisuals);
            Assert.Contains("viewBox=\"0 0 24 24\"", primitiveGallery);
            Assert.DoesNotContain("data-storefront-gallery-status", primitiveGallery, StringComparison.Ordinal);
            Assert.DoesNotContain("Image 1 of", primitiveGallery, StringComparison.Ordinal);
            Assert.Contains("data-storefront-gallery-thumb-viewport", primitiveGallery);
            Assert.Contains("data-storefront-gallery-thumbnail", primitiveGallery);
            Assert.Contains("bs-product-gallery__thumb-fallback", v2GalleryVisuals);
            Assert.Contains("data-storefront-gallery-thumb-fallback", primitiveGallery);
            Assert.Contains("data-gallery-index=\"@index\"", primitiveGallery);
            Assert.Contains("aria-selected=\"@(index == Gallery.SelectedIndex ? \"true\" : \"false\")\"", primitiveGallery);
            Assert.Contains("data-storefront-gallery-prev", primitiveGallery);
            Assert.Contains("disabled=\"@(!Gallery.CanSelectPrevious)\"", primitiveGallery);
            Assert.Contains("disabled=\"@(!Gallery.CanSelectNext)\"", primitiveGallery);
            Assert.Contains("data-[selected=true]:ring-2", v2GalleryVisuals);
            Assert.Contains("product.Image", mapper);
            Assert.Contains("ImageUnavailableText: \"Image unavailable\"", v2GalleryVisuals);
            Assert.Contains("BrokenImageFallbackScript", primitiveGallery);
            Assert.Contains("onerror=\"@BrokenImageFallbackScript\"", primitiveGallery);
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
        public void CatalogPages_ComposePortableFeatureComponents()
        {
            var home = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/Home.razor");
            var categoryPage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/CategoryPage.razor");
            var searchPage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/SearchPage.razor");
            var discountedRailSection = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Catalog/StorefrontDiscountedProductRailSection.razor");
            var dealsBlock = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/StorefrontDealsSection.razor");
            var productGrid = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/StorefrontProductSummaryGrid.razor");
            var productCard = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/Catalog/StorefrontProductSummaryCard.razor");
            var productActions = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/Catalog/StorefrontProductSummaryPurchaseActions.razor");
            var v2Visuals = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/ProductSummaryCardVisuals.cs");
            var dealsPlacement = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Deals/DealsPlacement.cs");
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Catalog/StorefrontDealsPageService.cs")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Catalog/StorefrontNewReleasesPageService.cs")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Deals/DealsBlock.razor")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Catalog/ProductSummaryGrid.razor")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Catalog/ProductSummaryCard.razor")));

            Assert.Contains("<StorefrontDealsSection Placement=\"DealsPlacement.Home\"", home, StringComparison.Ordinal);
            Assert.Contains("<StorefrontDiscountedProductRailSection @rendermode=\"InteractiveWebAssembly\" />", home, StringComparison.Ordinal);
            Assert.DoesNotContain("<ItemTemplate", home, StringComparison.Ordinal);
            Assert.Contains("<StorefrontDiscountedProductRail Labels=\"Labels\"", discountedRailSection, StringComparison.Ordinal);
            Assert.Contains("StorefrontDiscountedProductRailActionDescriptor Action", discountedRailSection, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductSummaryCard", discountedRailSection, StringComparison.Ordinal);
            Assert.DoesNotContain("data-storefront-product-summary-card", discountedRailSection, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductSummaryGrid Items=\"Context.ProductSummaries\"", categoryPage, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductSummaryGrid Items=\"Context.ProductSummaries\"", searchPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<ProductGrid Products=\"_products\"", categoryPage + searchPage, StringComparison.Ordinal);
            var catalogServices = string.Concat(
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Catalog/StorefrontHomePageService.cs"),
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Catalog/StorefrontCategoryPageService.cs"),
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Catalog/StorefrontSearchPageService.cs"));
            Assert.Contains("StorefrontProductSummaryMapper.ToProductSummary", catalogServices, StringComparison.Ordinal);

            Assert.Contains("data-storefront-deals-block", dealsBlock, StringComparison.Ordinal);
            Assert.DoesNotContain("DedicatedPage", dealsPlacement, StringComparison.Ordinal);
            Assert.DoesNotContain("DedicatedPage", dealsBlock, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductSummaryGrid", dealsBlock, StringComparison.Ordinal);
            Assert.Contains("data-storefront-product-summary-grid", productGrid, StringComparison.Ordinal);
            Assert.Contains("data-storefront-product-summary-card", productCard, StringComparison.Ordinal);
            Assert.Contains("data-storefront-product-purchase", productActions, StringComparison.Ordinal);
            Assert.Contains("data-storefront-command=\"cart.add-line\"", productActions, StringComparison.Ordinal);
            Assert.Contains("data-storefront-product-purchase-submit", productActions, StringComparison.Ordinal);
            Assert.Contains("data-currency-code=\"@Item.CurrencyCode\"", productActions, StringComparison.Ordinal);
            Assert.Contains("ProductSummaryCardVisuals.Classes", productGrid, StringComparison.Ordinal);
            Assert.Contains("Root: \"group relative", v2Visuals, StringComparison.Ordinal);
            Assert.Contains("ProductDetailFooter", dealsPlacement, StringComparison.Ordinal);
            Assert.DoesNotContain("IStorefront", dealsBlock + productGrid + productCard + productActions, StringComparison.Ordinal);
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
        public void CheckoutPage_RendersAddressLookupThroughRuntimeFacade()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor");
            var addressFields = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Checkout/StorefrontCheckoutAddressFields.razor");
            var pageService = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Checkout/StorefrontCheckoutPageService.cs");
            var accountEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontPresentationAccountEndpoints.cs");
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");

            Assert.Contains("<StorefrontCheckoutAddressFields", markup);
            Assert.Contains("data-storefront-manual-address", addressFields);
            Assert.Contains("data-storefront-manual-address-field", addressFields);
            Assert.Contains("StorefrontCheckoutFormFieldNames.ShippingFullName", addressFields, StringComparison.Ordinal);
            Assert.Contains("StorefrontCheckoutFormFieldNames.ShippingCountryCode", addressFields, StringComparison.Ordinal);
            Assert.Contains("StorefrontCheckoutFormFieldNames.ShippingPostalCode", addressFields, StringComparison.Ordinal);
            Assert.Contains("IStorefrontRuntimeAddressFacade addressFacade", pageService);
            Assert.Contains("addressFacade.ListCountriesAsync", pageService);
            Assert.Contains("addressFacade.ListStatesAsync", pageService);
            Assert.Contains("addressFacade.GetConfigurationAsync", pageService);
            Assert.Contains("GetCustomerAddressesAsync(session.AccessToken!", accountEndpoints);
            Assert.DoesNotContain("data-storefront-address-select", markup + addressFields + script, StringComparison.Ordinal);
            Assert.DoesNotContain("manualAddressFieldSelector", script, StringComparison.Ordinal);
            Assert.DoesNotContain("syncManualAddressFields", script, StringComparison.Ordinal);
            Assert.DoesNotContain("field.disabled", script, StringComparison.Ordinal);
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

        private static int CountOccurrences(string value, string token)
        {
            var count = 0;
            var index = 0;
            while ((index = value.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += token.Length;
            }

            return count;
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
