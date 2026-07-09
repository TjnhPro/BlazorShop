namespace BlazorShop.Application.CommerceNode.Tasks
{
    public interface ICommerceTaskService
    {
        Task<CommerceTaskOperationResult<CommerceTaskSummary>> EnqueueAsync(
            EnqueueCommerceTaskRequest request,
            CancellationToken cancellationToken = default);

        Task<CommerceTaskOperationResult<CommerceTaskListResponse>> ListAsync(
            CommerceTaskListQuery query,
            CancellationToken cancellationToken = default);

        Task<CommerceTaskOperationResult<CommerceTaskDetail>> GetByPublicIdAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<CommerceTaskOperationResult<CommerceTaskDetail>> CancelAsync(
            Guid publicId,
            CancelCommerceTaskRequest request,
            CancellationToken cancellationToken = default);

        Task<CommerceTaskOperationResult<CommerceTaskDetail>> RetryAsync(
            Guid publicId,
            RetryCommerceTaskRequest request,
            CancellationToken cancellationToken = default);
    }
}
