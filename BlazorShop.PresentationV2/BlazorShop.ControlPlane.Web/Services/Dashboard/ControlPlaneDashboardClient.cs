namespace BlazorShop.ControlPlane.Web.Services.Dashboard
{
    using BlazorShop.ControlPlane.Web.Services.Common;

    public interface IControlPlaneDashboardClient
    {
        Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
    }

    public sealed class ControlPlaneDashboardClient : IControlPlaneDashboardClient
    {
        private readonly IControlPlaneApiClient apiClient;

        public ControlPlaneDashboardClient(IControlPlaneApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.GetPrivateAsync<DashboardSummary>(
                "api/control-plane/dashboard/summary",
                "Unable to load dashboard summary.",
                cancellationToken);

            if (result.Success)
            {
                return result.Data ?? new DashboardSummary(0, 0, 0, 0, 0);
            }

            throw new InvalidOperationException(result.Message);
        }
    }

    public sealed record DashboardSummary(
        int TotalNodes,
        int HealthyNodes,
        int WarningNodes,
        int DownNodes,
        int TotalStores);
}
