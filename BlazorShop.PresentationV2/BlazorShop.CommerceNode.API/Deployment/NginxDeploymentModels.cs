namespace BlazorShop.CommerceNode.API.Deployment
{
    public sealed record NginxStoreProxyRequest(
        string StoreKey,
        string ServerName,
        string UpstreamUrl);

    public sealed record NginxStoreProxyPlan(
        string StoreKey,
        string ServerName,
        string UpstreamUrl,
        string ConfigPath);

    public sealed record NginxDeploymentCommandResult(
        bool Success,
        string Message,
        string? StandardOutput = null,
        string? StandardError = null,
        int ExitCode = 0);
}
