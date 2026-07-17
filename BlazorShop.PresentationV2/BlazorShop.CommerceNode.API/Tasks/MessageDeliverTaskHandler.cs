namespace BlazorShop.CommerceNode.API.Tasks
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Tasks;

    public sealed class MessageDeliverTaskHandler : ICommerceTaskHandler
    {
        private readonly IMessageDeliveryService deliveryService;

        public MessageDeliverTaskHandler(IMessageDeliveryService deliveryService)
        {
            this.deliveryService = deliveryService;
        }

        public string TaskType => TransactionalMessageTaskTypes.Deliver;

        public async Task<CommerceTaskHandlerResult> ExecuteAsync(
            CommerceTaskHandlerContext context,
            CancellationToken cancellationToken)
        {
            MessageDeliverTaskPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<MessageDeliverTaskPayload>(context.PayloadJson);
            }
            catch (JsonException)
            {
                return CommerceTaskHandlerResult.Failed("Message delivery payload is not valid JSON.", "invalid_message_payload");
            }

            if (payload is null || payload.QueuedMessagePublicId == Guid.Empty)
            {
                return CommerceTaskHandlerResult.Failed("Queued message id is required.", "invalid_message_payload");
            }

            var result = await this.deliveryService.DeliverAsync(
                payload.QueuedMessagePublicId,
                context.AttemptNumber,
                cancellationToken);

            return result.Success
                ? CommerceTaskHandlerResult.Succeeded(result.Message)
                : CommerceTaskHandlerResult.Failed(
                    result.Message ?? "Queued message delivery failed.",
                    result.ErrorCode,
                    result.Retryable);
        }
    }
}
