namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Text.Json;

    using BlazorShop.Application.ControlPlane.Actions;
    using BlazorShop.Application.ControlPlane.Common;
    using BlazorShop.Domain.Entities.ControlPlane;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    public sealed class ControlPlaneActionService : IControlPlaneActionService
    {
        private static readonly HashSet<string> AllowedActionTypes = new(StringComparer.Ordinal)
        {
            "probe_health",
            "fetch_capabilities",
            "sync_store_placeholder"
        };

        private static readonly HashSet<string> TerminalStatuses = new(StringComparer.Ordinal)
        {
            "cancelled",
            "succeeded"
        };

        private readonly ControlPlaneDbContext dbContext;
        private readonly ILogger<ControlPlaneActionService> logger;

        public ControlPlaneActionService(
            ControlPlaneDbContext dbContext,
            ILogger<ControlPlaneActionService> logger)
        {
            this.dbContext = dbContext;
            this.logger = logger;
        }

        public async Task<ControlPlaneActionListResponse> ListAsync(
            ControlPlaneActionListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var actions = this.dbContext.Actions
                .AsNoTracking()
                .Include(action => action.Node)
                .Include(action => action.Store)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = query.Status.Trim().ToLowerInvariant();
                actions = actions.Where(action => action.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.ActionType))
            {
                var actionType = query.ActionType.Trim().ToLowerInvariant();
                actions = actions.Where(action => action.ActionType == actionType);
            }

            if (query.NodePublicId is not null)
            {
                actions = actions.Where(action => action.Node != null && action.Node.PublicId == query.NodePublicId);
            }

            if (query.StorePublicId is not null)
            {
                actions = actions.Where(action => action.Store != null && action.Store.PublicId == query.StorePublicId);
            }

            var page = ControlPlanePaging.Normalize(query.PageNumber, query.PageSize);
            var totalCount = await actions.CountAsync(cancellationToken);
            var items = await actions
                .OrderByDescending(action => action.Id)
                .Skip(page.Skip)
                .Take(page.PageSize)
                .ToListAsync(cancellationToken);

            var actionIds = items.Select(action => action.Id).ToArray();
            var attemptCounts = await this.dbContext.ActionAttempts
                .AsNoTracking()
                .Where(attempt => actionIds.Contains(attempt.ActionId))
                .GroupBy(attempt => attempt.ActionId)
                .Select(group => new { ActionId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.ActionId, item => item.Count, cancellationToken);

            return new ControlPlaneActionListResponse(
                items.Select(action => MapSummary(action, attemptCounts.GetValueOrDefault(action.Id))).ToArray(),
                totalCount,
                page.PageNumber,
                page.PageSize,
                ControlPlanePaging.GetTotalPages(totalCount, page.PageSize));
        }

        public async Task<ApplicationResult<ControlPlaneActionDetail>> GetByPublicIdAsync(
            Guid publicId,
            CancellationToken cancellationToken = default)
        {
            var action = await this.LoadActionAsync(publicId, cancellationToken);
            return action is null ? NotFound("Control action was not found.") : Succeeded(MapDetail(action));
        }

        public async Task<ApplicationResult<ControlPlaneActionDetail>> EnqueueAsync(
            EnqueueControlActionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return ValidationFailed("Request body is required.");
            }

            var actionType = request.ActionType?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(actionType) || !AllowedActionTypes.Contains(actionType))
            {
                return ValidationFailed("Action type must be probe_health, fetch_capabilities, or sync_store_placeholder.");
            }

            var node = await this.dbContext.Nodes
                .Include(candidate => candidate.Stores)
                .FirstOrDefaultAsync(candidate => candidate.PublicId == request.NodePublicId, cancellationToken);

            if (node is null)
            {
                return NotFound("Node was not found.");
            }

            if (node.Status == "disabled")
            {
                return ValidationFailed("Disabled nodes cannot receive control actions.");
            }

            StoreRegistry? store = null;
            if (actionType == "sync_store_placeholder")
            {
                if (request.StorePublicId is null)
                {
                    return ValidationFailed("Store is required for sync_store_placeholder.");
                }

                store = await this.dbContext.Stores.FirstOrDefaultAsync(
                    candidate => candidate.PublicId == request.StorePublicId,
                    cancellationToken);

                if (store is null)
                {
                    return NotFound("Store was not found.");
                }

                if (store.ArchivedAt is not null || store.Status == "archived")
                {
                    return ValidationFailed("Archived stores cannot be synced.");
                }

                if (store.NodeId != node.Id)
                {
                    return ValidationFailed("Store must belong to the selected node.");
                }
            }
            else if (request.StorePublicId is not null)
            {
                return ValidationFailed("Store can only be attached to sync_store_placeholder actions.");
            }

            var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? Guid.NewGuid().ToString("N")
                : request.IdempotencyKey.Trim();

            if (idempotencyKey.Length > 128)
            {
                return ValidationFailed("Idempotency key must be 128 characters or fewer.");
            }

            var existing = await this.dbContext.Actions
                .Include(action => action.Node)
                .Include(action => action.Store)
                .Include(action => action.Attempts)
                .FirstOrDefaultAsync(
                    action => action.NodeId == node.Id && action.IdempotencyKey == idempotencyKey,
                    cancellationToken);

            if (existing is not null)
            {
                this.logger.LogInformation(
                    "Control action enqueue deduplicated for node {NodePublicId}, idempotency key {IdempotencyKey}, existing action {ActionPublicId}.",
                    node.PublicId,
                    idempotencyKey,
                    existing.PublicId);

                return new ApplicationResult<ControlPlaneActionDetail>(
                    true,
                    "Duplicate idempotency key matched an existing action.",
                    MapDetail(existing),
                    AlreadyExists: true);
            }

            var payload = BuildPayloadEnvelope(request.CorrelationId, request.PayloadJson);
            if (!payload.Success)
            {
                return ValidationFailed(payload.Message!);
            }

            var now = DateTimeOffset.UtcNow;
            var action = new ControlAction
            {
                NodeId = node.Id,
                Node = node,
                StoreId = store?.Id,
                Store = store,
                ActionType = actionType,
                Status = "queued",
                IdempotencyKey = idempotencyKey,
                PayloadJson = payload.Payload,
                CreatedAt = now,
                UpdatedAt = now
            };

            this.dbContext.Actions.Add(action);
            await this.dbContext.SaveChangesAsync(cancellationToken);

            this.logger.LogInformation(
                "Enqueued Control Plane action {ActionPublicId} of type {ActionType} for node {NodePublicId} and store {StorePublicId}.",
                action.PublicId,
                action.ActionType,
                node.PublicId,
                store?.PublicId);

            return Succeeded(MapDetail((await this.LoadActionAsync(action.PublicId, cancellationToken))!));
        }

        public async Task<ApplicationResult<ControlPlaneActionDetail>> RecordAttemptAsync(
            Guid publicId,
            RecordControlActionAttemptRequest request,
            CancellationToken cancellationToken = default)
        {
            var action = await this.LoadActionAsync(publicId, cancellationToken);
            if (action is null)
            {
                return NotFound("Control action was not found.");
            }

            if (request is null)
            {
                return ValidationFailed("Request body is required.");
            }

            var status = request.Status?.Trim().ToLowerInvariant();
            if (status is not ("running" or "failed" or "succeeded" or "cancelled"))
            {
                return ValidationFailed("Attempt status must be running, failed, succeeded, or cancelled.");
            }

            if (TerminalStatuses.Contains(action.Status))
            {
                return Conflict("Terminal actions cannot record new attempts.");
            }

            if (request.DurationMs < 0)
            {
                return ValidationFailed("Attempt duration must be zero or greater.");
            }

            var responseValidation = ValidateOptionalJson(request.ResponseJson, "Response JSON");
            if (responseValidation is not null)
            {
                return ValidationFailed(responseValidation);
            }

            var now = DateTimeOffset.UtcNow;
            var attempt = new ControlActionAttempt
            {
                ActionId = action.Id,
                AttemptNumber = action.Attempts.Count == 0 ? 1 : action.Attempts.Max(candidate => candidate.AttemptNumber) + 1,
                Status = status,
                HttpStatusCode = request.HttpStatusCode,
                DurationMs = request.DurationMs,
                ResponseJson = NormalizeOptionalJson(request.ResponseJson),
                ErrorCode = NormalizeOptionalText(request.ErrorCode),
                ErrorMessage = NormalizeOptionalText(request.ErrorMessage),
                StartedAt = now,
                CompletedAt = status == "running" ? null : now
            };

            action.Attempts.Add(attempt);
            action.Status = status == "running" ? "running" : status;
            action.StartedAt ??= now;
            action.CompletedAt = status == "running" ? null : now;
            action.UpdatedAt = now;
            action.ResultJson = status == "succeeded" ? NormalizeOptionalJson(request.ResponseJson) : action.ResultJson;
            action.ErrorCode = status == "failed" ? NormalizeOptionalText(request.ErrorCode) : null;
            action.ErrorMessage = status == "failed" ? NormalizeOptionalText(request.ErrorMessage) : null;

            await this.dbContext.SaveChangesAsync(cancellationToken);
            this.logger.LogInformation(
                "Recorded attempt {AttemptNumber} for Control Plane action {ActionPublicId} with status {AttemptStatus}, HTTP {HttpStatusCode}, duration {DurationMs} ms.",
                attempt.AttemptNumber,
                action.PublicId,
                attempt.Status,
                attempt.HttpStatusCode,
                attempt.DurationMs);

            return Succeeded(MapDetail(action));
        }

        public async Task<ApplicationResult<ControlPlaneActionDetail>> CancelAsync(
            Guid publicId,
            CancellationToken cancellationToken = default)
        {
            var action = await this.LoadActionAsync(publicId, cancellationToken);
            if (action is null)
            {
                return NotFound("Control action was not found.");
            }

            if (action.Status == "succeeded")
            {
                return Conflict("Succeeded actions cannot be cancelled.");
            }

            if (action.Status != "cancelled")
            {
                var now = DateTimeOffset.UtcNow;
                action.Status = "cancelled";
                action.CompletedAt = now;
                action.UpdatedAt = now;
                action.ErrorCode = null;
                action.ErrorMessage = null;
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);
            this.logger.LogInformation(
                "Cancelled Control Plane action {ActionPublicId} with current status {ActionStatus}.",
                action.PublicId,
                action.Status);

            return Succeeded(MapDetail(action));
        }

        private async Task<ControlAction?> LoadActionAsync(Guid publicId, CancellationToken cancellationToken)
        {
            return await this.dbContext.Actions
                .Include(action => action.Node)
                .Include(action => action.Store)
                .Include(action => action.Attempts)
                .FirstOrDefaultAsync(action => action.PublicId == publicId, cancellationToken);
        }

        private static PayloadResult BuildPayloadEnvelope(string? correlationId, string? payloadJson)
        {
            var correlation = string.IsNullOrWhiteSpace(correlationId)
                ? Guid.NewGuid().ToString("N")
                : correlationId.Trim();

            if (correlation.Length > 128)
            {
                return new PayloadResult(false, null, "Correlation id must be 128 characters or fewer.");
            }

            try
            {
                var normalizedPayload = NormalizeOptionalJson(payloadJson) ?? "{}";
                using var payloadDocument = JsonDocument.Parse(normalizedPayload);
                var envelope = new
                {
                    correlationId = correlation,
                    payload = payloadDocument.RootElement.Clone()
                };

                return new PayloadResult(true, JsonSerializer.Serialize(envelope), null);
            }
            catch (JsonException)
            {
                return new PayloadResult(false, null, "Payload must be valid JSON.");
            }
        }

        private static string? ValidateOptionalJson(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            try
            {
                using var _ = JsonDocument.Parse(value);
                return null;
            }
            catch (JsonException)
            {
                return $"{fieldName} must be valid JSON.";
            }
        }

        private static string? NormalizeOptionalJson(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            using var document = JsonDocument.Parse(value);
            return document.RootElement.GetRawText();
        }

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? ExtractCorrelationId(string? payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(payloadJson);
                return document.RootElement.TryGetProperty("correlationId", out var correlation)
                       && correlation.ValueKind == JsonValueKind.String
                    ? correlation.GetString()
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static ControlPlaneActionSummary MapSummary(ControlAction action)
        {
            return new ControlPlaneActionSummary(
                action.Id,
                action.PublicId,
                action.ActionType,
                action.Status,
                action.IdempotencyKey,
                ExtractCorrelationId(action.PayloadJson),
                action.Node?.PublicId ?? Guid.Empty,
                action.Node?.NodeKey ?? string.Empty,
                action.Node?.Name ?? string.Empty,
                action.Store?.PublicId,
                action.Store?.StoreKey,
                action.Store?.Name,
                action.ErrorCode,
                action.ErrorMessage,
                action.CreatedAt,
                action.UpdatedAt,
                action.StartedAt,
                action.CompletedAt,
                action.Attempts.Count);
        }

        private static ControlPlaneActionSummary MapSummary(ControlAction action, int attemptCount)
        {
            return new ControlPlaneActionSummary(
                action.Id,
                action.PublicId,
                action.ActionType,
                action.Status,
                action.IdempotencyKey,
                ExtractCorrelationId(action.PayloadJson),
                action.Node?.PublicId ?? Guid.Empty,
                action.Node?.NodeKey ?? string.Empty,
                action.Node?.Name ?? string.Empty,
                action.Store?.PublicId,
                action.Store?.StoreKey,
                action.Store?.Name,
                action.ErrorCode,
                action.ErrorMessage,
                action.CreatedAt,
                action.UpdatedAt,
                action.StartedAt,
                action.CompletedAt,
                attemptCount);
        }

        private static ControlPlaneActionDetail MapDetail(ControlAction action)
        {
            return new ControlPlaneActionDetail(
                action.Id,
                action.PublicId,
                action.ActionType,
                action.Status,
                action.IdempotencyKey,
                ExtractCorrelationId(action.PayloadJson),
                action.PayloadJson,
                action.ResultJson,
                action.ErrorCode,
                action.ErrorMessage,
                SuggestedFix(action.ErrorCode, action.ErrorMessage),
                action.Node?.PublicId ?? Guid.Empty,
                action.Node?.NodeKey ?? string.Empty,
                action.Node?.Name ?? string.Empty,
                action.Store?.PublicId,
                action.Store?.StoreKey,
                action.Store?.Name,
                action.CreatedAt,
                action.UpdatedAt,
                action.StartedAt,
                action.CompletedAt,
                action.Attempts
                    .OrderBy(attempt => attempt.AttemptNumber)
                    .Select(attempt => new ControlPlaneActionAttemptDto(
                        attempt.Id,
                        attempt.AttemptNumber,
                        attempt.Status,
                        attempt.HttpStatusCode,
                        attempt.DurationMs,
                        attempt.ResponseJson,
                        attempt.ErrorCode,
                        attempt.ErrorMessage,
                        SuggestedFix(attempt.ErrorCode, attempt.ErrorMessage),
                        attempt.StartedAt,
                        attempt.CompletedAt))
                    .ToArray());
        }

        private static string? SuggestedFix(string? errorCode, string? errorMessage)
        {
            return errorCode switch
            {
                "timeout" => "Check the node control API URL, network path, and node process health before retrying.",
                "unauthorized" => "Rotate the node API key and verify the active credential is installed on the Commerce Node.",
                "unsupported" => "Refresh node capabilities before enqueueing this action again.",
                null when string.IsNullOrWhiteSpace(errorMessage) => null,
                _ => "Review the node response, fix the reported cause, then retry with a new attempt."
            };
        }

        private static ApplicationResult<ControlPlaneActionDetail> Succeeded(ControlPlaneActionDetail payload)
        {
            return new ApplicationResult<ControlPlaneActionDetail>(true, Payload: payload);
        }

        private static ApplicationResult<ControlPlaneActionDetail> ValidationFailed(string message)
        {
            return new ApplicationResult<ControlPlaneActionDetail>(false, message, Failure: ApplicationErrorKind.Validation);
        }

        private static ApplicationResult<ControlPlaneActionDetail> Conflict(string message)
        {
            return new ApplicationResult<ControlPlaneActionDetail>(false, message, Failure: ApplicationErrorKind.Conflict);
        }

        private static ApplicationResult<ControlPlaneActionDetail> NotFound(string message)
        {
            return new ApplicationResult<ControlPlaneActionDetail>(false, message, Failure: ApplicationErrorKind.NotFound);
        }

        private sealed record PayloadResult(bool Success, string? Payload, string? Message);
    }
}
