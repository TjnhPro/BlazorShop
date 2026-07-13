namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;

    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    public sealed class CommerceMediaAssetService : ICommerceMediaAssetService
    {
        private const int MaxPageSize = 100;
        private const int HeaderLength = 32;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".gif",
            ".ico",
        };

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
            "image/gif",
            "image/x-icon",
            "image/vnd.microsoft.icon",
        };

        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly CommerceMediaStorageOptions options;
        private readonly IHostEnvironment environment;

        public CommerceMediaAssetService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            IOptions<CommerceMediaStorageOptions> options,
            IHostEnvironment environment)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.options = options.Value;
            this.environment = environment;
        }

        public async Task<CommerceMediaAssetOperationResult<CommerceMediaAssetListResponse>> ListAsync(
            CommerceMediaAssetListQuery query,
            CancellationToken cancellationToken = default)
        {
            var scope = await this.ResolveStoreAsync(cancellationToken);
            if (!scope.Success)
            {
                return Failed<CommerceMediaAssetListResponse>(scope.Failure!.Value, scope.Message);
            }

            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            var assets = this.context.CommerceMediaAssets
                .AsNoTracking()
                .Where(asset => asset.StoreId == scope.StoreId);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                assets = assets.Where(
                    asset =>
                        EF.Functions.ILike(asset.DisplayName, $"%{search}%") ||
                        EF.Functions.ILike(asset.OriginalFileName, $"%{search}%") ||
                        EF.Functions.ILike(asset.CanonicalFileName, $"%{search}%"));
            }

            var totalCount = await assets.CountAsync(cancellationToken);
            var items = await assets
                .OrderByDescending(asset => asset.UpdatedAt)
                .ThenBy(asset => asset.DisplayName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(asset => ToDto(asset))
                .ToListAsync(cancellationToken);

            return Succeeded(
                new CommerceMediaAssetListResponse(
                    items,
                    totalCount,
                    pageNumber,
                    pageSize,
                    CalculateTotalPages(totalCount, pageSize)));
        }

        public async Task<CommerceMediaAssetOperationResult<CommerceMediaAssetDto>> GetAsync(
            Guid assetPublicId,
            CancellationToken cancellationToken = default)
        {
            var asset = await this.GetScopedAssetAsync(assetPublicId, cancellationToken);
            return asset is null
                ? Failed<CommerceMediaAssetDto>(CommerceMediaAssetOperationFailure.NotFound, "Media asset was not found.")
                : Succeeded(ToDto(asset));
        }

        public async Task<CommerceMediaAssetOperationResult<CommerceMediaAssetDto>> UploadAsync(
            CommerceMediaAssetUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            var scope = await this.ResolveStoreAsync(cancellationToken);
            if (!scope.Success)
            {
                return Failed<CommerceMediaAssetDto>(scope.Failure!.Value, scope.Message);
            }

            var validation = await ReadValidatedFileAsync(request, this.options.MaxUploadBytes, cancellationToken);
            if (!validation.Success)
            {
                return Failed<CommerceMediaAssetDto>(CommerceMediaAssetOperationFailure.Validation, validation.Message);
            }

            var file = validation.File!;
            var now = DateTimeOffset.UtcNow;
            var publicId = Guid.NewGuid();
            var canonicalFileName = BuildCanonicalFileName(file.OriginalFileName, file.Extension);
            var generatedName = GenerateDisplayName(file.OriginalFileName);
            var storagePath = BuildStoragePath(scope.StoreId, publicId, file.Extension);
            var physicalPath = ResolvePhysicalPath(storagePath);

            Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);
            await File.WriteAllBytesAsync(physicalPath, file.Content, cancellationToken);

            var asset = new CommerceMediaAsset
            {
                Id = Guid.NewGuid(),
                PublicId = publicId,
                StoreId = scope.StoreId,
                OriginalFileName = file.OriginalFileName,
                CanonicalFileName = canonicalFileName,
                DisplayName = generatedName,
                AltText = generatedName,
                TitleText = generatedName,
                OriginalStoragePath = storagePath,
                ContentHash = file.ContentHash,
                MimeType = file.MimeType,
                Extension = file.Extension.TrimStart('.').ToLowerInvariant(),
                Width = file.Width,
                Height = file.Height,
                FileSizeBytes = file.Content.Length,
                CreatedAt = now,
                UpdatedAt = now,
            };

            this.context.CommerceMediaAssets.Add(asset);
            await this.context.SaveChangesAsync(cancellationToken);
            return Succeeded(ToDto(asset), "Media asset uploaded.");
        }

        public async Task<CommerceMediaAssetOperationResult<CommerceMediaAssetDto>> UpdateMetadataAsync(
            Guid assetPublicId,
            CommerceMediaAssetMetadataRequest request,
            CancellationToken cancellationToken = default)
        {
            var asset = await this.GetScopedAssetAsync(assetPublicId, cancellationToken);
            if (asset is null)
            {
                return Failed<CommerceMediaAssetDto>(CommerceMediaAssetOperationFailure.NotFound, "Media asset was not found.");
            }

            var displayName = request.DisplayName?.Trim();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return Failed<CommerceMediaAssetDto>(CommerceMediaAssetOperationFailure.Validation, "Display name is required.");
            }

            asset.DisplayName = displayName;
            asset.AltText = string.IsNullOrWhiteSpace(request.AltText) ? displayName : request.AltText.Trim();
            asset.TitleText = string.IsNullOrWhiteSpace(request.TitleText) ? null : request.TitleText.Trim();
            asset.UpdatedAt = DateTimeOffset.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken);
            return Succeeded(ToDto(asset), "Media asset updated.");
        }

        public async Task<CommerceMediaAssetOperationResult<CommerceMediaAssetDto>> ReplaceAsync(
            Guid assetPublicId,
            CommerceMediaAssetUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            var asset = await this.GetScopedAssetAsync(assetPublicId, cancellationToken);
            if (asset is null)
            {
                return Failed<CommerceMediaAssetDto>(CommerceMediaAssetOperationFailure.NotFound, "Media asset was not found.");
            }

            var validation = await ReadValidatedFileAsync(request, this.options.MaxUploadBytes, cancellationToken);
            if (!validation.Success)
            {
                return Failed<CommerceMediaAssetDto>(CommerceMediaAssetOperationFailure.Validation, validation.Message);
            }

            var file = validation.File!;
            var oldPhysicalPath = ResolvePhysicalPath(asset.OriginalStoragePath);
            var storagePath = BuildStoragePath(asset.StoreId, asset.PublicId, file.Extension);
            var newPhysicalPath = ResolvePhysicalPath(storagePath);

            Directory.CreateDirectory(Path.GetDirectoryName(newPhysicalPath)!);
            await File.WriteAllBytesAsync(newPhysicalPath, file.Content, cancellationToken);
            if (!oldPhysicalPath.Equals(newPhysicalPath, StringComparison.OrdinalIgnoreCase) && File.Exists(oldPhysicalPath))
            {
                File.Delete(oldPhysicalPath);
            }

            asset.OriginalFileName = file.OriginalFileName;
            asset.OriginalStoragePath = storagePath;
            asset.ContentHash = file.ContentHash;
            asset.MimeType = file.MimeType;
            asset.Extension = file.Extension.TrimStart('.').ToLowerInvariant();
            asset.Width = file.Width;
            asset.Height = file.Height;
            asset.FileSizeBytes = file.Content.Length;
            asset.UpdatedAt = DateTimeOffset.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken);
            return Succeeded(ToDto(asset), "Media asset replaced.");
        }

        public async Task<CommerceMediaAssetOperationResult<object>> DeleteAsync(
            Guid assetPublicId,
            CancellationToken cancellationToken = default)
        {
            var asset = await this.GetScopedAssetAsync(assetPublicId, cancellationToken);
            if (asset is null)
            {
                return Failed<object>(CommerceMediaAssetOperationFailure.NotFound, "Media asset was not found.");
            }

            this.context.CommerceMediaAssets.Remove(asset);
            await this.context.SaveChangesAsync(cancellationToken);

            var assetDirectory = ResolvePhysicalPath(Path.GetDirectoryName(asset.OriginalStoragePath) ?? string.Empty);
            if (Directory.Exists(assetDirectory))
            {
                Directory.Delete(assetDirectory, recursive: true);
            }

            return Succeeded<object>(new { }, "Media asset deleted.");
        }

        private async Task<CommerceMediaAsset?> GetScopedAssetAsync(Guid assetPublicId, CancellationToken cancellationToken)
        {
            var scope = await this.ResolveStoreAsync(cancellationToken);
            if (!scope.Success)
            {
                return null;
            }

            return await this.context.CommerceMediaAssets
                .FirstOrDefaultAsync(asset => asset.PublicId == assetPublicId && asset.StoreId == scope.StoreId, cancellationToken);
        }

        private async Task<StoreScopeResult> ResolveStoreAsync(CancellationToken cancellationToken)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success || storeResult.Payload == Guid.Empty)
            {
                return new StoreScopeResult(
                    false,
                    Guid.Empty,
                    "Store scope could not be resolved.",
                    CommerceMediaAssetOperationFailure.Validation);
            }

            return new StoreScopeResult(true, storeResult.Payload);
        }

        private static async Task<FileValidationResult> ReadValidatedFileAsync(
            CommerceMediaAssetUploadRequest request,
            long maxUploadBytes,
            CancellationToken cancellationToken)
        {
            if (request.Content is null || !request.Content.CanRead)
            {
                return FileValidationResult.Failed("Image file is required.");
            }

            if (request.FileSizeBytes <= 0 || request.FileSizeBytes > maxUploadBytes)
            {
                return FileValidationResult.Failed($"Image file must be between 1 byte and {maxUploadBytes} bytes.");
            }

            var originalFileName = Path.GetFileName(request.FileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(originalFileName))
            {
                return FileValidationResult.Failed("File name is required.");
            }

            var extension = Path.GetExtension(originalFileName);
            if (!AllowedExtensions.Contains(extension))
            {
                return FileValidationResult.Failed("Image file type is not supported.");
            }

            await using var memory = new MemoryStream();
            await request.Content.CopyToAsync(memory, cancellationToken);
            if (memory.Length == 0 || memory.Length > maxUploadBytes)
            {
                return FileValidationResult.Failed($"Image file must be between 1 byte and {maxUploadBytes} bytes.");
            }

            var content = memory.ToArray();
            var signature = DetectSignature(content);
            if (signature is null)
            {
                return FileValidationResult.Failed("Image file signature is not supported.");
            }

            if (!ExtensionMatchesSignature(extension, signature.Value))
            {
                return FileValidationResult.Failed("Image file extension does not match its content.");
            }

            var mimeType = NormalizeMimeType(request.ContentType, signature.Value);
            if (!AllowedMimeTypes.Contains(mimeType))
            {
                return FileValidationResult.Failed("Image content type is not supported.");
            }

            var dimensions = ReadDimensions(content, signature.Value);
            var hash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

            return FileValidationResult.Succeeded(
                new ValidatedImageFile(
                    content,
                    originalFileName,
                    NormalizeExtension(extension, signature.Value),
                    mimeType,
                    dimensions.Width,
                    dimensions.Height,
                    hash));
        }

        private string ResolvePhysicalPath(string storagePath)
        {
            var rootPath = Path.IsPathRooted(this.options.RootPath)
                ? this.options.RootPath
                : Path.GetFullPath(Path.Combine(this.environment.ContentRootPath, this.options.RootPath));

            return Path.GetFullPath(Path.Combine(rootPath, storagePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string BuildStoragePath(Guid storeId, Guid assetPublicId, string extension)
        {
            return Path.Combine(
                    "stores",
                    storeId.ToString("N"),
                    assetPublicId.ToString("N"),
                    "original" + extension.ToLowerInvariant())
                .Replace('\\', '/');
        }

        private static CommerceMediaAssetDto ToDto(CommerceMediaAsset asset)
        {
            return new CommerceMediaAssetDto(
                asset.PublicId,
                asset.StoreId,
                asset.OriginalFileName,
                asset.CanonicalFileName,
                asset.DisplayName,
                asset.AltText,
                asset.TitleText,
                $"/media/assets/{asset.PublicId:D}/{Uri.EscapeDataString(asset.CanonicalFileName)}",
                asset.MimeType,
                asset.Extension,
                asset.Width,
                asset.Height,
                asset.FileSizeBytes,
                asset.UpdatedAt.ToUnixTimeMilliseconds(),
                asset.CreatedAt,
                asset.UpdatedAt);
        }

        private static string BuildCanonicalFileName(string originalFileName, string extension)
        {
            var name = Path.GetFileNameWithoutExtension(originalFileName);
            var slug = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            return string.IsNullOrWhiteSpace(slug)
                ? "media-asset" + extension.ToLowerInvariant()
                : slug + extension.ToLowerInvariant();
        }

        private static string GenerateDisplayName(string originalFileName)
        {
            var name = Path.GetFileNameWithoutExtension(originalFileName);
            var words = Regex.Split(name, @"[^A-Za-z0-9]+")
                .Where(word => !string.IsNullOrWhiteSpace(word))
                .Select(ToTitleWord)
                .ToArray();

            return words.Length == 0 ? "Media Asset" : string.Join(' ', words);
        }

        private static string ToTitleWord(string word)
        {
            if (word.Length == 0)
            {
                return word;
            }

            return char.ToUpperInvariant(word[0]) + (word.Length == 1 ? string.Empty : word[1..].ToLowerInvariant());
        }

        private static ImageSignature? DetectSignature(byte[] content)
        {
            if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
            {
                return ImageSignature.Jpeg;
            }

            if (content.Length >= 8
                && content[0] == 0x89
                && content[1] == 0x50
                && content[2] == 0x4E
                && content[3] == 0x47
                && content[4] == 0x0D
                && content[5] == 0x0A
                && content[6] == 0x1A
                && content[7] == 0x0A)
            {
                return ImageSignature.Png;
            }

            if (content.Length >= 12
                && Encoding.ASCII.GetString(content, 0, 4) == "RIFF"
                && Encoding.ASCII.GetString(content, 8, 4) == "WEBP")
            {
                return ImageSignature.Webp;
            }

            if (content.Length >= 6)
            {
                var header = Encoding.ASCII.GetString(content, 0, 6);
                if (header is "GIF87a" or "GIF89a")
                {
                    return ImageSignature.Gif;
                }
            }

            if (content.Length >= HeaderLength && content[0] == 0 && content[1] == 0 && content[2] == 1 && content[3] == 0)
            {
                return ImageSignature.Ico;
            }

            return null;
        }

        private static bool ExtensionMatchesSignature(string extension, ImageSignature signature)
        {
            return signature switch
            {
                ImageSignature.Jpeg => extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase),
                ImageSignature.Png => extension.Equals(".png", StringComparison.OrdinalIgnoreCase),
                ImageSignature.Webp => extension.Equals(".webp", StringComparison.OrdinalIgnoreCase),
                ImageSignature.Gif => extension.Equals(".gif", StringComparison.OrdinalIgnoreCase),
                ImageSignature.Ico => extension.Equals(".ico", StringComparison.OrdinalIgnoreCase),
                _ => false,
            };
        }

        private static string NormalizeExtension(string extension, ImageSignature signature)
        {
            return signature == ImageSignature.Jpeg ? ".jpg" : extension.ToLowerInvariant();
        }

        private static string NormalizeMimeType(string? contentType, ImageSignature signature)
        {
            return signature switch
            {
                ImageSignature.Jpeg => "image/jpeg",
                ImageSignature.Png => "image/png",
                ImageSignature.Webp => "image/webp",
                ImageSignature.Gif => "image/gif",
                ImageSignature.Ico => "image/x-icon",
                _ => contentType?.Trim().ToLowerInvariant() ?? "application/octet-stream",
            };
        }

        private static ImageDimensions ReadDimensions(byte[] content, ImageSignature signature)
        {
            try
            {
                return signature switch
                {
                    ImageSignature.Png when content.Length >= 24 => new ImageDimensions(
                        ReadBigEndianInt32(content.AsSpan(16, 4)),
                        ReadBigEndianInt32(content.AsSpan(20, 4))),
                    ImageSignature.Gif when content.Length >= 10 => new ImageDimensions(
                        ReadLittleEndianUInt16(content.AsSpan(6, 2)),
                        ReadLittleEndianUInt16(content.AsSpan(8, 2))),
                    ImageSignature.Webp => ReadWebpDimensions(content),
                    ImageSignature.Ico when content.Length >= 8 => new ImageDimensions(
                        content[6] == 0 ? 256 : content[6],
                        content[7] == 0 ? 256 : content[7]),
                    _ => new ImageDimensions(null, null),
                };
            }
            catch
            {
                return new ImageDimensions(null, null);
            }
        }

        private static ImageDimensions ReadWebpDimensions(byte[] content)
        {
            if (content.Length < 30)
            {
                return new ImageDimensions(null, null);
            }

            var chunkType = Encoding.ASCII.GetString(content, 12, 4);
            if (chunkType == "VP8X" && content.Length >= 30)
            {
                var width = 1 + content[24] + (content[25] << 8) + (content[26] << 16);
                var height = 1 + content[27] + (content[28] << 8) + (content[29] << 16);
                return new ImageDimensions(width, height);
            }

            return new ImageDimensions(null, null);
        }

        private static int ReadBigEndianInt32(ReadOnlySpan<byte> value)
        {
            return (value[0] << 24) | (value[1] << 16) | (value[2] << 8) | value[3];
        }

        private static int ReadLittleEndianUInt16(ReadOnlySpan<byte> value)
        {
            return value[0] | (value[1] << 8);
        }

        private static int CalculateTotalPages(int totalCount, int pageSize)
        {
            return totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        }

        private static CommerceMediaAssetOperationResult<TPayload> Succeeded<TPayload>(
            TPayload payload,
            string? message = null)
        {
            return new CommerceMediaAssetOperationResult<TPayload>(true, message, payload);
        }

        private static CommerceMediaAssetOperationResult<TPayload> Failed<TPayload>(
            CommerceMediaAssetOperationFailure failure,
            string? message)
        {
            return new CommerceMediaAssetOperationResult<TPayload>(false, message, default, failure);
        }

        private sealed record StoreScopeResult(
            bool Success,
            Guid StoreId,
            string? Message = null,
            CommerceMediaAssetOperationFailure? Failure = null);

        private sealed record FileValidationResult(bool Success, ValidatedImageFile? File = null, string? Message = null)
        {
            public static FileValidationResult Succeeded(ValidatedImageFile file)
            {
                return new FileValidationResult(true, file);
            }

            public static FileValidationResult Failed(string message)
            {
                return new FileValidationResult(false, null, message);
            }
        }

        private sealed record ValidatedImageFile(
            byte[] Content,
            string OriginalFileName,
            string Extension,
            string MimeType,
            int? Width,
            int? Height,
            string ContentHash);

        private sealed record ImageDimensions(int? Width, int? Height);

        private enum ImageSignature
        {
            Jpeg,
            Png,
            Webp,
            Gif,
            Ico
        }
    }
}
