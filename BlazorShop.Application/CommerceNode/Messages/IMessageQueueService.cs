namespace BlazorShop.Application.CommerceNode.Messages
{
    public interface IMessageQueueService
    {
        Task<QueuedMessageResult> QueueAsync(
            QueueTransactionalMessageRequest request,
            CancellationToken cancellationToken = default);
    }
}
