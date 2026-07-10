namespace BlazorShop.ControlPlane.API.Controllers
{
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
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

        [HttpGet("products")]
        public async Task<IActionResult> QueryProducts(
            Guid storePublicId,
            [FromQuery] ProductCatalogQuery query,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.QueryProductsAsync(storePublicId, query, cancellationToken));
        }

        [HttpGet("products/{productId:guid}")]
        public async Task<IActionResult> GetProduct(Guid storePublicId, Guid productId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.GetProductAsync(storePublicId, productId, cancellationToken));
        }

        [HttpPost("products")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CreateProduct(
            Guid storePublicId,
            CreateProduct request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.CreateProductAsync(storePublicId, request, cancellationToken));
        }

        [HttpPut("products/{productId:guid}")]
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
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> ArchiveProduct(Guid storePublicId, Guid productId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.ArchiveProductAsync(storePublicId, productId, cancellationToken));
        }

        [HttpGet("products/{productId:guid}/media")]
        public async Task<IActionResult> ListProductMedia(Guid storePublicId, Guid productId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.ListProductMediaAsync(storePublicId, productId, cancellationToken));
        }

        [HttpPost("products/{productId:guid}/media/import")]
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
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> RetryProductMedia(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.RetryProductMediaAsync(storePublicId, productId, mediaPublicId, cancellationToken));
        }

        [HttpGet("categories")]
        public async Task<IActionResult> ListCategories(Guid storePublicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.ListCategoriesAsync(storePublicId, cancellationToken));
        }

        [HttpGet("categories/tree")]
        public async Task<IActionResult> GetCategoryTree(Guid storePublicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.GetCategoryTreeAsync(storePublicId, cancellationToken));
        }

        [HttpPost("categories")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CreateCategory(
            Guid storePublicId,
            CreateCategory request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.CreateCategoryAsync(storePublicId, request, cancellationToken));
        }

        [HttpPut("categories/{categoryId:guid}")]
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
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> ArchiveCategory(Guid storePublicId, Guid categoryId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.ArchiveCategoryAsync(storePublicId, categoryId, cancellationToken));
        }

        [HttpGet("products/{productId:guid}/variants")]
        public async Task<IActionResult> ListVariants(Guid storePublicId, Guid productId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.ListVariantsAsync(storePublicId, productId, cancellationToken));
        }

        [HttpPost("products/{productId:guid}/variants")]
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
        public async Task<IActionResult> QueryInventory(
            Guid storePublicId,
            [FromQuery] AdminInventoryQueryDto query,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.QueryInventoryAsync(storePublicId, query, cancellationToken));
        }

        [HttpPut("inventory/products/{productId:guid}")]
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
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateVariantStock(
            Guid storePublicId,
            Guid variantId,
            UpdateVariantStockDto request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.catalogService.UpdateVariantStockAsync(storePublicId, variantId, request, cancellationToken));
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
    }
}
