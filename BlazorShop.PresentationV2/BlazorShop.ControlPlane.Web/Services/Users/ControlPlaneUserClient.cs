namespace BlazorShop.ControlPlane.Web.Services.Users
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.ControlPlane.Web.Services.Common;
    using BlazorShop.Web.Shared.Helper.Contracts;

    public interface IControlPlaneUserClient
    {
        Task<UserListResponse> ListAsync(
            string? search,
            string? status,
            string? roleKey,
            string? permissionKey,
            string? cursor,
            CancellationToken cancellationToken = default);

        Task<UserDetail?> GetAsync(Guid publicId, CancellationToken cancellationToken = default);

        Task<RoleCatalogResponse> ListRolesAsync(CancellationToken cancellationToken = default);

        Task<PermissionCatalogResponse> ListPermissionsAsync(CancellationToken cancellationToken = default);

        Task<CreateUserResult> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

        Task<UserMutationResult> UpdateAsync(Guid publicId, UpdateUserRequest request, CancellationToken cancellationToken = default);

        Task<UserMutationResult> DisableAsync(Guid publicId, ChangeUserStatusRequest request, CancellationToken cancellationToken = default);

        Task<UserMutationResult> EnableAsync(Guid publicId, ChangeUserStatusRequest request, CancellationToken cancellationToken = default);

        Task<UserMutationResult> AssignRoleAsync(Guid publicId, AssignRoleRequest request, CancellationToken cancellationToken = default);

        Task<UserMutationResult> RemoveRoleAsync(Guid publicId, string roleKey, CancellationToken cancellationToken = default);

        Task<UserMutationResult> AssignPermissionAsync(Guid publicId, AssignPermissionRequest request, CancellationToken cancellationToken = default);

        Task<UserMutationResult> RemovePermissionAsync(Guid publicId, string permissionKey, CancellationToken cancellationToken = default);
    }

    public sealed class ControlPlaneUserClient : IControlPlaneUserClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly IHttpClientHelper httpClientHelper;
        private readonly IControlPlaneApiClient apiClient;

        public ControlPlaneUserClient(IHttpClientHelper httpClientHelper, IControlPlaneApiClient apiClient)
        {
            this.httpClientHelper = httpClientHelper;
            this.apiClient = apiClient;
        }

        public async Task<UserListResponse> ListAsync(
            string? search,
            string? status,
            string? roleKey,
            string? permissionKey,
            string? cursor,
            CancellationToken cancellationToken = default)
        {
            var query = new List<string> { "limit=25" };

            AddQuery(query, "search", search);
            AddQuery(query, "status", status);
            AddQuery(query, "roleKey", roleKey);
            AddQuery(query, "permissionKey", permissionKey);
            AddQuery(query, "cursor", cursor);

            var result = await this.apiClient.GetPrivateAsync<UserListResponse>(
                $"api/control-plane/users?{string.Join("&", query)}",
                "Unable to load users.",
                cancellationToken);

            if (result.Success)
            {
                return result.Data ?? new UserListResponse([], null);
            }

            throw new InvalidOperationException(result.Message);
        }

        public async Task<UserDetail?> GetAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.GetPrivateAsync<UserDetail>(
                $"api/control-plane/users/{publicId}",
                "Unable to load user details.",
                cancellationToken);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (result.Success)
            {
                return result.Data;
            }

            throw new InvalidOperationException(result.Message);
        }

        public async Task<RoleCatalogResponse> ListRolesAsync(CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.GetPrivateAsync<RoleCatalogResponse>(
                "api/control-plane/users/roles",
                "Unable to load roles.",
                cancellationToken);

            if (result.Success)
            {
                return result.Data ?? new RoleCatalogResponse([]);
            }

            throw new InvalidOperationException(result.Message);
        }

        public async Task<PermissionCatalogResponse> ListPermissionsAsync(CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.GetPrivateAsync<PermissionCatalogResponse>(
                "api/control-plane/users/permissions",
                "Unable to load permissions.",
                cancellationToken);

            if (result.Success)
            {
                return result.Data ?? new PermissionCatalogResponse([]);
            }

            throw new InvalidOperationException(result.Message);
        }

        public async Task<CreateUserResult> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<CreateUserRequest, CreateUserResponse>(
                "api/control-plane/users",
                request,
                "Unable to create user.",
                cancellationToken);

            if (result.Success)
            {
                return result.Data is null
                    ? new CreateUserResult(false, "Invalid create user response.")
                    : new CreateUserResult(true, result.Message, result.Data);
            }

            return new CreateUserResult(false, result.Message);
        }

        public async Task<UserMutationResult> UpdateAsync(Guid publicId, UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PutPrivateAsync<UpdateUserRequest, UserDetail>(
                $"api/control-plane/users/{publicId}",
                request,
                "Unable to update user.",
                cancellationToken);

            return new UserMutationResult(result.Success, result.Message, result.Data);
        }

        public async Task<UserMutationResult> DisableAsync(Guid publicId, ChangeUserStatusRequest request, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<ChangeUserStatusRequest, UserDetail>(
                $"api/control-plane/users/{publicId}/disable",
                request,
                "Unable to disable user.",
                cancellationToken);

            return new UserMutationResult(result.Success, result.Message, result.Data);
        }

        public async Task<UserMutationResult> EnableAsync(Guid publicId, ChangeUserStatusRequest request, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<ChangeUserStatusRequest, UserDetail>(
                $"api/control-plane/users/{publicId}/enable",
                request,
                "Unable to enable user.",
                cancellationToken);

            return new UserMutationResult(result.Success, result.Message, result.Data);
        }

        public async Task<UserMutationResult> AssignRoleAsync(Guid publicId, AssignRoleRequest request, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<AssignRoleRequest, UserDetail>(
                $"api/control-plane/users/{publicId}/roles",
                request,
                "Unable to assign role.",
                cancellationToken);

            return new UserMutationResult(result.Success, result.Message, result.Data);
        }

        public async Task<UserMutationResult> RemoveRoleAsync(Guid publicId, string roleKey, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.DeletePrivateAsync<UserDetail>(
                $"api/control-plane/users/{publicId}/roles/{Uri.EscapeDataString(roleKey)}",
                "Unable to remove role.",
                cancellationToken);

            return new UserMutationResult(result.Success, result.Message, result.Data);
        }

        public async Task<UserMutationResult> AssignPermissionAsync(Guid publicId, AssignPermissionRequest request, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.PostPrivateAsync<AssignPermissionRequest, UserDetail>(
                $"api/control-plane/users/{publicId}/permissions",
                request,
                "Unable to assign permission.",
                cancellationToken);

            return new UserMutationResult(result.Success, result.Message, result.Data);
        }

        public async Task<UserMutationResult> RemovePermissionAsync(Guid publicId, string permissionKey, CancellationToken cancellationToken = default)
        {
            var result = await this.apiClient.DeletePrivateAsync<UserDetail>(
                $"api/control-plane/users/{publicId}/permissions/{Uri.EscapeDataString(permissionKey)}",
                "Unable to remove permission.",
                cancellationToken);

            return new UserMutationResult(result.Success, result.Message, result.Data);
        }

        private static void AddQuery(ICollection<string> query, string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                query.Add($"{key}={Uri.EscapeDataString(value)}");
            }
        }

        private static async Task<string> ResolveErrorMessageAsync(HttpResponseMessage response, string defaultMessage)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return "Sign in with a Control Plane account that has access to users.";
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return "Your Control Plane account does not have permission for User Management.";
            }

            if (response.Content is null)
            {
                return defaultMessage;
            }

            try
            {
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (document.RootElement.TryGetProperty("message", out var messageElement)
                    && messageElement.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(messageElement.GetString()))
                {
                    return messageElement.GetString()!;
                }
            }
            catch (JsonException)
            {
            }

            return defaultMessage;
        }
    }

    public sealed record UserListResponse(IReadOnlyList<UserSummary> Items, string? NextCursor);

    public sealed record UserSummary(
        Guid PublicId,
        string Email,
        string DisplayName,
        string Status,
        IReadOnlyList<string> IdentityRoles,
        IReadOnlyList<UserRoleAssignment> ControlPlaneRoles,
        IReadOnlyList<string> DirectPermissionKeys,
        IReadOnlyList<string> EffectivePermissionKeys,
        DateTimeOffset? LastLoginAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public sealed record UserDetail(
        Guid PublicId,
        string IdentityUserId,
        string Email,
        string DisplayName,
        string Status,
        string? StatusReason,
        DateTimeOffset? StatusChangedAt,
        IReadOnlyList<string> IdentityRoles,
        IReadOnlyList<UserRoleAssignment> ControlPlaneRoles,
        IReadOnlyList<DirectPermissionGrant> DirectPermissions,
        IReadOnlyList<EffectivePermission> EffectivePermissions,
        DateTimeOffset? LastLoginAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public sealed record UserRoleAssignment(string Key, string Name, string? Description);

    public sealed record DirectPermissionGrant(string Key, string? Description, DateTimeOffset CreatedAt);

    public sealed record EffectivePermission(string Key, string? Description, IReadOnlyList<string> Sources);

    public sealed record RoleCatalogResponse(IReadOnlyList<RoleCatalogItem> Items);

    public sealed record RoleCatalogItem(
        string Key,
        string Name,
        string? Description,
        bool IsSystem,
        IReadOnlyList<string> PermissionKeys);

    public sealed record PermissionCatalogResponse(IReadOnlyList<PermissionCatalogItem> Items);

    public sealed record PermissionCatalogItem(string Key, string? Description);

    public sealed record CreateUserRequest(
        string Email,
        string DisplayName,
        string? IdentityRole,
        IReadOnlyList<string> ControlPlaneRoleKeys,
        IReadOnlyList<string> DirectPermissionKeys,
        string? TemporaryPassword);

    public sealed record CreateUserResponse(UserDetail User, string? TemporaryPassword);

    public sealed record UpdateUserRequest(string DisplayName);

    public sealed record ChangeUserStatusRequest(string? Reason);

    public sealed record AssignRoleRequest(string RoleKey);

    public sealed record AssignPermissionRequest(string PermissionKey);

    public sealed record CreateUserResult(bool Success, string? Message = null, CreateUserResponse? Payload = null);

    public sealed record UserMutationResult(bool Success, string? Message = null, UserDetail? User = null);
}
