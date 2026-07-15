namespace BlazorShop.Application.CommerceNode.Media
{
    public sealed record MediaTransformQuery(int? Width, int? Height, string Fit, string Format);

    public sealed record MediaTransformPolicyResult(
        bool Success,
        MediaTransformQuery Value,
        string? Message = null)
    {
        public static MediaTransformPolicyResult Succeeded(MediaTransformQuery query)
        {
            return new MediaTransformPolicyResult(true, query);
        }

        public static MediaTransformPolicyResult Failed(MediaTransformQuery fallback, string message)
        {
            return new MediaTransformPolicyResult(false, fallback, message);
        }
    }

    public static class MediaTransformPolicy
    {
        public const int ProductDefaultDimension = 1000;
        public const int ProductMaxDimension = 2000;
        public const int AssetMaxDimension = 4096;
        public const long AssetMaxOutputPixels = 16_000_000;

        public static readonly IReadOnlyDictionary<string, string> ProductImgproxyFitByRequestFit =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["contain"] = "fit",
                ["cover"] = "fill",
                ["max"] = "fit",
            };

        public static readonly IReadOnlyDictionary<string, string> AssetImgproxyFitByRequestFit =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["contain"] = "fit",
                ["cover"] = "fill",
                ["inside"] = "fit",
            };

        public static readonly IReadOnlySet<string> ProductOutputFormats =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "webp",
                "jpg",
                "png",
            };

        public static readonly IReadOnlySet<string> AssetOutputFormats =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "original",
                "webp",
                "jpg",
                "png",
            };

        public static MediaTransformPolicyResult NormalizeProductQuery(
            int? width,
            int? height,
            string? fit,
            string? format)
        {
            var normalizedWidth = ClampDimension(width, ProductMaxDimension);
            var normalizedHeight = ClampDimension(height, ProductMaxDimension);
            if (normalizedWidth is null && normalizedHeight is null)
            {
                normalizedWidth = ProductDefaultDimension;
            }

            var normalizedFit = NormalizeOption(fit, "contain");
            if (!ProductImgproxyFitByRequestFit.ContainsKey(normalizedFit))
            {
                return MediaTransformPolicyResult.Failed(
                    new MediaTransformQuery(ProductDefaultDimension, null, "contain", "webp"),
                    "Media fit is invalid.");
            }

            var normalizedFormat = NormalizeOption(format, "webp");
            if (!ProductOutputFormats.Contains(normalizedFormat))
            {
                return MediaTransformPolicyResult.Failed(
                    new MediaTransformQuery(ProductDefaultDimension, null, "contain", "webp"),
                    "Media format is invalid.");
            }

            return MediaTransformPolicyResult.Succeeded(
                new MediaTransformQuery(normalizedWidth!.Value, normalizedHeight, normalizedFit, normalizedFormat));
        }

        public static MediaTransformPolicyResult NormalizeAssetQuery(
            int? width,
            int? height,
            string? fit,
            string? format,
            int? sourceWidth,
            int? sourceHeight,
            bool hasTransformQuery)
        {
            var normalizedWidth = ClampDimension(width, AssetMaxDimension);
            var normalizedHeight = ClampDimension(height, AssetMaxDimension);
            var normalizedFit = NormalizeOption(fit, "inside");
            if (!AssetImgproxyFitByRequestFit.ContainsKey(normalizedFit))
            {
                return MediaTransformPolicyResult.Failed(
                    new MediaTransformQuery(null, null, "inside", "original"),
                    "Media fit is invalid.");
            }

            var normalizedFormat = NormalizeOption(format, "original");
            if (!AssetOutputFormats.Contains(normalizedFormat))
            {
                return MediaTransformPolicyResult.Failed(
                    new MediaTransformQuery(null, null, "inside", "original"),
                    "Media format is invalid.");
            }

            if (normalizedWidth is null && normalizedHeight is null && hasTransformQuery && normalizedFormat != "original")
            {
                normalizedWidth = sourceWidth;
                normalizedHeight = sourceHeight;
            }

            if (normalizedWidth is not null && sourceWidth is not null)
            {
                normalizedWidth = Math.Min(normalizedWidth.Value, sourceWidth.Value);
            }

            if (normalizedHeight is not null && sourceHeight is not null)
            {
                normalizedHeight = Math.Min(normalizedHeight.Value, sourceHeight.Value);
            }

            if (normalizedWidth is not null && normalizedHeight is not null
                && (long)normalizedWidth.Value * normalizedHeight.Value > AssetMaxOutputPixels)
            {
                return MediaTransformPolicyResult.Failed(
                    new MediaTransformQuery(null, null, "inside", "original"),
                    "Media transform output is too large.");
            }

            return MediaTransformPolicyResult.Succeeded(
                new MediaTransformQuery(normalizedWidth, normalizedHeight, normalizedFit, normalizedFormat));
        }

        public static int? ClampDimension(int? value, int maxDimension)
        {
            if (value is null || value <= 0)
            {
                return null;
            }

            return Math.Min(value.Value, maxDimension);
        }

        public static string NormalizeOption(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();
        }

        public static string ToContentType(string format)
        {
            return format.Equals("png", StringComparison.OrdinalIgnoreCase)
                ? "image/png"
                : format.Equals("jpg", StringComparison.OrdinalIgnoreCase)
                    ? "image/jpeg"
                    : "image/webp";
        }
    }
}
