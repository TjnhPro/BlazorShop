# BlazorShop Architecture Docs

This folder is the architecture entry point for agents working in BlazorShop.

Read these documents before planning or implementing new work:

1. [System Map](01-system-map.md)
2. [Layered Architecture](02-layered-architecture.md)
3. [Runtime Boundaries](03-runtime-boundaries.md)
4. [Data Ownership](04-data-ownership.md)
5. [Project And Folder Guide](05-project-and-folder-guide.md)
6. [Feature Map](06-feature-map.md)
7. [Deployment And Local Run](07-deployment-and-local-run.md)
8. [Agent Decision Rules](08-agent-decision-rules.md)

The current documentation backlog is tracked in [BlazorShop.ArchitectureDocumentation.todo.md](BlazorShop.ArchitectureDocumentation.todo.md).

## Current State

BlazorShop is a single ecommerce product with shared core layers and two presentation generations:

- `BlazorShop.Presentation` is legacy. Do not extend it unless the user explicitly asks for legacy work.
- `BlazorShop.PresentationV2` is the active direction. New Control Plane, Commerce Node, Storefront V2, and shared Web V2 work belongs here.
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

## Related Docs

- [Root agent guide](../../AGENTS.md)
- [Domain docs guide](../agents/domain.md)
- [Historical Control Plane and Commerce Storefront plans](../refactor-control-Commerce-storefront/)
- [Production runbook](../production-runbook.md)
