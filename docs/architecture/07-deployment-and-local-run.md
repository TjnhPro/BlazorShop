# Deployment And Local Run

This document records the current development deployment model. It is not a production hardening guide.

## Compose Files

| File | Purpose |
| --- | --- |
| `compose.controlplane.yml` | Runs Control Plane PostgreSQL on host port `5433`. |
| `compose.commercenode.yml` | Runs Commerce Node PostgreSQL on host port `5434`, Nginx on host port `8088`, and imgproxy on host port `8089`. |
| `compose.production.yml` | Production-oriented compose file. Check before using because V2 architecture is evolving. |

## Local Ports

| Service | Port | Notes |
| --- | --- | --- |
| Control Plane PostgreSQL | `5433 -> 5432` | Database `blazorshop_controlplane`. |
| Commerce Node PostgreSQL | `5434 -> 5432` | Database `blazorshop_commerce_node`. |
| Legacy/default PostgreSQL | `5432` | Used by legacy `AppDbContext` if running legacy. |
| Commerce Node Nginx | `8088 -> 80` | Reverse proxy/runtime config for deployed storefront containers. |
| Commerce Node imgproxy | `8089 -> 8080` | Local image resize/format service for product media. |

## Control Plane Local Run

Database:

```powershell
docker compose -f compose.controlplane.yml up -d
```

Typical API project:

```powershell
dotnet run --project BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj
```

Typical Web project:

```powershell
dotnet run --project BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj
```

Control Plane API defaults to `ControlPlaneConnection` on port `5433`.

Control Plane API applies pending `ControlPlaneDbContext` EF Core migrations on startup when `ControlPlane:Database:MigrateOnStartup=true`. Development enables this by default.

## Commerce Node Local Run

Dependencies:

```powershell
docker compose -f compose.commercenode.yml up -d
```

Typical API project:

```powershell
dotnet run --project BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj
```

Commerce Node API defaults to `CommerceNodeConnection` on port `5434`.

Commerce Node API applies pending `CommerceNodeDbContext` EF Core migrations on startup when `CommerceNode:Database:MigrateOnStartup=true`. Development enables this by default.

The Commerce Node compose file includes:

- PostgreSQL.
- Nginx.
- imgproxy.
- A dedicated `blazorshop-commercenode` network.
- Mounted Nginx config and log folders under `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/runtime/nginx`.
- Bind-mounted product media storage under `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/runtime/media`.

Product media local QA notes:

- Admin media APIs use `api/commerce/admin/products/{productId}/media/*` with node credentials and `X-Store-Key`.
- Public media URLs use `/media/products/{mediaPublicId}`.
- Direct local calls to `localhost:5180/media/products/{mediaId}` may need `X-Store-Key` because `localhost` is not a real store domain in a multi-store database.
- Production/storefront traffic should resolve the store from the request host/domain through Nginx.

In deployment environments where Commerce Node manages Docker containers, it may require:

```text
/var/run/docker.sock:/var/run/docker.sock
```

Only add Docker socket access intentionally and only for environments where Commerce Node is allowed to act as a node deployer.

## Storefront V2 Local Run

Typical project:

```powershell
dotnet run --project BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj
```

Storefront V2 calls Commerce Node internal APIs. It sends a store key from configuration:

- `StorefrontApi:StoreKey`
- `StoreKey`
- `STORE_KEY`

Development default is currently `default`.

## Store Deployment Flow

Target design:

```text
ControlPlane.Web
  -> ControlPlane.API
      -> CommerceNode.API api/commerce/tasks
          -> CommerceNode persists pending task
              -> CommerceNode worker runs task asynchronously
                  -> Docker/Nginx deploy Storefront V2 with STORE_KEY/store_id config
```

Rules:

- Control Plane creates or manages intent.
- Commerce Node owns node-local task execution.
- Storefront containers should be uniquely named/keyed so Commerce Node can start, stop, remove, retry, or inspect them.
- Storefront image selection can be database-configured so test images and versions can be controlled.

## V2 Production Database Migration

V2 uses Smartstore-style startup migration for MVP.

Control Plane:

```text
ControlPlane__Database__MigrateOnStartup=true
ControlPlane__Database__FailStartupOnMigrationError=true
ControlPlane__Database__LogMigrationState=true
```

Commerce Node:

```text
CommerceNode__Database__MigrateOnStartup=true
CommerceNode__Database__FailStartupOnMigrationError=true
CommerceNode__Database__LogMigrationState=true
```

Production operation:

1. Backup the target PostgreSQL database before replacing the API image.
2. Ensure only one API instance starts against that database while migration runs.
3. Start the latest API image and watch startup logs for applied and pending migration names.
4. Treat migration failure as startup failure. Do not run traffic against a partially migrated database.
5. If startup fails, inspect logs, then restore the database backup or roll back the app image manually.

Long data migrations need manual review before release because they can block readiness while the API starts.

## QA Run Notes

Use the relevant QA todo files under `docs/refactor-control-Commerce-storefront/`:

- `QA-ControlPlane.todo.md`
- `QA-CommerceNode.todo.md`
- `QA-CommerceNode-TaskOrchestration.todo.md`
- `QA-StorefrontV2.todo.md`

For browser QA, prefer Playwright with a visible browser when the user asks to observe the run.

For API QA, use clean databases when validating migrations or seeded MVP flows.
