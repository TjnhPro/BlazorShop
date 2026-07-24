namespace BlazorShop.Storefront.Starter.Security
{
    public static class StarterReturnUrlValidator
    {
        public static bool IsSafeLocalReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return true;
            }

            var trimmed = returnUrl.Trim();
            if (!trimmed.StartsWith("/", StringComparison.Ordinal))
            {
                return false;
            }

            if (trimmed.StartsWith("//", StringComparison.Ordinal)
                || trimmed.StartsWith("/\\", StringComparison.Ordinal)
                || trimmed.Contains("://", StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }
    }
}
