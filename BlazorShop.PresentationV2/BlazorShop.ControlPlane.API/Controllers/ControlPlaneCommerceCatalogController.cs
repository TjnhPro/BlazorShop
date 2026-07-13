namespace BlazorShop.ControlPlane.API.Controllers
{
    using System.Globalization;
    using System.Text;
    using System.Text.Json;

    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
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
    public sealed class ControlPlaneCommerceCatalogController : ControllerBase
    {
        private readonly IControlPlaneCommerceCatalogService catalogService;

        public ControlPlaneCommerceCatalogController(IControlPlaneCommerceCatalogService catalogService)
        {
            this.catalogService = catalogService;
        }

        private const string ProductImportTemplateHeader = "sku,name,slug,category_slug,product_type,variation_template_slug,price,compare_price,quantity,is_published,short_description,description,image_urls";

        [HttpGet("products")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/products")]
        public async Task<IActionResult> QueryProducts(
            Guid storePublicId,
            [FromQuery] ProductCatalogQuery query,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.QueryProductsAsync(storePublicId, query, cancellationToken));
        }

        [HttpGet("products/{productId:guid}")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}")]
        public async Task<IActionResult> GetProduct(Guid storePublicId, Guid productId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.GetProductAsync(storePublicId, productId, cancellationToken));
        }

        [HttpPost("products")]
        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/products")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CreateProduct(
            Guid storePublicId,
            CreateProduct request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.CreateProductAsync(storePublicId, request, cancellationToken));
        }

        [HttpPut("products/{productId:guid}")]
        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateProduct(
            Guid storePublicId,
            Guid productId,
            UpdateProduct request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.UpdateProductAsync(storePublicId, productId, request, cancellationToken));
        }

        [HttpDelete("products/{productId:guid}")]
        [HttpDelete("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> ArchiveProduct(Guid storePublicId, Guid productId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.ArchiveProductAsync(storePublicId, productId, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/seo")]
        public async Task<IActionResult> GetProductSeo(Guid storePublicId, Guid productId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.GetProductSeoAsync(storePublicId, productId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/seo")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateProductSeo(
            Guid storePublicId,
            Guid productId,
            UpdateProductSeoDto request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.UpdateProductSeoAsync(storePublicId, productId, request, cancellationToken));
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
            return ToActionResult(await this.catalogService.UploadProductImportAsync(
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
            return ToActionResult(await this.catalogService.ListProductImportsAsync(storePublicId, query, cancellationToken));
        }

        [HttpGet("products/imports/{jobPublicId:guid}")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/product-imports/{jobPublicId:guid}")]
        public async Task<IActionResult> GetProductImport(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.GetProductImportAsync(storePublicId, jobPublicId, cancellationToken));
        }

        [HttpGet("products/imports/{jobPublicId:guid}/rows")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/product-imports/{jobPublicId:guid}/rows")]
        public async Task<IActionResult> ListProductImportRows(
            Guid storePublicId,
            Guid jobPublicId,
            [FromQuery] ProductImportRowsQuery query,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.ListProductImportRowsAsync(storePublicId, jobPublicId, query, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/product-imports/{jobPublicId:guid}/errors.csv")]
        public async Task<IActionResult> DownloadProductImportErrors(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken)
        {
            var jobResult = await this.catalogService.GetProductImportAsync(storePublicId, jobPublicId, cancellationToken);
            if (!jobResult.Success || jobResult.Payload is null)
            {
                return ToActionResult(jobResult);
            }

            var result = await this.catalogService.ListProductImportRowsAsync(
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

        [HttpGet("products/{productId:guid}/media")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/media")]
        public async Task<IActionResult> ListProductMedia(
            Guid storePublicId,
            Guid productId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return ToActionResult(await this.catalogService.ListProductMediaAsync(storePublicId, productId, new ProductMediaListQuery(pageNumber, pageSize), cancellationToken));
        }

        [HttpPost("products/{productId:guid}/media/import")]
        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/media/import")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> ImportProductMedia(
            Guid storePublicId,
            Guid productId,
            ImportProductMediaRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.ImportProductMediaAsync(storePublicId, productId, request, cancellationToken));
        }

        [HttpPut("products/{productId:guid}/media/order")]
        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/media/order")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateProductMediaOrder(
            Guid storePublicId,
            Guid productId,
            UpdateProductMediaOrderRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.UpdateProductMediaOrderAsync(storePublicId, productId, request, cancellationToken));
        }

        [HttpPost("products/{productId:guid}/media/{mediaPublicId:guid}/primary")]
        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/media/{mediaPublicId:guid}/primary")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> SetPrimaryProductMedia(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.SetPrimaryProductMediaAsync(storePublicId, productId, mediaPublicId, cancellationToken));
        }

        [HttpDelete("products/{productId:guid}/media/{mediaPublicId:guid}")]
        [HttpDelete("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/media/{mediaPublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> DeleteProductMedia(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.DeleteProductMediaAsync(storePublicId, productId, mediaPublicId, cancellationToken));
        }

        [HttpPost("products/{productId:guid}/media/{mediaPublicId:guid}/retry")]
        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/media/{mediaPublicId:guid}/retry")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> RetryProductMedia(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.RetryProductMediaAsync(storePublicId, productId, mediaPublicId, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/media/{mediaPublicId:guid}/preview")]
        public async Task<IActionResult> PreviewProductMedia(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            [FromQuery(Name = "w")] int? width,
            [FromQuery(Name = "h")] int? height,
            [FromQuery] string? fit,
            [FromQuery] string? format,
            [FromQuery(Name = "v")] int? version,
            CancellationToken cancellationToken)
        {
            _ = productId;
            var result = await this.catalogService.GetProductMediaPreviewAsync(
                storePublicId,
                mediaPublicId,
                new ProductMediaPreviewQuery(width, height, fit, format, version),
                cancellationToken);

            if (!result.Success || result.Content is null)
            {
                return ToActionResult(new ControlPlaneCommerceCatalogResult<object>(
                    false,
                    result.Message,
                    Failure: result.Failure,
                    HttpStatusCode: result.HttpStatusCode));
            }

            return this.File(result.Content, result.ContentType ?? "application/octet-stream");
        }

        [HttpGet("categories")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/categories")]
        public async Task<IActionResult> ListCategories(
            Guid storePublicId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return ToActionResult(await this.catalogService.ListCategoriesAsync(storePublicId, pageNumber, pageSize, cancellationToken));
        }

        [HttpGet("categories/tree")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/categories/tree")]
        public async Task<IActionResult> GetCategoryTree(Guid storePublicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.GetCategoryTreeAsync(storePublicId, cancellationToken));
        }

        [HttpPost("categories")]
        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/categories")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CreateCategory(
            Guid storePublicId,
            CreateCategory request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.CreateCategoryAsync(storePublicId, request, cancellationToken));
        }

        [HttpPut("categories/{categoryId:guid}")]
        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/categories/{categoryId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateCategory(
            Guid storePublicId,
            Guid categoryId,
            UpdateCategory request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.UpdateCategoryAsync(storePublicId, categoryId, request, cancellationToken));
        }

        [HttpDelete("categories/{categoryId:guid}")]
        [HttpDelete("~/api/controlplane/commerce/stores/{storePublicId:guid}/categories/{categoryId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> ArchiveCategory(Guid storePublicId, Guid categoryId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.ArchiveCategoryAsync(storePublicId, categoryId, cancellationToken));
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
            return ToActionResult(await this.catalogService.ListVariantsAsync(storePublicId, productId, pageNumber, pageSize, cancellationToken));
        }

        [HttpPost("products/{productId:guid}/variants")]
        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/variants")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CreateVariant(
            Guid storePublicId,
            Guid productId,
            CreateProductVariant request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.CreateVariantAsync(storePublicId, productId, request, cancellationToken));
        }

        [HttpPut("products/{productId:guid}/variants/{variantId:guid}")]
        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/variants/{variantId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateVariant(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            UpdateProductVariant request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.UpdateVariantAsync(storePublicId, productId, variantId, request, cancellationToken));
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
            return ToActionResult(await this.catalogService.DeleteVariantAsync(storePublicId, productId, variantId, cancellationToken));
        }

        [HttpGet("inventory")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/inventory")]
        public async Task<IActionResult> QueryInventory(
            Guid storePublicId,
            [FromQuery] AdminInventoryQueryDto query,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.QueryInventoryAsync(storePublicId, query, cancellationToken));
        }

        [HttpPut("inventory/products/{productId:guid}")]
        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/inventory")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateProductStock(
            Guid storePublicId,
            Guid productId,
            UpdateProductStockDto request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.UpdateProductStockAsync(storePublicId, productId, request, cancellationToken));
        }

        [HttpPut("inventory/variants/{variantId:guid}")]
        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/inventory/variants/{variantId:guid}")]
        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/products/{productId:guid}/variants/{variantId:guid}/inventory")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateVariantStock(
            Guid storePublicId,
            Guid? productId,
            Guid variantId,
            UpdateVariantStockDto request,
            CancellationToken cancellationToken)
        {
            _ = productId;
            return ToActionResult(await this.catalogService.UpdateVariantStockAsync(storePublicId, variantId, request, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/variation-templates")]
        public async Task<IActionResult> ListVariationTemplates(
            Guid storePublicId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return ToActionResult(await this.catalogService.ListVariationTemplatesAsync(storePublicId, new VariationTemplateListQuery(pageNumber, pageSize), cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/variation-templates")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CreateVariationTemplate(
            Guid storePublicId,
            CreateVariationTemplateRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.CreateVariationTemplateAsync(storePublicId, request, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/variation-templates/{templatePublicId:guid}")]
        public async Task<IActionResult> GetVariationTemplate(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.GetVariationTemplateAsync(storePublicId, templatePublicId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/variation-templates/{templatePublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateVariationTemplate(
            Guid storePublicId,
            Guid templatePublicId,
            UpdateVariationTemplateRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.UpdateVariationTemplateAsync(storePublicId, templatePublicId, request, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/variation-templates/{templatePublicId:guid}/options")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CreateVariationTemplateOption(
            Guid storePublicId,
            Guid templatePublicId,
            CreateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.CreateVariationTemplateOptionAsync(storePublicId, templatePublicId, request, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/variation-templates/{templatePublicId:guid}/options/{optionPublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateVariationTemplateOption(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            UpdateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.UpdateVariationTemplateOptionAsync(storePublicId, templatePublicId, optionPublicId, request, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/variation-templates/{templatePublicId:guid}/options/{optionPublicId:guid}/values")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CreateVariationTemplateValue(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            CreateVariationTemplateValueRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.CreateVariationTemplateValueAsync(storePublicId, templatePublicId, optionPublicId, request, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/variation-templates/{templatePublicId:guid}/options/{optionPublicId:guid}/values/{valuePublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateVariationTemplateValue(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            Guid valuePublicId,
            UpdateVariationTemplateValueRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.UpdateVariationTemplateValueAsync(storePublicId, templatePublicId, optionPublicId, valuePublicId, request, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/pages")]
        [Authorize(Policy = ControlPlanePolicyNames.CommercePagesRead)]
        public async Task<IActionResult> ListStorefrontPages(
            Guid storePublicId,
            [FromQuery] StorefrontPageListQuery query,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.ListStorefrontPagesAsync(storePublicId, query, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/pages")]
        [Authorize(Policy = ControlPlanePolicyNames.CommercePagesWrite)]
        public async Task<IActionResult> CreateStorefrontPage(
            Guid storePublicId,
            CreateStorefrontPageRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.CreateStorefrontPageAsync(storePublicId, request, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/pages/{pagePublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.CommercePagesRead)]
        public async Task<IActionResult> GetStorefrontPage(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.GetStorefrontPageAsync(storePublicId, pagePublicId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/pages/{pagePublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.CommercePagesWrite)]
        public async Task<IActionResult> UpdateStorefrontPage(
            Guid storePublicId,
            Guid pagePublicId,
            UpdateStorefrontPageRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.UpdateStorefrontPageAsync(storePublicId, pagePublicId, request, cancellationToken));
        }

        [HttpDelete("~/api/controlplane/commerce/stores/{storePublicId:guid}/pages/{pagePublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.CommercePagesWrite)]
        public async Task<IActionResult> ArchiveStorefrontPage(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.ArchiveStorefrontPageAsync(storePublicId, pagePublicId, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/orders")]
        public async Task<IActionResult> QueryOrders(
            Guid storePublicId,
            [FromQuery] AdminOrderQueryDto query,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.QueryOrdersAsync(storePublicId, query, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/orders/{orderId:guid}")]
        public async Task<IActionResult> GetOrder(Guid storePublicId, Guid orderId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.GetOrderAsync(storePublicId, orderId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/orders/{orderId:guid}/admin-note")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateOrderAdminNote(
            Guid storePublicId,
            Guid orderId,
            UpdateOrderAdminNoteRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.UpdateOrderAdminNoteAsync(storePublicId, orderId, request, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/orders/{orderId:guid}/shipping-status")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateOrderShippingStatus(
            Guid storePublicId,
            Guid orderId,
            UpdateShippingStatusRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.UpdateOrderShippingStatusAsync(storePublicId, orderId, request, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/orders/{orderId:guid}/complete")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CompleteOrder(Guid storePublicId, Guid orderId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.CompleteOrderAsync(storePublicId, orderId, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/orders/{orderId:guid}/cancel")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CancelOrder(Guid storePublicId, Guid orderId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.CancelOrderAsync(storePublicId, orderId, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/payment-methods")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresRead)]
        public async Task<IActionResult> ListPaymentMethods(Guid storePublicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.ListPaymentMethodsAsync(storePublicId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/payment-methods/{paymentMethodKey}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdatePaymentMethod(
            Guid storePublicId,
            string paymentMethodKey,
            UpdateStorePaymentMethodRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.UpdatePaymentMethodAsync(storePublicId, paymentMethodKey, request, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/orders/{orderId:guid}/shipment")]
        public async Task<IActionResult> GetShipment(Guid storePublicId, Guid orderId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.GetShipmentAsync(storePublicId, orderId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/orders/{orderId:guid}/shipment")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpsertShipment(
            Guid storePublicId,
            Guid orderId,
            UpsertShipmentRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.UpsertShipmentAsync(storePublicId, orderId, request, cancellationToken));
        }

        private static IActionResult ToActionResult<TPayload>(ControlPlaneCommerceCatalogResult<TPayload> result)
        {
            if (result.Success)
            {
                return ControlPlaneApiResponseWriter.Success(
                    StatusCodes.Status200OK,
                    result.Payload,
                    string.IsNullOrWhiteSpace(result.Message) ? "Catalog request completed." : result.Message);
            }

            return result.Failure switch
            {
                ControlPlaneCommerceCatalogFailure.NotFound => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status404NotFound, result.Message, result.Payload),
                ControlPlaneCommerceCatalogFailure.RemoteFailure => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status502BadGateway, result.Message, result.Payload),
                ControlPlaneCommerceCatalogFailure.Validation => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status400BadRequest, result.Message, result.Payload),
                _ => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status400BadRequest, result.Message, result.Payload),
            };
        }

        private static string BuildProductImportErrorCsv(ProductImportJobDto job, IReadOnlyList<ProductImportRowDto> rows)
        {
            var builder = new StringBuilder();
            builder.AppendLine("row_number,sku,status,error_column,error_message,error_json");
            if (rows.Count == 0 && !string.IsNullOrWhiteSpace(job.ErrorMessage))
            {
                builder.AppendLine(string.Join(
                    ",",
                    Csv(string.Empty),
                    Csv(string.Empty),
                    Csv(job.Status),
                    Csv("file"),
                    Csv(job.ErrorMessage),
                    Csv(job.ErrorJson)));
                return builder.ToString();
            }

            foreach (var row in rows)
            {
                var errors = ExtractErrors(row);
                if (errors.Count == 0)
                {
                    builder.AppendLine(string.Join(
                        ",",
                        Csv(row.RowNumber.ToString(CultureInfo.InvariantCulture)),
                        Csv(row.Sku),
                        Csv(row.Status),
                        Csv(string.Empty),
                        Csv(row.ErrorMessage),
                        Csv(row.ErrorJson)));
                    continue;
                }

                foreach (var error in errors)
                {
                    builder.AppendLine(string.Join(
                        ",",
                        Csv(row.RowNumber.ToString(CultureInfo.InvariantCulture)),
                        Csv(row.Sku),
                        Csv(row.Status),
                        Csv(error.Column),
                        Csv(error.Message),
                        Csv(row.ErrorJson)));
                }
            }

            return builder.ToString();
        }

        private static IReadOnlyList<(string Column, string Message)> ExtractErrors(ProductImportRowDto row)
        {
            if (string.IsNullOrWhiteSpace(row.ErrorJson))
            {
                return string.IsNullOrWhiteSpace(row.ErrorMessage)
                    ? []
                    : [(string.Empty, row.ErrorMessage)];
            }

            try
            {
                using var document = JsonDocument.Parse(row.ErrorJson);
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return document.RootElement
                        .EnumerateArray()
                        .Select(ReadError)
                        .Where(error => !string.IsNullOrWhiteSpace(error.Column) || !string.IsNullOrWhiteSpace(error.Message))
                        .ToArray();
                }

                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    return [ReadError(document.RootElement)];
                }
            }
            catch (JsonException)
            {
                return [(string.Empty, row.ErrorMessage ?? row.ErrorJson)];
            }

            return [(string.Empty, row.ErrorMessage ?? row.ErrorJson)];
        }

        private static (string Column, string Message) ReadError(JsonElement element)
        {
            return (
                ReadString(element, "column") ?? ReadString(element, "Column") ?? string.Empty,
                ReadString(element, "message") ?? ReadString(element, "Message") ?? element.ToString());
        }

        private static string? ReadString(JsonElement element, string propertyName)
        {
            return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var property)
                ? property.GetString()
                : null;
        }

        private static string Csv(string? value)
        {
            var normalized = value ?? string.Empty;
            return "\"" + normalized.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }
    }
}
