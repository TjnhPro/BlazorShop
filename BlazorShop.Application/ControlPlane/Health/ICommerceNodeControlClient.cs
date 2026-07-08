namespace BlazorShop.Application.ControlPlane.Health
{
    public interface ICommerceNodeControlClient
    {
        Task<CommerceNodeControlProbeResponse> ProbeAsync(
            string controlApiBaseUrl,
            CancellationToken cancellationToken = default);
    }

    public sealed record CommerceNodeControlProbeResponse(
        string HealthStatus,
        int? HttpStatusCode,
        int DurationMs,
        string? DependencyStatusJson,
        string? HealthErrorCode,
        string? HealthErrorMessage,
        string? CapabilitySchemaVersion,
        string? CapabilityJson);
}
