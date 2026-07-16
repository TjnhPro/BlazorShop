extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using System.Reflection;

    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Domain.Constants;
    using CommerceNodeApi::BlazorShop.CommerceNode.API.Tasks;

    using Xunit;

    public sealed class CatalogProductTypeGateTests
    {
        [Fact]
        public void ProductTypes_AllContainsOnlyImplementedMvpTypes()
        {
            var expected = new HashSet<string>(
                [ProductTypes.Simple, ProductTypes.VariantInventory, ProductTypes.CustomVariations],
                StringComparer.OrdinalIgnoreCase);

            Assert.True(expected.SetEquals(ProductTypes.All));
        }

        [Fact]
        public void ProductImportResolver_RejectsUnsupportedProductType()
        {
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["product_type"] = "Bundle",
            };
            var errors = new List<ProductImportError>();
            var method = typeof(ProductImportTaskHandler).GetMethod("ResolveProductType", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("ResolveProductType was not found.");

            var result = method.Invoke(null, [values, null, true, errors]);

            Assert.Null(result);
            var error = Assert.Single(errors);
            Assert.Equal("product_type", error.Column);
            Assert.Equal("Product type is invalid.", error.Message);
        }
    }
}
