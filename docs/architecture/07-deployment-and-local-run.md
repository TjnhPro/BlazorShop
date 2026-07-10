# Deployment And Local Run

This document records the current development deployment model. It is not a production hardening guide.

## Compose Files

| File | Purpose |
| --- | --- |
| `compose.controlplane.yml` | Runs Control Plane PostgreSQL on host port `5433`. |
| `compose.commercenode.yml` | Runs Commerce Node PostgreSQL on host port `5434` and Nginx on host port `8088`. |
| `compose.production.yml` | Production-oriented compose file. Check before using because V2 architecture is evolving. |

## Local Ports

| Service | Port | Notes |
| --- | --- | --- |
| Control Plane PostgreSQL | `5433 -> 5432` | Database `blazorshop_controlplane`. |
| Commerce Node PostgreSQL | `5434 -> 5432` | Database `blazorshop_commerce_node`. |
| Legacy/default PostgreSQL | `5432` | Used by legacy `AppDbContext` if running legacy. |
| Commerce Node Nginx | `8088 -> 80` | Reverse proxy/runtime config for deployed storefront containers. |

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

The Commerce Node compose file includes:

- PostgreSQL.
- Nginx.
- A dedicated `blazorshop-commercenode` network.
- Mounted Nginx config and log folders under `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/runtime/nginx`.

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

## QA Run Notes

Use the relevant QA todo files under `docs/refactor-control-Commerce-storefront/`:

- `QA-ControlPlane.todo.md`
- `QA-CommerceNode.todo.md`
- `QA-CommerceNode-TaskOrchestration.todo.md`
- `QA-StorefrontV2.todo.md`

For browser QA, prefer Playwright with a visible browser when the user asks to observe the run.

For API QA, use clean databases when validating migrations or seeded MVP flows.
