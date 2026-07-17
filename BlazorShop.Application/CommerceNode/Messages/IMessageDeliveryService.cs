namespace BlazorShop.Application.CommerceNode.Messages
{
    public interface IMessageDeliveryService
    {
        Task<MessageDeliveryResult> DeliverAsync(
            Guid queuedMessagePublicId,
            int attemptNumber,
            CancellationToken cancellationToken = default);

        Task<QueuedMessageResult> RetryAsync(
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default);

        Task<QueuedMessageResult> CancelAsync(
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default);
    }
}
