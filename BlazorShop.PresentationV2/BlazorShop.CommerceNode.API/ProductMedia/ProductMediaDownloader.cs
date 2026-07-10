namespace BlazorShop.CommerceNode.API.ProductMedia
{
    using System.Buffers.Binary;
    using System.Net;
    using System.Security.Cryptography;

    using BlazorShop.CommerceNode.API.Configuration;
    using BlazorShop.CommerceNode.API.Validation;

    using Microsoft.Extensions.Options;

    public sealed class ProductMediaDownloader : IProductMediaDownloader
    {
        private static readonly Dictionary<string, string> ExtensionsByContentType = new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["image/gif"] = ".gif",
        };

        private readonly HttpClient httpClient;
        private readonly IWebHostEnvironment environment;
        private readonly ProductMediaStorageOptions options;

        public ProductMediaDownloader(
            HttpClient httpClient,
            IWebHostEnvironment environment,
            IOptions<ProductMediaStorageOptions> options)
        {
            this.httpClient = httpClient;
            this.environment = environment;
            this.options = options.Value;
        }

        public async Task<ProductMediaDownloadResult> DownloadOriginalAsync(
            Guid storeId,
            Guid productId,
            Guid mediaPublicId,
            string sourceUrl,
            CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme is not ("http" or "https"))
            {
                return ProductMediaDownloadResult.Failed("Media source URL must be HTTP or HTTPS.");
            }

            var hostValidation = await ValidatePublicHostAsync(uri, cancellationToken);
            if (hostValidation is not null)
            {
                return ProductMediaDownloadResult.Failed(hostValidation);
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, this.options.DownloadTimeoutSeconds)));

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                using var response = await this.httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutCts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    return ProductMediaDownloadResult.Failed("Media source returned an unsuccessful status.");
                }

                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (string.IsNullOrWhiteSpace(contentType) ||
                    !ExtensionsByContentType.TryGetValue(contentType, out var extension))
                {
                    return ProductMediaDownloadResult.Failed("Media source content type is not supported.");
                }

                var rootPath = ResolveRootPath(this.environment.ContentRootPath, this.options.RootPath);
                var tempPath = Path.Combine(rootPath, "tmp", $"{Guid.NewGuid():N}.download");
                Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

                await using (var source = await response.Content.ReadAsStreamAsync(timeoutCts.Token))
                await using (var target = File.Create(tempPath))
                {
                    var copyResult = await CopyWithLimitAsync(source, target, this.options.MaxDownloadBytes, timeoutCts.Token);
                    if (!copyResult.Success)
                    {
                        File.Delete(tempPath);
                        return ProductMediaDownloadResult.Failed(copyResult.Message);
                    }
                }

                await using (var validationStream = File.OpenRead(tempPath))
                {
                    var validImage = await ImageFileSignatureValidator.IsValidAsync(validationStream, contentType, timeoutCts.Token);
                    if (!validImage)
                    {
                        File.Delete(tempPath);
                        return ProductMediaDownloadResult.Failed("Downloaded media content does not match its declared image type.");
                    }
                }

                var fileInfo = new FileInfo(tempPath);
                var fileSizeBytes = fileInfo.Length;
                var metadata = await ReadImageMetadataAsync(tempPath, contentType, timeoutCts.Token);
                var contentHash = await ComputeSha256Async(tempPath, timeoutCts.Token);
                var relativeStoragePath = string.Join(
                    '/',
                    "stores",
                    storeId.ToString("N"),
                    "products",
                    productId.ToString("N"),
                    mediaPublicId.ToString("N"),
                    $"original{extension}");
                var finalPath = Path.Combine(rootPath, relativeStoragePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);

                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }

                File.Move(tempPath, finalPath);

                return ProductMediaDownloadResult.Succeeded(
                    relativeStoragePath,
                    Path.GetFileName(uri.LocalPath),
                    contentType,
                    contentHash,
                    metadata.Width,
                    metadata.Height,
                    fileSizeBytes);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return ProductMediaDownloadResult.Failed("Media download timed out.");
            }
            catch (HttpRequestException)
            {
                return ProductMediaDownloadResult.Failed("Media source could not be downloaded.");
            }
            catch (IOException)
            {
                return ProductMediaDownloadResult.Failed("Media file could not be stored.");
            }
        }

        public static string ResolveRootPath(string contentRootPath, string configuredRootPath)
        {
            return Path.IsPathRooted(configuredRootPath)
                ? configuredRootPath
                : Path.GetFullPath(Path.Combine(contentRootPath, configuredRootPath));
        }

        private static async Task<string?> ValidatePublicHostAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return "Media source host is not allowed.";
            }

            IPAddress[] addresses;
            try
            {
                addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);
            }
            catch
            {
                return "Media source host could not be resolved.";
            }

            return addresses.Length == 0 || addresses.Any(IsPrivateOrLocalAddress)
                ? "Media source host resolves to a private or local address."
                : null;
        }

        private static bool IsPrivateOrLocalAddress(IPAddress address)
        {
            if (IPAddress.IsLoopback(address))
            {
                return true;
            }

            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                var bytes = address.GetAddressBytes();
                return address.IsIPv6LinkLocal || (bytes[0] & 0xFE) == 0xFC;
            }

            var ipv4 = address.MapToIPv4().GetAddressBytes();
            return ipv4[0] == 10
                   || ipv4[0] == 127
                   || (ipv4[0] == 172 && ipv4[1] >= 16 && ipv4[1] <= 31)
                   || (ipv4[0] == 192 && ipv4[1] == 168)
                   || (ipv4[0] == 169 && ipv4[1] == 254);
        }

        private static async Task<CopyResult> CopyWithLimitAsync(
            Stream source,
            Stream target,
            long maxBytes,
            CancellationToken cancellationToken)
        {
            var buffer = new byte[81920];
            long totalBytes = 0;

            while (true)
            {
                var bytesRead = await source.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0)
                {
                    return CopyResult.Succeeded();
                }

                totalBytes += bytesRead;
                if (totalBytes > maxBytes)
                {
                    return CopyResult.Failed($"Media file is too large. Max {maxBytes / (1024 * 1024)}MB.");
                }

                await target.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }
        }

        private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
        {
            await using var stream = File.OpenRead(path);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static async Task<ImageMetadata> ReadImageMetadataAsync(
            string path,
            string contentType,
            CancellationToken cancellationToken)
        {
            var buffer = new byte[64 * 1024];
            await using var stream = File.OpenRead(path);
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            var data = buffer.AsSpan(0, bytesRead);

            return contentType.ToLowerInvariant() switch
            {
                "image/png" when data.Length >= 24 => new ImageMetadata(
                    BinaryPrimitives.ReadInt32BigEndian(data.Slice(16, 4)),
                    BinaryPrimitives.ReadInt32BigEndian(data.Slice(20, 4))),
                "image/gif" when data.Length >= 10 => new ImageMetadata(
                    BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(6, 2)),
                    BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8, 2))),
                "image/jpeg" => ReadJpegMetadata(data),
                "image/webp" => ReadWebpMetadata(data),
                _ => new ImageMetadata(null, null),
            };
        }

        private static ImageMetadata ReadJpegMetadata(ReadOnlySpan<byte> data)
        {
            var index = 2;
            while (index + 9 < data.Length)
            {
                if (data[index] != 0xFF)
                {
                    index++;
                    continue;
                }

                var marker = data[index + 1];
                var length = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(index + 2, 2));
                if (length < 2 || index + 2 + length > data.Length)
                {
                    break;
                }

                if (marker is >= 0xC0 and <= 0xC3)
                {
                    return new ImageMetadata(
                        BinaryPrimitives.ReadUInt16BigEndian(data.Slice(index + 7, 2)),
                        BinaryPrimitives.ReadUInt16BigEndian(data.Slice(index + 5, 2)));
                }

                index += 2 + length;
            }

            return new ImageMetadata(null, null);
        }

        private static ImageMetadata ReadWebpMetadata(ReadOnlySpan<byte> data)
        {
            if (data.Length < 30)
            {
                return new ImageMetadata(null, null);
            }

            var chunk = System.Text.Encoding.ASCII.GetString(data.Slice(12, 4));
            if (chunk == "VP8X" && data.Length >= 30)
            {
                var width = 1 + data[24] + (data[25] << 8) + (data[26] << 16);
                var height = 1 + data[27] + (data[28] << 8) + (data[29] << 16);
                return new ImageMetadata(width, height);
            }

            return new ImageMetadata(null, null);
        }

        private sealed record CopyResult(bool Success, string Message)
        {
            public static CopyResult Succeeded()
            {
                return new CopyResult(true, "Copied.");
            }

            public static CopyResult Failed(string message)
            {
                return new CopyResult(false, message);
            }
        }

        private sealed record ImageMetadata(int? Width, int? Height);
    }
}
