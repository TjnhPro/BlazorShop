namespace BlazorShop.Application.CommerceNode.Media
{
    public static class MediaDeliveryUrlPolicy
    {
        public static string BuildAbsoluteUrl(string publicBaseUrl, string rootRelativeOrAbsoluteUrl)
        {
            if (string.IsNullOrWhiteSpace(rootRelativeOrAbsoluteUrl))
            {
                throw new ArgumentException("Media URL is required.", nameof(rootRelativeOrAbsoluteUrl));
            }

            var url = rootRelativeOrAbsoluteUrl.Trim();
            if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUrl))
            {
                if (IsSupportedAbsoluteUrl(absoluteUrl))
                {
                    return absoluteUrl.AbsoluteUri;
                }

                throw new ArgumentException("Media URL must use http or https.", nameof(rootRelativeOrAbsoluteUrl));
            }

            if (!url.StartsWith("/", StringComparison.Ordinal) || url.StartsWith("//", StringComparison.Ordinal))
            {
                throw new ArgumentException("Media URL must be root-relative or absolute.", nameof(rootRelativeOrAbsoluteUrl));
            }

            var baseUri = NormalizePublicBaseUrl(publicBaseUrl);
            return new Uri(baseUri, url.TrimStart('/')).AbsoluteUri;
        }

        public static bool TryBuildAbsoluteUrl(string? publicBaseUrl, string? rootRelativeOrAbsoluteUrl, out string? absoluteUrl)
        {
            absoluteUrl = null;

            if (string.IsNullOrWhiteSpace(publicBaseUrl) || string.IsNullOrWhiteSpace(rootRelativeOrAbsoluteUrl))
            {
                return false;
            }

            try
            {
                absoluteUrl = BuildAbsoluteUrl(publicBaseUrl, rootRelativeOrAbsoluteUrl);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static Uri NormalizePublicBaseUrl(string publicBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(publicBaseUrl)
                || !Uri.TryCreate(publicBaseUrl.Trim(), UriKind.Absolute, out var baseUri)
                || !IsSupportedAbsoluteUrl(baseUri))
            {
                throw new ArgumentException("Public base URL must be an absolute http or https URL.", nameof(publicBaseUrl));
            }

            var uriBuilder = new UriBuilder(baseUri)
            {
                Fragment = string.Empty,
                Query = string.Empty,
                Path = EnsureTrailingSlash(baseUri.AbsolutePath),
            };

            return uriBuilder.Uri;
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

        private static bool IsSupportedAbsoluteUrl(Uri uri)
        {
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }
    }
}
