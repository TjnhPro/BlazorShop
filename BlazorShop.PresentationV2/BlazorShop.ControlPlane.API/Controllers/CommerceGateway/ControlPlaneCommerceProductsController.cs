namespace BlazorShop.ControlPlane.API.Controllers
{
    using System.Globalization;
    using System.Text;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.ControlPlane.API.Responses;
    using BlazorShop.Domain.Contracts;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    [ApiController]
    [Route("api/control-plane/stores/{storePublicId:guid}/catalog")]
    [Authorize(Policy = ControlPlanePolicyNames.StoresRead)]
    public sealed class ControlPlaneCommerceProductsController : ControlPlaneCommerceGatewayControllerBase
    {
        private readonly BlazorShop.Application.ControlPlane.CommerceGateway.Products.IControlPlaneProductGateway gateway;

        public ControlPlaneCommerceProductsController(BlazorShop.Application.ControlPlane.CommerceGateway.Products.IControlPlaneProductGateway gateway)
        {
            this.gateway = gateway;
        }

        [HttpGet("products")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/products")]
        public async Task<IActionResult> QueryProducts(
            Guid storePublicId,
            [FromQuery] ProductCatalogQuery query,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.QueryProductsAsync(storePublicId, query, cancellationToken));
        }

        [HttpGet("products/{productId:guid}")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}")]
        public async Task<IActionResult> GetProduct(Guid storePublicId, Guid productId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.GetProductAsync(storePublicId, productId, cancellationToken));
        }

        [HttpPost("products")]
        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/products")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CreateProduct(
            Guid storePublicId,
            [FromBody] CreateProduct request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.CreateProductAsync(storePublicId, request, cancellationToken));
        }

        [HttpPut("products/{productId:guid}")]
        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateProduct(
            Guid storePublicId,
            Guid productId,
            [FromBody] UpdateProduct request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateProductAsync(storePublicId, productId, request, cancellationToken));
        }

        [HttpDelete("products/{productId:guid}")]
        [HttpDelete("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> ArchiveProduct(Guid storePublicId, Guid productId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ArchiveProductAsync(storePublicId, productId, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/seo")]
        public async Task<IActionResult> GetProductSeo(Guid storePublicId, Guid productId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.GetProductSeoAsync(storePublicId, productId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/seo")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateProductSeo(
            Guid storePublicId,
            Guid productId,
            [FromBody] UpdateProductSeoDto request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateProductSeoAsync(storePublicId, productId, request, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/seo/slugs/generate")]
        public async Task<IActionResult> GenerateSeoSlug(
            Guid storePublicId,
            [FromBody] StoreSeoSlugGenerateRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.GenerateSeoSlugAsync(storePublicId, request, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/seo/slugs/validate")]
        public async Task<IActionResult> ValidateSeoSlug(
            Guid storePublicId,
            [FromBody] StoreSeoSlugValidateRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ValidateSeoSlugAsync(storePublicId, request, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/seo/slugs/history")]
        public async Task<IActionResult> ListSeoSlugHistory(
            Guid storePublicId,
            [FromQuery] StoreSeoSlugHistoryQuery query,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ListSeoSlugHistoryAsync(storePublicId, query, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/product-imports/template")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/product-imports/template")]
        public IActionResult DownloadProductImportTemplate(Guid? storePublicId = null)
        {
            _ = storePublicId;
            return this.File(
                Encoding.UTF8.GetBytes(ProductImportTemplateHeader + Environment.NewLine),
                "text/csv",
                "product-import-template.csv");
        }

        [HttpPost("products/import")]
        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/product-imports")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadProductImport(
            Guid storePublicId,
            IFormFile file,
            [FromForm] string? mode,
            CancellationToken cancellationToken)
        {
            await using var stream = file.OpenReadStream();
            return ToActionResult(await this.gateway.UploadProductImportAsync(
                storePublicId,
                new ProductImportUploadRequest(file.FileName, mode, stream, file.Length, this.User.Identity?.Name),
                cancellationToken));
        }

        [HttpGet("products/imports")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/product-imports")]
        public async Task<IActionResult> ListProductImports(
            Guid storePublicId,
            [FromQuery] ProductImportJobListQuery query,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ListProductImportsAsync(storePublicId, query, cancellationToken));
        }

        [HttpGet("products/imports/{jobPublicId:guid}")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/product-imports/{jobPublicId:guid}")]
        public async Task<IActionResult> GetProductImport(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.GetProductImportAsync(storePublicId, jobPublicId, cancellationToken));
        }

        [HttpGet("products/imports/{jobPublicId:guid}/rows")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/product-imports/{jobPublicId:guid}/rows")]
        public async Task<IActionResult> ListProductImportRows(
            Guid storePublicId,
            Guid jobPublicId,
            [FromQuery] ProductImportRowsQuery query,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ListProductImportRowsAsync(storePublicId, jobPublicId, query, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/product-imports/{jobPublicId:guid}/errors.csv")]
        public async Task<IActionResult> DownloadProductImportErrors(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken)
        {
            var jobResult = await this.gateway.GetProductImportAsync(storePublicId, jobPublicId, cancellationToken);
            if (!jobResult.Success || jobResult.Payload is null)
            {
                return ToActionResult(jobResult);
            }

            var result = await this.gateway.ListProductImportRowsAsync(
                storePublicId,
                jobPublicId,
                new ProductImportRowsQuery("failed", PageNumber: 1, PageSize: 100),
                cancellationToken);

            if (!result.Success || result.Payload is null)
            {
                return ToActionResult(result);
            }

            var csv = BuildProductImportErrorCsv(jobResult.Payload.Job, result.Payload.Items);
            return this.File(
                Encoding.UTF8.GetBytes(csv),
                "text/csv",
                $"product-import-{jobPublicId:D}-errors.csv");
        }

        [HttpPut("categories/{categoryId:guid}/media/primary")]
        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/categories/{categoryId:guid}/media/primary")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> SetCategoryPrimaryMedia(
            Guid storePublicId,
            Guid categoryId,
            [FromBody] SetCategoryPrimaryMediaRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.SetCategoryPrimaryMediaAsync(storePublicId, categoryId, request, cancellationToken));
        }

        [HttpDelete("categories/{categoryId:guid}/media/primary")]
        [HttpDelete("~/api/controlplane/commerce/stores/{storePublicId:guid}/categories/{categoryId:guid}/media/primary")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> ClearCategoryPrimaryMedia(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ClearCategoryPrimaryMediaAsync(storePublicId, categoryId, cancellationToken));
        }

        [HttpGet("products/{productId:guid}/variants")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/variants")]
        public async Task<IActionResult> ListVariants(
            Guid storePublicId,
            Guid productId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return ToActionResult(await this.gateway.ListVariantsAsync(storePublicId, productId, pageNumber, pageSize, cancellationToken));
        }

        [HttpPost("products/{productId:guid}/variants")]
        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/variants")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CreateVariant(
            Guid storePublicId,
            Guid productId,
            [FromBody] CreateProductVariant request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.CreateVariantAsync(storePublicId, productId, request, cancellationToken));
        }

        [HttpPut("products/{productId:guid}/variants/{variantId:guid}")]
        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/variants/{variantId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateVariant(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            [FromBody] UpdateProductVariant request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateVariantAsync(storePublicId, productId, variantId, request, cancellationToken));
        }

        [HttpDelete("products/{productId:guid}/variants/{variantId:guid}")]
        [HttpDelete("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/variants/{variantId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> DeleteVariant(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.DeleteVariantAsync(storePublicId, productId, variantId, cancellationToken));
        }

        [HttpGet("inventory")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/inventory")]
        public async Task<IActionResult> QueryInventory(
            Guid storePublicId,
            [FromQuery] AdminInventoryQueryDto query,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.QueryInventoryAsync(storePublicId, query, cancellationToken));
        }

        [HttpPut("inventory/products/{productId:guid}")]
        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/inventory")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateProductStock(
            Guid storePublicId,
            Guid productId,
            [FromBody] UpdateProductStockDto request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateProductStockAsync(storePublicId, productId, request, cancellationToken));
        }

        [HttpPut("inventory/variants/{variantId:guid}")]
        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/inventory/variants/{variantId:guid}")]
        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/variants/{variantId:guid}/inventory")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateVariantStock(
            Guid storePublicId,
            Guid? productId,
            Guid variantId,
            [FromBody] UpdateVariantStockDto request,
            CancellationToken cancellationToken)
        {
            _ = productId;
            return ToActionResult(await this.gateway.UpdateVariantStockAsync(storePublicId, variantId, request, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/variation-templates")]
        public async Task<IActionResult> ListVariationTemplates(
            Guid storePublicId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return ToActionResult(await this.gateway.ListVariationTemplatesAsync(storePublicId, new VariationTemplateListQuery(pageNumber, pageSize), cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/variation-templates")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CreateVariationTemplate(
            Guid storePublicId,
            [FromBody] CreateVariationTemplateRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.CreateVariationTemplateAsync(storePublicId, request, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/variation-templates/{templatePublicId:guid}")]
        public async Task<IActionResult> GetVariationTemplate(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.GetVariationTemplateAsync(storePublicId, templatePublicId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/variation-templates/{templatePublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateVariationTemplate(
            Guid storePublicId,
            Guid templatePublicId,
            [FromBody] UpdateVariationTemplateRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateVariationTemplateAsync(storePublicId, templatePublicId, request, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/variation-templates/{templatePublicId:guid}/options")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CreateVariationTemplateOption(
            Guid storePublicId,
            Guid templatePublicId,
            [FromBody] CreateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.CreateVariationTemplateOptionAsync(storePublicId, templatePublicId, request, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/variation-templates/{templatePublicId:guid}/options/{optionPublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateVariationTemplateOption(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            [FromBody] UpdateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateVariationTemplateOptionAsync(storePublicId, templatePublicId, optionPublicId, request, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/variation-templates/{templatePublicId:guid}/options/{optionPublicId:guid}/values")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CreateVariationTemplateValue(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            [FromBody] CreateVariationTemplateValueRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.CreateVariationTemplateValueAsync(storePublicId, templatePublicId, optionPublicId, request, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/variation-templates/{templatePublicId:guid}/options/{optionPublicId:guid}/values/{valuePublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateVariationTemplateValue(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            Guid valuePublicId,
            [FromBody] UpdateVariationTemplateValueRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateVariationTemplateValueAsync(storePublicId, templatePublicId, optionPublicId, valuePublicId, request, cancellationToken));
        }
    }
}
