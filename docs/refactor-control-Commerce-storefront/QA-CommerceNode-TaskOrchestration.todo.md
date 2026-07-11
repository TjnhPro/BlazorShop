# QA CommerceNode Task Orchestration Todo

Status: draft
Created: 2026-07-09
Scope: QA checklist and runbook for CommerceNode async task orchestration.

## Environment Setup

### Compose Test Harness

- [x] `docker compose -f compose.commercenode.yml config` is valid.
- [x] `docker compose -f compose.commercenode.yml up -d` starts PostgreSQL and Nginx.
- [x] Nginx container `blazorshop-commercenode-nginx` is running.
- [x] Nginx config volume persists under `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/runtime/nginx/conf.d`.
- [x] Nginx log volume persists under `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/runtime/nginx/logs`.
- [x] `docker exec blazorshop-commercenode-nginx nginx -t` passes.

### PostgreSQL

- [x] Start CommerceNode PostgreSQL on port `5434`.
- [x] Apply CommerceNode migrations through API startup. Development should run with `CommerceNode:Database:MigrateOnStartup=true`.

```powershell
dotnet run --project BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --urls http://localhost:5180
```

### CommerceNode API

- [x] Configure node credentials:
  - `CommerceNode:NodeKey=dev-node`
  - `CommerceNode:NodeSecret=dev-node-secret`
  - `CommerceNode:AllowedControlPlaneIps` includes caller IP.
- [x] Start `BlazorShop.CommerceNode.API`.
- [x] Confirm `GET /api/commerce/healthz` works with `X-Node-Key` and `X-Node-Secret`.

### Docker

- [x] Docker engine is available from the CommerceNode process user.
- [x] Storefront image exists locally or is pullable:
  - default: `blazorshop-storefront-v2:latest`
- [x] `StorefrontDeployment:AllowedImages` includes the image under test.
- [x] Optional Docker network exists if `StorefrontDeployment:NetworkName` is configured.

### Nginx

- [x] Nginx executable is available through Docker exec from the CommerceNode process user.
- [x] `NginxDeployment:UseDockerExec=true` in development config.
- [x] `NginxDeployment:ContainerName=blazorshop-commercenode-nginx` in development config.
- [x] `NginxDeployment:ConfigDirectory` points to an included `conf.d` directory.
- [x] `nginx -t` passes before test.
- [x] Nginx can reload from the CommerceNode process user.

## API Task Tests

### Enqueue

- [x] `POST /api/commerce/tasks` enqueues `commerce.test.complete`.
- [x] Response envelope has `success=true`, `message`, and `data.publicId`.
- [x] Duplicate `idempotencyKey` returns the existing task instead of creating another task.
- [x] Invalid JSON payload is rejected with `success=false`.
- [x] Missing `taskType` is rejected with `success=false`.

### List And Detail

- [x] `GET /api/commerce/tasks` returns the queued task.
- [x] `GET /api/commerce/tasks/{publicId}` returns task detail and steps.
- [x] Unknown task id returns `success=false` and HTTP `404`.

### Worker Success

- [x] Worker processes `commerce.test.complete`.
- [x] Task reaches `succeeded`.
- [x] Step `execute_handler` reaches `succeeded`.

### Worker Failure And Retry

- [x] Worker processes `commerce.test.fail` with payload `{"retryable":true}`.
- [x] Task reaches `waiting_retry` before max attempts are exhausted.
- [x] Task reaches `dead` after max attempts are exhausted.
- [x] `POST /api/commerce/tasks/{publicId}/retry` moves failed/dead task back to `pending`.
- [x] Non-retryable `commerce.test.fail` reaches `failed`.
- [x] Unknown task type reaches `failed` with `errorCode=handler_not_found`.

### Cancel

- [x] Enqueue `commerce.test.wait` with payload `{"delayMs":30000}`.
- [x] `POST /api/commerce/tasks/{publicId}/cancel` requests cancellation.
- [x] Worker observes cancellation and task reaches `cancelled`.

## Store Deploy Task Tests

### Validation Failure

- [x] Enqueue `store.create_and_deploy` with missing `storeKey`.
- [x] Task reaches `failed`.
- [x] `errorCode=invalid_store_deploy_payload`.
- [x] No Storefront container is created.

### Successful Deploy

- [x] Enqueue `store.create_and_deploy` with valid payload:

```json
{
  "schemaVersion": "v1",
  "controlPlaneStorePublicId": "00000000-0000-0000-0000-000000000000",
  "storeKey": "demo-store",
  "name": "Demo Store",
  "primaryDomain": "demo.local",
  "baseUrl": "https://demo.local",
  "defaultCurrencyCode": "USD",
  "defaultCulture": "en-US",
  "storefrontImage": "blazorshop-storefront-v2:latest"
}
```

- [x] Omitting `storefrontImage` resolves the default image from `storefront_deployment_image`.
- [x] `store_deployment.storefront_image` stores the resolved Docker image.
- [x] `commerce_store` row is created or updated.
- [x] `commerce_store_domain` primary domain is created or updated.
- [x] Storefront env file is generated.
- [x] Storefront container is created and started.
- [x] Nginx config fragment is generated.
- [x] Nginx config validates.
- [x] Nginx reloads.
- [x] Storefront `/` liveness probe passes.
- [x] `store_deployment.status=active`.
- [x] `commerce_store.status=active`.
- [x] Task reaches `succeeded`.

### Storefront Image Config

- [x] `storefront_deployment_image` migration applies.
- [x] Default image row exists for `storefront-v2`.
- [x] Valid deploy without `storefrontImage` uses the default DB image.
- [x] Unknown image selector reaches `failed`.
- [x] Unknown image selector returns `errorCode=storefront_image_not_allowed`.
- [x] Unknown image selector does not create a Storefront container.

### Container Ownership Guard

- [x] Storefront container has `blazorshop.owner=commercenode`.
- [x] Storefront container has `blazorshop.kind=storefront`.
- [x] Storefront container has `blazorshop.store_key`.
- [x] Storefront container has `blazorshop.store_id`.
- [x] Storefront container has `blazorshop.store_public_id`.
- [x] Storefront container has `blazorshop.task_public_id`.
- [x] Existing unmanaged container with matching generated name is not removed.
- [x] Task fails with `docker_create_failed` when generated name is occupied by unmanaged container.

### Docker Failure Rollback

- [ ] Use an allowed image name that cannot run successfully.
- [ ] Task reaches `failed`.
- [ ] Store remains `disabled`.
- [ ] Deployment reaches `failed`.
- [ ] Container is stopped/removed.

### Nginx Failure Rollback

- [ ] Configure invalid Nginx executable or config path.
- [ ] Task reaches `failed`.
- [ ] Generated Nginx config is removed.
- [ ] Storefront container is stopped/removed.
- [ ] Store remains `disabled`.

### Health Failure Rollback

- [ ] Start a Storefront image without a healthy liveness path.
- [ ] Task reaches `failed`.
- [ ] Deployment `last_health_status=unhealthy`.
- [ ] Storefront container is stopped/removed.
- [ ] Store remains `disabled`.

## ControlPlane Integration Tests

- [ ] `POST /api/control-plane/stores/{publicId}/deployment-tasks` submits a task to CommerceNode.
- [ ] ControlPlane does not create any local task table/row.
- [ ] `GET /api/control-plane/stores/{publicId}/deployment-tasks/{taskPublicId}` proxies task status from CommerceNode.
- [ ] `POST /api/control-plane/stores/{publicId}/deployment-tasks/{taskPublicId}/cancel` cancels through CommerceNode.
- [ ] `POST /api/control-plane/stores/{publicId}/deployment-tasks/{taskPublicId}/retry` retries through CommerceNode.
- [ ] Audit log records submit/cancel/retry.

## QA Run Results - 2026-07-09

Status: completed for CommerceNode task API, happy-path Storefront deployment, DB-backed image config, and container ownership guard; rollback and ControlPlane proxy cases remain pending.

### Environment

- [x] Compose config valid.
- [x] Compose services started.
- [x] CommerceNode migrations applied by API startup.
- [x] CommerceNode API started.
- [x] CommerceNode health endpoint verified.

### Task API

- [x] Enqueue/list/detail verified.
- [x] Duplicate idempotency verified.
- [x] Worker success verified.
- [x] Worker non-retryable failure verified.
- [x] Worker retry/dead path verified.
- [x] Worker cancel verified.
- [x] Unknown task handler verified.
- [x] Invalid enqueue request verified.

### Store Deploy Validation

- [x] Invalid `store.create_and_deploy` payload reaches failed.
- [x] No Storefront container created for invalid payload.
- [x] Unknown Storefront image selector reaches failed before runtime mutation.
- [x] Existing unmanaged container with generated name is preserved.

### Deferred Runtime Deployment

- [x] Successful Storefront deploy.
- [ ] Docker failure rollback.
- [ ] Nginx failure rollback.
- [ ] Health failure rollback.

Notes:

- Successful deploy verified with task `f1ab26eb-2a39-43a6-9d96-bc5a8827e332`, store `qa-store-20260709214646`, container `blazorshop-storefront-qa-store-20260709214646`, and Nginx proxy `http://localhost:8088/` with Host `qa-store-20260709214646.local`.
- DB image config verified with task `9dc3de47-b23f-41b7-b848-b4ec8fb0d71c`, store `qa-dbimage-20260709223814`, omitted `storefrontImage`, and deployed `blazorshop-storefront-v2:latest`.
- Unknown image rejection verified with task `01f16e98-ab0c-4e66-922c-f70ce37c0928`, `errorCode=storefront_image_not_allowed`, and no container created.
- Container ownership guard verified with task `aaa8e49e-d58c-4d2f-8a97-29e183e263bd`; unmanaged container survived until explicit QA cleanup.
- Runtime deploy fixes found during QA: missing Storefront container env values, host-to-Docker health probe mismatch, Storefront health path mismatch, and UTF-8 BOM in generated Nginx config.
- Rollback failure cases and ControlPlane proxy tests were not executed in this run.

## Build Verification

- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj`
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj`
- [x] `dotnet build BlazorShop.sln`
