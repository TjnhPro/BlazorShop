namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;

    [ApiController]
    [Route("media/assets")]
    public sealed class CommerceMediaAssetPublicController : ControllerBase
    {
        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly CommerceMediaStorageOptions options;
        private readonly IWebHostEnvironment environment;
        private readonly IHttpClientFactory httpClientFactory;

        public CommerceMediaAssetPublicController(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            IOptions<CommerceMediaStorageOptions> options,
            IWebHostEnvironment environment,
            IHttpClientFactory httpClientFactory)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.options = options.Value;
            this.environment = environment;
            this.httpClientFactory = httpClientFactory;
        }

        [HttpGet("/api/commerce/admin/media/assets/{assetPublicId:guid}/preview")]
        public async Task<IActionResult> GetAdminDebug(
            Guid assetPublicId,
            [FromQuery] int? w,
            [FromQuery] int? width,
            [FromQuery] int? h,
            [FromQuery] int? height,
            [FromQuery] string? fit,
            [FromQuery] string? format,
            [FromQuery] long? v,
            CancellationToken cancellationToken)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success || storeResult.Payload == Guid.Empty)
            {
                return this.NotFound();
            }

            var asset = await this.context.CommerceMediaAssets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.PublicId == assetPublicId && item.StoreId == storeResult.Payload,
                    cancellationToken);

            if (asset is null)
            {
                return this.NotFound();
            }

            return await this.Get(
                assetPublicId,
                asset.CanonicalFileName,
                w,
                width,
                h,
                height,
                fit,
                format,
                v,
                cancellationToken);
        }

        [HttpGet("{assetPublicId:guid}/{fileName}")]
        public async Task<IActionResult> Get(
            Guid assetPublicId,
            string fileName,
            [FromQuery] int? w,
            [FromQuery] int? width,
            [FromQuery] int? h,
            [FromQuery] int? height,
            [FromQuery] string? fit,
            [FromQuery] string? format,
            [FromQuery] long? v,
            CancellationToken cancellationToken)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success || storeResult.Payload == Guid.Empty)
            {
                return this.NotFound();
            }

            var asset = await this.context.CommerceMediaAssets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.PublicId == assetPublicId && item.StoreId == storeResult.Payload,
                    cancellationToken);

            if (asset is null)
            {
                return this.NotFound();
            }

            if (!asset.CanonicalFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                return this.RedirectPermanentPreserveMethod(this.BuildCanonicalRequestPath(asset));
            }

            var hasTransformQuery = w is not null
                || width is not null
                || h is not null
                || height is not null
                || !string.IsNullOrWhiteSpace(fit)
                || !string.IsNullOrWhiteSpace(format);

            var query = MediaTransformPolicy.NormalizeAssetQuery(
                width ?? w,
                height ?? h,
                fit,
                format,
                asset.Width,
                asset.Height,
                hasTransformQuery);
            if (!query.Success)
            {
                return this.BadRequest(query.Message);
            }

            this.SetCacheHeaders(asset, query.Value, v);

            if (!hasTransformQuery)
            {
                return this.ServeOriginal(asset);
            }

            if (asset.Extension.Equals("gif", StringComparison.OrdinalIgnoreCase)
                || asset.Extension.Equals("ico", StringComparison.OrdinalIgnoreCase))
            {
                return this.BadRequest("GIF and ICO assets do not support transform queries.");
            }

            if (!this.options.UseImgproxy || string.IsNullOrWhiteSpace(this.options.ImgproxyBaseUrl))
            {
                return this.StatusCode(StatusCodes.Status503ServiceUnavailable, "Image processor is not configured.");
            }

            var imgproxyUrl = this.BuildImgproxyUrl(asset, query.Value);
            using var response = await this.httpClientFactory.CreateClient().GetAsync(imgproxyUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return this.StatusCode(StatusCodes.Status502BadGateway, "Image processor could not render media.");
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? ToContentType(GetOutputFormat(asset, query.Value));
            return this.File(bytes, contentType);
        }

        private IActionResult ServeOriginal(CommerceMediaAsset asset)
        {
            var physicalPath = this.ResolvePhysicalPath(asset.OriginalStoragePath);
            if (!System.IO.File.Exists(physicalPath))
            {
                return this.NotFound();
            }

            return this.PhysicalFile(physicalPath, asset.MimeType);
        }

        private void SetCacheHeaders(CommerceMediaAsset asset, MediaTransformQuery query, long? requestedVersion)
        {
            var hasVersion = requestedVersion is > 0;
            this.Response.Headers.CacheControl = hasVersion
                ? "public, max-age=31536000, immutable"
                : "public, max-age=3600";
            this.Response.Headers.XContentTypeOptions = "nosniff";
            this.Response.Headers.ETag = $"\"{asset.PublicId:D}:{query.Width}:{query.Height}:{query.Fit}:{query.Format}:{requestedVersion ?? asset.UpdatedAt.ToUnixTimeMilliseconds()}\"";
        }

        private string BuildCanonicalRequestPath(CommerceMediaAsset asset)
        {
            var path = $"/media/assets/{asset.PublicId:D}/{Uri.EscapeDataString(asset.CanonicalFileName)}";
            return this.Request.QueryString.HasValue ? path + this.Request.QueryString.Value : path;
        }

        private string BuildImgproxyUrl(CommerceMediaAsset asset, MediaTransformQuery query)
        {
            var normalizedBaseUrl = this.options.ImgproxyBaseUrl!.TrimEnd('/');
            var imgproxyFit = MediaTransformPolicy.AssetImgproxyFitByRequestFit[query.Fit];
            var imgproxyPath = this.BuildImgproxyLocalPath(asset.OriginalStoragePath);
            var source = Uri.EscapeDataString($"local:///{imgproxyPath}");
            return $"{normalizedBaseUrl}/insecure/rs:{imgproxyFit}:{query.Width ?? 0}:{query.Height ?? 0}/plain/{source}@{GetOutputFormat(asset, query)}";
        }

        private string BuildImgproxyLocalPath(string storagePath)
        {
            var normalizedStoragePath = storagePath.Replace('\\', '/').TrimStart('/');
            var prefix = this.options.ImgproxyLocalPathPrefix?.Replace('\\', '/').Trim('/');
            return string.IsNullOrWhiteSpace(prefix)
                ? normalizedStoragePath
                : $"{prefix}/{normalizedStoragePath}";
        }

        private string ResolvePhysicalPath(string storagePath)
        {
            var rootPath = Path.IsPathRooted(this.options.RootPath)
                ? this.options.RootPath
                : Path.GetFullPath(Path.Combine(this.environment.ContentRootPath, this.options.RootPath));

            return Path.GetFullPath(Path.Combine(rootPath, storagePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string ToContentType(string format)
        {
            return MediaTransformPolicy.ToContentType(format);
        }

        private static string GetOutputFormat(CommerceMediaAsset asset, MediaTransformQuery query)
        {
            return query.Format.Equals("original", StringComparison.OrdinalIgnoreCase)
                ? asset.Extension
                : query.Format;
        }
    }
}
