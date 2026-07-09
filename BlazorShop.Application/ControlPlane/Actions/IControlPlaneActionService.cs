namespace BlazorShop.Application.ControlPlane.Actions
{
    public interface IControlPlaneActionService
    {
        Task<ControlPlaneActionListResponse> ListAsync(
            ControlPlaneActionListQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneActionOperationResult<ControlPlaneActionDetail>> GetByPublicIdAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneActionOperationResult<ControlPlaneActionDetail>> EnqueueAsync(
            EnqueueControlActionRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneActionOperationResult<ControlPlaneActionDetail>> RecordAttemptAsync(
            Guid publicId,
            RecordControlActionAttemptRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneActionOperationResult<ControlPlaneActionDetail>> CancelAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);
    }
}
