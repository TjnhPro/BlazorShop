namespace BlazorShop.Web.Authentication
{
    public static class ProtectedRouteRedirectResolver
    {
        public static string? ResolveLoginRedirectPath(string? relativePath, bool isAuthenticated)
        {
            return BlazorShop.Web.Shared.Authentication.ProtectedRouteRedirectResolver.ResolveLoginRedirectPath(
                relativePath,
                isAuthenticated);
        }

        public static string ResolvePostLoginPath(string? relativePath, bool isAdmin)
        {
            return BlazorShop.Web.Shared.Authentication.ProtectedRouteRedirectResolver.ResolvePostLoginPath(
                relativePath,
                isAdmin);
        }
    }
}
