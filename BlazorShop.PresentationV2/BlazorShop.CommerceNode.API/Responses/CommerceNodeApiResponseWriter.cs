namespace BlazorShop.CommerceNode.API.Responses
{
    public static class CommerceNodeApiResponseWriter
    {
        private const string JsonContentType = "application/json";

        public static IResult Success<TData>(
            int statusCode,
            TData? data,
            string? message)
        {
            return Results.Json(
                CommerceNodeApiResponse<TData>.Succeeded(data, NormalizeMessage(message)),
                statusCode: statusCode);
        }

        public static IResult Failure<TData>(
            int statusCode,
            string? message,
            TData? data = default)
        {
            return Results.Json(
                CommerceNodeApiResponse<TData>.Failed(NormalizeMessage(message), data),
                statusCode: statusCode);
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
                CommerceNodeApiResponse<TData>.Failed(NormalizeMessage(message), data),
                cancellationToken);
        }

        private static string NormalizeMessage(string? message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? "The Commerce Node request could not be completed."
                : message;
        }
    }
}
