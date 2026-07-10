namespace BlazorShop.CommerceNode.API.ProductMedia
{
    public interface IProductMediaDownloader
    {
        Task<ProductMediaDownloadResult> DownloadOriginalAsync(
            Guid storeId,
            Guid productId,
            Guid mediaPublicId,
            string sourceUrl,
            CancellationToken cancellationToken);
    }

    public sealed record ProductMediaDownloadResult(
        bool Success,
        string Message,
        string? StoragePath = null,
        string? FileName = null,
        string? MimeType = null,
        string? ContentHash = null,
        int? Width = null,
        int? Height = null,
        long? FileSizeBytes = null)
    {
        public static ProductMediaDownloadResult Failed(string message)
        {
            return new ProductMediaDownloadResult(false, message);
        }

        public static ProductMediaDownloadResult Succeeded(
            string storagePath,
            string? fileName,
            string mimeType,
            string contentHash,
            int? width,
            int? height,
            long fileSizeBytes)
        {
            return new ProductMediaDownloadResult(
                true,
                "Product media downloaded.",
                storagePath,
                fileName,
                mimeType,
                contentHash,
                width,
                height,
                fileSizeBytes);
        }
    }
}
