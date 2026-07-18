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
    public sealed class ControlPlaneCommerceMediaController : ControlPlaneCommerceGatewayControllerBase
    {
        private readonly BlazorShop.Application.ControlPlane.CommerceGateway.Media.IControlPlaneMediaGateway gateway;

        public ControlPlaneCommerceMediaController(BlazorShop.Application.ControlPlane.CommerceGateway.Media.IControlPlaneMediaGateway gateway)
        {
            this.gateway = gateway;
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
            return ToActionResult(await this.gateway.ListProductMediaAsync(storePublicId, productId, new ProductMediaListQuery(pageNumber, pageSize), cancellationToken));
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
            return ToActionResult(await this.gateway.ImportProductMediaAsync(storePublicId, productId, request, cancellationToken));
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
            return ToActionResult(await this.gateway.UpdateProductMediaOrderAsync(storePublicId, productId, request, cancellationToken));
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
            return ToActionResult(await this.gateway.SetPrimaryProductMediaAsync(storePublicId, productId, mediaPublicId, cancellationToken));
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
            return ToActionResult(await this.gateway.DeleteProductMediaAsync(storePublicId, productId, mediaPublicId, cancellationToken));
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
            return ToActionResult(await this.gateway.RetryProductMediaAsync(storePublicId, productId, mediaPublicId, cancellationToken));
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
            var result = await this.gateway.GetProductMediaPreviewAsync(
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

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/media/assets")]
        public async Task<IActionResult> ListMediaAssets(
            Guid storePublicId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? search = null,
            [FromQuery] string? usageType = null,
            CancellationToken cancellationToken = default)
        {
            return ToActionResult(await this.gateway.ListMediaAssetsAsync(
                storePublicId,
                new CommerceMediaAssetListQuery(pageNumber, pageSize, search, usageType),
                cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/media/assets/{assetPublicId:guid}")]
        public async Task<IActionResult> GetMediaAsset(
            Guid storePublicId,
            Guid assetPublicId,
            CancellationToken cancellationToken = default)
        {
            return ToActionResult(await this.gateway.GetMediaAssetAsync(storePublicId, assetPublicId, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/media/assets")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadMediaAsset(
            Guid storePublicId,
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (file is null)
            {
                return ControlPlaneApiResponseWriter.Failure<object>(
                    StatusCodes.Status400BadRequest,
                    "Image file is required.");
            }

            await using var stream = file.OpenReadStream();
            return ToActionResult(await this.gateway.UploadMediaAssetAsync(
                storePublicId,
                new CommerceMediaAssetUploadRequest(stream, file.FileName, file.ContentType, file.Length),
                cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/media/assets/{assetPublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateMediaAssetMetadata(
            Guid storePublicId,
            Guid assetPublicId,
            CommerceMediaAssetMetadataRequest request,
            CancellationToken cancellationToken = default)
        {
            return ToActionResult(await this.gateway.UpdateMediaAssetMetadataAsync(
                storePublicId,
                assetPublicId,
                request,
                cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/media/assets/{assetPublicId:guid}/replace")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> ReplaceMediaAsset(
            Guid storePublicId,
            Guid assetPublicId,
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (file is null)
            {
                return ControlPlaneApiResponseWriter.Failure<object>(
                    StatusCodes.Status400BadRequest,
                    "Image file is required.");
            }

            await using var stream = file.OpenReadStream();
            return ToActionResult(await this.gateway.ReplaceMediaAssetAsync(
                storePublicId,
                assetPublicId,
                new CommerceMediaAssetUploadRequest(stream, file.FileName, file.ContentType, file.Length),
                cancellationToken));
        }

        [HttpDelete("~/api/controlplane/commerce/stores/{storePublicId:guid}/media/assets/{assetPublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> DeleteMediaAsset(
            Guid storePublicId,
            Guid assetPublicId,
            CancellationToken cancellationToken = default)
        {
            return ToActionResult(await this.gateway.DeleteMediaAssetAsync(storePublicId, assetPublicId, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/media/assets/{assetPublicId:guid}/preview")]
        public async Task<IActionResult> PreviewMediaAsset(
            Guid storePublicId,
            Guid assetPublicId,
            [FromQuery] string fileName,
            [FromQuery(Name = "w")] int? width,
            [FromQuery(Name = "h")] int? height,
            [FromQuery] string? fit,
            [FromQuery] string? format,
            [FromQuery(Name = "v")] long? version,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return ControlPlaneApiResponseWriter.Failure<object>(
                    StatusCodes.Status400BadRequest,
                    "Media asset file name is required.");
            }

            var result = await this.gateway.GetMediaAssetPreviewAsync(
                storePublicId,
                assetPublicId,
                fileName,
                new MediaAssetPreviewQuery(width, height, fit, format, version),
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
    }
}
