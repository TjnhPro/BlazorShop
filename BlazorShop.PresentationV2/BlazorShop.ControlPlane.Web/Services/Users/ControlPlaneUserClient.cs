namespace BlazorShop.ControlPlane.Web.Services.Users
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

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

        public ControlPlaneUserClient(IHttpClientHelper httpClientHelper)
        {
            this.httpClientHelper = httpClientHelper;
        }

        public async Task<UserListResponse> ListAsync(
            string? search,
            string? status,
            string? roleKey,
            string? permissionKey,
            string? cursor,
            CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            var query = new List<string> { "limit=25" };

            AddQuery(query, "search", search);
            AddQuery(query, "status", status);
            AddQuery(query, "roleKey", roleKey);
            AddQuery(query, "permissionKey", permissionKey);
            AddQuery(query, "cursor", cursor);

            using var response = await client.GetAsync($"api/control-plane/users?{string.Join("&", query)}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserListResponse>(SerializerOptions, cancellationToken)
                       ?? new UserListResponse([], null);
            }

            throw new InvalidOperationException(await ResolveErrorMessageAsync(response, "Unable to load users."));
        }

        public async Task<UserDetail?> GetAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.GetAsync($"api/control-plane/users/{publicId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserDetail>(SerializerOptions, cancellationToken);
            }

            throw new InvalidOperationException(await ResolveErrorMessageAsync(response, "Unable to load user details."));
        }

        public async Task<RoleCatalogResponse> ListRolesAsync(CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.GetAsync("api/control-plane/users/roles", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<RoleCatalogResponse>(SerializerOptions, cancellationToken)
                       ?? new RoleCatalogResponse([]);
            }

            throw new InvalidOperationException(await ResolveErrorMessageAsync(response, "Unable to load roles."));
        }

        public async Task<PermissionCatalogResponse> ListPermissionsAsync(CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.GetAsync("api/control-plane/users/permissions", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PermissionCatalogResponse>(SerializerOptions, cancellationToken)
                       ?? new PermissionCatalogResponse([]);
            }

            throw new InvalidOperationException(await ResolveErrorMessageAsync(response, "Unable to load permissions."));
        }

        public async Task<CreateUserResult> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsJsonAsync("api/control-plane/users", request, SerializerOptions, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync<CreateUserResponse>(SerializerOptions, cancellationToken);
                return payload is null
                    ? new CreateUserResult(false, "Invalid create user response.")
                    : new CreateUserResult(true, Payload: payload);
            }

            return new CreateUserResult(false, await ResolveErrorMessageAsync(response, "Unable to create user."));
        }

        public async Task<UserMutationResult> UpdateAsync(Guid publicId, UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PutAsJsonAsync($"api/control-plane/users/{publicId}", request, SerializerOptions, cancellationToken);
            return await ToMutationResultAsync(response, "Unable to update user.", cancellationToken);
        }

        public async Task<UserMutationResult> DisableAsync(Guid publicId, ChangeUserStatusRequest request, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsJsonAsync($"api/control-plane/users/{publicId}/disable", request, SerializerOptions, cancellationToken);
            return await ToMutationResultAsync(response, "Unable to disable user.", cancellationToken);
        }

        public async Task<UserMutationResult> EnableAsync(Guid publicId, ChangeUserStatusRequest request, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsJsonAsync($"api/control-plane/users/{publicId}/enable", request, SerializerOptions, cancellationToken);
            return await ToMutationResultAsync(response, "Unable to enable user.", cancellationToken);
        }

        public async Task<UserMutationResult> AssignRoleAsync(Guid publicId, AssignRoleRequest request, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsJsonAsync($"api/control-plane/users/{publicId}/roles", request, SerializerOptions, cancellationToken);
            return await ToMutationResultAsync(response, "Unable to assign role.", cancellationToken);
        }

        public async Task<UserMutationResult> RemoveRoleAsync(Guid publicId, string roleKey, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.DeleteAsync($"api/control-plane/users/{publicId}/roles/{Uri.EscapeDataString(roleKey)}", cancellationToken);
            return await ToMutationResultAsync(response, "Unable to remove role.", cancellationToken);
        }

        public async Task<UserMutationResult> AssignPermissionAsync(Guid publicId, AssignPermissionRequest request, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.PostAsJsonAsync($"api/control-plane/users/{publicId}/permissions", request, SerializerOptions, cancellationToken);
            return await ToMutationResultAsync(response, "Unable to assign permission.", cancellationToken);
        }

        public async Task<UserMutationResult> RemovePermissionAsync(Guid publicId, string permissionKey, CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.DeleteAsync($"api/control-plane/users/{publicId}/permissions/{Uri.EscapeDataString(permissionKey)}", cancellationToken);
            return await ToMutationResultAsync(response, "Unable to remove permission.", cancellationToken);
        }

        private static async Task<UserMutationResult> ToMutationResultAsync(
            HttpResponseMessage response,
            string defaultErrorMessage,
            CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<UserDetail>(SerializerOptions, cancellationToken);
                return new UserMutationResult(true, User: user);
            }

            return new UserMutationResult(false, await ResolveErrorMessageAsync(response, defaultErrorMessage));
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
