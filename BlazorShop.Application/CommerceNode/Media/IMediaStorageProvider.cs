namespace BlazorShop.Application.CommerceNode.Media
{
    public interface IMediaStorageProvider
    {
        string BuildStoragePath(params string[] segments);

        string ResolveRootPath(string contentRootPath, string configuredRootPath);

        string ResolvePhysicalPath(string contentRootPath, string configuredRootPath, string storagePath);

        Stream OpenRead(string contentRootPath, string configuredRootPath, string storagePath);

        Stream OpenWrite(string contentRootPath, string configuredRootPath, string storagePath);

        bool FileExists(string contentRootPath, string configuredRootPath, string storagePath);

        long GetFileSize(string contentRootPath, string configuredRootPath, string storagePath);

        Task WriteAllBytesAsync(
            string contentRootPath,
            string configuredRootPath,
            string storagePath,
            byte[] content,
            CancellationToken cancellationToken = default);

        Task MoveAsync(
            string contentRootPath,
            string configuredRootPath,
            string sourceStoragePath,
            string destinationStoragePath,
            bool replace = false,
            CancellationToken cancellationToken = default);

        Task DeleteFileIfExistsAsync(
            string contentRootPath,
            string configuredRootPath,
            string storagePath,
            CancellationToken cancellationToken = default);

        Task DeleteDirectoryIfExistsAsync(
            string contentRootPath,
            string configuredRootPath,
            string storagePath,
            bool recursive,
            CancellationToken cancellationToken = default);
    }
}
