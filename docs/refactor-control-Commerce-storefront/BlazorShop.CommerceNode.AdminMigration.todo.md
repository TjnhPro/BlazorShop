# BlazorShop Commerce Node Admin Migration Todo

## Goal

Move legacy commerce-admin business from `BlazorShop.Presentation/BlazorShop.API` into the new Commerce Node boundary without refactoring or deleting legacy Presentation code.

Commerce Node has two API boundaries:

- `api/commerce/*`: Control Plane calls Commerce Node. Auth is node key + node secret + allowed IP.
- `api/internal/*`: Storefront calls Commerce Node through a private internal API. This is not the Admin-first scope.

This plan is for review before implementation. Do not remove or rename legacy API routes until parity is verified.

## Investigation Summary

`BlazorShop.API` contains 22 controllers. The boundary is mixed:

- Some controllers are pure Admin commerce.
- Some controllers are pure Storefront/public.
- Some controllers mix Admin and Storefront routes inside the same file.
- Auth/user management is legacy Identity and must not be copied into Commerce Node.
- Mixed controllers must be split by action-level authorization, especially `[Authorize(Roles = "Admin")]`.

The migration should therefore split by use case and route, not by copying controllers wholesale.

## Architecture Decisions

- Keep `BlazorShop.Presentation/BlazorShop.API` as legacy during migration.
- Put new admin commerce endpoints in `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API`.
- Reuse existing Application DTOs/services where practical.
- Do not inject `AppDbContext` into Commerce Node.
- Use `CommerceNodeDbContext` for all Commerce Node database access.
- Keep Control Plane auth/user/permission management separate from Commerce Node.
- Treat Control Plane as the admin gateway. Commerce Node receives trusted node-authenticated admin requests from Control Plane.
- Use the existing API response envelope in Commerce Node.
- Use `[Authorize(Roles = "Admin")]` as a discovery signal, then classify by business boundary. A legacy Admin role on a Storefront controller does not automatically make that action part of Control Plane Admin.

## Current Legacy Controller Inventory

| Controller | Legacy Route | Current Auth | Classification | Target Route | Migration Phase | Notes |
|---|---|---:|---|---|---:|---|
| `AdminAuditController` | `api/admin/audit` | Admin | Admin support | `api/commerce/admin/audit` | 7 | Requires Commerce Node audit table. |
| `AdminInventoryController` | `api/admin/inventory` | Admin | Admin commerce | `api/commerce/admin/inventory` | 3 | Uses products and variants. |
| `AdminOrdersController` | `api/admin/orders` | Admin | Admin commerce | `api/commerce/admin/orders` | 5 | Admin-first order management source. |
| `AdminSettingsController` | `api/admin/settings` | Admin | Admin config | `api/commerce/admin/settings` | 6 | Requires Commerce Node settings table. |
| `AdminUsersController` | `api/admin/users` | Admin | Legacy auth/admin | Do not migrate | Deferred | Uses Identity `AppUser`, roles, lockout. Control Plane replaces this. |
| `AdminProductSeoController` | `api/admin/products/{id}/seo` | Admin | Admin SEO | `api/commerce/admin/products/{id}/seo` | 4 | Reuse SEO service logic with Commerce Node repositories. |
| `AdminCategorySeoController` | `api/admin/categories/{id}/seo` | Admin | Admin SEO | `api/commerce/admin/categories/{id}/seo` | 4 | Reuse SEO service logic with Commerce Node repositories. |
| `AdminSeoRedirectsController` | `api/admin/seo/redirects` | Admin | Admin SEO | `api/commerce/admin/seo/redirects` | 4 | CRUD/deactivate/delete. |
| `AdminSeoSettingsController` | `api/admin/seo/settings` | Admin | Admin SEO | `api/commerce/admin/seo/settings` | 4 | Read/update SEO settings. |
| `ProductController` | `api/Product/*` | Mixed | Mixed | Split by action | 2 and 8 | Exact `[Authorize(Roles = "Admin")]` actions go to Admin; unauthenticated catalog reads go to Storefront/internal. |
| `CategoryController` | `api/Category/*` | Mixed | Mixed | Split by action | 2 and 8 | Exact `[Authorize(Roles = "Admin")]` actions go to Admin; unauthenticated catalog reads go to Storefront/internal. |
| `ProductVariantController` | `api/product/*/variants` | Admin | Admin commerce | `api/commerce/admin/products/{productId}/variants` | 3 | Legacy read endpoint is Admin-only. |
| `FileUploadController` | `api/upload/image` | Admin | Admin media | `api/commerce/admin/media/images` | 6 | Needs storage policy per node. |
| `MetricsController` | `api/Metrics/*` | Admin | Admin analytics | `api/commerce/admin/metrics` | 7 | Uses orders and newsletter subscribers. |
| `AuthenticationController` | `api/Authentication/*` | Mixed | Storefront auth | `api/internal/auth/*` | 8 | Commerce Node Storefront auth logic; not Control Plane Admin. |
| `CartController` | `api/Cart/*` | Mixed | Storefront cart/order | `api/internal/cart/*`, `api/internal/orders/*` | 8 | Entire controller belongs to Storefront scope. Legacy Admin roles exist because auth was shared. |
| `PaymentController` | `api/Payment/*` | Public | Storefront/payment | `api/internal/payments/*` | 8 | PayPal capture redirects to client app; needs separate design. |
| `NewsletterController` | `api/Newsletter/subscribe` | Public | Storefront | `api/internal/newsletter/subscribe` | 8 | May stay public through Storefront, not Control Plane. |
| `ProductRecommendationController` | `api/ProductRecommendation/{productId}` | Public | Storefront | `api/internal/recommendations/products/{productId}` | 8 | Depends catalog read model. |
| `PublicCatalogController` | `api/public/catalog/*` | Public | Storefront catalog | `api/internal/catalog/*` | 8 | Full Storefront catalog read API. |
| `PublicSeoRedirectsController` | `api/public/seo/redirects/resolve` | Public | Storefront SEO | `api/internal/seo/redirects/resolve` | 8 | Read-only public resolution. |
| `SeoSettingsController` | `api/seo/settings` | Public | Storefront SEO | `api/internal/seo/settings` | 8 | Read-only settings. |

## Mixed Controller Action Split

Rule for Admin-first migration:

- Action has exact `[Authorize(Roles = "Admin")]`: mark as a Commerce Admin candidate, then confirm the controller business boundary.
- Action has no `[Authorize]`: migrate later to `api/internal/*` if it is Storefront-facing.
- Action has `[Authorize(Roles = "User")]`: migrate later with Storefront/customer auth.
- Action has `[Authorize(Roles = "User, Admin")]`: do not treat as Control Plane Admin by default, because the action still depends on the current storefront user identity.
- `CartController` is an explicit Storefront controller even when a legacy action has exact Admin role.
- `AuthenticationController` is Commerce Node Storefront auth, not Control Plane Admin.

### ProductController

| Legacy Action | Legacy Route | Auth | Target Boundary | Target Route | Phase | Decision |
|---|---|---|---|---|---:|---|
| `GetAll` | `GET api/Product/all` | Admin | Commerce Admin | `GET /api/commerce/admin/products` | 2 | Migrate. |
| `Add` | `POST api/Product/add` | Admin | Commerce Admin | `POST /api/commerce/admin/products` | 2 | Migrate. |
| `Update` | `PUT api/Product/update` | Admin | Commerce Admin | `PUT /api/commerce/admin/products/{id}` | 2 | Migrate; prefer route id over body-only id if DTO supports it cleanly. |
| `Delete` | `DELETE api/Product/delete/{id}` | Admin | Commerce Admin | `DELETE /api/commerce/admin/products/{id}` | 2 | Migrate. |
| `GetCatalog` | `GET api/Product/catalog` | Public | Storefront Internal | `GET /api/internal/catalog/products` | 8 | Do not migrate in Admin-first. |
| `GetSingle` | `GET api/Product/single/{id}` | Public | Storefront Internal | `GET /api/internal/catalog/products/{id}` | 8 | Do not migrate in Admin-first. |

### CategoryController

| Legacy Action | Legacy Route | Auth | Target Boundary | Target Route | Phase | Decision |
|---|---|---|---|---|---:|---|
| `GetAllForAdmin` | `GET api/Category/all/admin` | Admin | Commerce Admin | `GET /api/commerce/admin/categories` | 2 | Migrate. |
| `Add` | `POST api/Category/add` | Admin | Commerce Admin | `POST /api/commerce/admin/categories` | 2 | Migrate. |
| `Update` | `PUT api/Category/update` | Admin | Commerce Admin | `PUT /api/commerce/admin/categories/{id}` | 2 | Migrate; prefer route id over body-only id if DTO supports it cleanly. |
| `Delete` | `DELETE api/Category/delete/{id}` | Admin | Commerce Admin | `DELETE /api/commerce/admin/categories/{id}` | 2 | Migrate. |
| `GetAll` | `GET api/Category/all` | Public | Storefront Internal | `GET /api/internal/catalog/categories` | 8 | Do not migrate in Admin-first. |
| `GetById` | `GET api/Category/single/{id}` | Public | Storefront Internal | `GET /api/internal/catalog/categories/{id}` | 8 | Do not migrate in Admin-first. |
| `GetProductsByCategory` | `GET api/Category/products-by-category/{categoryId}` | Public | Storefront Internal | `GET /api/internal/catalog/categories/{categoryId}/products` | 8 | Do not migrate in Admin-first. |

### CartController

| Legacy Action | Legacy Route | Auth | Target Boundary | Target Route | Phase | Decision |
|---|---|---|---|---|---:|---|
| `GetAllCheckoutHistory` | `GET api/Cart/order-items` | Admin | Storefront/Internal review | TBD under `api/internal/*` only if still used | 8 | Do not migrate in Admin-first; legacy Admin role came from shared auth. |
| `GetAllOrders` | `GET api/Cart/orders` | Admin | Storefront/Internal review | TBD under `api/internal/*` only if still used | 8 | Do not migrate in Admin-first; Admin order management should come from `AdminOrdersController`. |
| `UpdateTracking` | `PUT api/Cart/orders/{orderId}/tracking` | Admin | Storefront/Internal review | TBD under `api/internal/*` only if still used | 8 | Do not migrate in Admin-first; review usage before copying. |
| `UpdateShippingStatus` | `PUT api/Cart/orders/{orderId}/shipping-status` | Admin | Storefront/Internal review | TBD under `api/internal/*` only if still used | 8 | Do not migrate in Admin-first; review usage before copying. |
| `Checkout` | `POST api/Cart/checkout` | User | Storefront Internal | `POST /api/internal/cart/checkout` | 8 | Do not migrate in Admin-first. |
| `SaveCheckout` | `POST api/Cart/save-checkout` | User | Storefront Internal | `POST /api/internal/cart/save-checkout` | 8 | Do not migrate in Admin-first. |
| `ConfirmOrder` | `POST api/Cart/confirm-order` | User, Admin | Storefront Internal | `POST /api/internal/orders/confirm` | 8 | Do not migrate in Admin-first; depends on current storefront user id. |
| `GetUserOrderItems` | `GET api/Cart/user/order-items` | User, Admin | Storefront Internal | `GET /api/internal/orders/current-user/items` | 8 | Do not migrate in Admin-first; depends on current storefront user id. |
| `GetUserOrders` | `GET api/Cart/user/orders` | User, Admin | Storefront Internal | `GET /api/internal/orders/current-user` | 8 | Do not migrate in Admin-first; depends on current storefront user id. |

### AuthenticationController

| Legacy Action | Legacy Route | Auth | Target Boundary | Target Route | Phase | Decision |
|---|---|---|---|---|---:|---|
| `CreateUser` | `POST api/Authentication/create` | Public + rate limit | Storefront Auth | `POST /api/internal/auth/create` | 8 | Migrate in Storefront auth phase; preserve existing logic and adapt context. |
| `LoginUser` | `POST api/Authentication/login` | Public + rate limit | Storefront Auth | `POST /api/internal/auth/login` | 8 | Migrate in Storefront auth phase; preserve JWT/refresh-cookie behavior unless later changed. |
| `RefreshToken` | `POST api/Authentication/refresh-token` | Public + rate limit | Storefront Auth | `POST /api/internal/auth/refresh-token` | 8 | Migrate in Storefront auth phase. |
| `Logout` | `POST api/Authentication/logout` | Public + rate limit | Storefront Auth | `POST /api/internal/auth/logout` | 8 | Migrate in Storefront auth phase. |
| `ChangePassword` | `POST api/Authentication/change-password` | User, Admin | Storefront Auth | `POST /api/internal/auth/change-password` | 8 | Migrate in Storefront auth phase; Admin role here is shared legacy auth, not Control Plane Admin. |
| `ConfirmEmail` | `GET api/Authentication/confirm-email` | Public | Storefront Auth | `GET /api/internal/auth/confirm-email` | 8 | Migrate in Storefront auth phase. |
| `UpdateProfile` | `POST api/Authentication/update-profile` | User, Admin | Storefront Auth | `POST /api/internal/auth/update-profile` | 8 | Migrate in Storefront auth phase; Admin role here is shared legacy auth, not Control Plane Admin. |

### ProductVariantController

Although the controller class itself is not `[Authorize]`, every action is exact `[Authorize(Roles = "Admin")]`, so the whole controller belongs to Admin-first Phase 3.

| Legacy Action | Legacy Route | Auth | Target Boundary | Target Route | Phase | Decision |
|---|---|---|---|---|---:|---|
| `GetByProductId` | `GET api/product/{productId}/variants` | Admin | Commerce Admin | `GET /api/commerce/admin/products/{productId}/variants` | 3 | Migrate. |
| `Add` | `POST api/product/{productId}/variants` | Admin | Commerce Admin | `POST /api/commerce/admin/products/{productId}/variants` | 3 | Migrate. |
| `Update` | `PUT api/product/variants` | Admin | Commerce Admin | `PUT /api/commerce/admin/products/{productId}/variants/{variantId}` | 3 | Migrate; prefer explicit route ids. |
| `Delete` | `DELETE api/product/variants/{variantId}` | Admin | Commerce Admin | `DELETE /api/commerce/admin/products/{productId}/variants/{variantId}` | 3 | Migrate; product id can be optional only if service can validate variant ownership another way. |

### FileUploadController

The controller is not `[Authorize]`, but `UploadFile` is exact `[Authorize(Roles = "Admin")]`, so only that action belongs to Admin-first media migration.

| Legacy Action | Legacy Route | Auth | Target Boundary | Target Route | Phase | Decision |
|---|---|---|---|---|---:|---|
| `UploadFile` | `POST api/upload/image` | Admin | Commerce Admin | `POST /api/commerce/admin/media/images` | 6 | Migrate after storage policy is accepted. |

## Admin Authorization Coverage Audit

This audit was generated by scanning all `BlazorShop.Presentation/BlazorShop.API/Controllers/*.cs` for:

- `[Authorize(Roles = "Admin")]` at controller level.
- `[Authorize(Roles = "Admin")]` at action level.
- `[Authorize(Roles = "User, Admin")]` at action level.

Coverage result:

- 22 controller files scanned.
- 10 exact Admin controller-level attributes found.
- 17 exact Admin action-level attributes found.
- 5 `User, Admin` action-level attributes found.
- No exact Admin candidate was found outside the inventory below.
- Exact Admin attributes in `CartController` are intentionally excluded from Admin-first because the controller is Storefront cart/order scope.

### Exact Admin Controller-Level Coverage

These controllers have `[Authorize(Roles = "Admin")]` at controller level. Every action inside the controller inherits Admin authorization.

| Controller | Target Decision | Action Coverage |
|---|---|---|
| `AdminAuditController` | Migrate to Commerce Admin Phase 7 | `Get`, `GetById` |
| `AdminCategorySeoController` | Migrate to Commerce Admin Phase 4 | `Get`, `Update` |
| `AdminInventoryController` | Migrate to Commerce Admin Phase 3 | `Get`, `UpdateProductStock`, `UpdateVariantStock` |
| `AdminOrdersController` | Migrate to Commerce Admin Phase 5 | `Get`, `GetById`, `UpdateTracking`, `UpdateShippingStatus`, `UpdateAdminNote` |
| `AdminProductSeoController` | Migrate to Commerce Admin Phase 4 | `Get`, `Update` |
| `AdminSeoRedirectsController` | Migrate to Commerce Admin Phase 4 | `GetAll`, `GetById`, `Create`, `Update`, `Deactivate`, `Delete` |
| `AdminSeoSettingsController` | Migrate to Commerce Admin Phase 4 | `Get`, `Update` |
| `AdminSettingsController` | Migrate to Commerce Admin Phase 6 | `Get`, `UpdateStore`, `UpdateOrders`, `UpdateNotifications` |
| `MetricsController` | Migrate to Commerce Admin Phase 7 | `GetSales`, `GetTraffic` |
| `AdminUsersController` | Do not migrate | `GetAll`, `GetRoles`, `GetById`, `UpdateRoles`, `Lock`, `Unlock`, `ConfirmEmail`, `RequirePasswordChange`, `Deactivate`; excluded because it is Identity/user-management scope and Control Plane replaces it. |

### Exact Admin Action-Level Coverage

These actions have exact `[Authorize(Roles = "Admin")]` at method level.

| Controller | Admin Actions | Target Decision |
|---|---|---|
| `ProductController` | `GetAll`, `Add`, `Update`, `Delete` | Migrate to Commerce Admin Phase 2. |
| `CategoryController` | `GetAllForAdmin`, `Add`, `Update`, `Delete` | Migrate to Commerce Admin Phase 2. |
| `CartController` | `GetAllCheckoutHistory`, `GetAllOrders`, `UpdateTracking`, `UpdateShippingStatus` | Do not migrate in Admin-first; entire controller belongs to Storefront/internal review. |
| `ProductVariantController` | `GetByProductId`, `Add`, `Update`, `Delete` | Migrate to Commerce Admin Phase 3. |
| `FileUploadController` | `UploadFile` | Migrate to Commerce Admin Phase 6 after storage policy review. |

### Admin-Containing But Not Commerce Admin-First

These actions contain `Admin` in the roles list but are not exact Admin-only actions. They depend on current user identity and should not be pulled into Control Plane Admin by default.

| Controller | Action | Auth | Target Decision |
|---|---|---|---|
| `AuthenticationController` | `ChangePassword` | `User, Admin` | Storefront auth; migrate later under `api/internal/auth/*`. |
| `AuthenticationController` | `UpdateProfile` | `User, Admin` | Storefront auth; migrate later under `api/internal/auth/*`. |
| `CartController` | `ConfirmOrder` | `User, Admin` | Storefront/internal cart-order scope; migrate later. |
| `CartController` | `GetUserOrderItems` | `User, Admin` | Storefront/internal cart-order scope; migrate later. |
| `CartController` | `GetUserOrders` | `User, Admin` | Storefront/internal cart-order scope; migrate later. |

## Database Plan

### Existing Commerce Node Tables

`CommerceNodeDbContext` already maps these commerce tables:

- `Categories`
- `Products`
- `ProductVariants`
- `PaymentMethods`
- `CheckoutOrderItems`
- `NewsletterSubscribers`
- `Orders`
- `OrderLines`
- `SeoRedirects`
- `SeoSettings`

### Missing Commerce Node Admin Tables

Add these to `CommerceNodeDbContext` before moving admin support services:

- `AdminAuditLogs`
- `AdminSettings`

These domain entities already exist and do not require Identity tables:

- `BlazorShop.Domain.Entities.AdminAuditLog`
- `BlazorShop.Domain.Entities.AdminSettings`

Recommended MVP choice: reuse the existing entity classes and existing EF configurations in Commerce Node, but register them against `CommerceNodeDbContext`.

### Tables Not Allowed In Admin-First Scope

Do not add these to the Admin-first migration:

- `AspNetUsers`
- `AspNetRoles`
- `AspNetUserRoles`
- `RefreshTokens`
- legacy `AppUser`
- any Identity auth table

Reason: Control Plane owns admin user management, and Admin-first commerce APIs must not pull auth/user-management into the Control Plane boundary.

Storefront auth is a separate Commerce Node phase. In that phase, the existing `AuthenticationController` and `AuthenticationService` logic can be moved with the smallest dependency changes needed for Commerce Node database scope.

### Order User Data Risk

Legacy orders store `UserId` and admin order mapping currently joins `AppDbContext.Users` to show customer name/email.

Admin-first migration should not join legacy `AppDbContext.Users`. The migration must choose one of these before moving Admin Orders:

- Add denormalized customer fields to `Order`, such as `CustomerName` and `CustomerEmail`.
- Keep `UserId` only and show limited order data until Commerce Node Storefront auth/customer design is migrated.
- Add a Commerce Node customer profile table later under Storefront/internal scope.

Recommended for Admin-first MVP: add denormalized customer snapshot fields to orders when checkout is migrated; until then Admin Orders can return `UserId` and blank customer display fields.

## API Route Plan

### Admin Routes From Control Plane

All routes below are under Commerce Node and protected by node key + node secret + allowed IP.

- `GET /api/commerce/admin/products`
- `GET /api/commerce/admin/products/{id}`
- `POST /api/commerce/admin/products`
- `PUT /api/commerce/admin/products/{id}`
- `DELETE /api/commerce/admin/products/{id}`
- `GET /api/commerce/admin/categories`
- `GET /api/commerce/admin/categories/{id}`
- `POST /api/commerce/admin/categories`
- `PUT /api/commerce/admin/categories/{id}`
- `DELETE /api/commerce/admin/categories/{id}`
- `GET /api/commerce/admin/products/{productId}/variants`
- `POST /api/commerce/admin/products/{productId}/variants`
- `PUT /api/commerce/admin/products/{productId}/variants/{variantId}`
- `DELETE /api/commerce/admin/products/{productId}/variants/{variantId}`
- `GET /api/commerce/admin/inventory`
- `PUT /api/commerce/admin/inventory/products/{productId}`
- `PUT /api/commerce/admin/inventory/variants/{variantId}`
- `GET /api/commerce/admin/products/{id}/seo`
- `PUT /api/commerce/admin/products/{id}/seo`
- `GET /api/commerce/admin/categories/{id}/seo`
- `PUT /api/commerce/admin/categories/{id}/seo`
- `GET /api/commerce/admin/seo/settings`
- `PUT /api/commerce/admin/seo/settings`
- `GET /api/commerce/admin/seo/redirects`
- `GET /api/commerce/admin/seo/redirects/{id}`
- `POST /api/commerce/admin/seo/redirects`
- `PUT /api/commerce/admin/seo/redirects/{id}`
- `POST /api/commerce/admin/seo/redirects/{id}/deactivate`
- `DELETE /api/commerce/admin/seo/redirects/{id}`
- `GET /api/commerce/admin/orders`
- `GET /api/commerce/admin/orders/{id}`
- `PUT /api/commerce/admin/orders/{id}/tracking`
- `PUT /api/commerce/admin/orders/{id}/shipping-status`
- `PUT /api/commerce/admin/orders/{id}/admin-note`
- `GET /api/commerce/admin/settings`
- `PUT /api/commerce/admin/settings/store`
- `PUT /api/commerce/admin/settings/orders`
- `PUT /api/commerce/admin/settings/notifications`
- `POST /api/commerce/admin/media/images`
- `GET /api/commerce/admin/audit`
- `GET /api/commerce/admin/audit/{id}`
- `GET /api/commerce/admin/metrics/sales`
- `GET /api/commerce/admin/metrics/traffic`

### Storefront Internal Routes

These are not Admin-first, but must be tracked so business is not missed:

- `GET /api/internal/catalog/categories`
- `GET /api/internal/catalog/categories/{id}`
- `GET /api/internal/catalog/categories/slug/{slug}`
- `GET /api/internal/catalog/categories/{categoryId}/products`
- `GET /api/internal/catalog/products`
- `GET /api/internal/catalog/products/{id}`
- `GET /api/internal/catalog/products/slug/{slug}`
- `GET /api/internal/catalog/sitemap`
- `GET /api/internal/seo/settings`
- `GET /api/internal/seo/redirects/resolve`
- `GET /api/internal/recommendations/products/{productId}`
- `GET /api/internal/payments/methods`
- `GET /api/internal/payments/paypal/capture`
- `POST /api/internal/newsletter/subscribe`
- `POST /api/internal/cart/checkout`
- `POST /api/internal/cart/save-checkout`
- `POST /api/internal/orders/confirm`
- `GET /api/internal/orders/current-user`
- `GET /api/internal/orders/current-user/items`

## Implementation Phases

## Phase 0 - Review And Scope Lock

- [ ] Review this inventory against all `BlazorShop.API` controllers.
- [ ] Review the mixed-controller action split before implementing any endpoint.
- [ ] Confirm route prefix: recommended `api/commerce/admin/*` for Control Plane admin operations.
- [ ] Confirm Admin Users/Auth are excluded from Commerce Node migration.
- [ ] Confirm Admin-first priority before Storefront `api/internal/*`.
- [ ] Confirm `CartController` is excluded from Admin-first and remains Storefront/internal scope.
- [ ] Confirm `AuthenticationController` is Storefront auth for Commerce Node, not Control Plane Admin.
- [ ] Confirm whether `AdminAuditLog` and `AdminSettings` reuse existing entity names.

Stop gate: no implementation until this file is approved.

## Phase 1 - Commerce Node Admin Infrastructure

Database:

- [x] Add `DbSet<AdminAuditLog>` to `CommerceNodeDbContext`.
- [x] Add `DbSet<AdminSettings>` to `CommerceNodeDbContext`.
- [x] Apply existing `AdminAuditLogConfiguration`.
- [x] Apply existing `AdminSettingsConfiguration`.
- [x] Create Commerce Node migration for the two admin support tables.

API:

- [x] Add `/api/commerce/admin` route group or controller base convention.
- [x] Reuse Commerce Node API response envelope.
- [x] Keep HTTP status meaningful for client transport, but UI reads `success/message/data`.
- [x] Ensure all admin routes use existing node credential + IP guard.

Services:

- [x] Add Commerce Node equivalents of repository registrations that depend on `CommerceNodeDbContext`.
- [x] Avoid touching legacy `AddInfrastructure`.
- [x] Add a Commerce Node audit actor provider that reads optional Control Plane headers:
  - `X-Control-Plane-Actor-Id`
  - `X-Control-Plane-Actor-Email`
  - `X-Control-Plane-Action-Id`

QA:

- [x] Commerce Node API builds successfully.
- [x] Commerce Node EF migration is generated successfully.
- [ ] Clean Commerce Node database migration succeeds.
- [ ] `api/commerce/healthz` still works with node credentials.
- [ ] Unauthorized admin route calls fail without node credentials.
- [ ] Authorized admin route calls return response envelope.

## Phase 2 - Product And Category Admin CRUD

Database:

- [ ] Use existing `Products` and `Categories`.
- [ ] Verify indexes/constraints from existing configurations are applied in `CommerceNodeDbContext`.

API:

- [ ] Migrate `ProductController.GetAll` from `GET api/Product/all`.
- [ ] Migrate `ProductController.Add` from `POST api/Product/add`.
- [ ] Migrate `ProductController.Update` from `PUT api/Product/update`.
- [ ] Migrate `ProductController.Delete` from `DELETE api/Product/delete/{id}`.
- [ ] Migrate `CategoryController.GetAllForAdmin` from `GET api/Category/all/admin`.
- [ ] Migrate `CategoryController.Add` from `POST api/Category/add`.
- [ ] Migrate `CategoryController.Update` from `PUT api/Category/update`.
- [ ] Migrate `CategoryController.Delete` from `DELETE api/Category/delete/{id}`.
- [ ] Do not migrate storefront catalog reads in this phase.

Services:

- [ ] Reuse `ProductService` and `CategoryService` if they can run against Commerce Node repository bindings.
- [ ] Add Commerce Node repository implementations or generic repository binding for `CommerceNodeDbContext`.
- [ ] Keep existing audit calls, backed by Commerce Node `AdminAuditLogs`.

QA:

- [ ] List products.
- [ ] Create product.
- [ ] Update product.
- [ ] Delete product.
- [ ] List categories.
- [ ] Create category.
- [ ] Update category.
- [ ] Delete category.
- [ ] Verify audit logs are written for create/update/delete.

Stop gate: Control Plane can manage basic catalog records through Commerce Node.

## Phase 3 - Product Variants And Inventory

Database:

- [ ] Use existing `ProductVariants`.
- [ ] Confirm unique index on product + size scale + size value exists.

API:

- [ ] Migrate `ProductVariantController` Admin endpoints.
- [ ] Migrate `AdminInventoryController`.

Services:

- [ ] Reuse `ProductVariantService`.
- [ ] Adapt `AdminInventoryService` to `CommerceNodeDbContext`.
- [ ] Keep inventory audit logs.

QA:

- [ ] List variants by product.
- [ ] Create variant.
- [ ] Update variant.
- [ ] Delete variant.
- [ ] List inventory.
- [ ] Update product stock.
- [ ] Update variant stock.
- [ ] Verify low-stock/out-of-stock filters.
- [ ] Verify audit logs.

## Phase 4 - SEO Admin

Database:

- [ ] Use existing `SeoSettings`.
- [ ] Use existing `SeoRedirects`.
- [ ] Use product/category SEO fields already on existing entities.

API:

- [ ] Migrate product SEO read/update.
- [ ] Migrate category SEO read/update.
- [ ] Migrate SEO settings read/update.
- [ ] Migrate SEO redirect CRUD/deactivate/delete.

Services:

- [ ] Reuse `ProductSeoService`.
- [ ] Reuse `CategorySeoService`.
- [ ] Reuse `SeoSettingsService`.
- [ ] Reuse `SeoRedirectService`.
- [ ] Reuse `SeoRedirectAutomationService`.
- [ ] Provide Commerce Node versions of SEO repositories and transaction manager.

QA:

- [ ] Product SEO get/update.
- [ ] Category SEO get/update.
- [ ] SEO settings get/update.
- [ ] SEO redirect list/create/update/deactivate/delete.
- [ ] Validate duplicate slug/path checks still work.
- [ ] Verify audit logs.

## Phase 5 - Admin Orders

Database:

- [ ] Use existing `Orders` and `OrderLines`.
- [ ] Review whether order customer snapshot fields are needed before full Storefront migration.
- [ ] Keep `Order.AdminNote` max length.

API:

- [ ] Migrate `AdminOrdersController`.
- [ ] Do not migrate any `CartController` action in Admin-first.
- [ ] Treat legacy `CartController` Admin-only actions as Storefront/shared-auth leftovers unless a later review proves they are still needed.

Services:

- [ ] Adapt `AdminOrderService` to `CommerceNodeDbContext`.
- [ ] Adapt `OrderTrackingService` to `CommerceNodeDbContext`.
- [ ] Remove direct query dependency on legacy `_db.Users`.
- [ ] Decide whether email notification side effects remain enabled in Commerce Node MVP.

QA:

- [ ] List orders.
- [ ] View order detail.
- [ ] Update tracking.
- [ ] Update shipping status.
- [ ] Update admin note.
- [ ] Verify invalid status returns `success=false` and message.
- [ ] Verify audit logs.

Stop gate: Admin order management no longer depends on `AppDbContext`.

## Phase 6 - Admin Settings And Media Upload

Database:

- [ ] Use `AdminSettings` in `CommerceNodeDbContext`.

API:

- [ ] Migrate Admin settings get/update store.
- [ ] Migrate Admin settings update orders.
- [ ] Migrate Admin settings update notifications.
- [ ] Migrate image upload endpoint as `api/commerce/admin/media/images`.

Services:

- [ ] Adapt `AdminSettingsService` to `CommerceNodeDbContext`.
- [ ] Review runtime/system metadata fields for Commerce Node.
- [ ] Keep file upload local to node for MVP unless a storage abstraction is required.

QA:

- [ ] Read settings.
- [ ] Update store settings.
- [ ] Update order settings.
- [ ] Update notification settings.
- [ ] Upload image.
- [ ] Verify uploaded image URL is reachable from Commerce Node.
- [ ] Verify audit logs.

## Phase 7 - Audit And Metrics

Database:

- [ ] Use `AdminAuditLogs`.
- [ ] Use `Orders` for sales metrics.
- [ ] Use `NewsletterSubscribers` for current traffic proxy.

API:

- [ ] Migrate Admin audit list/detail.
- [ ] Migrate metrics sales.
- [ ] Migrate metrics traffic.

Services:

- [ ] Adapt `AdminAuditService` to `CommerceNodeDbContext`.
- [ ] Reuse `MetricsService` with Commerce Node repositories.
- [ ] Confirm whether newsletter count is still acceptable as `traffic`.

QA:

- [ ] Search audit by actor.
- [ ] Search audit by action.
- [ ] Search audit by entity.
- [ ] Sales metrics return expected date buckets.
- [ ] Traffic metrics return expected date buckets.

## Phase 8 - Storefront Internal API Inventory

This phase is documented now to avoid missing business, but implementation comes after Admin migration review.

Database:

- [ ] Use catalog, SEO, recommendations, newsletter, payment, cart, and order tables already in Commerce Node.
- [ ] Move Storefront auth tables/logic into Commerce Node database scope without pulling Control Plane auth.
- [ ] Adapt existing Storefront auth logic to `CommerceNodeDbContext` or a Commerce Node auth context backed by the Commerce Node database.
- [ ] Decide Storefront customer model before moving user-specific cart/order routes.

API:

- [ ] Migrate `AuthenticationController.CreateUser` to `api/internal/auth/create`.
- [ ] Migrate `AuthenticationController.LoginUser` to `api/internal/auth/login`.
- [ ] Migrate `AuthenticationController.RefreshToken` to `api/internal/auth/refresh-token`.
- [ ] Migrate `AuthenticationController.Logout` to `api/internal/auth/logout`.
- [ ] Migrate `AuthenticationController.ChangePassword` to `api/internal/auth/change-password`.
- [ ] Migrate `AuthenticationController.ConfirmEmail` to `api/internal/auth/confirm-email`.
- [ ] Migrate `AuthenticationController.UpdateProfile` to `api/internal/auth/update-profile`.
- [ ] Migrate public catalog reads to `api/internal/catalog/*`.
- [ ] Migrate public SEO reads to `api/internal/seo/*`.
- [ ] Migrate product recommendations.
- [ ] Migrate payment methods.
- [ ] Migrate newsletter subscribe.
- [ ] Migrate `CartController.Checkout`.
- [ ] Migrate `CartController.SaveCheckout`.
- [ ] Migrate `CartController.ConfirmOrder`.
- [ ] Migrate `CartController.GetUserOrderItems`.
- [ ] Migrate `CartController.GetUserOrders`.
- [ ] Review `CartController.GetAllCheckoutHistory`, `GetAllOrders`, `UpdateTracking`, and `UpdateShippingStatus`; migrate under Storefront/internal only if still used.

Services:

- [ ] Reuse `AuthenticationService` logic where possible; change only dependencies required for Commerce Node database/auth.
- [ ] Reuse `PublicCatalogService`.
- [ ] Reuse `SeoRedirectResolutionService`.
- [ ] Reuse `ProductRecommendationService`.
- [ ] Reuse `PaymentMethodService`.
- [ ] Reuse `NewsletterService`.
- [ ] Reuse `CartService` logic where possible; change only dependencies required for Commerce Node auth/database.

QA:

- [ ] Storefront register/login/refresh/logout.
- [ ] Storefront change password/update profile.
- [ ] Catalog categories.
- [ ] Catalog product list/detail.
- [ ] Catalog slug routes.
- [ ] Sitemap.
- [ ] SEO settings.
- [ ] SEO redirect resolution.
- [ ] Recommendations.
- [ ] Payment methods.
- [ ] Newsletter subscribe.
- [ ] Cart checkout and order history.

## Phase 9 - Control Plane Integration And Legacy Cutover

- [ ] Add Control Plane client methods for Commerce Node Admin routes.
- [ ] Wire Control Plane UI to call Commerce Node for selected node.
- [ ] Keep legacy Admin UI/API available until parity is verified.
- [ ] Add QA checklist for node selection + admin operations.
- [ ] Compare results between legacy API and Commerce Node API for same seeded data.
- [ ] Mark legacy routes as deprecated only after review.
- [ ] Remove legacy Presentation only in a separate approved cutover phase.

## Service Migration Notes

| Service | Current Dependency Risk | Commerce Node Action |
|---|---|---|
| `ProductService` | Repository bindings use `AppDbContext`. | Reuse service with Commerce Node repository bindings. |
| `CategoryService` | Repository bindings use `AppDbContext`. | Reuse service with Commerce Node repository bindings. |
| `ProductVariantService` | Generic repository uses `AppDbContext`. | Reuse with Commerce Node generic repository. |
| `AdminInventoryService` | Direct `AppDbContext`. | Add Commerce Node implementation or refactor constructor to accept a commerce data abstraction. |
| `ProductSeoService` | Repositories + transaction manager use `AppDbContext`. | Reuse with Commerce Node repository/transaction bindings. |
| `CategorySeoService` | Repositories + transaction manager use `AppDbContext`. | Reuse with Commerce Node repository/transaction bindings. |
| `SeoSettingsService` | Repositories use `AppDbContext`. | Reuse with Commerce Node repositories. |
| `SeoRedirectService` | Repositories use `AppDbContext`. | Reuse with Commerce Node repositories. |
| `AdminOrderService` | Direct `AppDbContext`, queries `Users`. | Needs Commerce Node implementation and user-data decision. |
| `OrderTrackingService` | Direct `AppDbContext`, email side effects. | Needs Commerce Node implementation and side-effect review. |
| `AdminSettingsService` | Direct `AppDbContext`, host/env/email options. | Needs Commerce Node implementation or context abstraction. |
| `AdminAuditService` | Direct `AppDbContext`, reads JWT user claims. | Needs Commerce Node implementation reading Control Plane actor headers. |
| `MetricsService` | Repository based. | Reuse with Commerce Node repositories. |
| `AdminUserService` | Identity `UserManager`, `RoleManager`, `AppUser`. | Do not migrate. Control Plane replaces it. |
| `AuthenticationService` | Identity/JWT/refresh token. | Migrate in Commerce Node Storefront auth phase; preserve logic and adapt dependencies to Commerce Node database/auth scope. |
| `CartService` | Uses `IAppUserManager`, orders, cart, payment. | Migrate in Storefront/internal phase after Commerce Node Storefront auth is available. |
| `OrderQueryService` | Uses `IAppUserManager`. | Admin order listing should not reuse as-is; Storefront order history can reuse after auth dependency is adapted. |

## Open Questions To Close

- Should Control Plane admin operations use `api/commerce/admin/*` as recommended, or direct `api/commerce/*` without the `admin` segment?
- Should Commerce Node reuse entity names `AdminAuditLog` and `AdminSettings`, or rename them later to `CommerceNodeAuditLog` and `CommerceNodeSettings`?
- For Admin Orders, is it acceptable in MVP to return `UserId` without customer display fields until Commerce Node Storefront auth/customer migration?
- Should Commerce Node send order tracking emails in MVP, or should email side effects stay disabled until settings are verified?
- Is local node filesystem acceptable for image upload in MVP?
- Should `api/internal/*` use one shared internal key, per-storefront key, or IP allowlist only?
- Should `AuthenticationController` keep route prefix `api/internal/auth/*`, or should Storefront auth use a separate public-facing prefix behind the Storefront gateway?
- Should legacy `CartController` Admin-only actions be migrated under Storefront/internal if still used, or dropped as unused legacy admin shortcuts?

## Review Checklist

- [ ] No `AppDbContext` dependency is introduced into Commerce Node.
- [ ] No Identity table is added to Commerce Node database.
- [ ] Admin and Storefront routes are split even when the legacy controller is mixed.
- [ ] Admin-first phases are complete before Storefront internal migration.
- [ ] Every legacy controller has a target decision.
- [ ] Every Commerce Node migrated endpoint keeps response envelope consistency.
- [ ] Legacy API remains untouched until explicit cutover.
