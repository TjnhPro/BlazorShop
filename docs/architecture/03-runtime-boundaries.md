# Runtime Boundaries

## Control Plane Boundary

Control Plane has two active projects:

- `BlazorShop.ControlPlane.Web`
- `BlazorShop.ControlPlane.API`

The Web project is UI-only. It stores browser-side session state and renders pages, but it must not know Commerce Node credentials or call Commerce Node directly.

Required call path:

```text
BlazorShop.ControlPlane.Web
  -> BlazorShop.ControlPlane.API
      -> BlazorShop.CommerceNode.API
```

Forbidden call path:

```text
BlazorShop.ControlPlane.Web
  -> BlazorShop.CommerceNode.API
```

Control Plane API owns:

- JWT authentication and refresh behavior.
- Platform permissions.
- API response envelope.
- Rate limiting and correlation.
- Node/store registry lookup.
- Commerce Node credential usage.
- Audit for platform actions.
- Commerce Admin gateway capability calls to Commerce Node.
- Startup EF Core migration for `ControlPlaneDbContext` only.

Main route group:

```text
api/control-plane/*
```

Examples:

- `api/control-plane/auth`
- `api/control-plane/users`
- `api/control-plane/nodes`
- `api/control-plane/stores`
- `api/control-plane/health`
- `api/control-plane/actions`
- `api/controlplane/commerce/stores/{storePublicId}/products`
- `api/controlplane/commerce/stores/{storePublicId}/orders`
- `api/controlplane/commerce/stores/{storePublicId}/pages`

## Commerce Node Boundary

Commerce Node is the ecommerce runtime boundary. It owns node-local commerce data, node-local admin/control endpoints, scoped Storefront APIs, and local deployment tasks.

Commerce Node API also owns startup EF Core migration for `CommerceNodeDbContext` only. It must not migrate `ControlPlaneDbContext`.

Main route groups:

```text
api/commerce/*
api/storefront/stores/{storeKey}/*
```

### `api/commerce/*`

Caller:

- `BlazorShop.ControlPlane.API`

Security:

- Node key.
- Node secret.
- Allowed IP behavior where configured.
- Store scope through required query `storeKey` for store-scoped Commerce Admin endpoints.
- Store-scoped Commerce Admin requests resolve `storeKey` in Commerce Node API middleware into a scoped `StoreExecutionContext` before Application/Infrastructure services run.

Responsibilities:

- Commerce admin/control APIs.
- Store management on the node.
- Task orchestration.
- Deployment task lifecycle.
- Catalog, content, media, inventory, order, currency, shipping, payment, security/privacy, and message admin operations.
- Inventory, order admin, metrics, SEO, audit, media.
- Product media import is asynchronous through `commerce_task` and the existing `CommerceTaskWorker` in MVP.

### `api/storefront/stores/{storeKey}/*`

Caller:

- Storefront Presentation/Runtime inside `BlazorShop.Storefront.V2`, `BlazorShop.Storefront.Starter`, and generated storefront hosts.

Security:

- Store scope comes from the route value `{storeKey}`.
- Commerce Node API resolves `{storeKey}` once per request into a scoped `StoreExecutionContext`; Infrastructure services consume that resolved context instead of reading HTTP route values, query strings, headers, or hosts.
- Storefront/customer session behavior where needed.
- No node key or node secret.
- No `X-Store-Key` header.

Responsibilities:

- Public storefront catalog.
- Store context and maintenance state.
- Storefront auth.
- Cart and checkout.
- Customer orders.
- Payment method lookup and payment callbacks.
- Newsletter subscription.
- SEO settings and redirect resolution.
- Recommendations.

### Removed `api/internal/*`

Status:

- Removed from the active V2 Commerce Node runtime after the Storefront V2 scoped route migration and QA pass.

Rules:

- Do not add new `api/internal/*` controllers, Swagger documents, or Storefront clients.
- Historical planning files may mention `api/internal/*` as migration context only.

## Storefront Presentation Boundary

`BlazorShop.Storefront.Presentation` is the shared storefront application engine. It owns App/Routes, route shells, page services, same-origin BFF/local endpoints, media/local endpoints, SEO/discovery composition, and view-slot contracts used by Storefront V2, Starter, and generated storefronts.

Its public namespace surface is explicitly Presentation-owned: `BlazorShop.Storefront.Presentation.Services`, `BlazorShop.Storefront.Presentation.Contracts`, `BlazorShop.Storefront.Presentation.Models`, `BlazorShop.Storefront.Presentation.Options`, and `BlazorShop.Storefront.Presentation.Configuration`. New shared storefront application contracts must not use the older generic `BlazorShop.Storefront.Services`, `BlazorShop.Storefront.Models`, `BlazorShop.Storefront.Options`, or `BlazorShop.Storefront.Configuration` namespaces.

Responsibilities:

- Storefront application bootstrap extensions, middleware order, and route/endpoint mapping used by visual hosts.
- Runtime registration for the shared storefront application graph.
- Current-store middleware, public redirect middleware, rate limiting, antiforgery policy, and BFF security behavior.
- Storefront App/Routes and SSR, hybrid, and WASM-hosted route shells.
- Page services and context models for home, catalog, content, auth, system, cart, checkout, account, and payment surfaces.
- Header, footer, account menu, auth form, checkout form, currency/logout, cart, product purchase, price, and stock presentation contexts.
- Sitemap, robots, SEO metadata, canonical URL, structured data, redirects, and media/local endpoint composition.
- Same-origin browser/BFF endpoint groups for cart, checkout, account, consent, preferences, media, and other browser-safe flows.
- View-slot contracts that let each host provide visual templates without Presentation referencing V2, Starter, or generated projects.

Do not:

- Put Storefront V2-specific design, CSS, copy, or generated visual output here.
- Reference `BlazorShop.Storefront.V2`, `BlazorShop.Storefront.Starter`, generated storefront projects, Control Plane, Commerce Node API, Application, Domain, Infrastructure, or `Web.SharedV2`.
- Move ecommerce truth such as pricing, sellability, checkout validity, order creation, or inventory decisions out of Commerce Node Storefront APIs.

Hosts call `AddStorefrontApplication()`, `UseStorefrontApplication()`, and `MapStorefrontApplication()` instead of registering runtime services or mapping individual middleware, route, BFF, SEO, and media endpoint groups.

## Storefront V2 Boundary

`BlazorShop.Storefront.V2` is a thin server-side storefront host and visual implementation that consumes Storefront Presentation. Storefront V2 does not reference Runtime or Client directly; Commerce Node Storefront API calls flow through the shared Presentation/Runtime/Client path.

Responsibilities:

- Storefront V2 host configuration, static assets, layout views, visual templates, copy, and interactive V2 root components.
- Storefront V2 view registration for Presentation route shells.
- Host assembly selection for Presentation route discovery and static asset behavior.
- Store key/base URL configuration consumed by Presentation/Runtime.

Storefront V2 must not own application services, middleware, direct client/runtime injection, application data loading, manual mutation contracts, business decisions, route/SEO/status behavior, or BFF endpoint contracts. V2 visual components render Presentation contexts and may submit to Presentation-owned local routes/actions supplied in those contexts.

It must not call Control Plane APIs and must not use Control Plane credentials.

### Browser/BFF Boundary

Browser and WASM code calls same-origin storefront endpoints under `/api/*`. It must not call Commerce Node protected APIs directly, must not know the Commerce Node base URL, must not hold node credentials, and must not store Commerce access tokens in browser local storage.

Storefront Presentation BFF/local endpoints are responsible for:

- resolving the current store;
- resolving the HttpOnly customer session;
- attaching Commerce access tokens server-side when a customer flow requires them;
- attaching or resolving the cart token;
- validating antiforgery on browser mutations;
- normalizing Commerce API failures into local frontend-safe errors;
- returning only local/browser-safe response shapes.

Storefront Presentation BFF/local endpoints are not responsible for:

- price calculation;
- sellability calculation;
- cart validity decisions;
- checkout business rules;
- order creation outside Commerce checkout/place-order APIs.

Local `/api/*` contracts are storefront browser contracts owned by Storefront Presentation unless a host explicitly owns a narrower local extension. They may differ from Commerce Node Storefront API DTOs when the browser needs a smaller or presentation-specific shape, but they must not duplicate ecommerce business truth.

## Generated Storefront Boundary

Generated storefronts from StorefrontBuilder follow the same public Storefront API and browser/BFF model as Storefront V2, but they are isolated from Storefront V2 source code.

Required generated storefront shape:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.{Name}
  -> BlazorShop.Storefront.Presentation package
      -> BlazorShop.Storefront.Runtime package
          -> BlazorShop.Storefront.Client package
              -> BlazorShop.CommerceNode.API api/storefront/stores/{storeKey}/*
  -> BlazorShop.Storefront.Components package
```

Rules:

- Generated storefronts must not reference `BlazorShop.Storefront.V2`.
- Generated storefronts that need full storefront routes/BFF/SEO/media composition must consume `BlazorShop.Storefront.Presentation` through a package boundary and provide project-local registered views/assets/copy.
- Generated storefronts consume `BlazorShop.Storefront.Presentation` and `BlazorShop.Storefront.Components` directly. Presentation exposes the required Runtime dependency, and Runtime owns the generated `BlazorShop.Storefront.Client` transport dependency. Generated hosts keep Client/Runtime version metadata only for package proof compatibility and must not direct-reference Runtime or Client.
- Generated storefronts must not reference `BlazorShop.Application`, `BlazorShop.Domain`, `BlazorShop.Infrastructure`, `BlazorShop.CommerceNode.API`, `BlazorShop.ControlPlane.API`, or `BlazorShop.Web.SharedV2`/`Web.SharedV2`.
- Browser code in generated storefronts must not call Commerce Node admin/control, Control Plane, or removed `api/internal/*` routes directly.
- `BlazorShop.Storefront.Starter` is a neutral template input. Store-specific generated CSS, assets, pages, and analysis artifacts belong in the generated storefront project only.
- StorefrontBuilder tooling is development-time only and is documented in [StorefrontBuilder Architecture](11-storefront-builder.md).

### Storefront Store Resolution

Storefront Presentation resolves storefront store scope from host configuration before catalog, settings, SEO, media, cart, checkout, or customer context is read. Storefront V2, Starter, and generated hosts provide configuration only; they do not own the current-store middleware. The accepted Storefront V2 configuration keys are:

- `Api:StoreKey`
- `StoreKey`
- `STORE_KEY`

Presentation/Runtime resolves the current store through:

```text
GET api/storefront/stores/{storeKey}/store/current
```

Rules:

- A missing or invalid store must return a clear failure (`404` for missing store, `503` for unavailable/maintenance/config failure).
- Storefront V2 must not fall back to another store when current-store resolution fails.
- Static assets and health endpoints may skip the current-store guard.
- Production Storefront V2 requires a configured store key when current-store resolution is enabled.
- Storefront public absolute URLs prefer `PublicUrl:BaseUrl`, then SEO configured base URL, then request fallback after trusted forwarded headers have run.

## Public Product Media Boundary

Product media URLs are public storefront URLs, but they are still store-scoped:

```text
/media/products/{mediaPublicId}?w=600&h=600&fit=contain&format=webp&v=1
```

Resolution rules:

- Commerce Node Nginx keeps an explicit default/catch-all server returning `403` for unmatched hosts.
- Production/storefront traffic should resolve the store through Nginx/domain/rewrite behavior.
- Commerce Node API public media middleware may resolve clean media URLs from trusted host/forwarded-host data into `StoreExecutionContext`; this host compatibility path is limited to `/media/products/*` and `/media/assets/*`.
- Local admin/debug media QA should use Commerce Admin media debug endpoints with `storeKey` query.
- A plain `localhost:5180/media/products/{mediaId}` request can return `404` when the Commerce Node database has multiple active stores, because `localhost` is not enough store identity.
- Store A media must not resolve from Store B host or store key.
- Public media rendering validates the store/media row first, then proxies optimized output through imgproxy.

## Legacy Boundary

`BlazorShop.Presentation` has been removed from the active branch. Use git history or the `legacy-presentation-final` tag for legacy behavior comparison.

Legacy route groups include:

- `api/admin/*`
- `api/public/*`
- `api/[controller]`

Do not use legacy APIs as a dependency for V2 features. If behavior must be migrated, copy the behavior intentionally into the active V2 boundary and adapt it to `CommerceNodeDbContext` or `ControlPlaneDbContext` as appropriate.

The legacy `AppDbContext` migration path has been removed from active Infrastructure. V2 startup migration flow is limited to `ControlPlaneDbContext` and `CommerceNodeDbContext`.

## API Response Pattern

All active V2 HTTP APIs must follow [API Contract Standards](09-api-contract-standards.md).

OpenAPI is a contract surface, not only debug UI. New or changed operations must publish stable operation IDs, summaries, explicit request/response DTO schemas, standard error schemas, required request bodies, validation metadata, and security requirements. Contract tests must protect those fields before the change is considered complete.

Control Plane API uses the standardized API envelope:

```json
{
  "success": true,
  "message": "string",
  "data": {}
}
```

The Web UI should rely on `success`, `message`, and `data` for user-facing results while still allowing the client layer to handle HTTP status for auth and infrastructure cases.

Commerce Node APIs should follow existing response helpers/patterns in the project being edited. Prefer consistency with nearby controllers before adding another response abstraction.

## Control Plane Paging Pattern

Control Plane admin list/search/query endpoints use `pageNumber/pageSize` rather than `skip/take` or hidden caps.

List responses should include:

```json
{
  "items": [],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 25,
  "totalPages": 0
}
```

Rules:

- Every Control Plane API method named `List`, `Query`, or `Search` must be paged unless it is a static lookup/catalog.
- Static lookup/catalog endpoints should be named as catalog/lookup APIs, not `List*`.
- API services may compute skip/take internally, but Web/API contracts must not expose skip/take for admin list pages.
