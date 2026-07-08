namespace BlazorShop.ControlPlane.Web.Services.Common
{
    public interface IControlPlaneApiClient
    {
        Task<ControlPlaneClientResult<TData>> GetPrivateAsync<TData>(
            string route,
            string fallbackMessage,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<TData>> GetPublicAsync<TData>(
            string route,
            string fallbackMessage,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<TData>> PostPrivateAsync<TRequest, TData>(
            string route,
            TRequest request,
            string fallbackMessage,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<TData>> PostPrivateAsync<TData>(
            string route,
            string fallbackMessage,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<TData>> PostPublicAsync<TRequest, TData>(
            string route,
            TRequest request,
            string fallbackMessage,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<TData>> PostPublicAsync<TData>(
            string route,
            string fallbackMessage,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<TData>> PutPrivateAsync<TRequest, TData>(
            string route,
            TRequest request,
            string fallbackMessage,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<TData>> DeletePrivateAsync<TData>(
            string route,
            string fallbackMessage,
            CancellationToken cancellationToken = default);
    }
}
