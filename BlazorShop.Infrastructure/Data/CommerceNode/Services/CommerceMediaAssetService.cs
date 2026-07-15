namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Globalization;
    using System.Security.Cryptography;
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

        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly CommerceMediaStorageOptions options;
        private readonly IHostEnvironment environment;
        private readonly IMediaStorageProvider storageProvider;
        private readonly ICommerceMediaUrlBuilder urlBuilder;

        public CommerceMediaAssetService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            IOptions<CommerceMediaStorageOptions> options,
            IHostEnvironment environment,
            IMediaStorageProvider storageProvider,
            ICommerceMediaUrlBuilder urlBuilder)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.options = options.Value;
            this.environment = environment;
            this.storageProvider = storageProvider;
            this.urlBuilder = urlBuilder;
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
            var assetRows = await assets
                .OrderByDescending(asset => asset.UpdatedAt)
                .ThenBy(asset => asset.DisplayName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            var items = assetRows.Select(this.ToDto).ToList();

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
                : Succeeded(this.ToDto(asset));
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
            var storagePath = this.BuildStoragePath(scope.StoreId, publicId, file.Extension);
            await this.storageProvider.WriteAllBytesAsync(
                this.environment.ContentRootPath,
                this.options.RootPath,
                storagePath,
                file.Content,
                cancellationToken);

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
            return Succeeded(this.ToDto(asset), "Media asset uploaded.");
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
            return Succeeded(this.ToDto(asset), "Media asset updated.");
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
            var storagePath = this.BuildStoragePath(asset.StoreId, asset.PublicId, file.Extension);
            await this.storageProvider.WriteAllBytesAsync(
                this.environment.ContentRootPath,
                this.options.RootPath,
                storagePath,
                file.Content,
                cancellationToken);
            if (!asset.OriginalStoragePath.Equals(storagePath, StringComparison.OrdinalIgnoreCase))
            {
                await this.storageProvider.DeleteFileIfExistsAsync(
                    this.environment.ContentRootPath,
                    this.options.RootPath,
                    asset.OriginalStoragePath,
                    cancellationToken);
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
            return Succeeded(this.ToDto(asset), "Media asset replaced.");
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

            await this.storageProvider.DeleteDirectoryIfExistsAsync(
                this.environment.ContentRootPath,
                this.options.RootPath,
                Path.GetDirectoryName(asset.OriginalStoragePath) ?? string.Empty,
                recursive: true,
                cancellationToken);

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
            if (!MediaFilePolicy.SupportedGenericImageExtensions.Contains(extension))
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
            var fileType = MediaFilePolicy.DetectImageType(content);
            if (fileType == MediaFileType.Unknown)
            {
                return FileValidationResult.Failed("Image file signature is not supported.");
            }

            if (!MediaFilePolicy.ExtensionMatchesType(extension, fileType))
            {
                return FileValidationResult.Failed("Image file extension does not match its content.");
            }

            var mimeType = MediaFilePolicy.NormalizeMimeType(request.ContentType, fileType);
            if (!MediaFilePolicy.SupportedGenericImageMimeTypes.Contains(mimeType))
            {
                return FileValidationResult.Failed("Image content type is not supported.");
            }

            var dimensions = MediaFilePolicy.ReadBasicDimensions(content, fileType);
            var hash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

            return FileValidationResult.Succeeded(
                new ValidatedImageFile(
                    content,
                    originalFileName,
                    MediaFilePolicy.NormalizeExtension(extension, fileType),
                    mimeType,
                    dimensions.Width,
                    dimensions.Height,
                    hash));
        }

        private string BuildStoragePath(Guid storeId, Guid assetPublicId, string extension)
        {
            return this.storageProvider.BuildStoragePath(
                    "stores",
                    storeId.ToString("N"),
                    assetPublicId.ToString("N"),
                    "original" + extension.ToLowerInvariant());
        }

        private CommerceMediaAssetDto ToDto(CommerceMediaAsset asset)
        {
            return new CommerceMediaAssetDto(
                asset.PublicId,
                asset.StoreId,
                asset.OriginalFileName,
                asset.CanonicalFileName,
                asset.DisplayName,
                asset.AltText,
                asset.TitleText,
                this.urlBuilder.BuildAssetUrl(asset.PublicId, asset.CanonicalFileName),
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

    }
}
