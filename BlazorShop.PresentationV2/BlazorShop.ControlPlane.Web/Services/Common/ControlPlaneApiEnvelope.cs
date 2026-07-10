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

    public sealed record ControlPlaneFileResult(
        bool Success,
        string Message,
        byte[]? Content = null,
        string? ContentType = null,
        string? FileName = null,
        HttpStatusCode? StatusCode = null)
    {
        public static ControlPlaneFileResult Succeeded(
            byte[] content,
            string? contentType,
            string? fileName,
            HttpStatusCode? statusCode = null)
        {
            return new ControlPlaneFileResult(true, string.Empty, content, contentType, fileName, statusCode);
        }

        public static ControlPlaneFileResult Failed(string message, HttpStatusCode? statusCode = null)
        {
            return new ControlPlaneFileResult(false, message, StatusCode: statusCode);
        }
    }
}
