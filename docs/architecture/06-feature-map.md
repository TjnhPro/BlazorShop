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
- Commerce Admin gateway capabilities from Control Plane API to Commerce Node API for products, categories, media, pages, navigation, orders, currencies, payment methods, shipping, security/privacy, messages, and runtime store configuration.
- Control Plane Web commerce admin UI. Web calls typed Control Plane API clients only; Commerce Node credentials and store-key forwarding stay server-side in Control Plane API.

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
- Store lifecycle and store-scoped configuration consumption.
- Task orchestration.
- Storefront deployment image configuration.
- Store deployment records.
- Docker/Nginx deployment support.
- Admin settings.
- Store feature states.
- Security and privacy settings.
- Shipping settings.
- Admin audit.
- Product categories.
- Products.
- Product variants with attribute combinations.
- Variation templates.
- Product media import, primary image selection, public media URL generation, and imgproxy-backed rendering.
- Category media assignment.
- Media asset library.
- Inventory.
- Media upload.
- Orders and order admin actions.
- Payment methods.
- Currency configuration and exchange-rate update tasks.
- Transactional message templates and queued messages.
- Metrics.
- Product/category SEO.
- SEO settings and redirects.
- SEO slug lifecycle.
- Store navigation menus.
- Store-scoped dynamic storefront pages, including admin CRUD, HTML validation, archive, and sitemap inclusion.

Decision rule:

- If the feature is node-local ecommerce admin behavior, it belongs to Commerce Node admin/control.
- Control Plane Web must reach it only through Control Plane API.

## Commerce Node Storefront API

Project:

- `BlazorShop.CommerceNode.API`

Route group:

```text
api/storefront/stores/{storeKey}/*
```

Capabilities:

- Store context and maintenance state.
- Storefront configuration, contact data, default currency, feature state, and display context.
- Security/privacy consent state.
- Storefront auth create/login/refresh/logout/change password/update profile.
- Customer profile and address book.
- Public catalog categories/products by id or slug.
- Product variant selection and availability validation.
- Public storefront pages by slug.
- Public navigation menus.
- Sitemap catalog feed.
- Cart lifecycle, cart merge, checkout session, address step, shipping method selection, payment method selection, review, and order placement.
- Current user orders, order details, receipts, and guest order lookup.
- Payment methods, payment attempts, and provider operations/callbacks/webhooks.
- Currency preference and active currency lookup.
- Shipping address field configuration.
- Contact metadata.
- Newsletter subscribe.
- SEO settings and redirect resolution.
- Product recommendations.
- Public product media rendering through store-scoped `/media/products/{mediaPublicId}` URLs.

Decision rule:

- If the caller is Storefront V2 and the feature is public/customer behavior, it belongs to `api/storefront/stores/{storeKey}/*`.
- `api/internal/*` was the migration compatibility path and has been removed from active V2 runtime guidance.

## Storefront V2 UI

Project:

- `BlazorShop.Storefront.V2`
- `BlazorShop.Storefront.Components`
- `BlazorShop.Storefront.WASM`

Capabilities:

- Public storefront pages.
- Dynamic informational pages at `/pages/{slug}` loaded from Commerce Node.
- Product/category route rendering.
- Search, new releases, today's deals, product variant selection, and local cart UI.
- Login/register/logout forms in the storefront layout.
- Storefront auth cookie bridge.
- Customer account profile, addresses, order list/detail, and change password pages.
- Checkout flow with address, shipping, payment, review, and payment result pages.
- Maintenance page and current-store guard.
- Currency preference UI.
- Consent/banner behavior.
- Navigation menus loaded from Commerce Node.
- Robots and sitemap documents.
- SEO composition and structured data.
- Storefront API client with store key in the scoped route path.
- Public media proxy routes for `/media/products/*` and `/media/assets/*`.

Decision rule:

- If the feature is visual/storefront page behavior, it belongs to Storefront V2.
- If the feature needs data, call Commerce Node Storefront APIs under `api/storefront/stores/{storeKey}/*`.

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

## V2 Contract And QA Surface

Projects/files:

- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Swagger`
- `BlazorShop.Tests/PresentationV2/CommerceNode`
- `BlazorShop.Tests/PresentationV2/CommerceNode/Snapshots`
- `docs/refactor-control-Commerce-storefront/QA-*.todo.md`

Capabilities:

- Separate Commerce Admin and Storefront Swagger documents.
- Stable Storefront operation IDs, summaries, response schemas, error schemas, required request bodies, validation metadata, and security requirements.
- Commerce Admin metadata for store, currency, navigation, media, shipping, security/privacy, message, slug, and related admin endpoints.
- OpenAPI reader validation and generator-safety contract tests.
- Swagger snapshot coverage for Storefront API breaking-change detection.

Decision rule:

- API contract changes are product changes. Update Swagger metadata, contract tests, snapshots, and QA docs in the same phase.

## Legacy

Status: removed from the active branch. Use git history or the `legacy-presentation-final` tag for comparison.

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
