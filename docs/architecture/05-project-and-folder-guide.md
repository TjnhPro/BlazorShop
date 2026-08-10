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

### `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation`

Important folders:

- `App/` - shared Storefront root App/Routes and head/script slots.
- `Pages/` - shared SSR, Hybrid, and WASM-host route shells. These are BlazorShop ownership folders, not direct ASP.NET render mode names.
- `Endpoints/` - same-origin browser/BFF/local endpoint groups.
- `Services/` - page services, route contexts, API adapter contracts, and generated Runtime-backed adapters.
- `Seo/` - SEO metadata, canonical, robots, sitemap, and structured-data composition.
- `Media/` - storefront media endpoint/proxy composition.
- `Hosting/` - `UseStorefrontPresentation()` and `MapStorefrontPresentation()` aggregation helpers.
- `ViewModels/` and `ViewSlots/` - host-provided view-slot contracts.

Use for:

- Shared storefront application orchestration.
- Route composition and route-owned page contexts.
- Same-origin browser/BFF contracts and endpoint groups.
- SEO/discovery, media, auth/system/cart/checkout/account route shells, and local browser-safe endpoint behavior used by V2, Starter, and generated storefronts.

Do not:

- Add Storefront V2-specific visual markup, CSS, copy, or generated visual output.
- Reference `BlazorShop.Storefront.V2`, `BlazorShop.Storefront.Starter`, generated storefront projects, Control Plane, Commerce Node API, Application, Domain, Infrastructure, or `BlazorShop.Web.SharedV2`/`Web.SharedV2`.
- Put ecommerce truth such as pricing, sellability, inventory, checkout validity, or order creation here.

### `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`

Important folders:

- `Components/` - Razor components.
- `Pages/` - host-provided view templates grouped by BlazorShop render ownership. These are view components registered into Storefront Presentation, not route owners; `Pages/Hybrid` does not mean `.NET InteractiveAuto` and does not require `Components.Hybrid`.
- `Services/` - host-specific service registration and any remaining host-local adapters.
- `Options/` - Storefront API/public URL options.
- `Configuration/` - options validators.
- `wwwroot/` - static storefront assets.

Use for:

- Public/store-scoped Storefront V2 visual implementation.
- Host configuration and view registration for Storefront Presentation.
- Storefront V2 layout, CSS/assets, final copy, and interactive V2 component placement.
- Store key propagation to Commerce Node Storefront API.
- Static assets for interactive cart, checkout, account, and other surfaces hosted from the Storefront V2 WASM client assembly.

Asset and layout rules:

- Root Storefront CSS and scripts must stay explicit in `BlazorShop.Storefront.Presentation/App/StorefrontApp.razor` through host-provided head/script slots, and host-provided asset entries must resolve static web assets through Razor `@Assets[...]` so published/fingerprinted URLs are used instead of raw root paths.
- Storefront V2 host CSS owns `css/site.css`, Storefront V2.WASM interactive CSS owns `css/wasm-site.css`, and handwritten V2 structural overrides own `css/storefront.css`; the root document must load them in that order.
- `StorefrontIconHead` owns store favicon/png/apple/MS tile tags; `StorefrontBrandHead` owns non-icon storefront metadata such as the language marker. Host application head components must render them before `HeadOutlet`, and brand/runtime metadata must not use layout-level `HeadContent`.
- Page SEO metadata belongs in page/SEO components such as `SeoHead`.
- Page-specific CSS should prefer scoped CSS or controlled app-owned classes in `wwwroot/css`.
- Page-specific JavaScript should prefer `IJSRuntime` module imports. Add root scripts only when they must load with the root document, and update the root asset allowlist tests with the reason.
- Store configuration must not accept arbitrary public script or stylesheet injection.
- Storefront V2 layout views own the global header, toast DOM region, `<main>`, and footer through Presentation view slots. Page-level structure belongs in optional host views such as `StorefrontPageShell` and catalog-only components such as `CatalogFilterPanel`.

Do not:

- Call Control Plane.
- Manage node credentials.
- Reference `BlazorShop.Application`, `BlazorShop.Domain`, `BlazorShop.Infrastructure`, Commerce Node API, or Control Plane API projects.
- Import `BlazorShop.Web.SharedV2`/`Web.SharedV2`.
- Map duplicate route/BFF/SEO endpoint groups that Storefront Presentation already owns.

### `BlazorShop.PresentationV2/BlazorShop.Storefront.Components`

Use for:

- Headless Storefront presentation contracts under `Contracts/{Capability}`.
- Browser-safe behavior/state primitives under `Headless/{Capability}`.
- Reusable component descriptor contracts under `Contracts/Components`.
- Component-facing presentation models that contain only render/input state and are mapped by the Storefront V2 host from API DTOs or local endpoint contracts.

Do not:

- Put Commerce Node clients, Control Plane clients, credentials, EF logic, or product business services here.
- Reference `Web.SharedV2`, Storefront route helpers, Storefront API clients, Application, Domain, Infrastructure, Control Plane, or Commerce Node runtime projects.
- Add public API DTOs, admin request models, store ownership fields, credentials, or server-owned fields to component-facing models.
- Use this as a general design system for Control Plane.
- Add Razor components, static web assets, V2 layout/theme implementations, visual class bags, final copy, or generated visual output.
- Reintroduce `Features/*` compatibility wrappers without a new architecture decision; visual templates belong in `Storefront.V2`, `Storefront.Starter`, or a generated/custom storefront.

### `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives`

Status:

- Browser-safe render-only Razor primitives.
- Not a component mode, descriptor inventory, registry, transport layer, or design system.

Use for:

- Small reusable semantic Razor primitives that consume `BlazorShop.Storefront.Components` contracts.
- Stable `data-storefront-*` hooks, accessibility markup, links/buttons, and fully host-supplied class slots.
- Product Summary primitive rendering shared by Storefront V2 SSR surfaces and V2.WASM rail item rendering.
- Product Detail gallery render markup through `StorefrontProductGallery`, using `ProductGalleryItem`, `ProductGalleryLabels`, `ProductGalleryClasses`, and `ProductGalleryState` while V2 owns final labels/classes and JavaScript progressive enhancement.

Do not:

- Reference `BlazorShop.Storefront.Presentation`, `BlazorShop.Storefront.Browser`, `Runtime`, `Client`, V2, V2.WASM, Starter, Starter.WASM, generated storefront projects, backend/core/API projects, Control Plane projects, or `Web.SharedV2`.
- Use `HttpClient`, `IJSRuntime`, `@rendermode`, component descriptors, direct `/api/*`, Commerce Node URLs, or localhost backend URLs.
- Own theme CSS, static assets, V2 layout classes, store-specific copy, generated output, or business decisions.

Allowed direct project references:

- `BlazorShop.Storefront.Components`

### `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr`

Status:

- Reusable Storefront SSR component mode library.
- Real reference components now exist; see `BlazorShop.PresentationV2/COMPONENT-MODES.md` for the current source of truth.

Use for:

- Server-rendered reusable components that consume prepared Presentation contexts or component contracts.
- Semantic hooks, accessibility markup, forms, links, `RenderFragment`, and host-supplied class slots.
- Product Detail display components such as `StorefrontProductPricing`, `StorefrontProductAvailability`, and informational `StorefrontProductVariantList` over prepared Presentation product views.

Do not:

- Reference `BlazorShop.Storefront.Browser`, `Runtime`, `Client`, V2, V2.WASM, Starter, Starter.WASM, generated storefront projects, backend/core/API projects, Control Plane projects, or `Web.SharedV2`.
- Use `HttpClient`, `IJSRuntime`, `@rendermode`, `InteractiveWebAssembly`, direct `/api/*`, Commerce Node URLs, or localhost backend URLs.
- Own theme CSS, V2 layout classes, store-specific copy, or generated output.

Allowed direct project references:

- `BlazorShop.Storefront.Components`
- `BlazorShop.Storefront.Presentation`

### Retired `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid`

Status:

- Retired historical compatibility project.
- H3 moved the public `contact-form` descriptor to `BlazorShop.Storefront.Components.WasmHost`, deleted the historical nested contact shell, and removed the physical Hybrid project from the active solution.
- `Hybrid` remains a semantic render/runtime classification, not a required project name.

Use for:

- Historical archaeology through git history or old completed plans only.
- Understanding why semantic `Hybrid` descriptors can physically live in `Components.WasmHost`.

Do not:

- Recreate the project, add new reusable components under this path, or use nested server-shell-to-WasmHost composition as the canonical Hybrid model without a new architecture decision.
- Put `@rendermode` into reusable component libraries; V2/host composition owns `InteractiveWebAssembly` placement.

### `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost`

Status:

- Reusable Storefront browser-interactive component mode library.
- Real reference components now exist; see `BlazorShop.PresentationV2/COMPONENT-MODES.md` for current mode semantics.
- H2 canonical Hybrid runtime proof lives here as `System/StorefrontHybridRuntimeProbe.razor`; its descriptor mode is semantic `Hybrid` even though the physical project is WasmHost.

Use for:

- WASM feature roots that consume `BlazorShop.Storefront.Browser` controllers.
- Browser-safe state/action contracts, `EventCallback`, lifecycle interaction, and `IJSRuntime` only for real browser behavior.

Do not:

- Reference `BlazorShop.Storefront.Presentation`, `Runtime`, `Client`, V2, V2.WASM, Starter, Starter.WASM, generated storefront projects, backend/core/API projects, Control Plane projects, or `Web.SharedV2`.
- Use `HttpContext`, `IHttpContextAccessor`, `HttpClient`, direct `/api/*`, direct `api/storefront/*`, localhost/backend URLs, or Presentation service injection.
- Use `@rendermode`; the host or composition root owns render-mode placement.
- Own theme CSS, V2 layout classes, store-specific copy, or generated output.

Allowed direct project references:

- `BlazorShop.Storefront.Components`
- `BlazorShop.Storefront.Browser`

### `BlazorShop.PresentationV2/BlazorShop.Storefront.Browser`

Important folders:

- `Cart/` - browser cart controller, state, and mutation orchestration.
- `Checkout/` - browser checkout controller, state, and command orchestration.
- `Account/` - browser profile, address, order, password, and form orchestration.

Use for:

- Same-origin local API client primitives used by interactive storefront browser flows.
- Browser-owned request DTO construction, antiforgery-aware local API calls, mutation state, and error/success mapping.
- High-level controllers consumed by V2.WASM visual components.

Do not:

- Reference Storefront Presentation server APIs, Runtime, Client, backend/core/API projects, or host visual projects.
- Add visual markup, CSS, route shells, or store-specific copy.
- Move ecommerce business truth such as pricing, sellability, checkout validity, or order creation into browser code.

### `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM`

Use for:

- Storefront V2 interactive WebAssembly components and bootstrapping required by `AddInteractiveWebAssemblyRenderMode`.
- Interactive cart, checkout, and account root components used by Storefront V2 after SSR route/security/bootstrap has selected the page surface.
- Visual composition for interactive browser roots. Components render `BlazorShop.Storefront.Browser` controller state and invoke high-level controller methods.

Do not:

- Call Control Plane.
- Hold node credentials.
- Call `StorefrontLocalApiClient`, construct Browser request DTOs, or resolve application services manually.
- Duplicate server-owned Storefront API contract behavior when `Storefront.V2` already owns the server/client integration.

### `BlazorShop.Storefront.Client`

Status:

- Active generated Storefront API client under `BlazorShop.PresentationV2/BlazorShop.Storefront.Client`.
- Generated from the canonical Storefront OpenAPI contract at `contracts/storefront/storefront.openapi.json` by `scripts/generate-storefront-client.ps1` using the pinned NSwag dotnet tool.

Use for:

- Generated request and response DTOs from Commerce Node Storefront OpenAPI.
- Generated typed HTTP clients and transport primitives.
- JSON serialization settings, route construction with `storeKey`, cancellation token propagation, and error deserialization.
- Partial hooks and injected `HttpClient` for correlation, tracing, and caller-owned retry policy wiring. `HttpClient.BaseAddress` is the single Commerce Node base URL source for generated clients.
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
- Runtime configures generated clients through the named `StorefrontGenerated` `HttpClient`; callers should configure `StorefrontRuntimeOptions.CommerceNodeBaseUrl` instead of passing per-client base URL strings.
- Runtime errors expose technical fallback state through `Status`, `Code`, `DefaultMessage`/`Message`, `TraceId`, `FieldErrors`, and `Retryable`. Storefront hosts own final localized copy, toast/inline/page placement, and retry CTA behavior.
- Server hosts that need the full storefront surface should call `AddStorefrontPlatformRuntime`; narrower hosts may call `AddStorefront{Capability}Runtime` methods such as catalog, cart, checkout, account, payment, consent, or address. The old `AddStorefrontServerGeneratedClients` and `AddStorefrontGeneratedClients` aliases have been removed.

Do not:

- Add Storefront V2 layout/design, CSS/assets, store-specific composition, backend business rules, provider secrets, or references to backend/core/API projects.
- Reference Runtime from browser/WASM projects; browser actions must go through Storefront Presentation same-origin BFF endpoints or an explicitly documented host-local extension.

### Storefront Feature Module Boundary

Current ownership map:

- `BlazorShop.Storefront.Presentation` owns storefront App/Routes, route composition, page services, SEO/discovery, media/local endpoint composition, and local browser/BFF application services for content, auth, system, cart, checkout, account, and other shared storefront entry points.
- `BlazorShop.Storefront.Client` owns generated Storefront API transport/contracts.
- `BlazorShop.Storefront.Components` owns browser-safe reusable `Contracts`, `Contracts.Diagnostics`, `Headless` state/behavior, and descriptor contracts only. Visual templates belong to Storefront V2, Starter, or generated/custom storefront projects.
- `BlazorShop.Storefront.V2` owns host configuration, session/cart-token policy, store resolution, deployment/static asset behavior, view registration, static storefront assets, and storefront-specific design. Its WASM client assembly owns the interactive V2 root components that must hydrate in the browser.
- `BlazorShop.Storefront.Runtime` owns neutral runtime primitives and server-side generated-client registration.
- `Storefront.Features.*` projects are deferred until repeated neutral feature logic proves the need.

Do not create feature packages just to move code out of Storefront V2. Extract only when it removes real duplication and can stay independent of Storefront V2 design, Presentation route shells/BFF endpoints, and backend/core/API projects.

### `BlazorShop.Storefront.Starter`

Status:

- Active neutral skeleton source for deterministic generated storefronts.
- Future `BlazorShop.Storefront.Starter` changes must preserve package-first generation, protected manifests, and store-neutral defaults.

Use for:

- Neutral visual templates for Presentation SSR, Hybrid, and WASM-host route shells. `Hybrid` here is a BlazorShop route ownership term, not `.NET InteractiveAuto` and not a requirement to use `Components.Hybrid`.
- Examples of generated Storefront API consumption through Runtime-backed Presentation contexts instead of direct generated-client use in visual source.
- Examples of `BlazorShop.Storefront.Presentation` consumption for shared App/Routes/page services/BFF/SEO/media composition; monorepo development may use a ProjectReference, while independent proof and generated storefronts use a PackageReference.
- Browser/WASM host parity with Storefront V2 through `AddStorefrontBrowserControllers()` and `MapStorefrontApplication(..., Starter.WASM assembly)`.
- Monorepo-only ProjectReferences to Presentation, Components, Browser, and Starter.WASM. Independent proof and generated storefronts rewrite shared foundation references to packages and keep only server-to-sibling-WASM ProjectReference.
- Examples of `BlazorShop.Storefront.Runtime` package consumption for server-side generated-client registration, store context, capability/error primitives, and BFF integration primitives.
- Examples should use `AddStorefrontPlatformRuntime` for simple server/BFF composition or the specific `AddStorefront{Capability}Runtime` methods for intentionally narrow generated hosts.
- Optional `BlazorShop.Storefront.Components` package consumption for reusable browser-safe UI components; Starter-local neutral components may remain local until shared reuse is needed.
- Starter owns its neutral visual templates and may consume `Storefront.Components` contracts/headless behavior without copying Storefront V2 visual components.
- Same-origin Presentation BFF flows for protected browser actions.
- Store bootstrap, capability reading, feature placement, loading/error/empty states, and generation manifest conventions.
- Deterministic generated storefront output under ignored artifact roots such as `artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof`.

Do not:

- Copy Storefront V2 source as the Starter baseline.
- Copy Storefront V2 visual components or theme/layout markup into Starter.
- Turn Storefront V2 into a neutral template.
- Reference `BlazorShop.Storefront.V2`, backend/core/API projects, Control Plane Web, or `BlazorShop.Web.SharedV2`/`Web.SharedV2`.
- Copy the manual `StorefrontApiClient` transport from Storefront V2.
- Move pricing, sellability, cart validation, checkout, order placement, payment, or authorization rules into Starter.
- Reference ServiceDefaults as a required generated storefront dependency.

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
- Hosting presentation-specific CSS, assets, generated pages, visual analysis artifacts, and AI-tuned components for exactly one generated/custom storefront.
- Owning generated markup/CSS and replacing product card/grid/gallery/purchase/cart/checkout/account visual templates while reusing contracts/headless behavior.
- Routing protected browser actions through same-origin Presentation BFF endpoints before Storefront Runtime or Commerce Node Storefront APIs.

Do not:

- Add generated proof output to `BlazorShop.sln` by default.
- Treat generated proof output as a platform contract owner.
- Backport store-specific CSS, assets, generated pages, or analysis artifacts into Starter.
- Use generated proof output as evidence that generated storefronts may reference Storefront V2, `BlazorShop.Web.SharedV2`/`Web.SharedV2`, or backend/core/API projects.
- Use Storefront V2 visual markup as the generated/custom storefront presentation source.
- Guess Storefront API response shapes instead of using generated package contracts.

### `BlazorShop.Storefront.Starter.WASM`

Status:

- Active neutral browser skeleton source for deterministic generated storefront WASM projects.

Use for:

- Browser-safe account, cart, and checkout host components that consume `BlazorShop.Storefront.Browser` controllers and `BlazorShop.Storefront.Components` contracts/headless state.
- `Program.cs` calls `WebAssemblyHostBuilder.CreateDefault(args)` and `AddStorefrontBrowserRuntime(builder.HostEnvironment)`.
- Store-neutral interactive component templates with no route declarations; route ownership remains in Storefront Presentation.

Allowed:

- Monorepo ProjectReferences to `BlazorShop.Storefront.Browser` and `BlazorShop.Storefront.Components`.
- Store-neutral visual host components under `Components/Account`, `Components/Cart`, and `Components/Checkout`.

Do not:

- Reference `BlazorShop.Storefront.Runtime`, `BlazorShop.Storefront.Client`, Storefront V2/V2.WASM, ServiceDefaults, backend/core/API projects, or `Web.SharedV2`.
- Declare routes or call Commerce Node directly from browser code.

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

- Transitional Control Plane/shared browser helper ownership while Control Plane Web remains an active consumer.
- Shared UI/browser utilities across V2 Web projects.
- Browser storage, cookie, auth-session sync, and toast/helper behavior that is useful to more than one active V2 frontend.
- Small framework-neutral helper services where sharing reduces duplicated behavior without coupling product UI.

Do not:

- Put project-specific business logic here.
- Put Storefront-specific files, namespaces, route helpers, cookie names, or business models here.
- Add Storefront business model folders to `Models/`; the existing inventory is frozen in architecture tests until a later Control Plane/shared-helper migration removes or relocates it.
- Reuse Control Plane auth/token/JWT helpers from Storefront code. Storefront auth/session behavior must stay in Storefront-owned host/runtime contracts.
- Put Commerce Node credentials here.
- Move Storefront header/footer/cart/toast DOM behavior here unless Control Plane has the same real need.
- Move Control Plane nav/sidebar/topbar/page chrome here unless Storefront has the same real need.
- Use `Web.SharedV2` as a forced visual design system; Storefront and Control Plane intentionally keep different UI density and product identity.

If Storefront no longer consumes `Web.SharedV2` and Control Plane Web becomes the only active consumer, move the remaining helpers into `BlazorShop.ControlPlane.Web` in a later phase, or extract a smaller generic helper package only after at least two active consumers need the same behavior.

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
