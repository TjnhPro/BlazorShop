namespace BlazorShop.Tests.Application.Services
{
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;

    using Xunit;

    public sealed class StoreSeoSlugPolicyServiceTests
    {
        private readonly Guid storeId = Guid.NewGuid();

        [Fact]
        public async Task ValidateSlugAsync_NormalizesToLowercaseSafeSlug()
        {
            var service = CreateService();

            var result = await service.ValidateSlugAsync(SeoSlugEntityTypes.Product, "Summer Sale 2026", this.storeId);

            Assert.True(result.Success);
            Assert.Equal("summer-sale-2026", result.Slug);
        }

        [Fact]
        public async Task ValidateSlugAsync_PreservesUnicodeLetters()
        {
            var service = CreateService();

            var result = await service.ValidateSlugAsync(SeoSlugEntityTypes.Category, "Товары Для Дома", this.storeId);

            Assert.True(result.Success);
            Assert.Equal("товары-для-дома", result.Slug);
        }

        [Theory]
        [InlineData("!!!", "Slug is invalid after normalization.")]
        [InlineData("summer/sale", "Slug must not contain slash characters.")]
        [InlineData("api", "Slug is reserved by a system route.")]
        public async Task ValidateSlugAsync_RejectsInvalidManualSlug(string slug, string expectedMessage)
        {
            var service = CreateService();

            var result = await service.ValidateSlugAsync(SeoSlugEntityTypes.Page, slug, this.storeId);

            Assert.False(result.Success);
            Assert.Equal(expectedMessage, result.Message);
        }

        [Fact]
        public async Task ValidateSlugAsync_WhenDuplicateExists_ReturnsFailure()
        {
            var service = CreateService("summer-sale");

            var result = await service.ValidateSlugAsync(SeoSlugEntityTypes.Product, "summer-sale", this.storeId);

            Assert.False(result.Success);
            Assert.Equal("Slug is already in use.", result.Message);
        }

        [Fact]
        public async Task GenerateSlugAsync_WhenBaseSlugExists_AppendsNextAvailableSuffix()
        {
            var service = CreateService("summer-sale", "summer-sale-2");

            var result = await service.GenerateSlugAsync(SeoSlugEntityTypes.Product, "Summer Sale", this.storeId);

            Assert.True(result.Success);
            Assert.Equal("summer-sale-3", result.Slug);
        }

        private static StoreSeoSlugPolicyService CreateService(params string[] existingSlugs)
        {
            return new StoreSeoSlugPolicyService(
                new SlugService(),
                [new FakeCollisionChecker(existingSlugs)]);
        }

        private sealed class FakeCollisionChecker : IStoreSeoSlugCollisionChecker
        {
            private readonly HashSet<string> existingSlugs;

            public FakeCollisionChecker(IEnumerable<string> existingSlugs)
            {
                this.existingSlugs = new HashSet<string>(existingSlugs, StringComparer.OrdinalIgnoreCase);
            }

            public Task<bool> SlugExistsAsync(
                string entityType,
                string slug,
                Guid? storeId,
                string? languageCode = null,
                Guid? excludedEntityId = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(this.existingSlugs.Contains(slug));
            }
        }
    }
}
