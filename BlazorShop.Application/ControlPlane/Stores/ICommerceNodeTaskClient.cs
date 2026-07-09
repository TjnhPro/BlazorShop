namespace BlazorShop.Application.ControlPlane.Stores
{
    using BlazorShop.Application.CommerceNode.Tasks;

    public interface ICommerceNodeTaskClient
    {
        Task<CommerceNodeTaskClientResult<CommerceTaskSummary>> EnqueueAsync(
            string controlApiBaseUrl,
            string nodeKey,
            string nodeSecret,
            EnqueueCommerceTaskRequest request,
            CancellationToken cancellationToken = default);

        Task<CommerceNodeTaskClientResult<CommerceTaskDetail>> GetAsync(
            string controlApiBaseUrl,
            string nodeKey,
            string nodeSecret,
            Guid taskPublicId,
            CancellationToken cancellationToken = default);

        Task<CommerceNodeTaskClientResult<CommerceTaskDetail>> CancelAsync(
            string controlApiBaseUrl,
            string nodeKey,
            string nodeSecret,
            Guid taskPublicId,
            CancelCommerceTaskRequest request,
            CancellationToken cancellationToken = default);

        Task<CommerceNodeTaskClientResult<CommerceTaskDetail>> RetryAsync(
            string controlApiBaseUrl,
            string nodeKey,
            string nodeSecret,
            Guid taskPublicId,
            RetryCommerceTaskRequest request,
            CancellationToken cancellationToken = default);
    }

    public sealed record CommerceNodeTaskClientResult<TPayload>(
        bool Success,
        int? HttpStatusCode,
        string? Message,
        TPayload? Payload = default,
        string? ErrorCode = null);
}
