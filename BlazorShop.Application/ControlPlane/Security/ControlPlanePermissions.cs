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

        public static readonly string[] All =
        [
            NodesRead,
            NodesWrite,
            CredentialsRotate,
            StoresRead,
            StoresWrite,
            HealthRead,
            ActionsRead,
            AuditRead
        ];
    }
}
