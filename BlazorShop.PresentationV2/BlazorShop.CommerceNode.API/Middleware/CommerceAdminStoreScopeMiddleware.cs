namespace BlazorShop.CommerceNode.API.Middleware
{
    using BlazorShop.CommerceNode.API.Responses;

    public sealed class CommerceAdminStoreScopeMiddleware
    {
        private readonly RequestDelegate next;

        public CommerceAdminStoreScopeMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var relativePath = NormalizePath(context.Request.Path.Value);
            if (IsStoreScopedCommerceAdminPath(relativePath)
                && string.IsNullOrWhiteSpace(context.Request.Query["storeKey"].FirstOrDefault()))
            {
                await CommerceNodeApiResponseWriter.WriteFailureAsync<object>(
                    context,
                    StatusCodes.Status400BadRequest,
                    "storeKey query parameter is required.");
                return;
            }

            await this.next(context);
        }

        public static bool IsStoreScopedCommerceAdminPath(string relativePath)
        {
            if (!relativePath.StartsWith("api/commerce/admin/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !relativePath.StartsWith("api/commerce/admin/stores", StringComparison.OrdinalIgnoreCase)
                   && !relativePath.StartsWith("api/commerce/admin/audit", StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizePath(string? path)
        {
            return (path ?? string.Empty)
                .Split('?', 2)[0]
                .Trim('/')
                .ToLowerInvariant();
        }
    }
}
