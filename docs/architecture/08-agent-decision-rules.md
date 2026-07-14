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

Control Plane list/search/query APIs must use a page contract:

- Use `pageNumber/pageSize` in public API and Web client contracts.
- Include `items`, `totalCount`, `pageNumber`, `pageSize`, and `totalPages` in list responses.
- Do not expose `skip/take` to Control Plane Web.
- Do not hide paging with `.Take(100)` or `.Take(200)`.
- If a collection is a static lookup/catalog and is intentionally not paged, do not name the endpoint or client method `List*`.

## Commerce Node Rule

Commerce Node owns node-local ecommerce runtime behavior.

Use:

- `api/commerce/*` for Control Plane/API admin-control calls. Store-scoped Commerce Admin endpoints must use query `storeKey`; do not use `X-Store-Key`.
- `api/storefront/stores/{storeKey}/*` for Storefront V2 calls. Store scope must come from route value `{storeKey}`; do not use `X-Store-Key`.
- `api/internal/*` has been removed from the active V2 Commerce Node runtime. Do not add compatibility routes there.
- `CommerceNodeDbContext` for ecommerce node data.
- Existing `CommerceTaskWorker` and `commerce_task` for asynchronous node-local work unless a separate worker has been explicitly approved.

## API Contract Rule

Before adding or changing any active V2 API, read `docs/architecture/09-api-contract-standards.md`.

Every API operation must be generator-safe and AI-readable from OpenAPI:

- Stable `operationId`.
- Short summary.
- Explicit request DTO when a body exists.
- Explicit success response DTO.
- Standard error response DTOs.
- Required request body metadata when the body is required.
- Security requirement metadata when protected.
- Validation metadata for required fields, formats, lengths, ranges, paging bounds, quantity minimums, passwords, and shipping addresses where applicable.

Do not expose domain entities, EF entities, admin-only DTOs, secrets, node credentials, store ownership fields, audit fields, authenticated `userId`, client-supplied order status, or `IsPublished` in client request/public schemas.

Client-facing sort/filter values should be named strings. Do not use numeric enum values in public contracts unless preserving an already-approved compatibility contract.

Side-effecting operations must not be `GET`. Payment capture, checkout submit, logout/revocation, imports, and task commands must use command methods such as `POST`.

API changes need focused contract tests:

- Swagger/OpenAPI fetches and validates.
- 100% of declared responses have schemas.
- Protected operations have security metadata.
- No operation declares only 200 OK.
- Public schemas do not expose unsafe entities or fields.
- Request body, validation, and paging metadata are asserted where relevant.
- A C# or TypeScript client generation smoke, or equivalent generator-safety check, passes.
- Swagger snapshots are updated when the surface is consumed by another V2 runtime or external client.

## Storefront Rule

Storefront V2 is store-scoped and calls Commerce Node Storefront APIs at `api/storefront/stores/{storeKey}/*`.

Do not make Storefront V2 call Control Plane. Do not give Storefront V2 node credentials.

Public Storefront media is also store-scoped. Do not design product media as a global file endpoint. Public media URLs stay clean and are scoped by Nginx/domain/rewrite behavior; Commerce Admin media debug endpoints use `storeKey` query.

## Database Rule

Use the correct context:

- Platform data: `ControlPlaneDbContext`.
- Ecommerce node data: `CommerceNodeDbContext`.
- Legacy comparison only: `AppDbContext`.

Do not add new V2 migrations to `AppDbContext`.

V2 database upgrades use startup EF Core migrations for MVP:

- Control Plane API owns startup migration for `ControlPlaneDbContext`.
- Commerce Node API owns startup migration for `CommerceNodeDbContext`.
- Each runtime migrates only its own database.
- Do not introduce a migrator image, CommerceNode Agent, or Control Plane migration UI unless that architecture is explicitly reopened.
- Production operation requires a manual database backup and a single API instance during migration.

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
