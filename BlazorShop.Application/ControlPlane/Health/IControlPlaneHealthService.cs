namespace BlazorShop.Application.ControlPlane.Health
{
    public interface IControlPlaneHealthService
    {
        Task<ControlPlaneHealthListResponse> ListAsync(CancellationToken cancellationToken = default);

        Task<ControlPlaneHealthOperationResult<ControlPlaneHealthDetail>> GetDetailAsync(
            Guid nodePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneHealthOperationResult<ControlPlaneProbeResult>> ProbeAsync(
            Guid nodePublicId,
            CancellationToken cancellationToken = default);

        Task<int> ProbeAllActiveAsync(CancellationToken cancellationToken = default);
    }
}
