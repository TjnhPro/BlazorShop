namespace BlazorShop.Application.ControlPlane.Audit
{
    public interface IControlPlaneAuditService
    {
        Task WriteAsync(ControlPlaneAuditEntry entry, CancellationToken cancellationToken = default);
    }
}
