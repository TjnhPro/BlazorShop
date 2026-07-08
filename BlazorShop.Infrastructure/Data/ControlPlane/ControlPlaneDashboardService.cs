namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using BlazorShop.Application.ControlPlane.Dashboard;

    using Microsoft.EntityFrameworkCore;

    public sealed class ControlPlaneDashboardService : IControlPlaneDashboardService
    {
        private readonly ControlPlaneDbContext dbContext;

        public ControlPlaneDashboardService(ControlPlaneDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<ControlPlaneDashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            var totalNodes = await this.dbContext.Nodes.CountAsync(cancellationToken);
            var healthyNodes = await this.dbContext.Nodes.CountAsync(node => node.Status == "healthy", cancellationToken);
            var warningNodes = await this.dbContext.Nodes.CountAsync(node => node.Status == "warning", cancellationToken);
            var downNodes = await this.dbContext.Nodes.CountAsync(node => node.Status == "down", cancellationToken);
            var totalStores = await this.dbContext.Stores.CountAsync(cancellationToken);

            return new ControlPlaneDashboardSummary(
                totalNodes,
                healthyNodes,
                warningNodes,
                downNodes,
                totalStores);
        }
    }
}
