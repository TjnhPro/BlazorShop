# BlazorShop Catalog Structure Core Todo

Generated: 2026-07-16

Scope:

- 13.1 Category.
- 13.2 Product publication.
- 13.3 Product identity.
- 13.4 Product types can biet.

Goal: strengthen the active V2 Commerce Node catalog structure without replacing the working product/category/store/SEO/media foundations.

Boundary checklist:

- [ ] Keep catalog data in `CommerceNodeDbContext`.
- [ ] Keep Storefront V2 reads under `api/storefront/stores/{storeKey}/*`.
- [ ] Keep Commerce Admin writes under `api/commerce/*` with `storeKey` query where store-scoped.
- [ ] Keep Control Plane Web calling Control Plane API only.
- [ ] Do not extend legacy `BlazorShop.Presentation` or `AppDbContext`.
- [ ] Preserve existing `Product.CategoryId`, `Product.Image`, and `Category.Image` compatibility fields until a migration phase proves replacement is safe.
- [ ] Reuse SEO Routing Slug Core and Media Core; do not create parallel slug/media systems.

## Phase 0 - Baseline And Safety Inventory

Goal: lock current product/category behavior before schema changes.

Implementation checklist:

- [x] Re-read active V2 category/product services, repositories, controllers, DTOs, import task, cart/checkout product checks, recommendations, SEO services, and Storefront API client. 2026-07-16 Phase 1 prep: reviewed category/product entity, DTO, mapping, service, repository, Commerce admin controller, Storefront scoped catalog controller, Storefront contract mappings, ControlPlane categories page, Storefront API client, and category page.
- [ ] Capture current Storefront behavior for:
  - [ ] category tree.
  - [ ] category by slug.
  - [ ] products by category.
  - [ ] product by slug.
  - [ ] sitemap.
  - [ ] cart product validation.
  - [ ] checkout product validation.
- [ ] Add or identify tests for:
  - [ ] published category tree.
  - [ ] unpublished or archived category hidden from public.
  - [ ] unpublished or archived product hidden from public list/detail/sitemap.
  - [ ] category parent cannot cross store.
  - [ ] product category cannot cross store.
  - [ ] product variant/cart behavior still works.
- [ ] Update QA checklist seeds for Commerce Node, Control Plane, and Storefront V2.
- [ ] Make no runtime schema change in this phase.

Verification checklist:

- [ ] Focused category/product public-read tests pass.
- [ ] Focused cart/checkout product validation tests pass.
- [ ] No active V2 route ownership changes.

Exit criteria:

- [ ] Current behavior is documented.
- [ ] Any failing cross-store/public-visibility gaps are known before implementation.
- [ ] Plan/checklist file is ready for implementation commit.

Suggested commit:

```text
docs: plan catalog structure core
```

## Phase 1 - Category Content And Admin Surface

Goal: add missing category content while keeping category tree/store/SEO/media behavior stable.

Implementation checklist:

- [x] Add nullable `Category.Description` text field. 2026-07-16: added `Category.Description` and EF text mapping.
- [x] Add Commerce Node migration only. 2026-07-16: added `CommerceNodeCategoryDescription` migration under `Data/CommerceNode/Migrations`.
- [x] Update category DTOs:
  - [x] admin create/update can set description. 2026-07-16: Application `CategoryBase` carries description through Commerce admin and Control Plane gateway.
  - [x] admin detail/list can return description where useful. 2026-07-16: `GetCategory` mapping returns description and Control Plane category list/drawer shows it.
  - [x] public category response can return description if safe. 2026-07-16: Storefront category response contract and SharedV2 category model include description.
- [x] Confirm `IsPublished` is exposed consistently in admin flows. 2026-07-16: existing category SEO form remains the publish toggle owner.
- [n/a] Add explicit admin publish field and validation if current create/update DTO cannot set it. Existing publish toggle is already in category SEO flow; create/update was left focused on content/tree fields.
- [x] Preserve existing `DisplayOrder`, `ParentCategoryId`, `Image`, slug, SEO, and store scope. 2026-07-16: no route/scope/SEO field ownership changes.
- [n/a] Update category import/seeding only if existing dev seeder needs the field. Description is optional and no seed/import requirement was introduced.
- [x] Add tests for create/update/read description. 2026-07-16: Category service normalization tests, mapping tests, and Storefront OpenAPI schema guard added.
- [x] Add tests for publish toggle behavior. Existing category SEO publish behavior remains covered by Category SEO service tests and was not changed in this phase.

Verification checklist:

- [x] Commerce Node API builds. 2026-07-16: EF migration generation and focused test build compiled CommerceNode API.
- [x] Commerce Node migration model snapshot is valid. 2026-07-16: migration and CommerceNode snapshot contain only nullable `Category.Description`; `CommerceNodeDbContextModelTests` passed.
- [x] Category contract/OpenAPI tests pass if category endpoints changed. 2026-07-16: `CommerceNodeStorefrontOpenApiContractTests` passed after snapshot update.

Exit criteria:

- [x] Category description can be managed without using SEO fields as body content. 2026-07-16: Control Plane category drawer edits `Description`; Storefront category page renders description before falling back to meta description.
- [x] Existing category tree output stays backward compatible. 2026-07-16: tree contract was not changed.
- [x] Storefront category pages can display description without breaking older clients. 2026-07-16: nullable field added to response; old clients can ignore it.

Suggested commit:

```text
feat(commerce-node): add category content fields
```

## Phase 2 - Category Tree, Breadcrumb, Counts, And Descendant Product Rules

Goal: make public category behavior predictable.

Implementation checklist:

- [x] Add category breadcrumb projection using existing parent chain and store-scoped category repository. 2026-07-16: category page response now carries root-to-current breadcrumbs from published category tree data.
- [x] Add category product count projection:
  - [x] direct product count. 2026-07-16: `DirectProductCount` added to category page response and Storefront display.
  - [x] descendant-inclusive count. 2026-07-16: `DescendantProductCount` added using published descendant category ids.
- [x] Add explicit product query option:
  - [x] `includeSubcategories=false` by default unless current route behavior already implies true. 2026-07-16: Storefront product query exposes `includeSubcategories`; default API behavior is direct-only.
  - [x] document current default before changing public behavior. 2026-07-16: Storefront Search sends `includeSubcategories=true` when filtering by category slug to preserve the previous descendant search UX.
- [x] Normalize category page and product catalog query behavior so category slug and category id paths use the same descendant rule. 2026-07-16: repository applies the same `IncludeSubcategories` descendant expansion to slug and id filters.
- [x] Ensure counts include only public-visible products:
  - [x] same store. 2026-07-16: repository count test excludes other-store products.
  - [x] `IsPublished=true`. 2026-07-16: repository count/query filters remain public-only and tests include a draft product.
  - [x] `ArchivedAt=null`. 2026-07-16: repository count/query filters retain archived exclusion.
  - [n/a] availability window valid after Phase 4. Availability window fields are not implemented yet.
  - [x] category published and not archived. 2026-07-16: descendant ids come from published categories and repository counts also require published category.
- [n/a] Cache/invalidate category tree/count data through existing catalog cache invalidation. 2026-07-16: counts are computed live; category tree keeps the existing catalog cache path.
- [x] Add tests for direct count. 2026-07-16: `PublicCatalogServiceTests.GetPublishedCategoryPageBySlugAsync_AddsBreadcrumbsAndProductCounts`.
- [x] Add tests for descendant count. 2026-07-16: `CommerceNodeProductStoreScopeTests.CountPublishedProductsByCategoryIdsAsync_ExcludesHiddenCategoriesAndOtherStores`.
- [x] Add tests for hidden products. 2026-07-16: repository tests include draft/hidden category products.
- [x] Add tests for hidden categories. 2026-07-16: descendant query/count tests exclude products in unpublished child category.
- [x] Add tests for breadcrumb order. 2026-07-16: application service test asserts root-to-current order.

Verification checklist:

- [x] Storefront catalog tests pass. 2026-07-16: focused `PublicCatalogServiceTests`, `CommerceNodeProductStoreScopeTests`, and `StorefrontV2ApiClientTests` passed 21/21.
- [n/a] Sitemap tests pass. Phase 2 did not change sitemap behavior.
- [x] OpenAPI contract tests pass if query contracts changed. 2026-07-16: `CommerceNodeStorefrontOpenApiContractTests` passed 23/23 after snapshot refresh.

Exit criteria:

- [x] Category breadcrumb and counts are store-scoped. 2026-07-16: service/repository tests cover current-store category breadcrumbs and count filters.
- [x] Category product listing behavior is explicit and tested. 2026-07-16: `includeSubcategories` controls descendant expansion; direct-only default and descendant true paths are tested.
- [~] Storefront and sitemap do not include unpublished/archived category/product data. 2026-07-16: catalog query/count paths are verified; sitemap awaits later availability/publication release gate.

Suggested commit:

```text
feat(storefront): add category breadcrumbs and product counts
```

## Phase 3 - Product Category Mapping Compatibility Layer

Goal: support product-category mapping and per-category display order without breaking `Product.CategoryId`.

Entry criteria:

- [ ] User confirms products must be allowed in multiple categories, or product display order must be category-specific.
- [ ] Phase 0 tests are passing.
- [ ] Phase 2 tests are passing.

Implementation checklist:

- [ ] Add `ProductCategoryMapping` entity and EF configuration in Commerce Node.
- [ ] Add Commerce Node migration with backfill from existing `Product.CategoryId`.
- [ ] Keep `Product.CategoryId` as primary category compatibility field.
- [ ] Add application service methods:
  - [ ] list category mappings for product.
  - [ ] set primary category.
  - [ ] add category mapping.
  - [ ] update mapping display order.
  - [ ] remove category mapping.
- [ ] Validate product and category belong to current store.
- [ ] Keep one primary mapping per product.
- [ ] Sync `Product.CategoryId` when primary mapping changes.
- [ ] Update public product/category queries to use mapping table after backfill.
- [ ] Keep compatibility fallback to `Product.CategoryId` while migration is incomplete.
- [ ] Update product import:
  - [ ] existing `category_slug` continues to set primary category.
  - [ ] optional future `category_slugs` can add additional mappings.
- [ ] Update Control Plane gateway/UI only through Control Plane API.
- [ ] Add tests for product in multiple categories.
- [ ] Add tests for per-category product order.
- [ ] Add tests for primary category sync.
- [ ] Add tests for cross-store mapping rejection.
- [ ] Add tests for delete/archive category behavior with mappings.

Verification checklist:

- [ ] Commerce Node migration model snapshot is valid.
- [ ] Admin contract tests pass.
- [ ] Storefront category/product tests pass.
- [ ] Product import tests pass if import changed.

Exit criteria:

- [ ] Existing single-category products behave the same.
- [ ] Multiple-category products render in every mapped category.
- [ ] Product detail still has one canonical/primary category for breadcrumb and SEO.
- [ ] No Storefront route changes are required.

Suggested commit:

```text
feat(commerce-node): add product category mappings
```

## Phase 4 - Product Publication Availability

Goal: add scheduled/expired product visibility without replacing current publication fields.

Implementation checklist:

- [x] Add nullable `Product.AvailableStartUtc`.
- [x] Add nullable `Product.AvailableEndUtc`.
- [x] Add validation:
  - [x] end must be after start when both are set.
  - [x] archived product cannot be public-visible.
- [x] Add derived admin status:
  - [x] `draft` when `IsPublished=false` and not archived.
  - [x] `scheduled` when start is in the future.
  - [x] `published` when public-visible now.
  - [x] `expired` when end is in the past.
  - [~] `archived` when `ArchivedAt != null`. Product DTOs do not currently expose `ArchivedAt` to Control Plane Web; public filters still exclude archived products.
- [x] Apply availability to:
  - [x] storefront product list/search.
  - [x] product detail by id/slug.
  - [x] category product lists/counts.
  - [x] sitemap.
  - [x] cart line validation.
  - [x] checkout submit validation.
  - [x] recommendations.
- [x] Keep `PublishedOn` semantics compatible for existing DTOs and SEO redirect logic.
- [x] Update product import optional fields:
  - [x] `available_start_utc`.
  - [x] `available_end_utc`.
- [x] Add tests for scheduled products.
- [x] Add tests for expired products.
- [x] Add tests for unpublished products.
- [x] Add tests for archived products.
- [x] Add tests for currently available products.

Verification checklist:

- [x] Commerce Node API builds.
- [x] Storefront V2 builds.
- [x] Cart/checkout tests pass.
- [x] Sitemap tests pass.
- [x] OpenAPI contract tests pass if DTOs changed.

Exit criteria:

- [x] Scheduled products are hidden publicly until start time.
- [x] Expired products disappear from list/detail/cart/checkout/sitemap.
- [x] Admin can still view/edit all non-archived and archived products according to existing manager rules.

Suggested commit:

```text
feat(commerce-node): add product availability windows
```

## Phase 5 - Product Identity Fields

Goal: add commerce identity fields needed for feeds, integration, shipping prep, and manager data quality.

Implementation checklist:

- [x] Add nullable `Product.Gtin`.
- [x] Add nullable `Product.Barcode`.
- [x] Add nullable `Product.ManufacturerPartNumber`.
- [x] Add nullable `Product.Condition`.
- [x] Add nullable `Product.Weight`.
- [x] Add nullable `Product.Length`.
- [x] Add nullable `Product.Width`.
- [x] Add nullable `Product.Height`.
- [x] Add validation:
  - [x] max lengths.
  - [x] non-negative dimensions/weight.
  - [x] condition allowlist only if condition is exposed.
- [x] Update Product DTOs:
  - [x] admin create/update/detail includes new fields.
  - [x] public storefront only receives useful and safe fields.
- [x] Update product import with optional columns.
- [x] Update SEO/structured data hooks only where current composer supports product identifiers safely.
- [x] Do not add Manufacturer entity in this phase.
- [x] Add tests for validation.
- [x] Add tests for persistence.
- [x] Add tests for import.
- [x] Add tests for public-safe projection.
- [x] Add tests for structured data if changed.

Verification checklist:

- [x] Commerce Node migration model snapshot is valid.
- [x] Admin contract tests pass.
- [x] Storefront public schema guardrails pass.
- [x] Product import tests pass if import changed.

Exit criteria:

- [x] Existing product create/update requests still work because fields are optional.
- [x] Admin can store identity/dimension data.
- [x] Storefront public schema does not leak unsupported/internal fields.
- [x] Shipping does not start using dimensions until shipping rules explicitly consume them.

Suggested commit:

```text
feat(commerce-node): add product identity fields
```

## Phase 6 - Product Variant And Attribute MVP Hardening

Goal: strengthen the product-with-variants path that already exists before adding new product types.

Implementation checklist:

- [x] Re-test `Simple`, `VariantInventory`, and `CustomVariations` behavior across:
  - [x] admin.
  - [~] product import. Product import variant behavior was not changed in Phase 6; existing `variation_template_slug` validation remains in place.
  - [~] Storefront product detail. No product-detail DTO change was required in Phase 6.
  - [x] cart.
  - [x] checkout.
  - [~] inventory stock deduction. Inventory deduction behavior was re-covered through focused checkout tests, not changed directly in this phase.
- [x] Ensure variant SKU uniqueness is scoped to product/store according to current model.
- [x] Ensure default variant behavior is deterministic.
- [x] Ensure selected attributes are normalized consistently.
- [x] Ensure cart/checkout error messages clearly say when variant selection is required or invalid.
- [x] Add product detail DTO fields only if Storefront needs them for variant selector.
- [x] Do not add variant media in this phase.
- [x] Add tests for product with no variants.
- [x] Add tests for product with default variant.
- [x] Add tests for product requiring variant selection.
- [x] Add tests for invalid variant from another product/store.
- [x] Add tests for custom variation attributes normalized into cart/order lines.

Verification checklist:

- [x] Product variant service tests pass.
- [~] Storefront product detail tests pass. No product-detail code path changed; not re-run as a dedicated browser/detail test in Phase 6.
- [x] Cart/checkout variant tests pass.
- [~] Product import variant tests pass if import changed. Product import was unchanged in Phase 6.

Exit criteria:

- [x] Current variant model is reliable enough for Storefront use.
- [x] Product type constants still match implemented behavior only.
- [x] No advanced product type constant is added without cart/checkout behavior.

Suggested commit:

```text
test(catalog): harden product variant behavior
```

## Phase 7 - Admin And Control Plane Integration

Goal: expose the approved catalog structure safely to managers.

Implementation checklist:

- [x] Keep Control Plane Web calling Control Plane API only.
- [x] Add or extend Control Plane API gateway routes for:
  - [x] category description/publish fields.
  - [~] category product counts if manager list needs them. Storefront category counts are available from CommerceNode public catalog API; Control Plane category list does not currently require count display.
  - [x] product availability window.
  - [x] product identity fields.
  - [~] product category mappings if Phase 3 is approved. Phase 3 remains deferred.
- [x] Ensure Commerce Node admin endpoints use `storeKey` query and current store context.
- [x] Add permissions only if current catalog/SEO/store permissions are too broad. No new permission was required; existing catalog store read/write policies cover these fields.
- [x] UI behavior:
  - [x] product manager shows publication status.
  - [x] product editor shows availability window.
  - [~] category manager shows publish state, display order, parent, product count. Description/display/parent are shown; publish remains in SEO drawer; product count not required in current list.
  - [~] mapping UI clearly marks primary category if Phase 3 is implemented. Phase 3 remains deferred.
- [x] Add API contract tests for any changed endpoints.
- [~] Add browser QA only for visible UI changes. Visible browser QA remains in the QA checklist; Phase 7 used build/gateway verification.

Verification checklist:

- [x] Control Plane API build passes.
- [x] Control Plane Web build passes if UI changed.
- [x] Gateway tests prove `storeKey` forwarding and no direct Web-to-CommerceNode call.
- [x] Permission tests pass if permissions changed.

Exit criteria:

- [x] No Commerce Node credentials/base URLs leak to Control Plane Web.
- [x] Cross-store object access returns Not Found or standard safe error.
- [x] Manager can understand publication/mapping state before save.

Suggested commit:

```text
feat(control-plane): expose catalog structure fields
```

## Phase 8 - Storefront Rendering, SEO, Sitemap, And Cache Alignment

Goal: make Storefront V2 consume the new catalog fields without changing public route ownership.

Implementation checklist:

- [x] Category page:
  - [x] render description where approved.
  - [x] render breadcrumb from category hierarchy.
  - [x] use count/list behavior from Phase 2.
- [x] Product page:
  - [x] hide unavailable products with correct 404/gone behavior from SEO resolver policy.
  - [x] include safe identity fields in structured data only where useful.
  - [x] keep product canonical path based on SEO Routing Slug Core.
- [x] Navigation/menu:
  - [x] continue using published categories only.
  - [x] invalidate menu/cache when category publish/order/slug changes.
- [x] Sitemap:
  - [x] include only current-store available products and published categories.
  - [x] exclude scheduled future products until available.
  - [x] exclude expired/archived products.
- [x] Cart/checkout:
  - [x] reject products no longer public-available.
- [x] Add Storefront tests for category page.
- [x] Add Storefront tests for product page.
- [x] Add Storefront tests for sitemap.
- [x] Add Storefront tests for unavailable product detail.
- [x] Add Storefront tests for cart/checkout validation.

Verification checklist:

- [x] Storefront V2 build passes.
- [x] Storefront host smoke tests pass.
- [x] Sitemap/robots tests pass.
- [x] No immutable cache headers are applied to dynamic routes.

Exit criteria:

- [x] Storefront public behavior matches admin publication state.
- [x] SEO canonical/slug systems remain unchanged.
- [x] Dynamic routes are not given immutable cache headers.

Suggested commit:

```text
feat(storefront): align catalog rendering with publication rules
```

## Phase 9 - Advanced Product Types Deferred Design

Goal: define entry criteria so future product types are not added as empty constants.

Implementation checklist:

- [x] Document runtime requirements before adding grouped product:
  - [x] child product selection.
  - [x] price display.
  - [x] inventory aggregation.
  - [x] cart lines.
- [x] Document runtime requirements before adding bundle product:
  - [x] bundle component rules.
  - [x] pricing.
  - [x] stock deduction.
  - [x] returns.
- [x] Document runtime requirements before adding downloadable/digital product:
  - [x] secure file download authorization.
  - [x] order entitlement.
  - [x] download limits.
- [x] Document runtime requirements before adding gift-card product:
  - [x] balance ledger.
  - [x] redemption.
  - [x] refunds.
  - [x] fraud controls.
- [x] Document runtime requirements before adding recurring/subscription product:
  - [x] payment provider subscription lifecycle.
  - [x] renewals.
  - [x] cancellation.
  - [x] invoice/order generation.
- [x] Document runtime requirements before adding customer-entered-price product:
  - [x] min/max price.
  - [x] tax.
  - [x] fraud controls.
  - [x] payment validation.
- [x] Keep current `ProductTypes.All` limited to implemented behavior.
- [x] Add tests preventing unsupported product types from being persisted.
- [x] Create separate plan files when any advanced type is approved.

Verification checklist:

- [x] Product type validation tests pass.
- [x] Product import rejects unsupported product types.
- [x] Admin create/update rejects unsupported product types.

Exit criteria:

- [x] Unsupported product types are not accepted by admin/import APIs.
- [x] Future agents have clear entry criteria and do not infer features from names alone.

Suggested commit:

```text
docs(catalog): document advanced product type gates
```

## Phase 10 - QA, Contract Tests, And Release Gate

Goal: close the phase with focused verification and checklist evidence.

Implementation checklist:

- [ ] Update `QA-CommerceNode.todo.md`.
- [ ] Update `QA-ControlPlane.todo.md` if gateway/UI changed.
- [ ] Update `QA-StorefrontV2.todo.md` if public rendering changed.
- [ ] Update `QA-CommerceNode-TaskOrchestration.todo.md` if product import changes.
- [ ] Run focused tests for changed services/repositories/controllers.
- [ ] Run API contract/OpenAPI tests for new/changed V2 APIs.
- [ ] Run Storefront tests for category/product public visibility.
- [ ] Run Control Plane Web build/browser QA if manager UI changes.
- [ ] Verify Commerce Node migration model snapshot.
- [ ] Review diff for:
  - [ ] no legacy `BlazorShop.Presentation` feature changes.
  - [ ] no `AppDbContext` V2 migration.
  - [ ] no direct ControlPlane.Web to CommerceNode API call.

Release gate:

- [ ] Category description/publish/count/breadcrumb behavior is verified.
- [ ] Product availability rules are enforced in public catalog, detail, sitemap, cart, and checkout.
- [ ] Product identity fields are optional and validated.
- [ ] Product mapping, if implemented, preserves primary category compatibility.
- [ ] Advanced product types remain deferred unless fully implemented.
- [ ] QA checklist contains evidence.

Suggested commit:

```text
test(catalog): complete catalog structure core qa
```

## QA Checklist Seeds

### Commerce Node

- [x] Category create/update stores description. 2026-07-16: service normalization/mapping tests and CommerceNode migration added.
- [ ] Category publish toggle hides/shows category in public tree.
- [x] Category breadcrumb is store-scoped and ordered root to leaf. 2026-07-16: application service breadcrumb test passed.
- [x] Category direct product count excludes hidden/unavailable products. 2026-07-16: direct count uses public product count repository; Phase 4 availability filters now exclude scheduled/expired/archived products.
- [x] Category descendant product count excludes hidden/unavailable products. 2026-07-16: descendant count/query tests exclude draft, hidden-category, other-store, scheduled, expired, and archived products.
- [ ] Product category mapping rejects cross-store category/product pairs.
- [ ] Product primary category syncs to `Product.CategoryId` when mappings are enabled.
- [x] Product availability start hides future product from list/detail/sitemap/cart/checkout. 2026-07-16: repository public catalog/detail/sitemap tests and cart availability test passed; checkout uses the same server-side product availability predicate.
- [x] Product availability end hides expired product from list/detail/sitemap/cart/checkout. 2026-07-16: repository public catalog/detail/sitemap tests passed for expired products and cart/checkout validation paths include end-window checks.
- [x] Product identity fields validate max length and non-negative dimensions. 2026-07-16 Phase 5: ProductService validation tests cover invalid condition, negative dimensions, and normalized persistence.
- [x] Unsupported product type is rejected. 2026-07-16 Phase 9: admin service and product import resolver reject unsupported types; `CatalogProductTypeGateTests|ProductServiceTests` passed 29/29.
- [x] Variant-required product cannot be checked out without valid variant selection. 2026-07-16 Phase 6: focused cart/checkout variant tests passed.

### Control Plane

- [~] Category manager shows description, publish state, display order, parent, and product count. 2026-07-16 Phase 1: description/display order/parent are shown; publish remains in SEO drawer; product count waits for Phase 2.
- [x] Product manager shows derived publication status. 2026-07-16 Phase 4: Control Plane product list/drawer now derive Draft/Scheduled/Published/Expired from publish flag and availability window.
- [x] Product editor can set availability window and identity fields. 2026-07-16 Phase 5: Basic info form now includes GTIN, barcode, MPN, condition, weight, length, width, and height.
- [ ] Product category mapping UI marks primary category if mappings are enabled.
- [ ] Control Plane Web calls only Control Plane API.
- [ ] Cross-store product/category edit returns safe Not Found.
- [ ] Validation errors from Commerce Node surface clearly.

### Storefront V2

- [x] Category page renders description and breadcrumb. 2026-07-16 Phase 2: category page consumes API breadcrumbs; Phase 1 rendered description.
- [x] Category product list follows approved include-subcategories rule. 2026-07-16: category page remains direct-only; search category filter sends explicit descendant flag.
- [x] Product count matches visible products. 2026-07-16: category page displays direct product count; descendant count is available in the API contract.
- [x] Future scheduled product is not accessible publicly. 2026-07-16: `PublishedCatalogQueries_ExcludeScheduledAndExpiredProducts` asserts scheduled product is absent from public page, detail, and sitemap.
- [x] Expired product is not accessible publicly. 2026-07-16: `PublishedCatalogQueries_ExcludeScheduledAndExpiredProducts` asserts expired product is absent from public page, detail, and sitemap.
- [x] Product sitemap excludes hidden/unavailable products. 2026-07-16: sitemap repository test excludes scheduled, expired, and archived products.
- [x] Cart/checkout reject unavailable product. 2026-07-16: cart service test rejects scheduled product; checkout/payment attempt validation uses the same product availability window predicate.
- [x] Product structured data includes only safe identity fields. 2026-07-16 Phase 5: structured data adds SKU/GTIN/MPN/itemCondition and omits dimensions.

## Deferred Scope Checklist

- [ ] Full localization model for category/product fields remains deferred.
- [ ] Customer-role ACL visibility remains deferred.
- [ ] Recycle-bin UI beyond existing soft archive remains deferred.
- [ ] Manufacturer/brand domain model remains deferred.
- [ ] Grouped product remains deferred.
- [ ] Bundle product remains deferred.
- [ ] Downloadable/digital product remains deferred.
- [ ] Gift-card product remains deferred.
- [ ] Recurring/subscription product remains deferred.
- [ ] Customer-entered-price product remains deferred.
- [ ] Full Smartstore-style product type framework remains deferred.
- [ ] Replacing existing product/category public routes remains deferred.

## Risk Register

- [ ] Mapping table must not break current single-category behavior.
- [x] Availability filter must not be missed in cart/checkout. 2026-07-16: `StorefrontCartService` and `PaymentAttemptService` both enforce start/end windows.
- [ ] Product status enum migration must not cause unnecessary churn.
- [x] Advanced product type constants must not imply unsupported behavior. 2026-07-16 Phase 9: `ProductTypes.All` gate and unsupported import/admin tests passed.
- [ ] Category counts must not become stale.
- [ ] Cross-store category/product mapping must not leak data.
- [ ] SEO/media systems must not be duplicated.
- [ ] Localization fields must not create false promises.
- [ ] Legacy projects must not be extended.

## Recommended Implementation Order

- [ ] Phase 0 - baseline and guardrails.
- [x] Phase 1 - category content/admin publish surface. 2026-07-16: committed after focused tests.
- [x] Phase 2 - category breadcrumb/count/descendant product behavior. 2026-07-16: implemented and verified with focused catalog/client/OpenAPI tests.
- [x] Phase 4 - product availability window. 2026-07-16: implemented and verified with focused product service, repository, cart, import, Control Plane, DbContext model, and Storefront OpenAPI contract tests.
- [x] Phase 5 - product identity fields. 2026-07-16: implemented with migration, validation, admin edit, import columns, and structured data tests.
- [x] Phase 6 - variant MVP hardening. 2026-07-16: added ProductVariantService guard tests and re-ran cart/checkout variant tests.
- [ ] Phase 3 - product-category mapping only if multi-category/per-category order is approved for implementation.
- [x] Phase 7 - Control Plane/admin integration. 2026-07-16: verified existing gateway/UI integration with Control Plane API/Web builds and storeKey boundary tests.
- [x] Phase 8 - Storefront rendering/SEO/sitemap/cache alignment. 2026-07-16: verified with Storefront V2 build, host smoke, sitemap, API client, structured data, cart, and checkout tests.
- [x] Phase 9 - advanced product type gates. 2026-07-16: `CatalogProductTypeGateTests|ProductServiceTests` passed 29/29.
- [ ] Phase 10 - QA/release gate.

## Definition Of Done

- [ ] Category content, tree, breadcrumb, publish state, display order, store mapping, SEO/slug, image, and product counts are verified.
- [x] Product publication has clear admin state and public availability rules. 2026-07-16 Phase 4: Control Plane derives Draft/Scheduled/Published/Expired; public catalog/detail/sitemap/cart/checkout/recommendation filters use availability windows.
- [x] Product identity fields exist as optional validated data.
- [ ] Current variant/product-with-attributes behavior is tested across admin, Storefront, cart, and checkout.
- [ ] Product-category mapping is either implemented compatibly or explicitly deferred with entry criteria.
- [x] Advanced product types remain blocked until their runtime behavior is designed and approved. 2026-07-16 Phase 9: documented entry criteria and tests keep `ProductTypes.All` limited to implemented MVP behavior.
- [ ] All changed V2 APIs satisfy API contract standards.
- [ ] QA checklists contain verification evidence.
- [ ] No legacy presentation or `AppDbContext` V2 feature work is introduced.
