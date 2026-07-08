namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using BlazorShop.Application.ControlPlane.Audit;
    using BlazorShop.Domain.Entities.ControlPlane;

    public sealed class ControlPlaneAuditService : IControlPlaneAuditService
    {
        private readonly ControlPlaneDbContext dbContext;

        public ControlPlaneAuditService(ControlPlaneDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task WriteAsync(ControlPlaneAuditEntry entry, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entry);

            var auditLog = new ControlAuditLog
            {
                ActorAdminUserId = entry.ActorAdminUserId,
                ActorIdentityUserId = entry.ActorIdentityUserId,
                ActorEmail = entry.ActorEmail,
                Action = entry.Action,
                EntityType = entry.EntityType,
                EntityPublicId = entry.EntityPublicId,
                NodeId = entry.NodeId,
                StoreId = entry.StoreId,
                ControlActionId = entry.ControlActionId,
                Result = entry.Result,
                MetadataJson = entry.MetadataJson,
                IpAddress = entry.IpAddress,
                UserAgent = entry.UserAgent,
                CreatedAt = DateTimeOffset.UtcNow
            };

            this.dbContext.AuditLogs.Add(auditLog);
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
