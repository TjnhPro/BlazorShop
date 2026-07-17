namespace BlazorShop.Storefront.Services
{
    using Microsoft.AspNetCore.Http;

    public static class StorefrontReturnUrl
    {
        public static string Normalize(string? returnUrl, string fallback = StorefrontRoutes.Home)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return fallback;
            }

            var candidate = returnUrl.Trim();
            if (!candidate.StartsWith("/", StringComparison.Ordinal)
                || candidate.StartsWith("//", StringComparison.Ordinal)
                || candidate.Contains("\\", StringComparison.Ordinal)
                || candidate.Contains("\r", StringComparison.Ordinal)
                || candidate.Contains("\n", StringComparison.Ordinal))
            {
                return fallback;
            }

            return candidate;
        }

        public static string BuildSignInUrl(string? returnUrl = null, string? error = null, bool registered = false)
        {
            var query = new Dictionary<string, string?>();
            var safeReturnUrl = Normalize(returnUrl, fallback: string.Empty);
            if (!string.IsNullOrWhiteSpace(safeReturnUrl))
            {
                query["returnUrl"] = safeReturnUrl;
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                query["error"] = error;
            }

            if (registered)
            {
                query["registered"] = "1";
            }

            return query.Count == 0
                ? StorefrontRoutes.SignIn
                : $"{StorefrontRoutes.SignIn}{QueryString.Create(query)}";
        }

        public static string BuildRegisterUrl(string? returnUrl = null, string? error = null)
        {
            var query = new Dictionary<string, string?>();
            var safeReturnUrl = Normalize(returnUrl, fallback: string.Empty);
            if (!string.IsNullOrWhiteSpace(safeReturnUrl))
            {
                query["returnUrl"] = safeReturnUrl;
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                query["error"] = error;
            }

            return query.Count == 0
                ? StorefrontRoutes.Register
                : $"{StorefrontRoutes.Register}{QueryString.Create(query)}";
        }

        public static string BuildAccountProfileUrl(string? error = null, bool saved = false)
        {
            return BuildAccountUrl(StorefrontRoutes.AccountProfile, error, saved);
        }

        public static string BuildAccountChangePasswordUrl(string? error = null, bool saved = false)
        {
            return BuildAccountUrl(StorefrontRoutes.AccountChangePassword, error, saved);
        }

        private static string BuildAccountUrl(string route, string? error, bool saved)
        {
            var query = new Dictionary<string, string?>();
            if (!string.IsNullOrWhiteSpace(error))
            {
                query["error"] = error;
            }

            if (saved)
            {
                query["saved"] = "1";
            }

            return query.Count == 0
                ? route
                : $"{route}{QueryString.Create(query)}";
        }
    }
}
