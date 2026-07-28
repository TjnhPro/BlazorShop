namespace BlazorShop.Storefront.Presentation.Services
{
    public sealed record StorefrontSubmitResult<TData>(bool Success, string Message, TData? Data, int? StatusCode = null)
    {
        public static StorefrontSubmitResult<TData> Succeeded(TData? data, string? message)
        {
            return new(true, string.IsNullOrWhiteSpace(message) ? "Request completed." : message, data);
        }

        public static StorefrontSubmitResult<TData> Failed(string? message, int? statusCode = null)
        {
            return new(false, string.IsNullOrWhiteSpace(message) ? "The request could not be completed." : message, default, statusCode);
        }
    }
}
