# Feature Map

Use this map to decide where a feature belongs.

## Control Plane

Projects:

- `BlazorShop.ControlPlane.API`
- `BlazorShop.ControlPlane.Web`
- `BlazorShop.Infrastructure/Data/ControlPlane`
- `BlazorShop.Application/ControlPlane`

Capabilities:

- Dashboard summary.
- Control Plane login/logout/refresh/me.
- Control Plane users.
- Roles and permissions.
- Node list/create/view/update/disable.
- Node credentials list/create/rotate/revoke.
- Store registry list/create/view/update/archive.
- Store domain registry.
- Store deployment task requests through Commerce Node.
- Node health and manual probe.
- Control actions and attempts.
- Audit logs.
- Catalog gateway from Control Plane API to Commerce Node API.
- Commerce storefront page gateway and admin UI. Control Plane Web must call Control Plane API only.

Decision rule:

- If the feature is about platform administration, node registry, store assignment, node credentials, platform audit, or Web admin UX, it belongs to Control Plane.
- If the feature edits ecommerce data, Control Plane API should usually call Commerce Node API rather than writing ecommerce tables directly.

## Commerce Node Admin And Control

Projects:

- `BlazorShop.CommerceNode.API`
- `BlazorShop.Infrastructure/Data/CommerceNode`
- `BlazorShop.Application/CommerceNode`
- Relevant shared application services under `BlazorShop.Application/Services`

Route group:

```text
api/commerce/*
```

Capabilities:

- Commerce stores.
- Commerce store domains.
- Task orchestration.
- Storefront deployment image configuration.
- Store deployment records.
- Docker/Nginx deployment support.
- Admin settings.
- Admin audit.
- Product categories.
- Products.
- Product variants with attribute combinations.
- Product media import, primary image selection, public media URL generation, and imgproxy-backed rendering.
- Inventory.
- Media upload.
- Orders and order admin actions.
- Metrics.
- Product/category SEO.
- SEO settings and redirects.
- Store-scoped dynamic storefront pages, including admin CRUD, HTML validation, archive, and sitemap inclusion.

Decision rule:

- If the feature is node-local ecommerce admin behavior, it belongs to Commerce Node admin/control.
- Control Plane Web must reach it only through Control Plane API.

## Commerce Node Internal Storefront

Project:

- `BlazorShop.CommerceNode.API`

Route group:

```text
api/internal/*
```

Capabilities:

- Store context and maintenance state.
- Storefront auth create/login/refresh/logout/change password/update profile.
- Public catalog categories/products by id or slug.
- Public storefront pages by slug.
- Sitemap catalog feed.
- Cart checkout/save checkout.
- Order confirmation and current user orders.
- Payment methods and PayPal capture.
- Newsletter subscribe.
- SEO settings and redirect resolution.
- Product recommendations.
- Public product media rendering through store-scoped `/media/products/{mediaPublicId}` URLs.

Decision rule:

- If the caller is Storefront V2 and the feature is public/customer behavior, it belongs to `api/internal/*`.
- Keep this path private/internal to the node/storefront runtime.

## Storefront V2 UI

Project:

- `BlazorShop.Storefront.V2`

Capabilities:

- Public storefront pages.
- Dynamic informational pages at `/pages/{slug}` loaded from Commerce Node.
- Product/category route rendering.
- Login/register/logout forms in the storefront layout.
- Storefront auth cookie bridge.
- Checkout redirect flow.
- Robots and sitemap documents.
- SEO composition and structured data.
- Storefront API client with store key header.

Decision rule:

- If the feature is visual/storefront page behavior, it belongs to Storefront V2.
- If the feature needs data, call Commerce Node internal API.

## Shared V2 Web

Project:

- `BlazorShop.Web.SharedV2`

Capabilities:

- Browser/session storage.
- Cookie storage.
- Token service.
- API call helpers.
- Auth session sync.
- Toast service.

Decision rule:

- Put only generic V2 Web helper behavior here.
- Do not put Control Plane business rules or Storefront business rules here.

## Legacy

Projects:

- `BlazorShop.Presentation/BlazorShop.API`
- `BlazorShop.Presentation/BlazorShop.Web`
- `BlazorShop.Presentation/BlazorShop.Storefront`
- `BlazorShop.Presentation/BlazorShop.Web.Shared`

Capabilities:

- Original admin APIs and pages.
- Original storefront pages.
- Original public catalog/auth/cart/payment behavior.

Decision rule:

- Use for migration reference only.
- Do not extend for active V2 work unless the user explicitly asks.

## Smartstore Research

Smartstore should be used to learn ecommerce depth, not to introduce heavy scope automatically.

Good Smartstore research outputs:

- Property comparison tables.
- Workflow summaries.
- Feature candidate CSVs.
- MVP vs later-phase recommendations.
- Notes about what not to build yet.

Bad Smartstore usage:

- Copying source code.
- Adding Smartstore project references.
- Importing module architecture wholesale.
- Expanding scope beyond the selected MVP.
