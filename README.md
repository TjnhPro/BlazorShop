# BlazorShop

[![CI](https://github.com/TjnhPro/BlazorShop/actions/workflows/ci.yml/badge.svg)](https://github.com/TjnhPro/BlazorShop/actions/workflows/ci.yml)

BlazorShop is a .NET 10 ecommerce codebase being migrated from a legacy single API/UI shape into an active V2 architecture with clear runtime boundaries:

```text
ControlPlane.Web
  -> ControlPlane.API
      -> CommerceNode.API

Storefront.V2
  -> CommerceNode.API api/storefront/stores/{storeKey}/*
```

The repository still contains the original `BlazorShop.Presentation` source temporarily for reference and migration comparison, but the main solution is V2 canonical. New feature work targets `BlazorShop.PresentationV2` unless a task explicitly says otherwise.

## What Is Active

| Area | Project | Responsibility |
| --- | --- | --- |
| Shared core | `BlazorShop.Domain` | Domain entities and contracts. |
| Shared core | `BlazorShop.Application` | DTOs, validators, options, application services, Control Plane and Commerce Node contracts. |
| Shared infrastructure | `BlazorShop.Infrastructure` | EF Core contexts, migrations, repositories, seeders, auth, email, payment, task, and node infrastructure. |
| Runtime defaults | `BlazorShop.ServiceDefaults` | Common .NET hosting defaults. |
| Control Plane API | `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API` | Platform auth, users, permissions, nodes, stores, health, actions, audit, and gateway calls to Commerce Node. |
| Control Plane Web | `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web` | Blazor WebAssembly admin/control UI that calls only Control Plane API. |
| Commerce Node API | `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API` | Node-local ecommerce admin APIs, Storefront APIs, task orchestration, media, deployment support, and Commerce Node database migration. |
| Storefront V2 | `BlazorShop.PresentationV2/BlazorShop.Storefront.V2` | Server-side public storefront, SEO documents, account/cart/checkout forms, and scoped Storefront API client. |
| Storefront components | `BlazorShop.PresentationV2/BlazorShop.Storefront.Components` | Reusable Razor components for the Storefront V2 render modes. |
| Storefront WASM | `BlazorShop.PresentationV2/BlazorShop.Storefront.WASM` | Interactive WebAssembly assembly used by Storefront V2. |
| Shared Web V2 | `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2` | Shared browser storage, cookie storage, auth session sync, API helpers, and toast utilities. |
| Tests | `BlazorShop.Tests.V2` | Active V2 unit, integration, contract, snapshot, and selected smoke tests. |

## Feature Surface

Current V2 work covers these major areas:

- Control Plane: login/logout/refresh/me, users, roles and permissions, nodes, node credentials, store registry, store domain registry, node health/probe, actions, audit logs, dashboard, and Commerce Node gateway pages.
- Commerce Admin: stores, store domains, feature states, settings, security/privacy, shipping settings, currencies, categories, products, variants, variation templates, inventory, media assets, product media import, pages, navigation, SEO settings, redirects, slug lifecycle, orders, payment methods, message templates, queued messages, tasks, metrics, and audit.
- Storefront API: current store, maintenance, configuration, catalog, category/product slug routes, recommendations, cart, checkout, address/customer profile, auth, orders, payments, currency, contact, newsletter, consent, SEO, pages, and navigation.
- Storefront V2 UI: home, search, new releases, deals, category, product, pages by slug, sign in, register, account profile, addresses, orders, cart, checkout, payment result pages, maintenance page, sitemap, robots, and public media proxy routes.
- Contract foundation: Commerce Node publishes separate Swagger documents for Commerce Admin and Storefront, with stable operation IDs, response schemas, standard error schemas, validation metadata, and security metadata guarded by tests and snapshots.

See [docs/architecture/06-feature-map.md](docs/architecture/06-feature-map.md) for the full ownership map.

## Requirements

- .NET SDK `10.0.107` or compatible `10.0.x` feature roll-forward, as pinned in [global.json](global.json).
- Docker Desktop or a compatible Docker engine for PostgreSQL, Nginx, and imgproxy local dependencies.
- PowerShell for the V2 local runner scripts.
- Node.js 20 if you work on frontend package assets or CI-equivalent frontend restore paths.

## Run V2 Locally

The preferred local V2 entry point is:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

The script reads [scripts/env/v2-local.env](scripts/env/v2-local.env), starts Docker dependencies unless skipped, starts the four active V2 services, waits for health endpoints, and bootstraps the local Control Plane registry.

Default local URLs:

| Surface | URL |
| --- | --- |
| Control Plane Web | `http://localhost:5281` |
| Control Plane API Swagger | `http://localhost:5280/swagger` |
| Commerce Node API Swagger | `http://localhost:5180/swagger` |
| Storefront V2 | `http://localhost:18598` |
| Commerce Node Nginx | `http://localhost:8088` |
| Commerce Node imgproxy | `http://localhost:8089` |

Stop the local V2 runtime with:

```powershell
.\scripts\stop-v2-local.ps1
```

Manual dependency startup is still available:

```powershell
docker compose -f compose.controlplane.yml up -d
docker compose -f compose.commercenode.yml up -d
```

## Databases

| Context | Connection string name | Local port | Owner |
| --- | --- | --- | --- |
| `ControlPlaneDbContext` | `ControlPlaneConnection` | `5433` | Platform auth, permissions, nodes, stores, credentials, actions, health, and audit. |
| `CommerceNodeDbContext` | `CommerceNodeConnection` | `5434` | Ecommerce stores, catalog, variants, media, inventory, cart, checkout, orders, payments, customers, SEO, messages, tasks, and deployment state. |
| `AppDbContext` | `DefaultConnection` | `5432` | Legacy commerce/storefront schema only. |

V2 API projects can apply their own EF Core migrations on startup when their `MigrateOnStartup` option is enabled. Do not use `AppDbContext` for new V2 work.

Development seeding is only for local QA fixture bootstrap. Commerce Node startup must not reset store runtime configuration that was edited through Control Plane; use a clean Commerce Node database when you intentionally need a full QA fixture rebuild.

## API And OpenAPI

Active V2 route ownership:

- `api/control-plane/*`: Control Plane API.
- `api/commerce/*`: Commerce Node admin/control APIs, called by Control Plane API with node credentials. Store-scoped admin endpoints require `storeKey` query.
- `api/storefront/stores/{storeKey}/*`: Commerce Node Storefront APIs, called by Storefront V2. Store scope comes from the route.

Commerce Node Swagger documents:

- `http://localhost:5180/swagger/commerce-admin/swagger.json`
- `http://localhost:5180/swagger/storefront/swagger.json`

Every new or changed active V2 endpoint must follow [docs/architecture/09-api-contract-standards.md](docs/architecture/09-api-contract-standards.md). In short: publish stable operation IDs, summaries, explicit request and response DTOs, error responses, security metadata, validation metadata, required request bodies, and contract tests.

## Tests And QA

Active V2 release-gate commands:

```powershell
dotnet restore BlazorShop.sln
dotnet build BlazorShop.sln -c Release --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release --no-build --verbosity normal
```

V2 production artifact validation uses the V2 compose and Dockerfiles:

```powershell
docker compose -f compose.v2.production.yml config
docker build -f BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Dockerfile -t blazorshop-commercenode-api:v2-ci .
docker build -f BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/Dockerfile -t blazorshop-controlplane-api:v2-ci .
docker build -f BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Dockerfile -t blazorshop-controlplane-web:v2-ci .
docker build -f BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Dockerfile -t blazorshop-storefront-v2:ci .
```

Use `compose.v2.production.yml` for V2 production topology until the canonical compose swap lands in the legacy-removal plan. The older `compose.production.yml` remains a legacy compatibility artifact.

Feature QA checklists live in [docs/refactor-control-Commerce-storefront](docs/refactor-control-Commerce-storefront):

- [QA-ControlPlane.todo.md](docs/refactor-control-Commerce-storefront/QA-ControlPlane.todo.md)
- [QA-CommerceNode.todo.md](docs/refactor-control-Commerce-storefront/QA-CommerceNode.todo.md)
- [QA-CommerceNode-TaskOrchestration.todo.md](docs/refactor-control-Commerce-storefront/QA-CommerceNode-TaskOrchestration.todo.md)
- [QA-StorefrontV2.todo.md](docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md)

When a behavior changes, update the matching checklist and run focused API/browser verification. Browser-facing changes should be checked with Playwright; use a visible browser when manual observation is requested.

## Documentation Map

Start here:

- [AGENTS.md](AGENTS.md): required rules for coding agents and V2 implementation work.
- [docs/architecture/README.md](docs/architecture/README.md): architecture index.
- [docs/architecture/01-system-map.md](docs/architecture/01-system-map.md): project map and runtime status.
- [docs/architecture/03-runtime-boundaries.md](docs/architecture/03-runtime-boundaries.md): allowed and forbidden call paths.
- [docs/architecture/04-data-ownership.md](docs/architecture/04-data-ownership.md): DbContext ownership.
- [docs/architecture/06-feature-map.md](docs/architecture/06-feature-map.md): feature ownership.
- [docs/architecture/07-deployment-and-local-run.md](docs/architecture/07-deployment-and-local-run.md): local run and deployment notes.
- [docs/architecture/09-api-contract-standards.md](docs/architecture/09-api-contract-standards.md): API contract requirements.
- [docs/production-runbook.md](docs/production-runbook.md): production notes. Some sections still document the legacy container path; verify against V2 architecture before using it for a release.

## Legacy And Reference Code

- `BlazorShop.Presentation/*` remains temporarily for comparison and migration reference, but is no longer part of the main solution.
- `BlazorShop.AppHost` has been removed. Use `scripts/run-v2-local.ps1` for local V2 orchestration.
- `Smartstore/` is reference source only. Study it for ecommerce concepts and workflows, but do not copy implementation code or add runtime references.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a PR. New work should preserve V2 boundaries, update docs/QA when behavior changes, and keep API contracts generator-safe.

## Security

Security reporting and supported-surface guidance are in [SECURITY.md](SECURITY.md). Do not put secrets in committed appsettings, docs, screenshots, logs, or QA artifacts.

## License

MIT License. See [LICENSE.txt](LICENSE.txt).
