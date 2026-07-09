# BlazorShop ControlPlane Auth Isolation Plan

## Context

Plan nay thay the huong cu trong `BlazorShop.ControlPlane.Auth.md` ve viec dung `AppDbContext` cho ControlPlane auth.

Ket luan moi:

- `AppDbContext` la legacy Commerce/Storefront context.
- `ControlPlane` la bounded context rieng, dung PostgreSQL rieng tren port `5433`.
- ControlPlane chi can auth tables va ControlPlane operational tables trong `ControlPlaneDbContext`.
- Khong migrate `AppDbContext` vao ControlPlane database.
- Khong de ControlPlane API phu thuoc `DefaultConnection` hoac legacy Commerce schema.

## Autoplan Decision Summary

| Lens | Decision | Rationale |
| --- | --- | --- |
| Product/CEO | Sua kien truc auth isolation truoc khi build them user/admin UI. | Neu tiep tuc dua `AppDbContext` vao ControlPlane, ControlPlane DB se bi nhiem legacy commerce schema va kho cat ve sau. |
| Engineering | `ControlPlaneDbContext` se la context duy nhat cho ControlPlane database, bao gom ASP.NET Identity tables, refresh tokens va ControlPlane tables. | Day la boundary dung: mot database ControlPlane rieng, mot EF migration stream rieng. |
| Design | Login/logout UI hien co co the giu, nhung backend store phai doi ve ControlPlane DB. | UX khong can viet lai; loi nam o persistence boundary. |
| DX/QA | Local dev phai de nhin: chi can PostgreSQL `5433`, migrate mot context, seed mot admin. | Giam nham lan `DefaultConnection`, `AuthConnection`, `ControlPlaneConnection`. |

## Implementation Status

| Phase | Status | Commit |
| --- | --- | --- |
| Plan artifact | Done | `213bddb docs(control-plane): plan isolated auth database boundary` |
| Stop `AppDbContext` leak | Done | `e35ac23 refactor(control-plane): remove AppDbContext from auth startup` |
| Identity in `ControlPlaneDbContext` | Done | `b84abbe feat(control-plane): host identity auth in ControlPlaneDbContext` |
| ControlPlane-specific auth DI | Done | `2d46a8d feat(control-plane): add isolated auth infrastructure` |
| Identity migration and dev admin seed | Done | `af222c9 feat(control-plane): add identity migration and dev admin seed` |
| QA/runbook update | Done | `docs(qa): update Control Plane auth isolation checks` |

Runtime verification on 2026-07-08 used a clean QA database `blazorshop_controlplane_isolation_qa` so the contaminated local dev database did not need to be dropped.

## Target Architecture

```text
BlazorShop.ControlPlane.Web (Blazor WASM)
  Login / Logout / Protected routes
  Reuses BlazorShop.Web.SharedV2 auth client helpers
        |
        v
BlazorShop.ControlPlane.API
  ControlPlaneAuthController
  ASP.NET Identity + JWT + refresh token
  ControlPlane permission policies
        |
        v
ControlPlaneDbContext
  Identity/Auth tables
  RefreshTokens
  ControlPlane admin/profile/permission tables
  Nodes / Stores / Health / Actions / Audit
        |
        v
PostgreSQL: blazorshop_controlplane on localhost:5433
```

Explicit non-goals:

- Khong dung `AppDbContext` trong ControlPlane API.
- Khong migrate legacy commerce tables vao ControlPlane DB.
- Khong tao database auth thu ba.
- Khong rewrite JWT/login flow neu `BlazorShop.Application.Services.Authentication.AuthenticationService` van reuse duoc qua abstraction.
- Khong sua legacy Storefront auth behavior trong phase nay.

## Database Topology

### Connection strings

ControlPlane API chi can mot database connection cho phase nay:

```json
{
  "ConnectionStrings": {
    "ControlPlaneConnection": "Host=localhost;Port=5433;Database=blazorshop_controlplane;Username=blazorshop_controlplane;Password=blazorshop_controlplane_dev"
  }
}
```

Rules:

- `ControlPlaneConnection` la source of truth cho ControlPlane database.
- `AuthConnection` khong can dung trong ControlPlane API neu auth tables nam trong `ControlPlaneDbContext`.
- `DefaultConnection` khong duoc ControlPlane API doc/fallback.
- Legacy app van co the dung `DefaultConnection` rieng o host legacy.

### DbContext target

`ControlPlaneDbContext` nen chuyen tu:

```csharp
public sealed class ControlPlaneDbContext : DbContext
```

sang:

```csharp
public sealed class ControlPlaneDbContext : IdentityDbContext<AppUser>
```

Trong `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.HasPostgresExtension("pgcrypto");

    ConfigureIdentity(modelBuilder);
    ConfigureRefreshTokens(modelBuilder);
    ConfigureAdminUsers(modelBuilder);
    ConfigureRolesAndPermissions(modelBuilder);
    ConfigureNodes(modelBuilder);
    ConfigureStores(modelBuilder);
    ConfigureActions(modelBuilder);
    ConfigureAudit(modelBuilder);
    SeedAuthorization(modelBuilder);
}
```

## Database Schema Detail

### Identity/Auth tables

These tables are owned by `ControlPlaneDbContext` migrations.

| Table | Purpose | Source |
| --- | --- | --- |
| `AspNetUsers` | First-party ControlPlane login accounts. | ASP.NET Identity via `IdentityDbContext<AppUser>`. |
| `AspNetRoles` | Identity roles required by reused auth service, initially `Admin` and `User`. | ASP.NET Identity. |
| `AspNetUserRoles` | User-to-Identity-role relation. | ASP.NET Identity. |
| `AspNetUserClaims` | Extra claims if needed later. | ASP.NET Identity. |
| `AspNetRoleClaims` | Role claims if needed later. | ASP.NET Identity. |
| `AspNetUserLogins` | External login placeholder; unused now but created by Identity. | ASP.NET Identity. |
| `AspNetUserTokens` | Identity token storage placeholder. | ASP.NET Identity. |
| `RefreshTokens` | JWT refresh token rotation/revocation store. | Existing `RefreshToken` entity. |

### `AspNetUsers` important columns

| Column | Type | Note |
| --- | --- | --- |
| `Id` | `text` | Identity user id. Referenced by `control_plane_admin_user.identity_user_id`. |
| `UserName` | `varchar(256)` | Usually email. |
| `NormalizedUserName` | `varchar(256)` | Unique index from Identity. |
| `Email` | `varchar(256)` | Login email. |
| `NormalizedEmail` | `varchar(256)` | Indexed by Identity. |
| `EmailConfirmed` | `boolean` | Dev can be false/true depending Identity confirmation policy. |
| `PasswordHash` | `text` | ASP.NET Identity password hash. |
| `SecurityStamp` | `text` | Identity invalidation stamp. |
| `ConcurrencyStamp` | `text` | Identity concurrency token. |
| `PhoneNumber` | `text` | Optional. |
| `PhoneNumberConfirmed` | `boolean` | Optional. |
| `TwoFactorEnabled` | `boolean` | Future hardening. |
| `LockoutEnd` | `timestamp with time zone` | Used by lockout. |
| `LockoutEnabled` | `boolean` | Must remain true for new users. |
| `AccessFailedCount` | `integer` | Used by repeated wrong-password lockout. |
| `FullName` | `text` | From existing `AppUser`. |
| `CreatedOn` | `timestamp with time zone` | Existing `AppUser` field. |
| `RequirePasswordChange` | `boolean` | Existing `AppUser` field. |

### `RefreshTokens`

| Column | Type | Rule |
| --- | --- | --- |
| `Id` | `uuid` | Primary key. |
| `UserId` | `text` | FK to `AspNetUsers.Id`, cascade delete. |
| `TokenHash` | `varchar(64)` | Unique SHA-256 hash. Never store raw token. |
| `CreatedAtUtc` | `timestamp with time zone` | Creation time. |
| `ExpiresAtUtc` | `timestamp with time zone` | Expiration. |
| `RevokedAtUtc` | `timestamp with time zone`, nullable | Revocation marker. |
| `ReplacedByTokenHash` | `varchar(64)`, nullable | Rotation chain. |
| `CreatedByIp` | `varchar(64)`, nullable | Audit metadata. |
| `RevokedByIp` | `varchar(64)`, nullable | Audit metadata. |
| `UserAgent` | `varchar(512)`, nullable | Audit metadata. |

Indexes:

- Unique index on `TokenHash`.
- Index on `(UserId, RevokedAtUtc)`.
- Optional index on `ExpiresAtUtc` for cleanup job later.

### ControlPlane admin/profile tables

Existing tables stay in `ControlPlaneDbContext`.

| Table | Purpose | Auth relation |
| --- | --- | --- |
| `control_plane_admin_user` | ControlPlane profile/status for Identity user. | `identity_user_id` references `AspNetUsers.Id` logically; add FK if migration risk is acceptable. |
| `control_plane_role` | ControlPlane domain roles: `platform_owner`, `node_operator`, `auditor`. | Separate from `AspNetRoles`; used for ControlPlane permissions. |
| `control_plane_permission` | Fine-grained ControlPlane permissions. | Used by `ControlPlanePermissionAuthorizationHandler`. |
| `control_plane_admin_user_role` | Admin profile to ControlPlane role. | Uses `admin_user_id`, not Identity role id. |
| `control_plane_role_permission` | Role to permission mapping. | Seeded system matrix. |

Important decision:

- `AspNetRoles` remains only for compatibility with existing `AuthenticationService` claims generation.
- Real ControlPlane authorization continues to use `control_plane_role` + `control_plane_permission`.
- Long term, login claim role can be simplified, but phase nay khong doi Application auth contract neu khong can.

### Operational ControlPlane tables

These remain unchanged:

| Group | Tables |
| --- | --- |
| Nodes | `commerce_node`, `commerce_node_endpoint`, `commerce_node_credential` |
| Health | `node_health_snapshot`, `node_capability_snapshot` |
| Stores | `store_registry`, `store_domain_registry` |
| Actions | `control_action`, `control_action_attempt` |
| Audit | `control_audit_log` |

Audit rules:

- `control_audit_log.actor_identity_user_id` stores `AspNetUsers.Id` as text, no hard FK required because audit must survive deleted users.
- `control_audit_log.actor_admin_user_id` can FK to `control_plane_admin_user.id` with `SetNull`.
- No raw password, raw refresh token, raw API secret, or raw node secret in audit metadata.

## Phase 0 - Stop the AppDbContext leak

Goal: freeze the wrong direction before adding more features.

Tasks:

- [ ] Mark `9526119 fix(qa): restore Control Plane auth startup` as needing replacement in the work log.
- [ ] Remove `AppDbContext` usage from `ControlPlaneDatabaseBootstrapper`.
- [ ] Remove `using BlazorShop.Infrastructure;` if only needed for `AddSharedAuthenticationInfrastructure`.
- [ ] Remove `builder.Services.AddSharedAuthenticationInfrastructure(builder.Configuration)` from ControlPlane API.
- [ ] Confirm `rg "AppDbContext" BlazorShop.PresentationV2/BlazorShop.ControlPlane.API BlazorShop.Infrastructure/Data/ControlPlane` returns no runtime registration usage.

Acceptance:

- ControlPlane API no longer resolves `AppDbContext`.
- ControlPlane API does not read `AuthConnection` or `DefaultConnection`.
- Startup migration touches only `ControlPlaneDbContext`.

## Phase 1 - Move Identity schema into ControlPlaneDbContext

Goal: ControlPlane database owns auth tables directly.

Tasks:

- [ ] Update `ControlPlaneDbContext` base type to `IdentityDbContext<AppUser>`.
- [ ] Add `DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();`.
- [ ] Add `using BlazorShop.Domain.Entities.Identity;`.
- [ ] Add `using Microsoft.AspNetCore.Identity;`.
- [ ] Add `using Microsoft.AspNetCore.Identity.EntityFrameworkCore;`.
- [ ] Call `base.OnModelCreating(modelBuilder)` before ControlPlane table config.
- [ ] Extract identity-specific config into `ConfigureIdentity(modelBuilder)`:
  - `AppUser.CreatedOn` column type/default.
  - `IdentityUserLogin<string>.LoginProvider` max length 128.
  - `IdentityUserLogin<string>.ProviderKey` max length 128.
  - `IdentityUserToken<string>.LoginProvider` max length 128.
  - `IdentityUserToken<string>.Name` max length 128.
  - Seed Identity roles `Admin`, `User` if reused auth service requires them.
- [ ] Extract refresh-token config into `ConfigureRefreshTokens(modelBuilder)`:
  - max lengths
  - unique token hash
  - user/revoked index
  - FK to `AppUser`
- [ ] Decide whether to add FK from `control_plane_admin_user.identity_user_id` to `AspNetUsers.Id`.

Acceptance:

- `dotnet build BlazorShop.Infrastructure/BlazorShop.Infrastructure.csproj` passes.
- `ControlPlaneDbContextModelSnapshot` will include Identity tables after migration.
- No Commerce tables are introduced by this phase.

## Phase 2 - ControlPlane-specific Identity DI

Goal: register Identity/JWT/auth services for ControlPlane without legacy `AppDbContext`.

Preferred implementation:

- Add a new extension method in ControlPlane infrastructure area, for example:

```csharp
public static IServiceCollection AddControlPlaneAuthenticationInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
```

Register:

- `AddDefaultIdentity<AppUser>(...)`
- `.AddRoles<IdentityRole>()`
- `.AddEntityFrameworkStores<ControlPlaneDbContext>()`
- JWT bearer auth with existing `JWT:*` configuration.
- `IAppUserManager` mapped to ControlPlane-safe implementation.
- `IAppTokenManager` mapped to ControlPlane-safe implementation.
- `IAppRoleManager` can reuse `AppRoleManager` if it only depends on `UserManager<AppUser>`.
- `IAppLogger<>`, email service/options if required by reused `AuthenticationService`.

Implementation choices:

| Option | Description | Decision |
| --- | --- | --- |
| A | Create `ControlPlaneUserManager` and `ControlPlaneTokenManager` that implement existing interfaces and use `ControlPlaneDbContext`. | Recommended: safest, no legacy behavior change. |
| B | Refactor existing `AppUserManager`/`AppTokenManager` to no longer depend on `AppDbContext`. | Viable later, but higher regression risk for legacy. |
| C | Generic managers over `TDbContext`. | More abstraction than needed right now. |

Acceptance:

- ControlPlane API can resolve `IAuthenticationService`.
- `IAppTokenManager` uses `ControlPlaneDbContext.RefreshTokens`.
- `IAppUserManager.GetAllUsersAsync()` does not query `AppDbContext.Users`.
- Legacy `BlazorShop.Web/API` still uses existing `AddSharedAuthenticationInfrastructure`.

## Phase 3 - Migration and local DB cleanup

Goal: produce a clean ControlPlane DB migration stream.

Tasks:

- [ ] Generate a new ControlPlane migration, for example `ControlPlaneIdentityAuth`.
- [ ] Review generated migration to ensure it adds Identity/Auth tables only plus existing ControlPlane tables.
- [ ] Confirm migration does not create:
  - `Products`
  - `Categories`
  - `Orders`
  - `PaymentMethods`
  - `SeoSettings`
  - `AdminSettings`
  - any other legacy commerce table.
- [ ] For local dev DB already contaminated by previous AppDbContext migration, reset `blazorshop_controlplane` or drop legacy tables manually.
- [ ] Document reset command in runbook.

Suggested local reset:

```powershell
docker exec blazorshop-controlplane-postgres psql -U blazorshop_controlplane -d postgres -c "drop database if exists blazorshop_controlplane;"
docker exec blazorshop-controlplane-postgres psql -U blazorshop_controlplane -d postgres -c "create database blazorshop_controlplane;"
dotnet ef database update --project BlazorShop.Infrastructure --startup-project BlazorShop.PresentationV2/BlazorShop.ControlPlane.API --context ControlPlaneDbContext
```

Acceptance SQL:

```sql
select tablename
from pg_tables
where schemaname = 'public'
order by tablename;
```

Expected groups:

- `AspNet*`
- `RefreshTokens`
- `control_plane_*`
- `commerce_node*`
- `node_*`
- `store_*`

Forbidden groups:

- `Products`
- `Categories`
- `Orders`
- `PaymentMethods`
- `Seo*`
- legacy admin/storefront tables not explicitly part of ControlPlane.

## Phase 4 - Admin seed for QA

Goal: make valid login and permission QA executable.

Tasks:

- [ ] Add a dev-only seed path for one platform owner admin.
- [ ] Seed an `AspNetUsers` row via `UserManager<AppUser>`, not raw SQL password hash.
- [ ] Assign Identity role `Admin` if reused claims generation still requires Identity role.
- [ ] Create matching `control_plane_admin_user` profile.
- [ ] Assign `control_plane_admin_user_role` to `platform_owner`.
- [ ] Keep password out of committed config; use user-secrets or environment variables.

Suggested config keys:

```json
{
  "ControlPlane": {
    "SeedAdmin": {
      "Enabled": true,
      "Email": "admin@example.local",
      "Password": "<user-secret>",
      "DisplayName": "Control Plane Admin"
    }
  }
}
```

Acceptance:

- `AspNetUsers` has one dev admin.
- Admin can log in through `/login`.
- `control_plane_admin_user.identity_user_id` matches `AspNetUsers.Id`.
- Admin has `platform_owner` permissions.

## Phase 5 - Auth controller and browser flow regression

Goal: existing login/logout UI continues to work after DB boundary correction.

Tasks:

- [ ] Keep `ControlPlaneAuthController` endpoint contract:
  - `POST api/control-plane/auth/login`
  - `POST api/control-plane/auth/refresh-token`
  - `POST api/control-plane/auth/logout`
  - `GET api/control-plane/auth/me`
- [ ] Keep no-session refresh behavior as non-error for browser bootstrap.
- [ ] Keep credentialed CORS for ControlPlane Web origin.
- [ ] Verify HttpOnly refresh cookie behavior under local HTTPS/HTTP decision.
- [ ] Verify failed login writes audit.
- [ ] Verify successful login creates or loads `control_plane_admin_user` profile.

Acceptance:

- Wrong password returns safe 400 and audit failure.
- No-session refresh does not produce browser console error.
- Valid login stores JWT client-side and sets refresh cookie server-side.
- Logout revokes refresh token and clears cookie.

## Phase 6 - Tests and QA checklist update

Goal: prevent this boundary regression from returning.

Automated checks:

- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj`
- [ ] Add/adjust tests for `ControlPlaneAuthController` login failure/success if test harness is available.
- [ ] Add test or script assertion that ControlPlane DB does not contain legacy commerce tables after migration.
- [ ] Browser QA:
  - `/login` loads with 0 console errors.
  - wrong password safe error.
  - valid admin login.
  - refresh on page reload.
  - `/nodes` anonymous redirects to `/login/nodes`.
  - logout clears session.

Update `QA-ControlPlane.todo.md`:

- Mark valid login unblocked after seed.
- Add explicit regression item: `ControlPlane API must not resolve AppDbContext`.
- Add explicit DB item: `ControlPlane DB must not contain legacy Commerce/Storefront tables`.

## Phase 7 - Commit plan

Commit per phase:

1. `docs(control-plane): plan isolated auth database boundary`
2. `refactor(control-plane): remove AppDbContext from auth startup`
3. `feat(control-plane): host identity auth in ControlPlaneDbContext`
4. `feat(control-plane): seed development platform owner`
5. `test(control-plane): cover isolated auth database boundary`
6. `docs(qa): update Control Plane auth isolation checks`

## File Impact Map

Expected edits:

| File | Change |
| --- | --- |
| `BlazorShop.Infrastructure/Data/ControlPlane/ControlPlaneDbContext.cs` | Inherit Identity context, add refresh tokens, configure Identity/Auth schema. |
| `BlazorShop.Infrastructure/Data/ControlPlane/DependencyInjection.cs` | Add/register ControlPlane authentication infrastructure or call new extension. |
| `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/Program.cs` | Replace `AddSharedAuthenticationInfrastructure` with ControlPlane-specific auth registration. |
| `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/ControlPlaneDatabaseBootstrapper.cs` | Migrate only `ControlPlaneDbContext`. |
| `BlazorShop.Infrastructure/Data/ControlPlane/Migrations/*` | Add Identity/Auth migration under ControlPlane migration stream. |
| `BlazorShop.Infrastructure/Repositories/Authentication/*` or new ControlPlane auth repository files | Add ControlPlane-safe `IAppUserManager` and `IAppTokenManager` implementations. |
| `docs/refactor-control-Commerce-storefront/QA-ControlPlane.todo.md` | Add regression checklist. |
| `docs/refactor-control-Commerce-storefront/control-plane-runbook.md` | Document clean DB reset and seed. |

Files that should not be required:

- `BlazorShop.Infrastructure/Data/AppDbContext.cs`
- legacy AppDbContext migrations
- legacy `BlazorShop.Presentation/BlazorShop.Web` UI files
- Commerce repositories/services

## Definition of Done

- `rg "AppDbContext" BlazorShop.PresentationV2/BlazorShop.ControlPlane.API BlazorShop.Infrastructure/Data/ControlPlane` has no runtime dependency except docs/comments if any.
- ControlPlane API starts with only PostgreSQL `5433`.
- ControlPlane migration creates Identity/Auth tables and ControlPlane tables, not legacy commerce tables.
- Login/logout works against `ControlPlaneDbContext`.
- Valid dev admin can access Dashboard/Nodes.
- Wrong-password, repeated wrong-password, refresh, logout, audit all pass QA.
- Legacy Commerce/Storefront build remains unaffected.

## Open Questions To Close Before Implementation

- Should `control_plane_admin_user.identity_user_id` have a hard FK to `AspNetUsers.Id` now, or stay logical-only to reduce migration risk?
- Should Identity roles `Admin/User` remain long term, or should `AuthenticationService` stop requiring Identity role when ControlPlane permissions are authoritative?
- Should local dev require HTTPS so `__Host-*` refresh cookie is fully standards-compliant, or allow dev-only non-secure cookie?
- Should first admin seed be CLI/user-secret driven, or manual SQL/script plus password reset flow?
