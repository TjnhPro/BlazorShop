namespace BlazorShop.CommerceNode.API.Tasks
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    public sealed class OrderCreatedTaskHandler : ICommerceTaskHandler
    {
        private readonly ICommerceTransactionalMessageService messageService;

        public OrderCreatedTaskHandler(ICommerceTransactionalMessageService messageService)
        {
            this.messageService = messageService;
        }

        public string TaskType => OrderPlacementTaskTypes.OrderCreated;

        public async Task<CommerceTaskHandlerResult> ExecuteAsync(
            CommerceTaskHandlerContext context,
            CancellationToken cancellationToken)
        {
            OrderCreatedTaskPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<OrderCreatedTaskPayload>(context.PayloadJson);
            }
            catch (JsonException)
            {
                return CommerceTaskHandlerResult.Failed("Order created payload is not valid JSON.", "invalid_order_created_payload");
            }

            if (payload is null || payload.OrderId == Guid.Empty || payload.StoreId == Guid.Empty)
            {
                return CommerceTaskHandlerResult.Failed("Order and store are required.", "invalid_order_created_payload");
            }

            var result = await this.messageService.QueueOrderPlacedAsync(
                payload.StoreId,
                payload.OrderId,
                cancellationToken);

            return result.Success
                ? CommerceTaskHandlerResult.Succeeded(result.Message ?? "Order placed message queued.")
                : CommerceTaskHandlerResult.Failed(
                    result.Message ?? "Order placed message could not be queued.",
                    result.ErrorCode,
                    retryable: true);
        }
    }
}
