namespace BlazorShop.CommerceNode.API.Deployment
{
    public interface IStorefrontDockerDeploymentService
    {
        StorefrontContainerPlan CreatePlan(StorefrontDeploymentRequest request);

        Task<string> RenderEnvironmentFileAsync(
            StorefrontContainerPlan plan,
            StorefrontDeploymentRequest request,
            CancellationToken cancellationToken = default);

        Task<StorefrontDeploymentCommandResult> CreateOrUpdateContainerAsync(
            StorefrontContainerPlan plan,
            CancellationToken cancellationToken = default);

        Task<StorefrontDeploymentCommandResult> StartContainerAsync(
            StorefrontContainerPlan plan,
            CancellationToken cancellationToken = default);

        Task<StorefrontDeploymentCommandResult> StopContainerAsync(
            StorefrontContainerPlan plan,
            CancellationToken cancellationToken = default);

        Task<StorefrontDeploymentCommandResult> RemoveContainerAsync(
            StorefrontContainerPlan plan,
            CancellationToken cancellationToken = default);

        Task<StorefrontHealthProbeResult> ProbeHealthAsync(
            StorefrontContainerPlan plan,
            CancellationToken cancellationToken = default);
    }
}
