# Agent Decision Rules

Read this before planning or editing code.

## First Steps

1. Read `AGENTS.md`.
2. Read `docs/architecture/README.md`.
3. Read the architecture page matching the area being changed.
4. Search existing code patterns before proposing new abstractions.
5. Check the relevant planning and QA docs under `docs/refactor-control-Commerce-storefront/`.

## Legacy Rule

`BlazorShop.Presentation` is legacy.

Do not extend it for V2 work unless the user explicitly asks. If behavior must be preserved, inspect legacy code, then migrate/adapt the behavior into the active V2 boundary.

## Control Plane Rule

`BlazorShop.ControlPlane.Web` must only call `BlazorShop.ControlPlane.API`.

`BlazorShop.ControlPlane.API` is the only Control Plane boundary that may call `BlazorShop.CommerceNode.API`.

Never put Commerce Node base URLs, node keys, node secrets, allowed IP assumptions, or store security headers into Control Plane Web.

## Commerce Node Rule

Commerce Node owns node-local ecommerce runtime behavior.

Use:

- `api/commerce/*` for Control Plane/API admin-control calls.
- `api/internal/*` for Storefront V2 private/internal calls.
- `CommerceNodeDbContext` for ecommerce node data.

## Storefront Rule

Storefront V2 is store-scoped and calls Commerce Node internal APIs.

Do not make Storefront V2 call Control Plane. Do not give Storefront V2 node credentials.

## Database Rule

Use the correct context:

- Platform data: `ControlPlaneDbContext`.
- Ecommerce node data: `CommerceNodeDbContext`.
- Legacy comparison only: `AppDbContext`.

Do not add new V2 migrations to `AppDbContext`.

## Smartstore Rule

Smartstore is business reference source only.

Allowed:

- Study properties.
- Study workflows.
- Compare feature depth.
- Create candidate lists and staged plans.

Not allowed:

- Copy implementation code.
- Add runtime project references.
- Import Smartstore module architecture wholesale.
- Expand scope beyond the selected MVP.

## QA Rule

When behavior changes, update and run the relevant QA checklist:

- Control Plane: `docs/refactor-control-Commerce-storefront/QA-ControlPlane.todo.md`
- Commerce Node: `docs/refactor-control-Commerce-storefront/QA-CommerceNode.todo.md`
- Commerce Node tasks/deployment: `docs/refactor-control-Commerce-storefront/QA-CommerceNode-TaskOrchestration.todo.md`
- Storefront V2: `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`

If browser behavior changes, use Playwright. If the user asks to observe, run with a visible browser.

## Implementation Rule

Prefer narrow phases:

1. Investigate current code.
2. Compare existing project patterns.
3. If using Smartstore, document business learning first.
4. Write or update a phase todo.
5. Implement one phase.
6. Run focused verification.
7. Update QA docs.
8. Commit the phase when requested.
