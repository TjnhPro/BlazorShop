namespace BlazorShop.CommerceNode.API.Responses
{
    public sealed record CommerceNodeApiResponse(bool Success, string? Message)
    {
        public static CommerceNodeApiResponse Succeeded(string? message)
        {
            return new CommerceNodeApiResponse(true, NormalizeMessage(message));
        }

        public static CommerceNodeApiResponse Failed(string? message)
        {
            return new CommerceNodeApiResponse(false, NormalizeMessage(message));
        }

        private static string NormalizeMessage(string? message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? "The Commerce Node request completed."
                : message;
        }
    }

    public sealed record CommerceNodeApiResponse<TData>(bool Success, string Message, TData? Data)
    {
        public static CommerceNodeApiResponse<TData> Succeeded(TData? data, string message)
        {
            return new CommerceNodeApiResponse<TData>(true, message, data);
        }

        public static CommerceNodeApiResponse<TData> Failed(string message, TData? data = default)
        {
            return new CommerceNodeApiResponse<TData>(false, message, data);
        }
    }
}
