namespace BlazorShop.Application.CommerceNode.Media
{
    public sealed class CommerceMediaStorageOptions
    {
        public const string SectionName = "CommerceMediaStorage";

        public string RootPath { get; set; } = "runtime/media/assets";

        public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;

        public string? ImgproxyBaseUrl { get; set; } = "http://localhost:8089";

        public bool UseImgproxy { get; set; } = true;
    }
}
