namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.CommerceNode.API.Configuration;
    using BlazorShop.CommerceNode.API.ProductMedia;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;

    [ApiController]
    [Route("media/products")]
    public sealed class ProductMediaController : ControllerBase
    {
        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly ProductMediaStorageOptions options;
        private readonly IWebHostEnvironment environment;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IMediaStorageProvider storageProvider;

        public ProductMediaController(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            IOptions<ProductMediaStorageOptions> options,
            IWebHostEnvironment environment,
            IHttpClientFactory httpClientFactory,
            IMediaStorageProvider storageProvider)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.options = options.Value;
            this.environment = environment;
            this.httpClientFactory = httpClientFactory;
            this.storageProvider = storageProvider;
        }

        [HttpGet("{mediaPublicId:guid}")]
        [HttpGet("/api/commerce/admin/media/products/{mediaPublicId:guid}")]
        public async Task<IActionResult> Get(
            Guid mediaPublicId,
            [FromQuery] int? w,
            [FromQuery] int? h,
            [FromQuery] string? fit,
            [FromQuery] string? format,
            [FromQuery] int? v,
            CancellationToken cancellationToken)
        {
            var query = MediaTransformPolicy.NormalizeProductQuery(w, h, fit, format);
            if (!query.Success)
            {
                return this.BadRequest(query.Message);
            }

            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success || storeResult.Payload == Guid.Empty)
            {
                return this.NotFound();
            }

            var media = await this.context.ProductMedia
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.PublicId == mediaPublicId &&
                        item.StoreId == storeResult.Payload &&
                        item.Status == ProductMediaStatuses.Stored &&
                        item.DeletedAt == null,
                    cancellationToken);

            if (media is null || string.IsNullOrWhiteSpace(media.OriginalStoragePath))
            {
                return this.NotFound();
            }

            this.SetCacheHeaders(media, query.Value, v);

            if (!this.storageProvider.FileExists(
                this.environment.ContentRootPath,
                this.options.RootPath,
                media.OriginalStoragePath!))
            {
                return this.NotFound();
            }

            if (!this.options.UseImgproxy || string.IsNullOrWhiteSpace(this.options.ImgproxyBaseUrl))
            {
                return this.ServeOriginal(media);
            }

            var imgproxyUrl = BuildImgproxyUrl(this.options.ImgproxyBaseUrl, media.OriginalStoragePath, query.Value);
            using var response = await this.httpClientFactory.CreateClient().GetAsync(imgproxyUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return this.StatusCode(StatusCodes.Status502BadGateway, "Image processor could not render media.");
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? ToContentType(query.Value.Format);
            return this.File(bytes, contentType);
        }

        private IActionResult ServeOriginal(Domain.Entities.CommerceNode.ProductMedia media)
        {
            var physicalPath = this.storageProvider.ResolvePhysicalPath(
                this.environment.ContentRootPath,
                this.options.RootPath,
                media.OriginalStoragePath!);
            return this.PhysicalFile(physicalPath, media.MimeType ?? "application/octet-stream");
        }

        private void SetCacheHeaders(Domain.Entities.CommerceNode.ProductMedia media, MediaTransformQuery query, int? requestedVersion)
        {
            var hasVersion = requestedVersion is > 0;
            var version = hasVersion ? requestedVersion!.Value : media.Version;
            this.Response.Headers.CacheControl = hasVersion
                ? "public, max-age=31536000, immutable"
                : "public, max-age=3600";
            this.Response.Headers.ETag = $"\"{media.PublicId:D}:{query.Width}:{query.Height}:{query.Fit}:{query.Format}:{version}\"";
            this.Response.Headers.XContentTypeOptions = "nosniff";
        }

        private static string BuildImgproxyUrl(string baseUrl, string storagePath, MediaTransformQuery query)
        {
            var normalizedBaseUrl = baseUrl.TrimEnd('/');
            var imgproxyFit = MediaTransformPolicy.ProductImgproxyFitByRequestFit[query.Fit];
            var height = query.Height ?? 0;
            var source = Uri.EscapeDataString($"local:///{storagePath.Replace('\\', '/')}");
            return $"{normalizedBaseUrl}/insecure/rs:{imgproxyFit}:{query.Width}:{height}/plain/{source}@{query.Format}";
        }

        private static string ToContentType(string format)
        {
            return MediaTransformPolicy.ToContentType(format);
        }
    }
}
