namespace BlazorShop.Application.ControlPlane.Security
{
    public sealed record ControlPlaneProfileResult(
        long AdminUserId,
        string IdentityUserId,
        string Email,
        string DisplayName,
        bool Created);
}
