namespace BlazorShop.CommerceNode.API.Tasks
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Tasks;

    public sealed class CompleteTestCommerceTaskHandler : ICommerceTaskHandler
    {
        public string TaskType => "commerce.test.complete";

        public Task<CommerceTaskHandlerResult> ExecuteAsync(
            CommerceTaskHandlerContext context,
            CancellationToken cancellationToken)
        {
            var resultJson = JsonSerializer.Serialize(
                new
                {
                    context.PublicId,
                    context.AttemptNumber,
                    CompletedAt = DateTimeOffset.UtcNow,
                });

            return Task.FromResult(CommerceTaskHandlerResult.Succeeded("Test task completed.", resultJson));
        }
    }

    public sealed class FailTestCommerceTaskHandler : ICommerceTaskHandler
    {
        public string TaskType => "commerce.test.fail";

        public Task<CommerceTaskHandlerResult> ExecuteAsync(
            CommerceTaskHandlerContext context,
            CancellationToken cancellationToken)
        {
            var retryable = false;
            if (!string.IsNullOrWhiteSpace(context.PayloadJson))
            {
                using var document = JsonDocument.Parse(context.PayloadJson);
                if (document.RootElement.TryGetProperty("retryable", out var retryableProperty) &&
                    retryableProperty.ValueKind == JsonValueKind.True)
                {
                    retryable = true;
                }
            }

            return Task.FromResult(
                CommerceTaskHandlerResult.Failed(
                    "Test task failed.",
                    "test_task_failed",
                    retryable));
        }
    }

    public sealed class WaitTestCommerceTaskHandler : ICommerceTaskHandler
    {
        public string TaskType => "commerce.test.wait";

        public async Task<CommerceTaskHandlerResult> ExecuteAsync(
            CommerceTaskHandlerContext context,
            CancellationToken cancellationToken)
        {
            var delayMs = 5000;
            if (!string.IsNullOrWhiteSpace(context.PayloadJson))
            {
                using var document = JsonDocument.Parse(context.PayloadJson);
                if (document.RootElement.TryGetProperty("delayMs", out var delayProperty) &&
                    delayProperty.TryGetInt32(out var configuredDelayMs))
                {
                    delayMs = Math.Clamp(configuredDelayMs, 100, 30000);
                }
            }

            var remainingDelayMs = delayMs;
            while (remainingDelayMs > 0)
            {
                if (await context.IsCancellationRequestedAsync(cancellationToken))
                {
                    return CommerceTaskHandlerResult.Failed(
                        "Test task observed cancellation.",
                        "test_task_cancelled",
                        retryable: false);
                }

                var currentDelayMs = Math.Min(500, remainingDelayMs);
                await Task.Delay(currentDelayMs, cancellationToken);
                remainingDelayMs -= currentDelayMs;
            }

            return CommerceTaskHandlerResult.Succeeded(
                "Test wait task completed.",
                JsonSerializer.Serialize(new { DelayMs = delayMs }));
        }
    }
}
