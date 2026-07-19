namespace BlazorShop.Application.CommerceNode.Tasks
{
    using BlazorShop.Application.Common.Results;

    public interface ICommerceTaskService
    {
        Task<ApplicationResult<CommerceTaskSummary>> EnqueueAsync(
            EnqueueCommerceTaskRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceTaskListResponse>> ListAsync(
            CommerceTaskListQuery query,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceTaskDetail>> GetByPublicIdAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceTaskDetail>> CancelAsync(
            Guid publicId,
            CancelCommerceTaskRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceTaskDetail>> RetryAsync(
            Guid publicId,
            RetryCommerceTaskRequest request,
            CancellationToken cancellationToken = default);
    }
}
