namespace BlazorShop.Application.CommerceNode.Media
{
    public static class MediaUrlPresetNames
    {
        public const string ProductCard = "product-card";
        public const string ProductDetail = "product-detail";
        public const string CartLine = "cart-line";
        public const string CategoryCard = "category-card";
        public const string ContentBanner = "content-banner";
        public const string ContentCard = "content-card";
        public const string BrandLogo = "brand-logo";
    }

    public sealed record MediaUrlPreset(
        string Name,
        int? Width,
        int? Height,
        string Fit,
        string Format);

    public static class MediaUrlPresets
    {
        private static readonly IReadOnlyDictionary<string, MediaUrlPreset> Presets =
            new Dictionary<string, MediaUrlPreset>(StringComparer.OrdinalIgnoreCase)
            {
                [MediaUrlPresetNames.ProductCard] = new(MediaUrlPresetNames.ProductCard, 600, 600, "contain", "webp"),
                [MediaUrlPresetNames.ProductDetail] = new(MediaUrlPresetNames.ProductDetail, 1000, 1000, "contain", "webp"),
                [MediaUrlPresetNames.CartLine] = new(MediaUrlPresetNames.CartLine, 160, 160, "cover", "webp"),
                [MediaUrlPresetNames.CategoryCard] = new(MediaUrlPresetNames.CategoryCard, 600, 400, "cover", "webp"),
                [MediaUrlPresetNames.ContentBanner] = new(MediaUrlPresetNames.ContentBanner, 1920, 600, "cover", "webp"),
                [MediaUrlPresetNames.ContentCard] = new(MediaUrlPresetNames.ContentCard, 800, 600, "cover", "webp"),
                [MediaUrlPresetNames.BrandLogo] = new(MediaUrlPresetNames.BrandLogo, 320, null, "inside", "png"),
            };

        public static MediaUrlPreset Get(string name)
        {
            return Presets.TryGetValue(name, out var preset)
                ? preset
                : throw new ArgumentException("Unknown media URL preset.", nameof(name));
        }

        public static bool TryGet(string? name, out MediaUrlPreset preset)
        {
            if (!string.IsNullOrWhiteSpace(name)
                && Presets.TryGetValue(name.Trim(), out var found))
            {
                preset = found;
                return true;
            }

            preset = default!;
            return false;
        }
    }
}
