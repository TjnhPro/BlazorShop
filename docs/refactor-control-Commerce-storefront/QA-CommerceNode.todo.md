# QA Commerce Node Todo

## Scope

QA verifies `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API` through HTTP against a local PostgreSQL container.

Database target:

- Container: `blazorshop-commercenode-postgres`
- Compose file: `compose.commercenode.yml`
- Host port: `5434`
- Database: `blazorshop_commerce_node`
- User: `blazorshop_commerce_node`

Last verified: 2026-07-10

## Environment Checklist

- [x] Start `blazorshop-commercenode-postgres` on port `5434`.
- [x] Apply `CommerceNodeDbContext` migrations.
- [x] Start `BlazorShop.CommerceNode.API` in Development.
- [x] Verify API base URL: `http://localhost:5180`.
- [x] Verify Swagger/API host is reachable.
- [x] Verify response envelope shape: `success`, `message`, `data`.

## Credential Boundary

### `api/commerce/*`

- [x] Missing `X-Node-Key` / `X-Node-Secret` returns `success=false`.
- [x] Invalid node credential returns `success=false`.
- [x] Valid node credential allows `api/commerce/healthz`.
- [x] Valid node credential allows `api/commerce/admin/*`.

### `api/internal/*`

- [x] Internal catalog routes do not require node key.
- [x] Internal SEO routes do not require node key.
- [x] Internal auth create/login/refresh/logout do not require node key.
- [x] Customer routes require JWT:
  - [x] `api/internal/cart/checkout`
  - [x] `api/internal/cart/save-checkout`
  - [x] `api/internal/orders/confirm`
  - [x] `api/internal/orders/current-user`
  - [x] `api/internal/orders/current-user/items`

## Seed Data

- [x] Create admin seed category through `api/commerce/admin/categories`.
- [x] Create admin seed product through `api/commerce/admin/products`.
- [x] Create product variant through `api/commerce/admin/products/{productId}/variants`.
- [x] Update product SEO through `api/commerce/admin/products/{id}/seo`.
- [x] Update category SEO through `api/commerce/admin/categories/{id}/seo`.
- [x] Update global SEO settings through `api/commerce/admin/seo/settings`.
- [x] Create SEO redirect through `api/commerce/admin/seo/redirects`.
- [x] Register Storefront customer through `api/internal/auth/create`.
- [x] Login Storefront customer through `api/internal/auth/login`.

## Commerce Admin API Checklist

### Health

- [x] `GET /api/commerce/healthz`

### Categories

- [x] `GET /api/commerce/admin/categories`
- [x] `POST /api/commerce/admin/categories`
- [x] `GET /api/commerce/admin/categories/{id}`
- [x] `PUT /api/commerce/admin/categories/{id}`
- [x] `DELETE /api/commerce/admin/categories/{id}` with disposable category only.

### Products

- [x] `GET /api/commerce/admin/products`
- [x] `POST /api/commerce/admin/products`
- [x] `GET /api/commerce/admin/products/{id}`
- [x] `PUT /api/commerce/admin/products/{id}`
- [x] `DELETE /api/commerce/admin/products/{id}` with disposable product only.

### Product Variants

- [x] `GET /api/commerce/admin/products/{productId}/variants`
- [x] `POST /api/commerce/admin/products/{productId}/variants`
- [x] `PUT /api/commerce/admin/products/{productId}/variants/{variantId}`
- [x] `DELETE /api/commerce/admin/products/{productId}/variants/{variantId}` with disposable variant only.

### Product Media

- [~] `GET /api/commerce/admin/products/{productId}/media` returns an empty list before import. Existing QA DB already contained media rows after this run; list endpoint itself verified.
- [x] `POST /api/commerce/admin/products/{productId}/media/import` queues a valid public image URL. 2026-07-10: queued `picsum.photos` image.
- [x] `POST /api/commerce/admin/products/{productId}/media/import` rejects unsupported URL schemes. 2026-07-10: `ftp://` returned `success=false`.
- [x] `POST /api/commerce/admin/products/{productId}/media/import` blocks localhost/private IP source URLs. 2026-07-10: `127.0.0.1` source failed with safe private/local host message.
- [x] Media task transitions imported image to `stored`. 2026-07-10: retry after storage fix stored media `973caf94-14c9-4c12-9376-7101d17e061a`.
- [x] Failed media records a safe error message. 2026-07-10: failed source returned safe unsuccessful/private-host messages.
- [x] Primary stored media updates `Product.Image` to `/media/products/{mediaPublicId}`. 2026-07-10: admin product detail returned Product.Image media URL.
- [x] `GET /media/products/{mediaPublicId}` returns optimized image content. 2026-07-10: public resolver returned `image/webp` through imgproxy when store scope was provided.
- [x] `GET /media/products/{mediaPublicId}?w=320&fit=contain&format=webp` returns image content. 2026-07-10: returned 200, `image/webp`, immutable cache headers.
- [x] `GET /media/products/{mediaPublicId}?w=3000` rejects or clamps invalid dimensions so rendered output never exceeds `2000`. 2026-07-10: controller clamps to max `2000`.
- [x] Set primary media succeeds. 2026-07-10: imported primary became stored primary.
- [x] Retry failed media succeeds when the media is retryable. 2026-07-10: retry converted failed storage row to stored after fix.
- [ ] Delete primary media chooses next stored image or clears `Product.Image`.
- [x] Store-scoped media cannot be read from another store host. 2026-07-10: `X-Store-Key: other` returned 404 for default store media.

### Inventory

- [x] `GET /api/commerce/admin/inventory`
- [x] `PUT /api/commerce/admin/inventory/products/{productId}`
- [x] `PUT /api/commerce/admin/inventory/variants/{variantId}`

### SEO

- [x] `GET /api/commerce/admin/products/{id}/seo`
- [x] `PUT /api/commerce/admin/products/{id}/seo`
- [x] `GET /api/commerce/admin/categories/{id}/seo`
- [x] `PUT /api/commerce/admin/categories/{id}/seo`
- [x] `GET /api/commerce/admin/seo/settings`
- [x] `PUT /api/commerce/admin/seo/settings`
- [x] `GET /api/commerce/admin/seo/redirects`
- [x] `POST /api/commerce/admin/seo/redirects`
- [x] `GET /api/commerce/admin/seo/redirects/{id}`
- [x] `PUT /api/commerce/admin/seo/redirects/{id}`
- [x] `POST /api/commerce/admin/seo/redirects/{id}/deactivate`
- [x] `DELETE /api/commerce/admin/seo/redirects/{id}` with disposable redirect only.

### Orders

- [x] `GET /api/commerce/admin/orders`
- [x] `GET /api/commerce/admin/orders/{id}` after creating an order through Storefront flow.
- [x] `PUT /api/commerce/admin/orders/{id}/tracking`
- [x] `PUT /api/commerce/admin/orders/{id}/shipping-status`
- [x] `PUT /api/commerce/admin/orders/{id}/admin-note`

### Settings, Audit, Metrics

- [x] `GET /api/commerce/admin/settings`
- [x] `PUT /api/commerce/admin/settings/store`
- [x] `PUT /api/commerce/admin/settings/orders`
- [x] `PUT /api/commerce/admin/settings/notifications`
- [x] `GET /api/commerce/admin/audit`
- [x] `GET /api/commerce/admin/audit/{id}` if audit entries exist.
- [x] `GET /api/commerce/admin/metrics/sales`
- [x] `GET /api/commerce/admin/metrics/traffic`

## Storefront Internal API Checklist

### Catalog

- [x] `GET /api/internal/catalog/categories`
- [x] `GET /api/internal/catalog/categories/tree` returns parent/child tree. 2026-07-10: endpoint added; admin tree smoke verified same hierarchy.
- [x] `GET /api/internal/catalog/categories/{id}`
- [x] `GET /api/internal/catalog/categories/slug/{slug}`
- [x] `GET /api/internal/catalog/categories/{categoryId}/products`
- [x] `GET /api/internal/catalog/products`
- [x] `GET /api/internal/catalog/products?minPrice=&maxPrice=&inStock=&sortBy=DisplayOrder` filters expanded catalog. 2026-07-10: smoke returned filtered product page.
- [x] `GET /api/internal/catalog/products/{id}`
- [x] `GET /api/internal/catalog/products/slug/{slug}`
- [x] `GET /api/internal/catalog/sitemap`

### Catalog Expansion

- [x] Development seeder creates `default` store if missing. 2026-07-10: verified in DB.
- [x] Development seeder creates parent category `Apparel`. 2026-07-10: seeder added.
- [x] Development seeder creates child category `T-Shirts`. 2026-07-10: seeder added.
- [x] Development seeder creates product `QA-TSHIRT` with SKU, short/full description, compare price, display order. 2026-07-10: verified in DB.
- [x] Development seeder creates variant `Color=Red, Size=M`. 2026-07-10: verified in DB.
- [x] Development seeder creates variant `Color=Red, Size=XL`. 2026-07-10: verified in DB.
- [x] Development seeder creates variant `Color=Black, Size=M` with zero stock. 2026-07-10: verified in DB.
- [x] Development seeder creates low-stock product `QA-LOW-STOCK`. 2026-07-10: verified in DB.
- [x] Development seeder creates sample order `QA-CATALOG-SNAPSHOT` with product/variant snapshot fields. 2026-07-10: verified in DB.
- [x] `GET /api/commerce/admin/products/query` searches by SKU. 2026-07-10: `searchTerm=QA-TSHIRT` returned seeded product.
- [x] `GET /api/commerce/admin/categories/tree` returns category hierarchy. 2026-07-10: returned `Apparel -> T-Shirts`.
- [ ] Duplicate variant combination is rejected.
- [ ] Second default variant is rejected.
- [ ] Checkout with out-of-stock variant is rejected.
- [ ] Successful order deducts product/variant stock.
- [ ] Admin order detail prefers order line snapshot fields.

### SEO

- [x] `GET /api/internal/seo/settings`
- [x] `GET /api/internal/seo/redirects/resolve?path=/legacy-qa-product`

### Auth

- [x] `POST /api/internal/auth/create`
- [x] `POST /api/internal/auth/login`
- [x] Login wrong password returns `success=false`.
- [x] `POST /api/internal/auth/refresh-token` with login cookie.
- [x] `POST /api/internal/auth/change-password` with JWT.
- [x] `POST /api/internal/auth/update-profile` with JWT.
- [x] `POST /api/internal/auth/logout` revokes refresh cookie.

### Payments, Newsletter, Recommendations

- [x] `GET /api/internal/payments/methods`
- [x] `GET /api/internal/recommendations/products/{productId}`
- [x] `POST /api/internal/newsletter/subscribe`
- [x] Duplicate newsletter subscription returns a stable response.

### Cart And Orders

- [x] Unauthenticated cart/order routes reject missing JWT.
- [x] `POST /api/internal/cart/checkout` with Cash on Delivery.
- [x] `POST /api/internal/cart/save-checkout`.
- [x] `POST /api/internal/orders/confirm`.
- [x] `GET /api/internal/orders/current-user`.
- [x] `GET /api/internal/orders/current-user/items`.
- [x] Admin order list sees the created order.

## Deferred/Manual Checks

- [x] `POST /api/commerce/admin/media/images` with multipart image upload. 2026-07-09: curl multipart upload returned `success=true`; uploaded PNG was reachable under `/uploads`.
- [n/a] PayPal capture redirect, because current PayPal service is a stub in this MVP.
- [n/a] Email delivery for newsletter/bank transfer, because SMTP is not configured for local MVP QA.

## QA Notes

- Use `X-Node-Key: dev-node`.
- Use `X-Node-Secret: dev-node-secret`.
- Customer JWT comes from `api/internal/auth/login` response `data.token`.
- Refresh token is stored in `Set-Cookie` from login.
- Local HTTP clients may not resend the refresh cookie automatically because it is `Secure=true`; QA verified refresh by replaying the `Set-Cookie` value as a `Cookie` header.
- Missing JWT on `[Authorize]` Storefront routes returns HTTP `401` from ASP.NET auth middleware, with an empty body.
- `api/internal/cart/save-checkout` and `api/internal/orders/confirm` expect a top-level JSON array. When using PowerShell, send raw JSON for single-item arrays to avoid `ConvertTo-Json` collapsing the array.
- `api/internal/cart/save-checkout` currently also requires a `userId` field in the request body even though the authenticated customer is resolved from JWT; QA supplied a harmless placeholder. This is a cleanup candidate for the later Storefront auth contract pass.
- `api/internal/recommendations/products/{productId}` requires at least one related published product; a single product in a category correctly returns a not-found response.

## Fixes Applied During QA

- Fixed `GET /api/commerce/admin/products/{id}` serialization cycle by preventing `Category.Products` from being mapped back into `GetProduct.Category`.
- Fixed Commerce Node transaction execution with PostgreSQL retry strategy by wrapping manual transactions in `Database.CreateExecutionStrategy()`.

## Verification Commands

- `docker compose -f compose.commercenode.yml up -d`
- `dotnet ef database update --project BlazorShop.Infrastructure/BlazorShop.Infrastructure.csproj --startup-project BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --context CommerceNodeDbContext`
- `dotnet test BlazorShop.sln`

Latest ProductMedia QA result: 2026-07-10 CommerceNode API smoke passed for import queue, retry, worker storage, Product.Image sync, public imgproxy rendering, invalid scheme rejection, private/local source blocking, and cross-store 404. Fixed EF projection and temp-file length bugs found during QA.

Latest test result: 2026-07-09 full solution test passed: 485 passed, 10 skipped. Independent API smoke passed for ControlPlane -> CommerceNode health probe, Commerce admin catalog/media, Storefront internal auth/cart/order, and admin order visibility.
