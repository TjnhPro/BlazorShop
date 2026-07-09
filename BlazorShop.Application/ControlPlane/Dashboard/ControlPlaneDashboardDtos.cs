namespace BlazorShop.Application.ControlPlane.Dashboard
{
    public sealed record ControlPlaneDashboardSummary(
        int TotalNodes,
        int HealthyNodes,
        int WarningNodes,
        int DownNodes,
        int TotalStores);
}
