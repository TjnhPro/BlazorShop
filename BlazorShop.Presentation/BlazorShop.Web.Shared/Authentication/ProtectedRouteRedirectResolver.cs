namespace BlazorShop.Web.Shared.Authentication
{
    public static class ProtectedRouteRedirectResolver
    {
        public static string? ResolveLoginRedirectPath(
            string? relativePath,
            bool isAuthenticated,
            string loginPath = "/authentication/login")
        {
            if (isAuthenticated)
            {
                return null;
            }

            var sanitizedPath = Sanitize(relativePath);
            var normalizedLoginPath = NormalizeRootedPath(loginPath);

            return string.IsNullOrWhiteSpace(sanitizedPath)
                ? normalizedLoginPath
                : $"{normalizedLoginPath}/{sanitizedPath}";
        }

        public static string ResolvePostLoginPath(
            string? relativePath,
            bool isAdmin,
            string adminPath = "/admin",
            string accountPath = "/account",
            string blockedAdminFallbackPath = "/account")
        {
            var sanitizedPath = Sanitize(relativePath);

            if (string.IsNullOrWhiteSpace(sanitizedPath)
                || string.Equals(sanitizedPath, Constant.Cart.Name, StringComparison.OrdinalIgnoreCase))
            {
                return isAdmin ? adminPath : accountPath;
            }

            var normalizedPath = NormalizeRootedPath(sanitizedPath);

            if (!isAdmin && normalizedPath.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
            {
                return blockedAdminFallbackPath;
            }

            return normalizedPath;
        }

        private static string NormalizeRootedPath(string path)
        {
            return path.StartsWith("/", StringComparison.Ordinal)
                ? path
                : $"/{path}";
        }

        private static string Sanitize(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return string.Empty;
            }

            var trimmed = relativePath.Trim().TrimStart('/');
            var queryIndex = trimmed.IndexOfAny(['?', '#']);

            return queryIndex >= 0
                ? trimmed[..queryIndex]
                : trimmed;
        }
    }
}
