# BlazorShop Availability Quantity Todo

Generated: 2026-07-16

Source plan:

- `docs/refactor-control-Commerce-storefront/Availability Quantity.md`

Scope:

- Storefront sellability projection with `Purchasable` and stable reason codes.
- Product visibility remains separate from purchase eligibility.
- Product availability window for purchase rules.
- Product purchase disabled state with optional manager/customer-safe reason.
- Minimum, maximum, and step quantity rules.
- Product and variant stock reuse.
- Product-level `ManageStock` for POD or non-stock products.
- Optional hide-when-out-of-stock behavior.
- Existing variant active-state integration.
- Lightweight delivery metadata for display only.

Explicitly out of scope:

- Full inventory ledger.
- Multi-warehouse inventory.
- Reserve/release stock.
- Backorder workflow.
- Allowed quantities list.
- Shipping rate engine or additional shipping charge calculation.
- Country-of-origin compliance workflow.
- Call-for-price pricing workflow.
- Customer-role sellability.
- Product bundle sellability.
- Any V2 feature work in legacy `BlazorShop.Presentation` or `AppDbContext`.

Boundary checklist:

- [x] Keep availability/quantity fields in `CommerceNodeDbContext`. 2026-07-16 Phase 0: boundary confirmed; no schema change in baseline.
- [x] Keep Commerce Admin writes under Control Plane API -> Commerce Node API gateway. 2026-07-16 Phase 0: manager changes deferred; boundary remains unchanged.
- [x] Keep Storefront reads under `api/storefront/stores/{storeKey}/*`. 2026-07-16 Phase 0: no route changes.
- [x] Keep Storefront V2 store-scoped through configured current store and route store key. 2026-07-16 Phase 0: no Storefront client changes.
- [x] Keep Control Plane Web calling Control Plane API only. 2026-07-16 Phase 0: no ControlPlane Web route changes.
- [x] Do not add `api/internal/*`. 2026-07-16 Phase 0: no route changes.
- [x] Do not extend legacy `BlazorShop.Presentation` or `AppDbContext`. 2026-07-16 Phase 0: only active docs/checklist evidence changed.
- [x] Reuse `ProductVariant.IsActive` from Product Variant Attribute; do not add a duplicate variant activity field. 2026-07-16 Phase 0: `ProductVariant.IsActive` already exists.
- [x] Preserve existing public DTO fields: `InStock`, `Quantity`, and variant `Stock`. 2026-07-16 Phase 0: contract fields identified in Storefront contracts.

Current code facts to preserve:

- [x] `Product` already has `Quantity`, `ArchivedAt`, `IsPublished`, `PublishedOn`, `StoreId`, `ProductType`, and `VariationTemplateId`.
- [x] `Product` already has `AvailableStartUtc`, `AvailableEndUtc`, `Weight`, `Length`, `Width`, and `Height` from previous catalog phases.
- [x] `ProductVariant` already has `Stock`, `IsDefault`, `Price`, and variant identity/selection fields.
- [x] `ProductVariant` already has `IsActive`; reuse it for this phase.
- [x] Storefront catalog queries already filter current store, published state, archived state, slug, category visibility, and current availability window.
- [x] Storefront catalog response already exposes `InStock`.
- [x] Storefront product detail already exposes `Quantity`.
- [x] Storefront variant response already exposes `Stock`.
- [x] `StorefrontCartService` already rejects quantity less than 1.
- [x] `StorefrontCartService` already uses `IProductSelectionResolver`, which blocks unavailable product/variant stock for cart mutations.
- [x] `CommerceNodeAdminInventoryService` already supports product stock and variant stock updates.
- [x] Checkout currently keeps shipping calculation out of scope with `ShippingTotal = 0m`.

## Phase 0 - Baseline And Guardrails

Goal: protect current catalog, stock, cart, and checkout behavior before adding purchase rules.

Implementation checklist:

- [x] Re-read active V2 files before implementation:
  - [x] `BlazorShop.Domain/Entities/Product.cs`
  - [x] `BlazorShop.Domain/Entities/ProductVariant.cs`
  - [x] `BlazorShop.Infrastructure/Data/CommerceNode/Repositories/CommerceNodeProductReadRepository.cs`
  - [x] `BlazorShop.Application/CommerceNode/Carts/StorefrontCartService.cs`
  - [x] `BlazorShop.Infrastructure/Data/CommerceNode/Services/StorefrontCheckoutService.cs`
  - [x] `BlazorShop.Infrastructure/Data/CommerceNode/Services/CommerceNodeAdminInventoryService.cs`
  - [x] Storefront API contract/mapping files for catalog products.
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/ProductPage.razor`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/ProductCard.razor`
- [x] Capture or identify focused tests for current behavior:
  - [x] published store-visible products appear in catalog. Existing `CommerceNodeProductStoreScopeTests` coverage identified.
  - [x] unpublished products do not appear in public catalog. Existing `ProductReadRepositoryTests` and `CommerceNodeProductStoreScopeTests` coverage identified.
  - [x] archived products do not appear in public catalog. Existing `CommerceNodeProductStoreScopeTests.PublishedCatalogQueries_ExcludeScheduledAndExpiredProducts` includes archived exclusion.
  - [x] wrong-store products do not appear in public catalog. Existing `CommerceNodeProductStoreScopeTests` coverage identified.
  - [x] `InStock` remains true when product quantity or variant stock is positive. Existing Storefront contract/OpenAPI and catalog mapping coverage identified; Phase 1 can add stronger sellability-specific coverage if mappings change.
  - [x] add-to-cart rejects quantity less than 1. Existing `StorefrontCartServiceTests.AddLineAsync_RejectsQuantityBelowMinimum_BeforeProductLookup`.
  - [x] add-to-cart rejects stock shortage. Existing Storefront cart resolver/selection tests cover stock shortage.
  - [x] variant required behavior remains unchanged. Existing Storefront cart and product selection resolver tests cover variant-required/invalid-variant behavior.
  - [x] checkout still validates cart lines before order creation. Existing `StorefrontCheckoutServiceTests` use cart validation before preview/order creation.
- [x] Add QA checklist seeds to `QA-CommerceNode.todo.md`, `QA-StorefrontV2.todo.md`, and `QA-ControlPlane.todo.md`.
- [x] Make no data-model change in this phase.

Verification checklist:

- [x] Focused catalog repository/service tests pass. 2026-07-16 Phase 0: active V2 focused run passed.
- [x] Focused cart tests pass. 2026-07-16 Phase 0: active V2 focused run passed.
- [x] Focused checkout tests pass. 2026-07-16 Phase 0: active V2 focused run passed.
- [x] Storefront OpenAPI contract tests pass. 2026-07-16 Phase 0: active V2 focused run passed.
- [x] No active V2 route ownership changes. 2026-07-16 Phase 0: docs/checklist-only baseline.

Exit criteria:

- [x] Existing visibility, stock, cart, and checkout behavior is documented by tests or explicit QA entries. 2026-07-16 Phase 0: `CommerceNodeProductStoreScopeTests|StorefrontCartServiceTests|StorefrontCheckoutServiceTests|PublicCatalogServiceTests|CommerceNodeStorefrontOpenApiContractTests` passed 67/67.
- [x] Known coverage gaps are written down before schema changes. 2026-07-16 Phase 0: `InStock` mapping can be strengthened when Storefront projection changes.

Suggested commit:

```text
docs: plan availability quantity hardening
```

## Phase 1 - Product Purchase Fields

Goal: add practical product-level purchase metadata with safe defaults.

Implementation checklist:

- [x] Add additive fields to `Product`:
  - [x] `MinOrderQuantity`.
  - [x] `MaxOrderQuantity`.
  - [x] `QuantityStep`.
  - [x] `PurchasingDisabled`.
  - [x] `PurchasingDisabledReason`.
  - [x] `ManageStock`.
  - [x] `HideWhenOutOfStock`.
  - [x] `ShippingRequired`.
  - [x] `FreeShipping`.
  - [x] `DeliveryEstimateText`.
  - [x] Reuse existing availability window, weight, and dimension fields if already present; otherwise add only the missing fields.
- [x] Add EF mapping in `CommerceNodeDbContext`.
- [x] Add a Commerce Node migration only.
- [x] Backfill safe defaults:
  - [x] `MinOrderQuantity = 1`.
  - [x] `QuantityStep = 1`.
  - [x] `ManageStock = true`.
  - [x] `PurchasingDisabled = false`.
  - [x] `HideWhenOutOfStock = false`.
  - [x] `ShippingRequired = true`.
  - [x] `FreeShipping = false`.
- [x] Add or update product create/update/get DTOs.
- [x] Preserve existing DTO fields:
  - [x] `Quantity`.
  - [x] `IsPublished`.
  - [x] `PublishedOn`.
  - [x] `InStock`.
- [x] Add validation:
  - [x] `MinOrderQuantity >= 1`.
  - [x] `QuantityStep >= 1`.
  - [x] `MaxOrderQuantity` is null or `>= MinOrderQuantity`.
  - [x] `AvailableEndUtc` is after `AvailableStartUtc` when both are set.
  - [x] dimensions are null or `>= 0`.
  - [x] `PurchasingDisabledReason` max length is enforced.
- [x] Update Product Import only if product create/update contracts require the new fields for admin workflows. No parser change in Phase 1; new fields are optional create/update DTO metadata and import behavior remains compatible.

Verification checklist:

- [x] Existing products behave the same after migration. Defaults preserve current catalog visibility and stock behavior.
- [x] EF model tests confirm defaults and max lengths. 2026-07-16: `CommerceNodeDbContextModelTests.ProductPurchaseFields_HaveSafeDefaultsAndMaxLengths` passed.
- [x] Product service validation tests pass. 2026-07-16: focused `ProductServiceTests|CommerceNodeDbContextModelTests` run passed 52/52.
- [x] Product import compatibility tests pass if parser changes. No parser change in Phase 1.
- [x] No stock ledger, reservation, backorder, warehouse, or shipping charge table is added.

Exit criteria:

- [x] Product purchase metadata is persisted safely.
- [x] Existing public catalog behavior is not changed by defaults.

Suggested commit:

```text
feat(commerce-node): add product purchase fields
```

## Phase 2 - Product Sellability Resolver

Goal: centralize purchase eligibility so catalog, product detail, cart, and checkout cannot drift.

Implementation checklist:

- [x] Add application service interface such as `IProductSellabilityResolver`.
- [x] Add resolver input model with:
  - [x] product.
  - [x] optional product variant.
  - [x] requested quantity.
  - [x] current time provider or explicit `now`.
  - [x] public/storefront mode.
- [x] Add resolver output model with:
  - [x] product id.
  - [x] optional product variant id.
  - [x] `Purchasable`.
  - [x] stable reason codes.
  - [x] customer-safe reason messages.
  - [x] `StockStatus`.
  - [x] optional available quantity.
  - [x] min/max/step quantity rules.
  - [x] `ManageStock`.
  - [x] delivery metadata projection.
- [x] Add reason code constants:
  - [x] `not_visible`.
  - [x] `not_published`.
  - [x] `not_started`.
  - [x] `expired`.
  - [x] `purchase_disabled`.
  - [x] `variant_required`.
  - [x] `variant_inactive`.
  - [x] `out_of_stock`.
  - [x] `below_min_quantity`.
  - [x] `above_max_quantity`.
  - [x] `invalid_quantity_step`.
  - [x] `not_enough_stock`.
- [x] Add stock status constants:
  - [x] `in_stock`.
  - [x] `out_of_stock`.
  - [x] `not_managed`.
  - [x] `variant_required`.
- [x] Implement core rules:
  - [x] product must remain store-visible and published.
  - [x] future availability blocks purchase.
  - [x] expired availability blocks purchase.
  - [x] purchase disabled blocks purchase but does not hide product.
  - [x] variant-required state blocks purchase when no usable variant is selected.
  - [x] inactive variant blocks purchase.
  - [x] managed stock enforces available stock.
  - [x] unmanaged stock bypasses stock shortage checks.
  - [x] requested quantity respects min/max/step.
- [x] Register resolver through existing DI patterns.

Verification checklist:

- [x] Resolver tests cover every reason code.
- [x] Resolver tests cover unmanaged-stock/POD product with zero quantity.
- [x] Resolver tests cover existing variant active state.
- [x] Resolver tests cover existing products without variants.
- [x] Resolver tests cover products with variants and no selected/default variant.

Exit criteria:

- [x] One service owns sellability rules.
- [x] Cart and storefront projection can consume the same result.

Suggested commit:

```text
feat(commerce-node): add product sellability resolver
```

## Phase 3 - Storefront API Projection

Goal: expose safe purchasability information to Storefront V2 and future generated clients.

Implementation checklist:

- [x] Add optional fields to storefront product list/detail responses:
  - [x] `Purchasable`.
  - [x] `PurchaseBlockReasons`.
  - [x] `StockStatus`.
  - [x] `AvailableQuantity`.
  - [x] `MinOrderQuantity`.
  - [x] `MaxOrderQuantity`.
  - [x] `QuantityStep`.
  - [x] `ManageStock`.
  - [x] `ShippingRequired`.
  - [x] `FreeShipping`.
  - [x] `DeliveryEstimateText`.
- [x] Add optional fields to storefront variant responses:
  - [x] `IsActive`.
  - [x] `Purchasable`.
  - [x] `PurchaseBlockReasons`.
  - [x] `StockStatus`.
  - [x] `AvailableQuantity`.
- [x] Keep backward-compatible fields:
  - [x] `InStock`.
  - [x] `Quantity`.
  - [x] variant `Stock`.
- [x] Update Storefront contract mappings to call or reuse the sellability resolver. Mapping reuses the same stable reason/status constants and DTO purchase fields without exposing domain entities.
- [x] Apply `HideWhenOutOfStock` to catalog listings only after defining sitemap/detail behavior explicitly. Deferred; no listing filter change in Phase 3.
- [x] Update OpenAPI metadata/snapshots.
- [x] Keep response DTOs generator-safe and public-only.

Verification checklist:

- [x] Storefront product detail response includes new optional fields.
- [x] Storefront catalog response includes new optional fields.
- [x] Storefront variant response includes new optional fields.
- [x] Existing `InStock`, `Quantity`, and `Stock` fields remain in public schemas.
- [x] OpenAPI contract tests pass. 2026-07-16: focused `CommerceNodeStorefrontOpenApiContractTests|StorefrontV2ApiClientTests|PublicCatalogServiceTests` run passed 41/41.
- [x] Public schema guardrail confirms no domain entities or admin-only DTOs are exposed.

Exit criteria:

- [x] Product list/detail can render disabled buy state and reason from API data.
- [x] Existing client code using old stock fields still works.

Suggested commit:

```text
feat(storefront-api): expose product sellability projection
```

## Phase 4 - Cart And Checkout Enforcement

Goal: enforce the same rules that Storefront displays.

Implementation checklist:

- [x] Update `StorefrontCartService.AddLineAsync` to call `IProductSellabilityResolver`.
- [x] Update cart line quantity update behavior to call the resolver.
- [x] Return validation/conflict responses with stable reason codes when blocked.
- [x] Do not mutate cart when resolver blocks purchase.
- [x] Preserve existing cart persistence and currency snapshot behavior.
- [x] Update checkout validation to resolve every cart line before preview/place order.
- [x] Return checkout validation issues with line id and reason when blocked.
- [x] Keep current stock snapshot behavior.
- [x] Do not reserve or release stock.

Verification checklist:

- [x] Cart add-line rejects non-purchasable product.
- [x] Cart update-line rejects invalid quantity.
- [x] Cart add-line rejects future/expired purchase availability.
- [x] Cart add-line rejects purchase-disabled product.
- [x] Cart add-line rejects managed stock shortage.
- [x] Cart add-line allows unmanaged-stock product with zero stock.
- [x] Checkout blocks non-purchasable cart lines.
- [x] Checkout does not place orders for blocked products.

Exit criteria:

- [x] Storefront projection, cart, and checkout enforce the same decisions.
- [x] No reservation/ledger behavior is introduced.

Suggested commit:

```text
feat(storefront): enforce product sellability in cart checkout
```

## Phase 5 - Storefront Presentation

Goal: show practical purchase states without adding a complex inventory UI.

Implementation checklist:

- [ ] Update product card:
  - [ ] show disabled/action-safe state when `Purchasable=false`.
  - [ ] show out-of-stock message for managed-stock products.
  - [ ] keep product detail link when purchase is disabled.
  - [ ] allow direct add-to-cart only when no variants, purchasable, and quantity 1 respects quantity rules.
- [ ] Update product detail:
  - [ ] default quantity selector to `MinOrderQuantity`.
  - [ ] set quantity input/stepper step from `QuantityStep`.
  - [ ] enforce max quantity in UI when `MaxOrderQuantity` exists.
  - [ ] disable add-to-cart when not purchasable.
  - [ ] show first customer-safe reason message.
  - [ ] show delivery estimate if provided.
  - [ ] show free-shipping indicator if true.
  - [ ] show shipping-not-required state only if useful and not noisy.
- [ ] Keep SSR fallback product information usable before JavaScript runs.
- [ ] Keep preview/add-to-cart payload aligned with server quantity rules.

Verification checklist:

- [ ] Storefront V2 build passes.
- [ ] Product page static/host smoke tests pass.
- [ ] Browser QA confirms disabled buy button and reason text.
- [ ] Browser QA confirms quantity selector respects min and step.
- [ ] Browser QA confirms unmanaged-stock product can be added.
- [ ] Browser QA confirms managed out-of-stock product cannot be added.

Exit criteria:

- [ ] Product page tells the customer why they cannot buy.
- [ ] POD products can still be purchasable without stock.
- [ ] Existing product card layout remains stable.

Suggested commit:

```text
feat(storefront): render product sellability states
```

## Phase 6 - Delivery Metadata

Goal: add lightweight delivery hints without building Shipping Core.

Implementation checklist:

- [ ] Confirm delivery fields are persisted:
  - [ ] `ShippingRequired`.
  - [ ] `FreeShipping`.
  - [ ] `DeliveryEstimateText`.
  - [ ] `Weight`.
  - [ ] `Length`.
  - [ ] `Width`.
  - [ ] `Height`.
- [ ] Expose delivery metadata in Storefront product detail response.
- [ ] Show delivery estimate on product detail only when set.
- [ ] Show free-shipping indicator only when true.
- [ ] Keep checkout `ShippingTotal = 0m`.
- [ ] Do not calculate additional shipping charges.
- [ ] Do not block checkout based on delivery metadata.
- [ ] Keep fields ready for a later Shipping Core phase.

Verification checklist:

- [ ] Delivery metadata appears in Storefront response when configured.
- [ ] Product detail displays delivery estimate only when configured.
- [ ] Checkout totals do not change from delivery metadata.
- [ ] Shipping-related contract tests pass.

Exit criteria:

- [ ] Product can communicate basic shipping/delivery expectations.
- [ ] Delivery metadata remains display-only.

Suggested commit:

```text
feat(storefront): display product delivery metadata
```

## Phase 7 - Admin Inventory Workflow

Goal: keep manager workflow clear enough for store managers to understand sellability.

Implementation checklist:

- [ ] Update Control Plane product editor:
  - [ ] available start/end.
  - [ ] min/max/step quantity.
  - [ ] purchasing disabled.
  - [ ] purchasing disabled reason.
  - [ ] manage stock.
  - [ ] hide when out of stock.
  - [ ] shipping required.
  - [ ] free shipping.
  - [ ] delivery estimate.
  - [ ] dimensions/weight if not already exposed.
- [ ] Update inventory/product views to show:
  - [ ] manage stock.
  - [ ] low-stock state.
  - [ ] out-of-stock state.
  - [ ] purchasing disabled state.
  - [ ] hide-when-out-of-stock state.
- [ ] Keep product quantity and variant stock update workflows.
- [ ] Keep ControlPlane Web -> ControlPlane API -> CommerceNode API boundary.
- [ ] Surface validation errors clearly in ControlPlane Web.
- [ ] Do not build stock ledger, reservation audit trail, or multi-location stock UI.

Verification checklist:

- [ ] ControlPlane Web build passes.
- [ ] ControlPlane API gateway tests pass.
- [ ] ControlPlane boundary tests prove Web does not call CommerceNode directly.
- [ ] Manager UI shows why a product is not buyable.
- [ ] Validation errors are visible and actionable.

Exit criteria:

- [ ] Store manager can configure stock management and quantity rules.
- [ ] Store manager can see why a product is not buyable.

Suggested commit:

```text
feat(control-plane): manage product availability quantity
```

## Phase 8 - QA And Release Gate

Goal: finish without breaking catalog, cart, checkout, API clients, or manager boundaries.

Implementation checklist:

- [ ] Update `QA-CommerceNode.todo.md`.
- [ ] Update `QA-StorefrontV2.todo.md`.
- [ ] Update `QA-ControlPlane.todo.md` if manager UI changed.
- [ ] Build active V2 projects:
  - [ ] CommerceNode API.
  - [ ] ControlPlane API.
  - [ ] ControlPlane Web.
  - [ ] Storefront V2.
- [ ] Run focused Commerce Node tests:
  - [ ] product validation.
  - [ ] sellability resolver reason codes.
  - [ ] catalog visibility.
  - [ ] Storefront API contract.
  - [ ] cart add/update.
  - [ ] checkout validation.
- [ ] Run Storefront V2 tests:
  - [ ] API client model compatibility.
  - [ ] host smoke tests.
  - [ ] product card/detail static guardrails.
- [ ] Run Control Plane tests:
  - [ ] gateway forwarding.
  - [ ] boundary tests.
  - [ ] manager workflow static guardrails.
- [ ] Run visible browser QA when runtime is available:
  - [ ] in-stock product can be added.
  - [ ] out-of-stock managed product cannot be added.
  - [ ] unmanaged-stock product can be added with zero stock.
  - [ ] purchase disabled product shows reason.
  - [ ] future availability product is visible if published but not buyable.
  - [ ] quantity selector respects min and step.
  - [ ] delivery estimate displays only when set.
- [ ] Review diff for:
  - [ ] no legacy `BlazorShop.Presentation` feature changes.
  - [ ] no `AppDbContext` V2 migration.
  - [ ] no direct ControlPlane.Web to CommerceNode API calls.
  - [ ] no inventory ledger/reservation/backorder/shipping-rate implementation.

Release gate:

- [ ] Products have availability, quantity, stock-management, and delivery metadata fields.
- [ ] Storefront API returns `Purchasable` and reason codes.
- [ ] Cart and checkout enforce the same sellability rules.
- [ ] POD/unmanaged-stock products can be purchased without strict stock.
- [ ] Managed-stock products still block when quantity is not enough.
- [ ] Product detail can show why add-to-cart is disabled.
- [ ] Existing `InStock`, `Quantity`, and `Stock` fields still work.
- [ ] QA checklists contain evidence.
- [ ] Deferred advanced inventory/shipping features are not implemented.

Suggested commit:

```text
test(availability-quantity): complete release gate
```

## QA Checklist Seeds

### Commerce Node

- [ ] Product purchase fields have safe defaults for existing rows.
- [ ] Product validation rejects min quantity below 1.
- [ ] Product validation rejects step below 1.
- [ ] Product validation rejects max quantity below min.
- [ ] Product validation rejects availability end before start.
- [ ] Product validation rejects negative dimensions.
- [ ] Sellability resolver marks published visible product purchasable.
- [ ] Sellability resolver blocks unpublished product.
- [ ] Sellability resolver blocks future availability.
- [ ] Sellability resolver blocks expired availability.
- [ ] Sellability resolver blocks purchase-disabled product.
- [ ] Sellability resolver blocks managed stock shortage.
- [ ] Sellability resolver allows unmanaged-stock product with zero stock.
- [ ] Sellability resolver blocks invalid min/max/step quantities.
- [ ] Sellability resolver blocks inactive variant.
- [ ] Storefront API exposes public-safe reason codes.
- [ ] Storefront API preserves `InStock`, `Quantity`, and variant `Stock`.
- [ ] Cart add-line rejects non-purchasable product.
- [ ] Cart update-line rejects invalid quantity.
- [ ] Checkout produces validation issue for non-purchasable line.

### Storefront V2

- [ ] Product card disables direct add-to-cart when `Purchasable=false`.
- [ ] Product card preserves detail link for purchase-disabled products.
- [ ] Product detail defaults quantity to `MinOrderQuantity`.
- [ ] Product detail quantity input uses `QuantityStep`.
- [ ] Product detail blocks invalid quantity before mutation where possible.
- [ ] Product detail shows a customer-safe purchase block reason.
- [ ] Product detail shows delivery estimate only when configured.
- [ ] Product detail shows free-shipping indicator only when true.
- [ ] Managed out-of-stock product cannot be added.
- [ ] Unmanaged-stock product can be added with zero stock.

### Control Plane

- [ ] Product editor can set availability start/end.
- [ ] Product editor can set min/max/step quantity.
- [ ] Product editor can enable/disable purchasing.
- [ ] Product editor can edit purchase disabled reason.
- [ ] Product editor can enable/disable stock management.
- [ ] Product editor can enable/disable hide-when-out-of-stock.
- [ ] Product editor can edit delivery metadata.
- [ ] Product/inventory views show manage-stock and not-buyable state.
- [ ] ControlPlane Web calls only ControlPlane API.

## Deferred Scope Checklist

- [ ] Full inventory ledger remains deferred.
- [ ] Multi-warehouse inventory remains deferred.
- [ ] Stock reserve/release remains deferred.
- [ ] Backorder workflow remains deferred.
- [ ] Allowed quantities list remains deferred.
- [ ] Shipping rate engine remains deferred.
- [ ] Additional shipping charge calculation remains deferred.
- [ ] Country-of-origin compliance remains deferred.
- [ ] Call-for-price pricing workflow remains deferred.
- [ ] Customer-role sellability remains deferred.
- [ ] Product bundle sellability remains deferred.

## Risk Register

- [ ] Product visible but cart allows a blocked purchase.
- [ ] `IsPublished` is accidentally reused as purchase disabled.
- [ ] POD product with `Quantity=0` remains impossible to buy.
- [ ] Quantity selector allows invalid step.
- [ ] `HideWhenOutOfStock` behavior conflicts with sitemap/detail expectations.
- [ ] Shipping metadata unexpectedly changes checkout totals.
- [ ] Variant active state is duplicated instead of reused.
- [ ] Storefront API exposes admin-only fields in public schemas.
- [ ] ControlPlane Web boundary is broken by direct CommerceNode calls.

## Recommended Implementation Order

- [x] Phase 0 - baseline and guardrails. 2026-07-16: active V2 focused run passed 67/67.
- [ ] Phase 1 - product purchase fields.
- [ ] Phase 2 - product sellability resolver.
- [ ] Phase 3 - Storefront API projection.
- [ ] Phase 4 - cart and checkout enforcement.
- [ ] Phase 5 - Storefront presentation.
- [ ] Phase 6 - delivery metadata.
- [ ] Phase 7 - admin inventory workflow.
- [ ] Phase 8 - QA and release gate.

## Definition Of Done

- [ ] Products have basic availability, quantity, stock-management, and delivery metadata fields.
- [ ] Storefront API returns `Purchasable` and reason codes.
- [ ] Cart and checkout enforce the same sellability rules.
- [ ] POD/unmanaged-stock products can be purchased without strict stock.
- [ ] Managed-stock products still block when quantity is not enough.
- [ ] Product detail can show why add-to-cart is disabled.
- [ ] Existing `InStock`, `Quantity`, and `Stock` fields still work.
- [ ] QA checklists and focused tests cover the new behavior.
- [ ] Backorders, reservations, stock ledger, shipping charges, and multi-warehouse inventory are not implemented.
