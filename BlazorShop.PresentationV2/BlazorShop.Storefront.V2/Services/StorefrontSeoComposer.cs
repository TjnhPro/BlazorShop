namespace BlazorShop.Storefront.Services
{
    using BlazorShop.Storefront.Models;
    using BlazorShop.Storefront.Services.Contracts;

    public class StorefrontSeoComposer : IStorefrontSeoComposer
    {
        private readonly IStorefrontPublicUrlResolver _publicUrlResolver;
        private readonly IStorefrontSeoSettingsProvider _settingsProvider;

        public StorefrontSeoComposer(
            IStorefrontPublicUrlResolver publicUrlResolver,
            IStorefrontSeoSettingsProvider settingsProvider)
        {
            _publicUrlResolver = publicUrlResolver;
            _settingsProvider = settingsProvider;
        }

        public async Task<SeoMetadataDto> ComposeStaticPageAsync(string title, string relativePath, string fallbackMetaDescription, CancellationToken cancellationToken = default)
        {
            var settings = await GetEffectiveSettingsAsync(cancellationToken);
            return BuildMetadata(new SeoMetadataBuildRequest
            {
                PageTitle = title,
                RelativePath = relativePath,
                Settings = settings,
                PageSeo = new SeoFieldsDto
                {
                    MetaDescription = fallbackMetaDescription,
                },
            });
        }

        public async Task<SeoMetadataDto> ComposeHomePageAsync(GetStorefrontPage? homePage, string fallbackTitle, string fallbackMetaDescription, CancellationToken cancellationToken = default)
        {
            var settings = await GetEffectiveSettingsAsync(cancellationToken);
            return BuildMetadata(new SeoMetadataBuildRequest
            {
                PageTitle = string.IsNullOrWhiteSpace(homePage?.Title) ? fallbackTitle : homePage.Title,
                RelativePath = StorefrontRoutes.Home,
                Settings = settings,
                PageSeo = homePage is null
                    ? new SeoFieldsDto
                    {
                        MetaDescription = fallbackMetaDescription,
                    }
                    : new SeoFieldsDto
                    {
                        MetaTitle = homePage.Seo.MetaTitle,
                        MetaDescription = string.IsNullOrWhiteSpace(homePage.Seo.MetaDescription) ? Truncate(homePage.Intro, 160) : homePage.Seo.MetaDescription,
                        OgTitle = homePage.Seo.OgTitle,
                        OgDescription = homePage.Seo.OgDescription,
                        OgImage = homePage.Seo.OgImage,
                        RobotsIndex = homePage.Seo.RobotsIndex,
                        RobotsFollow = homePage.Seo.RobotsFollow,
                    },
            });
        }

        public async Task<SeoMetadataDto> ComposeCategoryPageAsync(GetCategory category, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(category);

            var settings = await GetEffectiveSettingsAsync(cancellationToken);
            return BuildMetadata(new SeoMetadataBuildRequest
            {
                PageTitle = $"{category.Name} Products",
                RelativePath = StorefrontRoutes.Category(category.Slug),
                Settings = settings,
                PageSeo = MapCategorySeo(category, $"Browse {category.Name} products, descriptions, pricing, and availability in the BlazorShop catalog."),
            });
        }

        public async Task<SeoMetadataDto> ComposeProductPageAsync(GetProduct product, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(product);

            var settings = await GetEffectiveSettingsAsync(cancellationToken);
            return BuildMetadata(new SeoMetadataBuildRequest
            {
                PageTitle = product.Name,
                RelativePath = StorefrontRoutes.Product(product.Slug),
                Settings = settings,
                PageSeo = MapProductSeo(product, Truncate(product.Description, 160)),
            });
        }

        public async Task<SeoMetadataDto> ComposeStorefrontPageAsync(GetStorefrontPage page, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(page);

            var settings = await GetEffectiveSettingsAsync(cancellationToken);
            return BuildMetadata(new SeoMetadataBuildRequest
            {
                PageTitle = page.Title,
                RelativePath = StorefrontRoutes.Page(page.Slug),
                Settings = settings,
                PageSeo = new SeoFieldsDto
                {
                    MetaTitle = page.Seo.MetaTitle,
                    MetaDescription = string.IsNullOrWhiteSpace(page.Seo.MetaDescription) ? Truncate(page.Intro, 160) : page.Seo.MetaDescription,
                    CanonicalUrl = page.Seo.CanonicalUrl,
                    OgTitle = page.Seo.OgTitle,
                    OgDescription = page.Seo.OgDescription,
                    OgImage = page.Seo.OgImage,
                    RobotsIndex = page.Seo.RobotsIndex,
                    RobotsFollow = page.Seo.RobotsFollow,
                },
            });
        }

        public async Task<SeoMetadataDto> ComposeServiceUnavailablePageAsync(string title, string relativePath, string fallbackMetaDescription, CancellationToken cancellationToken = default)
        {
            var settings = await GetEffectiveSettingsAsync(cancellationToken);
            return BuildMetadata(new SeoMetadataBuildRequest
            {
                PageTitle = title,
                RelativePath = relativePath,
                SuppressCanonicalUrl = true,
                SuppressOpenGraph = true,
                Settings = settings,
                PageSeo = new SeoFieldsDto
                {
                    MetaDescription = fallbackMetaDescription,
                    RobotsIndex = false,
                    RobotsFollow = false,
                },
            });
        }

        public async Task<SeoMetadataDto> ComposeNotFoundPageAsync(string title, string relativePath, string fallbackMetaDescription, CancellationToken cancellationToken = default)
        {
            var settings = await GetEffectiveSettingsAsync(cancellationToken);
            return BuildMetadata(new SeoMetadataBuildRequest
            {
                PageTitle = title,
                RelativePath = relativePath,
                SuppressCanonicalUrl = true,
                SuppressOpenGraph = true,
                Settings = settings,
                PageSeo = new SeoFieldsDto
                {
                    MetaDescription = fallbackMetaDescription,
                    RobotsIndex = false,
                    RobotsFollow = false,
                },
            });
        }

        private static SeoFieldsDto MapCategorySeo(GetCategory category, string fallbackMetaDescription)
        {
            return new SeoFieldsDto
            {
                MetaTitle = category.MetaTitle,
                MetaDescription = string.IsNullOrWhiteSpace(category.MetaDescription) ? fallbackMetaDescription : category.MetaDescription,
                OgTitle = category.OgTitle,
                OgDescription = category.OgDescription,
                OgImage = category.OgImage,
                RobotsIndex = category.RobotsIndex,
                RobotsFollow = category.RobotsFollow,
            };
        }

        private static SeoFieldsDto MapProductSeo(GetProduct product, string fallbackMetaDescription)
        {
            return new SeoFieldsDto
            {
                MetaTitle = product.MetaTitle,
                MetaDescription = string.IsNullOrWhiteSpace(product.MetaDescription) ? fallbackMetaDescription : product.MetaDescription,
                OgTitle = product.OgTitle,
                OgDescription = product.OgDescription,
                OgImage = product.OgImage,
                RobotsIndex = product.RobotsIndex,
                RobotsFollow = product.RobotsFollow,
            };
        }

        private static SeoMetadataDto BuildMetadata(SeoMetadataBuildRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var title = AppendTitleSuffix(
                FirstNonEmpty(request.PageSeo?.MetaTitle, request.PageTitle, request.Settings?.SiteName),
                request.Settings?.DefaultTitleSuffix);
            var metaDescription = FirstNonEmpty(request.PageSeo?.MetaDescription, request.Settings?.DefaultMetaDescription);
            var canonicalUrl = request.SuppressCanonicalUrl
                ? null
                : ResolveCanonicalUrl(request.PageSeo?.CanonicalUrl, request.Settings?.BaseCanonicalUrl, request.RelativePath);
            var suppressOpenGraph = request.SuppressOpenGraph;

            return new SeoMetadataDto
            {
                Title = title,
                MetaDescription = metaDescription,
                CanonicalUrl = canonicalUrl,
                OgTitle = suppressOpenGraph ? null : FirstNonEmpty(request.PageSeo?.OgTitle, title),
                OgDescription = suppressOpenGraph ? null : FirstNonEmpty(request.PageSeo?.OgDescription, metaDescription),
                OgImage = suppressOpenGraph ? null : ResolveContentUrl(FirstNonEmpty(request.PageSeo?.OgImage, request.Settings?.DefaultOgImage), request.Settings?.BaseCanonicalUrl),
                SiteName = suppressOpenGraph ? null : request.Settings?.SiteName,
                RobotsIndex = request.PageSeo?.RobotsIndex ?? true,
                RobotsFollow = request.PageSeo?.RobotsFollow ?? true,
            };
        }

        private static string? AppendTitleSuffix(string? title, string? suffix)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return string.IsNullOrWhiteSpace(suffix) ? null : suffix.Trim();
            }

            if (string.IsNullOrWhiteSpace(suffix))
            {
                return title.Trim();
            }

            var normalizedTitle = title.Trim();
            var normalizedSuffix = suffix.Trim();

            return normalizedTitle.EndsWith(normalizedSuffix, StringComparison.OrdinalIgnoreCase)
                ? normalizedTitle
                : $"{normalizedTitle} {normalizedSuffix}";
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
        }

        private static string? ResolveCanonicalUrl(string? canonicalUrl, string? baseCanonicalUrl, string? relativePath)
        {
            if (!string.IsNullOrWhiteSpace(canonicalUrl))
            {
                var resolvedCanonical = ResolveContentUrl(canonicalUrl, baseCanonicalUrl);
                if (!string.IsNullOrWhiteSpace(resolvedCanonical))
                {
                    return resolvedCanonical;
                }
            }

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            return ResolveContentUrl(relativePath, baseCanonicalUrl);
        }

        private static string? ResolveContentUrl(string? value, string? baseCanonicalUrl)
        {
            return TryCombineAbsoluteUrl(baseCanonicalUrl, value) ?? TryNormalizeRootRelativeUrl(value);
        }

        private static string? TryCombineAbsoluteUrl(string? baseCanonicalUrl, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (Uri.TryCreate(value, UriKind.Absolute, out var absoluteValue)
                && IsSupportedAbsoluteUri(absoluteValue))
            {
                return absoluteValue.ToString();
            }

            if (string.IsNullOrWhiteSpace(baseCanonicalUrl)
                || !Uri.TryCreate(baseCanonicalUrl, UriKind.Absolute, out var baseUri)
                || !IsSupportedAbsoluteUri(baseUri)
                || !value.StartsWith("/", StringComparison.Ordinal))
            {
                return null;
            }

            return new Uri(baseUri, value).ToString();
        }

        private static string? TryNormalizeRootRelativeUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();
            return normalized.StartsWith("/", StringComparison.Ordinal) &&
                !normalized.StartsWith("//", StringComparison.Ordinal) &&
                !normalized.Contains("\r", StringComparison.Ordinal) &&
                !normalized.Contains("\n", StringComparison.Ordinal)
                    ? normalized
                    : null;
        }

        private static bool IsSupportedAbsoluteUri(Uri uri)
        {
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        private async Task<SeoSettingsDto?> GetEffectiveSettingsAsync(CancellationToken cancellationToken)
        {
            var settings = await _settingsProvider.GetAsync(cancellationToken);
            var resolvedBaseUrl = _publicUrlResolver.ResolveBaseUrl(settings?.BaseCanonicalUrl);

            if (string.IsNullOrWhiteSpace(resolvedBaseUrl))
            {
                return settings;
            }

            if (settings is null)
            {
                return new SeoSettingsDto
                {
                    BaseCanonicalUrl = resolvedBaseUrl,
                };
            }

            if (string.Equals(settings.BaseCanonicalUrl, resolvedBaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return settings;
            }

            return CloneSettings(settings, resolvedBaseUrl);
        }

        private static SeoSettingsDto CloneSettings(SeoSettingsDto settings, string baseCanonicalUrl)
        {
            return new SeoSettingsDto
            {
                Id = settings.Id,
                SiteName = settings.SiteName,
                DefaultTitleSuffix = settings.DefaultTitleSuffix,
                DefaultMetaDescription = settings.DefaultMetaDescription,
                DefaultOgImage = settings.DefaultOgImage,
                BaseCanonicalUrl = baseCanonicalUrl,
                CompanyName = settings.CompanyName,
                CompanyLogoUrl = settings.CompanyLogoUrl,
                CompanyPhone = settings.CompanyPhone,
                CompanyEmail = settings.CompanyEmail,
                CompanyAddress = settings.CompanyAddress,
                FacebookUrl = settings.FacebookUrl,
                InstagramUrl = settings.InstagramUrl,
                XUrl = settings.XUrl,
            };
        }

        private static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            {
                return value ?? string.Empty;
            }

            return $"{value[..maxLength].TrimEnd()}...";
        }

        private sealed class SeoMetadataBuildRequest
        {
            public string? PageTitle { get; set; }

            public string? RelativePath { get; set; }

            public bool SuppressCanonicalUrl { get; set; }

            public bool SuppressOpenGraph { get; set; }

            public SeoFieldsDto? PageSeo { get; set; }

            public SeoSettingsDto? Settings { get; set; }
        }
    }
}
