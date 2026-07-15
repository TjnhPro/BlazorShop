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

- `Data/AppDbContext.cs` - legacy context.
- `Data/ControlPlane/` - Control Plane EF context, migrations, services, seeders.
- `Data/CommerceNode/` - Commerce Node EF context, migrations, repositories, services, seeders.
- `Repositories/` - legacy/general repository implementations.
- `Services/` - infrastructure service implementations.
- `Migrations/` - legacy `AppDbContext` migrations.

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
- `Authorization/` - Control Plane policies and auth setup.
- `Middleware/` - correlation and API behavior middleware.
- `Responses/` - Control Plane API envelope helpers.
- `ControlPlaneDatabaseBootstrapper.cs` - startup migration and Development seeding for `ControlPlaneDbContext`.

Use for:

- Platform-facing APIs.
- Control Plane auth and permission enforcement.
- Gateway behavior to Commerce Node.

Do not:

- Put Commerce Node credentials in Web clients.
- Store commerce catalog/order/customer data directly here.
- Migrate `CommerceNodeDbContext` or legacy `AppDbContext` from this runtime.

### `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web`

Important folders:

- `Pages/` - Blazor WASM pages.
- `Layout/` - Control Plane layouts.
- `Services/` - typed clients calling Control Plane API.
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
- `CommerceNodeDatabaseBootstrapper.cs` - startup migration and Development seeding for `CommerceNodeDbContext`.

Use for:

- Node-local ecommerce API.
- Scoped Storefront API.
- Node task orchestration.
- Store deployment support.

Do not:

- Add Control Plane UI logic.
- Persist platform-level users/permissions/credentials here.
- Migrate `ControlPlaneDbContext` or legacy `AppDbContext` from this runtime.

### `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`

Important folders:

- `Components/` - Razor components.
- `Pages/` - server-side storefront pages.
- `Services/` - Storefront API clients, SEO, sitemap, robots, auth form handlers.
- `Options/` - Storefront API/public URL options.
- `Configuration/` - options validators.
- `wwwroot/` - static storefront assets.

Use for:

- Public/store-scoped storefront UI.
- Storefront login/register/logout forms.
- SEO and public discovery documents.
- Store key propagation to Commerce Node Storefront API.

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

`BlazorShop.Presentation` contains:

- `BlazorShop.API` - legacy API.
- `BlazorShop.Web` - legacy admin/account/customer UI.
- `BlazorShop.Storefront` - legacy storefront.
- `BlazorShop.Web.Shared` - legacy shared Web helpers.

Use legacy code for:

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
