# BlazorShop Commerce Node Storefront Migration Todo

## Goal

Move Storefront-facing business from legacy `BlazorShop.Presentation/BlazorShop.API` into `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API` while preserving the existing Layered Architecture and avoiding a rewrite.

This plan is based on `BlazorShop.CommerceNode.AdminMigration.todo.md` and the current legacy controller inventory.

## Autoplan Review Summary

### Decisions

- Use a separate PostgreSQL container for Commerce Node instead of sharing the Control Plane PostgreSQL container.
- Use one Commerce Node database, one Commerce Node connection string, and one Commerce Node EF context:
  - `CommerceNodeDbContext` owns commerce data plus Storefront Identity/refresh-token data.
- Do not use `AppDbContext` in Commerce Node.
- Do not use Control Plane auth for Storefront customer auth.
- Keep route boundaries explicit:
  - `api/commerce/*` for Control Plane -> Commerce Node admin/control operations.
  - `api/internal/*` for Storefront -> Commerce Node private/internal operations.
- Rename controller classes for readability without changing already-agreed route prefixes:
  - Commerce Admin controllers should start with `Commerce`.
  - Storefront/Internal controllers should start with `Storefront`.

### Main Risk

Read-only Storefront endpoints can be migrated safely first. Auth/cart/order endpoints are higher risk because the current legacy logic depends on `AppUser`, Identity, refresh tokens, current user claims, email, payment services, and order history.

### Scope Rule

Do not migrate by copying whole legacy controllers blindly. Split by business responsibility:

- Storefront catalog/SEO/recommendations/payments/newsletter -> `api/internal/*`.
- Storefront auth/customer -> `api/internal/auth/*`.
- Storefront cart/order actions -> `api/internal/cart/*` and `api/internal/orders/*`.
- Legacy Admin-only actions inside `CartController` should not become Commerce Admin routes unless a later usage review proves they are still required.

## Naming Convention Plan

### Controller Class Names

Existing Commerce Admin controllers in `BlazorShop.CommerceNode.API` should be renamed by class/file only:

| Current Controller | Target Controller Class | Route Must Stay |
|---|---|---|
| `ProductsController` | `CommerceProductsController` | `api/commerce/admin/products` |
| `CategoriesController` | `CommerceCategoriesController` | `api/commerce/admin/categories` |
| `ProductVariantsController` | `CommerceProductVariantsController` | `api/commerce/admin/products/{productId}/variants` |
| `InventoryController` | `CommerceInventoryController` | `api/commerce/admin/inventory` |
| `ProductSeoController` | `CommerceProductSeoController` | `api/commerce/admin/products/{id}/seo` |
| `CategorySeoController` | `CommerceCategorySeoController` | `api/commerce/admin/categories/{id}/seo` |
| `SeoSettingsController` | `CommerceSeoSettingsController` | `api/commerce/admin/seo/settings` |
| `SeoRedirectsController` | `CommerceSeoRedirectsController` | `api/commerce/admin/seo/redirects` |
| `OrdersController` | `CommerceOrdersController` | `api/commerce/admin/orders` |
| `AdminSettingsController` | `CommerceAdminSettingsController` | `api/commerce/admin/settings` |
| `MediaController` | `CommerceMediaController` | `api/commerce/admin/media` |
| `AdminAuditController` | `CommerceAuditController` | `api/commerce/admin/audit` |
| `MetricsController` | `CommerceMetricsController` | `api/commerce/admin/metrics` |

New Storefront controllers should use `Storefront` prefix from the start:

| New Controller | Route |
|---|---|
| `StorefrontAuthController` | `api/internal/auth` |
| `StorefrontCatalogController` | `api/internal/catalog` |
| `StorefrontSeoController` | `api/internal/seo` |
| `StorefrontRecommendationsController` | `api/internal/recommendations` |
| `StorefrontPaymentsController` | `api/internal/payments` |
| `StorefrontNewsletterController` | `api/internal/newsletter` |
| `StorefrontCartController` | `api/internal/cart` |
| `StorefrontOrdersController` | `api/internal/orders` |

## Database And Compose Plan

### Containers

Create a new compose file dedicated to Commerce Node local development:

- File: `compose.commercenode.yml`
- Service: `commercenode-postgres`
- Container: `blazorshop-commercenode-postgres`
- Image: `postgres:17-alpine`
- Host port: `5434`
- Container port: `5432`
- Database: `blazorshop_commerce_node`
- User: `blazorshop_commerce_node`
- Password: `blazorshop_commerce_node_dev`
- Volume: `commercenode_postgres_data`

Do not reuse `compose.controlplane.yml` because it already owns host port `5433` and database/user `blazorshop_controlplane`.

### Connection Strings

Commerce Node local development should use one connection string:

```text
ConnectionStrings__CommerceNodeConnection=Host=localhost;Port=5434;Database=blazorshop_commerce_node;Username=blazorshop_commerce_node;Password=blazorshop_commerce_node_dev
```

Do not add `CommerceNodeAuthConnection` unless there is a later, explicit decision to split Storefront auth into a separate physical database. That split is not recommended for this roadmap.

### Context

Use one EF context for the Commerce Node bounded context:

| Context | Owns | Notes |
|---|---|---|
| `CommerceNodeDbContext` | Products, categories, variants, SEO, orders, order lines, payment methods, newsletter, admin audit/settings, Storefront Identity tables, `RefreshTokens`, future store/customer membership tables | This should evolve from `DbContext` to `IdentityDbContext<AppUser>` when Storefront auth is migrated. |

Reason for one context:

- Commerce Node is one bounded context for Storefront commerce execution.
- Multi-store will require `StoreId` filtering across catalog, orders, customers, settings, and permissions.
- Customer/store membership must be queryable in the same unit of work as orders and catalog rules.
- One context avoids the earlier Control Plane mistake in a different way: do not reuse `AppDbContext`; instead make the new bounded-context context own its own Identity schema.
- One migration stream is easier to run, reason about, reset, and QA.

### Auth Tables

Storefront auth phase may add these tables to the Commerce Node database:

- `AspNetUsers`
- `AspNetRoles`
- `AspNetUserRoles`
- `AspNetUserClaims`
- `AspNetRoleClaims`
- `AspNetUserLogins`
- `AspNetUserTokens`
- `RefreshTokens`

Seed only the roles required by Storefront:

- `User`
- `Admin` only if preserving legacy first-user behavior requires it; otherwise prefer no Storefront Admin dependency.

## API Response Pattern

All new Storefront controllers must return the Commerce Node response envelope:

```json
{
  "success": true,
  "message": "string",
  "data": {}
}
```

Transport status should remain meaningful, but Storefront UI/client should read `success`, `message`, and `data`.

## Internal API Security Plan

`api/internal/*` should not be protected by the same Control Plane node key middleware used by `api/commerce/*`.

Recommended MVP:

- Do not add an internal API key or IP allowlist middleware for `api/internal/*`.
- Treat `api/internal/*` as a same-node private API surface.
- Keep `api/internal/*` reachable only through localhost or the Docker/internal network.
- Do not expose `api/internal/*` through the public reverse proxy.
- Customer-specific routes still require Storefront JWT auth after Storefront auth is migrated because JWT identifies the customer, not the Storefront-to-CommerceNode boundary.

Reason:

- Control Plane credentials identify Control Plane-to-node admin traffic.
- Storefront and Commerce Node run inside the same node boundary for this MVP.
- Network/deployment isolation is enough for Storefront-to-CommerceNode internal traffic at this stage.
- Customer JWT identifies the actual Storefront customer for cart/order/profile actions.

## Legacy Storefront Controller Inventory

| Legacy Controller | Legacy Route | Storefront Target | Notes |
|---|---|---|---|
| `AuthenticationController` | `api/Authentication/*` | `api/internal/auth/*` | Storefront customer auth. Requires Commerce Node auth context. |
| `PublicCatalogController` | `api/public/catalog/*` | `api/internal/catalog/*` | Read-only, lower risk. |
| `ProductController` public reads | `api/Product/catalog`, `api/Product/single/{id}` | `api/internal/catalog/products`, `api/internal/catalog/products/{id}` | Duplicate with `PublicCatalogController`; expose canonical internal routes. |
| `CategoryController` public reads | `api/Category/all`, `api/Category/single/{id}`, `api/Category/products-by-category/{categoryId}` | `api/internal/catalog/categories*` | Duplicate with `PublicCatalogController`; expose canonical internal routes. |
| `SeoSettingsController` | `api/seo/settings` | `api/internal/seo/settings` | Read-only SEO settings. |
| `PublicSeoRedirectsController` | `api/public/seo/redirects/resolve` | `api/internal/seo/redirects/resolve` | Read-only redirect resolution. |
| `ProductRecommendationController` | `api/ProductRecommendation/{productId}` | `api/internal/recommendations/products/{productId}` | Needs Commerce Node recommendation repository adapter. |
| `PaymentController` | `api/Payment/*` | `api/internal/payments/*` | PayPal redirect needs special review. |
| `NewsletterController` | `api/Newsletter/subscribe` | `api/internal/newsletter/subscribe` | Needs email side-effect decision. |
| `CartController` | `api/Cart/*` | `api/internal/cart/*`, `api/internal/orders/*` | Requires Storefront auth/current customer. |

## Target Route Map

### Storefront Auth

- `POST /api/internal/auth/create`
- `POST /api/internal/auth/login`
- `POST /api/internal/auth/refresh-token`
- `POST /api/internal/auth/logout`
- `POST /api/internal/auth/change-password`
- `GET /api/internal/auth/confirm-email`
- `POST /api/internal/auth/update-profile`

### Catalog

- `GET /api/internal/catalog/categories`
- `GET /api/internal/catalog/categories/{id}`
- `GET /api/internal/catalog/categories/slug/{slug}`
- `GET /api/internal/catalog/categories/{categoryId}/products`
- `GET /api/internal/catalog/products`
- `GET /api/internal/catalog/products/{id}`
- `GET /api/internal/catalog/products/slug/{slug}`
- `GET /api/internal/catalog/sitemap`

### SEO

- `GET /api/internal/seo/settings`
- `GET /api/internal/seo/redirects/resolve?path=...`

### Recommendations

- `GET /api/internal/recommendations/products/{productId}`

### Payments

- `GET /api/internal/payments/methods`
- `GET /api/internal/payments/paypal/capture?token=...`

### Newsletter

- `POST /api/internal/newsletter/subscribe`

### Cart And Orders

- `POST /api/internal/cart/checkout`
- `POST /api/internal/cart/save-checkout`
- `POST /api/internal/orders/confirm`
- `GET /api/internal/orders/current-user`
- `GET /api/internal/orders/current-user/items`

Legacy `CartController` Admin-only actions should not be migrated into Storefront by default:

- `GET api/Cart/order-items`
- `GET api/Cart/orders`
- `PUT api/Cart/orders/{orderId}/tracking`
- `PUT api/Cart/orders/{orderId}/shipping-status`

Admin order management already exists under `api/commerce/admin/orders`.

## Implementation Phases

## Phase 0 - Scope Lock And Naming Cleanup

Database:

- [x] Confirm `compose.commercenode.yml` will use host port `5434`.
- [x] Confirm one physical Commerce Node database with one `CommerceNodeConnection`.
- [x] Confirm `CommerceNodeDbContext` will own Storefront auth tables when auth is migrated.

API:

- [x] Rename existing Commerce Admin controller classes/files to `Commerce*Controller`.
- [x] Keep existing `api/commerce/admin/*` routes unchanged.
- [x] Add `Storefront*Controller` naming rule for new internal controllers.
- [x] Confirm `api/internal/*` route prefix.

Services:

- [ ] Keep existing Application services where repository/context binding can be switched safely.
- [ ] Do not modify legacy `BlazorShop.Presentation/BlazorShop.API`.

QA:

- [x] Build solution after controller class/file renames.
- [ ] Verify Swagger still exposes existing Commerce Admin routes.

Stop gate: naming cleanup is complete with no route changes.

## Phase 1 - Commerce Node PostgreSQL Container

Database:

- [x] Add `compose.commercenode.yml`.
- [x] Add `commercenode-postgres` service.
- [x] Add `commercenode_postgres_data` volume.
- [x] Move Commerce Node local default connection from port `5433` to `5434`.
- [ ] Document startup command:

```powershell
docker compose -f compose.commercenode.yml up -d
```

API:

- [x] Ensure `BlazorShop.CommerceNode.API` reads `CommerceNodeConnection`.
- [x] Ensure no `CommerceNodeAuthConnection`, `AuthConnection`, or `DefaultConnection` fallback is used by Commerce Node.

QA:

- [ ] Start Commerce Node PostgreSQL container.
- [ ] Run Commerce Node commerce migration against clean DB.
- [ ] Verify `api/commerce/healthz` still runs.

Stop gate: Commerce Node has its own working PostgreSQL container and migrations no longer hit Control Plane DB credentials.

## Phase 2 - Internal API Route Foundation

Database:

- [ ] No schema changes.

API:

- [x] Do not add middleware for `api/internal/*`.
- [x] Confirm `api/internal/*` routes are not covered by `CommerceNodeCredentialMiddleware`.
- [x] Add a lightweight `StorefrontInternalControllerBase` for consistent response handling.
- [x] Document that `api/internal/*` must be bound privately by deployment config.
- [x] Customer-specific `api/internal/*` routes will use JWT auth after Storefront auth migration.

Services:

- [x] Keep middleware separate from `CommerceNodeCredentialMiddleware`.

QA:

- [x] `api/internal/*` does not require Control Plane node key headers.
- [ ] `api/internal/*` remains absent from public reverse-proxy exposure checklist.
- [ ] `api/commerce/*` still uses node key + node secret.

Stop gate: Admin and Storefront internal boundaries are separate, and internal routes are private-by-network for MVP.

## Phase 3 - Read-Only Catalog Internal API

Database:

- [ ] Use existing `Categories`, `Products`, `ProductVariants`.
- [ ] Reuse existing Commerce Node repositories:
  - `CommerceNodeCategoryRepository`
  - `CommerceNodeProductReadRepository`

API:

- [x] Add `StorefrontCatalogController`.
- [x] Migrate `PublicCatalogController.GetCategories`.
- [x] Migrate `PublicCatalogController.GetSitemap`.
- [x] Migrate `PublicCatalogController.GetCategoryBySlug`.
- [x] Migrate `PublicCatalogController.GetProducts`.
- [x] Migrate `PublicCatalogController.GetProductBySlug`.
- [x] Migrate `ProductController.GetCatalog`.
- [x] Migrate `ProductController.GetSingle`.
- [x] Migrate `CategoryController.GetAll`.
- [x] Migrate `CategoryController.GetById`.
- [x] Migrate `CategoryController.GetProductsByCategory`.

Services:

- [x] Reuse `PublicCatalogService`.
- [x] Ensure `IPublicCatalogService` is registered in Commerce Node DI.

QA:

- [ ] List published categories.
- [ ] Get category by id.
- [ ] Get category by slug.
- [ ] Get category products.
- [ ] List published products with query.
- [ ] Get product by id.
- [ ] Get product by slug.
- [ ] Get sitemap.
- [ ] Verify unpublished products/categories are not returned.

Stop gate: Storefront can read catalog from Commerce Node without legacy API.

## Phase 4 - Storefront SEO Internal API

Database:

- [ ] Use existing `SeoSettings`.
- [ ] Use existing `SeoRedirects`.

API:

- [x] Add `StorefrontSeoController`.
- [x] Migrate `SeoSettingsController.Get`.
- [x] Migrate `PublicSeoRedirectsController.Resolve`.

Services:

- [x] Reuse `SeoSettingsService`.
- [x] Reuse `SeoRedirectResolutionService`.
- [x] Ensure repository bindings point to Commerce Node.

QA:

- [ ] Get SEO settings.
- [ ] Resolve matching redirect.
- [ ] Resolve non-matching redirect returns `success=false` with message or 404 envelope, matching API response policy.

Stop gate: Storefront SEO reads no longer require legacy API.

## Phase 5 - Recommendations, Payments, Newsletter

Database:

- [ ] Use `Products`, `OrderLines`, `PaymentMethods`, `NewsletterSubscribers`.

API:

- [x] Add `StorefrontRecommendationsController`.
- [x] Add `StorefrontPaymentsController`.
- [x] Add `StorefrontNewsletterController`.
- [x] Migrate `ProductRecommendationController.GetRecommendations`.
- [x] Migrate `PaymentController.GetPaymentMethods`.
- [x] Review and migrate `PaymentController.CapturePayPal`.
- [x] Migrate `NewsletterController.Subscribe`.

Services:

- [x] Add `CommerceNodeProductRecommendationRepository`.
- [x] Add `CommerceNodePaymentMethodRepository`.
- [x] Reuse `ProductRecommendationService`.
- [x] Reuse `PaymentMethodService`.
- [x] Reuse `NewsletterService` if email side effects are acceptable.
- [ ] If email side effects are not ready, add Commerce Node newsletter service variant that stores subscription and defers email.

QA:

- [ ] Recommendations by product return expected list.
- [ ] Empty recommendations return envelope with clear message.
- [ ] Payment methods exclude disabled methods as legacy does.
- [ ] PayPal capture success redirects correctly.
- [ ] PayPal capture failure redirects correctly.
- [ ] Newsletter subscribe creates row.
- [ ] Duplicate newsletter email returns `success=false` with message.

Stop gate: Storefront non-auth utility APIs run from Commerce Node.

## Phase 6 - Storefront Auth Database Isolation

Database:

- [x] Change `CommerceNodeDbContext` from `DbContext` to `IdentityDbContext<AppUser>`.
- [x] Configure Identity tables for `AppUser` inside `CommerceNodeDbContext`.
- [x] Add and configure `RefreshToken` inside `CommerceNodeDbContext`.
- [x] Seed Storefront roles.
- [x] Create auth migration against Commerce Node database.

API:

- [x] Add `StorefrontAuthController`.
- [x] Migrate `AuthenticationController.CreateUser`.
- [x] Migrate `AuthenticationController.LoginUser`.
- [x] Migrate `AuthenticationController.RefreshToken`.
- [x] Migrate `AuthenticationController.Logout`.
- [x] Migrate `AuthenticationController.ChangePassword`.
- [x] Migrate `AuthenticationController.ConfirmEmail`.
- [x] Migrate `AuthenticationController.UpdateProfile`.
- [x] Preserve refresh-token cookie behavior.

Services:

- [x] Reuse `AuthenticationService` if possible.
- [x] Add Commerce Node auth DI method that registers Identity against `CommerceNodeDbContext`.
- [x] Add Commerce Node versions or generic versions of:
  - `IAppUserManager`
  - `IAppTokenManager`
  - `IAppRoleManager`
- [x] Avoid depending on `AppDbContext`.
- [x] Use only `CommerceNodeConnection`.
- [x] Decide whether first Storefront user should become `Admin` or always `User`.

QA:

- [ ] Register user.
- [ ] Login user.
- [ ] Login wrong password.
- [ ] Repeated wrong password lockout.
- [ ] Refresh token.
- [ ] Logout revokes token.
- [ ] Change password.
- [ ] Confirm email.
- [ ] Update profile.
- [ ] Verify auth DB tables are created only in Commerce Node database.

Stop gate: Storefront customer identity works inside `CommerceNodeDbContext` without legacy `AppDbContext`.

## Phase 7 - Cart And Order Internal API

Database:

- [ ] Use `CheckoutOrderItems`.
- [ ] Use `Orders`.
- [ ] Use `OrderLines`.
- [ ] Consider adding customer snapshot fields to `Orders`:
  - `CustomerEmail`
  - `CustomerName`
- [ ] If customer snapshot is added, update admin order mapping to use it.

API:

- [ ] Add `StorefrontCartController`.
- [ ] Add `StorefrontOrdersController`.
- [ ] Migrate `CartController.Checkout`.
- [ ] Migrate `CartController.SaveCheckout`.
- [ ] Migrate `CartController.ConfirmOrder`.
- [ ] Migrate `CartController.GetUserOrderItems`.
- [ ] Migrate `CartController.GetUserOrders`.
- [ ] Do not migrate legacy `CartController.GetAllOrders` into Storefront unless review proves it is used.
- [ ] Do not migrate legacy `CartController.UpdateTracking` into Storefront unless review proves it is used.
- [ ] Do not migrate legacy `CartController.UpdateShippingStatus` into Storefront unless review proves it is used.

Services:

- [ ] Add `CommerceNodeCartRepository`.
- [ ] Reuse `CartService` if `IAppUserManager` and repository bindings work with Commerce Node auth/database.
- [ ] Add Commerce Node order query service variant if existing `OrderQueryService` still assumes legacy users.
- [ ] Reuse `CommerceNodeOrderRepository`.
- [ ] Reuse `CommerceNodeOrderTrackingService` only if needed by reviewed legacy shortcuts.
- [ ] Review email side effects for bank transfer instructions.

QA:

- [ ] Checkout with credit card.
- [ ] Checkout with cash on delivery.
- [ ] Checkout with bank transfer.
- [ ] Save checkout history.
- [ ] Confirm order.
- [ ] Get current user orders.
- [ ] Get current user order items.
- [ ] Verify unauthorized user routes reject missing/invalid JWT.
- [ ] Verify order lines and totals are persisted correctly.
- [ ] Verify admin order list can show customer snapshot after checkout.

Stop gate: Storefront can complete basic order flow through Commerce Node.

## Phase 8 - Storefront Client Cutover

Database:

- [ ] No schema changes expected.

API:

- [ ] Add typed client/service in Storefront for `api/internal/*`.
- [ ] Update Storefront API base URL config to Commerce Node internal API.
- [ ] Keep legacy API config available as fallback until parity is verified.

Services:

- [ ] Ensure Storefront calls Commerce Node over same-node private networking.
- [ ] Ensure customer auth token/cookie flow still works through Storefront.

QA:

- [ ] Storefront loads catalog from Commerce Node.
- [ ] Storefront product detail loads from Commerce Node.
- [ ] Storefront login/register works.
- [ ] Storefront cart checkout works.
- [ ] Storefront order history works.
- [ ] Compare key responses with legacy API for same seeded data.

Stop gate: Storefront can run without calling legacy API for migrated surfaces.

## Phase 9 - Legacy Storefront API Decommission Plan

- [ ] Mark migrated legacy Storefront routes as deprecated in docs.
- [ ] Keep legacy API physically present until full QA is complete.
- [ ] Remove legacy API calls from Storefront config only after cutover.
- [ ] Remove legacy Storefront API routes only in a separate approved phase.

## Service Migration Notes

| Service | Existing Dependency | Storefront Migration Action |
|---|---|---|
| `PublicCatalogService` | Repository based | Reuse with Commerce Node repository bindings. |
| `SeoSettingsService` | Repository based | Reuse with Commerce Node repository bindings. |
| `SeoRedirectResolutionService` | Repository based | Reuse with Commerce Node repository bindings. |
| `ProductRecommendationService` | `IProductRecommendationRepository`, cache, logger | Reuse after adding Commerce Node recommendation repository. |
| `PaymentMethodService` | `IPaymentMethod` | Reuse after adding Commerce Node payment method repository. |
| `NewsletterService` | Generic repository + email | Reuse if email configured; otherwise add no-email Commerce Node variant for MVP. |
| `AuthenticationService` | `IAppUserManager`, `IAppTokenManager`, `IAppRoleManager` | Reuse only after managers are wired to `CommerceNodeDbContext`. |
| `CartService` | cart/order/product/payment/user/email | Reuse after Commerce Node cart repo and auth manager bindings exist. |
| `OrderQueryService` | order/product/user manager | Reuse or add Commerce Node variant; should not query legacy users. |

## Open Questions To Close Before Implementation

- Confirm reverse proxy/deployment excludes `api/internal/*` from public exposure.
- Should Storefront registration still create a global `User`, or should it require/derive a `StoreId` from domain/store context?
- Should Storefront registration still assign first user to `Admin`, or should all Storefront registrations be `User`?
- Should order checkout add `CustomerEmail` and `CustomerName` snapshots now?
- Should newsletter/bank-transfer emails be enabled in Commerce Node MVP or deferred?
- Should PayPal capture remain an API redirect, or should Storefront handle callback and call Commerce Node internally?
- Should Storefront auth routes remain under `api/internal/auth/*`, or does the deployment require a public-facing auth proxy route?

## Review Checklist

- [ ] `BlazorShop.CommerceNode.API` has no `AppDbContext` dependency.
- [ ] Control Plane DB and Commerce Node DB run in separate PostgreSQL containers.
- [ ] Commerce Node uses one connection string: `CommerceNodeConnection`.
- [ ] Storefront Identity tables are owned by `CommerceNodeDbContext`.
- [ ] Commerce Admin controllers are named `Commerce*Controller`.
- [ ] Storefront controllers are named `Storefront*Controller`.
- [ ] `api/commerce/*` uses node credential middleware; `api/internal/*` is private-by-network and does not use internal key/IP middleware.
- [ ] Read-only Storefront APIs are migrated before auth/cart/order.
- [ ] Storefront auth tables live in Commerce Node database, not Control Plane database, and do not require a second Commerce Node connection.
- [ ] Legacy API remains untouched until explicit cutover.
