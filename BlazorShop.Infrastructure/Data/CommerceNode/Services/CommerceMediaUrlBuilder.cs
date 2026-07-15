namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Media;

    public sealed class CommerceMediaUrlBuilder : ICommerceMediaUrlBuilder
    {
        public string BuildAssetUrl(
            Guid assetPublicId,
            string canonicalFileName,
            long? version = null,
            string? presetName = null)
        {
            var url = $"/media/assets/{assetPublicId:D}/{Uri.EscapeDataString(canonicalFileName)}";
            var query = new List<string>();

            if (MediaUrlPresets.TryGet(presetName, out var preset))
            {
                AddQuery(query, "w", preset.Width);
                AddQuery(query, "h", preset.Height);
                AddQuery(query, "fit", preset.Fit);
                AddQuery(query, "format", preset.Format);
            }

            if (version is > 0)
            {
                AddQuery(query, "v", version.Value);
            }

            return query.Count == 0
                ? url
                : $"{url}?{string.Join("&", query)}";
        }

        public string BuildAbsoluteAssetUrl(
            Guid assetPublicId,
            string canonicalFileName,
            string publicBaseUrl,
            long? version = null,
            string? presetName = null)
        {
            return MediaDeliveryUrlPolicy.BuildAbsoluteUrl(
                publicBaseUrl,
                this.BuildAssetUrl(assetPublicId, canonicalFileName, version, presetName));
        }

        private static void AddQuery(List<string> query, string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                query.Add($"{key}={Uri.EscapeDataString(value)}");
            }
        }

        private static void AddQuery(List<string> query, string key, int? value)
        {
            if (value is > 0)
            {
                query.Add($"{key}={value.Value}");
            }
        }

        private static void AddQuery(List<string> query, string key, long? value)
        {
            if (value is > 0)
            {
                query.Add($"{key}={value.Value}");
            }
        }
    }
}
