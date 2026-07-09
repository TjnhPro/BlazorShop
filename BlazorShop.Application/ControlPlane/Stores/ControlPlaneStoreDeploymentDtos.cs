namespace BlazorShop.Application.ControlPlane.Stores
{
    using BlazorShop.Application.CommerceNode.Tasks;

    public sealed record DeployControlPlaneStoreRequest(
        string StorefrontImage,
        string? PrimaryDomain = null,
        string? BaseUrl = null,
        string DefaultCurrencyCode = "USD",
        string DefaultCulture = "en-US",
        string? NetworkName = null);

    public sealed record ControlPlaneStoreDeploymentOperationResult<TPayload>(
        bool Success,
        string? Message = null,
        TPayload? Payload = default,
        ControlPlaneStoreDeploymentOperationFailure? Failure = null);

    public enum ControlPlaneStoreDeploymentOperationFailure
    {
        Validation,
        NotFound,
        Conflict,
        RemoteFailure
    }

    public interface IControlPlaneStoreDeploymentService
    {
        Task<ControlPlaneStoreDeploymentOperationResult<CommerceTaskSummary>> ProvisionAsync(
            Guid storePublicId,
            DeployControlPlaneStoreRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneStoreDeploymentOperationResult<CommerceTaskDetail>> GetTaskAsync(
            Guid storePublicId,
            Guid taskPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneStoreDeploymentOperationResult<CommerceTaskDetail>> CancelTaskAsync(
            Guid storePublicId,
            Guid taskPublicId,
            CancelCommerceTaskRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneStoreDeploymentOperationResult<CommerceTaskDetail>> RetryTaskAsync(
            Guid storePublicId,
            Guid taskPublicId,
            RetryCommerceTaskRequest request,
            CancellationToken cancellationToken = default);
    }
}
