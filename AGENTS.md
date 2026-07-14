# BlazorShop Agent Guide

This file is the first stop for agents working in this repository. The goal is to keep decisions aligned with the current V2 architecture and avoid guessing from old legacy code.

## Required Reading Order

Before planning or editing code:

1. Read this file.
2. Read `docs/architecture/README.md`.
3. Read the architecture page that matches the area being changed.
4. Read relevant historical plan or QA files under `docs/refactor-control-Commerce-storefront/`.
5. Search existing code patterns with `rg` before adding abstractions.

Architecture docs:

- `docs/architecture/01-system-map.md`
- `docs/architecture/02-layered-architecture.md`
- `docs/architecture/03-runtime-boundaries.md`
- `docs/architecture/04-data-ownership.md`
- `docs/architecture/05-project-and-folder-guide.md`
- `docs/architecture/06-feature-map.md`
- `docs/architecture/07-deployment-and-local-run.md`
- `docs/architecture/08-agent-decision-rules.md`
- `docs/architecture/09-api-contract-standards.md`

## Project Shape

BlazorShop is one ecommerce product using a single-context domain documentation model.

Active shared core:

- `BlazorShop.Domain`
- `BlazorShop.Application`
- `BlazorShop.Infrastructure`
- `BlazorShop.ServiceDefaults`

Active V2 presentation/runtime:

- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API`
- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`
- `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2`

Legacy presentation:

- `BlazorShop.Presentation/BlazorShop.API`
- `BlazorShop.Presentation/BlazorShop.Web`
- `BlazorShop.Presentation/BlazorShop.Storefront`
- `BlazorShop.Presentation/BlazorShop.Web.Shared`

Legacy projects remain in the solution for comparison and migration reference. Do not extend them for V2 work unless the user explicitly asks.

## Runtime Boundaries

Control Plane path:

```text
BlazorShop.ControlPlane.Web
  -> BlazorShop.ControlPlane.API
      -> BlazorShop.CommerceNode.API
```

Forbidden path:

```text
BlazorShop.ControlPlane.Web
  -> BlazorShop.CommerceNode.API
```

Rules:

- Control Plane Web is UI-only.
- Control Plane API owns platform auth, permissions, audit, node/store registry, and Commerce Node gateway calls.
- Commerce Node API owns ecommerce node runtime, commerce admin/control APIs, scoped Storefront APIs, task orchestration, and node-local deployment.
- Storefront V2 calls Commerce Node Storefront APIs at `api/storefront/stores/{storeKey}/*` and stays store-scoped through the route path.
- Public product media URLs are store-scoped too. Storefront-facing media URLs stay clean (`/media/products/{mediaId}` and `/media/assets/{assetId}`); store scope is handled by Nginx/domain/rewrite behavior. Commerce Admin media debug endpoints use `storeKey` query.
- ProductMedia MVP uses the existing `CommerceTaskWorker` and `commerce_task` table with task type `product.media.import`. Do not introduce a separate media/product worker unless the workload grows enough to justify that extraction.

Route ownership:

- `api/control-plane/*` belongs to Control Plane API.
- `api/commerce/*` belongs to Commerce Node admin/control and is called by Control Plane API. Store-scoped Commerce Admin endpoints require `storeKey` query.
- `api/storefront/stores/{storeKey}/*` belongs to Commerce Node Storefront APIs and is called by Storefront V2. Store scope must come from the route value.
- `api/internal/*` has been removed from the active V2 Commerce Node runtime. Do not add new features or compatibility routes there.
- Legacy route groups such as `api/admin/*`, `api/public/*`, and `api/[controller]` are not V2 targets.

## API Contract Standards

Every new or changed active V2 API must satisfy `docs/architecture/09-api-contract-standards.md`.

Minimum bar:

- Stable `operationId` and short summary for every operation.
- Explicit request DTOs and response DTOs.
- Standard error response schemas for expected failures.
- Required request body metadata when the endpoint reads a body.
- Security schemes and per-operation security requirements for protected endpoints.
- Validation metadata in OpenAPI, including required fields, length/range bounds, email/password requirements, shipping-address requirements where applicable, and `minimum: 1` for quantity-like fields.
- Client-facing sort/filter enums should be named strings, not numeric enum values.
- No server-owned fields in client request contracts, such as authenticated `userId`, client-supplied order status, `IsPublished`, audit fields, credentials, or store ownership.
- No domain entities or admin-only DTOs in public schemas.
- No side-effecting `GET`; payment capture, checkout submit, logout/revocation, imports, and task commands must use an appropriate command method such as `POST`.
- Contract tests must prove response schemas, security metadata, error responses, OpenAPI validity, generator safety, and snapshots where relevant.

## Database Ownership

Use the DbContext that matches the product boundary:

- `ControlPlaneDbContext` with `ControlPlaneConnection`, local PostgreSQL port `5433`, owns platform auth, users, roles, permissions, nodes, credentials, store registry, actions, health snapshots, and audit.
- `CommerceNodeDbContext` with `CommerceNodeConnection`, local PostgreSQL port `5434`, owns ecommerce node data: commerce stores, storefront auth, catalog, variants, product media, inventory, carts, orders, payments, SEO, newsletters, deployment images, deployments, and task orchestration.
- `AppDbContext` with `DefaultConnection`, local legacy/default port `5432`, belongs to legacy commerce/storefront. Do not add new V2 features or migrations there.

Do not merge contexts just to simplify implementation. Cross-boundary behavior should go through APIs.

V2 production database upgrades follow the Smartstore-style startup migration decision:

- `BlazorShop.ControlPlane.API` may apply pending EF Core migrations for `ControlPlaneDbContext` on startup when `ControlPlane:Database:MigrateOnStartup=true`.
- `BlazorShop.CommerceNode.API` may apply pending EF Core migrations for `CommerceNodeDbContext` on startup when `CommerceNode:Database:MigrateOnStartup=true`.
- A runtime must only migrate its own DbContext.
- Do not propose a separate migrator image, CommerceNode Agent, or Control Plane "Update DB" button for MVP migration unless the user explicitly reopens that decision.
- Operators must backup the target PostgreSQL database and run one API instance per database during startup migration.

## Smartstore Usage

`Smartstore/` is reference source only. It exists to help design better ecommerce business behavior.

Allowed:

- Study entity properties.
- Study workflows and service boundaries.
- Compare Smartstore business depth with current BlazorShop behavior.
- Produce CSVs, notes, and phased plans.
- Select a small MVP subset before implementation.

Not allowed:

- Copy Smartstore implementation code into BlazorShop.
- Add runtime references to Smartstore projects.
- Import Smartstore module architecture wholesale.
- Expand scope beyond the user's selected features.

When using Smartstore for a feature:

1. Investigate the relevant Smartstore area.
2. Summarize business concepts and properties.
3. Compare with existing BlazorShop code.
4. Propose a staged plan.
5. Implement only the approved scope using BlazorShop's Layered Architecture.

## Feature Implementation Rules

Default workflow:

1. Investigate current code and docs.
2. Identify the correct boundary: Control Plane, Commerce Node admin/control, Commerce Node Storefront API, Storefront V2, shared core, or legacy reference.
3. Reuse existing DTOs, services, repositories, validators, options, clients, and response helpers where they fit.
4. Preserve migrated legacy behavior first; refactor only when requested.
5. Keep changes narrow by phase.
6. Update the relevant QA checklist.
7. Run focused verification.
8. Commit when the user asks or the active workflow requires it.

Do not introduce ABP module-style structure unless the user explicitly asks.

## QA Rules

QA checklists live under `docs/refactor-control-Commerce-storefront/`.

Use these files when related behavior changes:

- `QA-ControlPlane.todo.md`
- `QA-CommerceNode.todo.md`
- `QA-CommerceNode-TaskOrchestration.todo.md`
- `QA-StorefrontV2.todo.md`

Control Plane QA should verify auth, user management, permissions, nodes, stores, health, actions, audit, and Web/API response behavior.

Commerce Node QA should verify API route groups, credential behavior for `api/commerce/*`, Storefront scoped flows for `api/storefront/stores/{storeKey}/*`, old `api/internal/*` routes returning 404 after removal, database behavior on `CommerceNodeConnection`, catalog, orders, inventory, tasks, deployment, and audit where applicable.

Storefront V2 QA should verify public page rendering, store key route behavior, catalog pages, slug routes, auth forms, checkout redirects, SEO documents, sitemap, robots, and visible browser flows when requested.

If browser behavior changes, use Playwright. If the user asks to observe the test, run with a visible browser.

## Local Runtime Notes

Control Plane database:

```powershell
docker compose -f compose.controlplane.yml up -d
```

Commerce Node dependencies:

```powershell
docker compose -f compose.commercenode.yml up -d
```

Important local ports:

- Control Plane PostgreSQL: `5433`
- Commerce Node PostgreSQL: `5434`
- Legacy/default PostgreSQL: `5432`
- Commerce Node Nginx: `8088`
- Commerce Node imgproxy: `8089`

See `docs/architecture/07-deployment-and-local-run.md` before changing deployment or runtime behavior.

## Issue Tracker And Domain Docs

Issues are tracked in GitHub Issues for `TjnhPro/BlazorShop`; external PRs are not treated as a triage surface. See `docs/agents/issue-tracker.md`.

Use the default triage labels: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

Domain documentation uses a single-context layout for the BlazorShop ecommerce product. See `docs/agents/domain.md`.
