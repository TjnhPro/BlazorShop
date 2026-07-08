namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using BlazorShop.Domain.Entities.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    public sealed class ControlPlaneDevelopmentSeeder
    {
        private readonly ControlPlaneDbContext dbContext;

        public ControlPlaneDevelopmentSeeder(ControlPlaneDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task SeedLocalMockNodeAsync(CancellationToken cancellationToken = default)
        {
            const string nodeKey = "local-mock-node";

            if (await this.dbContext.Nodes.AnyAsync(node => node.NodeKey == nodeKey, cancellationToken))
            {
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var node = new CommerceNode
            {
                NodeKey = nodeKey,
                Name = "Local Mock Node",
                Status = "unknown",
                Description = "Development-only placeholder node for Control Plane smoke testing.",
                CreatedAt = now,
                UpdatedAt = now,
                Endpoints =
                [
                    new CommerceNodeEndpoint
                    {
                        Kind = "control_api",
                        Url = "http://localhost:5180/api/controlpanel",
                        IsPrimary = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                ]
            };

            this.dbContext.Nodes.Add(node);
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
