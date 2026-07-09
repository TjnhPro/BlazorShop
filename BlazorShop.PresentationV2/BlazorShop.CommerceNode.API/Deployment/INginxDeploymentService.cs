namespace BlazorShop.CommerceNode.API.Deployment
{
    public interface INginxDeploymentService
    {
        NginxStoreProxyPlan CreatePlan(NginxStoreProxyRequest request);

        Task<string> RenderConfigAsync(
            NginxStoreProxyPlan plan,
            CancellationToken cancellationToken = default);

        Task<NginxDeploymentCommandResult> ValidateConfigAsync(
            CancellationToken cancellationToken = default);

        Task<NginxDeploymentCommandResult> ReloadAsync(
            CancellationToken cancellationToken = default);

        Task RemoveConfigAsync(
            NginxStoreProxyPlan plan,
            CancellationToken cancellationToken = default);
    }
}
