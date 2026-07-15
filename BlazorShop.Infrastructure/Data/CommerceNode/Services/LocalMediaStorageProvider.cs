namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Media;

    public sealed class LocalMediaStorageProvider : IMediaStorageProvider
    {
        public string BuildStoragePath(params string[] segments)
        {
            if (segments is null || segments.Length == 0)
            {
                throw new ArgumentException("Storage path segments are required.", nameof(segments));
            }

            var safeSegments = segments
                .SelectMany(segment => (segment ?? string.Empty).Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries))
                .Select(ValidateSegment)
                .ToArray();

            if (safeSegments.Length == 0)
            {
                throw new ArgumentException("Storage path segments are required.", nameof(segments));
            }

            return string.Join('/', safeSegments);
        }

        public string ResolveRootPath(string contentRootPath, string configuredRootPath)
        {
            if (string.IsNullOrWhiteSpace(contentRootPath))
            {
                throw new ArgumentException("Content root path is required.", nameof(contentRootPath));
            }

            if (string.IsNullOrWhiteSpace(configuredRootPath))
            {
                throw new ArgumentException("Media root path is required.", nameof(configuredRootPath));
            }

            return Path.GetFullPath(Path.IsPathRooted(configuredRootPath)
                ? configuredRootPath
                : Path.Combine(contentRootPath, configuredRootPath));
        }

        public string ResolvePhysicalPath(string contentRootPath, string configuredRootPath, string storagePath)
        {
            var rootPath = this.ResolveRootPath(contentRootPath, configuredRootPath);
            var normalizedStoragePath = NormalizeStoragePath(storagePath);
            var physicalPath = Path.GetFullPath(Path.Combine(
                rootPath,
                normalizedStoragePath.Replace('/', Path.DirectorySeparatorChar)));

            if (!IsWithinRoot(rootPath, physicalPath))
            {
                throw new InvalidOperationException("Media storage path escapes the configured root.");
            }

            return physicalPath;
        }

        public Stream OpenRead(string contentRootPath, string configuredRootPath, string storagePath)
        {
            return File.OpenRead(this.ResolvePhysicalPath(contentRootPath, configuredRootPath, storagePath));
        }

        public Stream OpenWrite(string contentRootPath, string configuredRootPath, string storagePath)
        {
            var physicalPath = this.ResolvePhysicalPath(contentRootPath, configuredRootPath, storagePath);
            Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);
            return File.Create(physicalPath);
        }

        public bool FileExists(string contentRootPath, string configuredRootPath, string storagePath)
        {
            return File.Exists(this.ResolvePhysicalPath(contentRootPath, configuredRootPath, storagePath));
        }

        public long GetFileSize(string contentRootPath, string configuredRootPath, string storagePath)
        {
            return new FileInfo(this.ResolvePhysicalPath(contentRootPath, configuredRootPath, storagePath)).Length;
        }

        public async Task WriteAllBytesAsync(
            string contentRootPath,
            string configuredRootPath,
            string storagePath,
            byte[] content,
            CancellationToken cancellationToken = default)
        {
            var physicalPath = this.ResolvePhysicalPath(contentRootPath, configuredRootPath, storagePath);
            Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);
            await File.WriteAllBytesAsync(physicalPath, content, cancellationToken);
        }

        public Task MoveAsync(
            string contentRootPath,
            string configuredRootPath,
            string sourceStoragePath,
            string destinationStoragePath,
            bool replace = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sourcePath = this.ResolvePhysicalPath(contentRootPath, configuredRootPath, sourceStoragePath);
            var destinationPath = this.ResolvePhysicalPath(contentRootPath, configuredRootPath, destinationStoragePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            if (replace && File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            File.Move(sourcePath, destinationPath);
            return Task.CompletedTask;
        }

        public Task DeleteFileIfExistsAsync(
            string contentRootPath,
            string configuredRootPath,
            string storagePath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var physicalPath = this.ResolvePhysicalPath(contentRootPath, configuredRootPath, storagePath);
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }

            return Task.CompletedTask;
        }

        public Task DeleteDirectoryIfExistsAsync(
            string contentRootPath,
            string configuredRootPath,
            string storagePath,
            bool recursive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var physicalPath = this.ResolvePhysicalPath(contentRootPath, configuredRootPath, storagePath);
            if (Directory.Exists(physicalPath))
            {
                Directory.Delete(physicalPath, recursive);
            }

            return Task.CompletedTask;
        }

        private static string NormalizeStoragePath(string storagePath)
        {
            if (string.IsNullOrWhiteSpace(storagePath))
            {
                throw new ArgumentException("Storage path is required.", nameof(storagePath));
            }

            if (Path.IsPathRooted(storagePath))
            {
                throw new InvalidOperationException("Media storage path must be relative.");
            }

            return string.Join(
                '/',
                storagePath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(ValidateSegment));
        }

        private static string ValidateSegment(string segment)
        {
            if (string.IsNullOrWhiteSpace(segment) || segment == "." || segment == "..")
            {
                throw new InvalidOperationException("Media storage path contains an invalid segment.");
            }

            if (segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new InvalidOperationException("Media storage path contains invalid characters.");
            }

            return segment;
        }

        private static bool IsWithinRoot(string rootPath, string physicalPath)
        {
            var normalizedRoot = Path.GetFullPath(rootPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var normalizedPhysicalPath = Path.GetFullPath(physicalPath);

            return normalizedPhysicalPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }
    }
}
