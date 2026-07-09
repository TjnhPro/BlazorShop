namespace BlazorShop.ControlPlane.Web.Services.Common
{
    using System.Net;

    public sealed record ControlPlaneApiEnvelope<TData>(bool Success, string? Message, TData? Data);

    public sealed record ControlPlaneClientResult<TData>(
        bool Success,
        string Message,
        TData? Data,
        HttpStatusCode? StatusCode = null)
    {
        public static ControlPlaneClientResult<TData> Succeeded(
            TData? data,
            string message,
            HttpStatusCode? statusCode = null)
        {
            return new ControlPlaneClientResult<TData>(true, message, data, statusCode);
        }

        public static ControlPlaneClientResult<TData> Failed(
            string message,
            HttpStatusCode? statusCode = null,
            TData? data = default)
        {
            return new ControlPlaneClientResult<TData>(false, message, data, statusCode);
        }
    }
}
