namespace BlazorShop.Application.ControlPlane.Security
{
    public static class ControlPlanePermissions
    {
        public const string NodesRead = "nodes.read";
        public const string NodesWrite = "nodes.write";
        public const string CredentialsRotate = "credentials.rotate";
        public const string StoresRead = "stores.read";
        public const string StoresWrite = "stores.write";
        public const string HealthRead = "health.read";
        public const string ActionsRead = "actions.read";
        public const string AuditRead = "audit.read";
        public const string UsersRead = "users.read";
        public const string UsersWrite = "users.write";
        public const string RolesAssign = "roles.assign";
        public const string PermissionsManage = "permissions.manage";

        public static readonly string[] All =
        [
            NodesRead,
            NodesWrite,
            CredentialsRotate,
            StoresRead,
            StoresWrite,
            HealthRead,
            ActionsRead,
            AuditRead,
            UsersRead,
            UsersWrite,
            RolesAssign,
            PermissionsManage
        ];
    }
}
