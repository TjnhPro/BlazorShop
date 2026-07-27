namespace BlazorShop.Storefront.Services
{
    using Microsoft.AspNetCore.Http;

    public static class StorefrontCookieBridge
    {
        public static void CopySetCookieHeaders(IEnumerable<string> setCookieHeaders, HttpResponse response)
        {
            foreach (var header in setCookieHeaders)
            {
                if (!string.IsNullOrWhiteSpace(header))
                {
                    response.Headers.Append("Set-Cookie", header);
                }
            }
        }
    }
}
