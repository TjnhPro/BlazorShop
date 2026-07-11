namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Security.Cryptography;

    using BlazorShop.Application.ControlPlane.Common;
    using BlazorShop.Application.ControlPlane.Users;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;

    public sealed class ControlPlaneUserManagementService : IControlPlaneUserManagementService
    {
        private const string DefaultIdentityRole = "User";
        private const string PlatformOwnerRoleKey = "platform_owner";

        private readonly ControlPlaneDbContext dbContext;
        private readonly UserManager<AppUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;

        public ControlPlaneUserManagementService(
            ControlPlaneDbContext dbContext,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        public async Task<ControlPlaneUserListResponse> ListAsync(
            ControlPlaneUserListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var users = BaseUserQuery();

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

            var page = ControlPlanePaging.Normalize(query.PageNumber, query.PageSize);
            var totalCount = await users.CountAsync(cancellationToken);
            var fetchedUsers = await users
                .OrderByDescending(user => user.Id)
                .Skip(page.Skip)
                .Take(page.PageSize)
                .ToListAsync(cancellationToken);

            var identityRoleLookup = await LoadIdentityRoleLookupAsync(
                fetchedUsers.Select(user => user.IdentityUserId),
                cancellationToken);

            var items = fetchedUsers
                .Select(user => MapSummary(user, identityRoleLookup))
                .ToArray();

            return new ControlPlaneUserListResponse(
                items,
                totalCount,
                page.PageNumber,
                page.PageSize,
                ControlPlanePaging.GetTotalPages(totalCount, page.PageSize));
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

        public async Task<ControlPlaneUserOperationResult<CreateControlPlaneUserResponse>> CreateAsync(
            CreateControlPlaneUserRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default)
        {
            var validation = ValidateCreateRequest(request);
            if (validation is not null)
            {
                return Failed<CreateControlPlaneUserResponse>(validation, ControlPlaneUserOperationFailure.Validation);
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var displayName = request.DisplayName.Trim();
            var identityRole = string.IsNullOrWhiteSpace(request.IdentityRole)
                ? DefaultIdentityRole
                : request.IdentityRole.Trim();
            var controlPlaneRoleKeys = NormalizeKeys(request.ControlPlaneRoleKeys);
            var directPermissionKeys = NormalizeKeys(request.DirectPermissionKeys);

            if (!IsAllowedIdentityRole(identityRole) || !await this.roleManager.RoleExistsAsync(identityRole))
            {
                return Failed<CreateControlPlaneUserResponse>("Identity role is invalid.", ControlPlaneUserOperationFailure.Validation);
            }

            if (await this.userManager.FindByEmailAsync(normalizedEmail) is not null
                || await this.dbContext.AdminUsers.AnyAsync(user => user.Email == normalizedEmail && user.DeletedAt == null, cancellationToken))
            {
                return Failed<CreateControlPlaneUserResponse>("A Control Plane user with this email already exists.", ControlPlaneUserOperationFailure.Conflict);
            }

            var roles = await LoadRolesByKeyAsync(controlPlaneRoleKeys, cancellationToken);
            if (roles.Count != controlPlaneRoleKeys.Count)
            {
                return Failed<CreateControlPlaneUserResponse>("One or more Control Plane roles are invalid.", ControlPlaneUserOperationFailure.Validation);
            }

            var permissions = await LoadPermissionsByKeyAsync(directPermissionKeys, cancellationToken);
            if (permissions.Count != directPermissionKeys.Count)
            {
                return Failed<CreateControlPlaneUserResponse>("One or more Control Plane permissions are invalid.", ControlPlaneUserOperationFailure.Validation);
            }

            var actorAdminUserId = await ResolveActorAdminUserIdAsync(actor, cancellationToken);
            var temporaryPassword = string.IsNullOrWhiteSpace(request.TemporaryPassword)
                ? GenerateTemporaryPassword()
                : request.TemporaryPassword;
            var now = DateTimeOffset.UtcNow;

            var strategy = this.dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await this.dbContext.Database.BeginTransactionAsync(cancellationToken);

                var appUser = new AppUser
                {
                    Email = normalizedEmail,
                    UserName = normalizedEmail,
                    FullName = displayName,
                    EmailConfirmed = true,
                    LockoutEnabled = true,
                    RequirePasswordChange = true,
                    CreatedOn = DateTime.UtcNow
                };

                var createResult = await this.userManager.CreateAsync(appUser, temporaryPassword);
                if (!createResult.Succeeded)
                {
                    return Failed<CreateControlPlaneUserResponse>(
                        FormatIdentityErrors(createResult),
                        ControlPlaneUserOperationFailure.Validation);
                }

                var roleResult = await this.userManager.AddToRoleAsync(appUser, identityRole);
                if (!roleResult.Succeeded)
                {
                    return Failed<CreateControlPlaneUserResponse>(
                        FormatIdentityErrors(roleResult),
                        ControlPlaneUserOperationFailure.Validation);
                }

                var profile = new ControlPlaneAdminUser
                {
                    IdentityUserId = appUser.Id,
                    Email = normalizedEmail,
                    DisplayName = displayName,
                    Status = "active",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                foreach (var role in roles)
                {
                    profile.Roles.Add(new ControlPlaneAdminUserRole
                    {
                        RoleId = role.Id,
                        CreatedAt = now
                    });
                }

                foreach (var permission in permissions)
                {
                    profile.DirectPermissions.Add(new ControlPlaneAdminUserPermission
                    {
                        PermissionId = permission.Id,
                        CreatedAt = now,
                        CreatedByAdminUserId = actorAdminUserId
                    });
                }

                this.dbContext.AdminUsers.Add(profile);
                await this.dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var created = await this.GetAsync(profile.PublicId, cancellationToken);
                return created.Success && created.Payload is not null
                    ? Succeeded(new CreateControlPlaneUserResponse(created.Payload, string.IsNullOrWhiteSpace(request.TemporaryPassword) ? temporaryPassword : null))
                    : Failed<CreateControlPlaneUserResponse>("Control Plane user was created but could not be loaded.", ControlPlaneUserOperationFailure.Conflict);
            });
        }

        public async Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> UpdateAsync(
            Guid publicId,
            UpdateControlPlaneUserRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return Failed<ControlPlaneUserDetail>("Display name is required.", ControlPlaneUserOperationFailure.Validation);
            }

            if (request.DisplayName.Trim().Length > 160)
            {
                return Failed<ControlPlaneUserDetail>("Display name must be 160 characters or fewer.", ControlPlaneUserOperationFailure.Validation);
            }

            var user = await LoadUserForMutationAsync(publicId, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }

            var displayName = request.DisplayName.Trim();
            user.DisplayName = displayName;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            var appUser = await this.userManager.FindByIdAsync(user.IdentityUserId);
            if (appUser is not null)
            {
                appUser.FullName = displayName;
                var updateResult = await this.userManager.UpdateAsync(appUser);
                if (!updateResult.Succeeded)
                {
                    return Failed<ControlPlaneUserDetail>(
                        FormatIdentityErrors(updateResult),
                        ControlPlaneUserOperationFailure.Validation);
                }
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);
            return await ReloadDetailAsync(publicId, cancellationToken);
        }

        public async Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> DisableAsync(
            Guid publicId,
            ChangeControlPlaneUserStatusRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default)
        {
            var user = await LoadUserForMutationAsync(publicId, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(actor.IdentityUserId) && user.IdentityUserId == actor.IdentityUserId)
            {
                return Failed<ControlPlaneUserDetail>("You cannot disable your own Control Plane account.", ControlPlaneUserOperationFailure.Conflict);
            }

            if (user.Roles.Any(userRole => userRole.Role?.Key == PlatformOwnerRoleKey)
                && await CountActivePlatformOwnersAsync(cancellationToken) <= 1)
            {
                return Failed<ControlPlaneUserDetail>("Cannot disable the last active platform owner.", ControlPlaneUserOperationFailure.Conflict);
            }

            if (user.Status == "disabled")
            {
                return await ReloadDetailAsync(publicId, cancellationToken);
            }

            var now = DateTimeOffset.UtcNow;
            var actorAdminUserId = await ResolveActorAdminUserIdAsync(actor, cancellationToken);
            user.Status = "disabled";
            user.StatusChangedAt = now;
            user.StatusChangedByAdminUserId = actorAdminUserId;
            user.StatusReason = NormalizeOptionalText(request?.Reason);
            user.UpdatedAt = now;

            var nowUtc = DateTime.UtcNow;
            var activeTokens = await this.dbContext.RefreshTokens
                .Where(token => token.UserId == user.IdentityUserId && token.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.RevokedAtUtc = nowUtc;
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);
            return await ReloadDetailAsync(publicId, cancellationToken);
        }

        public async Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> EnableAsync(
            Guid publicId,
            ChangeControlPlaneUserStatusRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default)
        {
            var user = await LoadUserForMutationAsync(publicId, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }

            if (user.Status == "active")
            {
                return await ReloadDetailAsync(publicId, cancellationToken);
            }

            var now = DateTimeOffset.UtcNow;
            var actorAdminUserId = await ResolveActorAdminUserIdAsync(actor, cancellationToken);
            user.Status = "active";
            user.StatusChangedAt = now;
            user.StatusChangedByAdminUserId = actorAdminUserId;
            user.StatusReason = NormalizeOptionalText(request?.Reason);
            user.UpdatedAt = now;

            await this.dbContext.SaveChangesAsync(cancellationToken);
            return await ReloadDetailAsync(publicId, cancellationToken);
        }

        public async Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> AssignRoleAsync(
            Guid publicId,
            AssignControlPlaneRoleRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.RoleKey))
            {
                return Failed<ControlPlaneUserDetail>("Role key is required.", ControlPlaneUserOperationFailure.Validation);
            }

            var roleKey = request.RoleKey.Trim().ToLowerInvariant();
            var user = await LoadUserForMutationAsync(publicId, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }

            var role = await this.dbContext.ControlPlaneRoles
                .FirstOrDefaultAsync(role => role.Key == roleKey, cancellationToken);

            if (role is null)
            {
                return Failed<ControlPlaneUserDetail>("Control Plane role is invalid.", ControlPlaneUserOperationFailure.Validation);
            }

            if (user.Roles.Any(userRole => userRole.RoleId == role.Id))
            {
                return await ReloadDetailAsync(publicId, cancellationToken);
            }

            user.Roles.Add(new ControlPlaneAdminUserRole
            {
                AdminUserId = user.Id,
                RoleId = role.Id,
                CreatedAt = DateTimeOffset.UtcNow
            });
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await this.dbContext.SaveChangesAsync(cancellationToken);
            return await ReloadDetailAsync(publicId, cancellationToken);
        }

        public async Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> RemoveRoleAsync(
            Guid publicId,
            string roleKey,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(roleKey))
            {
                return Failed<ControlPlaneUserDetail>("Role key is required.", ControlPlaneUserOperationFailure.Validation);
            }

            var normalizedRoleKey = roleKey.Trim().ToLowerInvariant();
            var user = await LoadUserForMutationAsync(publicId, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }

            var userRole = user.Roles.FirstOrDefault(role => role.Role?.Key == normalizedRoleKey);
            if (userRole is null)
            {
                return await ReloadDetailAsync(publicId, cancellationToken);
            }

            if (normalizedRoleKey == PlatformOwnerRoleKey && await CountActivePlatformOwnersAsync(cancellationToken) <= 1)
            {
                return Failed<ControlPlaneUserDetail>("Cannot remove the last active platform owner.", ControlPlaneUserOperationFailure.Conflict);
            }

            this.dbContext.Remove(userRole);
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await this.dbContext.SaveChangesAsync(cancellationToken);
            return await ReloadDetailAsync(publicId, cancellationToken);
        }

        public async Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> AssignPermissionAsync(
            Guid publicId,
            AssignControlPlanePermissionRequest request,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.PermissionKey))
            {
                return Failed<ControlPlaneUserDetail>("Permission key is required.", ControlPlaneUserOperationFailure.Validation);
            }

            var permissionKey = request.PermissionKey.Trim().ToLowerInvariant();
            var user = await LoadUserForMutationAsync(publicId, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }

            var permission = await this.dbContext.Permissions
                .FirstOrDefaultAsync(permission => permission.Key == permissionKey, cancellationToken);

            if (permission is null)
            {
                return Failed<ControlPlaneUserDetail>("Control Plane permission is invalid.", ControlPlaneUserOperationFailure.Validation);
            }

            if (user.DirectPermissions.Any(userPermission => userPermission.PermissionId == permission.Id))
            {
                return await ReloadDetailAsync(publicId, cancellationToken);
            }

            var actorAdminUserId = await ResolveActorAdminUserIdAsync(actor, cancellationToken);
            user.DirectPermissions.Add(new ControlPlaneAdminUserPermission
            {
                AdminUserId = user.Id,
                PermissionId = permission.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByAdminUserId = actorAdminUserId
            });
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await this.dbContext.SaveChangesAsync(cancellationToken);
            return await ReloadDetailAsync(publicId, cancellationToken);
        }

        public async Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> RemovePermissionAsync(
            Guid publicId,
            string permissionKey,
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(permissionKey))
            {
                return Failed<ControlPlaneUserDetail>("Permission key is required.", ControlPlaneUserOperationFailure.Validation);
            }

            var normalizedPermissionKey = permissionKey.Trim().ToLowerInvariant();
            var user = await LoadUserForMutationAsync(publicId, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }

            var directPermission = user.DirectPermissions.FirstOrDefault(userPermission => userPermission.Permission?.Key == normalizedPermissionKey);
            if (directPermission is null)
            {
                return await ReloadDetailAsync(publicId, cancellationToken);
            }

            if (IsSelf(actor, user)
                && IsCriticalAccessPermission(normalizedPermissionKey)
                && !HasRolePermission(user, normalizedPermissionKey))
            {
                return Failed<ControlPlaneUserDetail>(
                    "Cannot remove your own final role or permission management access.",
                    ControlPlaneUserOperationFailure.Conflict);
            }

            this.dbContext.Remove(directPermission);
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await this.dbContext.SaveChangesAsync(cancellationToken);
            return await ReloadDetailAsync(publicId, cancellationToken);
        }

        public async Task<ControlPlaneRoleCatalogResponse> GetRoleCatalogAsync(
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

        public async Task<ControlPlanePermissionCatalogResponse> GetPermissionCatalogAsync(
            CancellationToken cancellationToken = default)
        {
            var permissions = await this.dbContext.Permissions
                .AsNoTracking()
                .OrderBy(permission => permission.Key)
                .Select(permission => new ControlPlanePermissionCatalogItem(permission.Key, permission.Description))
                .ToListAsync(cancellationToken);

            return new ControlPlanePermissionCatalogResponse(permissions);
        }

        private async Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> ReloadDetailAsync(
            Guid publicId,
            CancellationToken cancellationToken)
        {
            return await this.GetAsync(publicId, cancellationToken);
        }

        private async Task<ControlPlaneAdminUser?> LoadUserForMutationAsync(
            Guid publicId,
            CancellationToken cancellationToken)
        {
            return await this.dbContext.AdminUsers
                .AsSplitQuery()
                .Include(user => user.Roles)
                    .ThenInclude(userRole => userRole.Role)
                    .ThenInclude(role => role!.Permissions)
                    .ThenInclude(rolePermission => rolePermission.Permission)
                .Include(user => user.DirectPermissions)
                    .ThenInclude(userPermission => userPermission.Permission)
                .FirstOrDefaultAsync(user => user.PublicId == publicId && user.DeletedAt == null, cancellationToken);
        }

        private async Task<IReadOnlyList<ControlPlaneRole>> LoadRolesByKeyAsync(
            IReadOnlyCollection<string> roleKeys,
            CancellationToken cancellationToken)
        {
            if (roleKeys.Count == 0)
            {
                return [];
            }

            return await this.dbContext.ControlPlaneRoles
                .Where(role => roleKeys.Contains(role.Key))
                .ToListAsync(cancellationToken);
        }

        private async Task<IReadOnlyList<ControlPlanePermission>> LoadPermissionsByKeyAsync(
            IReadOnlyCollection<string> permissionKeys,
            CancellationToken cancellationToken)
        {
            if (permissionKeys.Count == 0)
            {
                return [];
            }

            return await this.dbContext.Permissions
                .Where(permission => permissionKeys.Contains(permission.Key))
                .ToListAsync(cancellationToken);
        }

        private async Task<long?> ResolveActorAdminUserIdAsync(
            ControlPlaneUserActor actor,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(actor.IdentityUserId))
            {
                return null;
            }

            return await this.dbContext.AdminUsers
                .AsNoTracking()
                .Where(user => user.IdentityUserId == actor.IdentityUserId && user.DeletedAt == null)
                .Select(user => (long?)user.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<int> CountActivePlatformOwnersAsync(CancellationToken cancellationToken)
        {
            return await this.dbContext.AdminUsers
                .AsNoTracking()
                .CountAsync(
                    user => user.Status == "active"
                            && user.DeletedAt == null
                            && user.Roles.Any(userRole => userRole.Role!.Key == PlatformOwnerRoleKey),
                    cancellationToken);
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

        private static string? ValidateCreateRequest(CreateControlPlaneUserRequest request)
        {
            if (request is null)
            {
                return "Request body is required.";
            }

            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@', StringComparison.Ordinal))
            {
                return "Email is required.";
            }

            if (request.Email.Trim().Length > 256)
            {
                return "Email must be 256 characters or fewer.";
            }

            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return "Display name is required.";
            }

            if (request.DisplayName.Trim().Length > 160)
            {
                return "Display name must be 160 characters or fewer.";
            }

            return null;
        }

        private static IReadOnlyList<string> NormalizeKeys(IReadOnlyList<string>? keys)
        {
            return (keys ?? [])
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim().ToLowerInvariant())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static bool IsAllowedIdentityRole(string identityRole)
        {
            return string.Equals(identityRole, "Admin", StringComparison.Ordinal)
                   || string.Equals(identityRole, "User", StringComparison.Ordinal);
        }

        private static bool IsSelf(ControlPlaneUserActor actor, ControlPlaneAdminUser user)
        {
            return !string.IsNullOrWhiteSpace(actor.IdentityUserId)
                   && string.Equals(actor.IdentityUserId, user.IdentityUserId, StringComparison.Ordinal);
        }

        private static bool IsCriticalAccessPermission(string permissionKey)
        {
            return string.Equals(permissionKey, "roles.assign", StringComparison.Ordinal)
                   || string.Equals(permissionKey, "permissions.manage", StringComparison.Ordinal);
        }

        private static bool HasRolePermission(ControlPlaneAdminUser user, string permissionKey)
        {
            return user.Roles.Any(userRole => userRole.Role!.Permissions
                .Any(rolePermission => rolePermission.Permission!.Key == permissionKey));
        }

        private static string GenerateTemporaryPassword()
        {
            Span<byte> bytes = stackalloc byte[12];
            RandomNumberGenerator.Fill(bytes);
            return $"Cp!{Convert.ToHexString(bytes)}a1";
        }

        private static string FormatIdentityErrors(IdentityResult result)
        {
            return string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        }

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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

        private static ControlPlaneUserOperationResult<TPayload> Succeeded<TPayload>(TPayload payload)
        {
            return new ControlPlaneUserOperationResult<TPayload>(true, Payload: payload);
        }

        private static ControlPlaneUserOperationResult<ControlPlaneUserDetail> NotFound()
        {
            return Failed<ControlPlaneUserDetail>(
                "Control Plane user was not found.",
                ControlPlaneUserOperationFailure.NotFound);
        }

        private static ControlPlaneUserOperationResult<TPayload> Failed<TPayload>(
            string message,
            ControlPlaneUserOperationFailure failure)
        {
            return new ControlPlaneUserOperationResult<TPayload>(
                false,
                message,
                Failure: failure);
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
