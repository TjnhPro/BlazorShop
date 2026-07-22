# Agent Decision Rules

Read this before planning or editing code.

## First Steps

1. Read `AGENTS.md`.
2. Read `docs/architecture/README.md`.
3. Read the architecture page matching the area being changed.
4. Search existing code patterns before proposing new abstractions.
5. Check the relevant planning and QA docs under `docs/refactor-control-Commerce-storefront/`.

## Legacy Rule

Legacy `BlazorShop.Presentation` projects have been removed from the active branch.

Do not recreate them for V2 work. If behavior must be preserved, inspect git history or the `legacy-presentation-final` tag, then migrate/adapt the behavior into the active V2 boundary.

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
- Commerce Node API middleware must resolve Storefront route scope and Commerce Admin query scope into `StoreExecutionContext` before services run. Infrastructure must consume `IStoreExecutionContextAccessor` through `ICommerceStoreContext`, not inspect `HttpContext`, route values, query strings, headers, or hosts.
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

Storefront-visible store profile fields come from the current Commerce Store runtime profile through the Storefront display context. Use that context before rendering catalog, settings, customer-facing contact data, branding, icons, language, or currency.

`DefaultCulture` is the source for document language; derive language from culture instead of adding a separate `DefaultLanguage` field.

Do not add `DefaultTheme` until a real theme system exists. A future theme setting should be a `ThemeKey` allowlist with implemented themes, not a free-form string.

Home page metadata uses the reserved published StorefrontPage slug `home`. The `/` route may consume that page's SEO fields, but `/` remains the canonical home route. Do not add another generic home metadata blob to `CommerceStore`.

Storefront root assets are intentionally explicit:

- Keep root CSS and script entries in `BlazorShop.Storefront.V2/App.razor` allowlisted by tests.
- Keep `blazor.web.js` before `storefrontCommerce.js` unless a focused test and browser QA justify changing the order.
- Use `SeoHead` and page-level `HeadContent` for metadata; do not move brand/runtime metadata into layout-level `HeadContent`.
- Prefer JS module imports through `IJSRuntime` for page-specific behavior.
- Do not add DB-configured or store-configured arbitrary public scripts/styles.
- Do not add a runtime asset registry until repeated page-specific asset registration creates real maintenance cost.

Storefront cache/versioning rules:

- Use ASP.NET Core static web assets and `MapStaticAssets()` before adding custom cache-busting.
- Fingerprinted framework/static assets may use long-cache behavior provided by the framework or by Commerce Node media endpoints.
- Dynamic Storefront pages, maintenance pages, current-store/config reads, checkout/auth pages, SEO documents, and error states must not receive immutable cache headers.
- Product and asset media proxy responses may forward Commerce Node `Cache-Control`/`ETag` headers, but Storefront V2 must not invent a broader immutable cache policy for dynamic routes.

Control Plane Web asset rules:

- Keep Control Plane root CSS and scripts explicit in `BlazorShop.ControlPlane.Web/wwwroot/index.html`.
- Keep `vendor/fontawesome/css/all.min.css`, `css/site.css`, `css/app.css`, `_framework/blazor.webassembly.js`, and `js/downloads.js` allowlisted by tests.
- Keep FontAwesome copied through the existing MSBuild `CopyFontAwesomeAssets` target.
- Keep Control Plane feature JavaScript module-imported through services when possible. Root scripts are only for boot-time or host-global helpers such as file downloads.
- Browser static assets and `wwwroot` config must point only to Control Plane API, never directly to Commerce Node `api/commerce/*`, `api/internal/*`, node keys, node secrets, or Commerce Node local ports.

Shared V2 UI/asset rules:

- Share browser behavior helpers through `BlazorShop.Web.SharedV2` only when both active V2 frontends have a real use case.
- Keep Storefront-specific header, footer, navigation, cart/toast DOM integration, SEO shell, and public commerce page structure in `BlazorShop.Storefront.V2`.
- Keep Control Plane-specific nav/sidebar/topbar/page header and dense operational components in `BlazorShop.ControlPlane.Web`.
- Do not create a shared visual shell or asset registry just to reduce superficial markup similarity.

## Database Rule

Use the correct context:

- Platform data: `ControlPlaneDbContext`.
- Ecommerce node data: `CommerceNodeDbContext`.

The legacy `AppDbContext`/`DefaultConnection` infrastructure path has been removed. Use git history for legacy comparison when needed.

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

For active V2 build/test verification, prefer:

```powershell
dotnet build BlazorShop.sln --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore
```

Use the mixed `BlazorShop.Tests` project only when the change intentionally touches legacy Presentation, a test has not yet been migrated into `BlazorShop.Tests.V2`, or a compatibility check explicitly needs the old mixed surface.

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
