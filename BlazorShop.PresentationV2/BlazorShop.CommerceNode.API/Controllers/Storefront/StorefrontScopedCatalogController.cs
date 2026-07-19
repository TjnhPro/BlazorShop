namespace BlazorShop.CommerceNode.API.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.Security.Claims;

    using BlazorShop.Application.CommerceNode.Addresses;
    using BlazorShop.Application.CommerceNode.Captcha;
    using ApplicationStorefrontCheckoutResult = BlazorShop.Application.DTOs.Payment.StorefrontCheckoutResult;
    using ApplicationStorefrontCheckoutPreviewResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutPreviewResult;
    using ApplicationStorefrontCheckoutReviewResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutReviewResult;
    using ApplicationStorefrontCheckoutSessionRequest = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutSessionRequest;
    using ApplicationStorefrontCheckoutSessionResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutSessionResult;
    using ApplicationStorefrontCheckoutStartRequest = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutStartRequest;
    using ApplicationStorefrontPlaceOrderResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontPlaceOrderResult;
    using IStorefrontCheckoutService = BlazorShop.Application.CommerceNode.Checkout.IStorefrontCheckoutService;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Consent;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Customers;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Orders;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Settings;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Discovery;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Application.Options;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Services.Contracts.Authentication;
    using BlazorShop.Application.Services.Contracts.Payment;
    using BlazorShop.CommerceNode.API.Configuration;
    using BlazorShop.CommerceNode.API.Contracts.Storefront;
    using BlazorShop.CommerceNode.API.Responses;
    using BlazorShop.Domain.Contracts;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.Extensions.Options;

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/catalog")]
    public sealed class StorefrontScopedCatalogController : StorefrontApiControllerBase
    {
        private readonly IPublicCatalogService publicCatalogService;
        private readonly ICommerceStoreContext storeContext;
        private readonly IStorefrontWorkingCurrencyResolver workingCurrencyResolver;
        private readonly IMoneyConversionService moneyConversionService;
        private readonly IProductSelectionResolver productSelectionResolver;

        public StorefrontScopedCatalogController(
            IPublicCatalogService publicCatalogService,
            ICommerceStoreContext storeContext,
            IStorefrontWorkingCurrencyResolver workingCurrencyResolver,
            IMoneyConversionService moneyConversionService,
            IProductSelectionResolver productSelectionResolver)
        {
            this.publicCatalogService = publicCatalogService;
            this.storeContext = storeContext;
            this.workingCurrencyResolver = workingCurrencyResolver;
            this.moneyConversionService = moneyConversionService;
            this.productSelectionResolver = productSelectionResolver;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await this.publicCatalogService.GetPublishedCategoriesAsync();
            return this.Success(
                categories.Select(category => category.ToStorefrontContract()).ToArray(),
                "Published categories loaded.");
        }

        [HttpGet("categories/tree")]
        public async Task<IActionResult> GetCategoryTree()
        {
            var categories = await this.publicCatalogService.GetPublishedCategoryTreeAsync();
            return this.Success(
                categories.Select(category => category.ToStorefrontContract()).ToArray(),
                "Published category tree loaded.");
        }

        [HttpGet("categories/{id:guid}")]
        public async Task<IActionResult> GetCategoryById(Guid id)
        {
            var category = await this.publicCatalogService.GetPublishedCategoryByIdAsync(id);
            return category is null
                ? this.Failure<StorefrontCategoryResponse>(ServiceResponseType.NotFound, "Published category was not found.")
                : this.Success(category.ToStorefrontContract(), "Published category loaded.");
        }

        [HttpGet("categories/slug/{slug}")]
        public async Task<IActionResult> GetCategoryBySlug(
            string slug,
            [FromQuery] string? currencyCode,
            CancellationToken cancellationToken)
        {
            var categoryPage = await this.publicCatalogService.GetPublishedCategoryPageBySlugAsync(slug);
            var displayCurrency = await this.ResolveDisplayCurrencyAsync(currencyCode, cancellationToken);
            var mappedProducts = categoryPage is null
                ? []
                : await this.ToDisplayCatalogProductContractsAsync(categoryPage.Products, displayCurrency, cancellationToken);
            return categoryPage is null
                ? this.Failure<StorefrontCategoryPageResponse>(ServiceResponseType.NotFound, "Published category was not found.")
                : this.Success(
                    new StorefrontCategoryPageResponse(
                        categoryPage.Category.ToStorefrontContract(),
                        categoryPage.Breadcrumbs.Select(crumb => crumb.ToStorefrontContract()).ToArray(),
                        mappedProducts,
                        categoryPage.DirectProductCount,
                        categoryPage.DescendantProductCount),
                    "Published category page loaded.");
        }

        [HttpGet("categories/{categoryId:guid}/products")]
        public async Task<IActionResult> GetProductsByCategory(
            Guid categoryId,
            [FromQuery] string? currencyCode,
            CancellationToken cancellationToken)
        {
            var category = await this.publicCatalogService.GetPublishedCategoryByIdAsync(categoryId);
            if (category is null)
            {
                return this.Failure<IReadOnlyList<StorefrontCatalogProductResponse>>(
                    ServiceResponseType.NotFound,
                    "Published category was not found.");
            }

            var products = await this.publicCatalogService.GetPublishedProductsByCategoryAsync(categoryId);
            var displayCurrency = await this.ResolveDisplayCurrencyAsync(currencyCode, cancellationToken);
            var mappedProducts = await this.ToDisplayCatalogProductContractsAsync(products, displayCurrency, cancellationToken);
            return this.Success(
                mappedProducts,
                "Published category products loaded.");
        }

        [HttpGet("product-filter-metadata")]
        public async Task<IActionResult> GetProductFilterMetadata(
            [FromQuery(Name = "categorySlug")]
            [MaxLength(256)]
            string? categorySlug,
            [FromQuery(Name = "searchTerm")]
            [MaxLength(256)]
            string? searchTerm,
            [FromQuery(Name = "currencyCode")]
            [StringLength(3, MinimumLength = 3)]
            string? currencyCode,
            CancellationToken cancellationToken)
        {
            var query = new StorefrontProductFilterMetadataQuery
            {
                CategorySlug = categorySlug,
                SearchTerm = searchTerm,
                CurrencyCode = currencyCode,
            };
            var categories = await this.publicCatalogService.GetPublishedCategoryTreeAsync();
            var metadata = await this.publicCatalogService.GetPublishedProductFilterMetadataAsync(new ProductCatalogQuery
            {
                CategorySlug = query.CategorySlug,
                IncludeSubcategories = !string.IsNullOrWhiteSpace(query.CategorySlug),
                SearchTerm = query.SearchTerm,
            });
            var displayCurrencyTask = this.ResolveDisplayCurrencyAsync(query.CurrencyCode, cancellationToken);

            var displayCurrency = await displayCurrencyTask;

            var response = new StorefrontProductFilterMetadataResponse(
                CatalogSearchPolicy.StorefrontPageSizes,
                BuildSortOptions(),
                BuildFilterFacets(categories, query.CategorySlug),
                await this.BuildPriceFacetAsync(metadata, displayCurrency, query.CurrencyCode, cancellationToken),
                CatalogSearchPolicy.MinimumSearchTermLength);

            return this.Success(response, "Product filter metadata loaded.");
        }

        [HttpGet("search-suggestions")]
        public async Task<IActionResult> GetSearchSuggestions(
            [FromQuery(Name = "searchTerm")]
            [MaxLength(256)]
            string? searchTerm,
            [FromQuery(Name = "categorySlug")]
            [MaxLength(256)]
            string? categorySlug,
            [FromQuery(Name = "limit")]
            [Range(1, CatalogSearchPolicy.SuggestionMaxLimit)]
            int? limit,
            [FromQuery(Name = "currencyCode")]
            [StringLength(3, MinimumLength = 3)]
            string? currencyCode,
            CancellationToken cancellationToken)
        {
            var query = new StorefrontSearchSuggestionQuery
            {
                SearchTerm = searchTerm,
                CategorySlug = categorySlug,
                Limit = limit,
                CurrencyCode = currencyCode,
            };
            var normalizedSearchTerm = CatalogSearchPolicy.NormalizeSearchTerm(query.SearchTerm);
            var suggestionLimit = Math.Clamp(
                query.Limit ?? CatalogSearchPolicy.SuggestionDefaultLimit,
                1,
                CatalogSearchPolicy.SuggestionMaxLimit);

            if (CatalogSearchPolicy.IsSearchTermTooShort(normalizedSearchTerm))
            {
                return this.Success(
                    new StorefrontSearchSuggestionResponse(
                        normalizedSearchTerm,
                        CatalogSearchPolicy.MinimumSearchTermLength,
                        suggestionLimit,
                        []),
                    "Search suggestions loaded.");
            }

            var displayCurrency = await this.ResolveDisplayCurrencyAsync(query.CurrencyCode, cancellationToken);
            IReadOnlyList<BlazorShop.Application.DTOs.Product.GetCatalogProduct> suggestions = [];
            if (!string.IsNullOrWhiteSpace(normalizedSearchTerm))
            {
                suggestions = await this.publicCatalogService.GetPublishedSearchSuggestionsAsync(new ProductCatalogQuery
                {
                    SearchTerm = normalizedSearchTerm,
                    CategorySlug = query.CategorySlug,
                    IncludeSubcategories = !string.IsNullOrWhiteSpace(query.CategorySlug),
                }, suggestionLimit);
            }

            var mappedSuggestions = await this.ToSearchSuggestionContractsAsync(
                suggestions,
                displayCurrency,
                cancellationToken);

            return this.Success(
                new StorefrontSearchSuggestionResponse(
                    normalizedSearchTerm,
                    CatalogSearchPolicy.MinimumSearchTermLength,
                    suggestionLimit,
                    mappedSuggestions),
                "Search suggestions loaded.");
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts(
            [FromQuery] StorefrontProductCatalogQuery query,
            CancellationToken cancellationToken)
        {
            var products = await this.publicCatalogService.GetPublishedCatalogPageAsync(query.ToApplicationQuery());
            var displayCurrency = await this.ResolveDisplayCurrencyAsync(query.CurrencyCode, cancellationToken);
            var mappedProducts = await this.ToDisplayCatalogProductContractsAsync(
                products.Items,
                displayCurrency,
                cancellationToken);
            return this.Success(
                new StorefrontPagedResponse<StorefrontCatalogProductResponse>(
                    mappedProducts,
                    products.PageNumber,
                    products.PageSize,
                    products.TotalCount,
                    products.TotalPages),
                "Published products loaded.");
        }

        [HttpGet("products/{id:guid}")]
        public async Task<IActionResult> GetProductById(
            Guid id,
            [FromQuery] string? currencyCode,
            CancellationToken cancellationToken)
        {
            var product = await this.publicCatalogService.GetPublishedProductByIdAsync(id);
            var displayCurrency = await this.ResolveDisplayCurrencyAsync(currencyCode, cancellationToken);
            return product is null
                ? this.Failure<StorefrontProductResponse>(ServiceResponseType.NotFound, "Published product was not found.")
                : this.Success(await this.ToDisplayProductContractAsync(product, displayCurrency, cancellationToken), "Published product loaded.");
        }

        [HttpGet("products/slug/{slug}")]
        public async Task<IActionResult> GetProductBySlug(
            string slug,
            [FromQuery] string? currencyCode,
            CancellationToken cancellationToken)
        {
            var product = await this.publicCatalogService.GetPublishedProductBySlugAsync(slug);
            var displayCurrency = await this.ResolveDisplayCurrencyAsync(currencyCode, cancellationToken);
            return product is null
                ? this.Failure<StorefrontProductResponse>(ServiceResponseType.NotFound, "Published product was not found.")
                : this.Success(await this.ToDisplayProductContractAsync(product, displayCurrency, cancellationToken), "Published product loaded.");
        }

        [HttpPost("products/{productId:guid}/selection-preview")]
        public async Task<IActionResult> PreviewProductSelection(
            Guid productId,
            [FromBody] StorefrontProductSelectionPreviewRequest request,
            CancellationToken cancellationToken)
        {
            var storeIdResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeIdResult.Success)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.productSelectionResolver.ResolveAsync(
                request.ToApplicationRequest(storeIdResult.Payload, productId),
                cancellationToken);

            return result.ResponseType == ServiceResponseType.NotFound
                ? this.Failure<StorefrontProductSelectionPreviewResponse>(ServiceResponseType.NotFound, result.Message)
                : this.Success(result.ToStorefrontContract(), result.Message);
        }

        [HttpGet("sitemap")]
        public async Task<IActionResult> GetSitemap()
        {
            var sitemap = await this.publicCatalogService.GetPublishedSitemapAsync();
            return this.Success<GetPublicCatalogSitemap>(sitemap, "Published catalog sitemap loaded.");
        }

        private static IReadOnlyList<StorefrontProductSortOptionResponse> BuildSortOptions()
        {
            return
            [
                new StorefrontProductSortOptionResponse(StorefrontProductCatalogSortValues.DisplayOrder, "Featured", 10),
                new StorefrontProductSortOptionResponse(StorefrontProductCatalogSortValues.Updated, "Recently updated", 20),
                new StorefrontProductSortOptionResponse(StorefrontProductCatalogSortValues.PriceLowToHigh, "Price low", 30),
                new StorefrontProductSortOptionResponse(StorefrontProductCatalogSortValues.PriceHighToLow, "Price high", 40),
                new StorefrontProductSortOptionResponse(StorefrontProductCatalogSortValues.Newest, "Newest", 50),
                new StorefrontProductSortOptionResponse(StorefrontProductCatalogSortValues.Oldest, "Oldest", 60),
                new StorefrontProductSortOptionResponse(StorefrontProductCatalogSortValues.NameAscending, "Name A-Z", 70),
                new StorefrontProductSortOptionResponse(StorefrontProductCatalogSortValues.NameDescending, "Name Z-A", 80),
            ];
        }

        private static IReadOnlyList<StorefrontFilterFacetResponse> BuildFilterFacets(
            IReadOnlyList<GetCategoryTreeNode> categories,
            string? selectedCategorySlug)
        {
            return
            [
                new StorefrontFilterFacetResponse(
                    "category",
                    "Category",
                    "choice",
                    10,
                    MaxChoices: 50,
                    MinimumHitCount: 0,
                    FlattenCategoryChoices(categories, selectedCategorySlug)),
                new StorefrontFilterFacetResponse(
                    "availability",
                    "Availability",
                    "choice",
                    20,
                    MaxChoices: 2,
                    MinimumHitCount: 0,
                    [
                        new StorefrontFilterChoiceResponse("inStock", "In stock", 10, HitCount: null, Selected: false),
                        new StorefrontFilterChoiceResponse("outOfStock", "Out of stock", 20, HitCount: null, Selected: false),
                    ]),
                new StorefrontFilterFacetResponse(
                    "newArrival",
                    "New arrival",
                    "choice",
                    40,
                    MaxChoices: 1,
                    MinimumHitCount: 0,
                    [
                        new StorefrontFilterChoiceResponse("last30Days", "Last 30 days", 10, HitCount: null, Selected: false),
                    ]),
            ];
        }

        private static IReadOnlyList<StorefrontFilterChoiceResponse> FlattenCategoryChoices(
            IReadOnlyList<GetCategoryTreeNode> categories,
            string? selectedCategorySlug)
        {
            var choices = new List<StorefrontFilterChoiceResponse>();
            foreach (var category in categories)
            {
                AppendCategoryChoice(choices, category, selectedCategorySlug, depth: 0);
            }

            return choices;
        }

        private static void AppendCategoryChoice(
            List<StorefrontFilterChoiceResponse> choices,
            GetCategoryTreeNode category,
            string? selectedCategorySlug,
            int depth)
        {
            if (!string.IsNullOrWhiteSpace(category.Slug))
            {
                var prefix = depth <= 0 ? string.Empty : $"{new string('-', depth * 2)} ";
                choices.Add(new StorefrontFilterChoiceResponse(
                    category.Slug,
                    $"{prefix}{category.Name}",
                    category.DisplayOrder,
                    HitCount: null,
                    Selected: string.Equals(category.Slug, selectedCategorySlug?.Trim(), StringComparison.OrdinalIgnoreCase)));
            }

            foreach (var child in category.Children)
            {
                AppendCategoryChoice(choices, child, selectedCategorySlug, depth + 1);
            }
        }

        private async Task<StorefrontPriceFacetResponse> BuildPriceFacetAsync(
            ProductFilterMetadataReadModel metadata,
            StorefrontDisplayCurrency? displayCurrency,
            string? requestedCurrencyCode,
            CancellationToken cancellationToken)
        {
            var minPrice = metadata.MinPrice;
            var maxPrice = metadata.MaxPrice;
            var currencyCode = NormalizeCurrencyCode(requestedCurrencyCode);

            if (displayCurrency is not null)
            {
                currencyCode = displayCurrency.CurrencyCode;
                if (minPrice.HasValue)
                {
                    minPrice = (await this.ResolveDisplayMoneyAsync(minPrice.Value, comparePrice: null, displayCurrency, cancellationToken)).Price;
                }

                if (maxPrice.HasValue)
                {
                    maxPrice = (await this.ResolveDisplayMoneyAsync(maxPrice.Value, comparePrice: null, displayCurrency, cancellationToken)).Price;
                }
            }

            return new StorefrontPriceFacetResponse(minPrice, maxPrice, currencyCode, DisplayOrder: 30);
        }

        private static string? NormalizeCurrencyCode(string? currencyCode)
        {
            var normalized = currencyCode?.Trim().ToUpperInvariant();
            return normalized is { Length: 3 } && normalized.All(char.IsLetter)
                ? normalized
                : null;
        }

        private async Task<StorefrontDisplayCurrency?> ResolveDisplayCurrencyAsync(
            string? requestedCurrencyCode,
            CancellationToken cancellationToken)
        {
            var storeIdResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeIdResult.Success)
            {
                return null;
            }

            var resolution = await this.workingCurrencyResolver.ResolveAsync(
                storeIdResult.Payload,
                requestedCurrencyCode,
                cancellationToken);

            return new StorefrontDisplayCurrency(
                storeIdResult.Payload,
                resolution.CurrencyCode,
                resolution.BaseCurrencyCode);
        }

        private async Task<StorefrontCatalogProductResponse> ToDisplayCatalogProductContractAsync(
            BlazorShop.Application.DTOs.Product.GetCatalogProduct product,
            StorefrontDisplayCurrency? displayCurrency,
            CancellationToken cancellationToken)
        {
            var displayMoney = displayCurrency is null
                ? null
                : await this.ResolveDisplayMoneyAsync(product.Price, product.ComparePrice, displayCurrency, cancellationToken);

            return product.ToStorefrontContract(displayMoney);
        }

        private async Task<StorefrontCatalogProductResponse[]> ToDisplayCatalogProductContractsAsync(
            IEnumerable<BlazorShop.Application.DTOs.Product.GetCatalogProduct> products,
            StorefrontDisplayCurrency? displayCurrency,
            CancellationToken cancellationToken)
        {
            var mappedProducts = new List<StorefrontCatalogProductResponse>();
            foreach (var product in products)
            {
                mappedProducts.Add(await this.ToDisplayCatalogProductContractAsync(
                    product,
                    displayCurrency,
                    cancellationToken));
            }

            return mappedProducts.ToArray();
        }

        private async Task<StorefrontSearchSuggestionItemResponse> ToSearchSuggestionContractAsync(
            BlazorShop.Application.DTOs.Product.GetCatalogProduct product,
            StorefrontDisplayCurrency? displayCurrency,
            CancellationToken cancellationToken)
        {
            var displayMoney = displayCurrency is null
                ? null
                : await this.ResolveDisplayMoneyAsync(product.Price, comparePrice: null, displayCurrency, cancellationToken);
            var slug = product.Slug ?? string.Empty;
            return new StorefrontSearchSuggestionItemResponse(
                product.Id,
                slug,
                product.Name ?? string.Empty,
                product.Sku,
                product.Image,
                product.PrimaryMediaPublicId,
                product.HasPrimaryMedia,
                product.Price,
                displayMoney?.Price,
                displayMoney?.CurrencyCode,
                product.CategoryName,
                product.CategorySlug,
                product.InStock,
                string.IsNullOrWhiteSpace(slug) ? "/product" : $"/product/{Uri.EscapeDataString(slug)}");
        }

        private async Task<StorefrontSearchSuggestionItemResponse[]> ToSearchSuggestionContractsAsync(
            IEnumerable<BlazorShop.Application.DTOs.Product.GetCatalogProduct> products,
            StorefrontDisplayCurrency? displayCurrency,
            CancellationToken cancellationToken)
        {
            var mappedSuggestions = new List<StorefrontSearchSuggestionItemResponse>();
            foreach (var product in products)
            {
                mappedSuggestions.Add(await this.ToSearchSuggestionContractAsync(
                    product,
                    displayCurrency,
                    cancellationToken));
            }

            return mappedSuggestions.ToArray();
        }

        private async Task<StorefrontProductResponse> ToDisplayProductContractAsync(
            BlazorShop.Application.DTOs.Product.GetProduct product,
            StorefrontDisplayCurrency? displayCurrency,
            CancellationToken cancellationToken)
        {
            var displayMoney = displayCurrency is null
                ? null
                : await this.ResolveDisplayMoneyAsync(product.Price, product.ComparePrice, displayCurrency, cancellationToken);

            if (displayCurrency is null)
            {
                return product.ToStorefrontContract(displayMoney);
            }

            var variantDisplayMoney = new Dictionary<Guid, StorefrontDisplayMoney>();
            foreach (var variant in product.Variants)
            {
                var effectivePrice = variant.EffectivePrice > 0
                    ? variant.EffectivePrice
                    : variant.Price ?? product.Price;
                variantDisplayMoney[variant.Id] = await this.ResolveDisplayMoneyAsync(
                    effectivePrice,
                    comparePrice: null,
                    displayCurrency,
                    cancellationToken);
            }

            return product.ToStorefrontContract(
                displayMoney,
                (variant, parentProduct) => variant.ToStorefrontContract(
                    variantDisplayMoney.TryGetValue(variant.Id, out var variantMoney)
                        ? variantMoney
                        : null,
                    parentProduct));
        }

        private async Task<StorefrontDisplayMoney> ResolveDisplayMoneyAsync(
            decimal price,
            decimal? comparePrice,
            StorefrontDisplayCurrency displayCurrency,
            CancellationToken cancellationToken)
        {
            if (string.Equals(displayCurrency.CurrencyCode, displayCurrency.BaseCurrencyCode, StringComparison.Ordinal))
            {
                return new StorefrontDisplayMoney(price, comparePrice, displayCurrency.BaseCurrencyCode);
            }

            var priceResult = await this.moneyConversionService.ConvertFromBaseAsync(
                displayCurrency.StoreId,
                price,
                displayCurrency.CurrencyCode,
                cancellationToken);
            if (!priceResult.Success || priceResult.Payload is null)
            {
                return new StorefrontDisplayMoney(price, comparePrice, displayCurrency.BaseCurrencyCode);
            }

            decimal? convertedComparePrice = null;
            if (comparePrice.HasValue)
            {
                var compareResult = await this.moneyConversionService.ConvertFromBaseAsync(
                    displayCurrency.StoreId,
                    comparePrice.Value,
                    displayCurrency.CurrencyCode,
                    cancellationToken);
                convertedComparePrice = compareResult.Success && compareResult.Payload is not null
                    ? compareResult.Payload.ConvertedAmount
                    : comparePrice;
            }

            return new StorefrontDisplayMoney(
                priceResult.Payload.ConvertedAmount,
                convertedComparePrice,
                priceResult.Payload.TargetCurrencyCode);
        }

        private sealed record StorefrontDisplayCurrency(
            Guid StoreId,
            string CurrencyCode,
            string BaseCurrencyCode);
    }

}
