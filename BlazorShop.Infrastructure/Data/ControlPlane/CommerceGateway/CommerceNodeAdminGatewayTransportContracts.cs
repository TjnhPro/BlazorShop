namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.ProductImports;

    public interface ICommerceNodeAdminGatewayTransport
    {
        Task<CommerceNodeAdminGatewayResult<TPayload>> SendAsync<TPayload>(
            Guid storePublicId,
            HttpMethod method,
            string path,
            object? body,
            CancellationToken cancellationToken = default);

        Task<CommerceNodeAdminGatewayResult<TPayload>> SendProductImportMultipartAsync<TPayload>(
            Guid storePublicId,
            string path,
            ProductImportUploadRequest upload,
            CancellationToken cancellationToken = default);

        Task<CommerceNodeAdminGatewayResult<TPayload>> SendMediaAssetMultipartAsync<TPayload>(
            Guid storePublicId,
            string path,
            CommerceMediaAssetUploadRequest upload,
            CancellationToken cancellationToken = default);

        Task<CommerceNodeAdminMediaGatewayResult> SendMediaAsync(
            Guid storePublicId,
            string path,
            CancellationToken cancellationToken = default);

        Task<CommerceNodeAdminGatewayResult<string>> ResolveStoreKeyAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);
    }

    public sealed record CommerceNodeAdminGatewayResult<TPayload>(
        bool Success,
        string? Message = null,
        TPayload? Payload = default,
        CommerceNodeAdminGatewayFailure? Failure = null,
        int? HttpStatusCode = null);

    public sealed record CommerceNodeAdminMediaGatewayResult(
        bool Success,
        string? Message = null,
        byte[]? Content = null,
        string? ContentType = null,
        CommerceNodeAdminGatewayFailure? Failure = null,
        int? HttpStatusCode = null);

    public enum CommerceNodeAdminGatewayFailure
    {
        Validation,
        NotFound,
        RemoteFailure
    }
}
