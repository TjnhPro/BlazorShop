# QA CommerceNode Task Orchestration Todo

Status: draft
Created: 2026-07-09
Scope: QA checklist and runbook for CommerceNode async task orchestration.

## Environment Setup

### PostgreSQL

- [ ] Start CommerceNode PostgreSQL on port `5434`.
- [ ] Apply CommerceNode migrations:

```powershell
dotnet ef database update --project BlazorShop.Infrastructure --startup-project BlazorShop.PresentationV2/BlazorShop.CommerceNode.API --context CommerceNodeDbContext
```

### CommerceNode API

- [ ] Configure node credentials:
  - `CommerceNode:NodeKey=dev-node`
  - `CommerceNode:NodeSecret=dev-node-secret`
  - `CommerceNode:AllowedControlPlaneIps` includes caller IP.
- [ ] Start `BlazorShop.CommerceNode.API`.
- [ ] Confirm `GET /api/commerce/healthz` works with `X-Node-Key` and `X-Node-Secret`.

### Docker

- [ ] Docker engine is available from the CommerceNode process user.
- [ ] Storefront image exists locally or is pullable:
  - default: `blazorshop-storefront-v2:latest`
- [ ] `StorefrontDeployment:AllowedImages` includes the image under test.
- [ ] Optional Docker network exists if `StorefrontDeployment:NetworkName` is configured.

### Nginx

- [ ] Nginx executable is available from the CommerceNode process user.
- [ ] `NginxDeployment:ConfigDirectory` points to an included `conf.d` directory.
- [ ] `nginx -t` passes before test.
- [ ] Nginx can reload from the CommerceNode process user.

## API Task Tests

### Enqueue

- [ ] `POST /api/commerce/tasks` enqueues `commerce.test.complete`.
- [ ] Response envelope has `success=true`, `message`, and `data.publicId`.
- [ ] Duplicate `idempotencyKey` returns the existing task instead of creating another task.

### List And Detail

- [ ] `GET /api/commerce/tasks` returns the queued task.
- [ ] `GET /api/commerce/tasks/{publicId}` returns task detail and steps.
- [ ] Unknown task id returns `success=false` and HTTP `404`.

### Worker Success

- [ ] Worker processes `commerce.test.complete`.
- [ ] Task reaches `succeeded`.
- [ ] Step `execute_handler` reaches `succeeded`.

### Worker Failure And Retry

- [ ] Worker processes `commerce.test.fail` with payload `{"retryable":true}`.
- [ ] Task reaches `waiting_retry` before max attempts are exhausted.
- [ ] Task reaches `dead` after max attempts are exhausted.
- [ ] `POST /api/commerce/tasks/{publicId}/retry` moves failed/dead task back to `pending`.

### Cancel

- [ ] Enqueue `commerce.test.wait` with payload `{"delayMs":30000}`.
- [ ] `POST /api/commerce/tasks/{publicId}/cancel` requests cancellation.
- [ ] Worker observes cancellation and task reaches `cancelled`.

## Store Deploy Task Tests

### Validation Failure

- [ ] Enqueue `store.create_and_deploy` with missing `storeKey`.
- [ ] Task reaches `failed`.
- [ ] `errorCode=invalid_store_deploy_payload`.
- [ ] No Storefront container is created.

### Successful Deploy

- [ ] Enqueue `store.create_and_deploy` with valid payload:

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

- [ ] `commerce_store` row is created or updated.
- [ ] `commerce_store_domain` primary domain is created or updated.
- [ ] Storefront env file is generated.
- [ ] Storefront container is created and started.
- [ ] Nginx config fragment is generated.
- [ ] Nginx config validates.
- [ ] Nginx reloads.
- [ ] Storefront `/healthz` passes.
- [ ] `store_deployment.status=active`.
- [ ] `commerce_store.status=active`.
- [ ] Task reaches `succeeded`.

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

- [ ] Start a Storefront image without healthy `/healthz`.
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

## Build Verification

- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj`
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj`
- [x] `dotnet build BlazorShop.sln`
