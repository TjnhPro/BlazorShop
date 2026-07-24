namespace BlazorShop.Storefront.Services
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using BlazorShop.Storefront.Configuration;
    using BlazorShop.Storefront.Models;
    using BlazorShop.Storefront.Services.Contracts;

    using Microsoft.AspNetCore.Http;

    using GeneratedClients = BlazorShop.Storefront.Client;
    using GeneratedCatalogClient = BlazorShop.Storefront.Client.IStorefrontCatalogClient;
    using GeneratedNavigationClient = BlazorShop.Storefront.Client.IStorefrontNavigationClient;
    using GeneratedPagesClient = BlazorShop.Storefront.Client.IStorefrontPagesClient;
    using GeneratedSeoClient = BlazorShop.Storefront.Client.IStorefrontSeoClient;

    public sealed class GeneratedStorefrontCatalogContentClient : IStorefrontCatalogClient, IStorefrontContentClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly GeneratedCatalogClient _catalogClient;
        private readonly GeneratedPagesClient _pagesClient;
        private readonly GeneratedNavigationClient _navigationClient;
        private readonly GeneratedSeoClient _seoClient;
        private readonly IConfiguration _configuration;

        public GeneratedStorefrontCatalogContentClient(
            GeneratedCatalogClient catalogClient,
            GeneratedPagesClient pagesClient,
            GeneratedNavigationClient navigationClient,
            GeneratedSeoClient seoClient,
            IConfiguration configuration)
        {
            _catalogClient = catalogClient;
            _pagesClient = pagesClient;
            _navigationClient = navigationClient;
            _seoClient = seoClient;
            _configuration = configuration;
        }

        public async Task<StorefrontApiResult<IReadOnlyList<GetCategory>>> GetPublishedCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteListAsync<GeneratedClients.StorefrontCategoryResponseIReadOnlyListCommerceNodeApiResponse, GeneratedClients.StorefrontCategoryResponse, GetCategory>(
                storeKey => _catalogClient.ListCategoriesAsync(storeKey, cancellationToken),
                cancellationToken);
        }

        public async Task<StorefrontApiResult<IReadOnlyList<GetCategoryTreeNode>>> GetPublishedCategoryTreeAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteListAsync<GeneratedClients.StorefrontCategoryTreeNodeResponseIReadOnlyListCommerceNodeApiResponse, GeneratedClients.StorefrontCategoryTreeNodeResponse, GetCategoryTreeNode>(
                storeKey => _catalogClient.GetCategoryTreeAsync(storeKey, cancellationToken),
                cancellationToken);
        }

        public async Task<StorefrontApiResult<GetPublicCatalogSitemap>> GetPublishedSitemapAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync<GeneratedClients.GetPublicCatalogSitemapCommerceNodeApiResponse, GeneratedClients.GetPublicCatalogSitemap, GetPublicCatalogSitemap>(
                storeKey => _catalogClient.GetSitemapAsync(storeKey, cancellationToken),
                cancellationToken,
                fallbackValue: new GetPublicCatalogSitemap());
        }

        public Task<StorefrontApiResult<PagedResult<GetCatalogProduct>>> GetPublishedCatalogPageAsync(
            ProductCatalogQuery query,
            CancellationToken cancellationToken = default)
        {
            return GetPublishedCatalogPageAsync(query, currencyCode: null, cancellationToken);
        }

        public async Task<StorefrontApiResult<PagedResult<GetCatalogProduct>>> GetPublishedCatalogPageAsync(
            ProductCatalogQuery query,
            string? currencyCode,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync<GeneratedClients.StorefrontCatalogProductResponseStorefrontPagedResponseCommerceNodeApiResponse, GeneratedClients.StorefrontCatalogProductResponseStorefrontPagedResponse, PagedResult<GetCatalogProduct>>(
                storeKey => _catalogClient.QueryProductsAsync(
                    Math.Max(1, query.PageNumber),
                    Math.Max(1, query.PageSize),
                    query.CategoryId,
                    NormalizeOptional(query.CategorySlug),
                    query.IncludeSubcategories,
                    NormalizeOptional(query.SearchTerm),
                    query.MinPrice.HasValue ? (double?)query.MinPrice.Value : null,
                    query.MaxPrice.HasValue ? (double?)query.MaxPrice.Value : null,
                    query.InStock,
                    MapSort(query.SortBy),
                    query.CreatedAfterUtc,
                    NormalizeCurrencyCode(currencyCode),
                    storeKey,
                    cancellationToken),
                cancellationToken,
                fallbackValue: new PagedResult<GetCatalogProduct>());
        }

        public Task<StorefrontApiResult<GetCategoryPage>> GetPublishedCategoryBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            return GetPublishedCategoryBySlugAsync(slug, currencyCode: null, cancellationToken);
        }

        public async Task<StorefrontApiResult<GetCategoryPage>> GetPublishedCategoryBySlugAsync(
            string slug,
            string? currencyCode,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return StorefrontApiResult<GetCategoryPage>.NotFound();
            }

            return await ExecuteAsync<GeneratedClients.StorefrontCategoryPageResponseCommerceNodeApiResponse, GeneratedClients.StorefrontCategoryPageResponse, GetCategoryPage>(
                storeKey => _catalogClient.GetCategoryBySlugAsync(slug.Trim(), NormalizeCurrencyCode(currencyCode), storeKey, cancellationToken),
                cancellationToken);
        }

        public async Task<StorefrontApiResult<StorefrontProductFilterMetadataResponse>> GetProductFilterMetadataAsync(
            string? categorySlug = null,
            string? searchTerm = null,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync<GeneratedClients.StorefrontProductFilterMetadataResponseCommerceNodeApiResponse, GeneratedClients.StorefrontProductFilterMetadataResponse, StorefrontProductFilterMetadataResponse>(
                storeKey => _catalogClient.GetProductFilterMetadataAsync(
                    NormalizeOptional(categorySlug),
                    NormalizeOptional(searchTerm),
                    NormalizeCurrencyCode(currencyCode),
                    storeKey,
                    cancellationToken),
                cancellationToken);
        }

        public async Task<StorefrontApiResult<StorefrontSearchSuggestionResponse>> GetSearchSuggestionsAsync(
            string? searchTerm,
            string? categorySlug = null,
            int? limit = null,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync<GeneratedClients.StorefrontSearchSuggestionResponseCommerceNodeApiResponse, GeneratedClients.StorefrontSearchSuggestionResponse, StorefrontSearchSuggestionResponse>(
                storeKey => _catalogClient.GetSearchSuggestionsAsync(
                    NormalizeOptional(searchTerm),
                    NormalizeOptional(categorySlug),
                    limit,
                    NormalizeCurrencyCode(currencyCode),
                    storeKey,
                    cancellationToken),
                cancellationToken);
        }

        public Task<StorefrontApiResult<GetProduct>> GetPublishedProductBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            return GetPublishedProductBySlugAsync(slug, currencyCode: null, cancellationToken);
        }

        public async Task<StorefrontApiResult<GetProduct>> GetPublishedProductBySlugAsync(
            string slug,
            string? currencyCode,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return StorefrontApiResult<GetProduct>.NotFound();
            }

            return await ExecuteAsync<GeneratedClients.StorefrontProductResponseCommerceNodeApiResponse, GeneratedClients.StorefrontProductResponse, GetProduct>(
                storeKey => _catalogClient.GetProductBySlugAsync(slug.Trim(), NormalizeCurrencyCode(currencyCode), storeKey, cancellationToken),
                cancellationToken);
        }

        public Task<StorefrontApiResult<GetProduct>> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return GetProductByIdAsync(id, currencyCode: null, cancellationToken);
        }

        public async Task<StorefrontApiResult<GetProduct>> GetProductByIdAsync(
            Guid id,
            string? currencyCode,
            CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                return StorefrontApiResult<GetProduct>.NotFound();
            }

            return await ExecuteAsync<GeneratedClients.StorefrontProductResponseCommerceNodeApiResponse, GeneratedClients.StorefrontProductResponse, GetProduct>(
                storeKey => _catalogClient.GetProductByIdAsync(id, NormalizeCurrencyCode(currencyCode), storeKey, cancellationToken),
                cancellationToken);
        }

        public async Task<StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>> PreviewProductSelectionAsync(
            Guid productId,
            StorefrontProductSelectionPreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            if (productId == Guid.Empty)
            {
                return StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>.Failed("Product is required.");
            }

            var storeKey = ResolveStoreKey();
            if (string.IsNullOrWhiteSpace(storeKey))
            {
                return StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>.Failed("Store key is required.");
            }

            try
            {
                var response = await _catalogClient.PreviewProductSelectionAsync(
                    productId,
                    storeKey,
                    Project<GeneratedClients.StorefrontProductSelectionPreviewRequest>(request),
                    cancellationToken);
                return response.Success == true && response.Data is not null
                    ? StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>.Succeeded(
                        Project<StorefrontProductSelectionPreviewResponse>(response.Data),
                        response.Message)
                    : StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>.Failed(response.Message);
            }
            catch (GeneratedClients.StorefrontApiException<GeneratedClients.CommerceNodeApiErrorResponse> exception)
            {
                return StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>.Failed(exception.Result.Message);
            }
            catch (Exception exception) when (IsGeneratedClientTransportFailure(exception))
            {
                return StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>.Failed("Unable to preview this product selection right now.");
            }
        }

        public async Task<StorefrontApiResult<GetStorefrontPage>> GetPublishedPageBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return StorefrontApiResult<GetStorefrontPage>.NotFound();
            }

            return await ExecuteAsync<GeneratedClients.StorefrontPagePublicDtoCommerceNodeApiResponse, GeneratedClients.StorefrontPagePublicDto, GetStorefrontPage>(
                storeKey => _pagesClient.GetBySlugAsync(slug.Trim(), storeKey, cancellationToken),
                cancellationToken);
        }

        public async Task<StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>> GetPageNavigationLinksAsync(
            CancellationToken cancellationToken = default)
        {
            return await ExecuteListAsync<GeneratedClients.StorefrontPageNavigationLinkDtoIReadOnlyListCommerceNodeApiResponse, GeneratedClients.StorefrontPageNavigationLinkDto, StorefrontPageNavigationLinkDto>(
                storeKey => _pagesClient.ListNavigationAsync(storeKey, cancellationToken),
                cancellationToken);
        }

        public async Task<StorefrontApiResult<StoreNavigationPublicMenuDto>> GetNavigationMenuAsync(
            string systemName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(systemName))
            {
                return StorefrontApiResult<StoreNavigationPublicMenuDto>.NotFound();
            }

            return await ExecuteAsync<GeneratedClients.StoreNavigationPublicMenuDtoCommerceNodeApiResponse, GeneratedClients.StoreNavigationPublicMenuDto, StoreNavigationPublicMenuDto>(
                storeKey => _navigationClient.GetMenuAsync(systemName.Trim().ToLowerInvariant(), storeKey, cancellationToken),
                cancellationToken);
        }

        public async Task<StorefrontApiResult<GetSeoSettings>> GetSeoSettingsAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync<GeneratedClients.SeoSettingsDtoCommerceNodeApiResponse, GeneratedClients.SeoSettingsDto, GetSeoSettings>(
                storeKey => _seoClient.GetSettingsAsync(storeKey, cancellationToken),
                cancellationToken);
        }

        public async Task<StorefrontApiResult<SeoRedirectResolutionDto>> GetRedirectResolutionAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync<GeneratedClients.SeoRedirectResolutionDtoCommerceNodeApiResponse, GeneratedClients.SeoRedirectResolutionDto, SeoRedirectResolutionDto>(
                storeKey => _seoClient.ResolveRedirectAsync(path, storeKey, cancellationToken),
                cancellationToken);
        }

        private async Task<StorefrontApiResult<IReadOnlyList<TLocal>>> ExecuteListAsync<TResponse, TGenerated, TLocal>(
            Func<string, Task<TResponse>> execute,
            CancellationToken cancellationToken)
        {
            var storeKey = ResolveStoreKey();
            if (string.IsNullOrWhiteSpace(storeKey))
            {
                return StorefrontApiResult<IReadOnlyList<TLocal>>.Success([]);
            }

            try
            {
                var response = await execute(storeKey);
                if (response is null)
                {
                    return StorefrontApiResult<IReadOnlyList<TLocal>>.Success([]);
                }

                var success = GetEnvelopeSuccess(response);
                var data = GetEnvelopeData<IEnumerable<TGenerated>>(response);
                return success == true && data is not null
                    ? StorefrontApiResult<IReadOnlyList<TLocal>>.Success(data.Select(item => Project<TLocal>(item!)).ToArray())
                    : StorefrontApiResult<IReadOnlyList<TLocal>>.Success([]);
            }
            catch (GeneratedClients.StorefrontApiException exception) when (exception.StatusCode == StatusCodes.Status404NotFound)
            {
                return StorefrontApiResult<IReadOnlyList<TLocal>>.Success([]);
            }
            catch (Exception exception) when (IsGeneratedClientTransportFailure(exception))
            {
                return StorefrontApiResult<IReadOnlyList<TLocal>>.ServiceUnavailable();
            }
        }

        private async Task<StorefrontApiResult<TLocal>> ExecuteAsync<TResponse, TGenerated, TLocal>(
            Func<string, Task<TResponse>> execute,
            CancellationToken cancellationToken,
            TLocal? fallbackValue = default)
        {
            var storeKey = ResolveStoreKey();
            if (string.IsNullOrWhiteSpace(storeKey))
            {
                return fallbackValue is not null
                    ? StorefrontApiResult<TLocal>.Success(fallbackValue)
                    : StorefrontApiResult<TLocal>.NotFound();
            }

            try
            {
                var response = await execute(storeKey);
                if (response is null)
                {
                    return fallbackValue is not null
                        ? StorefrontApiResult<TLocal>.Success(fallbackValue)
                        : StorefrontApiResult<TLocal>.NotFound();
                }

                var success = GetEnvelopeSuccess(response);
                var data = GetEnvelopeData<TGenerated>(response);
                if (success == true && data is not null)
                {
                    return StorefrontApiResult<TLocal>.Success(Project<TLocal>(data));
                }

                return fallbackValue is not null
                    ? StorefrontApiResult<TLocal>.Success(fallbackValue)
                    : StorefrontApiResult<TLocal>.NotFound();
            }
            catch (GeneratedClients.StorefrontApiException exception) when (exception.StatusCode == StatusCodes.Status404NotFound)
            {
                return fallbackValue is not null
                    ? StorefrontApiResult<TLocal>.Success(fallbackValue)
                    : StorefrontApiResult<TLocal>.NotFound();
            }
            catch (Exception exception) when (IsGeneratedClientTransportFailure(exception))
            {
                return StorefrontApiResult<TLocal>.ServiceUnavailable();
            }
        }

        private string? ResolveStoreKey()
        {
            return StorefrontApiEndpointResolver.ResolveStoreKey(_configuration);
        }

        private static bool? GetEnvelopeSuccess(object response)
        {
            return response.GetType().GetProperty("Success")?.GetValue(response) as bool?;
        }

        private static T? GetEnvelopeData<T>(object response)
        {
            return response.GetType().GetProperty("Data")?.GetValue(response) is { } data
                ? (T)data
                : default;
        }

        private static TTarget Project<TTarget>(object source)
        {
            return JsonSerializer.Deserialize<TTarget>(JsonSerializer.Serialize(source, JsonOptions), JsonOptions)
                ?? throw new InvalidOperationException($"Could not project generated Storefront DTO to {typeof(TTarget).Name}.");
        }

        private static GeneratedClients.SortBy MapSort(ProductCatalogSortBy sortBy)
        {
            return sortBy.ToApiValue() switch
            {
                "oldest" => GeneratedClients.SortBy.Oldest,
                "priceLowToHigh" => GeneratedClients.SortBy.PriceLowToHigh,
                "priceHighToLow" => GeneratedClients.SortBy.PriceHighToLow,
                "nameAscending" => GeneratedClients.SortBy.NameAscending,
                "nameDescending" => GeneratedClients.SortBy.NameDescending,
                "displayOrder" => GeneratedClients.SortBy.DisplayOrder,
                "updated" => GeneratedClients.SortBy.Updated,
                _ => GeneratedClients.SortBy.Newest,
            };
        }

        private static bool IsGeneratedClientTransportFailure(Exception exception)
        {
            return exception is GeneratedClients.StorefrontApiException
                or HttpRequestException
                or TaskCanceledException
                or InvalidOperationException;
        }

        private static string? NormalizeCurrencyCode(string? value)
        {
            var normalized = NormalizeOptional(value)?.ToUpperInvariant();
            return normalized is { Length: 3 } && normalized.All(char.IsLetter)
                ? normalized
                : null;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
