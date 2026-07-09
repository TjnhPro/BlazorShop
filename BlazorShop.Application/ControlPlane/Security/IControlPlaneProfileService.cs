namespace BlazorShop.Application.ControlPlane.Security
{
    public interface IControlPlaneProfileService
    {
        Task<ControlPlaneProfileResult> EnsureProfileAsync(
            string identityUserId,
            string email,
            string displayName,
            CancellationToken cancellationToken = default);
    }
}
