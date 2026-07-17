namespace BlazorShop.Application.CommerceNode.Messages
{
    public interface ICommerceTransactionalMessageService
    {
        Task<QueuedMessageResult> QueueOrderPlacedAsync(
            Guid storeId,
            Guid orderId,
            CancellationToken cancellationToken = default);

        Task<QueuedMessageResult> QueuePaymentStatusChangedAsync(
            Guid storeId,
            Guid orderId,
            CancellationToken cancellationToken = default);

        Task<QueuedMessageResult> QueueFulfillmentStatusChangedAsync(
            Guid storeId,
            Guid orderId,
            CancellationToken cancellationToken = default);
    }
}
