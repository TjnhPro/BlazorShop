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
        public const string CommercePagesRead = "commerce.pages.read";
        public const string CommercePagesWrite = "commerce.pages.write";
        public const string CommerceSettingsRead = "commerce.settings.read";
        public const string CommerceSettingsWrite = "commerce.settings.write";
        public const string CommerceFeaturesRead = "commerce.features.read";
        public const string CommerceFeaturesWrite = "commerce.features.write";
        public const string CommerceProvidersRead = "commerce.providers.read";
        public const string CommerceProvidersWrite = "commerce.providers.write";
        public const string CommerceNavigationRead = "commerce.navigation.read";
        public const string CommerceNavigationWrite = "commerce.navigation.write";

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
            PermissionsManage,
            CommercePagesRead,
            CommercePagesWrite,
            CommerceSettingsRead,
            CommerceSettingsWrite,
            CommerceFeaturesRead,
            CommerceFeaturesWrite,
            CommerceProvidersRead,
            CommerceProvidersWrite,
            CommerceNavigationRead,
            CommerceNavigationWrite
        ];
    }
}
