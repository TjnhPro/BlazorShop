namespace BlazorShop.CommerceNode.API.Workers
{
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.CommerceNode.API.Configuration;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;

    public sealed class CommerceTaskWorker : BackgroundService
    {
        private const string HandlerStepKey = "execute_handler";

        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<CommerceTaskWorker> logger;
        private readonly CommerceTaskWorkerOptions options;
        private readonly string workerId;

        public CommerceTaskWorker(
            IServiceScopeFactory scopeFactory,
            IOptions<CommerceTaskWorkerOptions> options,
            ILogger<CommerceTaskWorker> logger)
        {
            this.scopeFactory = scopeFactory;
            this.logger = logger;
            this.options = options.Value;
            this.workerId = string.IsNullOrWhiteSpace(this.options.WorkerId)
                ? $"{Environment.MachineName}-{Guid.NewGuid():N}"
                : this.options.WorkerId.Trim();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!this.options.Enabled)
            {
                this.logger.LogInformation("Commerce task worker is disabled.");
                return;
            }

            var pollInterval = TimeSpan.FromSeconds(Math.Max(1, this.options.PollIntervalSeconds));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var processed = await this.ProcessNextTaskAsync(stoppingToken);
                    if (!processed)
                    {
                        await Task.Delay(pollInterval, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Commerce task worker loop failed.");
                    await Task.Delay(pollInterval, stoppingToken);
                }
            }
        }

        private async Task<bool> ProcessNextTaskAsync(CancellationToken cancellationToken)
        {
            using var scope = this.scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CommerceNodeDbContext>();
            var handlers = scope.ServiceProvider.GetServices<ICommerceTaskHandler>()
                .GroupBy(handler => handler.TaskType, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            var task = await this.AcquireNextTaskAsync(context, cancellationToken);
            if (task is null)
            {
                return false;
            }

            var step = new CommerceTaskStep
            {
                Id = Guid.NewGuid(),
                TaskId = task.Id,
                StepKey = HandlerStepKey,
                Status = CommerceTaskStepStatuses.Running,
                AttemptNumber = task.AttemptCount,
                StartedAt = DateTimeOffset.UtcNow,
            };
            context.CommerceTaskSteps.Add(step);
            await context.SaveChangesAsync(cancellationToken);

            CommerceTaskHandlerResult result;

            if (!handlers.TryGetValue(task.TaskType, out var handler))
            {
                result = CommerceTaskHandlerResult.Failed(
                    $"No handler is registered for task type '{task.TaskType}'.",
                    "handler_not_found",
                    retryable: false);
            }
            else
            {
                try
                {
                    result = await handler.ExecuteAsync(
                        new CommerceTaskHandlerContext(
                            task.Id,
                            task.PublicId,
                            task.TaskType,
                            task.PayloadSchemaVersion,
                            task.PayloadJson,
                            task.AttemptCount,
                            token => IsCancellationRequestedAsync(context, task.Id, token)),
                        cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    this.logger.LogError(
                        ex,
                        "Commerce task handler failed. TaskPublicId={TaskPublicId} TaskType={TaskType}",
                        task.PublicId,
                        task.TaskType);

                    result = CommerceTaskHandlerResult.Failed(
                        "Task handler failed with an unexpected error.",
                        "handler_exception",
                        retryable: true);
                }
            }

            await this.CompleteTaskAsync(context, task.Id, step.Id, result, cancellationToken);
            return true;
        }

        private async Task<CommerceTask?> AcquireNextTaskAsync(
            CommerceNodeDbContext context,
            CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var task = await context.CommerceTasks
                .Where(entity =>
                    entity.Status == CommerceTaskStatuses.Pending ||
                    (entity.Status == CommerceTaskStatuses.WaitingRetry &&
                     (entity.NextAttemptAt == null || entity.NextAttemptAt <= now)))
                .OrderBy(entity => entity.CreatedAt)
                .ThenBy(entity => entity.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (task is null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(task.LockKey))
            {
                var hasConflictingTask = await context.CommerceTasks.AnyAsync(
                    entity =>
                        entity.Id != task.Id &&
                        entity.LockKey == task.LockKey &&
                        entity.Status == CommerceTaskStatuses.Running,
                    cancellationToken);

                if (hasConflictingTask)
                {
                    return null;
                }
            }

            if (task.CancelRequestedAt is not null)
            {
                task.Status = CommerceTaskStatuses.Cancelled;
                task.CompletedAt = now;
                task.UpdatedAt = now;
                await context.SaveChangesAsync(cancellationToken);
                return null;
            }

            task.Status = CommerceTaskStatuses.Running;
            task.AttemptCount += 1;
            task.StartedAt ??= now;
            task.WorkerId = this.workerId;
            task.LastHeartbeatAt = now;
            task.UpdatedAt = now;

            await context.SaveChangesAsync(cancellationToken);
            return task;
        }

        private async Task CompleteTaskAsync(
            CommerceNodeDbContext context,
            Guid taskId,
            Guid stepId,
            CommerceTaskHandlerResult result,
            CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var task = await context.CommerceTasks.FirstAsync(entity => entity.Id == taskId, cancellationToken);
            var step = await context.CommerceTaskSteps.FirstAsync(entity => entity.Id == stepId, cancellationToken);

            var cancelRequested = task.CancelRequestedAt is not null;
            step.CompletedAt = now;
            step.ResultJson = result.ResultJson;

            if (result.Success && !cancelRequested)
            {
                step.Status = CommerceTaskStepStatuses.Succeeded;
                task.Status = CommerceTaskStatuses.Succeeded;
                task.ResultJson = result.ResultJson;
                task.ErrorCode = null;
                task.ErrorMessage = null;
                task.CompletedAt = now;
            }
            else if (cancelRequested)
            {
                step.Status = result.Success ? CommerceTaskStepStatuses.Skipped : CommerceTaskStepStatuses.Failed;
                step.ErrorCode = result.ErrorCode;
                step.ErrorMessage = result.Message;
                task.Status = CommerceTaskStatuses.Cancelled;
                task.ErrorCode = result.ErrorCode;
                task.ErrorMessage = result.Message;
                task.CompletedAt = now;
            }
            else
            {
                step.Status = CommerceTaskStepStatuses.Failed;
                step.ErrorCode = result.ErrorCode;
                step.ErrorMessage = result.Message;
                task.ResultJson = result.ResultJson;
                task.ErrorCode = result.ErrorCode;
                task.ErrorMessage = result.Message;

                if (result.Retryable && task.AttemptCount < task.MaxAttempts)
                {
                    task.Status = CommerceTaskStatuses.WaitingRetry;
                    task.NextAttemptAt = now.AddSeconds(Math.Max(1, this.options.RetryDelaySeconds));
                }
                else
                {
                    task.Status = result.Retryable ? CommerceTaskStatuses.Dead : CommerceTaskStatuses.Failed;
                    task.CompletedAt = now;
                }
            }

            task.WorkerId = null;
            task.LastHeartbeatAt = now;
            task.UpdatedAt = now;

            await context.SaveChangesAsync(cancellationToken);
        }

        private static async Task<bool> IsCancellationRequestedAsync(
            CommerceNodeDbContext context,
            Guid taskId,
            CancellationToken cancellationToken)
        {
            return await context.CommerceTasks
                .Where(task => task.Id == taskId)
                .AnyAsync(task => task.CancelRequestedAt != null, cancellationToken);
        }
    }
}
