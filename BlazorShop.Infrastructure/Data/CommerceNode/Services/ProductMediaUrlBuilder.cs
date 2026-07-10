namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.ProductMedia;

    public sealed class ProductMediaUrlBuilder : IProductMediaUrlBuilder
    {
        private const int DefaultWidth = 1000;
        private const int MaxDimension = 2000;

        public string BuildProductMediaUrl(Guid mediaPublicId, int version, ProductMediaUrlOptions? options = null)
        {
            options ??= new ProductMediaUrlOptions();

            var width = ClampDimension(options.Width) ?? DefaultWidth;
            var height = ClampDimension(options.Height);
            var fit = NormalizeOption(options.Fit, "contain");
            var format = NormalizeOption(options.Format, "webp");

            var query = new List<string>
            {
                $"w={width}",
                $"fit={Uri.EscapeDataString(fit)}",
                $"format={Uri.EscapeDataString(format)}",
                $"v={Math.Max(1, version)}",
            };

            if (height is not null)
            {
                query.Insert(1, $"h={height.Value}");
            }

            return $"/media/products/{mediaPublicId:D}?{string.Join("&", query)}";
        }

        private static int? ClampDimension(int? value)
        {
            if (value is null || value <= 0)
            {
                return null;
            }

            return Math.Min(MaxDimension, value.Value);
        }

        private static string NormalizeOption(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();
        }
    }
}
