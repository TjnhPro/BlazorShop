namespace BlazorShop.Tests.Domain.Contracts
{
    using BlazorShop.Domain.Contracts;

    using Xunit;

    public sealed class ProductCatalogQueryTests
    {
        [Fact]
        public void NormalizedPaging_ClampsUnsafeValues()
        {
            var query = new ProductCatalogQuery
            {
                PageNumber = 0,
                PageSize = 250,
            };

            Assert.Equal(1, query.GetNormalizedPageNumber());
            Assert.Equal(100, query.GetNormalizedPageSize());
        }

        [Fact]
        public void NormalizedPaging_KeepsExplicitSafeValues()
        {
            var query = new ProductCatalogQuery
            {
                PageNumber = 3,
                PageSize = 48,
            };

            Assert.Equal(3, query.GetNormalizedPageNumber());
            Assert.Equal(48, query.GetNormalizedPageSize());
        }

        [Fact]
        public void NormalizedTerms_TrimSearchAndCategorySlug()
        {
            var query = new ProductCatalogQuery
            {
                SearchTerm = "  running \t  shoes  ",
                CategorySlug = "  apparel  ",
            };

            Assert.Equal("running shoes", query.GetNormalizedSearchTerm());
            Assert.Equal("apparel", query.GetNormalizedCategorySlug());
        }

        [Fact]
        public void NormalizedTerms_ReturnNullForBlankValues()
        {
            var query = new ProductCatalogQuery
            {
                SearchTerm = "   ",
                CategorySlug = "\t",
            };

            Assert.Null(query.GetNormalizedSearchTerm());
            Assert.Null(query.GetNormalizedCategorySlug());
        }
    }
}
