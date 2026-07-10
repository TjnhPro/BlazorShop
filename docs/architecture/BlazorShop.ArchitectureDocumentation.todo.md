# BlazorShop Architecture Documentation Plan

## Purpose

Create a stable architecture documentation set under `docs/architecture/` so future agents can understand the current BlazorShop codebase before making implementation decisions.

This is documentation-only work. It must not refactor runtime code, rename projects, change database schemas, or change deployment scripts.

## Investigation Snapshot

### Solution Projects

| Area | Project | Status | Responsibility |
| --- | --- | --- | --- |
| Core | `BlazorShop.Domain` | Active shared core | Entities and domain contracts used by both legacy and V2 boundaries. |
| Core | `BlazorShop.Application` | Active shared core | DTOs, service contracts, validation, application services, Control Plane and Commerce Node interfaces. |
| Core | `BlazorShop.Infrastructure` | Active shared infrastructure | EF contexts, repositories, infrastructure services, migrations, auth adapters, transaction managers. |
| Runtime | `BlazorShop.ServiceDefaults` | Active shared runtime | Aspire/service defaults and shared hosting concerns. |
| Runtime | `BlazorShop.AppHost` | Legacy-oriented | References legacy `BlazorShop.API`, `BlazorShop.Web`, and `BlazorShop.Storefront`. |
| Legacy Presentation | `BlazorShop.Presentation/BlazorShop.API` | Legacy, do not extend | Original commerce API mixing admin and storefront concerns. |
| Legacy Presentation | `BlazorShop.Presentation/BlazorShop.Web` | Legacy, do not extend | Original admin/account/customer Blazor Web UI. |
| Legacy Presentation | `BlazorShop.Presentation/BlazorShop.Storefront` | Legacy, do not extend | Original public storefront. |
| Legacy Shared | `BlazorShop.Presentation/BlazorShop.Web.Shared` | Legacy, do not extend | Original shared Web helpers. |
| V2 Control Plane | `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API` | Active V2 | Platform API for auth, nodes, stores, actions, health, catalog gateway, audit, user management. |
| V2 Control Plane | `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web` | Active V2 | Blazor WASM admin UI. Must only call `BlazorShop.ControlPlane.API`. |
| V2 Commerce Node | `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API` | Active V2 | Node-local ecommerce API, admin commerce endpoints, internal storefront endpoints, task orchestration, deployment support. |
| V2 Storefront | `BlazorShop.PresentationV2/BlazorShop.Storefront.V2` | Active V2 | Server-side storefront that calls Commerce Node internal APIs with store scope. |
| V2 Shared | `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2` | Active V2 | Shared browser storage, auth session, toast, API helpers for V2 UI projects. |
| Tests | `BlazorShop.Tests` | Active but mixed | Tests currently reference legacy and selected V2 projects. |
| Reference Source | `Smartstore/` | Reference only | Used for ecommerce business research. Do not copy code or add product runtime references. |

### Current Runtime Boundaries

```text
Control Plane Web
  -> Control Plane API
      -> Commerce Node API

Storefront V2
  -> Commerce Node API /api/internal/*

Control Plane API
  -> PostgreSQL ControlPlaneConnection, local dev port 5433

Commerce Node API
  -> PostgreSQL CommerceNodeConnection, local dev port 5434
  -> Nginx runtime config under CommerceNode runtime folder
  -> Docker deployment support for Storefront V2 containers
```

`BlazorShop.ControlPlane.Web` must never call `BlazorShop.CommerceNode.API` directly. Node keys, node secrets, node base URLs, IP allowlist behavior, and Commerce Node security headers belong behind `BlazorShop.ControlPlane.API`.

### API Boundary Map

| Boundary | Routes | Caller | Security Model |
| --- | --- | --- | --- |
| Control Plane API | `api/control-plane/*` | Control Plane Web and platform operators | Control Plane JWT, permissions, response envelope, rate limiting. |
| Commerce Node admin/control API | `api/commerce/*` | Control Plane API | Node key, node secret, allowed IP, store scope where applicable. |
| Commerce Node internal storefront API | `api/internal/*` | Storefront V2 on the same node/private path | Store key and storefront/customer session behavior. |
| Legacy API | `api/admin/*`, `api/public/*`, `api/[controller]` | Legacy Web/Storefront | Legacy auth and legacy AppDbContext. Do not use for V2 expansion. |

### Database Ownership

| Context | Connection | Dev Port | Owner | Contains |
| --- | --- | --- | --- | --- |
| `ControlPlaneDbContext` | `ControlPlaneConnection` | `5433` | Control Plane | Control Plane identity, users, roles, permissions, node registry, node credentials, store registry, action registry, health snapshots, audit logs. |
| `CommerceNodeDbContext` | `CommerceNodeConnection` | `5434` | Commerce Node | Commerce identity/customer auth, products, categories, variants, inventory, orders, carts, payments, SEO, newsletters, commerce stores, store domains, storefront deployment image config, deployment records, node task orchestration. |
| `AppDbContext` | `DefaultConnection` | `5432` | Legacy | Legacy commerce/storefront/admin data. Do not migrate new V2 features into this context. |

## Documentation Set To Create

### 1. `docs/architecture/README.md`

Entry point for agents.

Required content:
- Reading order.
- Short current-state summary.
- Explicit legacy vs V2 rule.
- Links to `AGENTS.md`, `docs/agents/domain.md`, and existing planning docs.
- Warning that `Smartstore/` is reference-only.

Acceptance criteria:
- A new agent can read this first and know where to go next.
- It states that V2 work must not extend legacy Presentation projects unless explicitly requested.

### 2. `docs/architecture/01-system-map.md`

Solution-level project map.

Required content:
- Every project in `BlazorShop.sln`.
- Project status: active V2, shared core, legacy, tests, reference source.
- Project references summary.
- Runtime ownership and development status.

Acceptance criteria:
- It is clear which projects are active targets.
- It is clear that the solution still includes legacy projects, but V2 has its own runtime boundary.

### 3. `docs/architecture/02-layered-architecture.md`

Layered Architecture explanation for this repo.

Required content:
- Domain layer responsibilities.
- Application layer responsibilities.
- Infrastructure layer responsibilities.
- Presentation and PresentationV2 responsibilities.
- Existing reuse rule: prefer existing services, repositories, validators, response patterns, and options before adding abstractions.

Acceptance criteria:
- Agents can decide where a new service/interface/repository should live.
- It explains why V2 still uses shared Domain/Application/Infrastructure while avoiding legacy Presentation references.

### 4. `docs/architecture/03-runtime-boundaries.md`

Service and API boundary map.

Required content:
- ControlPlane Web -> ControlPlane API -> CommerceNode API gateway rule.
- StorefrontV2 -> CommerceNode internal API rule.
- `api/control-plane/*`, `api/commerce/*`, `api/internal/*`, and legacy route groups.
- Security ownership per boundary.
- Response pattern ownership.

Acceptance criteria:
- It prevents agents from designing direct Web-to-CommerceNode calls.
- It prevents mixing Storefront internal APIs with Control Plane admin APIs.

### 5. `docs/architecture/04-data-ownership.md`

Database and EF context ownership.

Required content:
- `ControlPlaneDbContext`, `CommerceNodeDbContext`, `AppDbContext`.
- Connection names and local ports.
- Migration ownership.
- Entity/table groups by business area.
- Rule: new V2 commerce data belongs to `CommerceNodeDbContext`; new platform data belongs to `ControlPlaneDbContext`; legacy `AppDbContext` is not a target for new V2 features.

Acceptance criteria:
- It answers "which database/context should this feature use?" without needing prior conversation.
- It records why Control Plane auth and Commerce Node auth are separate despite sharing identity entity classes.

### 6. `docs/architecture/05-project-and-folder-guide.md`

Project/folder responsibility guide.

Required content:
- Important folders in each active V2 project.
- Important folders in `BlazorShop.Application` and `BlazorShop.Infrastructure`.
- Legacy folders summarized as read-only/reference for migration.
- `docs/refactor-control-Commerce-storefront/` as historical plan/QA area.

Acceptance criteria:
- Agents know where to look before editing.
- Agents know which folders are generated/runtime artifacts and should not be used as source-of-truth.

### 7. `docs/architecture/06-feature-map.md`

Business capability map.

Required content:
- Control Plane: dashboard, auth, users/roles/permissions, nodes, credentials, stores, health, actions, audit, catalog gateway.
- Commerce Node admin/control: stores, task orchestration, deployment, admin settings, audit, categories, products, variants, inventory, media, orders, metrics, SEO.
- Storefront internal: store context, auth, catalog by slug/id, cart/checkout, orders, payments, recommendations, newsletter, SEO.
- StorefrontV2 UI: public pages, auth forms, SEO docs, sitemap/robots, checkout redirects.
- Legacy capability summary.

Acceptance criteria:
- It is easy to decide whether a new feature belongs to Control Plane, Commerce Node admin/control, Commerce Node internal, StorefrontV2, or legacy.

### 8. `docs/architecture/07-deployment-and-local-run.md`

Deployment and local runtime guide.

Required content:
- Compose files: `compose.controlplane.yml`, `compose.commercenode.yml`, `compose.production.yml`.
- Local PostgreSQL ports: Control Plane `5433`, Commerce Node `5434`, legacy/default `5432`.
- Commerce Node Nginx role and mounted runtime config.
- Storefront deployment flow: ControlPlane API creates work via CommerceNode API; CommerceNode persists/runs local tasks asynchronously.
- Minimal run order for local QA.

Acceptance criteria:
- A future QA run can start the correct dependencies without guessing ports.
- It states that CommerceNode can act as a node deployer and may require Docker socket access in deployment environments.

### 9. `docs/architecture/08-agent-decision-rules.md`

Rules for future agents.

Required content:
- Read architecture docs before planning new work.
- Search existing patterns before adding abstractions.
- Do not refactor legacy Presentation unless explicitly requested.
- Do not copy Smartstore source into product code.
- Do not put Commerce Node credentials into Web clients.
- Keep ControlPlane Web UI-only.
- Keep StorefrontV2 store-scoped.
- Update QA todo files when touching a related feature.

Acceptance criteria:
- It is short enough to be read before work.
- It captures the architectural decisions that have repeatedly caused confusion.

## Implementation Phases

### Phase 0 - Documentation Baseline

- [x] Create `docs/architecture/README.md`.
- [x] Add this plan to the README as the current architecture-doc backlog.
- [x] Decide language policy: recommended English for agent-readability, with Vietnamese notes only where useful.

Deliverable:
- Architecture docs entrypoint exists and links to this todo.

### Phase 1 - System And Layer Map

- [x] Create `01-system-map.md`.
- [x] Create `02-layered-architecture.md`.
- [x] Verify project list with `dotnet sln BlazorShop.sln list`.
- [x] Verify project references from `.csproj` files.

Deliverable:
- Agents can identify project status and layer ownership.

### Phase 2 - Boundaries And Data Ownership

- [x] Create `03-runtime-boundaries.md`.
- [x] Create `04-data-ownership.md`.
- [x] Cross-check route groups from active controllers.
- [x] Cross-check EF context ownership and connection names.

Deliverable:
- Agents can choose API boundary and DbContext correctly.

### Phase 3 - Folder And Feature Map

- [x] Create `05-project-and-folder-guide.md`.
- [x] Create `06-feature-map.md`.
- [x] Map active V2 folders and legacy folders.
- [x] Summarize historical planning docs without duplicating every todo.

Deliverable:
- Agents can locate code and understand business ownership before editing.

### Phase 4 - Deployment And Operations

- [x] Create `07-deployment-and-local-run.md`.
- [x] Document compose files and local ports.
- [x] Document CommerceNode task/deployment flow.
- [x] Link to QA todo files and production runbook.

Deliverable:
- Agents can run local services and reason about deployment without guessing.

### Phase 5 - Agent Decision Rules

- [x] Create `08-agent-decision-rules.md`.
- [x] Cross-link from `AGENTS.md`.
- [x] Cross-link from `docs/agents/domain.md`.
- [x] Keep existing Control Plane gateway guardrail intact.

Deliverable:
- Future agents have a concise rule page before making plans or code changes.

### Phase 6 - Verification Pass

- [x] Run `dotnet sln BlazorShop.sln list`.
- [x] Run route scans for Control Plane and Commerce Node controllers.
- [x] Run reference scans to confirm no documented V2 boundary relies on legacy Presentation.
- [x] Run `git diff --check`.
- [x] Review docs for contradictions with `AGENTS.md` and `docs/agents/domain.md`.

Deliverable:
- Documentation is internally consistent and traceable to current code.

## Verification Commands

```powershell
dotnet sln BlazorShop.sln list
Get-ChildItem -Recurse -Filter *.csproj | Where-Object { $_.FullName -notmatch '\\Smartstore\\' }
rg -n "\[Route\(|class .*Controller|Http(Get|Post|Put|Delete|Patch)" BlazorShop.PresentationV2\BlazorShop.ControlPlane.API\Controllers
rg -n "\[Route\(|class .*Controller|Http(Get|Post|Put|Delete|Patch)" BlazorShop.PresentationV2\BlazorShop.CommerceNode.API\Controllers
rg -n "ControlPlaneConnection|CommerceNodeConnection|DefaultConnection" -g "appsettings*.json" BlazorShop.PresentationV2 BlazorShop.Presentation BlazorShop.Infrastructure
git diff --check
```

## Open Questions To Confirm

- Should final architecture docs be English-only for agent consumption, or bilingual English/Vietnamese?
- Should `BlazorShop.AppHost` remain documented as legacy-oriented, or will a future phase add a V2 app host?
- Should `BlazorShop.Tests` be split conceptually into legacy tests and V2 tests in documentation before any test-project refactor?
- Should architecture docs be committed as one documentation commit after all files are created, or one commit per documentation phase?

## Non-Goals

- Do not update database migrations.
- Do not change project references.
- Do not remove legacy projects from the solution.
- Do not modify QA checklist status.
- Do not copy Smartstore implementation code into BlazorShop.
