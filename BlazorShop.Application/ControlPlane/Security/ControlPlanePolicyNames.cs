namespace BlazorShop.Application.ControlPlane.Security
{
    public static class ControlPlanePolicyNames
    {
        public const string NodesRead = "ControlPlane.Nodes.Read";
        public const string NodesWrite = "ControlPlane.Nodes.Write";
        public const string CredentialsRotate = "ControlPlane.Credentials.Rotate";
        public const string StoresRead = "ControlPlane.Stores.Read";
        public const string StoresWrite = "ControlPlane.Stores.Write";
        public const string HealthRead = "ControlPlane.Health.Read";
        public const string ActionsRead = "ControlPlane.Actions.Read";
        public const string AuditRead = "ControlPlane.Audit.Read";
        public const string UsersRead = "ControlPlane.Users.Read";
        public const string UsersWrite = "ControlPlane.Users.Write";
        public const string RolesAssign = "ControlPlane.Roles.Assign";
        public const string PermissionsManage = "ControlPlane.Permissions.Manage";

        public static readonly IReadOnlyDictionary<string, string> PermissionByPolicy =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [NodesRead] = ControlPlanePermissions.NodesRead,
                [NodesWrite] = ControlPlanePermissions.NodesWrite,
                [CredentialsRotate] = ControlPlanePermissions.CredentialsRotate,
                [StoresRead] = ControlPlanePermissions.StoresRead,
                [StoresWrite] = ControlPlanePermissions.StoresWrite,
                [HealthRead] = ControlPlanePermissions.HealthRead,
                [ActionsRead] = ControlPlanePermissions.ActionsRead,
                [AuditRead] = ControlPlanePermissions.AuditRead,
                [UsersRead] = ControlPlanePermissions.UsersRead,
                [UsersWrite] = ControlPlanePermissions.UsersWrite,
                [RolesAssign] = ControlPlanePermissions.RolesAssign,
                [PermissionsManage] = ControlPlanePermissions.PermissionsManage
            };
    }
}
