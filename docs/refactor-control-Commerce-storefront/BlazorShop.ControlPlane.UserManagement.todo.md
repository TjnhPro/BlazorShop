# BlazorShop ControlPlane User Management Plan

Status: draft
Created: 2026-07-08
Scope: ControlPlane User Management: list, create, assign role, enable/disable, assign/remove permission
Target: `BlazorShop.ControlPlane.API`, `BlazorShop.ControlPlane.Web`, `BlazorShop.Application/ControlPlane`, `ControlPlaneDbContext`

## Context

ControlPlane auth da duoc tach khoi legacy Commerce/Storefront. User Management phai tiep tuc theo boundary nay:

- Dung `ControlPlaneDbContext` va `ControlPlaneConnection`.
- Khong dung `AppDbContext`.
- Khong migrate commerce tables vao ControlPlane database.
- Khong tao database auth rieng.
- Reuse ASP.NET Identity types/service pattern hien co neu phu hop.
- ControlPlane authorization source of truth la `control_plane_role` + `control_plane_permission`.

User Management la man hinh van hanh quyen truy cap ControlPlane. Day khong phai rewrite auth, ma la them API/UI tren nen auth da tach.

## Autoplan Decision Summary

| Lens | Decision | Rationale |
| --- | --- | --- |
| Product/CEO | Lam User Management sau Auth Isolation, truoc khi mo rong nhieu tinh nang operator. | Neu khong co quan tri user/permission, ControlPlane se kho QA bang nhieu vai tro va de tao tai khoan thu cong. |
| Engineering | Giu Identity cho login/password, giu ControlPlane role/permission cho authorization, them direct user permission table. | Tranh tron lan `AspNetRoles` voi business permission; van tan dung auth cu. |
| Design | Tao page `Users` dang admin console: table + filter + detail panel + modal tao user. | Phu hop UI hien tai cua Nodes/Stores, khong can component framework moi. |
| DX/QA | Moi mutation phai co audit log va test bang 2 account seed: admin va user. | QA hien dang thieu User Management; can do duoc permission denied va audit trail. |

## Goals

- List ControlPlane users with search, status, role, permission filters.
- Create a new ControlPlane user.
- Assign/remove ControlPlane role.
- Enable/disable user.
- Assign/remove direct user permission.
- Show inherited role permissions and direct permissions separately.
- Protect all APIs with server-side policies.
- Make UI permission-aware, but server remains authority.
- Write audit logs for every user/role/permission mutation.

## Non-Goals

- No legacy `BlazorShop.Presentation` change.
- No Commerce/Storefront user management.
- No external identity provider.
- No email invitation flow in MVP.
- No MFA management in MVP.
- No role definition editor in MVP.
- No direct permission deny/override in MVP unless explicitly required later.
- No password reset UI in first slice unless needed for QA unblock.

## Architecture

```text
BlazorShop.ControlPlane.Web
  /users page
  IControlPlaneUserClient
  Reuse BlazorShop.Web.Shared HTTP/token helpers
        |
        v
BlazorShop.ControlPlane.API
  ControlPlaneUsersController
  [Authorize(Policy = ...)]
        |
        v
BlazorShop.Application/ControlPlane/Users
  DTOs
  IControlPlaneUserManagementService
        |
        v
BlazorShop.Infrastructure/Data/ControlPlane
  ControlPlaneUserManagementService
  UserManager<AppUser>
  RoleManager<IdentityRole>
  ControlPlaneDbContext
        |
        v
PostgreSQL on 5433
  Identity tables
  control_plane_admin_user
  control_plane_role
  control_plane_permission
  control_plane_admin_user_role
  control_plane_admin_user_permission
  control_audit_log
```

Rules:

- Login/password remains owned by ASP.NET Identity.
- `AspNetRoles` remains only compatibility for reused auth claims (`Admin`, `User`).
- Effective ControlPlane permissions come from:
  - ControlPlane roles through `control_plane_admin_user_role`
  - direct grants through `control_plane_admin_user_permission`
- Removing a direct permission removes only the direct grant.
- If a permission is inherited from a role, it is removed by removing/changing the role, not by deleting the direct grant.

## Database Design

### Existing tables to reuse

| Table | Usage |
| --- | --- |
| `AspNetUsers` | Identity account, password hash, lockout, email, username. |
| `AspNetRoles` | Existing auth compatibility roles: `Admin`, `User`. |
| `AspNetUserRoles` | Identity role assignment for reused auth claims. |
| `RefreshTokens` | JWT refresh token persistence. |
| `control_plane_admin_user` | ControlPlane profile/status linked to Identity user. |
| `control_plane_role` | ControlPlane business roles: `platform_owner`, `node_operator`, `auditor`. |
| `control_plane_permission` | Fine-grained ControlPlane permissions. |
| `control_plane_admin_user_role` | User-to-ControlPlane-role relation. |
| `control_plane_role_permission` | Role-to-permission relation. |
| `control_audit_log` | Mutation audit trail. |

### Add/extend `control_plane_admin_user`

Add fields that make user APIs stable and status changes auditable.

| Column | Type | Required | Note |
| --- | --- | --- | --- |
| `public_id` | `uuid` | yes | Public API identifier. Backfill existing rows with `gen_random_uuid()`. |
| `status_changed_at` | `timestamptz` | no | Last enable/disable transition. |
| `status_changed_by_admin_user_id` | `bigint` | no | FK to `control_plane_admin_user.id`, `set null`. |
| `status_reason` | `text` | no | Optional disable/enable note. |

Indexes:

```sql
create unique index control_plane_admin_user_public_id_uq
    on control_plane_admin_user(public_id);

create index ix_control_plane_admin_user_status
    on control_plane_admin_user(status)
    where deleted_at is null;

create index ix_control_plane_admin_user_status_changed_by
    on control_plane_admin_user(status_changed_by_admin_user_id);
```

Notes:

- API routes must use `public_id`, not internal bigint `id`.
- Keep existing partial unique index on active email.
- Do not expose `identity_user_id` as the public route key.

### Add `control_plane_admin_user_permission`

This table supports assign/remove permission directly to a user without changing system roles.

| Column | Type | Required | Note |
| --- | --- | --- | --- |
| `admin_user_id` | `bigint` | yes | FK to `control_plane_admin_user.id`, cascade delete. |
| `permission_id` | `bigint` | yes | FK to `control_plane_permission.id`, cascade delete. |
| `created_at` | `timestamptz` | yes | Default `CURRENT_TIMESTAMP`. |
| `created_by_admin_user_id` | `bigint` | no | Actor profile id, `set null`. |

Constraints/indexes:

```sql
alter table control_plane_admin_user_permission
    add constraint pk_control_plane_admin_user_permission
    primary key (admin_user_id, permission_id);

create index ix_control_plane_admin_user_permission_permission_id
    on control_plane_admin_user_permission(permission_id);

create index ix_control_plane_admin_user_permission_created_by
    on control_plane_admin_user_permission(created_by_admin_user_id);
```

MVP semantics:

- Direct permissions are additive grants.
- No `deny` effect in phase 1.
- Remove permission deletes the direct grant row.
- If role still grants the same permission, the user still has it effectively.

Future extension if needed:

- Add `effect text check (effect in ('grant', 'deny'))`.
- Add `expires_at timestamptz` for temporary access.
- Add `reason text` for access reviews.

### Add ControlPlane permissions

Add these keys to `ControlPlanePermissions`, `ControlPlanePolicyNames`, seed data, and API authorization registration.

| Id range | Key | Purpose |
| --- | --- | --- |
| 9 | `users.read` | List/view users, roles, permission catalog. |
| 10 | `users.write` | Create/update user profile and enable/disable. |
| 11 | `roles.assign` | Assign/remove ControlPlane role. |
| 12 | `permissions.manage` | Assign/remove direct user permission. |

Seed assignment:

| Role | New permissions |
| --- | --- |
| `platform_owner` | all four new permissions. |
| `node_operator` | none by default. |
| `auditor` | none by default in MVP. |

Rationale:

- User Management is privileged; read access can reveal operator emails and permission maps.
- `auditor` can receive `users.read` later if that is an operational requirement.
- Avoid creating a new `user_admin` system role until there is a real need.

### Entity changes

Add:

```csharp
public sealed class ControlPlaneAdminUserPermission
{
    public long AdminUserId { get; set; }
    public ControlPlaneAdminUser? AdminUser { get; set; }
    public long PermissionId { get; set; }
    public ControlPlanePermission? Permission { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedByAdminUserId { get; set; }
    public ControlPlaneAdminUser? CreatedByAdminUser { get; set; }
}
```

Update:

- `ControlPlaneAdminUser.PublicId`
- `ControlPlaneAdminUser.StatusChangedAt`
- `ControlPlaneAdminUser.StatusChangedByAdminUserId`
- `ControlPlaneAdminUser.StatusReason`
- `ControlPlaneAdminUser.DirectPermissions`
- `ControlPlanePermission.DirectUsers`

### Authorization handler update

Current handler checks only role permissions. Update it to check effective permissions:

```text
active, not-deleted profile
  has required permission through roles
  OR has required permission through direct user grants
```

Keep it as a single database query where practical. Do not call `AppDbContext`.

## API Design

Base route:

```text
/api/control-plane/users
```

All endpoints require authenticated ControlPlane account.

### Catalog endpoints

| Method | Route | Policy | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/control-plane/users/roles` | `users.read` | List assignable ControlPlane roles. |
| `GET` | `/api/control-plane/users/permissions` | `users.read` | List ControlPlane permission catalog. |

Role response:

```json
{
  "items": [
    {
      "key": "platform_owner",
      "name": "Platform Owner",
      "description": "Full Control Plane access.",
      "isSystem": true,
      "permissionKeys": ["nodes.read", "users.read"]
    }
  ]
}
```

Permission response:

```json
{
  "items": [
    {
      "key": "users.read",
      "description": "List and view Control Plane users."
    }
  ]
}
```

### List users

```text
GET /api/control-plane/users?search=&status=&roleKey=&permissionKey=&cursor=&limit=25
Policy: users.read
```

Response:

```json
{
  "items": [
    {
      "publicId": "uuid",
      "email": "admin@example.com",
      "displayName": "Control Plane Admin",
      "status": "active",
      "identityRoles": ["Admin"],
      "controlPlaneRoles": [
        { "key": "platform_owner", "name": "Platform Owner" }
      ],
      "directPermissionKeys": ["users.read"],
      "effectivePermissionKeys": ["nodes.read", "users.read"],
      "lastLoginAt": "2026-07-08T00:00:00Z",
      "createdAt": "2026-07-08T00:00:00Z",
      "updatedAt": "2026-07-08T00:00:00Z"
    }
  ],
  "nextCursor": null
}
```

Filter rules:

- `search`: email or display name, case-insensitive.
- `status`: `active`, `disabled`, `invited`.
- `roleKey`: matches ControlPlane role key.
- `permissionKey`: matches effective permission, including role-inherited and direct grants.
- `limit`: clamp to 1..100, default 25.
- Cursor: stable by `created_at desc, id desc`.

### Get user detail

```text
GET /api/control-plane/users/{publicId:guid}
Policy: users.read
```

Detail should include:

- Profile fields.
- Identity role names.
- ControlPlane role assignments.
- Direct permission grants.
- Effective permission list grouped by source:
  - `role`
  - `direct`
- Status change metadata.
- Recent user-related audit entries, optional first 10.

### Create user

```text
POST /api/control-plane/users
Policy: users.write
```

Request:

```json
{
  "email": "operator@example.com",
  "displayName": "Node Operator",
  "identityRole": "User",
  "controlPlaneRoleKeys": ["node_operator"],
  "directPermissionKeys": [],
  "temporaryPassword": null
}
```

Response:

```json
{
  "user": {
    "publicId": "uuid",
    "email": "operator@example.com",
    "displayName": "Node Operator",
    "status": "active"
  },
  "temporaryPassword": "shown-once-if-generated"
}
```

Rules:

- Normalize email before uniqueness check.
- If `temporaryPassword` is null, generate a strong server-side password and return it once.
- Set `RequirePasswordChange = true` if current login flow can enforce it; otherwise keep the field ready and add enforcement in a follow-up auth slice.
- New users get Identity role `User` by default.
- Only allow Identity roles from allowlist: `Admin`, `User`.
- Create Identity user, profile, ControlPlane roles, direct permissions in one transaction.
- Audit action: `users.create`.
- Never log raw password.

Validation:

- Duplicate email -> `409 Conflict`.
- Invalid Identity role -> `400 Bad Request`.
- Invalid ControlPlane role key -> `400 Bad Request`.
- Invalid permission key -> `400 Bad Request`.
- Weak supplied password -> `400 Bad Request` with Identity errors.

### Update profile

```text
PUT /api/control-plane/users/{publicId:guid}
Policy: users.write
```

MVP request:

```json
{
  "displayName": "Updated Name"
}
```

Do not include email change in first phase unless required. Email changes affect Identity normalized fields, uniqueness, login identity, and audit expectations.

Audit action: `users.update`.

### Enable/disable user

```text
POST /api/control-plane/users/{publicId:guid}/disable
Policy: users.write

POST /api/control-plane/users/{publicId:guid}/enable
Policy: users.write
```

Request:

```json
{
  "reason": "Access review"
}
```

Rules:

- Disable sets profile `status = 'disabled'`.
- Disable also revokes active refresh tokens for that Identity user.
- Login/profile creation code must reject disabled profile.
- Cannot disable yourself.
- Cannot disable the last active `platform_owner`.
- Enable sets profile `status = 'active'`.
- Enable does not restore revoked refresh tokens.
- Audit actions: `users.disable`, `users.enable`.

### Assign/remove role

```text
POST /api/control-plane/users/{publicId:guid}/roles
Policy: roles.assign

DELETE /api/control-plane/users/{publicId:guid}/roles/{roleKey}
Policy: roles.assign
```

Assign request:

```json
{
  "roleKey": "node_operator"
}
```

Rules:

- Role key must exist in `control_plane_role`.
- Assign is idempotent.
- Remove is idempotent unless it violates safety guard.
- Cannot remove your own last `platform_owner` access.
- Cannot leave the system with zero active `platform_owner` users.
- Audit actions: `users.role.assign`, `users.role.remove`.

### Assign/remove direct permission

```text
POST /api/control-plane/users/{publicId:guid}/permissions
Policy: permissions.manage

DELETE /api/control-plane/users/{publicId:guid}/permissions/{permissionKey}
Policy: permissions.manage
```

Assign request:

```json
{
  "permissionKey": "stores.write"
}
```

Rules:

- Permission key must exist in `control_plane_permission`.
- Assign creates direct grant only.
- Remove deletes direct grant only.
- UI must show when permission remains effective through role.
- Cannot remove your own final access to `permissions.manage` or `roles.assign` if it would lock out all active managers.
- Audit actions: `users.permission.assign`, `users.permission.remove`.

### Error contract

Prefer existing controller style first. If adjusting error shape, use:

```json
{
  "message": "Cannot disable the last active platform owner.",
  "code": "last_platform_owner"
}
```

Recommended codes:

- `validation`
- `duplicate_email`
- `not_found`
- `invalid_role`
- `invalid_permission`
- `self_disable_not_allowed`
- `last_platform_owner`
- `access_lockout_guard`

## Application Layer Design

Add folder:

```text
BlazorShop.Application/ControlPlane/Users/
  ControlPlaneUserDtos.cs
  IControlPlaneUserManagementService.cs
```

Service contract:

```csharp
public interface IControlPlaneUserManagementService
{
    Task<ControlPlaneUserListResponse> ListAsync(ControlPlaneUserListQuery query, CancellationToken cancellationToken = default);
    Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> GetAsync(Guid publicId, CancellationToken cancellationToken = default);
    Task<ControlPlaneUserOperationResult<CreateControlPlaneUserResponse>> CreateAsync(CreateControlPlaneUserRequest request, ControlPlaneUserActor actor, CancellationToken cancellationToken = default);
    Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> UpdateAsync(Guid publicId, UpdateControlPlaneUserRequest request, ControlPlaneUserActor actor, CancellationToken cancellationToken = default);
    Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> DisableAsync(Guid publicId, ChangeControlPlaneUserStatusRequest request, ControlPlaneUserActor actor, CancellationToken cancellationToken = default);
    Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> EnableAsync(Guid publicId, ChangeControlPlaneUserStatusRequest request, ControlPlaneUserActor actor, CancellationToken cancellationToken = default);
    Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> AssignRoleAsync(Guid publicId, AssignControlPlaneRoleRequest request, ControlPlaneUserActor actor, CancellationToken cancellationToken = default);
    Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> RemoveRoleAsync(Guid publicId, string roleKey, ControlPlaneUserActor actor, CancellationToken cancellationToken = default);
    Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> AssignPermissionAsync(Guid publicId, AssignControlPlanePermissionRequest request, ControlPlaneUserActor actor, CancellationToken cancellationToken = default);
    Task<ControlPlaneUserOperationResult<ControlPlaneUserDetail>> RemovePermissionAsync(Guid publicId, string permissionKey, ControlPlaneUserActor actor, CancellationToken cancellationToken = default);
    Task<ControlPlaneRoleCatalogResponse> ListRolesAsync(CancellationToken cancellationToken = default);
    Task<ControlPlanePermissionCatalogResponse> ListPermissionsAsync(CancellationToken cancellationToken = default);
}
```

Implementation location:

```text
BlazorShop.Infrastructure/Data/ControlPlane/ControlPlaneUserManagementService.cs
```

Implementation dependencies:

- `ControlPlaneDbContext`
- `UserManager<AppUser>`
- `RoleManager<IdentityRole>`
- `IControlPlaneAuditService` or controller-level audit, but use one style consistently.
- Existing password/validation helpers if available.

Transaction boundaries:

- Create user: one transaction for Identity user, Identity role, admin profile, ControlPlane roles, direct permissions.
- Disable user: status update + refresh token revocation + audit.
- Role/permission changes: relation update + audit.

## UI Design

### Navigation

Add nav item:

```razor
<NavLink class="cp-nav-link" href="users">
    <i class="fa-solid fa-users-gear w-4" aria-hidden="true"></i>
    <span>Users</span>
</NavLink>
```

### Page route

```text
BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/Users.razor
@page "/users"
@attribute [Authorize]
```

### Page layout

Use current Tailwind card/table style from `Nodes.razor`.

```text
Users page
  Header
    title: Users
    subtitle: Manage Control Plane access
    action: Create user

  Summary row
    Total users
    Active users
    Disabled users
    Platform owners

  Filter bar
    Search email/display name
    Status select
    Role select
    Permission select
    Search button

  Main grid
    Left: user table
      User
      Status
      Roles
      Direct permissions
      Last login
      Actions

    Right: detail panel
      Profile
      Status
      Identity role
      ControlPlane roles
      Direct permissions
      Effective permissions
      Recent audit
```

### Create user modal/panel

Fields:

- Email
- Display name
- Identity role: `User` default, `Admin` optional
- ControlPlane roles multi-select
- Direct permissions multi-select
- Temporary password mode:
  - Generate automatically, recommended
  - Or manual temporary password for local QA

Actions:

- Create
- Cancel

Post-create:

- Show one-time temporary password in a dismissible result panel.
- Do not persist or re-fetch password.
- Provide visual warning that password cannot be shown again.

### Detail panel actions

Actions depend on permission:

| UI action | Required permission |
| --- | --- |
| View users | `users.read` |
| Create user | `users.write` |
| Edit display name | `users.write` |
| Enable/disable | `users.write` |
| Assign/remove role | `roles.assign` |
| Assign/remove direct permission | `permissions.manage` |

UI behavior:

- If user lacks a permission, hide primary action or show disabled state with a short tooltip.
- Do not rely on UI checks for security; API must return `403`.
- If API returns `403`, display: `Your Control Plane account does not have permission for this action.`

### Visual conventions

- Font Awesome icons:
  - `fa-users-gear` for page/nav.
  - `fa-user-plus` for create.
  - `fa-user-shield` for roles.
  - `fa-key` or `fa-fingerprint` for permissions.
  - `fa-ban` for disable.
  - `fa-circle-check` for enable.
- Tailwind style should stay dense and operational like existing Nodes page.
- Status badges:
  - active: green
  - invited: blue
  - disabled: gray/red neutral
- Avoid nested cards. Use table + detail panel.

## Web Client Design

Add:

```text
BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Services/Users/ControlPlaneUserClient.cs
```

Client pattern should follow `ControlPlaneNodeClient`:

- Use `IHttpClientHelper.GetPrivateClientAsync()`.
- Use `System.Net.Http.Json`.
- Handle `401` and `403` with clear messages.
- Parse `{ message }` response body when present.
- Keep Web DTOs local unless shared contracts are later introduced.

Client interface:

```csharp
public interface IControlPlaneUserClient
{
    Task<UserListResponse> ListAsync(UserListFilter filter, CancellationToken cancellationToken = default);
    Task<UserDetail?> GetAsync(Guid publicId, CancellationToken cancellationToken = default);
    Task<CreateUserResult> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserMutationResult> UpdateAsync(Guid publicId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserMutationResult> DisableAsync(Guid publicId, ChangeUserStatusRequest request, CancellationToken cancellationToken = default);
    Task<UserMutationResult> EnableAsync(Guid publicId, ChangeUserStatusRequest request, CancellationToken cancellationToken = default);
    Task<UserMutationResult> AssignRoleAsync(Guid publicId, AssignRoleRequest request, CancellationToken cancellationToken = default);
    Task<UserMutationResult> RemoveRoleAsync(Guid publicId, string roleKey, CancellationToken cancellationToken = default);
    Task<UserMutationResult> AssignPermissionAsync(Guid publicId, AssignPermissionRequest request, CancellationToken cancellationToken = default);
    Task<UserMutationResult> RemovePermissionAsync(Guid publicId, string permissionKey, CancellationToken cancellationToken = default);
    Task<RoleCatalogResponse> ListRolesAsync(CancellationToken cancellationToken = default);
    Task<PermissionCatalogResponse> ListPermissionsAsync(CancellationToken cancellationToken = default);
}
```

Register in Web `Program.cs`.

## Phase Plan

### Phase 0 - Planning and safety checks

Tasks:

- [ ] Create this plan artifact.
- [ ] Confirm no User Management code depends on `AppDbContext`.
- [ ] Confirm ControlPlane local PostgreSQL uses port `5433`.
- [ ] Confirm existing seeded accounts:
  - admin with platform owner access
  - user with restricted user/node operator access
- [ ] Decide whether `auditor` gets `users.read`; recommendation: no in MVP.

Acceptance:

- Plan is ready for phased implementation.
- Database/API/UI decisions are explicit enough for implementation commits.

### Phase 1 - Database schema and permission seed

Tasks:

- [ ] Add `PublicId`, status change metadata, and navigation properties to `ControlPlaneAdminUser`.
- [ ] Add `ControlPlaneAdminUserPermission` entity.
- [ ] Add `DbSet<ControlPlaneAdminUserPermission>`.
- [ ] Configure table, keys, FKs, indexes, and timestamps in `ControlPlaneDbContext`.
- [ ] Add permission constants:
  - `UsersRead`
  - `UsersWrite`
  - `RolesAssign`
  - `PermissionsManage`
- [ ] Add policy names and policy mapping.
- [ ] Seed new permission rows.
- [ ] Seed new permission mappings to `platform_owner`.
- [ ] Generate EF migration.
- [ ] Apply migration against local ControlPlane database on port `5433`.

Acceptance:

- `dotnet ef database update` succeeds for `ControlPlaneDbContext`.
- `platform_owner` has all User Management permissions.
- `node_operator` and `auditor` do not accidentally get User Management permissions.
- Existing admin/user seed still works.

### Phase 2 - Effective permission resolution

Tasks:

- [ ] Update `ControlPlanePermissionAuthorizationHandler` to include direct user permission grants.
- [ ] Add helper query or internal projection for effective permissions.
- [ ] Ensure disabled/deleted profiles cannot authorize.
- [ ] Add regression tests or API smoke checks for:
  - role permission succeeds
  - direct permission succeeds
  - missing permission returns `403`
  - disabled user returns `403`

Acceptance:

- Existing node/store/health/audit policies still pass for existing roles.
- Direct permission grant can unlock an action without changing role.

### Phase 3 - Application service and read APIs

Tasks:

- [ ] Add `BlazorShop.Application/ControlPlane/Users` DTOs.
- [ ] Add `IControlPlaneUserManagementService`.
- [ ] Implement list/detail/catalog methods in infrastructure.
- [ ] Add `ControlPlaneUsersController`.
- [ ] Add routes:
  - `GET /api/control-plane/users`
  - `GET /api/control-plane/users/{publicId}`
  - `GET /api/control-plane/users/roles`
  - `GET /api/control-plane/users/permissions`
- [ ] Protect all read endpoints with `users.read`.
- [ ] Return effective permissions grouped by role/direct source.

Acceptance:

- Admin can list users.
- Restricted user receives `403` for list users.
- List supports search/status/role/permission filters.
- Detail uses `publicId`, not internal bigint id.

### Phase 4 - Create/update/enable/disable APIs

Tasks:

- [ ] Implement create user service method.
- [ ] Generate temporary password when omitted.
- [ ] Assign Identity role `User` by default.
- [ ] Create `control_plane_admin_user` profile.
- [ ] Assign initial ControlPlane roles and direct permissions.
- [ ] Implement update display name.
- [ ] Implement disable:
  - status update
  - refresh token revocation
  - self-disable guard
  - last platform owner guard
- [ ] Implement enable.
- [ ] Write audit logs for every mutation.
- [ ] Add API routes:
  - `POST /api/control-plane/users`
  - `PUT /api/control-plane/users/{publicId}`
  - `POST /api/control-plane/users/{publicId}/disable`
  - `POST /api/control-plane/users/{publicId}/enable`

Acceptance:

- Admin can create a new active ControlPlane user.
- Duplicate email returns `409`.
- Disabled user cannot use protected APIs after token refresh/session renewal.
- Admin cannot disable self.
- Admin cannot disable/remove last active `platform_owner`.
- Audit logs contain create/update/enable/disable actions.

### Phase 5 - Role and direct permission mutation APIs

Tasks:

- [ ] Implement assign/remove ControlPlane role.
- [ ] Implement assign/remove direct permission.
- [ ] Add lockout guards for last platform owner and final access manager.
- [ ] Add routes:
  - `POST /api/control-plane/users/{publicId}/roles`
  - `DELETE /api/control-plane/users/{publicId}/roles/{roleKey}`
  - `POST /api/control-plane/users/{publicId}/permissions`
  - `DELETE /api/control-plane/users/{publicId}/permissions/{permissionKey}`
- [ ] Add audit logs:
  - `users.role.assign`
  - `users.role.remove`
  - `users.permission.assign`
  - `users.permission.remove`

Acceptance:

- Admin can assign/remove `node_operator` or `auditor`.
- Admin can grant a direct permission.
- Removing direct permission does not remove role-inherited permission.
- Restricted user cannot assign roles or permissions.
- Audit logs show actor, target user, action, result.

### Phase 6 - Web client and Users page read UI

Tasks:

- [ ] Add `IControlPlaneUserClient`.
- [ ] Register client in Web `Program.cs`.
- [ ] Add `Users.razor`.
- [ ] Add nav item with Font Awesome icon.
- [ ] Implement list filters.
- [ ] Implement detail panel.
- [ ] Load role and permission catalogs.
- [ ] Show inherited vs direct permission badges.
- [ ] Handle `401`/`403` cleanly.

Acceptance:

- Admin can open `/users` and see seeded admin/user accounts.
- Search/status/role/permission filters work.
- Restricted user gets a clear forbidden state, not a broken page.

### Phase 7 - Web mutation UI

Tasks:

- [ ] Add create user panel/modal.
- [ ] Show generated temporary password once.
- [ ] Add enable/disable actions with confirmation.
- [ ] Add role assignment controls.
- [ ] Add direct permission assignment controls.
- [ ] Hide/disable controls based on current user's effective permissions.
- [ ] Refresh selected user after mutation.
- [ ] Keep table row and detail panel consistent after mutations.

Acceptance:

- Admin can perform all requested UI operations.
- Restricted user cannot see or execute privileged actions.
- UI does not expose raw internal ids.
- API `403` is shown as permission error.

### Phase 8 - QA and documentation

Tasks:

- [ ] Update `QA-ControlPlane.todo.md` User section from `n/a` to concrete checks.
- [ ] Verify auth:
  - login as admin
  - login as user
  - wrong password and repeated wrong password
- [ ] Verify user:
  - admin can list/create
  - admin can assign role
  - admin can enable/disable
  - admin can assign/remove direct permission
  - user without permission gets `403`
- [ ] Verify audit logs for all User Management mutations.
- [ ] Verify disabled user behavior.
- [ ] Run backend build/tests.
- [ ] Run Web build.
- [ ] Run browser QA through ControlPlane UI if dev server is available.

Acceptance:

- `QA-ControlPlane.todo.md` records tested status and remaining gaps.
- No User Management feature remains marked `n/a` unless explicitly deferred.

## QA Checklist Additions

Add or expand these checks in `QA-ControlPlane.todo.md`:

- [ ] Admin can open Users page.
- [ ] Standard user without `users.read` cannot open/list Users.
- [ ] Admin can create user with generated temporary password.
- [ ] Duplicate email is rejected.
- [ ] Admin can assign `node_operator`.
- [ ] Admin can remove `node_operator`.
- [ ] Admin can assign direct `stores.read`.
- [ ] Admin can remove direct `stores.read`.
- [ ] Removing direct permission does not remove inherited role permission.
- [ ] Admin cannot disable own account.
- [ ] Admin cannot remove the last active `platform_owner`.
- [ ] Disabled user cannot continue after refresh/re-login.
- [ ] Audit logs record `users.create`.
- [ ] Audit logs record `users.disable`.
- [ ] Audit logs record `users.enable`.
- [ ] Audit logs record `users.role.assign`.
- [ ] Audit logs record `users.role.remove`.
- [ ] Audit logs record `users.permission.assign`.
- [ ] Audit logs record `users.permission.remove`.

## Open Questions To Confirm

1. Should `auditor` receive `users.read` by default?
   Recommendation: no for MVP, because user emails and permission maps are sensitive.

2. Should create user generate temporary password or require admin to type one?
   Recommendation: generate server-side and show once; allow manual password only in local/dev if needed.

3. Should direct permission support deny overrides?
   Recommendation: no for MVP. Additive direct grants are simpler and safer to reason about.

4. Should email update be included in User Management phase 1?
   Recommendation: no. Add after list/create/role/permission/status is stable.

## Implementation Order With Suggested Commits

1. `docs(control-plane): plan user management`
2. `feat(control-plane): add user management schema`
3. `feat(control-plane): support direct user permissions`
4. `feat(control-plane): add user management read api`
5. `feat(control-plane): add user mutation api`
6. `feat(control-plane): add role and permission assignment api`
7. `feat(control-plane): add users web page`
8. `feat(control-plane): add user management actions ui`
9. `docs(qa): verify control plane user management`

