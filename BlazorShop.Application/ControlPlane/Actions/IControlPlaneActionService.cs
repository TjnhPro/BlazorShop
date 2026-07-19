namespace BlazorShop.Application.ControlPlane.Actions
{
    public interface IControlPlaneActionService
    {
        Task<ControlPlaneActionListResponse> ListAsync(
            ControlPlaneActionListQuery query,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneActionDetail>> GetByPublicIdAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneActionDetail>> EnqueueAsync(
            EnqueueControlActionRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneActionDetail>> RecordAttemptAsync(
            Guid publicId,
            RecordControlActionAttemptRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<ControlPlaneActionDetail>> CancelAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);
    }
}
