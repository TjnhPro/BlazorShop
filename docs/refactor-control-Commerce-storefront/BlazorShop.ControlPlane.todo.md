# BlazorShop.ControlPlane.todo

Status: draft
Created: 2026-07-08
Branch: refactor-control-Commerce-storefront
Scope: Control Plane first, legacy `BlazorShop.Presentation` untouched, shared layers reused

## Decision

Build Control Plane as the first V2 feature using the existing Layered Architecture style. Do not refactor legacy `BlazorShop.Presentation` in this track.

Control Plane is new platform scope: node registry, node credentials, health snapshots, capabilities, store registry metadata, control actions, audit, and operator UI. It should reuse existing `BlazorShop.Application`, `BlazorShop.Infrastructure`, and `BlazorShop.Domain` patterns where they fit. Commerce product/order/cart/customer logic stays out of scope until Commerce Node V2 exists.

## Locked Implementation Choices

1. Auth: reuse existing auth logic from `BlazorShop.Application` instead of creating a separate Control Plane auth system.
2. UI: `BlazorShop.ControlPlane.Web` is Blazor WASM.
3. Database: PostgreSQL hosted locally with Docker for development.
4. Node authentication: simple API key MVP, stored hash-only.
5. Layering: Control Plane may reference and extend existing `BlazorShop.Application`, `BlazorShop.Infrastructure`, and `BlazorShop.Domain`.
6. Dashboard: only a minimal operational summary in the first implementation slice; no charts, alert rules, or advanced analytics until node registry and probes exist.
7. UI styling: use Tailwind CSS for layout/styling utilities and Font Awesome for icons in `BlazorShop.ControlPlane.Web`.
8. Web shared helpers: reuse `BlazorShop.Web.Shared` for generic WASM helpers instead of rewriting them, with an explicit allowlist.

## Premises

1. `BlazorShop.Presentation` is legacy and should be frozen except production bug fixes.
2. V2 development starts in a new presentation boundary, but core logic remains layered and reusable:

```text
BlazorShop.PresentationV2/
  BlazorShop.ControlPlane.API/
  BlazorShop.ControlPlane.Web/

BlazorShop.Application/
  ControlPlane/
    ... use cases, DTOs, contracts, validation

BlazorShop.Domain/
  Entities/ControlPlane/
    ... node registry, credentials, health, audit entities

BlazorShop.Infrastructure/
  Data/ControlPlane/
    ... EF configuration, repositories, PostgreSQL integration
```

3. Control Plane reuses the existing auth/application patterns. Auth should be shared through `BlazorShop.Application` contracts/services to avoid duplicate login/session logic.
4. Control Plane owns platform management data only. It never connects directly to a Commerce Node commerce database.
5. Commerce Node is treated as a remote target in Phase 1. Real Commerce Node implementation is deferred.
6. PostgreSQL is the Control Plane persistence target and is hosted locally with Docker for development.
7. Control Plane Web is Blazor WASM.
8. Node authentication starts with a simple API key model. HMAC can be introduced later if the operational threat model requires it.

## Not In Scope

- No edits to `BlazorShop.Presentation`.
- No migration of product, category, cart, checkout, payment, order, customer auth, inventory, media, or SEO commerce data.
- No StorefrontV2 implementation.
- No real Commerce Node implementation beyond mocked contract fixtures.
- No production secrets in appsettings.
- No multi-store schema inside legacy commerce DB.
- No removal of legacy projects from the solution.
- No clean-room rewrite of Application/Domain/Infrastructure.

## 12-Month Target

```text
CURRENT
  Legacy API/Web/Storefront mixed in BlazorShop.Presentation.
  Admin tools are commerce-local.
  No platform-level node registry.

THIS PLAN
  New Control Plane V2 stands up as new presentation hosts.
  Existing layered core is extended with ControlPlane slices.
  Operators can register nodes, inspect health/capabilities,
  manage node credentials, view audit, and prepare store registry metadata.

12-MONTH IDEAL
  Control Plane manages many Commerce Nodes.
  Each node exposes signed control APIs.
  StorefrontV2 resolves stores through Control Plane metadata and renders via Commerce Node internal APIs.
  Legacy Presentation can be removed after V2 reaches behavior parity where needed.
```

## Architecture

```text
Operator Browser
    |
    v
ControlPlane.Web  -- typed client -->  ControlPlane.API
                                      |
                                      v
                              ControlPlane DB
                                      |
                                      | scheduled probes / operator actions
                                      v
                         Commerce Node /api/controlpanel/*
```

Project and layer responsibilities:

| Project | Responsibility |
| --- | --- |
| `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API` | HTTP boundary for Control Plane, OpenAPI, auth policies, rate limits, health. |
| `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web` | Blazor WASM operator UI for node registry, health, capabilities, credentials, stores, audit. |
| `BlazorShop.Application/ControlPlane` | Use cases, DTOs, validators, authorization intent, service contracts. Reuse existing auth contracts where possible. |
| `BlazorShop.Domain/Entities/ControlPlane` | Node registry, credentials, health, capability, store registry, control action, audit entities. |
| `BlazorShop.Infrastructure/Data/ControlPlane` | EF Core mapping, repositories, PostgreSQL integration, credential hashing, HTTP clients, background probe jobs. |
| `BlazorShop.ServiceDefaults` | Shared hosting defaults, service discovery, health checks, resilience where applicable. |

Recommended API style:

- Use controller-based Web API for Control Plane API to stay consistent with existing `BlazorShop.API` patterns.
- Keep controllers thin and move business logic into `BlazorShop.Application/ControlPlane`.
- Do not mix controllers and Minimal APIs within the same Control Plane feature slice.

## UI Design Stack

`BlazorShop.ControlPlane.Web` should use a fresh V2 admin UI stack:

- Tailwind CSS owns layout, spacing, typography, color tokens, responsive behavior, tables, forms, badges, and empty states.
- Font Awesome owns iconography for navigation, status, actions, and destructive/credential flows.
- Shared Blazor components should be small and operational: `AppShell`, `SidebarNav`, `PageHeader`, `MetricCard`, `StatusBadge`, `DataTable`, `FilterBar`, `ConfirmDialog`, `DetailDrawer`, `EmptyState`, `ErrorState`.
- Avoid importing legacy `BlazorShop.Presentation` UI components into Control Plane.
- Keep the visual style dense, quiet, and admin-focused. No marketing hero sections.
- Use icons plus short labels for primary navigation; use icon-only buttons only when the action is common and has a tooltip.
- Status color semantics:
  - healthy: green
  - warning/degraded: amber
  - down/unhealthy: red
  - disabled/archived: neutral gray
  - running/pending: blue

## Shared Web Helper Reuse

`BlazorShop.ControlPlane.Web` should reuse `BlazorShop.Web.Shared` where the code is generic and not commerce-feature-specific.

Allowed reuse:

- `ApiCallHelper`, `HttpClientHelper`, and their contracts for consistent API call behavior.
- `TokenService` and token storage contracts, if compatible with the reused auth flow.
- Browser storage and cookie storage wrappers.
- JS module helper infrastructure.
- Toast service, toast models, and toast options.
- Generic response wrappers such as `ServiceResponse<T>`, `QueryResult<T>`, and `PagedResult<T>` if the Control Plane API response contract remains compatible.

Do not reuse in Control Plane:

- Product, category, cart, payment, order, inventory, SEO, newsletter, storefront, or commerce-admin service clients.
- Commerce-specific route constants from `Constant`.
- Commerce-specific models as Control Plane DTOs.
- Any component or asset that couples Control Plane to legacy `BlazorShop.Presentation` screens.

Recommended boundary:

- Register only the allowed shared helper services in `BlazorShop.ControlPlane.Web`.
- Add Control Plane-specific API clients on top of the generic HTTP helpers.
- If the shared helper project becomes a long-term dependency, move or alias the project as a first-class shared project later without changing behavior. Do not block Phase 1 on that move.

## Database Design

Naming conventions:

- Tables and columns use lowercase snake_case.
- Primary keys use `bigint generated always as identity` unless the ID is exposed across node boundaries.
- Public/external correlation identifiers use `uuid`, preferably UUIDv7 if the chosen Postgres environment supports it.
- All timestamps are `timestamptz`.
- All foreign keys must have explicit indexes.
- Use partial indexes for active/non-revoked rows.
- Use cursor pagination for audit, health, command, and event lists.

### Entity Relationship

```text
control_plane_admin_user
    |
    +-- control_plane_admin_user_role -- control_plane_role -- control_plane_role_permission -- control_plane_permission
    |
commerce_node
    |
    +-- commerce_node_endpoint
    +-- commerce_node_credential
    +-- node_health_snapshot
    +-- node_capability_snapshot
    +-- store_registry
    |       |
    |       +-- store_domain_registry
    |
    +-- control_action
            |
            +-- control_action_attempt

control_audit_log references actor, node, store, action where available.
```

### Tables

#### `control_plane_admin_user`

Control Plane operator account profile. If ASP.NET Core Identity tables are used, keep this as the platform profile table linked to Identity user id.

| Column | Type | Required | Notes |
| --- | --- | --- | --- |
| `id` | bigint identity PK | yes | Internal profile id. |
| `identity_user_id` | text | yes | Maps to ASP.NET Core Identity user id. Unique. |
| `email` | text | yes | Normalized by application. Unique among active users. |
| `display_name` | text | yes | Operator display name. |
| `status` | text | yes | `active`, `disabled`, `invited`. |
| `last_login_at` | timestamptz | no | Updated on successful sign-in. |
| `created_at` | timestamptz | yes | Default `now()`. |
| `updated_at` | timestamptz | yes | Updated by application. |
| `deleted_at` | timestamptz | no | Soft delete. |

Constraints/indexes:

```sql
check (status in ('active', 'disabled', 'invited'))
unique (identity_user_id)
create unique index control_plane_admin_user_active_email_uq
  on control_plane_admin_user (lower(email))
  where deleted_at is null;
```

#### `control_plane_role`

| Column | Type | Required | Notes |
| --- | --- | --- | --- |
| `id` | bigint identity PK | yes | |
| `key` | text | yes | Stable key: `platform_owner`, `node_operator`, `auditor`. |
| `name` | text | yes | Human label. |
| `description` | text | no | |
| `is_system` | boolean | yes | System roles cannot be deleted. |
| `created_at` | timestamptz | yes | |
| `updated_at` | timestamptz | yes | |

Constraints/indexes:

```sql
unique (key)
check (key = lower(key))
```

#### `control_plane_permission`

| Column | Type | Required | Notes |
| --- | --- | --- | --- |
| `id` | bigint identity PK | yes | |
| `key` | text | yes | Example: `nodes.read`, `nodes.write`, `credentials.rotate`, `audit.read`. |
| `description` | text | no | |
| `created_at` | timestamptz | yes | |

Constraints/indexes:

```sql
unique (key)
check (key = lower(key))
```

#### `control_plane_admin_user_role`

| Column | Type | Required | Notes |
| --- | --- | --- | --- |
| `admin_user_id` | bigint FK | yes | FK to `control_plane_admin_user`. |
| `role_id` | bigint FK | yes | FK to `control_plane_role`. |
| `assigned_by_admin_user_id` | bigint FK | no | Who assigned it. |
| `assigned_at` | timestamptz | yes | |

Constraints/indexes:

```sql
primary key (admin_user_id, role_id)
create index control_plane_admin_user_role_role_id_idx on control_plane_admin_user_role (role_id);
create index control_plane_admin_user_role_assigned_by_idx on control_plane_admin_user_role (assigned_by_admin_user_id);
```

#### `control_plane_role_permission`

| Column | Type | Required | Notes |
| --- | --- | --- | --- |
| `role_id` | bigint FK | yes | |
| `permission_id` | bigint FK | yes | |
| `created_at` | timestamptz | yes | |

Constraints/indexes:

```sql
primary key (role_id, permission_id)
create index control_plane_role_permission_permission_id_idx on control_plane_role_permission (permission_id);
```

#### `commerce_node`

Registry row for one remote Commerce Node.

| Column | Type | Required | Notes |
| --- | --- | --- | --- |
| `id` | bigint identity PK | yes | Internal id. |
| `public_id` | uuid | yes | Exposed id for URLs/log correlation. Prefer UUIDv7. |
| `node_key` | text | yes | Stable slug: `vn-main`, `us-east-1`. |
| `display_name` | text | yes | |
| `region_code` | text | no | Example: `VN`, `US`, `EU`, `SG`. |
| `environment` | text | yes | `dev`, `staging`, `production`. |
| `status` | text | yes | `pending`, `active`, `disabled`, `unhealthy`, `retired`. |
| `owner_note` | text | no | Operator note. |
| `last_seen_at` | timestamptz | no | Last successful health probe. |
| `created_by_admin_user_id` | bigint FK | yes | |
| `created_at` | timestamptz | yes | |
| `updated_at` | timestamptz | yes | |
| `deleted_at` | timestamptz | no | |

Constraints/indexes:

```sql
unique (public_id)
create unique index commerce_node_active_node_key_uq
  on commerce_node (node_key)
  where deleted_at is null;
check (environment in ('dev', 'staging', 'production'))
check (status in ('pending', 'active', 'disabled', 'unhealthy', 'retired'))
create index commerce_node_status_updated_idx on commerce_node (status, updated_at desc);
create index commerce_node_created_by_idx on commerce_node (created_by_admin_user_id);
```

#### `commerce_node_endpoint`

Separate endpoint table so a node can expose control, internal, and public storefront URLs without overloading `commerce_node`.

| Column | Type | Required | Notes |
| --- | --- | --- | --- |
| `id` | bigint identity PK | yes | |
| `commerce_node_id` | bigint FK | yes | |
| `endpoint_kind` | text | yes | `control_api`, `internal_api`, `public_storefront`, `health`. |
| `base_url` | text | yes | Absolute HTTPS URL except local dev. |
| `is_primary` | boolean | yes | One primary per kind. |
| `status` | text | yes | `active`, `disabled`. |
| `created_at` | timestamptz | yes | |
| `updated_at` | timestamptz | yes | |

Constraints/indexes:

```sql
check (endpoint_kind in ('control_api', 'internal_api', 'public_storefront', 'health'))
check (status in ('active', 'disabled'))
create index commerce_node_endpoint_node_kind_idx on commerce_node_endpoint (commerce_node_id, endpoint_kind);
create unique index commerce_node_endpoint_primary_kind_uq
  on commerce_node_endpoint (commerce_node_id, endpoint_kind)
  where is_primary = true and status = 'active';
```

#### `commerce_node_credential`

Stores metadata and non-reversible secret material for Control Plane to authenticate with Commerce Node.

| Column | Type | Required | Notes |
| --- | --- | --- | --- |
| `id` | bigint identity PK | yes | |
| `commerce_node_id` | bigint FK | yes | |
| `credential_key_id` | text | yes | Public key id sent in request header. |
| `credential_kind` | text | yes | `api_key`, later `hmac`. |
| `secret_hash` | text | yes | Hash only. Never store raw API key. |
| `secret_hash_algorithm` | text | yes | Example: `hmac-sha256`, `argon2id`. |
| `status` | text | yes | `active`, `pending_rotation`, `revoked`, `expired`. |
| `created_by_admin_user_id` | bigint FK | yes | |
| `rotated_from_credential_id` | bigint FK | no | Previous credential. |
| `last_used_at` | timestamptz | no | Updated by successful call if observable. |
| `expires_at` | timestamptz | no | Required for temporary credentials. |
| `revoked_at` | timestamptz | no | |
| `created_at` | timestamptz | yes | |

Constraints/indexes:

```sql
unique (credential_key_id)
check (credential_kind in ('api_key', 'hmac'))
check (status in ('active', 'pending_rotation', 'revoked', 'expired'))
create index commerce_node_credential_node_status_idx on commerce_node_credential (commerce_node_id, status);
create index commerce_node_credential_created_by_idx on commerce_node_credential (created_by_admin_user_id);
create index commerce_node_credential_rotated_from_idx on commerce_node_credential (rotated_from_credential_id);
create unique index commerce_node_credential_one_active_uq
  on commerce_node_credential (commerce_node_id)
  where status = 'active';
```

Phase note: `one_active_uq` can be relaxed during key rotation if overlapping active keys are required. If overlap is needed, replace with `(commerce_node_id, credential_key_id)` uniqueness and enforce max active count in application logic.

#### `node_health_snapshot`

Append-only health observation table. Use cursor pagination, never offset pagination.

| Column | Type | Required | Notes |
| --- | --- | --- | --- |
| `id` | bigint identity PK | yes | |
| `commerce_node_id` | bigint FK | yes | |
| `observed_at` | timestamptz | yes | |
| `status` | text | yes | `healthy`, `degraded`, `unhealthy`, `unknown`. |
| `latency_ms` | integer | no | |
| `http_status_code` | integer | no | |
| `version` | text | no | Node app version. |
| `error_code` | text | no | Stable error code. |
| `error_message` | text | no | Sanitized, operator-facing. |
| `raw_payload` | jsonb | no | Sanitized health response. |
| `created_at` | timestamptz | yes | |

Constraints/indexes:

```sql
check (status in ('healthy', 'degraded', 'unhealthy', 'unknown'))
check (latency_ms is null or latency_ms >= 0)
create index node_health_snapshot_node_observed_idx
  on node_health_snapshot (commerce_node_id, observed_at desc, id desc);
create index node_health_snapshot_status_observed_idx
  on node_health_snapshot (status, observed_at desc);
```

Retention:

- Keep full-resolution snapshots for 30 days.
- Aggregate or prune older records in a later operations phase.
- Partition by month only after snapshot volume proves it is needed.

#### `node_capability_snapshot`

Current and historical node capability documents.

| Column | Type | Required | Notes |
| --- | --- | --- | --- |
| `id` | bigint identity PK | yes | |
| `commerce_node_id` | bigint FK | yes | |
| `observed_at` | timestamptz | yes | |
| `schema_version` | text | yes | Contract schema version. |
| `app_version` | text | no | Node app version. |
| `capabilities` | jsonb | yes | Sanitized capability set. |
| `checksum` | text | yes | Hash of normalized capability JSON. |
| `is_current` | boolean | yes | One current snapshot per node. |
| `created_at` | timestamptz | yes | |

Constraints/indexes:

```sql
create index node_capability_snapshot_node_observed_idx
  on node_capability_snapshot (commerce_node_id, observed_at desc, id desc);
create unique index node_capability_snapshot_current_uq
  on node_capability_snapshot (commerce_node_id)
  where is_current = true;
create index node_capability_snapshot_capabilities_gin_idx
  on node_capability_snapshot using gin (capabilities);
```

#### `store_registry`

Control Plane metadata for stores known across nodes. This is not the commerce store database.

| Column | Type | Required | Notes |
| --- | --- | --- | --- |
| `id` | bigint identity PK | yes | |
| `public_id` | uuid | yes | Exposed id. |
| `commerce_node_id` | bigint FK | yes | Owning node. |
| `store_key` | text | yes | Stable slug within platform. |
| `remote_store_id` | text | no | Store id as known by Commerce Node. |
| `display_name` | text | yes | |
| `status` | text | yes | `draft`, `active`, `suspended`, `archived`. |
| `primary_domain` | text | no | Denormalized for list display. |
| `created_by_admin_user_id` | bigint FK | yes | |
| `created_at` | timestamptz | yes | |
| `updated_at` | timestamptz | yes | |
| `deleted_at` | timestamptz | no | |

Constraints/indexes:

```sql
unique (public_id)
create unique index store_registry_active_store_key_uq
  on store_registry (store_key)
  where deleted_at is null;
create index store_registry_node_status_idx on store_registry (commerce_node_id, status);
create index store_registry_created_by_idx on store_registry (created_by_admin_user_id);
check (status in ('draft', 'active', 'suspended', 'archived'))
```

#### `store_domain_registry`

| Column | Type | Required | Notes |
| --- | --- | --- | --- |
| `id` | bigint identity PK | yes | |
| `store_registry_id` | bigint FK | yes | |
| `domain_name` | text | yes | Lowercase punycode-normalized domain. |
| `domain_kind` | text | yes | `primary`, `alias`, `preview`. |
| `status` | text | yes | `pending`, `verified`, `failed`, `disabled`. |
| `verified_at` | timestamptz | no | |
| `created_at` | timestamptz | yes | |
| `updated_at` | timestamptz | yes | |

Constraints/indexes:

```sql
create unique index store_domain_registry_active_domain_uq
  on store_domain_registry (domain_name)
  where status in ('pending', 'verified');
create index store_domain_registry_store_idx on store_domain_registry (store_registry_id);
check (domain_kind in ('primary', 'alias', 'preview'))
check (status in ('pending', 'verified', 'failed', 'disabled'))
```

#### `control_action`

Tracks operator-initiated or scheduled control actions sent to nodes.

| Column | Type | Required | Notes |
| --- | --- | --- | --- |
| `id` | bigint identity PK | yes | |
| `public_id` | uuid | yes | Correlation id exposed in UI/logs. |
| `commerce_node_id` | bigint FK | yes | |
| `store_registry_id` | bigint FK | no | Null for node-level action. |
| `action_kind` | text | yes | `probe_health`, `fetch_capabilities`, `create_store`, `sync_store_settings`, `rotate_credential`. |
| `status` | text | yes | `queued`, `running`, `succeeded`, `failed`, `cancelled`. |
| `requested_by_admin_user_id` | bigint FK | no | Null for system-scheduled. |
| `request_payload` | jsonb | no | Sanitized. No secrets. |
| `response_payload` | jsonb | no | Sanitized. No secrets. |
| `error_code` | text | no | Stable code. |
| `error_message` | text | no | Operator-safe. |
| `idempotency_key` | text | no | Prevent duplicate dispatch. |
| `queued_at` | timestamptz | yes | |
| `started_at` | timestamptz | no | |
| `completed_at` | timestamptz | no | |
| `created_at` | timestamptz | yes | |

Constraints/indexes:

```sql
unique (public_id)
create unique index control_action_idempotency_uq
  on control_action (commerce_node_id, idempotency_key)
  where idempotency_key is not null;
create index control_action_node_status_queued_idx
  on control_action (commerce_node_id, status, queued_at desc, id desc);
create index control_action_store_idx on control_action (store_registry_id);
create index control_action_requested_by_idx on control_action (requested_by_admin_user_id);
check (status in ('queued', 'running', 'succeeded', 'failed', 'cancelled'))
```

#### `control_action_attempt`

Stores retry attempts separately from logical action.

| Column | Type | Required | Notes |
| --- | --- | --- | --- |
| `id` | bigint identity PK | yes | |
| `control_action_id` | bigint FK | yes | |
| `attempt_number` | integer | yes | Starts at 1. |
| `status` | text | yes | `running`, `succeeded`, `failed`, `timeout`. |
| `http_status_code` | integer | no | |
| `latency_ms` | integer | no | |
| `error_code` | text | no | |
| `error_message` | text | no | |
| `started_at` | timestamptz | yes | |
| `completed_at` | timestamptz | no | |

Constraints/indexes:

```sql
unique (control_action_id, attempt_number)
create index control_action_attempt_action_idx on control_action_attempt (control_action_id);
check (attempt_number > 0)
check (latency_ms is null or latency_ms >= 0)
check (status in ('running', 'succeeded', 'failed', 'timeout'))
```

#### `control_audit_log`

Append-only audit log for operator actions, credential operations, node changes, and security-sensitive reads.

| Column | Type | Required | Notes |
| --- | --- | --- | --- |
| `id` | bigint identity PK | yes | |
| `public_id` | uuid | yes | Exposed audit id. |
| `actor_admin_user_id` | bigint FK | no | Null for system. |
| `actor_type` | text | yes | `admin_user`, `system`, `node`. |
| `action` | text | yes | Stable action key. |
| `entity_type` | text | yes | `commerce_node`, `credential`, `store_registry`, `role`, etc. |
| `entity_id` | text | no | Store as text to support mixed ids. |
| `commerce_node_id` | bigint FK | no | Filter shortcut. |
| `store_registry_id` | bigint FK | no | Filter shortcut. |
| `control_action_id` | bigint FK | no | Link to dispatched action. |
| `ip_address` | inet | no | Operator request IP. |
| `user_agent` | text | no | |
| `correlation_id` | text | no | Request correlation id. |
| `before_data` | jsonb | no | Redacted. |
| `after_data` | jsonb | no | Redacted. |
| `metadata` | jsonb | no | Redacted. |
| `created_at` | timestamptz | yes | |

Constraints/indexes:

```sql
unique (public_id)
create index control_audit_log_created_idx on control_audit_log (created_at desc, id desc);
create index control_audit_log_actor_created_idx on control_audit_log (actor_admin_user_id, created_at desc, id desc);
create index control_audit_log_node_created_idx on control_audit_log (commerce_node_id, created_at desc, id desc);
create index control_audit_log_entity_idx on control_audit_log (entity_type, entity_id);
create index control_audit_log_correlation_idx on control_audit_log (correlation_id);
check (actor_type in ('admin_user', 'system', 'node'))
```

## Control API Contract Draft

Commerce Node endpoints consumed by Control Plane:

```text
GET  /api/controlpanel/health
GET  /api/controlpanel/version
GET  /api/controlpanel/capabilities
POST /api/controlpanel/stores
PUT  /api/controlpanel/stores/{storeId}/settings
POST /api/controlpanel/stores/{storeId}/sync
POST /api/controlpanel/credentials/verify
```

Required request headers:

```text
X-ControlPlane-KeyId: <credential_key_id>
X-ControlPlane-ApiKey: <one-time generated API key value>
X-Correlation-Id: <trace id>
```

MVP uses API key authentication only. Store only the key hash in PostgreSQL. HMAC signed requests are deferred and should not complicate Phase 1.

## Phase Plan

### Phase 0 - Plan Lock

- [ ] Accept this plan as the Control Plane V2 scope baseline.
- [ ] Confirm `BlazorShop.Presentation` is frozen.
- [ ] Confirm file/project naming: `BlazorShop.PresentationV2/BlazorShop.ControlPlane.*`.
- [ ] Confirm shared layered-core strategy: add ControlPlane slices under existing `Domain`, `Application`, and `Infrastructure`.
- [ ] Confirm PostgreSQL via Docker as Control Plane database.
- [ ] Confirm shared auth through existing `BlazorShop.Application` auth contracts/services.
- [ ] Confirm Blazor WASM for `BlazorShop.ControlPlane.Web`.
- [ ] Confirm simple API key for node authentication MVP.

Acceptance:

- No code changed.
- No legacy `BlazorShop.Presentation` project changed.
- This todo file is the source of truth for Phase 1 implementation.

### Phase 1 - Solution Skeleton

- [x] Create `BlazorShop.PresentationV2` directory.
- [x] Create `BlazorShop.ControlPlane.API` ASP.NET Core Web API.
- [x] Create `BlazorShop.ControlPlane.Web` Blazor WASM admin UI.
- [x] Configure Tailwind CSS for `BlazorShop.ControlPlane.Web`.
- [x] Configure Font Awesome for `BlazorShop.ControlPlane.Web`.
- [x] Add projects to solution.
- [x] Reference `BlazorShop.Web.Shared` from `BlazorShop.ControlPlane.Web` for approved generic helper reuse.
- [x] Register only approved shared helper services: API call, HTTP client, token, browser storage, cookie storage, JS module, toast.
- [x] Add `BlazorShop.Application/ControlPlane` folder for use cases, DTOs, contracts, validators.
- [x] Add `BlazorShop.Domain/Entities/ControlPlane` folder for new Control Plane entities.
- [x] Add `BlazorShop.Infrastructure/Data/ControlPlane` folder for EF configuration and repositories.
- [x] Wire Control Plane API to existing `AddApplication`/`AddInfrastructure` style, extending registration with ControlPlane services.
- [x] Wire Control Plane Web to existing WASM auth/token/client helper patterns where compatible.
- [x] Reference `BlazorShop.ServiceDefaults` from API.
- [x] Keep `BlazorShop.ControlPlane.Web` WASM free of server-only ServiceDefaults dependencies.
- [x] Add `appsettings.Development.json` examples with no secrets.
- [x] Add `compose.controlplane.yml` or extend local compose docs for PostgreSQL development database.
- [x] Add health endpoints for API.

Acceptance:

- `dotnet build BlazorShop.sln` passes.
- Control Plane API starts independently.
- Control Plane WASM starts independently and can call the API base URL from configuration.
- Tailwind and Font Awesome load in the WASM project without depending on legacy Presentation assets.
- Control Plane Web reuses approved `BlazorShop.Web.Shared` helpers without registering commerce feature service clients.
- Control Plane Web does not reference server-only ServiceDefaults.
- Legacy `BlazorShop.Presentation` remains untouched.
- Existing shared layers compile with additive ControlPlane folders.

### Phase 2 - Control Plane Database Foundation

- [x] Add Control Plane DbContext in Infrastructure.
- [x] Add EF Core migrations for admin profile, roles, permissions, node registry, endpoints, credentials, health, capabilities, stores, actions, attempts, audit.
- [x] Use snake_case table and column names.
- [x] Add indexes listed in this plan.
- [x] Add seed data for system roles and permissions.
- [x] Add development seed for one local mock node.
- [x] Add Docker PostgreSQL local setup and documented connection string.
- [x] Add migration test that validates FK indexes exist.
- [x] Add test that no credential raw secret is persisted.

Acceptance:

- Migration creates all Control Plane tables.
- All FK columns are indexed.
- Audit and health list queries use keyset/cursor pagination shape.
- No table stores raw API key material.

Verification:

- `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj` passes with 0 warnings.
- `dotnet ef database update --context ControlPlaneDbContext` applied to Docker PostgreSQL.
- PostgreSQL verification found 15 Control Plane tables, `pgcrypto`, 3 seeded roles, 8 seeded permissions, 20 role-permission mappings, and 0 missing FK indexes.
- Added opt-in `ControlPlaneDevelopmentSeeder` for one local mock node without polluting production migrations.
- Added model tests for indexed FK columns and hash-only credential persistence.

### Phase 3 - Operator Auth And Authorization

- [x] Reuse existing auth flow from `BlazorShop.Application` where possible.
- [x] Reuse existing token/session/client helper patterns where compatible with Control Plane Web WASM.
- [x] Implement profile mapping to `control_plane_admin_user`.
- [x] Implement policies for `nodes.read`, `nodes.write`, `credentials.rotate`, `stores.read`, `stores.write`, `audit.read`.
- [x] Add authorization tests for every Control Plane API route group/controller.
- [x] Add audit logging for sign-in/session actions.
- [ ] Add audit logging for role changes when role management endpoints exist.
- [ ] Add audit logging for credential creation and credential rotation in Phase 5.
- [ ] Add audit logging for node status changes in Phase 4 and Phase 6.

Acceptance:

- Anonymous callers cannot access Control Plane API except health/live endpoints.
- Disabled users cannot act.
- Permission gaps return 403, not 500.
- Audit logs are written for security-sensitive actions.
- Auth integration does not fork duplicate login/business logic from existing `BlazorShop.Application`.

Verification:

- `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj` passes with 0 warnings.
- `dotnet build BlazorShop.Presentation/BlazorShop.API/BlazorShop.API.csproj` still passes; only existing legacy `Microsoft.OpenApi` package warning remains.
- `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter FullyQualifiedName~ControlPlaneAuthorizationTests` passes 5 tests.
- Control Plane API now calls shared auth infrastructure, runs `UseAuthentication()` before `UseAuthorization()`, and uses Control Plane profile/permission policies backed by PostgreSQL.

### Phase 4 - Node Registry

- [x] Implement create/update/disable node use cases.
- [x] Implement endpoint management for control API URL.
- [x] Validate node key uniqueness and URL shape.
- [x] Add cursor-paginated node list.
- [x] Add node detail endpoint.
- [x] Add UI pages: Nodes list, Create node, Node detail, Disable node.
- [x] Add empty/loading/error states in UI.

Acceptance:

- Operator can register a node without touching Commerce Node code.
- Duplicate active `node_key` fails predictably.
- Disabled node cannot be updated through the node registry. Control action enqueue enforcement is deferred to Phase 8 because `control_action` dispatch does not exist yet.
- UI shows clear state when no nodes exist.

Verification:

- `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj` passes with 0 warnings.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj` passes with 0 warnings; Tailwind emits only the existing Browserslist database warning.
- `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~ControlPlaneNodeServiceTests|FullyQualifiedName~ControlPlaneAuthorizationTests"` passes 9 tests.
- Node registry API now exposes create, update, disable, list, and detail endpoints behind `nodes.read`/`nodes.write` policies.
- Control Plane Web nodes page now uses the shared WASM HTTP helper stack and shows real API-backed loading, empty, validation, access-denied, and error states.

### Phase 5 - Credential Lifecycle

- [x] Generate credential key id and one-time raw secret.
- [x] Store only hash and metadata.
- [x] Show raw secret once in UI.
- [x] Implement credential revoke.
- [x] Implement credential rotation draft flow.
- [x] Add audit for create, reveal, revoke, rotate.
- [x] Add tests for one-time secret reveal behavior.

Acceptance:

- Raw credential cannot be read back after creation.
- Revoked and rotated credentials cannot pass the Control Plane credential verifier. Full mock Commerce Node HTTP client enforcement remains in Phase 6.
- Audit shows who created/revoked/rotated a credential.
- No HMAC signing is implemented in this phase.

Verification:

- `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj` passes with 0 warnings.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj` passes with 0 warnings; Tailwind emits only the existing Browserslist database warning.
- `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~ControlPlaneCredentialServiceTests|FullyQualifiedName~ControlPlaneAuthorizationTests"` passes 9 tests.
- Credential API now exposes list, create, revoke, and rotate endpoints behind `credentials.rotate`.
- Control Plane Web has a Credentials page with node selection, empty/loading/error states, one-time secret callout, revoke, and rotate actions.

### Phase 6 - Health And Capabilities

- [ ] Implement typed HTTP client for Commerce Node control endpoints.
- [ ] Implement mock Commerce Node handler for tests.
- [ ] Implement manual health probe.
- [ ] Implement scheduled health probe background service.
- [ ] Persist `node_health_snapshot`.
- [ ] Persist `node_capability_snapshot` only when checksum changes or after explicit refresh.
- [ ] Update `commerce_node.last_seen_at` and status from probe results.
- [ ] Add UI: health timeline, latest status, capabilities viewer.

Acceptance:

- Healthy, degraded, unhealthy, timeout, malformed payload paths are tested.
- Probe failures produce operator-safe error messages.
- Repeated identical capability responses do not create noisy current snapshots.
- Node list can filter by status.

### Phase 7 - Store Registry Metadata

- [ ] Implement store registry create/update/archive.
- [ ] Implement store domain registry create/verify/disable placeholder flow.
- [ ] Link stores to node.
- [ ] Do not write to Commerce Node commerce DB.
- [ ] Add UI: Stores list, Store detail, Domains panel.
- [ ] Add audit for store metadata changes.

Acceptance:

- Store metadata can exist before Commerce Node supports real store creation.
- Domain uniqueness prevents two active stores claiming same domain.
- Archived store does not appear in active selectors by default.

### Phase 8 - Control Actions

- [ ] Implement `control_action` enqueue for `probe_health`, `fetch_capabilities`, and placeholder store sync.
- [ ] Implement `control_action_attempt` retry recording.
- [ ] Add idempotency key handling.
- [ ] Add UI: Actions list and action detail.
- [ ] Add correlation id propagation.
- [ ] Add tests for retry, timeout, cancellation, duplicate idempotency.

Acceptance:

- Duplicate action with same node/idempotency key does not dispatch twice.
- Every attempt is visible in action detail.
- Failed actions have problem, cause, and suggested fix.

### Phase 9 - Operator Dashboard

- [ ] Full dashboard analytics are deferred.
- [ ] Add the minimum shell/navigation needed to reach implemented pages.
- [ ] Add dashboard counters: Total Nodes, Healthy Nodes, Warning Nodes, Down Nodes, Total Stores.
- [ ] Link every counter to its filtered operational page.
- [ ] Show empty and API unavailable states without fake health data.

Acceptance:

- Operators can navigate to the implemented Control Plane pages.
- Dashboard only summarizes persisted node/store state.
- Dashboard does not pretend platform health is complete before probes have produced snapshots.
- Full dashboard is tracked as a later UX milestone.

### Phase 10 - Hardening

- [ ] Add rate limiting to Control Plane API.
- [ ] Configure forwarded headers for production topology.
- [ ] Add ProblemDetails responses.
- [ ] Add request correlation middleware.
- [ ] Add structured logs for node probes and control actions.
- [ ] Add health checks for Control Plane DB.
- [ ] Add production appsettings example.
- [ ] Add runbook for first node registration, credential rotation, and failed probe triage.

Acceptance:

- Production configuration fails fast on missing required settings.
- Logs contain correlation id for API request -> control action -> node call.
- Runbook can be followed by a new operator.

### Phase 11 - Legacy Cutover Readiness

- [ ] Verify Control Plane does not reference legacy `BlazorShop.Presentation` UI/API/Storefront projects.
- [ ] Verify any `BlazorShop.Web.Shared` dependency is limited to the approved generic helper allowlist.
- [ ] Verify Control Plane can reference shared `BlazorShop.Application`, `BlazorShop.Infrastructure`, and `BlazorShop.Domain` only through additive ControlPlane slices and existing reusable auth contracts.
- [ ] Verify Control Plane tests do not depend on legacy admin UI.
- [ ] Decide when Commerce Node V2 starts.
- [ ] Document conditions for removing legacy `BlazorShop.Presentation` from the solution.

Acceptance:

- Control Plane can ship while legacy remains frozen.
- Removal of legacy is explicitly deferred until Commerce Node and StorefrontV2 have their own parity plans.

## UI Screen Map

| Screen | Primary job | Required states |
| --- | --- | --- |
| Dashboard | Summarize current operational state with counters: Total Nodes, Healthy Nodes, Warning Nodes, Down Nodes, Total Stores. | empty, no health samples, API unavailable |
| Nodes List | Find and filter nodes by status. | loading, empty, filtered empty, error |
| Node Create | Register remote node metadata. | validation errors, duplicate key, success |
| Node Detail | Inspect endpoints, health, capabilities, stores, actions. | loading, node disabled, no health yet, probe failed |
| Credentials | Create/revoke/rotate node credentials. | one-time secret display, revoked state, rotation warning |
| Health Timeline | Inspect latest heartbeat and node dependency status. | no samples, degraded samples, pagination |
| Capabilities | Inspect supported node API features. | no capability, schema mismatch, current/historical |
| Stores List | View platform store registry metadata and assign stores to nodes. | empty, archived hidden, no nodes available, domain conflict |
| Store Detail | Inspect store status, node assignment, and domains. | loading, archived, unassigned node, node unhealthy |
| Actions | Inspect control commands and retries. | queued, running, failed, succeeded, cancelled |
| Audit Log | Search security and change history by action and actor. | cursor pagination, filters, no results |

### UI Navigation And Icon Map

| Area | Font Awesome icon intent | Tailwind layout intent |
| --- | --- | --- |
| Dashboard | gauge/status summary | metric grid, compact recent activity lists |
| Nodes | server/node infrastructure | searchable table, status badges, detail drawer |
| Stores | storefront/store registry | assignment workflow, node status context |
| Health | heartbeat/dependency status | timeline/list with severity badges |
| Audit Logs | clipboard/history/search | filter bar, cursor-paginated table, detail drawer |
| Credentials | key/rotate/revoke | warning panels, one-time secret callout, confirm dialog |

## Test Plan

### Unit tests

- Domain validation for node key, status transitions, endpoint kind, credential status.
- Credential hashing/reveal lifecycle.
- Permission policy mapping.
- Store domain normalization and uniqueness rules.
- Control action state transitions.

### Integration tests

- EF migration creates required tables and indexes.
- Authorization per API route.
- Node CRUD with duplicate key and disabled node cases.
- Credential creation stores hash only.
- Health probe persists snapshots and updates node status.
- Capability refresh stores current snapshot correctly.
- Audit log written for security-sensitive actions.

### UI tests

- Dashboard renders empty state and persisted counters.
- Node create validation errors are visible.
- Node detail shows health failure with actionable copy.
- Credential secret is shown once.
- Store assignment validates missing or disabled nodes.
- Audit log filters and paginates.

### Operations tests

- Missing production config fails startup.
- DB unavailable returns unhealthy readiness.
- Node timeout does not block API request indefinitely.
- Correlation id appears across logs.

## Failure Modes Registry

| Failure | Impact | Required handling |
| --- | --- | --- |
| Node URL misconfigured | Probes fail; operator cannot manage node. | Validate URL, show failed probe with fix text. |
| Credential leaked | Node can be controlled by attacker. | Revoke, rotate, audit, never store raw secret. |
| Duplicate node key | Operator confusion, wrong node selected. | Unique active index and validation error. |
| Health probe storm | Node overload. | Rate limit probes and schedule with backoff. |
| Capability schema mismatch | UI/API assumes unsupported action. | Store schema version, block unsupported actions. |
| Audit log grows fast | Slow audit queries. | Cursor pagination, indexes, retention plan. |
| Store domain conflict | Wrong storefront routing later. | Unique active domain index. |
| Legacy dependency sneaks in | V2 couples to frozen code. | Architecture test or dependency review gate. |

## Implementation Tasks

- [ ] T0: Approve Control Plane-first scope.
- [ ] T1: Scaffold V2 project structure.
- [ ] T2: Add Control Plane database schema and migrations.
- [ ] T3: Add auth/authorization foundation.
- [ ] T4: Implement node registry.
- [ ] T5: Implement credential lifecycle.
- [ ] T6: Implement health/capability probes with mock node.
- [ ] T7: Implement store registry metadata.
- [ ] T8: Implement control actions.
- [ ] T9: Implement Control Plane Web shell and minimal dashboard counters.
- [ ] T10: Add hardening, production config, runbook.
- [ ] T11: Define legacy removal readiness checklist.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|---|---|---|---|---|---|
| 1 | Intake | Start with Control Plane, not Storefront/Commerce refactor | Mechanical | Explicit over clever | Control Plane is new scope and avoids touching legacy commerce flows. | Refactor legacy Presentation first |
| 2 | Architecture | Create PresentationV2 hosts and reuse existing shared layers | Mechanical | Bias toward action | Isolates presentation while keeping the layered core reusable. | Clean-room ControlPlane layers |
| 3 | Data | Use PostgreSQL with snake_case, FK indexes, cursor pagination | Mechanical | Completeness | Fits multi-node operational data and avoids common Postgres scaling traps. | Ad hoc EF defaults without DB conventions |
| 4 | Security | Store credential hashes only and design for rotation from day one | Mechanical | Completeness | Node control is privileged; raw secret persistence is unacceptable. | Store raw API keys in DB |
| 5 | UI | Include Control Plane Web screens in Phase 1 planning but implement after API/data foundation | Taste | Pragmatic | UI is needed for operator value, but API/data contracts should stabilize first. | UI-first scaffolding |

## GSTACK REVIEW REPORT

### CEO Review

Premise challenge:

- Correct problem: yes. Control Plane is the right first V2 slice because it is new platform scope and does not require modifying frozen legacy commerce behavior.
- Business outcome: operators can register, monitor, and prepare to manage many Commerce Nodes.
- Risk if nothing is done: V2 remains an architecture idea with no platform primitive to coordinate nodes.

Alternatives:

| Approach | Summary | Effort | Risk | Verdict |
| --- | --- | --- | --- | --- |
| A. Documentation only | Add Control Plane docs without project/schema work. | S | Medium | Too weak; does not unblock implementation. |
| B. Control Plane first | Build Control Plane plan, DB, API, UI, tests before Commerce Node. | M | Low-Medium | Recommended. New scope, low legacy blast radius. |
| C. Copy all Presentation to V2 first | Clone legacy Web/API/Storefront into V2 before Control Plane. | L | High | Rejected for Phase 1; imports UI/route debt before the Control Plane boundary is proven. |

Scope decision: choose Approach B.

### Design Review

UI scope detected: yes.

Design completeness: 7/10.

What is strong:

- Screen map covers the operator jobs: dashboard, nodes, credentials, health, capabilities, stores, actions, audit.
- Required loading/empty/error states are listed per screen.

Gaps to resolve during implementation:

- Visual design system is not defined here. Reuse existing WASM admin/workspace patterns where compatible.
- Credential one-time secret display needs careful visual hierarchy and copy.
- Health and action failure screens need problem, cause, fix, and next action.

### Engineering Review

Architecture soundness: 8/10.

Dependency graph:

```text
ControlPlane.Web (Blazor WASM)
    -> ControlPlane.API typed HTTP clients
    -> existing Web.Shared/client helper patterns where compatible

ControlPlane.API (PresentationV2)
    -> BlazorShop.Application/ControlPlane
    -> BlazorShop.Infrastructure/Data/ControlPlane
    -> ServiceDefaults

BlazorShop.Application/ControlPlane
    -> existing auth/application contracts
    -> BlazorShop.Domain/Entities/ControlPlane

BlazorShop.Infrastructure/Data/ControlPlane
    -> BlazorShop.Domain/Entities/ControlPlane
    -> PostgreSQL
    -> Commerce Node control HTTP client

BlazorShop.Domain/Entities/ControlPlane
    -> no outward dependency
```

Test coverage target:

```text
Node registry          -> unit + integration + UI
Credential lifecycle   -> unit + integration + security tests
Health probes          -> unit + integration with mock HTTP handler
Capabilities           -> integration + JSON contract tests
Store registry         -> unit + integration
Control actions        -> unit + integration + retry tests
Audit                  -> integration + pagination tests
Dashboard              -> UI/component tests
```

Engineering concerns:

- Control Plane may reference existing `BlazorShop.Application`, `BlazorShop.Infrastructure`, and `BlazorShop.Domain`, but new behavior should live in explicit `ControlPlane` feature folders/namespaces.
- Shared auth is intentional for this upgrade path, but Control Plane permissions must remain platform-specific.
- Decide whether Identity/auth tables stay in the existing auth database or are hosted in the Control Plane PostgreSQL database before Phase 3 implementation.
- Avoid overusing JSONB for data that must be filtered frequently. JSONB is fine for sanitized capability payloads and audit metadata, not for core node/store fields.

### DX Review

Developer-facing scope detected: yes, because this plan defines API endpoints, project setup, migrations, and operational runbooks.

DX completeness: 7/10.

Required developer path:

| Stage | Expected path |
| --- | --- |
| Discover | Read this todo and architecture baseline. |
| Setup | Create V2 presentation hosts, shared-layer ControlPlane folders, Docker PostgreSQL, and appsettings examples. |
| Build | `dotnet build BlazorShop.sln`. |
| Migrate | Apply Control Plane EF migration to local PostgreSQL. |
| Run | Start ControlPlane.API and ControlPlane.Web. |
| Verify | Register mock node and run health probe. |
| Debug | Use logs with correlation id and audit entries. |

DX gaps:

- Add copy-paste local setup commands when implementation starts.
- Add sample appsettings with placeholder values and validation messages.
- Add runbook before first real node integration.

### Cross-Phase Themes

- Isolation from legacy Presentation appears in CEO and engineering review. Treat any dependency on `BlazorShop.Presentation` as a blocker, while shared layered-core reuse is intentional.
- Credential lifecycle appears in CEO, engineering, design, and DX review. It is the highest-risk Control Plane feature.
- Health/capability error quality appears in design, engineering, and DX review. Operator-facing errors must explain problem, cause, and fix.

### VERDICT

Proceed with Control Plane-first V2 planning. The plan is complete enough to start Phase 1 scaffolding after explicit approval.

NO UNRESOLVED DECISIONS
