namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Text;

    using BlazorShop.Application.ControlPlane.Users;
    using BlazorShop.Domain.Entities.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    public sealed class ControlPlaneUserManagementService : IControlPlaneUserManagementService
    {
        private const int DefaultLimit = 25;
        private const int MaxLimit = 100;

        private readonly ControlPlaneDbContext dbContext;

        public ControlPlaneUserManagementService(ControlPlaneDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<ControlPlaneUserListResponse> ListAsync(
            ControlPlaneUserListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var limit = Math.Clamp(query.Limit <= 0 ? DefaultLimit : query.Limit, 1, MaxLimit);
            var cursorId = DecodeCursor(query.Cursor);
            var users = BaseUserQuery();

            if (cursorId is not null)
            {
                users = users.Where(user => user.Id < cursorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = query.Status.Trim().ToLowerInvariant();
                users = users.Where(user => user.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.RoleKey))
            {
                var roleKey = query.RoleKey.Trim().ToLowerInvariant();
                users = users.Where(user => user.Roles.Any(userRole => userRole.Role!.Key == roleKey));
            }

            if (!string.IsNullOrWhiteSpace(query.PermissionKey))
            {
                var permissionKey = query.PermissionKey.Trim().ToLowerInvariant();
                users = users.Where(user =>
                    user.Roles.Any(userRole => userRole.Role!.Permissions
                        .Any(rolePermission => rolePermission.Permission!.Key == permissionKey))
                    || user.DirectPermissions.Any(userPermission => userPermission.Permission!.Key == permissionKey));
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLowerInvariant();
                users = users.Where(user =>
                    user.Email.ToLower().Contains(search)
                    || user.DisplayName.ToLower().Contains(search));
            }

            var fetchedUsers = await users
                .OrderByDescending(user => user.Id)
                .Take(limit + 1)
                .ToListAsync(cancellationToken);

            var pageUsers = fetchedUsers.Take(limit).ToArray();
            var identityRoleLookup = await LoadIdentityRoleLookupAsync(
                pageUsers.Select(user => user.IdentityUserId),
                cancellationToken);

            var items = pageUsers
                .Select(user => MapSummary(user, identityRoleLookup))
                .ToArray();
            var nextCursor = fetchedUsers.Count > limit ? EncodeCursor(fetchedUsers[limit - 1].Id) : null;

            return new ControlPlaneUserListResponse(items, nextCursor);
        }

        public async Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> GetAsync(
            Guid publicId,
            CancellationToken cancellationToken = default)
        {
            var user = await BaseUserQuery()
                .FirstOrDefaultAsync(user => user.PublicId == publicId, cancellationToken);

            if (user is null)
            {
                return NotFound();
            }

            var identityRoleLookup = await LoadIdentityRoleLookupAsync(
                [user.IdentityUserId],
                cancellationToken);

            return Succeeded(MapDetail(user, identityRoleLookup));
        }

        public async Task<ControlPlaneRoleCatalogResponse> ListRolesAsync(
            CancellationToken cancellationToken = default)
        {
            var roles = await this.dbContext.ControlPlaneRoles
                .AsNoTracking()
                .Include(role => role.Permissions)
                    .ThenInclude(rolePermission => rolePermission.Permission)
                .OrderBy(role => role.Name)
                .ToListAsync(cancellationToken);

            return new ControlPlaneRoleCatalogResponse(
                roles.Select(role => new ControlPlaneRoleCatalogItem(
                        role.Key,
                        role.Name,
                        role.Description,
                        role.IsSystem,
                        role.Permissions
                            .Select(rolePermission => rolePermission.Permission!.Key)
                            .Order(StringComparer.Ordinal)
                            .ToArray()))
                    .ToArray());
        }

        public async Task<ControlPlanePermissionCatalogResponse> ListPermissionsAsync(
            CancellationToken cancellationToken = default)
        {
            var permissions = await this.dbContext.Permissions
                .AsNoTracking()
                .OrderBy(permission => permission.Key)
                .Select(permission => new ControlPlanePermissionCatalogItem(permission.Key, permission.Description))
                .ToListAsync(cancellationToken);

            return new ControlPlanePermissionCatalogResponse(permissions);
        }

        private IQueryable<ControlPlaneAdminUser> BaseUserQuery()
        {
            return this.dbContext.AdminUsers
                .AsNoTracking()
                .AsSplitQuery()
                .Include(user => user.Roles)
                    .ThenInclude(userRole => userRole.Role)
                    .ThenInclude(role => role!.Permissions)
                    .ThenInclude(rolePermission => rolePermission.Permission)
                .Include(user => user.DirectPermissions)
                    .ThenInclude(userPermission => userPermission.Permission)
                .Where(user => user.DeletedAt == null);
        }

        private async Task<Dictionary<string, IReadOnlyList<string>>> LoadIdentityRoleLookupAsync(
            IEnumerable<string> identityUserIds,
            CancellationToken cancellationToken)
        {
            var ids = identityUserIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (ids.Length == 0)
            {
                return new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            }

            var rows = await (
                    from userRole in this.dbContext.UserRoles.AsNoTracking()
                    join role in this.dbContext.Roles.AsNoTracking()
                        on userRole.RoleId equals role.Id
                    where ids.Contains(userRole.UserId)
                    select new
                    {
                        userRole.UserId,
                        RoleName = role.Name ?? role.NormalizedName ?? role.Id
                    })
                .ToListAsync(cancellationToken);

            return rows
                .GroupBy(row => row.UserId, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<string>)group
                        .Select(row => row.RoleName)
                        .Order(StringComparer.OrdinalIgnoreCase)
                        .ToArray(),
                    StringComparer.Ordinal);
        }

        private static ControlPlaneUserSummary MapSummary(
            ControlPlaneAdminUser user,
            IReadOnlyDictionary<string, IReadOnlyList<string>> identityRoleLookup)
        {
            var effectivePermissions = BuildEffectivePermissions(user);

            return new ControlPlaneUserSummary(
                user.PublicId,
                user.Email,
                user.DisplayName,
                user.Status,
                ResolveIdentityRoles(user.IdentityUserId, identityRoleLookup),
                MapRoles(user),
                user.DirectPermissions
                    .Select(userPermission => userPermission.Permission!.Key)
                    .Order(StringComparer.Ordinal)
                    .ToArray(),
                effectivePermissions
                    .Select(permission => permission.Key)
                    .Order(StringComparer.Ordinal)
                    .ToArray(),
                user.LastLoginAt,
                user.CreatedAt,
                user.UpdatedAt);
        }

        private static ControlPlaneUserDetail MapDetail(
            ControlPlaneAdminUser user,
            IReadOnlyDictionary<string, IReadOnlyList<string>> identityRoleLookup)
        {
            return new ControlPlaneUserDetail(
                user.PublicId,
                user.IdentityUserId,
                user.Email,
                user.DisplayName,
                user.Status,
                user.StatusReason,
                user.StatusChangedAt,
                ResolveIdentityRoles(user.IdentityUserId, identityRoleLookup),
                MapRoles(user),
                user.DirectPermissions
                    .OrderBy(userPermission => userPermission.Permission!.Key)
                    .Select(userPermission => new ControlPlaneDirectPermissionGrant(
                        userPermission.Permission!.Key,
                        userPermission.Permission.Description,
                        userPermission.CreatedAt))
                    .ToArray(),
                BuildEffectivePermissions(user),
                user.LastLoginAt,
                user.CreatedAt,
                user.UpdatedAt);
        }

        private static IReadOnlyList<ControlPlaneUserRoleAssignment> MapRoles(ControlPlaneAdminUser user)
        {
            return user.Roles
                .OrderBy(userRole => userRole.Role!.Name)
                .Select(userRole => new ControlPlaneUserRoleAssignment(
                    userRole.Role!.Key,
                    userRole.Role.Name,
                    userRole.Role.Description))
                .ToArray();
        }

        private static IReadOnlyList<ControlPlaneEffectivePermission> BuildEffectivePermissions(ControlPlaneAdminUser user)
        {
            var permissions = new Dictionary<string, EffectivePermissionBuilder>(StringComparer.Ordinal);

            foreach (var userRole in user.Roles)
            {
                if (userRole.Role is null)
                {
                    continue;
                }

                foreach (var rolePermission in userRole.Role.Permissions)
                {
                    if (rolePermission.Permission is null)
                    {
                        continue;
                    }

                    var permission = GetOrAddPermission(permissions, rolePermission.Permission);
                    permission.Sources.Add($"role:{userRole.Role.Key}");
                }
            }

            foreach (var userPermission in user.DirectPermissions)
            {
                if (userPermission.Permission is null)
                {
                    continue;
                }

                var permission = GetOrAddPermission(permissions, userPermission.Permission);
                permission.Sources.Add("direct");
            }

            return permissions.Values
                .OrderBy(permission => permission.Key, StringComparer.Ordinal)
                .Select(permission => new ControlPlaneEffectivePermission(
                    permission.Key,
                    permission.Description,
                    permission.Sources.Order(StringComparer.Ordinal).ToArray()))
                .ToArray();
        }

        private static EffectivePermissionBuilder GetOrAddPermission(
            IDictionary<string, EffectivePermissionBuilder> permissions,
            ControlPlanePermission permission)
        {
            if (!permissions.TryGetValue(permission.Key, out var builder))
            {
                builder = new EffectivePermissionBuilder(permission.Key, permission.Description);
                permissions.Add(permission.Key, builder);
            }

            return builder;
        }

        private static IReadOnlyList<string> ResolveIdentityRoles(
            string identityUserId,
            IReadOnlyDictionary<string, IReadOnlyList<string>> identityRoleLookup)
        {
            return identityRoleLookup.TryGetValue(identityUserId, out var roles) ? roles : [];
        }

        private static string? EncodeCursor(long id)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(id.ToString()));
        }

        private static long? DecodeCursor(string? cursor)
        {
            if (string.IsNullOrWhiteSpace(cursor))
            {
                return null;
            }

            try
            {
                var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
                return long.TryParse(raw, out var id) && id > 0 ? id : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static ControlPlaneUserOperationResult<ControlPlaneUserDetail> Succeeded(ControlPlaneUserDetail payload)
        {
            return new ControlPlaneUserOperationResult<ControlPlaneUserDetail>(true, Payload: payload);
        }

        private static ControlPlaneUserOperationResult<ControlPlaneUserDetail> NotFound()
        {
            return new ControlPlaneUserOperationResult<ControlPlaneUserDetail>(
                false,
                "Control Plane user was not found.",
                Failure: ControlPlaneUserOperationFailure.NotFound);
        }

        private sealed class EffectivePermissionBuilder
        {
            public EffectivePermissionBuilder(string key, string? description)
            {
                Key = key;
                Description = description;
            }

            public string Key { get; }

            public string? Description { get; }

            public ISet<string> Sources { get; } = new SortedSet<string>(StringComparer.Ordinal);
        }
    }
}
