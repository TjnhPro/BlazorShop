# Contributing to BlazorShop

BlazorShop is in an active V2 migration. Before changing code, read:

1. [AGENTS.md](AGENTS.md)
2. [docs/architecture/README.md](docs/architecture/README.md)
3. The architecture page that matches the area you are changing
4. The relevant QA checklist under [docs/refactor-control-Commerce-storefront](docs/refactor-control-Commerce-storefront)

The current default branch in this repository is `master`.

## Report Bugs Or Request Enhancements

Use GitHub Issues for `TjnhPro/BlazorShop`:

- Bugs: include the affected runtime, exact route/page/API, reproduction steps, expected behavior, actual behavior, and any relevant logs.
- Enhancements: include the product area, user workflow, expected contract/API shape if applicable, and what should stay out of scope.
- Security issues: follow [SECURITY.md](SECURITY.md) instead of opening a public issue.

## Architecture Rules

New V2 work belongs in the active V2 boundary unless the task explicitly asks for legacy work.

Required Control Plane path:

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

Route ownership:

- `api/control-plane/*` belongs to Control Plane API.
- `api/commerce/*` belongs to Commerce Node admin/control and is called by Control Plane API.
- `api/storefront/stores/{storeKey}/*` belongs to Commerce Node Storefront APIs and is called by Storefront V2.
- `api/internal/*`, `api/admin/*`, `api/public/*`, and legacy `api/[controller]` routes are not V2 targets.

Data ownership:

- Use `ControlPlaneDbContext` for platform auth, users, permissions, node/store registry, credentials, actions, health, and audit.
- Use `CommerceNodeDbContext` for ecommerce stores, catalog, customers, carts, checkout, orders, payments, media, SEO, tasks, messages, and deployment state.
- Do not add new V2 migrations to legacy `AppDbContext`.

## API Contract Rules

Every new or changed active V2 API must satisfy [docs/architecture/09-api-contract-standards.md](docs/architecture/09-api-contract-standards.md).

Minimum checklist:

- Stable `operationId` and short summary.
- Explicit request DTO and response DTO.
- Standard error response schema for expected failures.
- Required request body metadata when the endpoint reads a body.
- Security requirement for protected endpoints.
- Validation metadata for required fields, length/range bounds, email/password fields, shipping address fields, and quantity minimums.
- Named string values for client-facing sort/filter enums.
- No domain entities, admin-only DTOs, credentials, audit fields, user-owned server fields, order status, or `IsPublished` in public request schemas.
- No side-effecting `GET`.
- Contract tests and Swagger snapshots when the API is consumed by another runtime or generated client.

## Local Development

Run the active V2 stack with:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

Common URLs:

- Control Plane Web: `http://localhost:5281`
- Control Plane API Swagger: `http://localhost:5280/swagger`
- Commerce Node API Swagger: `http://localhost:5180/swagger`
- Storefront V2: `http://localhost:18598`

Stop it with:

```powershell
.\scripts\stop-v2-local.ps1
```

## Tests And QA

Run focused tests when possible:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release
```

Run full solution verification before broad changes or release work:

```powershell
dotnet test BlazorShop.sln -c Release
```

Update the relevant QA checklist when behavior changes:

- [QA-ControlPlane.todo.md](docs/refactor-control-Commerce-storefront/QA-ControlPlane.todo.md)
- [QA-CommerceNode.todo.md](docs/refactor-control-Commerce-storefront/QA-CommerceNode.todo.md)
- [QA-CommerceNode-TaskOrchestration.todo.md](docs/refactor-control-Commerce-storefront/QA-CommerceNode-TaskOrchestration.todo.md)
- [QA-StorefrontV2.todo.md](docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md)

Browser-facing changes should be verified with Playwright. API contract changes should update contract tests and Swagger snapshots.

## Pull Request Checklist

Before opening a PR:

- The change targets the correct V2 boundary.
- Legacy projects were not extended unless intentionally requested.
- Database changes use the correct DbContext and migration folder.
- API contract metadata and tests are updated for changed endpoints.
- QA checklist items are added or updated for changed behavior.
- Docs are updated when architecture, configuration, features, scripts, routes, or operator behavior changes.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release` passes, or the PR explains why it was not run.

## Code Of Conduct

Participation is governed by [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md). Report conduct concerns privately to project maintainers through GitHub or another maintainer-provided private channel. Do not post sensitive personal information in public issues.
