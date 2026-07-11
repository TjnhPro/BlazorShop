# BlazorShop V2 Startup Database Migration Plan

Date: 2026-07-11

## Decision

BlazorShop V2 will follow a Smartstore-style runtime migration model for MVP:

- Every published API image is treated as the latest compatible application version.
- On startup, each API checks its own EF Core database migration state.
- If pending migrations exist and startup migration is enabled, the API applies them before accepting traffic.
- No separate migrator image in this MVP.
- No CommerceNode Agent for database migration.
- No Control Plane "Update DB" button in this MVP.
- No custom SQL bundle runner in this MVP.

EF Core remains the migration engine. EF Core stores migration history in `__EFMigrationsHistory`; do not add a custom DB version table unless later UI/runtime status needs it.

## Scope

In scope:

- `ControlPlaneDbContext` startup migration in `BlazorShop.ControlPlane.API`.
- `CommerceNodeDbContext` startup migration in `BlazorShop.CommerceNode.API`.
- Production-safe configuration flags.
- Startup logging for applied and pending migrations.
- Production runbook notes.
- QA checklist for clean DB, existing DB, restart, and failure cases.

Out of scope:

- `AppDbContext` and legacy Presentation migration changes.
- Multi-node rolling migration support.
- Distributed migration lock beyond EF Core/runtime provider behavior.
- Automatic PostgreSQL backup execution by the API.
- Automatic Docker stop/start orchestration for DB migration.
- UI for migration management.
- Replacing EF Core migrations with FluentMigrator.

## Current State

Control Plane already has a minimal startup migrator:

- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/Program.cs`
- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/ControlPlaneDatabaseBootstrapper.cs`
- Config key: `ControlPlane:Database:MigrateOnStartup`
- Development default: true
- Production example default: false

Commerce Node does not yet have the same startup migration pattern:

- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Program.cs`
- `CommerceNodeDbContext` is registered through `AddCommerceNodeInfrastructure`.
- Development seeding currently runs in Development only, but migration is not called first.

## Database Ownership

| Runtime | Context | Connection | Dev port | Migration owner |
| --- | --- | --- | --- | --- |
| Control Plane API | `ControlPlaneDbContext` | `ControlPlaneConnection` | `5433` | `BlazorShop.ControlPlane.API` |
| Commerce Node API | `CommerceNodeDbContext` | `CommerceNodeConnection` | `5434` | `BlazorShop.CommerceNode.API` |
| Legacy API/Web | `AppDbContext` | `DefaultConnection` | `5432` | Legacy only, not part of V2 plan |

Rule: a runtime only migrates the database it owns. Cross-boundary migration is forbidden.

## Target Architecture

```text
ControlPlane.API startup
  -> validate production config
  -> if ControlPlane:Database:MigrateOnStartup
       -> create service scope
       -> resolve ControlPlaneDbContext
       -> log applied/pending migrations
       -> Database.MigrateAsync()
       -> dev seed only in Development
  -> start HTTP pipeline and hosted services

CommerceNode.API startup
  -> validate production config
  -> if CommerceNode:Database:MigrateOnStartup
       -> create service scope
       -> resolve CommerceNodeDbContext
       -> log applied/pending migrations
       -> Database.MigrateAsync()
       -> dev seed only in Development
  -> start HTTP pipeline and CommerceTaskWorker
```

Important sequencing:

- Migration must run before `app.Run()`.
- Commerce Node migration must run before `CommerceTaskWorker` starts processing tasks.
- If migration fails, startup fails. Do not let API run against a partially migrated schema.

## Configuration Design

Control Plane:

```json
{
  "ControlPlane": {
    "Database": {
      "MigrateOnStartup": true,
      "FailStartupOnMigrationError": true,
      "LogMigrationState": true
    }
  }
}
```

Commerce Node:

```json
{
  "CommerceNode": {
    "Database": {
      "MigrateOnStartup": true,
      "FailStartupOnMigrationError": true,
      "LogMigrationState": true
    }
  }
}
```

Recommended defaults:

| Environment | `MigrateOnStartup` | Reason |
| --- | --- | --- |
| Development | `true` | Fast local setup and QA with clean DB. |
| Production MVP single instance | `true` | Matches chosen Smartstore-style workflow. |
| Production multi-instance same DB | `false` until deploy flow gates one instance | Avoid concurrent startup migration risk. |

Production compose may set:

```text
ControlPlane__Database__MigrateOnStartup=true
CommerceNode__Database__MigrateOnStartup=true
```

## Startup Migration Service Design

Create or standardize a small bootstrapper per API boundary.

Control Plane:

- Keep `ControlPlaneDatabaseBootstrapper`.
- Expand it to log migration state.
- Keep Development seeding after successful migration.

Commerce Node:

- Add `CommerceNodeDatabaseBootstrapper`.
- Use `CommerceNodeDbContext`.
- Run Development seeding after successful migration.

Shared logic can be extracted only if duplication becomes meaningful. For MVP, two explicit bootstrappers are acceptable because each boundary has different seeding and config.

## Migration Flow

For each context:

1. Create DI scope.
2. Resolve `ILogger`.
3. Resolve owned DbContext.
4. Read applied migrations with `Database.GetAppliedMigrationsAsync()`.
5. Read pending migrations with `Database.GetPendingMigrationsAsync()`.
6. Log:
   - context name
   - connection name, not raw connection string
   - applied migration count
   - pending migration count
   - pending migration names
7. If no pending migrations:
   - log no-op
   - continue startup
8. If pending migrations exist:
   - log migration started
   - call `Database.MigrateAsync()`
   - log migration completed with elapsed time
9. If migration throws:
   - log error with context name and pending migrations
   - rethrow when `FailStartupOnMigrationError=true`

Do not log passwords or raw connection strings.

## Production Operator Workflow

Manual MVP workflow:

1. Backup the target PostgreSQL database manually before replacing the image.
   - Control Plane: backup `blazorshop_controlplane`.
   - Commerce Node: backup `blazorshop_commerce_node`.
2. Ensure only one API instance is starting against that database.
3. Pull/deploy latest API image.
4. Start/restart API container.
5. Watch logs for:
   - pending migration list
   - migration completed
   - health endpoint ready
6. If startup migration fails:
   - API container must fail startup.
   - review logs.
   - restore DB backup or rollback image manually.

This deliberately trades automated orchestration for simple MVP operations.

## EF Core Notes

Official EF Core guidance supports programmatic startup migration with:

```csharp
await dbContext.Database.MigrateAsync(cancellationToken);
```

Useful APIs:

- `Database.GetAppliedMigrationsAsync()`
- `Database.GetPendingMigrationsAsync()`
- `Database.MigrateAsync()`

Production risks to acknowledge:

- Runtime migration can block startup while schema changes run.
- Long data migrations can delay health checks.
- Multiple instances starting at the same time can create deployment risk, even though newer EF Core versions include better migration locking.
- EF Core 9 can throw when model changes exist without a generated migration; CI/build should catch this before deployment.

## CI / Pre-Publish Guardrails

Add later if needed, but record now as required production discipline:

- Run `dotnet build`.
- Run focused tests.
- Run EF pending model change check when available in the toolchain:

```powershell
dotnet ef migrations has-pending-model-changes --context ControlPlaneDbContext --project BlazorShop.Infrastructure --startup-project BlazorShop.PresentationV2/BlazorShop.ControlPlane.API
dotnet ef migrations has-pending-model-changes --context CommerceNodeDbContext --project BlazorShop.Infrastructure --startup-project BlazorShop.PresentationV2/BlazorShop.CommerceNode.API
```

If the command is unavailable due to EF tool version, the fallback is to generate/check migrations locally before publish and fail review if model changes are not represented by migration files.

## Phase Plan

### Phase 0 - Decision Documentation

- [x] Record the Smartstore-style startup migration decision.
- [x] Record out-of-scope alternatives.
- [x] Record DB ownership and runtime ownership.
- [x] Update architecture docs to mention startup migration rule.
- [x] Update `AGENTS.md` with a short migration rule so future agents do not propose migrator image by default.

Commit: `docs: document v2 startup migration strategy`

### Phase 1 - Control Plane Migration Hardening

- [x] Keep `ControlPlaneDatabaseBootstrapper`.
- [x] Add structured migration logs.
- [x] Log applied and pending migration names.
- [x] Add `ControlPlane:Database:FailStartupOnMigrationError`.
- [x] Add `ControlPlane:Database:LogMigrationState`.
- [x] Ensure Development seeding runs only after successful migration.
- [x] Ensure no raw connection string is logged.
- [x] Update `appsettings.Production.example.json` so MVP production can opt into startup migration intentionally.

Commit: `feat(controlplane): harden startup database migration`

### Phase 2 - Commerce Node Startup Migration

- [x] Add `CommerceNodeDatabaseBootstrapper`.
- [x] Add `CommerceNode:Database:MigrateOnStartup`.
- [x] Add `CommerceNode:Database:FailStartupOnMigrationError`.
- [x] Add `CommerceNode:Database:LogMigrationState`.
- [x] Run migration before Development seeding.
- [x] Run migration before hosted services process work.
- [x] Ensure `CommerceTaskWorker` does not start before schema migration completes.
- [x] Add production example config if Commerce Node does not have one yet.

Commit: `feat(commercenode): apply database migrations on startup`

### Phase 3 - Local Run Script Alignment

- [x] Ensure `scripts/run-v2-local.ps1` works with startup migration enabled.
- [x] Ensure clean Control Plane DB bootstraps from migrations.
- [x] Ensure clean Commerce Node DB bootstraps from migrations.
- [x] Ensure script output points users to migration logs when startup fails.
- [x] Ensure `scripts/stop-v2-local.ps1` remains the documented shutdown path.

Commit: `chore: align v2 local scripts with startup migrations`

### Phase 4 - Production Runbook

- [x] Update `docs/architecture/07-deployment-and-local-run.md`.
- [x] Add manual backup-before-deploy note.
- [x] Add single-instance migration requirement for MVP.
- [x] Add rollback guidance:
  - failed startup means API should stay down
  - inspect logs
  - restore backup or rollback image manually
- [x] Add warning for long/data-heavy migrations requiring manual review.

Commit: `docs: add production database migration runbook`

### Phase 5 - QA Checklist

- [ ] Update `QA-ControlPlane.todo.md`.
- [ ] Update `QA-CommerceNode.todo.md`.
- [ ] Add migration-specific checks:
  - clean DB startup
  - existing DB startup with no pending migrations
  - restart idempotency
  - invalid connection failure
  - bad migration failure policy
  - no raw secrets in logs
- [ ] Run focused QA on both APIs.

Commit: `test: add qa coverage for startup migrations`

## QA Checklist

Control Plane:

- [ ] Start with empty `blazorshop_controlplane` DB on port `5433`.
- [ ] Start `BlazorShop.ControlPlane.API` with `ControlPlane:Database:MigrateOnStartup=true`.
- [ ] Verify all Control Plane tables are created.
- [ ] Verify seed behavior still follows Development seed config.
- [ ] Restart API and verify no duplicate seed/migration side effects.
- [ ] Disable `MigrateOnStartup`, drop DB, start API, verify startup behavior is documented and understandable.
- [ ] Use invalid DB credentials and verify API does not silently run.

Commerce Node:

- [ ] Start with empty `blazorshop_commerce_node` DB on port `5434`.
- [ ] Start `BlazorShop.CommerceNode.API` with `CommerceNode:Database:MigrateOnStartup=true`.
- [ ] Verify commerce tables are created.
- [ ] Verify Development seeder runs after migration.
- [ ] Verify task worker starts only after migration succeeds.
- [ ] Restart API and verify no duplicate seed/migration side effects.
- [ ] Use invalid DB credentials and verify API does not silently run.

Cross-runtime:

- [ ] Verify `ControlPlaneDbContext` never migrates `CommerceNodeConnection`.
- [ ] Verify `CommerceNodeDbContext` never migrates `ControlPlaneConnection`.
- [ ] Verify `AppDbContext` is untouched.
- [ ] Verify logs show context name and migration names, but not passwords.
- [ ] Verify local scripts still use fixed ports.

## Autoplan Review Summary

### CEO Review

Score: 8/10.

This matches the MVP goal: one product, fast deploy, minimal moving parts. The rejected migrator-image/agent approach was overbuilt for the current single-node/single-operator workflow and introduced the self-stop problem for CommerceNode.

Main business risk: a bad migration can block startup. The mitigation is manual backup, single-instance deploy, clear logs, and fail-fast startup.

### Engineering Review

Score: 7/10 for MVP, not enough for mature multi-node production yet.

The plan is technically coherent because each API owns one DbContext and runs migrations before hosted services. The main tradeoff is that startup becomes part of the database upgrade path. Long migrations and multi-instance rollout must be handled manually until the deployment model matures.

### DX Review

Score: 8/10.

Local development gets simpler because clean databases can bootstrap automatically. Production remains understandable because the operator flow is backup, deploy latest image, start, check logs.

### Design Review

Skipped. No user-facing UI scope in this phase.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|---|---|---|---|---|---|
| 1 | Strategy | Use startup EF Core migration | Auto-decided | Simplicity | Matches Smartstore-style startup upgrade and current MVP operations. | Migrator image, CommerceNode Agent, Update DB button |
| 2 | Data ownership | Runtime migrates only its own DbContext | Auto-decided | Boundary correctness | Prevents ControlPlane/CommerceNode/AppDbContext ownership leaks. | Cross-context migration helper |
| 3 | Failure policy | Fail startup on migration error | Auto-decided | Safety | API must not run against partial or incompatible schema. | Continue startup after migration failure |
| 4 | Version tracking | Use EF Core `__EFMigrationsHistory` only | Auto-decided | Avoid duplication | EF already tracks applied migrations; custom version table is unnecessary now. | Custom DB version table |
| 5 | Production operation | Manual backup and single-instance deploy for MVP | Taste decision | Operational clarity | Simple enough for solo deployment and avoids false automation. | Automated backup/rollback orchestration |

## Final Gate Recommendation

Approve this plan as the implementation direction.

Before coding, confirm only one detail:

- Should production example config default `MigrateOnStartup` to `true` for this MVP, or stay `false` with docs telling the operator to enable it explicitly?

Recommended: keep checked-in production example default `false`, but document the MVP deployment env var as `true`. This prevents accidental production migration when someone copies the example without reading the runbook.
