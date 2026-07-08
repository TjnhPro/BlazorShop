namespace BlazorShop.Application.ControlPlane.Dashboard
{
    public interface IControlPlaneDashboardService
    {
        Task<ControlPlaneDashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
    }
}
