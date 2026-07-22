extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using BlazorShop.Web.SharedV2.Models.Product;

    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Services;

    public sealed class StorefrontRoutesTests
    {
        [Fact]
        public void SearchUrl_PreservesCatalogQueryState()
        {
            var url = StorefrontRoutes.SearchUrl(
                " trail runner ",
                " shoes & boots ",
                3,
                48,
                ProductCatalogSortBy.PriceHighToLow,
                10.5m,
                99.99m,
                true);

            Assert.Equal(
                "/search?q=trail%20runner&category=shoes%20%26%20boots&page=3&pageSize=48&sortBy=priceHighToLow&minPrice=10.5&maxPrice=99.99&inStock=true",
                url);
        }

        [Fact]
        public void SearchUrl_OmitsDefaultPageAndEmptyValues()
        {
            var url = StorefrontRoutes.SearchUrl(
                "  ",
                null,
                1,
                null,
                ProductCatalogSortBy.DisplayOrder,
                null,
                null,
                false);

            Assert.Equal("/search?sortBy=displayOrder", url);
        }

        [Fact]
        public void CategoryUrl_PreservesCatalogQueryState()
        {
            var url = StorefrontRoutes.CategoryUrl(
                "apparel sale",
                2,
                12,
                ProductCatalogSortBy.Updated,
                5m,
                150m,
                true);

            Assert.Equal(
                "/category/apparel%20sale?page=2&pageSize=12&sortBy=updated&minPrice=5&maxPrice=150&inStock=true",
                url);
        }

        [Fact]
        public void CategoryUrl_OmitsQueryStringWhenNoFiltersAreProvided()
        {
            Assert.Equal("/category/apparel", StorefrontRoutes.CategoryUrl("apparel"));
        }
    }
}
