namespace BlazorShop.Application.Services
{
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Domain.Constants;

    public sealed class StoreSeoSlugPolicyService : IStoreSeoSlugPolicyService
    {
        private const int MaxSuffixAttempts = 100;

        private static readonly HashSet<string> ReservedSegments = new(StringComparer.OrdinalIgnoreCase)
        {
            "api",
            "admin",
            "commerce",
            "control-plane",
            "storefront",
            "media",
            "uploads",
            "css",
            "js",
            "images",
            "_framework",
            "_content",
            "_blazor",
            "swagger",
            "signin",
            "register",
            "logout",
            "my-cart",
            "cart",
            "checkout",
            "search",
            "new-releases",
            "todays-deals",
            "payment-success",
            "payment-cancel",
            "maintenance",
        };

        private readonly ISlugService slugService;
        private readonly IReadOnlyList<IStoreSeoSlugCollisionChecker> collisionCheckers;

        public StoreSeoSlugPolicyService(
            ISlugService slugService,
            IEnumerable<IStoreSeoSlugCollisionChecker> collisionCheckers)
        {
            this.slugService = slugService;
            this.collisionCheckers = collisionCheckers.ToArray();
        }

        public async Task<StoreSeoSlugPolicyResult> GenerateSlugAsync(
            string entityType,
            string? sourceName,
            Guid? storeId,
            string? languageCode = null,
            Guid? excludedEntityId = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedEntityType = SeoSlugEntityTypes.Normalize(entityType);
            if (!SeoSlugEntityTypes.IsKnown(normalizedEntityType))
            {
                return StoreSeoSlugPolicyResult.Failed("SEO slug entity type is not supported.");
            }

            var baseSlug = this.slugService.NormalizeSlug(sourceName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                return StoreSeoSlugPolicyResult.Failed("Slug is invalid after normalization.");
            }

            baseSlug = TrimToMaxLength(baseSlug, SeoConstraints.SlugMaxLength);
            for (var attempt = 0; attempt < MaxSuffixAttempts; attempt++)
            {
                var candidate = attempt == 0
                    ? baseSlug
                    : WithSuffix(baseSlug, attempt + 1);

                var validation = await this.ValidateSlugAsync(
                    normalizedEntityType,
                    candidate,
                    storeId,
                    languageCode,
                    excludedEntityId,
                    cancellationToken);

                if (validation.Success)
                {
                    return validation;
                }
            }

            return StoreSeoSlugPolicyResult.Failed("A unique slug could not be generated.");
        }

        public async Task<StoreSeoSlugPolicyResult> ValidateSlugAsync(
            string entityType,
            string? slug,
            Guid? storeId,
            string? languageCode = null,
            Guid? excludedEntityId = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedEntityType = SeoSlugEntityTypes.Normalize(entityType);
            if (!SeoSlugEntityTypes.IsKnown(normalizedEntityType))
            {
                return StoreSeoSlugPolicyResult.Failed("SEO slug entity type is not supported.");
            }

            if (string.IsNullOrWhiteSpace(slug))
            {
                return StoreSeoSlugPolicyResult.Failed("Slug is required.");
            }

            if (slug.Contains('/', StringComparison.Ordinal) || slug.Contains('\\', StringComparison.Ordinal))
            {
                return StoreSeoSlugPolicyResult.Failed("Slug must not contain slash characters.");
            }

            var normalizedSlug = this.slugService.NormalizeSlug(slug);
            if (string.IsNullOrWhiteSpace(normalizedSlug))
            {
                return StoreSeoSlugPolicyResult.Failed("Slug is invalid after normalization.");
            }

            // Preserve the current Unicode policy: letters and digits from non-Latin scripts remain valid.
            if (!this.slugService.IsSlugSafe(normalizedSlug))
            {
                return StoreSeoSlugPolicyResult.Failed("Slug is not URL safe.");
            }

            if (normalizedSlug.Length > SeoConstraints.SlugMaxLength)
            {
                return StoreSeoSlugPolicyResult.Failed($"Slug must be {SeoConstraints.SlugMaxLength} characters or fewer.");
            }

            if (ReservedSegments.Contains(normalizedSlug))
            {
                return StoreSeoSlugPolicyResult.Failed("Slug is reserved by a system route.");
            }

            if (await this.SlugExistsAsync(normalizedEntityType, normalizedSlug, storeId, languageCode, excludedEntityId, cancellationToken))
            {
                return StoreSeoSlugPolicyResult.Failed("Slug is already in use.");
            }

            return StoreSeoSlugPolicyResult.Succeeded(normalizedSlug);
        }

        private async Task<bool> SlugExistsAsync(
            string entityType,
            string slug,
            Guid? storeId,
            string? languageCode,
            Guid? excludedEntityId,
            CancellationToken cancellationToken)
        {
            foreach (var checker in this.collisionCheckers)
            {
                if (await checker.SlugExistsAsync(entityType, slug, storeId, languageCode, excludedEntityId, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        private static string WithSuffix(string baseSlug, int suffix)
        {
            var suffixText = $"-{suffix}";
            var maxBaseLength = Math.Max(1, SeoConstraints.SlugMaxLength - suffixText.Length);
            return $"{TrimToMaxLength(baseSlug, maxBaseLength)}{suffixText}";
        }

        private static string TrimToMaxLength(string value, int maxLength)
        {
            if (value.Length <= maxLength)
            {
                return value;
            }

            return value[..maxLength].Trim('-');
        }
    }
}
