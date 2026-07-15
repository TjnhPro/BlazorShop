namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.ProductMedia;

    public sealed class ProductMediaUrlBuilder : IProductMediaUrlBuilder
    {
        public string BuildProductMediaUrl(Guid mediaPublicId, int version, ProductMediaUrlOptions? options = null)
        {
            options ??= new ProductMediaUrlOptions();

            var width = MediaTransformPolicy.ClampDimension(options.Width, MediaTransformPolicy.ProductMaxDimension)
                ?? MediaTransformPolicy.ProductDefaultDimension;
            var height = MediaTransformPolicy.ClampDimension(options.Height, MediaTransformPolicy.ProductMaxDimension);
            var fit = MediaTransformPolicy.NormalizeOption(options.Fit, "contain");
            var format = MediaTransformPolicy.NormalizeOption(options.Format, "webp");

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
    }
}
