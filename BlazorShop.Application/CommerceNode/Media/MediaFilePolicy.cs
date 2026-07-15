namespace BlazorShop.Application.CommerceNode.Media
{
    using System.Buffers.Binary;
    using System.Text;

    public enum MediaFileType
    {
        Unknown = 0,
        Jpeg,
        Png,
        Webp,
        Gif,
        Ico,
        Bmp,
    }

    public sealed record MediaImageDimensions(int? Width, int? Height);

    public static class MediaFilePolicy
    {
        public const int MaxSignatureLength = 32;

        private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
        private static readonly byte[] BmpSignature = [0x42, 0x4D];
        private static readonly byte[] Gif87aSignature = Encoding.ASCII.GetBytes("GIF87a");
        private static readonly byte[] Gif89aSignature = Encoding.ASCII.GetBytes("GIF89a");
        private static readonly byte[] RiffSignature = Encoding.ASCII.GetBytes("RIFF");
        private static readonly byte[] WebpSignature = Encoding.ASCII.GetBytes("WEBP");

        public static readonly IReadOnlySet<string> SupportedGenericImageExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp",
                ".gif",
                ".ico",
            };

        public static readonly IReadOnlySet<string> SupportedGenericImageMimeTypes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/png",
                "image/webp",
                "image/gif",
                "image/x-icon",
                "image/vnd.microsoft.icon",
            };

        public static readonly IReadOnlySet<string> SupportedProductDownloadMimeTypes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/png",
                "image/webp",
                "image/gif",
            };

        public static async Task<bool> IsValidImageSignatureAsync(
            Stream stream,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            if (stream is null || !stream.CanRead || string.IsNullOrWhiteSpace(contentType))
            {
                return false;
            }

            var header = new byte[12];
            var bytesRead = await ReadHeaderAsync(stream, header, cancellationToken);
            if (bytesRead == 0)
            {
                return false;
            }

            var type = DetectImageType(header.AsSpan(0, bytesRead));
            return type != MediaFileType.Unknown
                && ContentTypeMatchesType(contentType, type);
        }

        public static MediaFileType DetectImageType(ReadOnlySpan<byte> content)
        {
            if (content.Length >= 3 && content.StartsWith(JpegSignature))
            {
                return MediaFileType.Jpeg;
            }

            if (content.Length >= PngSignature.Length && content[..PngSignature.Length].SequenceEqual(PngSignature))
            {
                return MediaFileType.Png;
            }

            if (content.Length >= 12
                && content[..4].SequenceEqual(RiffSignature)
                && content.Slice(8, 4).SequenceEqual(WebpSignature))
            {
                return MediaFileType.Webp;
            }

            if (content.Length >= Gif87aSignature.Length
                && (content[..Gif87aSignature.Length].SequenceEqual(Gif87aSignature)
                    || content[..Gif89aSignature.Length].SequenceEqual(Gif89aSignature)))
            {
                return MediaFileType.Gif;
            }

            if (content.Length >= MaxSignatureLength
                && content[0] == 0
                && content[1] == 0
                && content[2] == 1
                && content[3] == 0)
            {
                return MediaFileType.Ico;
            }

            if (content.Length >= BmpSignature.Length && content[..BmpSignature.Length].SequenceEqual(BmpSignature))
            {
                return MediaFileType.Bmp;
            }

            return MediaFileType.Unknown;
        }

        public static bool ExtensionMatchesType(string extension, MediaFileType type)
        {
            return type switch
            {
                MediaFileType.Jpeg => extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase),
                MediaFileType.Png => extension.Equals(".png", StringComparison.OrdinalIgnoreCase),
                MediaFileType.Webp => extension.Equals(".webp", StringComparison.OrdinalIgnoreCase),
                MediaFileType.Gif => extension.Equals(".gif", StringComparison.OrdinalIgnoreCase),
                MediaFileType.Ico => extension.Equals(".ico", StringComparison.OrdinalIgnoreCase),
                MediaFileType.Bmp => extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase),
                _ => false,
            };
        }

        public static bool ContentTypeMatchesType(string contentType, MediaFileType type)
        {
            return type switch
            {
                MediaFileType.Jpeg => contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase),
                MediaFileType.Png => contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase),
                MediaFileType.Webp => contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase),
                MediaFileType.Gif => contentType.Equals("image/gif", StringComparison.OrdinalIgnoreCase),
                MediaFileType.Ico => contentType.Equals("image/x-icon", StringComparison.OrdinalIgnoreCase)
                    || contentType.Equals("image/vnd.microsoft.icon", StringComparison.OrdinalIgnoreCase),
                MediaFileType.Bmp => contentType.Equals("image/bmp", StringComparison.OrdinalIgnoreCase),
                _ => false,
            };
        }

        public static string NormalizeExtension(string extension, MediaFileType type)
        {
            return type == MediaFileType.Jpeg
                ? ".jpg"
                : extension.ToLowerInvariant();
        }

        public static string NormalizeMimeType(string? contentType, MediaFileType type)
        {
            return type switch
            {
                MediaFileType.Jpeg => "image/jpeg",
                MediaFileType.Png => "image/png",
                MediaFileType.Webp => "image/webp",
                MediaFileType.Gif => "image/gif",
                MediaFileType.Ico => "image/x-icon",
                MediaFileType.Bmp => "image/bmp",
                _ => contentType?.Trim().ToLowerInvariant() ?? "application/octet-stream",
            };
        }

        public static string? GetPreferredExtensionForContentType(string? contentType)
        {
            return contentType?.Trim().ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => null,
            };
        }

        public static MediaImageDimensions ReadBasicDimensions(byte[] content, MediaFileType type)
        {
            try
            {
                return type switch
                {
                    MediaFileType.Png when content.Length >= 24 => new MediaImageDimensions(
                        BinaryPrimitives.ReadInt32BigEndian(content.AsSpan(16, 4)),
                        BinaryPrimitives.ReadInt32BigEndian(content.AsSpan(20, 4))),
                    MediaFileType.Gif when content.Length >= 10 => new MediaImageDimensions(
                        BinaryPrimitives.ReadUInt16LittleEndian(content.AsSpan(6, 2)),
                        BinaryPrimitives.ReadUInt16LittleEndian(content.AsSpan(8, 2))),
                    MediaFileType.Webp => ReadWebpDimensions(content),
                    MediaFileType.Ico when content.Length >= 8 => new MediaImageDimensions(
                        content[6] == 0 ? 256 : content[6],
                        content[7] == 0 ? 256 : content[7]),
                    _ => new MediaImageDimensions(null, null),
                };
            }
            catch
            {
                return new MediaImageDimensions(null, null);
            }
        }

        private static async Task<int> ReadHeaderAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            var totalRead = 0;

            while (totalRead < buffer.Length)
            {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                totalRead += bytesRead;
            }

            return totalRead;
        }

        private static MediaImageDimensions ReadWebpDimensions(byte[] content)
        {
            if (content.Length < 30)
            {
                return new MediaImageDimensions(null, null);
            }

            var chunkType = Encoding.ASCII.GetString(content, 12, 4);
            if (chunkType == "VP8X" && content.Length >= 30)
            {
                var width = 1 + content[24] + (content[25] << 8) + (content[26] << 16);
                var height = 1 + content[27] + (content[28] << 8) + (content[29] << 16);
                return new MediaImageDimensions(width, height);
            }

            return new MediaImageDimensions(null, null);
        }
    }
}
