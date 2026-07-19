namespace BlazorShop.Application.ControlPlane.Health
{
    public interface IControlPlaneHealthService
    {
        Task<ControlPlaneHealthListResponse> ListAsync(
            ControlPlaneHealthListQuery query,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneHealthDetail>> GetDetailAsync(
            Guid nodePublicId,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneHealthTimelineResponse>> GetTimelineAsync(
            Guid nodePublicId,
            ControlPlaneHealthTimelineQuery query,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneProbeResult>> ProbeAsync(
            Guid nodePublicId,
            CancellationToken cancellationToken = default);

        Task<int> ProbeAllActiveAsync(CancellationToken cancellationToken = default);
    }
}
