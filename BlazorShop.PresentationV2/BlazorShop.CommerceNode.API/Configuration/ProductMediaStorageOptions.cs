namespace BlazorShop.CommerceNode.API.Configuration
{
    public sealed class ProductMediaStorageOptions
    {
        public const string SectionName = "ProductMediaStorage";

        public string RootPath { get; set; } = "runtime/media";

        public long MaxDownloadBytes { get; set; } = 10 * 1024 * 1024;

        public int DownloadTimeoutSeconds { get; set; } = 30;
    }
}
