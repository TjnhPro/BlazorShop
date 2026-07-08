namespace BlazorShop.ControlPlane.API.Responses
{
    using BlazorShop.ControlPlane.API.Middleware;

    using Microsoft.AspNetCore.Mvc;

    public static class ControlPlaneApiResponseWriter
    {
        private const string JsonContentType = "application/json";

        public static ObjectResult Result<TData>(
            int statusCode,
            bool success,
            string? message,
            TData? data = default)
        {
            return new ObjectResult(new ControlPlaneApiResponse<TData>(success, NormalizeMessage(message), data))
            {
                StatusCode = statusCode
            };
        }

        public static ObjectResult Success<TData>(
            int statusCode,
            TData? data,
            string? message)
        {
            return Result(statusCode, success: true, message, data);
        }

        public static ObjectResult Failure<TData>(
            int statusCode,
            string? message,
            TData? data = default)
        {
            return Result(statusCode, success: false, message, data);
        }

        public static async Task WriteFailureAsync<TData>(
            HttpContext context,
            int statusCode,
            string? message,
            TData? data = default,
            CancellationToken cancellationToken = default)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = JsonContentType;
            await context.Response.WriteAsJsonAsync(
                ControlPlaneApiResponse<TData>.Failed(NormalizeMessage(message), data),
                cancellationToken);
        }

        public static object CreateCorrelationData(HttpContext context)
        {
            return new
            {
                correlationId = context.Items.TryGetValue(ControlPlaneCorrelationIdMiddleware.HeaderName, out var correlationId)
                    ? correlationId?.ToString()
                    : context.TraceIdentifier
            };
        }

        private static string NormalizeMessage(string? message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? "The Control Plane request could not be completed."
                : message;
        }
    }
}
