extern alias ControlPlaneApi;

namespace BlazorShop.Tests.PresentationV2.ControlPlane
{
    using System.Text;

    using BlazorShop.Application.ControlPlane.CommerceGateway.Products;
    using ControlPlaneApi::BlazorShop.ControlPlane.API.Controllers;

    using Microsoft.AspNetCore.Mvc;
    using Moq;

    using Xunit;

    public sealed class ControlPlaneCommerceProductControllerTests
    {
        [Fact]
        public void DownloadProductImportTemplate_ReturnsCanonicalParserHeader()
        {
            var controller = new ControlPlaneCommerceProductsController(new Mock<IControlPlaneProductGateway>().Object);

            var result = Assert.IsType<FileContentResult>(controller.DownloadProductImportTemplate());
            var content = Encoding.UTF8.GetString(result.FileContents);

            Assert.Equal("text/csv", result.ContentType);
            Assert.Equal("product-import-template.csv", result.FileDownloadName);
            Assert.Equal(
                "sku,name,slug,category_slug,product_type,variation_template_slug,price,compare_price,quantity,is_published,available_start_utc,available_end_utc,gtin,barcode,manufacturer_part_number,condition,weight,length,width,height,short_description,description,image_urls" + Environment.NewLine,
                content);
            Assert.DoesNotContain("title", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("full_description", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DownloadProductImportTemplate_HasGlobalRoute()
        {
            var routes = typeof(ControlPlaneCommerceProductsController)
                .GetMethod(nameof(ControlPlaneCommerceProductsController.DownloadProductImportTemplate))!
                .GetCustomAttributes(typeof(HttpGetAttribute), inherit: false)
                .Cast<HttpGetAttribute>()
                .Select(attribute => attribute.Template)
                .ToArray();

            Assert.Contains("~/api/controlplane/commerce/product-imports/template", routes);
        }

    }
}
