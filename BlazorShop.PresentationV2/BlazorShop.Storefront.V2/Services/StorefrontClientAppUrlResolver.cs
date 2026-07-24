namespace BlazorShop.Storefront.Services
{
    using BlazorShop.Storefront.Options;
    using BlazorShop.Storefront.Services.Contracts;

    using Microsoft.Extensions.Options;

    public class StorefrontClientAppUrlResolver : IStorefrontClientAppUrlResolver
    {
        private readonly IOptions<ClientAppOptions> _options;

        public StorefrontClientAppUrlResolver(IOptions<ClientAppOptions> options)
        {
            _options = options;
        }

        public string? ResolveBaseUrl()
        {
            return NormalizeBaseUrl(_options.Value.BaseUrl);
        }

        public string ResolveUrl(string? relativeOrAbsoluteUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsoluteUrl))
            {
                return "/";
            }

            if (Uri.TryCreate(relativeOrAbsoluteUrl.Trim(), UriKind.Absolute, out var absoluteUri)
                && IsSupportedAbsoluteUri(absoluteUri))
            {
                return absoluteUri.ToString();
            }

            var relativePath = relativeOrAbsoluteUrl.StartsWith("/", StringComparison.Ordinal)
                ? relativeOrAbsoluteUrl.Trim()
                : $"/{relativeOrAbsoluteUrl.TrimStart('/')}";

            var baseUrl = ResolveBaseUrl();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return relativePath;
            }

            return new Uri(new Uri(baseUrl, UriKind.Absolute), relativePath).ToString();
        }

        private static string? NormalizeBaseUrl(string? candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate)
                || !Uri.TryCreate(candidate.Trim(), UriKind.Absolute, out var absoluteUri)
                || !IsSupportedAbsoluteUri(absoluteUri))
            {
                return null;
            }

            var uriBuilder = new UriBuilder(absoluteUri)
            {
                Fragment = string.Empty,
                Query = string.Empty,
                Path = EnsureTrailingSlash(absoluteUri.AbsolutePath),
            };

            return uriBuilder.Uri.ToString();
        }

        private static string EnsureTrailingSlash(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "/";
            }

            return path.EndsWith("/", StringComparison.Ordinal)
                ? path
                : $"{path}/";
        }

        private static bool IsSupportedAbsoluteUri(Uri uri)
        {
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }
    }
}
