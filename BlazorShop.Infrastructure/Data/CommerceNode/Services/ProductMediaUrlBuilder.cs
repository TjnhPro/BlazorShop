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

        public string BuildProductMediaPresetUrl(Guid mediaPublicId, int version, string presetName)
        {
            var preset = MediaUrlPresets.Get(presetName);
            return this.BuildProductMediaUrl(
                mediaPublicId,
                version,
                new ProductMediaUrlOptions(
                    preset.Width,
                    preset.Height,
                    preset.Fit,
                    preset.Format));
        }

        public string BuildAbsoluteProductMediaUrl(
            Guid mediaPublicId,
            int version,
            string publicBaseUrl,
            ProductMediaUrlOptions? options = null)
        {
            return MediaDeliveryUrlPolicy.BuildAbsoluteUrl(
                publicBaseUrl,
                this.BuildProductMediaUrl(mediaPublicId, version, options));
        }

        public string BuildAbsoluteProductMediaPresetUrl(
            Guid mediaPublicId,
            int version,
            string publicBaseUrl,
            string presetName)
        {
            return MediaDeliveryUrlPolicy.BuildAbsoluteUrl(
                publicBaseUrl,
                this.BuildProductMediaPresetUrl(mediaPublicId, version, presetName));
        }
    }
}
