namespace BlazorShop.ControlPlane.API.Responses
{
    public sealed record ControlPlaneApiResponse<TData>(bool Success, string Message, TData? Data)
    {
        public static ControlPlaneApiResponse<TData> Succeeded(TData? data, string message)
        {
            return new ControlPlaneApiResponse<TData>(true, message, data);
        }

        public static ControlPlaneApiResponse<TData> Failed(string message, TData? data = default)
        {
            return new ControlPlaneApiResponse<TData>(false, message, data);
        }
    }
}
