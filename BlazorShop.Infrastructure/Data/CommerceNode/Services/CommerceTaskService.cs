namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceTaskService : ICommerceTaskService
    {
        private const int DefaultTake = 100;
        private const int MaxTake = 200;

        private readonly CommerceNodeDbContext context;

        public CommerceTaskService(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<CommerceTaskOperationResult<CommerceTaskSummary>> EnqueueAsync(
            EnqueueCommerceTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            var validationError = ValidateEnqueueRequest(request);
            if (validationError is not null)
            {
                return Failure<CommerceTaskSummary>(CommerceTaskOperationFailure.Validation, validationError);
            }

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existing = await this.context.CommerceTasks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        task => task.IdempotencyKey == request.IdempotencyKey.Trim(),
                        cancellationToken);

                if (existing is not null)
                {
                    return new CommerceTaskOperationResult<CommerceTaskSummary>(
                        true,
                        "Task already exists for this idempotency key.",
                        MapSummary(existing),
                        AlreadyExists: true);
                }
            }

            var now = DateTimeOffset.UtcNow;
            var taskEntity = new CommerceTask
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                TaskType = request.TaskType.Trim(),
                Status = CommerceTaskStatuses.Pending,
                IdempotencyKey = NormalizeOptional(request.IdempotencyKey),
                LockKey = NormalizeOptional(request.LockKey),
                PayloadSchemaVersion = string.IsNullOrWhiteSpace(request.PayloadSchemaVersion)
                    ? "v1"
                    : request.PayloadSchemaVersion.Trim(),
                PayloadJson = NormalizePayloadJson(request.PayloadJson),
                AttemptCount = 0,
                MaxAttempts = Math.Max(1, request.MaxAttempts),
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = NormalizeOptional(request.CreatedBy),
                CorrelationId = NormalizeOptional(request.CorrelationId),
            };

            this.context.CommerceTasks.Add(taskEntity);
            await this.context.SaveChangesAsync(cancellationToken);

            return new CommerceTaskOperationResult<CommerceTaskSummary>(
                true,
                "Task queued.",
                MapSummary(taskEntity));
        }

        public async Task<CommerceTaskOperationResult<CommerceTaskListResponse>> ListAsync(
            CommerceTaskListQuery query,
            CancellationToken cancellationToken = default)
        {
            var skip = Math.Max(0, query.Skip);
            var take = Math.Clamp(query.Take <= 0 ? DefaultTake : query.Take, 1, MaxTake);

            var tasks = this.context.CommerceTasks.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = query.Status.Trim();
                tasks = tasks.Where(task => task.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.TaskType))
            {
                var taskType = query.TaskType.Trim();
                tasks = tasks.Where(task => task.TaskType == taskType);
            }

            if (query.CreatedFrom is not null)
            {
                tasks = tasks.Where(task => task.CreatedAt >= query.CreatedFrom.Value);
            }

            if (query.CreatedTo is not null)
            {
                tasks = tasks.Where(task => task.CreatedAt <= query.CreatedTo.Value);
            }

            var totalCount = await tasks.CountAsync(cancellationToken);
            var items = await tasks
                .OrderByDescending(task => task.CreatedAt)
                .ThenByDescending(task => task.Id)
                .Skip(skip)
                .Take(take)
                .Select(task => MapSummary(task))
                .ToListAsync(cancellationToken);

            return new CommerceTaskOperationResult<CommerceTaskListResponse>(
                true,
                "Tasks retrieved.",
                new CommerceTaskListResponse(items, totalCount, skip, take));
        }

        public async Task<CommerceTaskOperationResult<CommerceTaskDetail>> GetByPublicIdAsync(
            Guid publicId,
            CancellationToken cancellationToken = default)
        {
            var task = await this.LoadDetailQuery()
                .FirstOrDefaultAsync(entity => entity.PublicId == publicId, cancellationToken);

            if (task is null)
            {
                return Failure<CommerceTaskDetail>(CommerceTaskOperationFailure.NotFound, "Task was not found.");
            }

            return new CommerceTaskOperationResult<CommerceTaskDetail>(
                true,
                "Task retrieved.",
                MapDetail(task));
        }

        public async Task<CommerceTaskOperationResult<CommerceTaskDetail>> CancelAsync(
            Guid publicId,
            CancelCommerceTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            var task = await this.context.CommerceTasks
                .Include(entity => entity.Steps.OrderBy(step => step.StartedAt).ThenBy(step => step.Id))
                .FirstOrDefaultAsync(entity => entity.PublicId == publicId, cancellationToken);

            if (task is null)
            {
                return Failure<CommerceTaskDetail>(CommerceTaskOperationFailure.NotFound, "Task was not found.");
            }

            if (task.Status is CommerceTaskStatuses.Succeeded or CommerceTaskStatuses.Cancelled)
            {
                return Failure<CommerceTaskDetail>(
                    CommerceTaskOperationFailure.Conflict,
                    "Task cannot be cancelled because it is already terminal.");
            }

            var now = DateTimeOffset.UtcNow;
            task.CancelRequestedAt = now;
            task.CancelReason = NormalizeOptional(request.Reason);
            task.UpdatedAt = now;

            if (task.Status is CommerceTaskStatuses.Pending or CommerceTaskStatuses.WaitingRetry)
            {
                task.Status = CommerceTaskStatuses.Cancelled;
                task.CompletedAt = now;
            }

            await this.context.SaveChangesAsync(cancellationToken);

            return new CommerceTaskOperationResult<CommerceTaskDetail>(
                true,
                task.Status == CommerceTaskStatuses.Cancelled ? "Task cancelled." : "Task cancellation requested.",
                MapDetail(task));
        }

        public async Task<CommerceTaskOperationResult<CommerceTaskDetail>> RetryAsync(
            Guid publicId,
            RetryCommerceTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            var task = await this.context.CommerceTasks
                .Include(entity => entity.Steps.OrderBy(step => step.StartedAt).ThenBy(step => step.Id))
                .FirstOrDefaultAsync(entity => entity.PublicId == publicId, cancellationToken);

            if (task is null)
            {
                return Failure<CommerceTaskDetail>(CommerceTaskOperationFailure.NotFound, "Task was not found.");
            }

            if (task.Status is not (CommerceTaskStatuses.Failed or CommerceTaskStatuses.Dead))
            {
                return Failure<CommerceTaskDetail>(
                    CommerceTaskOperationFailure.Conflict,
                    "Only failed or dead tasks can be retried.");
            }

            var now = DateTimeOffset.UtcNow;
            task.Status = CommerceTaskStatuses.Pending;
            task.NextAttemptAt = null;
            task.CompletedAt = null;
            task.ErrorCode = null;
            task.ErrorMessage = null;
            task.CancelRequestedAt = null;
            task.CancelReason = null;
            task.WorkerId = null;
            task.LastHeartbeatAt = null;
            task.UpdatedAt = now;

            await this.context.SaveChangesAsync(cancellationToken);

            return new CommerceTaskOperationResult<CommerceTaskDetail>(
                true,
                "Task queued for retry.",
                MapDetail(task));
        }

        private IQueryable<CommerceTask> LoadDetailQuery()
        {
            return this.context.CommerceTasks
                .AsNoTracking()
                .Include(entity => entity.Steps.OrderBy(step => step.StartedAt).ThenBy(step => step.Id));
        }

        private static string? ValidateEnqueueRequest(EnqueueCommerceTaskRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TaskType))
            {
                return "Task type is required.";
            }

            if (request.MaxAttempts < 1)
            {
                return "Max attempts must be at least 1.";
            }

            if (!string.IsNullOrWhiteSpace(request.PayloadJson) && !IsValidJson(request.PayloadJson))
            {
                return "PayloadJson must be valid JSON.";
            }

            return null;
        }

        private static bool IsValidJson(string value)
        {
            try
            {
                using var _ = JsonDocument.Parse(value);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static string NormalizePayloadJson(string? payloadJson)
        {
            return string.IsNullOrWhiteSpace(payloadJson)
                ? "{}"
                : payloadJson.Trim();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static CommerceTaskOperationResult<TPayload> Failure<TPayload>(
            CommerceTaskOperationFailure failure,
            string message)
        {
            return new CommerceTaskOperationResult<TPayload>(false, message, Failure: failure);
        }

        private static CommerceTaskSummary MapSummary(CommerceTask task)
        {
            return new CommerceTaskSummary(
                task.PublicId,
                task.TaskType,
                task.Status,
                task.IdempotencyKey,
                task.LockKey,
                task.PayloadSchemaVersion,
                task.ErrorCode,
                task.ErrorMessage,
                task.AttemptCount,
                task.MaxAttempts,
                task.NextAttemptAt,
                task.StartedAt,
                task.CompletedAt,
                task.CreatedAt,
                task.UpdatedAt,
                task.CreatedBy,
                task.CorrelationId,
                task.CancelRequestedAt,
                task.WorkerId,
                task.LastHeartbeatAt);
        }

        private static CommerceTaskDetail MapDetail(CommerceTask task)
        {
            return new CommerceTaskDetail(
                task.PublicId,
                task.TaskType,
                task.Status,
                task.IdempotencyKey,
                task.LockKey,
                task.PayloadSchemaVersion,
                task.PayloadJson,
                task.ResultJson,
                task.ErrorCode,
                task.ErrorMessage,
                task.AttemptCount,
                task.MaxAttempts,
                task.NextAttemptAt,
                task.StartedAt,
                task.CompletedAt,
                task.CreatedAt,
                task.UpdatedAt,
                task.CreatedBy,
                task.CorrelationId,
                task.CancelRequestedAt,
                task.CancelReason,
                task.WorkerId,
                task.LastHeartbeatAt,
                task.Steps.Select(MapStep).ToList());
        }

        private static CommerceTaskStepDto MapStep(CommerceTaskStep step)
        {
            return new CommerceTaskStepDto(
                step.Id,
                step.StepKey,
                step.Status,
                step.AttemptNumber,
                step.ResultJson,
                step.ErrorCode,
                step.ErrorMessage,
                step.StartedAt,
                step.CompletedAt);
        }
    }
}
