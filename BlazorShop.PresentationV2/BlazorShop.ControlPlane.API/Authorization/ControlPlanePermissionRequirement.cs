namespace BlazorShop.ControlPlane.API.Authorization
{
    using Microsoft.AspNetCore.Authorization;

    public sealed class ControlPlanePermissionRequirement : IAuthorizationRequirement
    {
        public ControlPlanePermissionRequirement(string permission)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(permission);
            this.Permission = permission;
        }

        public string Permission { get; }
    }
}
