# BlazorShop CommerceNode Task Orchestration Todo

Status: draft
Created: 2026-07-09
Scope: Build the CommerceNode async task foundation used by ControlPlane-driven operations. The first concrete operation is Storefront deployment per store.

## Goal

Create a reusable task orchestration layer inside `BlazorShop.CommerceNode.API` so ControlPlane can request long-running work without requiring real-time execution.

This is not Store business logic. This plan owns task persistence, task status, worker execution, Docker/Nginx deployment steps, retry, cancel, and rollback.

Future task types can reuse this foundation:

- create/deploy store
- disable store
- restart Storefront container
- import products
- upload/process media
- rebuild search index
- regenerate sitemap

## Locked Decisions

- Task source of truth is the CommerceNode database.
- ControlPlane does not persist task status in its own database.
- ControlPlane treats CommerceNode as an API service: submit, query, cancel, retry.
- CommerceNode API only enqueues and validates tasks.
- CommerceNode worker processes tasks asynchronously.
- PostgreSQL is the MVP task store.
- Redis is deferred; it is not required for the MVP task queue.
- Each Storefront V2 instance is deployed as one Docker container per store.
- Storefront containers receive `STORE_ID`/`STORE_KEY` style environment values.
- Nginx is part of the CommerceNode service cluster and reverse proxies to Storefront containers.
- Rollback is required for failed deployment steps.
- CommerceNode acts as the MVP deployment agent. API, task worker, Docker control, and Nginx control stay in one service for now; a separate agent process can be extracted later.
- Production-like CommerceNode deployments mount Docker socket access into the CommerceNode runtime, e.g. `/var/run/docker.sock:/var/run/docker.sock`.
- CommerceNode owns a dedicated Docker network for the node cluster. Nginx, Storefront containers, and future shared node services join that network.
- Storefront image/version selection is DB-backed deployment configuration. Appsettings may keep bootstrap defaults for local development, but the deploy task should resolve the approved image from CommerceNode DB config.
- CommerceNode manages only containers it created, identified by generated names and labels. Runtime operations such as stat/start/stop/remove must be scoped to that identity.
- Persisted runtime config volumes are accepted as the future production shape. MVP can keep the current local runtime directories until a dedicated volume layout is required.

## Non Goals

- Do not move Store business tables into ControlPlane.
- Do not make ControlPlane query CommerceNode PostgreSQL directly.
- Do not expose Docker or Nginx control outside CommerceNode.
- Do not accept raw Docker commands, raw compose files, or raw Nginx config from ControlPlane.
- Do not use Redis as a required queue in MVP.
- Do not implement product import/media upload handlers in the first slice unless explicitly requested.

## Architecture

```text
ControlPlane.Web
  -> ControlPlane.API
  -> CommerceNode API: POST /api/commerce/tasks
  -> commerce_task(status = pending)
  -> CommerceTaskWorker
  -> task handler
  -> Docker/Nginx/PostgreSQL operations
  -> commerce_task(status = succeeded/failed/cancelled)
```

ControlPlane can ask CommerceNode for current task state:

```text
ControlPlane.Web
  -> ControlPlane.API
  -> CommerceNode API: GET /api/commerce/tasks/{publicId}
```

CommerceNode remains autonomous after task submission. If ControlPlane goes offline, the task can still continue or fail locally.

## Database Design

### Commerce Node DB: `commerce_task`

Purpose: durable source of truth for asynchronous CommerceNode operations.

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `id` | `uuid` | no | Primary key. |
| `public_id` | `uuid` | no | External task id returned to ControlPlane. |
| `task_type` | `text` | no | Example: `store.create_and_deploy`. |
| `status` | `text` | no | `pending`, `running`, `waiting_retry`, `succeeded`, `failed`, `cancelled`, `dead`. |
| `idempotency_key` | `text` | yes | Prevent duplicate create/deploy requests. |
| `lock_key` | `text` | yes | Prevent conflicting tasks for same resource, e.g. `store:{storeKey}`. |
| `payload_schema_version` | `text` | no | Example: `v1`. |
| `payload_json` | `jsonb` | no | Handler-specific task payload. |
| `result_json` | `jsonb` | yes | Handler-specific result. |
| `error_code` | `text` | yes | Stable failure code. |
| `error_message` | `text` | yes | Safe failure message. |
| `attempt_count` | `integer` | no | Current attempts. |
| `max_attempts` | `integer` | no | Retry cap. |
| `next_attempt_at` | `timestamp with time zone` | yes | Retry scheduling. |
| `started_at` | `timestamp with time zone` | yes | First run time. |
| `completed_at` | `timestamp with time zone` | yes | Terminal time. |
| `created_at` | `timestamp with time zone` | no | Created time. |
| `updated_at` | `timestamp with time zone` | no | Last update time. |
| `created_by` | `text` | yes | Actor from ControlPlane or node admin. |
| `correlation_id` | `text` | yes | Cross-service trace id. |
| `cancel_requested_at` | `timestamp with time zone` | yes | Soft cancel request. |
| `cancel_reason` | `text` | yes | Cancellation reason. |
| `worker_id` | `text` | yes | Current worker identity. |
| `last_heartbeat_at` | `timestamp with time zone` | yes | Worker progress heartbeat. |

Indexes:

- unique `public_id`
- unique `idempotency_key` where `idempotency_key is not null`
- index `(status, next_attempt_at)`
- index `task_type`
- index `(lock_key, status)`
- index `correlation_id`

Check constraints:

- `status in ('pending', 'running', 'waiting_retry', 'succeeded', 'failed', 'cancelled', 'dead')`
- `attempt_count >= 0`
- `max_attempts >= 1`

### Commerce Node DB: `commerce_task_step`

Purpose: audit and debug individual task steps without forcing ControlPlane to store task internals.

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `id` | `uuid` | no | Primary key. |
| `task_id` | `uuid` | no | FK to `commerce_task`. |
| `step_key` | `text` | no | Stable step name. |
| `status` | `text` | no | `pending`, `running`, `succeeded`, `failed`, `skipped`, `rolled_back`. |
| `attempt_number` | `integer` | no | Task attempt number. |
| `result_json` | `jsonb` | yes | Step result. |
| `error_code` | `text` | yes | Step failure code. |
| `error_message` | `text` | yes | Safe failure message. |
| `started_at` | `timestamp with time zone` | yes | Step start. |
| `completed_at` | `timestamp with time zone` | yes | Step completion. |

Indexes:

- index `task_id`
- index `(task_id, step_key, attempt_number)`

### Commerce Node DB: `store_deployment`

Purpose: track the deployed runtime artifacts for one Storefront container.

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `id` | `uuid` | no | Primary key. |
| `store_id` | `uuid` | no | FK to CommerceStore. |
| `task_id` | `uuid` | yes | Last deployment task. |
| `storefront_image` | `text` | no | Approved image/tag used for deployment. |
| `container_name` | `text` | no | Generated safe container name. |
| `network_name` | `text` | yes | Docker network. |
| `public_url` | `text` | yes | Public storefront URL. |
| `internal_url` | `text` | yes | Internal container URL for Nginx. |
| `nginx_server_name` | `text` | yes | Primary Nginx server name. |
| `nginx_config_path` | `text` | yes | Generated config path. |
| `env_file_path` | `text` | yes | Generated env file path. |
| `status` | `text` | no | `provisioning`, `active`, `failed`, `disabled`, `removed`. |
| `last_health_status` | `text` | yes | Last Storefront liveness probe result. |
| `last_health_at` | `timestamp with time zone` | yes | Last health check time. |
| `deployed_at` | `timestamp with time zone` | yes | Successful deploy time. |
| `created_at` | `timestamp with time zone` | no | Created time. |
| `updated_at` | `timestamp with time zone` | no | Updated time. |

Indexes:

- unique `store_id`
- unique `container_name`
- index `status`

Check constraints:

- `status in ('provisioning', 'active', 'failed', 'disabled', 'removed')`

### Commerce Node DB: `storefront_deployment_image`

Purpose: controlled catalog of Storefront images that CommerceNode is allowed to deploy.

This keeps image/version selection operationally configurable without letting ControlPlane send arbitrary Docker image names. Appsettings can still provide local bootstrap defaults, but runtime deployment should prefer this table.

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `id` | `uuid` | no | Primary key. |
| `key` | `text` | no | Stable logical image key, e.g. `storefront-v2`. |
| `image` | `text` | no | Full Docker image reference, e.g. `registry.example.com/blazorshop/storefront:v2.0.1`. |
| `version` | `text` | yes | Human-readable version/tag used for audit and UI. |
| `is_default` | `boolean` | no | Default image for new store deployments. |
| `is_enabled` | `boolean` | no | Disabled images cannot be selected for new tasks. |
| `created_at` | `timestamp with time zone` | no | Created time. |
| `updated_at` | `timestamp with time zone` | no | Updated time. |

Indexes:

- unique `key`
- unique `image`
- index `(is_enabled, is_default)`

Rules:

- Only one enabled default image is allowed.
- Store deploy payload may omit image and use the default DB image.
- If a payload includes an image key/version for testing, CommerceNode resolves it through this table before deploying.
- `store_deployment.storefront_image` stores the resolved image actually deployed for audit.

## API Design

Prefix: `api/commerce/tasks`

Auth:

- Use existing CommerceNode `api/commerce/*` node key/secret/IP middleware.
- Return the existing `success/message/data` response envelope.
- Keep HTTP status meaningful for transport/auth failures.

Endpoints:

| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/api/commerce/tasks` | Enqueue a task. |
| `GET` | `/api/commerce/tasks` | List tasks by status/type/date. |
| `GET` | `/api/commerce/tasks/{publicId}` | Task detail with steps. |
| `POST` | `/api/commerce/tasks/{publicId}/cancel` | Request cancellation. |
| `POST` | `/api/commerce/tasks/{publicId}/retry` | Retry failed/dead task if allowed. |

Optional convenience endpoint:

| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/api/commerce/admin/stores/provision` | Validate store payload and enqueue `store.create_and_deploy`. |

### Enqueue Request

```json
{
  "taskType": "store.create_and_deploy",
  "idempotencyKey": "store:demo-store:create-and-deploy",
  "payloadSchemaVersion": "v1",
  "payload": {}
}
```

### Enqueue Response

```json
{
  "success": true,
  "message": "Task queued.",
  "data": {
    "publicId": "00000000-0000-0000-0000-000000000000",
    "taskType": "store.create_and_deploy",
    "status": "pending"
  }
}
```

## Worker Design

Add a CommerceNode background service:

```text
CommerceTaskWorker : BackgroundService
```

Responsibilities:

- Poll pending or retry-ready tasks.
- Acquire task lock in PostgreSQL.
- Dispatch by `task_type`.
- Record task steps.
- Update `last_heartbeat_at`.
- Respect `cancel_requested_at` between steps.
- Apply retry policy for transient failures.
- Mark terminal status deterministically.

Handler interface:

```text
ICommerceTaskHandler
  TaskType
  ExecuteAsync(task, cancellationToken)
```

MVP task handlers:

- `store.create_and_deploy`
- `store.disable`
- `store.restart_frontend`
- `store.remove_frontend_config`

Deferred handlers:

- `product.import`
- `media.upload`
- `seo.regenerate_sitemap`
- `search.reindex`

## Store Create And Deploy Task

Task type:

```text
store.create_and_deploy
```

Payload v1:

```json
{
  "schemaVersion": "v1",
  "controlPlaneStorePublicId": "00000000-0000-0000-0000-000000000000",
  "storeKey": "demo-store",
  "name": "Demo Store",
  "primaryDomain": "demo.example.com",
  "baseUrl": "https://demo.example.com",
  "defaultCurrencyCode": "USD",
  "defaultCulture": "en-US",
  "storefrontImage": "blazorshop-storefront-v2:latest"
}
```

Steps:

1. `validate_payload`
2. `create_or_update_commerce_store`
3. `create_or_update_domain`
4. `render_storefront_env`
5. `create_or_update_storefront_container`
6. `render_nginx_config`
7. `reload_nginx`
8. `health_check_storefront`
9. `activate_store`

The task handler owns deployment mechanics. StoreExpansion owns the `CommerceStore` model and store validation rules.

## Docker And Nginx Rules

Docker:

- CommerceNode must run with Docker control in the node environment. For containerized CommerceNode this means mounting `/var/run/docker.sock:/var/run/docker.sock`.
- All CommerceNode-managed runtime containers join a dedicated node network, for example `blazorshop-commercenode`.
- Use generated container names only, e.g. `blazorshop-storefront-{storeKey}`.
- Add stable labels to every managed container, including store key, store public id, task id, and an owner marker.
- Storefront image names/tags are resolved from CommerceNode DB deployment config. Do not rely on ControlPlane-submitted arbitrary image names.
- Appsettings image allowlist is acceptable only as a local bootstrap/default guard in MVP.
- Generate env files from typed payload and node config.
- Do not accept raw env file content from ControlPlane.
- Keep one Storefront container per store.
- Container stat/start/stop/remove operations must verify the expected owner label and generated name before mutating runtime state.

Nginx:

- Generate one config fragment per store.
- Validate config before reload.
- Reload Nginx only after config validation succeeds.
- Remove or disable generated config during rollback.
- Later image cache/media services can be added as extra reverse proxy upstreams.

## Rollback Strategy

Validation failure:

- No runtime mutation.
- Task status becomes `failed`.

Store row created but container fails:

- Mark store/deployment as `failed`.
- Remove generated env/container artifacts when safe.
- Optional debug mode can leave stopped artifacts for inspection.

Nginx config fails:

- Remove generated config.
- Validate and reload previous config.
- Stop/remove the new Storefront container unless debug mode is enabled.
- Mark deployment as `failed`.

Health check fails:

- Store remains inactive or failed.
- Stop container or leave stopped in debug mode.
- Task status becomes `failed`.

Cancel requested:

- Stop before the next step.
- If runtime artifacts were created, run rollback steps.
- Task status becomes `cancelled`.

## Security Rules

- CommerceNode API is protected by node key, node secret, and IP allowlist.
- Docker socket/control is available only inside the node deployment boundary and is never exposed outside the node.
- ControlPlane cannot submit raw shell commands.
- ControlPlane cannot submit arbitrary image names. The node resolves approved Storefront images from DB-backed deployment config.
- Generated file paths must be under configured CommerceNode directories.
- Store keys must be normalized before being used in container/file names.
- Docker runtime operations must be constrained to CommerceNode-owned labels/container prefixes.

## Phase Plan

### Phase 1 - Task Contract And Schema

- [x] Add `CommerceTask` entity.
- [x] Add `CommerceTaskStep` entity.
- [x] Add `StoreDeployment` entity.
- [x] Add EF mappings, indexes, and constraints.
- [x] Add migration.
- [x] Add DTOs for enqueue/list/detail/cancel/retry.

Stop gate:

- Clean CommerceNode DB migrates and task rows can be persisted.

### Phase 2 - Task API

- [x] Add `CommerceTasksController`.
- [x] Add enqueue endpoint.
- [x] Add list/detail endpoints.
- [x] Add cancel endpoint.
- [x] Add retry endpoint.
- [x] Apply `api/commerce/*` auth.
- [x] Return existing API response envelope.

Stop gate:

- ControlPlane can submit and inspect tasks without worker execution.

### Phase 3 - Worker And Dispatch

- [x] Add `CommerceTaskWorker`.
- [x] Add task locking.
- [x] Add handler registry.
- [x] Add task heartbeat.
- [x] Add step recording.
- [x] Add retry handling.
- [x] Add cancellation checks.

Stop gate:

- A test handler can complete, fail, retry, and cancel tasks deterministically.

### Phase 4 - Docker Deployment Service

- [x] Add typed deployment options.
- [x] Add generated container naming.
- [x] Add Storefront image whitelist as MVP bootstrap config.
- [ ] Move approved Storefront image/version resolution into CommerceNode DB deployment config.
- [ ] Add owner labels to Storefront containers.
- [ ] Guard stat/start/stop/remove by generated name and owner labels.
- [x] Add env file rendering.
- [x] Add container create/update/start/stop/remove operations.
- [x] Add health probe call to Storefront liveness path.

Stop gate:

- CommerceNode can start a StorefrontV2 container from typed config.

### Phase 5 - Nginx Deployment Service

- [x] Add typed Nginx options.
- [x] Add generated config path rules.
- [x] Add config fragment rendering.
- [x] Add Nginx config validation.
- [x] Add Nginx reload.
- [x] Add rollback for failed config/reload.

Stop gate:

- Nginx can proxy a store domain to a StorefrontV2 container.

### Phase 6 - Store Create And Deploy Handler

- [x] Implement `store.create_and_deploy`.
- [x] Validate payload.
- [x] Create or update `CommerceStore`.
- [x] Create or update primary domain.
- [x] Render Storefront env.
- [x] Create/start Storefront container.
- [x] Render/reload Nginx config.
- [x] Health check Storefront.
- [x] Activate store only after health succeeds.
- [x] Roll back on failure.

Stop gate:

- One store can be provisioned from ControlPlane request through async task execution.

### Phase 7 - ControlPlane Integration

- [x] ControlPlane submits store deployment task through CommerceNode API.
- [x] ControlPlane queries task status through CommerceNode API.
- [x] ControlPlane can request cancel/retry.
- [x] ControlPlane does not store task state locally.
- [x] Add audit logs for submit/cancel/retry actions.

Stop gate:

- Operator can create a store request and monitor the task from ControlPlane.

### Phase 8 - QA And Runbook

- [x] Add `QA-CommerceNode-TaskOrchestration.todo.md` or extend existing CommerceNode QA file.
- [x] Document clean DB setup.
- [x] Document Docker/Nginx local prerequisites.
- [ ] Test enqueue/list/detail/cancel/retry.
- [ ] Test successful store deploy.
- [ ] Test validation failure.
- [ ] Test Docker failure rollback.
- [ ] Test Nginx failure rollback.
- [ ] Test Storefront health failure rollback.

Stop gate:

- MVP async task system is safe enough for StoreExpansion deployment flow.

## QA Checklist

- [ ] Clean DB migration.
- [ ] Enqueue task returns task public id.
- [ ] Duplicate idempotency key does not create duplicate work.
- [ ] Task detail shows steps.
- [ ] Cancel pending task works.
- [ ] Cancel running task stops at a safe step.
- [ ] Failed task can be retried when retryable.
- [ ] Non-retryable task is not retried.
- [ ] Worker survives API process restart.
- [ ] Storefront container deploy succeeds.
- [ ] Nginx config validation failure rolls back.
- [ ] Storefront health failure rolls back.
- [ ] ControlPlane can query task state but has no task DB table dependency.

## Risks

| Risk | Impact | Mitigation |
| --- | --- | --- |
| ControlPlane and CommerceNode both own task state | Status divergence | CommerceNode DB is the only task source of truth. |
| Raw deployment input from ControlPlane | Security risk | Accept only typed payload and whitelisted images. |
| Failed Nginx reload breaks other stores | High production impact | Validate config before reload and keep rollback path. |
| Container naming collisions | Wrong store container modified | Normalize store key and enforce unique deployment row. |
| Task worker retries non-idempotent steps | Duplicate artifacts | Use idempotency keys, lock keys, and step-aware handlers. |

## Open Questions To Close

1. Should failed deploy artifacts be removed by default, or left stopped in local debug mode only?

## Decision Log

| # | Decision | Status | Reason | Deferred alternative |
| --- | --- | --- | --- | --- |
| 1 | CommerceNode is the MVP deployment agent | User-approved | Keeps MVP fast and avoids another service/process boundary. | Extract `CommerceNode.Agent` later. |
| 2 | Mount Docker socket for containerized CommerceNode | User-approved | Node must create/start/stop/remove Storefront containers. | Remote Docker API or external orchestrator. |
| 3 | Use a dedicated Docker network per node cluster | User-approved | Lets Nginx, Storefront, PostgreSQL/Redis/future shared services communicate privately. | Shared/default Docker network. |
| 4 | Storefront image/version comes from CommerceNode DB config | User-approved | Allows controlled testing of different images/versions without code/config redeploy. | Static appsettings-only image allowlist. |
| 5 | CommerceNode manages only owned containers | User-approved | Avoids duplicate/wrong-container operations and enables safe stat/start/stop/remove recovery. | Raw container names from ControlPlane. |
| 6 | Persisted config volumes are future production shape, not required immediately | User-approved | MVP can continue with current local runtime directories while deploy flow stabilizes. | Design full volume layout before MVP validation. |
| 7 | Keep deployment-agent behavior inside CommerceNode API for MVP | User-approved | Simpler deployment and fewer moving parts now. | Split worker/agent before core flow is proven. |
