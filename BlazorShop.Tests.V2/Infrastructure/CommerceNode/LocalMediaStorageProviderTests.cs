namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Xunit;

    public sealed class LocalMediaStorageProviderTests : IDisposable
    {
        private readonly string root;
        private readonly LocalMediaStorageProvider provider = new();

        public LocalMediaStorageProviderTests()
        {
            this.root = Path.Combine(Path.GetTempPath(), "blazorshop-media-storage-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(this.root);
        }

        [Fact]
        public void BuildStoragePath_NormalizesSeparators()
        {
            var path = this.provider.BuildStoragePath("stores", "abc\\products", "media", "original.png");

            Assert.Equal("stores/abc/products/media/original.png", path);
        }

        [Theory]
        [InlineData("../outside.png")]
        [InlineData("stores/../outside.png")]
        [InlineData("C:/outside.png")]
        public void ResolvePhysicalPath_RejectsTraversalAndAbsolutePaths(string storagePath)
        {
            Assert.ThrowsAny<Exception>(() => this.provider.ResolvePhysicalPath(this.root, "media", storagePath));
        }

        [Fact]
        public async Task WriteReadMoveAndDelete_PreservesRootedStorageLayout()
        {
            var source = this.provider.BuildStoragePath("stores", "store-a", "asset-a", "original.png");
            var destination = this.provider.BuildStoragePath("stores", "store-a", "asset-b", "original.png");
            var content = new byte[] { 1, 2, 3 };

            await this.provider.WriteAllBytesAsync(this.root, "media", source, content);

            Assert.True(this.provider.FileExists(this.root, "media", source));
            Assert.Equal(3, this.provider.GetFileSize(this.root, "media", source));

            await using (var stream = this.provider.OpenRead(this.root, "media", source))
            {
                var buffer = new byte[3];
                var read = await stream.ReadAsync(buffer);
                Assert.Equal(3, read);
                Assert.Equal(content, buffer);
            }

            await this.provider.MoveAsync(this.root, "media", source, destination, replace: true);

            Assert.False(this.provider.FileExists(this.root, "media", source));
            Assert.True(this.provider.FileExists(this.root, "media", destination));

            await this.provider.DeleteDirectoryIfExistsAsync(
                this.root,
                "media",
                Path.GetDirectoryName(destination) ?? string.Empty,
                recursive: true);

            Assert.False(this.provider.FileExists(this.root, "media", destination));
        }

        public void Dispose()
        {
            if (Directory.Exists(this.root))
            {
                Directory.Delete(this.root, recursive: true);
            }
        }
    }
}
