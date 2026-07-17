# BlazorShop Architecture Docs

This folder is the architecture entry point for agents and contributors working in BlazorShop.

Read these documents before planning or implementing new work:

1. [System Map](01-system-map.md)
2. [Layered Architecture](02-layered-architecture.md)
3. [Runtime Boundaries](03-runtime-boundaries.md)
4. [Data Ownership](04-data-ownership.md)
5. [Project And Folder Guide](05-project-and-folder-guide.md)
6. [Feature Map](06-feature-map.md)
7. [Deployment And Local Run](07-deployment-and-local-run.md)
8. [Agent Decision Rules](08-agent-decision-rules.md)
9. [API Contract Standards](09-api-contract-standards.md)

Historical implementation plans and QA checklists are tracked under [docs/refactor-control-Commerce-storefront](../refactor-control-Commerce-storefront/). This folder is the current architecture source of truth; do not rely on older plan files when they conflict with these pages.

## Current State

BlazorShop is a single ecommerce product with shared core layers and two presentation generations:

- `BlazorShop.Presentation` is legacy. Do not extend it unless the user explicitly asks for legacy work.
- `BlazorShop.PresentationV2` is the active direction. New Control Plane, Commerce Node, Storefront V2, Storefront component/WASM, and shared Web V2 work belongs here.
- `BlazorShop.Domain`, `BlazorShop.Application`, and `BlazorShop.Infrastructure` are shared core layers used by active V2 projects.
- `Smartstore/` is reference source for ecommerce business research only. Do not copy Smartstore implementation code into BlazorShop and do not add runtime references to Smartstore projects.

## Mandatory Boundary

Control Plane Web is UI-only:

```text
BlazorShop.ControlPlane.Web
  -> BlazorShop.ControlPlane.API
      -> BlazorShop.CommerceNode.API
```

Never design this path:

```text
BlazorShop.ControlPlane.Web
  -> BlazorShop.CommerceNode.API
```

Commerce Node credentials, node secrets, allowed IP behavior, Commerce Node base URLs, and store security headers must stay behind `BlazorShop.ControlPlane.API`.

## API Contract Standard

Every new or changed active V2 API must follow [API Contract Standards](09-api-contract-standards.md).

The short version:

- Do not expose domain entities as public HTTP schemas.
- Use explicit request and response DTOs.
- Publish validation metadata, error responses, response schemas, operation IDs, summaries, required request bodies, and security requirements in OpenAPI.
- Keep side-effecting operations off `GET`.
- Add contract tests and Swagger snapshots when the API is consumed by another V2 runtime or external client.

## Related Docs

- [Root agent guide](../../AGENTS.md)
- [Repository README](../../README.md)
- [Contributing guide](../../CONTRIBUTING.md)
- [Security policy](../../SECURITY.md)
- [Domain docs guide](../agents/domain.md)
- [Historical Control Plane and Commerce Storefront plans](../refactor-control-Commerce-storefront/)
- [Production runbook](../production-runbook.md)
