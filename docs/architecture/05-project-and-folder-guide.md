# Project And Folder Guide

Use this guide to find the right code before editing.

## Shared Core

### `BlazorShop.Domain`

Typical folders:

- `Entities/` - domain entities for commerce, identity, Control Plane, and Commerce Node.
- `Contracts/` - domain-facing contracts used by application/infrastructure.

Use for:

- Shared entity shape.
- Domain contracts.
- Core ecommerce concepts.

Avoid:

- API, UI, EF migration, or deployment logic.

### `BlazorShop.Application`

Typical folders:

- `DTOs/` - request/response models.
- `Services/` - application services.
- `Services/Contracts/` - service interfaces.
- `Validations/` - validators.
- `Options/` - configuration option models.
- `ControlPlane/` - Control Plane service interfaces and contracts.
- `CommerceNode/` - Commerce Node service interfaces and contracts.

Use for:

- Business service contracts.
- DTO shape shared by API/UI.
- Validation and reusable service behavior.

Avoid:

- Direct EF configuration.
- Controller-specific response formatting.
- Browser-only code.

### `BlazorShop.Infrastructure`

Typical folders:

- `Data/ControlPlane/` - Control Plane EF context, migrations, services, seeders.
- `Data/CommerceNode/` - Commerce Node EF context, migrations, repositories, services, seeders.
- `Repositories/Authentication/AppRoleManager.cs` - shared Identity role adapter used by active Control Plane and Commerce Node auth infrastructure.
- `Services/` - infrastructure service implementations.

Use for:

- EF persistence.
- Repository implementations.
- Auth infrastructure.
- External service adapters.
- Context-specific transaction and audit services.

Avoid:

- UI logic.
- Controller routing.
- Mixing Control Plane persistence with Commerce Node persistence.

## Active V2 Projects

### `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API`

Important folders:

- `Controllers/` - `api/control-plane/*` endpoints.
- `Controllers/CommerceGateway/` - Control Plane API capability controllers that forward Commerce Admin operations to Commerce Node through shared server-side transport.
- `Authorization/` - Control Plane policies and auth setup.
- `Middleware/` - correlation and API behavior middleware.
- `Responses/` - Control Plane API envelope helpers.
- `ControlPlaneDatabaseBootstrapper.cs` - startup migration and Development seeding for `ControlPlaneDbContext`.

Use for:

- Platform-facing APIs.
- Control Plane auth and permission enforcement.
- Commerce Admin gateway behavior to Commerce Node.

Do not:

- Put Commerce Node credentials in Web clients.
- Store commerce catalog/order/customer data directly here.
- Migrate `CommerceNodeDbContext` from this runtime.

### `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web`

Important folders:

- `Pages/` - Blazor WASM pages.
- `Layout/` - Control Plane layouts.
- `Services/` - typed clients calling Control Plane API.
- `Services/Commerce/` - capability-specific commerce admin clients. These call Control Plane API only and never hold Commerce Node URLs or credentials.
- `Authentication/` - Web client auth state.
- `wwwroot/` - static assets and Web config.

Use for:

- Control Plane UI.
- Client-side display, forms, and typed API clients to Control Plane API.

Do not:

- Call Commerce Node directly.
- Add node secrets, node keys, allowed IP config, or Commerce Node base URLs.

### `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API`

Important folders:

- `Controllers/` - `api/commerce/*` and `api/storefront/stores/{storeKey}/*` endpoints.
- `Configuration/` - node, worker, Nginx, and deployment options.
- `Deployment/` - Storefront Docker and Nginx deployment services.
- `Endpoints/` - endpoint mapping helpers such as health.
- `Middleware/` - Commerce Node credential middleware.
- `Responses/` - API response helpers.
- `Tasks/` - task handlers.
- `Workers/` - background task worker.
- `runtime/` - generated/runtime Nginx config and logs. Treat as runtime state, not source-of-truth business logic.
- `uploads/` - local upload storage.
- `CommerceNodeDatabaseBootstrapper.cs` - startup migration and Development QA fixture bootstrap for `CommerceNodeDbContext`; it must not reset existing store runtime configuration on restart.

Use for:

- Node-local ecommerce API.
- Scoped Storefront API.
- Node task orchestration.
- Store deployment support.

Do not:

- Add Control Plane UI logic.
- Persist platform-level users/permissions/credentials here.
- Migrate `ControlPlaneDbContext` from this runtime.

### `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`

Important folders:

- `Components/` - Razor components.
- `Pages/` - server-side storefront pages, grouped by render ownership:
  - `Pages/Ssr/` - server-rendered pages where WASM is not required for the primary function, such as auth, DB content, maintenance, and not-found pages.
  - `Pages/Hybrid/` - SEO/snapshot route pages that compose interactive features, such as catalog, product, cart, checkout, and payment result pages.
  - `Pages/WasmHost/` - server-owned route/security/bootstrap boundaries for WASM-owned features, currently customer account routes.
- `Services/` - Storefront API clients, SEO, sitemap, robots, auth form handlers.
- `Options/` - Storefront API/public URL options.
- `Configuration/` - options validators.
- `wwwroot/` - static storefront assets.

Use for:

- Public/store-scoped storefront UI.
- Storefront login/register/logout forms.
- SEO and public discovery documents.
- Store key propagation to Commerce Node Storefront API.
- Storefront-owned presentation/local endpoint contracts plus generated Storefront client adapters.
- `Web.SharedV2` utilities only when they are genuinely shared browser/runtime helpers.

Asset and layout rules:

- Root Storefront CSS and scripts must stay explicit in `App.razor`.
- `StorefrontBrandHead` must render before `HeadOutlet`, and brand/runtime metadata must not use layout-level `HeadContent`.
- Page SEO metadata belongs in page/SEO components such as `SeoHead`.
- Page-specific CSS should prefer scoped CSS or controlled app-owned classes in `wwwroot/css`.
- Page-specific JavaScript should prefer `IJSRuntime` module imports. Add root scripts only when they must load with the root document, and update the root asset allowlist tests with the reason.
- Store configuration must not accept arbitrary public script or stylesheet injection.
- `MainLayout.razor` owns the global header, toast DOM region, `<main>`, and footer. Page-level structure belongs in optional components such as `StorefrontPageShell` and catalog-only components such as `CatalogFilterPanel`.

Do not:

- Call Control Plane.
- Manage node credentials.
- Reference `BlazorShop.Application`, `BlazorShop.Domain`, `BlazorShop.Infrastructure`, Commerce Node API, or Control Plane API projects.
- Import `Web.SharedV2.Models` business contracts.

### `BlazorShop.PresentationV2/BlazorShop.Storefront.Components`

Use for:

- Reusable Storefront Razor components that can be shared between Storefront V2 server rendering and its interactive WebAssembly assembly.
- Presentation-only component behavior that belongs to the public Storefront experience.
- `Features/*` capability components such as account, cart, checkout, deals, catalog summaries, and product gallery/purchase panels.
- Component-facing presentation models that contain only render/input state and are mapped by the Storefront V2 host from API DTOs or local endpoint contracts.

Do not:

- Put Commerce Node clients, Control Plane clients, credentials, EF logic, or product business services here.
- Reference `Web.SharedV2`, Storefront route helpers, Storefront API clients, Application, Domain, Infrastructure, Control Plane, or Commerce Node runtime projects from `Features/*`.
- Add public API DTOs, admin request models, store ownership fields, credentials, or server-owned fields to component-facing models.
- Use this as a general design system for Control Plane.

### `BlazorShop.PresentationV2/BlazorShop.Storefront.WASM`

Use for:

- Storefront V2 interactive WebAssembly components and bootstrapping required by `AddInteractiveWebAssemblyRenderMode`.
- Browser-side Storefront UI behavior that is intentionally part of the public Storefront runtime.

Do not:

- Call Control Plane.
- Hold node credentials.
- Duplicate server-owned Storefront API contract behavior when `Storefront.V2` already owns the server/client integration.

### `BlazorShop.Storefront.Client`

Status:

- Active generated Storefront API client under `BlazorShop.PresentationV2/BlazorShop.Storefront.Client`.
- Generated from the Commerce Node Storefront OpenAPI snapshot by `scripts/generate-storefront-client.ps1` using the pinned NSwag dotnet tool.

Use for:

- Generated request and response DTOs from Commerce Node Storefront OpenAPI.
- Generated typed HTTP clients and transport primitives.
- JSON serialization settings, route construction with `storeKey`, cancellation token propagation, and error deserialization.
- Partial hooks and injected `HttpClient` for correlation, tracing, and caller-owned retry policy wiring.
- Small typed facades only when a generated client shape is proven too raw for Storefront consumers.

Do not:

- Add Razor components, CSS, browser storage, UI state, checkout/cart business rules, or handwritten duplicate API DTO clones.
- Reference `BlazorShop.Domain`, `BlazorShop.Application`, `BlazorShop.Infrastructure`, `BlazorShop.CommerceNode.API`, `BlazorShop.ControlPlane.API`, or `BlazorShop.Storefront.V2`.

### `BlazorShop.Storefront.Runtime`

Status:

- Active minimal runtime package under `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime`.
- Created by the Storefront Starter Foundation after Starter became the second consumer of generated-client registration, capability reading, and error normalization primitives.

Use for:

- Store context/options abstractions.
- Storefront API client registration helpers.
- Capability/configuration readers.
- Normalized error mapping primitives.
- BFF-safe result mapping primitives.

Do not:

- Add Storefront V2 layout/design, CSS/assets, store-specific composition, backend business rules, provider secrets, or references to backend/core/API projects.

### Storefront Feature Module Boundary

Current ownership map:

- `BlazorShop.Storefront.Client` owns generated Storefront API transport/contracts.
- `BlazorShop.Storefront.Components/Features/*` owns portable presentation-only feature components.
- `BlazorShop.Storefront.V2` owns route composition, SEO, BFF endpoints, session/cart-token handling, store resolution, deployment, and storefront-specific design.
- `BlazorShop.Storefront.Runtime` and `Storefront.Features.*` projects are deferred until a second consumer or repeated neutral runtime code proves the need.

Do not create feature packages just to move code out of Storefront V2. Extract only when it removes real duplication and can stay independent of Storefront V2 design, routes, BFF endpoints, and backend/core/API projects.

### `BlazorShop.Storefront.Starter`

Status:

- Active neutral skeleton source for deterministic generated storefronts.

Use for:

- Neutral SSR, Hybrid, and WASM-host route skeletons.
- Examples of generated `BlazorShop.Storefront.Client` package consumption.
- Same-origin BFF examples for protected browser flows.
- Store bootstrap, capability reading, feature placement, loading/error/empty states, and generation manifest conventions.
- Deterministic generated storefront output under ignored artifact roots such as `artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof`.

Do not:

- Copy Storefront V2 source as the Starter baseline.
- Turn Storefront V2 into a neutral template.
- Reference `BlazorShop.Storefront.V2`, backend/core/API projects, Control Plane Web, or `Web.SharedV2.Models` business contracts.
- Copy the manual `StorefrontApiClient` transport from Storefront V2.
- Move pricing, sellability, cart validation, checkout, order placement, payment, or authorization rules into Starter.

Protected areas for scaffolding or AI generation:

- generated client source;
- runtime security primitives;
- BFF transport/security code;
- package/version manifests;
- generated storefront manifests.

### Generated Storefront Artifacts

Status:

- Disposable StorefrontBuilder proof output under `artifacts/storefront-builder/generated/{ProjectName}` for manual proof runs or `obj/storefront-builder/generated/{ProjectName}` for automated proof runs.

Use for:

- Proving Starter can build, publish, and run outside the monorepo from packages/configuration.
- Reviewing generated pages, generated CSS, asset manifests, and QA artifacts.
- Running StorefrontBuilder static validation, isolation, visual smoke QA, and commerce-regression checks.

Do not:

- Add generated proof output to `BlazorShop.sln` by default.
- Treat generated proof output as a platform contract owner.
- Backport store-specific CSS, assets, generated pages, or analysis artifacts into Starter.
- Use generated proof output as evidence that generated storefronts may reference Storefront V2 or backend/core/API projects.

### `tools/BlazorShop.AI.StorefrontBuilder`

Status:

- Active development-time tooling for generated storefront preparation.

Important folders:

- `scripts/capture/` - Playwright capture and page discovery helpers.
- `scripts/generate/` - project creation, review artifact writing, token extraction, visual foundation, composition, and generated manifest updates.
- `scripts/validate/` - schema, project, asset, CSS, composition, idempotency, guard, and static gate validation.
- `scripts/qa/` - visual QA and commerce-regression browser runners.

Use for:

- Creating generated storefronts from Starter.
- Writing and validating visual reverse engineering artifacts.
- Regenerating generated CSS, page, component, and manifest output.
- Running browser QA against generated storefronts.

Do not:

- Add production API hosting behavior here.
- Add runtime dependencies from Commerce Node, Control Plane, Storefront V2, or generated storefront projects back to this tooling.
- Store secrets, node credentials, or production deployment state in generated analysis artifacts.

### `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2`

Important folders:

- `Authentication/` - auth session sync helpers.
- `BrowserStorage/` - browser storage abstractions.
- `CookieStorage/` - cookie storage abstractions.
- `Helper/` - token/API helper logic.
- `Services/` - shared services such as toast.
- `Toast/` - toast options and UI helpers.

Use for:

- Shared UI/browser utilities across V2 Web projects.
- Browser storage, cookie, auth-session sync, and toast/helper behavior that is useful to more than one active V2 frontend.
- Small framework-neutral helper services where sharing reduces duplicated behavior without coupling product UI.

Do not:

- Put project-specific business logic here.
- Put Commerce Node credentials here.
- Move Storefront header/footer/cart/toast DOM behavior here unless Control Plane has the same real need.
- Move Control Plane nav/sidebar/topbar/page chrome here unless Storefront has the same real need.
- Use `Web.SharedV2` as a forced visual design system; Storefront and Control Plane intentionally keep different UI density and product identity.

## Legacy Presentation

`BlazorShop.Presentation` has been removed from the active branch.

Use git history or the `legacy-presentation-final` tag for:

- Behavior comparison.
- Migration reference.
- QA comparison when preserving behavior.

Do not:

- Add new V2 features.
- Create new V2 runtime dependencies on legacy Presentation projects.

## Planning And QA Docs

Historical planning and QA files live under:

```text
docs/refactor-control-Commerce-storefront/
```

Important QA files:

- `QA-ControlPlane.todo.md`
- `QA-CommerceNode.todo.md`
- `QA-CommerceNode-TaskOrchestration.todo.md`
- `QA-StorefrontV2.todo.md`

When a feature changes behavior, update the matching QA todo and verify the relevant cases.
