namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using System.Text;

    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Xunit;

    public sealed class ProductImportCsvParserTests
    {
        [Fact]
        public async Task ParseAsync_WhenAvailabilityColumnsArePresent_PreservesValues()
        {
            const string content = """
                sku,name,slug,category_slug,product_type,variation_template_slug,price,compare_price,quantity,is_published,available_start_utc,available_end_utc,short_description,description,image_urls
                SKU-1,Product,product,default,simple,,10,,5,true,2026-08-01T00:00:00Z,2026-09-01T00:00:00Z,Short,Description,
                """;
            var parser = new ProductImportCsvParser();

            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content.Replace("\r\n", "\n", StringComparison.Ordinal)));
            var result = await parser.ParseAsync(stream, maxRows: 10);

            Assert.Empty(result.Errors);
            var row = Assert.Single(result.Rows);
            Assert.Equal("2026-08-01T00:00:00Z", row.Values["available_start_utc"]);
            Assert.Equal("2026-09-01T00:00:00Z", row.Values["available_end_utc"]);
        }

        [Fact]
        public async Task ParseAsync_WhenAvailabilityColumnsAreMissing_DoesNotRequireThem()
        {
            const string content = """
                sku,name,slug,category_slug,product_type,variation_template_slug,price,compare_price,quantity,is_published,short_description,description,image_urls
                SKU-1,Product,product,default,simple,,10,,5,true,Short,Description,
                """;
            var parser = new ProductImportCsvParser();

            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content.Replace("\r\n", "\n", StringComparison.Ordinal)));
            var result = await parser.ParseAsync(stream, maxRows: 10);

            Assert.Empty(result.Errors);
            Assert.Single(result.Rows);
        }
    }
}
