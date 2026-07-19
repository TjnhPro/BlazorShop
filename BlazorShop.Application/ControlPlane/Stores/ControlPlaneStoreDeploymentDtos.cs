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

    public interface IControlPlaneStoreDeploymentService
    {
        Task<ApplicationResult<CommerceTaskSummary>> ProvisionAsync(
            Guid storePublicId,
            DeployControlPlaneStoreRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceTaskDetail>> GetTaskAsync(
            Guid storePublicId,
            Guid taskPublicId,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceTaskDetail>> CancelTaskAsync(
            Guid storePublicId,
            Guid taskPublicId,
            CancelCommerceTaskRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceTaskDetail>> RetryTaskAsync(
            Guid storePublicId,
            Guid taskPublicId,
            RetryCommerceTaskRequest request,
            CancellationToken cancellationToken = default);
    }
}
