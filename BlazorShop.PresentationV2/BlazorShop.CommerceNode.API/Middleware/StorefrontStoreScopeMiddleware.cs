namespace BlazorShop.CommerceNode.API.Middleware
{
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.CommerceNode.API.Responses;

    public sealed class StorefrontStoreScopeMiddleware
    {
        private readonly RequestDelegate next;

        public StorefrontStoreScopeMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ICommerceStoreDomainResolver resolver,
            IStoreExecutionContextAccessor storeExecutionContextAccessor)
        {
            storeExecutionContextAccessor.Clear();

            var path = NormalizePath(context.Request.Path.Value);
            if (path.StartsWith("api/storefront/stores/", StringComparison.OrdinalIgnoreCase))
            {
                var storeKey = ExtractStorefrontStoreKey(path);
                if (string.IsNullOrWhiteSpace(storeKey))
                {
                    await WriteFailureAsync(context, StatusCodes.Status404NotFound, "storeKey route value is required.");
                    return;
                }

                var resolved = await resolver.ResolveExecutionContextAsync(
                    storeKey: storeKey,
                    source: StoreExecutionContextSources.StorefrontRoute,
                    cancellationToken: context.RequestAborted);
                if (!await SetResolvedContextAsync(context, storeExecutionContextAccessor, resolved))
                {
                    return;
                }
            }
            else if (IsPublicMediaPath(path))
            {
                var host = FirstHeaderValue(context.Request, "X-Store-Host")
                           ?? FirstHeaderValue(context.Request, "X-Forwarded-Host")
                           ?? context.Request.Host.Value;
                if (string.IsNullOrWhiteSpace(host))
                {
                    await WriteFailureAsync(context, StatusCodes.Status404NotFound, "Store host is required.");
                    return;
                }

                var resolved = await resolver.ResolveExecutionContextAsync(
                    host: host,
                    source: StoreExecutionContextSources.PublicMediaHost,
                    cancellationToken: context.RequestAborted);
                if (!await SetResolvedContextAsync(context, storeExecutionContextAccessor, resolved))
                {
                    return;
                }
            }

            await this.next(context);
        }

        public static bool IsStorefrontOrPublicMediaPath(PathString path)
        {
            return path.StartsWithSegments("/api/storefront/stores")
                   || path.StartsWithSegments("/media/products")
                   || path.StartsWithSegments("/media/assets");
        }

        public static string NormalizePath(string? path)
        {
            return (path ?? string.Empty)
                .Split('?', 2)[0]
                .Trim('/')
                .ToLowerInvariant();
        }

        private static bool IsPublicMediaPath(string relativePath)
        {
            return relativePath.StartsWith("media/products/", StringComparison.OrdinalIgnoreCase)
                   || relativePath.StartsWith("media/assets/", StringComparison.OrdinalIgnoreCase);
        }

        private static string? ExtractStorefrontStoreKey(string relativePath)
        {
            var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length >= 4
                   && string.Equals(segments[0], "api", StringComparison.OrdinalIgnoreCase)
                   && string.Equals(segments[1], "storefront", StringComparison.OrdinalIgnoreCase)
                   && string.Equals(segments[2], "stores", StringComparison.OrdinalIgnoreCase)
                ? segments[3]
                : null;
        }

        private static string? FirstHeaderValue(HttpRequest request, string headerName)
        {
            return request.Headers.TryGetValue(headerName, out var values)
                ? values.FirstOrDefault()
                : null;
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
