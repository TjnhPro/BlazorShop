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
- Gateway calls to Commerce Node.
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
- `api/control-plane/stores/{storePublicId}/catalog`

## Commerce Node Boundary

Commerce Node is the ecommerce runtime boundary. It owns node-local commerce data, node-local admin/control endpoints, internal Storefront APIs, and local deployment tasks.

Commerce Node API also owns startup EF Core migration for `CommerceNodeDbContext` only. It must not migrate `ControlPlaneDbContext` or legacy `AppDbContext`.

Main route groups:

```text
api/commerce/*
api/internal/*
```

### `api/commerce/*`

Caller:

- `BlazorShop.ControlPlane.API`

Security:

- Node key.
- Node secret.
- Allowed IP behavior where configured.
- Store scope where applicable.

Responsibilities:

- Commerce admin/control APIs.
- Store management on the node.
- Task orchestration.
- Deployment task lifecycle.
- Catalog admin operations.
- Inventory, order admin, metrics, SEO, audit, media.
- Product media import is asynchronous through `commerce_task` and the existing `CommerceTaskWorker` in MVP.

### `api/internal/*`

Caller:

- `BlazorShop.Storefront.V2`

Security:

- Store key scope.
- Storefront/customer session behavior where needed.
- Intended as a private/internal path, not public Control Plane UI traffic.

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

## Storefront V2 Boundary

`BlazorShop.Storefront.V2` is a server-side storefront. It renders public/store pages and calls Commerce Node internal APIs.

Responsibilities:

- Storefront pages and layout.
- Storefront login/register/logout forms.
- Sitemap and robots generation.
- SEO composition and structured data.
- Redirects to client account/checkout routes when needed.
- Store key propagation to Commerce Node.

It must not call Control Plane APIs and must not use Control Plane credentials.

## Public Product Media Boundary

Product media URLs are public storefront URLs, but they are still store-scoped:

```text
/media/products/{mediaPublicId}?w=600&h=600&fit=contain&format=webp&v=1
```

Resolution rules:

- Production/storefront traffic should resolve the store from `Host`, `X-Forwarded-Host`, or the Nginx/store domain path.
- Local direct API QA may send `X-Store-Key` to disambiguate the store.
- A plain `localhost:5180/media/products/{mediaId}` request can return `404` when the Commerce Node database has multiple active stores, because `localhost` is not enough store identity.
- Store A media must not resolve from Store B host or store key.
- Public media rendering validates the store/media row first, then proxies optimized output through imgproxy.

## Legacy Boundary

`BlazorShop.Presentation` remains in the solution for legacy support and migration comparison.

Legacy route groups include:

- `api/admin/*`
- `api/public/*`
- `api/[controller]`

Do not use legacy APIs as a dependency for V2 features. If behavior must be migrated, copy the behavior intentionally into the active V2 boundary and adapt it to `CommerceNodeDbContext` or `ControlPlaneDbContext` as appropriate.

Legacy `AppDbContext` migrations are not part of the V2 startup migration flow.

## API Response Pattern

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
