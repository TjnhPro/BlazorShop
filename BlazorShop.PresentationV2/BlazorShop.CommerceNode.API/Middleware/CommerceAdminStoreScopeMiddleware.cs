namespace BlazorShop.CommerceNode.API.Middleware
{
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.CommerceNode.API.Responses;

    public sealed class CommerceAdminStoreScopeMiddleware
    {
        private readonly RequestDelegate next;

        public CommerceAdminStoreScopeMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ICommerceStoreDomainResolver resolver,
            IStoreExecutionContextAccessor storeExecutionContextAccessor)
        {
            storeExecutionContextAccessor.Clear();

            var relativePath = NormalizePath(context.Request.Path.Value);
            if (IsStoreScopedCommerceAdminPath(relativePath))
            {
                var storeKey = context.Request.Query["storeKey"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(storeKey))
                {
                    await WriteFailureAsync(context, StatusCodes.Status400BadRequest, "storeKey query parameter is required.");
                    return;
                }

                var resolved = await resolver.ResolveExecutionContextAsync(
                    storeKey: storeKey,
                    source: StoreExecutionContextSources.CommerceAdminQuery,
                    cancellationToken: context.RequestAborted);
                if (!await SetResolvedContextAsync(context, storeExecutionContextAccessor, resolved))
                {
                    return;
                }
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

        private static async Task<bool> SetResolvedContextAsync(
            HttpContext context,
            IStoreExecutionContextAccessor accessor,
            ApplicationResult<StoreExecutionContext> resolved)
        {
            if (!resolved.Success || resolved.Value is null)
            {
                await WriteFailureAsync(context, ToStatusCode(resolved.Error?.Kind), resolved.Message);
                return false;
            }

            accessor.SetCurrent(resolved.Value);
            return true;
        }

        private static Task WriteFailureAsync(HttpContext context, int statusCode, string? message)
        {
            return CommerceNodeApiResponseWriter.WriteFailureAsync<object>(
                context,
                statusCode,
                message);
        }

        private static int ToStatusCode(ApplicationErrorKind? failure)
        {
            return failure switch
            {
                ApplicationErrorKind.Validation => StatusCodes.Status400BadRequest,
                ApplicationErrorKind.NotFound => StatusCodes.Status404NotFound,
                ApplicationErrorKind.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError,
            };
        }
    }
}
