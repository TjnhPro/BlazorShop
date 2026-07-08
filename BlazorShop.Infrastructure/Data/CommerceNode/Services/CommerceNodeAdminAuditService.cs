namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeAdminAuditService : IAdminAuditService
    {
        private readonly CommerceNodeDbContext context;
        private readonly ICommerceNodeAuditActorAccessor actorAccessor;

        public CommerceNodeAdminAuditService(
            CommerceNodeDbContext context,
            ICommerceNodeAuditActorAccessor actorAccessor)
        {
            this.context = context;
            this.actorAccessor = actorAccessor;
        }

        public async Task<PagedResult<AdminAuditLogDto>> GetAsync(AdminAuditQueryDto query)
        {
            ArgumentNullException.ThrowIfNull(query);

            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var logs = this.context.AdminAuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Actor))
            {
                var actor = query.Actor.Trim().ToLowerInvariant();
                logs = logs.Where(log =>
                    (log.ActorEmail != null && log.ActorEmail.ToLower().Contains(actor)) ||
                    (log.ActorUserId != null && log.ActorUserId.ToLower().Contains(actor)));
            }

            if (!string.IsNullOrWhiteSpace(query.Action))
            {
                var action = query.Action.Trim().ToLowerInvariant();
                logs = logs.Where(log => log.Action.ToLower().Contains(action));
            }

            if (!string.IsNullOrWhiteSpace(query.EntityType))
            {
                var entityType = query.EntityType.Trim().ToLowerInvariant();
                logs = logs.Where(log => log.EntityType.ToLower().Contains(entityType));
            }

            if (!string.IsNullOrWhiteSpace(query.EntityId))
            {
                var entityId = query.EntityId.Trim().ToLowerInvariant();
                logs = logs.Where(log => log.EntityId != null && log.EntityId.ToLower().Contains(entityId));
            }

            if (query.FromUtc.HasValue)
            {
                logs = logs.Where(log => log.CreatedOn >= EnsureUtc(query.FromUtc.Value));
            }

            if (query.ToUtc.HasValue)
            {
                logs = logs.Where(log => log.CreatedOn <= EnsureUtc(query.ToUtc.Value));
            }

            var total = await logs.CountAsync();
            var items = await logs
                .OrderByDescending(log => log.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(log => Map(log))
                .ToListAsync();

            return new PagedResult<AdminAuditLogDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = total,
            };
        }

        public async Task<ServiceResponse<AdminAuditLogDto>> GetByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return Failure("Audit log id is required.", ServiceResponseType.ValidationError);
            }

            var log = await this.context.AdminAuditLogs.AsNoTracking().FirstOrDefaultAsync(entry => entry.Id == id);
            return log is null
                ? Failure("Audit log entry not found.", ServiceResponseType.NotFound)
                : Success(Map(log), "Audit log entry retrieved successfully.");
        }

        public async Task<ServiceResponse<AdminAuditLogDto>> LogAsync(CreateAdminAuditLogDto request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Action))
            {
                return Failure("Audit action is required.", ServiceResponseType.ValidationError);
            }

            if (string.IsNullOrWhiteSpace(request.EntityType))
            {
                return Failure("Audit entity type is required.", ServiceResponseType.ValidationError);
            }

            var actor = this.actorAccessor.GetCurrentActor();
            var entry = new AdminAuditLog
            {
                Action = request.Action.Trim(),
                EntityType = request.EntityType.Trim(),
                EntityId = string.IsNullOrWhiteSpace(request.EntityId) ? null : request.EntityId.Trim(),
                Summary = string.IsNullOrWhiteSpace(request.Summary) ? request.Action.Trim() : request.Summary.Trim(),
                MetadataJson = MergeMetadata(request.MetadataJson, actor.ActionId),
                ActorUserId = actor.ActorUserId,
                ActorEmail = actor.ActorEmail,
                IpAddress = actor.IpAddress,
                UserAgent = actor.UserAgent,
                CreatedOn = DateTime.UtcNow,
            };

            this.context.AdminAuditLogs.Add(entry);
            await this.context.SaveChangesAsync();

            return Success(Map(entry), "Audit log entry created successfully.");
        }

        private static string? MergeMetadata(string? metadataJson, string? actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                return string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson;
            }

            if (string.IsNullOrWhiteSpace(metadataJson))
            {
                return JsonSerializer.Serialize(new { ControlPlaneActionId = actionId });
            }

            return metadataJson;
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };
        }

        private static AdminAuditLogDto Map(AdminAuditLog log)
        {
            return new AdminAuditLogDto
            {
                Id = log.Id,
                ActorUserId = log.ActorUserId,
                ActorEmail = log.ActorEmail,
                Action = log.Action,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                Summary = log.Summary,
                MetadataJson = log.MetadataJson,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                CreatedOn = log.CreatedOn,
            };
        }

        private static ServiceResponse<AdminAuditLogDto> Success(AdminAuditLogDto payload, string message)
        {
            return new ServiceResponse<AdminAuditLogDto>(true, message, payload.Id)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<AdminAuditLogDto> Failure(string message, ServiceResponseType responseType)
        {
            return new ServiceResponse<AdminAuditLogDto>(false, message)
            {
                ResponseType = responseType,
            };
        }
    }
}
