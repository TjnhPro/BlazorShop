namespace BlazorShop.CommerceNode.API.Responses
{
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
