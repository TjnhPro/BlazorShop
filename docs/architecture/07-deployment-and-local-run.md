# Deployment And Local Run

This document records the current development deployment model. It is not a production hardening guide.

## Compose Files

| File | Purpose |
| --- | --- |
| `compose.controlplane.yml` | Runs Control Plane PostgreSQL on host port `5433`. |
| `compose.commercenode.yml` | Runs Commerce Node PostgreSQL on host port `5434`, Nginx on host port `8088`, imgproxy on host port `8089`, and Mailpit SMTP capture on ports `1025`/`8025`. |
| `compose.v2.production.yml` | Active V2 production-oriented topology for Control Plane API/Web, Commerce Node API, Commerce Node PostgreSQL, Control Plane PostgreSQL, Nginx, imgproxy, and a Storefront V2 sample container. |
| `compose.production.yml` | Legacy production-oriented compose file. Do not use it as proof that V2 can release. |

## Local Ports

| Service | Port | Notes |
| --- | --- | --- |
| Control Plane PostgreSQL | `5433 -> 5432` | Database `blazorshop_controlplane`. |
| Commerce Node PostgreSQL | `5434 -> 5432` | Database `blazorshop_commerce_node`. |
| Legacy/default PostgreSQL | `5432` | Used by legacy `AppDbContext` if running legacy. |
| Commerce Node Nginx | `8088 -> 80` | Reverse proxy/runtime config for deployed storefront containers. |
| Commerce Node imgproxy | `8089 -> 8080` | Local image resize/format service for product media. |
| Commerce Node Mailpit SMTP | `1025 -> 1025` | Local/staging-style SMTP capture target for store email settings. |
| Commerce Node Mailpit inbox | `8025 -> 8025` | Web/API inbox for Playwright and manual QA: `http://localhost:8025`. |

## Preferred V2 Local Runner

Use the repository script for normal V2 manual QA:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

The script reads `scripts/env/v2-local.env`, starts the Control Plane and Commerce Node compose dependencies unless skipped, runs these active V2 projects, waits for health endpoints, and bootstraps the local Control Plane node/store registry:

- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API`
- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API`
- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`

Default script URLs:

| Surface | URL |
| --- | --- |
| Control Plane API | `http://localhost:5280` |
| Control Plane Web | `http://localhost:5281` |
| Commerce Node API | `http://localhost:5180` |
| Storefront V2 | `http://localhost:18598` |

Stop the local V2 runtime with:

```powershell
.\scripts\stop-v2-local.ps1
```

## Active V2 Build And Test

Use the V2 solution filter for normal active architecture work:

```powershell
dotnet build BlazorShop.V2.slnf --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore
```

`BlazorShop.V2.slnf` includes shared core, ServiceDefaults, active PresentationV2 projects, and `BlazorShop.Tests.V2`. It intentionally excludes legacy `BlazorShop.Presentation/*` projects. `BlazorShop.AppHost` is also excluded for now because the current AppHost still references legacy Presentation projects; `run-v2-local.ps1` is the active V2 local runtime entry point.

`BlazorShop.Tests.V2` links active V2 architecture, Commerce Node, Control Plane, Storefront V2, and Storefront WASM/browser host tests from the mixed historical test project. The V2 test assembly disables test parallelization so WebApplicationFactory/browser-host smoke tests do not race each other.

GitHub Actions uses `ci-v2` as the active release gate. That job restores/builds `BlazorShop.V2.slnf` and runs `BlazorShop.Tests.V2`. The historical `BlazorShop.sln`/`BlazorShop.Tests` path is kept as `legacy-compatibility` and must not be treated as proof that V2 is production-ready.

`ci-v2` also validates `compose.v2.production.yml` and builds the active V2 Dockerfiles:

- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Dockerfile`
- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/Dockerfile`
- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Dockerfile`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Dockerfile`

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

Development startup also runs `CommerceNodeDevelopmentSeeder` after a successful migration. The seeder is a QA fixture bootstrapper, not a runtime reset path:

- On a fresh local Commerce Node database, it creates the `default` store, auxiliary QA stores, catalog/page/navigation/customer/order fixtures, store payment methods, store email capture settings, shipping settings, feature states, and currency fixtures required by local QA.
- When the `default` store and core QA catalog fixture already exist, it exits without reseeding.
- Existing store runtime profile values edited through Control Plane, such as logo/favicon/icon URLs, default currency, default culture, contact/company fields, and maintenance state, must survive API restarts.
- Existing store-scoped settings rows are not overwritten by startup seeding; the seeder only fills missing bootstrap rows.
- To rebuild the full QA fixture set from scratch, use a clean Commerce Node database or a purpose-specific reseed operation. Restarting `run-v2-local.ps1` is not a data reset mechanism.

The Commerce Node compose file includes:

- PostgreSQL.
- Nginx.
- imgproxy.
- Mailpit for local SMTP capture.
- A dedicated `blazorshop-commercenode` network.
- Mounted Nginx config and log folders under `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/runtime/nginx`.
- Bind-mounted product media storage under `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/runtime/media`.

Local email capture:

- Development seeding configures the `default` and `qa-s2` stores with store-scoped email settings in capture mode.
- Both stores send to Mailpit SMTP at `localhost:1025`; Storefront V2 has no SMTP env/config dependency.
- Inspect captured messages at `http://localhost:8025` or through Mailpit's HTTP API from Playwright.
- `CommerceNode:EmailTransport:CaptureModeAllowed=true` is development/local only. Production examples keep capture disabled and require per-store SMTP setup through Control Plane once the management UI is enabled.
- `CommerceNode:EmailTransport:AllowGlobalEmailSettingsFallback=false` keeps production multi-store email from silently falling back to global `EmailSettings`.
- Store SMTP passwords are protected with ASP.NET Core Data Protection before they are stored in Commerce Node PostgreSQL. Production operators must persist and protect the Data Protection key ring outside the database and outside storefront/runtime env files so API restarts or multiple API instances can decrypt existing store SMTP secrets safely.

Email capture QA commands:

```powershell
.\scripts\qa\run-storefront-email-recovery-e2e.ps1
.\scripts\qa\run-storefront-order-email-e2e.ps1
```

Both scripts run visible Chromium by default through the local Playwright toolchain, read Mailpit at `http://localhost:8025/api/v1`, and assert that the browser does not call `api/internal/*`, `api/commerce/*`, or `api/control-plane/*` directly. The order email runner also verifies SMTP-outage retry behavior and store-scoped sender isolation through the Commerce Node resolver/transport path.

Nginx store-resolution smoke:

```powershell
docker compose -f compose.commercenode.yml up -d commercenode-nginx
docker exec blazorshop-commercenode-nginx nginx -t
curl.exe -i -H "Host: unknown.invalid" http://localhost:8088/
```

Expected:

- `nginx -t` succeeds.
- Unknown host returns `HTTP/1.1 403 Forbidden` from the default/catch-all server.
- Known generated store hosts continue to proxy to the intended Storefront container.

Product media local QA notes:

- Admin media APIs use `api/commerce/admin/products/{productId}/media/*` with node credentials and `storeKey` query.
- Public media URLs use `/media/products/{mediaPublicId}`.
- Direct local calls to `localhost:5180/media/products/{mediaId}` may return `404` when `localhost` does not map to exactly the target store host. Public media host scope comes from `Request.Host` after trusted forwarded headers have run; do not use `X-Store-Host` or raw browser-supplied forwarded headers for local QA.
- For local/admin media debugging, prefer Commerce Admin media endpoints with node credentials and `storeKey` query instead of forging public media headers.
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

Storefront V2 calls Commerce Node Storefront APIs under `api/storefront/stores/{storeKey}/*`. It resolves the route store key from configuration:

- `Api:StoreKey`
- `StoreKey`
- `STORE_KEY`

Development default is currently `default`.

Storefront V2 resolves the current store before store-scoped page/API work when `StoreResolution:RequireCurrentStore=true`, or by default outside Development. Static assets and health endpoints are skipped. A missing store returns `404`; CommerceNode unavailable, maintenance, or invalid current-store response returns `503`; Storefront V2 must not fall back to another store.

Public URL configuration:

- `PublicUrl:BaseUrl` is authoritative for canonical, discovery, sitemap, robots, and payment/client redirect URLs when configured.
- SEO configured base URL is second priority.
- Request fallback uses `Request.Scheme`, `Request.Host`, and `PathBase` only after `UseForwardedHeaders()` has run.
- Storefront trusted forwarded header config lives under `Storefront:ForwardedHeaders:KnownProxies` and `Storefront:ForwardedHeaders:KnownNetworks`.
- Configure trusted proxies/networks for the public ingress before relying on `X-Forwarded-Proto` or `X-Forwarded-Host`.

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

## V2 Production Compose

Use `compose.v2.production.yml` for V2 production-like validation. It intentionally uses separate database services and connection strings:

- `ConnectionStrings__ControlPlaneConnection` for Control Plane API.
- `ConnectionStrings__CommerceNodeConnection` for Commerce Node API.
- No `ConnectionStrings__DefaultConnection` in the V2 topology.

Required operator-provided values include:

- Control Plane database password and JWT key.
- Commerce Node database password, JWT key, node key, and node secret.
- Public base URLs for Control Plane API/Web, Commerce Node API, and Storefront V2.
- Storefront store key for the sample Storefront V2 container.

Production health endpoints are exposed only when `Runtime__Health__ExposeInProduction=true`; the V2 compose sets this for container healthchecks. The Commerce Node Nginx service mounts `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/runtime/nginx/conf.d`, which must keep `00-default-deny.conf` so unknown hosts return `403`.

## QA Run Notes

Use the relevant QA todo files under `docs/refactor-control-Commerce-storefront/`:

- `QA-ControlPlane.todo.md`
- `QA-CommerceNode.todo.md`
- `QA-CommerceNode-TaskOrchestration.todo.md`
- `QA-StorefrontV2.todo.md`

For browser QA, prefer Playwright with a visible browser when the user asks to observe the run.

For API QA, use clean databases when validating migrations or seeded MVP flows.
