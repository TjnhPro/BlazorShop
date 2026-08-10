namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Components.Contracts.Catalog;
using BlazorShop.Storefront.Presentation.Contracts;
using BlazorShop.Storefront.Presentation.Models;
using BlazorShop.Storefront.Presentation.Services;
using BlazorShop.Storefront.Presentation.Services.Catalog;

using Moq;

using Xunit;

public sealed class StorefrontDiscountedProductRailPresentationTests
{
    [Fact]
    public void PresentationMappingIncludesDiscountedProductRailEndpoint()
    {
        var hostingSource = File.ReadAllText(RepositoryPath(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontPresentationApplicationBuilderExtensions.cs"));
        var endpointSource = File.ReadAllText(RepositoryPath(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontPresentationCatalogEndpoints.cs"));
        var serviceSource = File.ReadAllText(RepositoryPath(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Catalog/StorefrontDiscountedProductRailService.cs"));

        Assert.Contains("MapStorefrontPresentationCatalogEndpoints", hostingSource, StringComparison.Ordinal);
        Assert.Contains("MapGet(StorefrontDiscountedProductRailService.LocalRoute", endpointSource, StringComparison.Ordinal);
        Assert.Contains("StorefrontDiscountedProductRailResponse", serviceSource, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, StorefrontDiscountedProductRailService.DefaultLimit)]
    [InlineData(1, 1)]
    [InlineData(24, 24)]
    public void LimitValidationAcceptsDefaultAndSafeRange(int? requestedLimit, int expectedLimit)
    {
        var valid = StorefrontDiscountedProductRailService.TryNormalizeLimit(
            requestedLimit,
            out var limit,
            out var error);

        Assert.True(valid);
        Assert.Equal(expectedLimit, limit);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    public void LimitValidationRejectsOutOfRangeValues(int requestedLimit)
    {
        var valid = StorefrontDiscountedProductRailService.TryNormalizeLimit(
            requestedLimit,
            out _,
            out var error);

        Assert.False(valid);
        Assert.NotNull(error);
        Assert.False(error!.Success);
        Assert.Equal("validation_error", error.Code);
        Assert.Empty(error.Products);
    }

    [Fact]
    public async Task ServiceReturnsOnlyProductsWithComparePriceEvidence()
    {
        ProductCatalogQuery? observedQuery = null;
        var service = CreateService(
            StorefrontApiResult<PagedResult<GetCatalogProduct>>.Success(new PagedResult<GetCatalogProduct>
            {
                Items =
                [
                    Product("plain", price: 20m, comparePrice: null),
                    Product("discounted", price: 20m, comparePrice: 30m),
                    Product("not-discounted", price: 20m, comparePrice: 20m),
                ],
            }),
            query => observedQuery = query);

        var response = await service.GetAsync(6);

        var product = Assert.Single(response.Products);
        Assert.True(response.Success);
        Assert.Equal("Discounted", product.Name);
        Assert.Equal("USD 30.00", product.ComparePriceDisplay);
        Assert.NotNull(observedQuery);
        Assert.Equal(1, observedQuery!.PageNumber);
        Assert.Equal(24, observedQuery.PageSize);
        Assert.Equal(ProductCatalogSortBy.DisplayOrder, observedQuery.SortBy);
    }

    [Fact]
    public async Task ServiceReturnsEmptySuccessWhenNoComparePriceProductsExist()
    {
        var service = CreateService(
            StorefrontApiResult<PagedResult<GetCatalogProduct>>.Success(new PagedResult<GetCatalogProduct>
            {
                Items =
                [
                    Product("plain", price: 20m, comparePrice: null),
                    Product("same-price", price: 20m, comparePrice: 20m),
                ],
            }));

        var response = await service.GetAsync(6);

        Assert.True(response.Success);
        Assert.Empty(response.Products);
        Assert.Null(response.Code);
    }

    [Fact]
    public async Task ServiceMapsCatalogServiceUnavailableToRetryableError()
    {
        var service = CreateService(
            StorefrontApiResult<PagedResult<GetCatalogProduct>>.ServiceUnavailable());

        var response = await service.GetAsync(6);

        Assert.False(response.Success);
        Assert.Empty(response.Products);
        Assert.Equal("service_unavailable", response.Code);
        Assert.True(response.Retryable);
    }

    [Fact]
    public void PhaseDoesNotIntroduceDiscountCoreQueryOrCommerceNodeRoute()
    {
        var presentationSources = Directory.EnumerateFiles(
                RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation"),
                "*",
                SearchOption.AllDirectories)
            .Where(file => !file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .Where(file => Path.GetExtension(file) is ".cs" or ".razor")
            .Select(File.ReadAllText);
        var commerceNodeSources = Directory.EnumerateFiles(
                RepositoryPath("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API"),
                "*",
                SearchOption.AllDirectories)
            .Where(file => !file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .Where(file => Path.GetExtension(file) is ".cs")
            .Select(File.ReadAllText);

        Assert.DoesNotContain(presentationSources, source =>
            source.Contains("discountedOnly", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(commerceNodeSources, source =>
            source.Contains("discounted-products", StringComparison.OrdinalIgnoreCase) ||
            source.Contains("discountedOnly", StringComparison.OrdinalIgnoreCase));
    }

    private static StorefrontDiscountedProductRailService CreateService(
        StorefrontApiResult<PagedResult<GetCatalogProduct>> result,
        Action<ProductCatalogQuery>? observeQuery = null)
    {
        var catalog = new Mock<IStorefrontCatalogClient>(MockBehavior.Strict);
        catalog
            .Setup(client => client.GetPublishedCatalogPageAsync(
                It.IsAny<ProductCatalogQuery>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<ProductCatalogQuery, string?, CancellationToken>((query, _, _) => observeQuery?.Invoke(query))
            .ReturnsAsync(result);

        var displayContextProvider = new Mock<IStorefrontDisplayContextProvider>(MockBehavior.Strict);
        displayContextProvider
            .Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(StorefrontDisplayContext.Fallback);

        return new StorefrontDiscountedProductRailService(
            catalog.Object,
            displayContextProvider.Object,
            new StorefrontPriceFormatter());
    }

    private static GetCatalogProduct Product(string slug, decimal price, decimal? comparePrice)
    {
        return new GetCatalogProduct
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = slug.Replace('-', ' ').ToTitleCaseInvariant(),
            Price = price,
            ComparePrice = comparePrice,
            CreatedOn = DateTime.UtcNow,
            InStock = true,
            Purchasable = true,
            Quantity = 10,
        };
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}

internal static class StorefrontDiscountedProductRailTestStringExtensions
{
    public static string ToTitleCaseInvariant(this string value)
    {
        return string.Join(
            ' ',
            value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }
}
