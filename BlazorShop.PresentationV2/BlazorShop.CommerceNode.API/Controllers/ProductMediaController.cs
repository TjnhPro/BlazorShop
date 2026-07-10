namespace BlazorShop.CommerceNode.API.Controllers
{
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
        private const int DefaultDimension = 1000;
        private const int MaxDimension = 2000;

        private static readonly HashSet<string> AllowedFormats = new(StringComparer.OrdinalIgnoreCase)
        {
            "webp",
            "jpg",
            "png",
        };

        private static readonly Dictionary<string, string> ImgproxyFitByRequestFit = new(StringComparer.OrdinalIgnoreCase)
        {
            ["contain"] = "fit",
            ["cover"] = "fill",
            ["max"] = "fit",
        };

        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly ProductMediaStorageOptions options;
        private readonly IWebHostEnvironment environment;
        private readonly IHttpClientFactory httpClientFactory;

        public ProductMediaController(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            IOptions<ProductMediaStorageOptions> options,
            IWebHostEnvironment environment,
            IHttpClientFactory httpClientFactory)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.options = options.Value;
            this.environment = environment;
            this.httpClientFactory = httpClientFactory;
        }

        [HttpGet("{mediaPublicId:guid}")]
        public async Task<IActionResult> Get(
            Guid mediaPublicId,
            [FromQuery] int? w,
            [FromQuery] int? h,
            [FromQuery] string? fit,
            [FromQuery] string? format,
            [FromQuery] int? v,
            CancellationToken cancellationToken)
        {
            var query = NormalizeQuery(w, h, fit, format);
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
            var rootPath = ProductMediaDownloader.ResolveRootPath(this.environment.ContentRootPath, this.options.RootPath);
            var physicalPath = Path.Combine(
                rootPath,
                media.OriginalStoragePath!.Replace('/', Path.DirectorySeparatorChar));

            if (!System.IO.File.Exists(physicalPath))
            {
                return this.NotFound();
            }

            return this.PhysicalFile(physicalPath, media.MimeType ?? "application/octet-stream");
        }

        private void SetCacheHeaders(Domain.Entities.CommerceNode.ProductMedia media, MediaQuery query, int? requestedVersion)
        {
            var version = requestedVersion is > 0 ? requestedVersion.Value : media.Version;
            this.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            this.Response.Headers.ETag = $"\"{media.PublicId:D}:{query.Width}:{query.Height}:{query.Fit}:{query.Format}:{version}\"";
            this.Response.Headers.XContentTypeOptions = "nosniff";
        }

        private static string BuildImgproxyUrl(string baseUrl, string storagePath, MediaQuery query)
        {
            var normalizedBaseUrl = baseUrl.TrimEnd('/');
            var imgproxyFit = ImgproxyFitByRequestFit[query.Fit];
            var height = query.Height ?? 0;
            var source = Uri.EscapeDataString($"local:///{storagePath.Replace('\\', '/')}");
            return $"{normalizedBaseUrl}/insecure/rs:{imgproxyFit}:{query.Width}:{height}/plain/{source}@{query.Format}";
        }

        private static MediaQueryResult NormalizeQuery(int? width, int? height, string? fit, string? format)
        {
            var normalizedWidth = NormalizeDimension(width);
            var normalizedHeight = NormalizeDimension(height);
            if (normalizedWidth is null && normalizedHeight is null)
            {
                normalizedWidth = DefaultDimension;
            }

            var normalizedFit = string.IsNullOrWhiteSpace(fit) ? "contain" : fit.Trim().ToLowerInvariant();
            if (!ImgproxyFitByRequestFit.ContainsKey(normalizedFit))
            {
                return MediaQueryResult.Failed("Media fit is invalid.");
            }

            var normalizedFormat = string.IsNullOrWhiteSpace(format) ? "webp" : format.Trim().ToLowerInvariant();
            if (!AllowedFormats.Contains(normalizedFormat))
            {
                return MediaQueryResult.Failed("Media format is invalid.");
            }

            return MediaQueryResult.Succeeded(new MediaQuery(normalizedWidth!.Value, normalizedHeight, normalizedFit, normalizedFormat));
        }

        private static int? NormalizeDimension(int? value)
        {
            if (value is null || value <= 0)
            {
                return null;
            }

            return Math.Min(value.Value, MaxDimension);
        }

        private static string ToContentType(string format)
        {
            return format.Equals("png", StringComparison.OrdinalIgnoreCase)
                ? "image/png"
                : format.Equals("jpg", StringComparison.OrdinalIgnoreCase)
                    ? "image/jpeg"
                    : "image/webp";
        }

        private sealed record MediaQuery(int Width, int? Height, string Fit, string Format);

        private sealed record MediaQueryResult(bool Success, MediaQuery Value, string? Message = null)
        {
            public static MediaQueryResult Succeeded(MediaQuery query)
            {
                return new MediaQueryResult(true, query);
            }

            public static MediaQueryResult Failed(string message)
            {
                return new MediaQueryResult(false, new MediaQuery(DefaultDimension, null, "contain", "webp"), message);
            }
        }
    }
}
