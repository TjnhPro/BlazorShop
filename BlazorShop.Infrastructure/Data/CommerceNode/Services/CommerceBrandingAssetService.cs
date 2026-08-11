namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Media;

    public sealed class CommerceBrandingAssetService : ICommerceBrandingAssetService
    {
        private static readonly IReadOnlySet<string> SupportedExtensions = new HashSet<string>(
            [".png", ".webp"],
            StringComparer.OrdinalIgnoreCase);

        private readonly ICommerceMediaAssetService mediaAssetService;
        private readonly ICommerceMediaUrlBuilder urlBuilder;

        public CommerceBrandingAssetService(
            ICommerceMediaAssetService mediaAssetService,
            ICommerceMediaUrlBuilder urlBuilder)
        {
            this.mediaAssetService = mediaAssetService;
            this.urlBuilder = urlBuilder;
        }

        public async Task<ApplicationResult<CommerceBrandingAssetResponse>> UploadAsync(
            CommerceBrandingAssetUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!CommerceBrandingAssetSlots.TryNormalize(request.Slot, out var slot))
            {
                return Failed("Branding slot must be logo or favicon.");
            }

            var extension = Path.GetExtension(request.File.FileName ?? string.Empty);
            if (!SupportedExtensions.Contains(extension))
            {
                return Failed("Branding images must use PNG or WebP format.");
            }

            await using var content = new MemoryStream();
            await request.File.Content.CopyToAsync(content, cancellationToken);
            var upload = await this.mediaAssetService.UploadAsync(
                new CommerceMediaAssetUploadRequest(
                    new MemoryStream(content.ToArray(), writable: false),
                    request.File.FileName,
                    request.File.ContentType,
                    content.Length),
                cancellationToken);
            if (!upload.Success || upload.Payload is null)
            {
                return ApplicationResult<CommerceBrandingAssetResponse>.Failed(upload.Error!);
            }

            var asset = upload.Payload;
            var branding = await this.mediaAssetService.UpdateMetadataAsync(
                asset.PublicId,
                new CommerceMediaAssetMetadataRequest(
                    asset.DisplayName,
                    asset.AltText,
                    asset.TitleText,
                    CommerceMediaAssetUsageTypes.Branding),
                cancellationToken);
            if (!branding.Success || branding.Payload is null)
            {
                return ApplicationResult<CommerceBrandingAssetResponse>.Failed(branding.Error!);
            }

            asset = branding.Payload;
            return ApplicationResult<CommerceBrandingAssetResponse>.Succeeded(
                new CommerceBrandingAssetResponse(
                    slot,
                    asset,
                    this.urlBuilder.BuildAssetUrl(
                        asset.PublicId,
                        asset.CanonicalFileName,
                        asset.Version,
                        CommerceBrandingAssetSlots.GetPresetName(slot))),
                "Branding image uploaded.");
        }

        private static ApplicationResult<CommerceBrandingAssetResponse> Failed(string message)
        {
            return ApplicationResult<CommerceBrandingAssetResponse>.Failed(
                ApplicationError.Validation("branding.validation", message));
        }
    }
}
