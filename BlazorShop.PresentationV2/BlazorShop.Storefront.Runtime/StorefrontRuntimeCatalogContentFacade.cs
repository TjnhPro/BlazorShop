namespace BlazorShop.Storefront.Runtime
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using BlazorShop.Storefront.Client;

    using GeneratedCatalogClient = BlazorShop.Storefront.Client.IStorefrontCatalogClient;
    using GeneratedNavigationClient = BlazorShop.Storefront.Client.IStorefrontNavigationClient;
    using GeneratedPagesClient = BlazorShop.Storefront.Client.IStorefrontPagesClient;
    using GeneratedSeoClient = BlazorShop.Storefront.Client.IStorefrontSeoClient;

    public interface IStorefrontRuntimeCatalogContentFacade
    {
        Task<StorefrontRuntimeResult<IReadOnlyList<StorefrontCategoryResponse>>> GetPublishedCategoriesAsync(CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeResult<IReadOnlyList<StorefrontCategoryTreeNodeResponse>>> GetPublishedCategoryTreeAsync(CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeResult<GetPublicCatalogSitemap>> GetPublishedSitemapAsync(CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeResult<StorefrontCatalogProductResponseStorefrontPagedResponse>> GetPublishedCatalogPageAsync(
            StorefrontRuntimeProductCatalogQuery query,
            string? currencyCode = null,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeResult<StorefrontCategoryPageResponse>> GetPublishedCategoryBySlugAsync(
            string slug,
            string? currencyCode = null,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeResult<StorefrontProductFilterMetadataResponse>> GetProductFilterMetadataAsync(
            string? categorySlug = null,
            string? searchTerm = null,
            string? currencyCode = null,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeResult<StorefrontSearchSuggestionResponse>> GetSearchSuggestionsAsync(
            string? searchTerm,
            string? categorySlug = null,
            int? limit = null,
            string? currencyCode = null,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeResult<StorefrontProductResponse>> GetPublishedProductBySlugAsync(
            string slug,
            string? currencyCode = null,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeResult<StorefrontProductResponse>> GetProductByIdAsync(
            Guid id,
            string? currencyCode = null,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontProductSelectionPreviewResponse>> PreviewProductSelectionAsync(
            Guid productId,
            StorefrontProductSelectionPreviewRequest request,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeResult<StorefrontPagePublicDto>> GetPublishedPageBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>> GetPageNavigationLinksAsync(
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeResult<StoreNavigationPublicMenuDto>> GetNavigationMenuAsync(
            string systemName,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeResult<SeoSettingsDto>> GetSeoSettingsAsync(CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeResult<SeoRedirectResolutionDto>> GetRedirectResolutionAsync(
            string path,
            CancellationToken cancellationToken = default);
    }

    public sealed record StorefrontRuntimeProductCatalogQuery(
        int PageNumber,
        int PageSize,
        Guid? CategoryId,
        string? CategorySlug,
        bool IncludeSubcategories,
        string? SearchTerm,
        decimal? MinPrice,
        decimal? MaxPrice,
        bool? InStock,
        string SortBy,
        DateTimeOffset? CreatedAfterUtc);

    public sealed class StorefrontRuntimeCatalogContentFacade : IStorefrontRuntimeCatalogContentFacade
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly IStorefrontRuntimeContext context;
        private readonly GeneratedCatalogClient catalogClient;
        private readonly GeneratedPagesClient pagesClient;
        private readonly GeneratedNavigationClient navigationClient;
        private readonly GeneratedSeoClient seoClient;

        public StorefrontRuntimeCatalogContentFacade(
            IStorefrontRuntimeContext context,
            GeneratedCatalogClient catalogClient,
            GeneratedPagesClient pagesClient,
            GeneratedNavigationClient navigationClient,
            GeneratedSeoClient seoClient)
        {
            this.context = context;
            this.catalogClient = catalogClient;
            this.pagesClient = pagesClient;
            this.navigationClient = navigationClient;
            this.seoClient = seoClient;
        }

        public Task<StorefrontRuntimeResult<IReadOnlyList<StorefrontCategoryResponse>>> GetPublishedCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return ExecuteListAsync<StorefrontCategoryResponseIReadOnlyListCommerceNodeApiResponse, StorefrontCategoryResponse>(
                storeKey => this.catalogClient.ListCategoriesAsync(storeKey, cancellationToken),
                cancellationToken);
        }

        public Task<StorefrontRuntimeResult<IReadOnlyList<StorefrontCategoryTreeNodeResponse>>> GetPublishedCategoryTreeAsync(CancellationToken cancellationToken = default)
        {
            return ExecuteListAsync<StorefrontCategoryTreeNodeResponseIReadOnlyListCommerceNodeApiResponse, StorefrontCategoryTreeNodeResponse>(
                storeKey => this.catalogClient.GetCategoryTreeAsync(storeKey, cancellationToken),
                cancellationToken);
        }

        public Task<StorefrontRuntimeResult<GetPublicCatalogSitemap>> GetPublishedSitemapAsync(CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<GetPublicCatalogSitemapCommerceNodeApiResponse, GetPublicCatalogSitemap>(
                storeKey => this.catalogClient.GetSitemapAsync(storeKey, cancellationToken),
                fallbackValue: new GetPublicCatalogSitemap(),
                cancellationToken: cancellationToken);
        }

        public Task<StorefrontRuntimeResult<StorefrontCatalogProductResponseStorefrontPagedResponse>> GetPublishedCatalogPageAsync(
            StorefrontRuntimeProductCatalogQuery query,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontCatalogProductResponseStorefrontPagedResponseCommerceNodeApiResponse, StorefrontCatalogProductResponseStorefrontPagedResponse>(
                storeKey => this.catalogClient.QueryProductsAsync(
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
                fallbackValue: new StorefrontCatalogProductResponseStorefrontPagedResponse(),
                cancellationToken: cancellationToken);
        }

        public Task<StorefrontRuntimeResult<StorefrontCategoryPageResponse>> GetPublishedCategoryBySlugAsync(
            string slug,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return string.IsNullOrWhiteSpace(slug)
                ? Task.FromResult(StorefrontRuntimeResult<StorefrontCategoryPageResponse>.Failed(NotFound()))
                : ExecuteAsync<StorefrontCategoryPageResponseCommerceNodeApiResponse, StorefrontCategoryPageResponse>(
                    storeKey => this.catalogClient.GetCategoryBySlugAsync(slug.Trim(), NormalizeCurrencyCode(currencyCode), storeKey, cancellationToken),
                    cancellationToken: cancellationToken);
        }

        public Task<StorefrontRuntimeResult<StorefrontProductFilterMetadataResponse>> GetProductFilterMetadataAsync(
            string? categorySlug = null,
            string? searchTerm = null,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontProductFilterMetadataResponseCommerceNodeApiResponse, StorefrontProductFilterMetadataResponse>(
                storeKey => this.catalogClient.GetProductFilterMetadataAsync(
                    NormalizeOptional(categorySlug),
                    NormalizeOptional(searchTerm),
                    NormalizeCurrencyCode(currencyCode),
                    storeKey,
                    cancellationToken),
                cancellationToken: cancellationToken);
        }

        public Task<StorefrontRuntimeResult<StorefrontSearchSuggestionResponse>> GetSearchSuggestionsAsync(
            string? searchTerm,
            string? categorySlug = null,
            int? limit = null,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontSearchSuggestionResponseCommerceNodeApiResponse, StorefrontSearchSuggestionResponse>(
                storeKey => this.catalogClient.GetSearchSuggestionsAsync(
                    NormalizeOptional(searchTerm),
                    NormalizeOptional(categorySlug),
                    limit,
                    NormalizeCurrencyCode(currencyCode),
                    storeKey,
                    cancellationToken),
                cancellationToken: cancellationToken);
        }

        public Task<StorefrontRuntimeResult<StorefrontProductResponse>> GetPublishedProductBySlugAsync(
            string slug,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return string.IsNullOrWhiteSpace(slug)
                ? Task.FromResult(StorefrontRuntimeResult<StorefrontProductResponse>.Failed(NotFound()))
                : ExecuteAsync<StorefrontProductResponseCommerceNodeApiResponse, StorefrontProductResponse>(
                    storeKey => this.catalogClient.GetProductBySlugAsync(slug.Trim(), NormalizeCurrencyCode(currencyCode), storeKey, cancellationToken),
                    cancellationToken: cancellationToken);
        }

        public Task<StorefrontRuntimeResult<StorefrontProductResponse>> GetProductByIdAsync(
            Guid id,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return id == Guid.Empty
                ? Task.FromResult(StorefrontRuntimeResult<StorefrontProductResponse>.Failed(NotFound()))
                : ExecuteAsync<StorefrontProductResponseCommerceNodeApiResponse, StorefrontProductResponse>(
                    storeKey => this.catalogClient.GetProductByIdAsync(id, NormalizeCurrencyCode(currencyCode), storeKey, cancellationToken),
                    cancellationToken: cancellationToken);
        }

        public async Task<StorefrontRuntimeSubmitResult<StorefrontProductSelectionPreviewResponse>> PreviewProductSelectionAsync(
            Guid productId,
            StorefrontProductSelectionPreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            if (productId == Guid.Empty)
            {
                return StorefrontRuntimeSubmitResult<StorefrontProductSelectionPreviewResponse>.Failed(BadRequest("Product is required."));
            }

            try
            {
                var response = await this.catalogClient.PreviewProductSelectionAsync(
                    productId,
                    this.context.RequireStoreKey(),
                    request,
                    cancellationToken).ConfigureAwait(false);
                return response.Success == true && response.Data is not null
                    ? StorefrontRuntimeSubmitResult<StorefrontProductSelectionPreviewResponse>.Succeeded(response.Data)
                    : StorefrontRuntimeSubmitResult<StorefrontProductSelectionPreviewResponse>.Failed(ServiceUnavailable(response.Message));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                return StorefrontRuntimeSubmitResult<StorefrontProductSelectionPreviewResponse>.Failed(StorefrontRuntimeErrorMapper.FromException(exception));
            }
        }

        public Task<StorefrontRuntimeResult<StorefrontPagePublicDto>> GetPublishedPageBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            return string.IsNullOrWhiteSpace(slug)
                ? Task.FromResult(StorefrontRuntimeResult<StorefrontPagePublicDto>.Failed(NotFound()))
                : ExecuteAsync<StorefrontPagePublicDtoCommerceNodeApiResponse, StorefrontPagePublicDto>(
                    storeKey => this.pagesClient.GetBySlugAsync(slug.Trim(), storeKey, cancellationToken),
                    cancellationToken: cancellationToken);
        }

        public Task<StorefrontRuntimeResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>> GetPageNavigationLinksAsync(CancellationToken cancellationToken = default)
        {
            return ExecuteListAsync<StorefrontPageNavigationLinkDtoIReadOnlyListCommerceNodeApiResponse, StorefrontPageNavigationLinkDto>(
                storeKey => this.pagesClient.ListNavigationAsync(storeKey, cancellationToken),
                cancellationToken);
        }

        public Task<StorefrontRuntimeResult<StoreNavigationPublicMenuDto>> GetNavigationMenuAsync(
            string systemName,
            CancellationToken cancellationToken = default)
        {
            return string.IsNullOrWhiteSpace(systemName)
                ? Task.FromResult(StorefrontRuntimeResult<StoreNavigationPublicMenuDto>.Failed(NotFound()))
                : ExecuteAsync<StoreNavigationPublicMenuDtoCommerceNodeApiResponse, StoreNavigationPublicMenuDto>(
                    storeKey => this.navigationClient.GetMenuAsync(systemName.Trim().ToLowerInvariant(), storeKey, cancellationToken),
                    cancellationToken: cancellationToken);
        }

        public Task<StorefrontRuntimeResult<SeoSettingsDto>> GetSeoSettingsAsync(CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<SeoSettingsDtoCommerceNodeApiResponse, SeoSettingsDto>(
                storeKey => this.seoClient.GetSettingsAsync(storeKey, cancellationToken),
                cancellationToken: cancellationToken);
        }

        public Task<StorefrontRuntimeResult<SeoRedirectResolutionDto>> GetRedirectResolutionAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<SeoRedirectResolutionDtoCommerceNodeApiResponse, SeoRedirectResolutionDto>(
                storeKey => this.seoClient.ResolveRedirectAsync(path, storeKey, cancellationToken),
                cancellationToken: cancellationToken);
        }

        private async Task<StorefrontRuntimeResult<IReadOnlyList<TData>>> ExecuteListAsync<TEnvelope, TData>(
            Func<string, Task<TEnvelope>> execute,
            CancellationToken cancellationToken)
        {
            var result = await ExecuteAsync<TEnvelope, IEnumerable<TData>>(
                execute,
                fallbackValue: Array.Empty<TData>(),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return result.Success && result.Value is not null
                ? StorefrontRuntimeResult<IReadOnlyList<TData>>.Succeeded(result.Value.ToArray())
                : StorefrontRuntimeResult<IReadOnlyList<TData>>.Failed(result.Error ?? ServiceUnavailable());
        }

        private async Task<StorefrontRuntimeResult<TData>> ExecuteAsync<TEnvelope, TData>(
            Func<string, Task<TEnvelope>> execute,
            TData? fallbackValue = default,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await execute(this.context.RequireStoreKey()).ConfigureAwait(false);
                if (response is null)
                {
                    return fallbackValue is not null
                        ? StorefrontRuntimeResult<TData>.Succeeded(fallbackValue)
                        : StorefrontRuntimeResult<TData>.Failed(NotFound());
                }

                var success = response.GetType().GetProperty("Success")?.GetValue(response) as bool?;
                var data = response.GetType().GetProperty("Data")?.GetValue(response);
                if (success == true && data is not null)
                {
                    return StorefrontRuntimeResult<TData>.Succeeded(Project<TData>(data));
                }

                return fallbackValue is not null
                    ? StorefrontRuntimeResult<TData>.Succeeded(fallbackValue)
                    : StorefrontRuntimeResult<TData>.Failed(NotFound());
            }
            catch (StorefrontApiException exception) when (exception.StatusCode == StorefrontRuntimeStatusCodes.NotFound)
            {
                return fallbackValue is not null
                    ? StorefrontRuntimeResult<TData>.Succeeded(fallbackValue)
                    : StorefrontRuntimeResult<TData>.Failed(NotFound());
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                return StorefrontRuntimeResult<TData>.Failed(StorefrontRuntimeErrorMapper.FromException(exception));
            }
        }

        private static TTarget Project<TTarget>(object source)
        {
            return JsonSerializer.Deserialize<TTarget>(JsonSerializer.Serialize(source, JsonOptions), JsonOptions)
                ?? throw new InvalidOperationException($"Could not project generated Storefront DTO to {typeof(TTarget).Name}.");
        }

        private static SortBy MapSort(string? sortBy)
        {
            return sortBy switch
            {
                "oldest" => SortBy.Oldest,
                "priceLowToHigh" => SortBy.PriceLowToHigh,
                "priceHighToLow" => SortBy.PriceHighToLow,
                "nameAscending" => SortBy.NameAscending,
                "nameDescending" => SortBy.NameDescending,
                "displayOrder" => SortBy.DisplayOrder,
                "updated" => SortBy.Updated,
                _ => SortBy.Newest,
            };
        }

        private static StorefrontRuntimeError NotFound()
        {
            return new StorefrontRuntimeError(StorefrontRuntimeStatusCodes.NotFound, "http.404", "The requested storefront resource was not found.", null, EmptyFieldErrors());
        }

        private static StorefrontRuntimeError BadRequest(string message)
        {
            return new StorefrontRuntimeError(400, "request.invalid", message, null, EmptyFieldErrors());
        }

        private static StorefrontRuntimeError ServiceUnavailable(string? message = null)
        {
            return new StorefrontRuntimeError(
                StorefrontRuntimeStatusCodes.ServiceUnavailable,
                "storefront.unavailable",
                string.IsNullOrWhiteSpace(message) ? "The storefront service is unavailable." : message.Trim(),
                null,
                EmptyFieldErrors());
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyFieldErrors()
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
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
