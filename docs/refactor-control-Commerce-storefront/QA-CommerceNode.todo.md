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
- [x] Apply `CommerceNodeDbContext` migrations through CommerceNode API startup.
- [x] Start `BlazorShop.CommerceNode.API` in Development.
- [x] Verify API base URL: `http://localhost:5180`.
- [x] Verify Swagger/API host is reachable.
- [x] Verify response envelope shape: `success`, `message`, `data`.

## Startup Database Migration

- [x] Clean `CommerceNodeConnection` database is created/migrated by `BlazorShop.CommerceNode.API` startup when `CommerceNode:Database:MigrateOnStartup=true`. 2026-07-11: startup smoke passed against disposable DB `blazorshop_commerce_node_startup_qa_20260711`.
- [ ] Existing migrated Commerce Node database restarts without duplicate migration or seed side effects.
- [x] Startup migration logs context name, connection name, applied count, pending count, and pending migration names. 2026-07-11: verified in `.gstack/startup-migration-qa/commercenode-startup-migration.log`.
- [x] Startup migration logs do not expose passwords or raw connection strings. 2026-07-11: smoke assertion checked logs did not contain `Password=`.
- [ ] Invalid `CommerceNodeConnection` fails API startup when `CommerceNode:Database:FailStartupOnMigrationError=true`.
- [ ] `CommerceNode:Database:LogMigrationState=false` still runs migration without state log noise.
- [x] `CommerceNodeDbContext` startup migration never touches `ControlPlaneConnection` or `AppDbContext`. 2026-07-11: smoke used only `ConnectionStrings__CommerceNodeConnection`; architecture/code path resolves only `CommerceNodeDbContext`.
- [x] `CommerceTaskWorker` starts only after startup migration completes. 2026-07-11: migration runs before `app.Run()` and smoke reached `api/commerce/healthz` after startup completed.

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

### Variation Templates

- [x] CommerceNode API builds after Variation Template Foundation changes. 2026-07-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- [x] ControlPlane API builds after product import proxy changes. 2026-07-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore` passed.
- [x] Apply `CommerceNodeVariationTemplateFoundation` migration to clean CommerceNode PostgreSQL on port `5434`. 2026-07-10: `dotnet ef database update --context CommerceNodeDbContext` applied pending migrations.
- [x] `GET /api/commerce/admin/variation-templates` returns list. 2026-07-10: list returned created QA templates.
- [x] `POST /api/commerce/admin/variation-templates` creates template. 2026-07-10: created `qa-template-20260710222346`.
- [x] Duplicate template slug in same store returns `success=false`. 2026-07-10: duplicate slug returned HTTP 409 envelope.
- [ ] Same template slug in another store is allowed.
- [x] `GET /api/commerce/admin/variation-templates/{id}` returns options/values. 2026-07-10: returned `Color -> Red`.
- [ ] `PUT /api/commerce/admin/variation-templates/{id}` updates name/slug/active state.
- [x] `POST /api/commerce/admin/variation-templates/{id}/options` creates option. 2026-07-10: fixed EF child-row state bug; `Color` option created.
- [ ] `PUT /api/commerce/admin/variation-templates/{id}/options/{optionId}` updates/disables option.
- [x] `POST /api/commerce/admin/variation-templates/{id}/options/{optionId}/values` creates value. 2026-07-10: fixed EF child-row state bug; `Red` and disabled `Blue` values created.
- [x] `PUT /api/commerce/admin/variation-templates/{id}/options/{optionId}/values/{valueId}` updates/disables value. 2026-07-10: `Red` disabled then re-enabled successfully.
- [x] Delete unreferenced variation template succeeds. 2026-07-10: disposable template deleted.
- [x] Delete referenced variation template fails. 2026-07-10: template referenced by product returned HTTP 409 envelope.
- [x] Create `CustomVariations` product without template fails. 2026-07-10: returned validation error.
- [x] Create `CustomVariations` product with active template succeeds. 2026-07-10: admin create succeeded with active template id.
- [x] Storefront product detail for `CustomVariations` returns active option/value `name` and `value` only. 2026-07-10: internal catalog product detail returned `Color -> Red`.
- [x] Disabled option/value is hidden from Storefront product detail. 2026-07-10: disabled `Blue` was not returned by Storefront detail.
- [ ] Cart/order accepts selected attributes for `CustomVariations`.
- [ ] Cart/order rejects more than 5 selected attributes.
- [ ] Cart/order stores selected attributes in `OrderLine.VariantAttributesJson`.
- [ ] Existing `ProductVariant` endpoints still work for `VariantInventory`.

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

### Media Library

- [x] CommerceNode API builds after Media Library MVP backend changes. 2026-07-13: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj` passed.
- [x] `CommerceNodeMediaLibraryMvp` migration was generated for `CommerceNodeDbContext`. 2026-07-13: `dotnet ef migrations add CommerceNodeMediaLibraryMvp` succeeded after stopping locked local dev processes.
- [x] Apply `CommerceNodeMediaLibraryMvp` migration to local CommerceNode PostgreSQL on port `5434`. 2026-07-13: local CommerceNode startup migration applied; browser upload/list used the new table.
- [x] `GET /api/commerce/admin/media/assets?pageNumber=1&pageSize=25` returns paged media assets for current store. 2026-07-13: ControlPlane browser list loaded page 1 with one uploaded asset, then empty state after delete.
- [~] `POST /api/commerce/admin/media/assets` uploads jpg/png/webp/gif/ico up to 10MB. 2026-07-13: PNG upload path verified; remaining supported formats still pending.
- [x] Upload auto-generates `displayName`, `altText`, and `titleText` from file name. 2026-07-13: `summer-sale-banner.png` generated `Summer Sale Banner` for all three fields.
- [x] Upload rejects unsupported extensions and mismatched file signatures. 2026-07-13: `.txt` upload returned validation error through the visible ControlPlane page.
- [x] `PUT /api/commerce/admin/media/assets/{assetPublicId}` updates metadata and bumps datetime version. 2026-07-13: metadata save updated generated link version and drawer values.
- [ ] Blank display name is rejected.
- [ ] Blank alt text falls back to display name.
- [x] `POST /api/commerce/admin/media/assets/{assetPublicId}/replace` replaces original bytes while keeping the public id and canonical file name. 2026-07-13: replacement kept the same public id and `summer-sale-banner.png`.
- [x] `DELETE /api/commerce/admin/media/assets/{assetPublicId}` hard-deletes the row and asset directory. 2026-07-13: visible browser delete returned the grid to the empty state.
- [ ] `GET /media/assets/{assetPublicId}/{canonicalFileName}` serves original content with store scope.
- [ ] Wrong canonical filename redirects permanently to the canonical URL while preserving query.
- [~] Transform query supports `w`, `h`, `fit=cover|contain|inside`, `format=original|webp|jpg|png`, and `v`. 2026-07-13: `w=320&h=180&fit=cover&format=webp&v=...` returned `200 image/webp`; other fit/format combinations still pending.
- [ ] Transform query for gif/ico returns 400.
- [ ] Store A cannot read Store B media asset.

### Product Import

- [x] CommerceNode API builds after Product Import Task changes. 2026-07-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- [x] ControlPlane API product import proxy builds. 2026-07-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore` passed.
- [x] Apply `CommerceNodeProductImport` migration to clean CommerceNode PostgreSQL on port `5434`. 2026-07-10: migration applied by `dotnet ef database update`.
- [x] Apply `CommerceNodeNullableProductCategory` migration to clean CommerceNode PostgreSQL on port `5434`. 2026-07-10: migration applied by `dotnet ef database update`.
- [x] `POST /api/commerce/admin/products/import` uploads valid CSV in `create_only` mode. 2026-07-10: job `8c004bac-546a-40bb-b8aa-41c478988e03` completed with 2 created rows.
- [x] `POST /api/commerce/admin/products/import` uploads valid CSV in `upsert` mode. 2026-07-10: job `014eb127-9827-410c-af5a-c9a8756d6e23` completed with 1 updated row.
- [x] `GET /api/commerce/admin/products/imports` lists import jobs. 2026-07-10: returned latest import jobs with counts and statuses.
- [x] `GET /api/commerce/admin/products/imports/{jobPublicId}` returns job detail. 2026-07-10: detail polling used until jobs reached terminal status.
- [x] `GET /api/commerce/admin/products/imports/{jobPublicId}/rows` returns row results. 2026-07-10: row list returned status/action/ErrorJson/media status.
- [x] Duplicate same file hash returns existing job and does not enqueue another task. 2026-07-10: duplicate upload returned the same job public id.
- [x] Same file cannot be imported again for same store/mode. 2026-07-10: same file hash/mode/store returned existing job.
- [x] Missing required CSV header fails the async import job. 2026-07-10: upload accepted the file, worker marked job `Failed` with missing header details.
- [x] Missing SKU row writes `sku` column error. 2026-07-10: row ErrorJson contained `sku`.
- [x] Missing name on create writes `name` column error. 2026-07-10: row ErrorJson contained `name`.
- [x] Missing description on create writes `description` column error. 2026-07-10: row ErrorJson contained `description`.
- [x] Missing price on create writes `price` column error. 2026-07-10: row ErrorJson contained `price`.
- [ ] Duplicate SKU in `create_only` writes row error.
- [x] `upsert` blank cells do not overwrite existing values. 2026-07-10: upsert blank `short_description` kept existing `Import simple`.
- [x] `__clear__` clears allowed nullable fields. 2026-07-10: upsert `compare_price=__clear__` returned product `comparePrice=null`.
- [ ] Create with blank `category_slug` succeeds and leaves product uncategorized.
- [x] Update with blank `category_slug` keeps existing category. 2026-07-10: upsert blank category kept `t-shirts`.
- [x] Unknown `category_slug` writes row error. 2026-07-10: row ErrorJson contained `category_slug`.
- [x] `CustomVariations` without `variation_template_slug` writes row error. 2026-07-10: row ErrorJson contained `variation_template_slug`.
- [ ] Unknown/inactive `variation_template_slug` writes row error.
- [x] Valid `variation_template_slug` sets product template reference. 2026-07-10: Storefront detail for imported custom product returned the expected `variationTemplateId`.
- [ ] `VariantInventory` import does not create `ProductVariant` rows.
- [x] `image_urls` with more than 10 URLs writes row error. 2026-07-10: row ErrorJson contained `image_urls`.
- [x] Valid `image_urls` queues one `product.media.import` task per product row. 2026-07-10: fixed background store scope; import row returned `mediaStatus=Queued`, media task succeeded.
- [x] Product import completes with `CompletedWithErrors` when some rows fail. 2026-07-10: row-errors job completed with 6 failed rows.
- [x] CommerceTask result contains product import summary counts. 2026-07-10: import task detail/list exposed created/updated/failed/media counts through job and task correlation.
- [x] ProductImportRows contain `ErrorJson` with column names. 2026-07-10: row ErrorJson included `sku`, `name`, `description`, `price`, `category_slug`, `variation_template_slug`, and `image_urls`.
- [ ] ControlPlane upload route `POST /api/control-plane/stores/{storePublicId}/catalog/products/import` proxies through ControlPlane API only.
- [ ] ControlPlane import list/detail/rows routes proxy through ControlPlane API only.

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

### Shipments

- [x] CommerceNode API builds after shipment foundation changes. 2026-07-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- [ ] Apply `CommerceNodeShipments` migration to clean CommerceNode PostgreSQL on port `5434`.
- [ ] `GET /api/commerce/admin/orders/{orderId}/shipment` returns `success=false`/not found before shipment exists.
- [ ] `PUT /api/commerce/admin/orders/{orderId}/shipment` creates a shipment for an existing store-scoped order.
- [ ] `GET /api/commerce/admin/orders/{orderId}/shipment` returns created shipment data.
- [ ] Second `PUT /api/commerce/admin/orders/{orderId}/shipment` updates/replaces the existing shipment instead of creating a duplicate.
- [ ] Database unique index `(StoreId, OrderId)` prevents duplicate shipment rows.
- [ ] Shipment create/update syncs order fields:
  - [ ] `ShippingStatus = Shipped`
  - [ ] `ShippedOn = ShipDate`
  - [ ] `ShippingCarrier = CarrierName`
  - [ ] `TrackingNumber = TrackingNumber`
  - [ ] `TrackingUrl = TrackingUrl`
  - [ ] `LastTrackingUpdate` is updated.
- [ ] Shipment request with empty `CarrierName` returns `success=false`.
- [ ] Shipment request with empty `TrackingNumber` returns `success=false`.
- [ ] Shipment request with over-length text fields returns `success=false`.
- [ ] Store isolation: a request scoped to another store cannot read the shipment.
- [ ] Store isolation: a request scoped to another store cannot update the shipment.
- [ ] Audit log includes `Order.ShipmentUpserted` after shipment upsert.
- [ ] Existing `PUT /api/commerce/admin/orders/{orderId}/tracking` still works after shipment migration.
- [ ] Existing `PUT /api/commerce/admin/orders/{orderId}/shipping-status` still works after shipment migration.
- [ ] Storefront order detail still reads shipping info from existing order fields.
- [ ] No new Storefront shipment endpoint is exposed under `api/internal/*`.

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

### Catalog Search MVP

- [x] CommerceNode API builds after catalog search/cache changes. 2026-07-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- [x] Legacy `BlazorShop.API` still builds after optional catalog cache dependencies were added. 2026-07-10: `dotnet build BlazorShop.Presentation/BlazorShop.API/BlazorShop.API.csproj --no-restore` passed with existing `Microsoft.OpenApi` advisory warning.
- [~] Full test suite attempted after CommerceNode catalog search/cache changes. 2026-07-10: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore` failed 11/512. Failures were in migration model consistency, existing Product/Category delete unit expectations, CartService tests, and sitemap timestamp tests; CommerceNode API and StorefrontV2 builds passed.
- [x] Storefront published catalog search uses PostgreSQL FTS over `Products.Name`, not `Contains` over SKU/description. 2026-07-10: code review verified `CommerceNodeProductReadRepository.GetPublishedCatalogPageAsync`.
- [x] Migration adds `ix_products_name_fts_simple` GIN expression index. 2026-07-10: migration `20260710120000_CommerceNodeCatalogSearchMvp` added.
- [ ] Apply `CommerceNodeCatalogSearchMvp` migration to local PostgreSQL on port `5434`.
- [ ] `GET /api/internal/catalog/products?searchTerm=shirt` returns title matches.
- [ ] `GET /api/internal/catalog/products?searchTerm=<sku-only-term>` does not match SKU-only text.
- [ ] `GET /api/internal/catalog/products?searchTerm=<description-only-term>` does not match description-only text.
- [ ] `GET /api/internal/catalog/products?categorySlug=t-shirts` returns category-scoped products.
- [ ] `GET /api/internal/catalog/products?categorySlug=apparel` includes child category products.
- [ ] `GET /api/internal/catalog/products?categorySlug=missing-category` returns `success=true` with empty page data.
- [ ] `GET /api/internal/catalog/products?categorySlug=t-shirts&searchTerm=shirt` combines category and title search.
- [ ] Empty `searchTerm` does not use FTS and returns browse listing.
- [ ] `pageNumber` greater than 10 is clamped by backend.
- [ ] `TotalCount` is capped to `PageSize * 10`.
- [ ] Catalog query cache returns stable repeated results.
- [ ] Product create/update/delete invalidates store catalog cache.
- [ ] Category create/update/delete invalidates store catalog cache.
- [ ] Variant create/update/delete invalidates store catalog cache.
- [ ] Inventory stock updates invalidate store catalog cache.
- [ ] Primary product media change invalidates store catalog cache.

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

### Storefront Pages

- [x] CommerceNode API builds after StorefrontPage schema/service/API changes. 2026-07-11: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- [x] Apply `CommerceNodeStorefrontPage` migration to CommerceNode PostgreSQL on port `5434`. 2026-07-12: `run-v2-local.ps1` startup migration applied `20260711155908_CommerceNodeStorefrontPage` to local `blazorshop_commerce_node_v2_local`; `storefront_page` table exists.
- [x] `GET /api/commerce/admin/pages?pageNumber=1&pageSize=25` returns paged response. 2026-07-12: direct API with node credentials returned `success=true`, `pageNumber=1`, `pageSize=25`, and `totalPages=1`.
- [x] Admin page list search matches title. 2026-07-12: `search=Dynamic` returned `qa-dynamic-page-20260712034014`.
- [x] Admin page list search matches slug. 2026-07-12: `search=qa-dynamic-page` returned `qa-dynamic-page-20260712034014`.
- [x] Status filter `all` includes draft and published non-archived pages. 2026-07-12: ControlPlane/CommerceNode list showed both `QA Dynamic Page 20260712034014` and `QA Draft Page 20260712034014`.
- [x] Status filter `published` includes published only. 2026-07-12: CommerceNode `status=published` returned the published page and excluded the draft page.
- [x] Status filter `draft` includes draft only. 2026-07-12: CommerceNode `status=draft` returned the draft page and excluded the published page.
- [x] `POST /api/commerce/admin/pages` creates draft page by default. 2026-07-12: direct API created `qa-draft-page-20260712034014` with `isPublished=false`.
- [ ] Create page requires slug.
- [ ] Slug is normalized before save.
- [ ] Duplicate slug in same store is rejected.
- [ ] Duplicate slug is rejected even if old page is archived.
- [x] Dangerous HTML `<script>` is rejected. 2026-07-12: direct API returned 400 with `success=false` and message `Page body HTML contains a disallowed tag.`
- [ ] Dangerous HTML `javascript:` is rejected.
- [ ] Dangerous inline event such as `onerror=` is rejected.
- [ ] External image URL in `<img src>` is rejected.
- [ ] Local image URL in `<img src="/media/...">` is accepted.
- [ ] External HTTPS link in `<a href>` is accepted.
- [ ] Body above 100 KB is rejected.
- [x] Draft page is not returned from `GET /api/internal/pages/{slug}`. 2026-07-12: `GET /api/internal/pages/qa-draft-page-20260712034014` with `X-Store-Key=default` returned 404.
- [ ] Archived page is not returned from `GET /api/internal/pages/{slug}`.
- [x] Published page is returned from `GET /api/internal/pages/{slug}`. 2026-07-12: `GET /api/internal/pages/qa-dynamic-page-20260712034014` returned title, intro, body HTML, and SEO data.
- [ ] Archive hides page from admin list.
- [ ] Archive reserves slug.
- [x] Sitemap entries include only published pages with `include_in_sitemap=true`. 2026-07-12: `GET /api/internal/catalog/sitemap` included `qa-dynamic-page-20260712034014` and Storefront `/sitemap.xml` excluded `qa-draft-page-20260712034014`.
- [ ] Store A cannot read Store B page by slug.

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
- Fixed Variation Template option/value creation returning HTTP 500 by marking new child rows as `Added` before `SaveChanges`.
- Fixed Product Import media queueing from background worker by adding store-scoped media import, avoiding dependency on HTTP `X-Store-Key`.

## Verification Commands

- `docker compose -f compose.commercenode.yml up -d`
- `dotnet run --project BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --urls http://localhost:5180`
- `dotnet test BlazorShop.sln`

CommerceNode migrations are applied by API startup when `CommerceNode:Database:MigrateOnStartup=true`. Use `dotnet ef database update` only as a manual diagnostic fallback, not as the normal V2 run path.

Latest ProductMedia QA result: 2026-07-10 CommerceNode API smoke passed for import queue, retry, worker storage, Product.Image sync, public imgproxy rendering, invalid scheme rejection, private/local source blocking, and cross-store 404. Fixed EF projection and temp-file length bugs found during QA.

Latest test result: 2026-07-09 full solution test passed: 485 passed, 10 skipped. Independent API smoke passed for ControlPlane -> CommerceNode health probe, Commerce admin catalog/media, Storefront internal auth/cart/order, and admin order visibility.

Latest startup migration QA result: 2026-07-11 CommerceNode API build passed, `run-v2-local.ps1 -DryRun` passed, and startup migration created/migrated disposable DB `blazorshop_commerce_node_startup_qa_20260711` with safe migration logs. Failure-policy and restart-idempotency checks remain open.

## Checkout And Payment Foundation

- [x] CommerceNode API builds after checkout/payment foundation changes. 2026-07-13: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- [x] `PaymentMethods` seed contains `cod`, `stripe`, and `paypal`. 2026-07-13: ControlPlane payment admin page loaded all three seeded methods for default store.
- [x] `bank_transfer` is not returned by Storefront payment methods. 2026-07-13: `GET /api/internal/payments/methods` returned COD only.
- [x] Default store has `cod` enabled and `stripe/paypal` disabled. 2026-07-13: payment admin page showed COD checked, Stripe/PayPal unchecked.
- [x] `GET /api/internal/payments/methods` returns only enabled methods for current store. 2026-07-13: request with `X-Store-Key=default` returned COD only.
- [x] `POST /api/internal/cart/checkout` with COD creates an order. 2026-07-13: visible Storefront checkout created `ORD-20260713-6672B965`.
- [x] Created COD order has `order_status=processing`. 2026-07-13: order list showed `processing` immediately after checkout before completion.
- [x] Created COD order has `payment_status=paid`. 2026-07-13: order list and detail API showed `paid`.
- [x] Created COD order has `payment_method_key=cod`. 2026-07-13: order detail API and DB query showed `cod`.
- [x] Created COD order has `payment_at`. 2026-07-13: order detail API and DB query showed non-null `payment_at`.
- [x] Created COD order has `payment_metadata_json`. 2026-07-13: DB query showed COD handler metadata JSON.
- [x] Created COD order has customer snapshot fields. 2026-07-13: order detail API returned customer name/email snapshot.
- [x] Created COD order has shipping address snapshot fields. 2026-07-13: order detail API and DB query returned shipping name/email/address fields.
- [x] Disabled `stripe` checkout request returns `success=false`. 2026-07-13: direct API checkout with `paymentMethodKey=stripe` returned `success=false`.
- [x] Unknown payment method key returns `success=false`. 2026-07-13: direct API checkout with `paymentMethodKey=unknown` returned `success=false`.
- [ ] Checkout creates customer when email does not exist.
- [ ] Checkout attaches existing customer when email exists.
- [x] Admin order detail returns payment fields. 2026-07-13: order detail API returned payment status, method, payment date, and completed date.
- [x] Admin mark complete succeeds for paid/shipped order. 2026-07-13: ControlPlane Orders drawer updated shipping to `shipped` and marked order complete.
- [ ] Admin mark complete rejects unpaid order.
- [ ] Store isolation blocks another store from reading/completing the order.
- [ ] Audit log includes `Order.Completed`.
