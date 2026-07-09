namespace BlazorShop.CommerceNode.API.Deployment
{
    public sealed record StorefrontDeploymentRequest(
        Guid StoreId,
        string StoreKey,
        string StorefrontImage,
        string? NetworkName,
        IReadOnlyDictionary<string, string> Environment);

    public sealed record StorefrontContainerPlan(
        string StoreKey,
        string ContainerName,
        string StorefrontImage,
        string? NetworkName,
        int ContainerPort,
        string EnvFilePath,
        string InternalUrl);

    public sealed record StorefrontDeploymentCommandResult(
        bool Success,
        string Message,
        string? StandardOutput = null,
        string? StandardError = null,
        int ExitCode = 0);

    public sealed record StorefrontHealthProbeResult(
        bool Healthy,
        int? StatusCode,
        string Message);
}
