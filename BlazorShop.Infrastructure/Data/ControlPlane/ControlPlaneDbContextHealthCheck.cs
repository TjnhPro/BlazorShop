namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using Microsoft.Extensions.Diagnostics.HealthChecks;

    public sealed class ControlPlaneDbContextHealthCheck : IHealthCheck
    {
        private readonly ControlPlaneDbContext dbContext;

        public ControlPlaneDbContextHealthCheck(ControlPlaneDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await dbContext.Database.CanConnectAsync(cancellationToken)
                    ? HealthCheckResult.Healthy("Control Plane database is reachable.")
                    : HealthCheckResult.Unhealthy("Control Plane database is unreachable.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Control Plane database check failed.", ex);
            }
        }
    }
}
