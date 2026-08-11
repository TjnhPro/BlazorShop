namespace BlazorShop.Application.CommerceNode.Media
{
    public static class CommerceBrandingAssetSlots
    {
        public const string Logo = "logo";
        public const string Favicon = "favicon";

        public static bool TryNormalize(string? value, out string slot)
        {
            slot = value?.Trim().ToLowerInvariant() ?? string.Empty;
            return slot is Logo or Favicon;
        }

        public static string GetPresetName(string slot)
        {
            return slot switch
            {
                Logo => MediaUrlPresetNames.BrandLogo,
                Favicon => MediaUrlPresetNames.BrandFavicon,
                _ => throw new ArgumentException("Unsupported branding asset slot.", nameof(slot)),
            };
        }
    }

    public sealed record CommerceBrandingAssetUploadRequest(
        string Slot,
        CommerceMediaAssetUploadRequest File);

    public sealed record CommerceBrandingAssetResponse(
        string Slot,
        CommerceMediaAssetDto Asset,
        string EffectiveUrl);
}
