namespace BlazorShop.Tests.Application.CommerceNode.Media
{
    using BlazorShop.Application.CommerceNode.Media;

    using Xunit;

    public sealed class MediaFilePolicyTests
    {
        [Theory]
        [InlineData("image/jpeg", MediaFileType.Jpeg)]
        [InlineData("image/png", MediaFileType.Png)]
        [InlineData("image/webp", MediaFileType.Webp)]
        [InlineData("image/gif", MediaFileType.Gif)]
        [InlineData("image/bmp", MediaFileType.Bmp)]
        public async Task IsValidImageSignatureAsync_AcceptsMatchingContentType(string contentType, MediaFileType type)
        {
            await using var stream = new MemoryStream(CreateHeader(type));

            var result = await MediaFilePolicy.IsValidImageSignatureAsync(stream, contentType);

            Assert.True(result);
        }

        [Fact]
        public async Task IsValidImageSignatureAsync_RejectsMismatchedContentType()
        {
            await using var stream = new MemoryStream(CreateHeader(MediaFileType.Png));

            var result = await MediaFilePolicy.IsValidImageSignatureAsync(stream, "image/jpeg");

            Assert.False(result);
        }

        [Theory]
        [InlineData(".jpeg", MediaFileType.Jpeg, true)]
        [InlineData(".jpg", MediaFileType.Jpeg, true)]
        [InlineData(".png", MediaFileType.Jpeg, false)]
        [InlineData(".ico", MediaFileType.Ico, true)]
        public void ExtensionMatchesType_UsesCurrentMediaCompatibilityRules(
            string extension,
            MediaFileType type,
            bool expected)
        {
            Assert.Equal(expected, MediaFilePolicy.ExtensionMatchesType(extension, type));
        }

        [Fact]
        public void ReadBasicDimensions_ReadsPngDimensions()
        {
            var bytes = CreateHeader(MediaFileType.Png, width: 640, height: 480);

            var dimensions = MediaFilePolicy.ReadBasicDimensions(bytes, MediaFileType.Png);

            Assert.Equal(640, dimensions.Width);
            Assert.Equal(480, dimensions.Height);
        }

        private static byte[] CreateHeader(MediaFileType type, int width = 1, int height = 1)
        {
            return type switch
            {
                MediaFileType.Jpeg => [0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0],
                MediaFileType.Png => CreatePngHeader(width, height),
                MediaFileType.Webp => CreateWebpHeader(width, height),
                MediaFileType.Gif => [.. "GIF89a"u8.ToArray(), 1, 0, 1, 0, 0, 0],
                MediaFileType.Bmp => [0x42, 0x4D, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
                MediaFileType.Ico => CreateIcoHeader(width, height),
                _ => [],
            };
        }

        private static byte[] CreatePngHeader(int width, int height)
        {
            var bytes = new byte[24];
            bytes[0] = 0x89;
            bytes[1] = 0x50;
            bytes[2] = 0x4E;
            bytes[3] = 0x47;
            bytes[4] = 0x0D;
            bytes[5] = 0x0A;
            bytes[6] = 0x1A;
            bytes[7] = 0x0A;
            WriteBigEndian(bytes.AsSpan(16, 4), width);
            WriteBigEndian(bytes.AsSpan(20, 4), height);
            return bytes;
        }

        private static byte[] CreateWebpHeader(int width, int height)
        {
            var bytes = new byte[30];
            "RIFF"u8.CopyTo(bytes);
            "WEBP"u8.CopyTo(bytes.AsSpan(8));
            "VP8X"u8.CopyTo(bytes.AsSpan(12));
            var encodedWidth = width - 1;
            var encodedHeight = height - 1;
            bytes[24] = (byte)(encodedWidth & 0xFF);
            bytes[25] = (byte)((encodedWidth >> 8) & 0xFF);
            bytes[26] = (byte)((encodedWidth >> 16) & 0xFF);
            bytes[27] = (byte)(encodedHeight & 0xFF);
            bytes[28] = (byte)((encodedHeight >> 8) & 0xFF);
            bytes[29] = (byte)((encodedHeight >> 16) & 0xFF);
            return bytes;
        }

        private static byte[] CreateIcoHeader(int width, int height)
        {
            var bytes = new byte[MediaFilePolicy.MaxSignatureLength];
            bytes[2] = 1;
            bytes[6] = (byte)width;
            bytes[7] = (byte)height;
            return bytes;
        }

        private static void WriteBigEndian(Span<byte> destination, int value)
        {
            destination[0] = (byte)((value >> 24) & 0xFF);
            destination[1] = (byte)((value >> 16) & 0xFF);
            destination[2] = (byte)((value >> 8) & 0xFF);
            destination[3] = (byte)(value & 0xFF);
        }
    }
}
