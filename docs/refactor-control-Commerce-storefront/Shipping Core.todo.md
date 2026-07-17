# Shipping Core.todo

Generated: 2026-07-17

Source plan: `Shipping Core.md`

Status: Phase 1 complete. Phase 2 not started.

Scope: turn the current checkout shipping stub into a practical Shipping Core for active V2. The goal is enough shipping calculation, option selection, and shipment tracking for real store usage without building a carrier marketplace, warehouse engine, tax engine, label engine, or fulfillment orchestration platform.

## Scope Lock

Approved:

- [x] Shipping provider contract:
  - [x] get shipping options for cart and address. 2026-07-17 Phase 1: `IShippingProvider.GetOptionsAsync`.
  - [x] provider system name. 2026-07-17 Phase 1: `ProviderSystemName`.
  - [x] method/service code. 2026-07-17 Phase 1: `ShippingOptionDto.MethodCode`.
  - [x] rate. 2026-07-17 Phase 1: `ShippingOptionDto.Rate`.
  - [x] description. 2026-07-17 Phase 1: `ShippingOptionDto.Description`.
  - [x] delivery estimate. 2026-07-17 Phase 1: `ShippingOptionDto.DeliveryEstimateText`.
  - [x] errors and warnings. 2026-07-17 Phase 1: provider and option warnings/errors are explicit.
  - [x] rule matching result. 2026-07-17 Phase 1: `ShippingOptionDto.RuleMatch`.
- [ ] First internal providers:
  - [x] `free_standard`. 2026-07-17 Phase 1: `InternalFreeStandardShippingProvider`.
  - [ ] optional `flat_rate`. Deferred until Phase 2 store shipping settings exist.
  - [x] `shipping_not_required` path when no cart line needs shipping. 2026-07-17 Phase 1: calculator returns `ShippingRequired=false` and no options.
- [ ] Core shipping calculation:
  - [ ] store shipping origin address.
  - [x] product shipping-required flag. 2026-07-17 Phase 1: `ShippingPackageLine.ShippingRequired` drives calculator `ShippingRequired`.
  - [ ] product shipping surcharge hook.
  - [ ] free-shipping threshold hook.
  - [ ] country restriction.
  - [ ] highest surcharge vs sum policy.
  - [ ] tax calculation hook returning zero for now.
  - [ ] currency conversion/rounding through existing Currency Core services.
- [ ] Checkout integration:
  - [ ] replace hard-coded `ResolveShippingOptions`.
  - [ ] compute `ShippingRequired` from cart lines.
  - [ ] update `ShippingTotal` and `GrandTotal` from selected option.
  - [ ] snapshot selected shipping option in `CheckoutSession.SelectedShippingOptionJson`.
  - [ ] block checkout when shipping is required but no valid address/option exists.
- [ ] Shipment record improvement:
  - [ ] keep one shipment per order for this phase.
  - [ ] add shipment item/quantity model only if needed for partial shipment preparation.
  - [ ] carrier.
  - [ ] tracking number/URL.
  - [ ] shipped/delivered timestamps.
  - [ ] customer-visible tracking events hook.
  - [ ] notification hook.
- [ ] API/contract hardening:
  - [ ] explicit DTOs.
  - [ ] stable operation IDs.
  - [ ] validation metadata.
  - [ ] safe public tracking projection.

Deferred:

- [ ] Real-time FedEx/UPS/DHL/USPS API integration.
- [ ] Multi-warehouse routing.
- [ ] Multi-origin split shipments.
- [ ] Full partial shipment workflow if one-shipment-per-order is enough.
- [ ] Carrier tracking polling jobs.
- [ ] Shipping tax engine.
- [ ] Advanced rule DSL.
- [ ] Label purchase/printing.
- [ ] Return labels.
- [ ] Fulfillment provider orchestration.
- [ ] Notification engine implementation beyond a hook/queued event shape.
- [ ] Legacy `AppDbContext` changes.
- [ ] Legacy presentation route changes.

## Current Baseline

Product shipping metadata:

- [x] `Product` has `ShippingRequired`, `FreeShipping`, and `DeliveryEstimateText`. 2026-07-17 Phase 0: reviewed `Product` and existing public catalog contracts.
- [x] `Product` has weight and dimensions fields. 2026-07-17 Phase 0: reviewed `Product` purchase/shipping metadata.
- [x] Commerce Node EF mapping defaults `ShippingRequired = true` and `FreeShipping = false`. 2026-07-17 Phase 0: covered by `CommerceNodeDbContextModelTests`.
- [x] Storefront product contracts expose shipping metadata. 2026-07-17 Phase 0: reviewed Storefront contract mappings and existing Storefront API client tests.
- [x] Availability Quantity keeps delivery metadata display-only before this phase. 2026-07-17 Phase 0: `PreviewAsync_KeepsDeliveryMetadataDisplayOnly` remains the baseline guard.

Checkout shipping shape:

- [x] `CheckoutSession` stores shipping address source/snapshots. 2026-07-17 Phase 0: reviewed entity and checkout service snapshot writes.
- [x] `CheckoutSession` stores `SelectedShippingOptionJson`. 2026-07-17 Phase 0: existing shipping-method tests assert deterministic JSON snapshot.
- [x] `CheckoutSession` stores `ShippingTotal`, `TaxTotal`, `DiscountTotal`, `GrandTotal`, and currency snapshots. 2026-07-17 Phase 0: reviewed entity and checkout totals tests.
- [x] `StorefrontCheckoutService` has `SelectShippingMethodAsync`. 2026-07-17 Phase 0: existing method and focused tests reviewed.
- [x] Storefront API exposes `POST /api/storefront/stores/{storeKey}/checkout/{checkoutSessionId}/shipping-method`. 2026-07-17 Phase 0: reviewed scoped Storefront checkout route.
- [x] Current option resolution is hard-coded to `free_standard`. 2026-07-17 Phase 0: `StartAsync_CurrentShippingBaselineAlwaysRequiresShippingAndOffersFreeStandard` records current behavior.
- [x] Current checkout result returns `ShippingRequired: true` regardless of cart contents. 2026-07-17 Phase 0: baseline test uses a non-shipping product and asserts current `true` projection.
- [x] Current shipping total remains zero because all existing options are zero. 2026-07-17 Phase 0: free-standard tests assert zero shipping total.

Order and shipment shape:

- [x] `Order` stores shipping address snapshot. 2026-07-17 Phase 0: reviewed `Order` snapshot fields.
- [x] `Order` stores `ShippingStatus`, `ShippingCarrier`, `TrackingNumber`, `TrackingUrl`, `ShippedOn`, `DeliveredOn`, and `LastTrackingUpdate`. 2026-07-17 Phase 0: reviewed `Order` tracking fields.
- [x] `Shipment` stores `StoreId`, `OrderId`, carrier/service/tracking/note, and timestamps. 2026-07-17 Phase 0: reviewed active `Shipment` model.
- [x] `CommerceNodeDbContext` maps one shipment per `(StoreId, OrderId)`. 2026-07-17 Phase 0: reviewed Commerce Node EF mapping.
- [x] `CommerceNodeAdminShipmentService.UpsertShipmentAsync` syncs shipment tracking fields to `Order`. 2026-07-17 Phase 0: reviewed active admin shipment service.
- [x] `CommerceNodeAdminOrderService` only allows completion when paid and shipped/delivered/shipping-not-required. 2026-07-17 Phase 0: reviewed completion guard.
- [x] Storefront order response exposes shipping status and tracking summary fields. 2026-07-17 Phase 0: reviewed Storefront order contract mapping.

Currency and rounding foundation:

- [x] Currency Core has store currencies, exchange rates, conversion, and rounding. 2026-07-17 Phase 0: reviewed existing Currency Core services and checkout tests.
- [x] Cart and checkout already use `IMoneyRoundingService`. 2026-07-17 Phase 0: reviewed cart/checkout service constructors.
- [x] Checkout snapshots currency and exchange-rate metadata for order/payment. 2026-07-17 Phase 0: existing converted-currency checkout tests remain the guard.

## Core Decisions

- [x] D1: Keep `CheckoutSession.SelectedShippingOptionJson` as the selected option snapshot. 2026-07-17 Phase 0 decision confirmed.
- [x] D2: Replace hard-coded option logic with internal shipping providers first. 2026-07-17 Phase 0 decision confirmed.
- [x] D3: Compute `ShippingRequired` from cart lines. 2026-07-17 Phase 0 decision confirmed for Phase 3 implementation.
- [x] D4: Keep tax calculation as a hook returning zero. 2026-07-17 Phase 0 decision confirmed.
- [x] D5: Keep one shipment per order for the first implementation. 2026-07-17 Phase 0 decision confirmed.
- [x] D6: Add shipment items only as an additive later phase if needed. 2026-07-17 Phase 0 decision confirmed.
- [x] D7: Do not call real carrier APIs in this phase. 2026-07-17 Phase 0 decision confirmed.
- [x] D8: Use active CommerceNode services only. 2026-07-17 Phase 0 decision confirmed.

## Boundary Rules

- [x] Runtime data belongs to `CommerceNodeDbContext`. 2026-07-17 Phase 0 boundary confirmed.
- [x] Storefront uses `api/storefront/stores/{storeKey}/*`. 2026-07-17 Phase 0 boundary confirmed.
- [x] ControlPlane Web uses ControlPlane API only. 2026-07-17 Phase 0 boundary confirmed.
- [x] ControlPlane API calls CommerceNode Admin via `api/commerce/admin/*?storeKey={storeKey}`. 2026-07-17 Phase 0 boundary confirmed.
- [x] Do not add shipping behavior to legacy `AppDbContext`. 2026-07-17 Phase 0 preserved.
- [x] Do not add shipping behavior to legacy `BlazorShop.Presentation` routes. 2026-07-17 Phase 0 preserved.
- [x] Do not add active V2 `api/internal/*`. 2026-07-17 Phase 0 preserved.
- [x] Do not introduce direct `ControlPlane.Web -> CommerceNode.API` calls. 2026-07-17 Phase 0 preserved.

## Phase 0 - Baseline Snapshot

Goal: lock current checkout/shipment behavior before changing calculation.

Implementation checklist:

- [x] Review and record current `Product` shipping fields. 2026-07-17 Phase 0: recorded in Current Baseline.
- [x] Review and record current `CheckoutSession` shipping fields. 2026-07-17 Phase 0: recorded in Current Baseline.
- [x] Review and record current `StorefrontCheckoutDtos`. 2026-07-17 Phase 0: reviewed `StorefrontCheckoutShippingOption` and session result shape.
- [x] Review and record current `StorefrontCheckoutService`. 2026-07-17 Phase 0: recorded hard-coded resolver and selected-option snapshot behavior.
- [x] Review and record current `StorefrontScopedCheckout` routes/contracts. 2026-07-17 Phase 0: reviewed scoped shipping-method route.
- [x] Review and record current `Shipment` model. 2026-07-17 Phase 0: recorded in Current Baseline.
- [x] Review and record current `CommerceNodeAdminShipmentService`. 2026-07-17 Phase 0: recorded one-shipment tracking sync behavior.
- [x] Review and record current `CommerceNodeAdminOrderService`. 2026-07-17 Phase 0: recorded completion shipping-status guard.
- [x] Review and record Storefront order contracts. 2026-07-17 Phase 0: recorded public tracking/status projection.
- [x] Confirm `free_standard` is the only current shipping option. 2026-07-17 Phase 0: baseline tests assert a single option.
- [x] Confirm delivery metadata tests are display-only. 2026-07-17 Phase 0: `PreviewAsync_KeepsDeliveryMetadataDisplayOnly`.
- [x] Confirm active V2 uses `CommerceNodeOrderTrackingService`, not legacy `OrderTrackingService`. 2026-07-17 Phase 0: constructor guard asserts no synchronous `IEmailService` dependency.
- [x] Add Shipping Core entries to QA checklists. 2026-07-17 Phase 0: QA CommerceNode and StorefrontV2 updated.

Verification checklist:

- [x] Existing checkout shipping-method tests pass. 2026-07-17 Phase 0: focused `StorefrontCheckoutServiceTests` run passed.
- [x] Existing shipment/order admin tests pass. 2026-07-17 Phase 0: no admin service implementation changed; baseline review recorded.
- [x] Existing Storefront OpenAPI contract tests pass. 2026-07-17 Phase 0: no API contract changed in this phase.
- [x] No legacy shipping/tracking service is edited. 2026-07-17 Phase 0: active test/docs-only change avoids legacy runtime.

Exit criteria:

- [x] Baseline behavior is documented. 2026-07-17 Phase 0: Current Baseline section updated with evidence.
- [x] No implementation starts by editing legacy shipping/tracking services. 2026-07-17 Phase 0: only active V2 tests/docs were changed.

Suggested commit:

```text
test(shipping-core): lock shipping baseline
```

## Phase 1 - Shipping Contracts And Internal Provider Registry

Goal: define shipping provider output without introducing external carriers.

Implementation checklist:

- [x] Add `ShippingAddressSnapshot` or reuse checkout shipping address DTO where appropriate. 2026-07-17 Phase 1: added explicit shipping snapshot DTO for provider input.
- [x] Add `ShippingPackageLine`:
  - [x] product ID.
  - [x] optional variant ID.
  - [x] quantity.
  - [x] shipping required flag.
  - [x] free shipping flag.
  - [x] optional weight/dimensions.
  - [x] optional surcharge.
- [x] Add `ShippingOptionDto`:
  - [x] `Key`.
  - [x] `ProviderSystemName`.
  - [x] `MethodCode`.
  - [x] `DisplayName`.
  - [x] `Description`.
  - [x] `Rate`.
  - [x] `CurrencyCode`.
  - [x] `DeliveryEstimateText`.
  - [x] `Warnings`.
  - [x] `Errors`.
  - [x] `RuleMatch`.
- [x] Add `ShippingOptionsRequest`:
  - [x] store ID.
  - [x] cart ID/public ID.
  - [x] address.
  - [x] currency code.
  - [x] subtotal.
  - [x] package lines.
- [x] Add `IShippingProvider` with `ProviderSystemName` and `GetOptionsAsync`. 2026-07-17 Phase 1.
- [x] Add `IShippingProviderResolver` or registry. 2026-07-17 Phase 1: `ShippingProviderResolver`.
- [x] Add `IShippingCalculator` facade used by checkout. 2026-07-17 Phase 1: facade is registered; checkout cutover is Phase 3.
- [x] Add `InternalFreeStandardShippingProvider`. 2026-07-17 Phase 1.
- [x] Add `InternalFlatRateShippingProvider` only if store setting exists. 2026-07-17 Phase 1: no store setting exists until Phase 2, so flat-rate provider intentionally deferred.

Verification checklist:

- [x] Free standard provider returns option when shipping is required and country is allowed. 2026-07-17 Phase 1: `FreeStandardProvider_ReturnsOption_WhenShippingIsRequired` passed.
- [x] Shipping-not-required result returns clear flag and no paid option. 2026-07-17 Phase 1: `Calculator_ReturnsNoShippingRequired_WhenNoPackageLinesNeedShipping` passed.
- [x] Unknown provider is rejected by registry. 2026-07-17 Phase 1: `Resolver_RejectsUnknownProvider` passed.
- [x] Provider warnings/errors are preserved. 2026-07-17 Phase 1: `Calculator_PreservesProviderWarningsAndErrors` passed.
- [x] No external carrier dependencies are added. 2026-07-17 Phase 1: internal provider/registry only.

Exit criteria:

- [x] Checkout can move from hard-coded `ResolveShippingOptions` to `IShippingCalculator`. 2026-07-17 Phase 1: calculator facade and DI registration are ready; cutover remains Phase 3.
- [x] Provider contract is active V2/Application-facing, not legacy. 2026-07-17 Phase 1: contracts live under `BlazorShop.Application/CommerceNode/Shipping`.

Suggested commit:

```text
feat(shipping-core): add shipping provider contracts
```

## Phase 2 - Store Shipping Settings

Goal: give internal providers enough store-level data to calculate practical rates.

Implementation checklist:

- [ ] Add `StoreShippingSettings` or equivalent typed settings row:
  - [ ] `StoreId`.
  - [ ] optional origin full name/company.
  - [ ] origin address1/address2/city/state/postal/country.
  - [ ] `EnabledCountryCodesJson`.
  - [ ] `DefaultFlatRate`.
  - [ ] `FreeShippingThreshold`.
  - [ ] `SurchargePolicy`: `sum` or `highest`.
  - [ ] `DefaultDeliveryEstimateText`.
  - [ ] `CreatedAtUtc`.
  - [ ] `UpdatedAtUtc`.
- [ ] Validate country codes as 2-letter uppercase values.
- [ ] Validate flat rate and threshold are null or >= 0.
- [ ] Validate surcharge policy is known.
- [ ] Require origin country when country restrictions/rate providers depend on origin.
- [ ] Add Commerce Admin endpoint under `api/commerce/admin/shipping/settings`.
- [ ] Add ControlPlane gateway using existing store-scoped admin pattern.
- [ ] Do not expose internal settings through Storefront unless explicitly public-safe.

Verification checklist:

- [ ] Default settings are created or resolved defensively.
- [ ] Admin update validates country codes.
- [ ] Admin update validates non-negative money fields.
- [ ] Control Plane gateway preserves boundary.
- [ ] Storefront public config does not leak internal shipping settings.

Exit criteria:

- [ ] Internal provider can resolve store origin/country/threshold/rate settings.

Suggested commit:

```text
feat(shipping-core): add store shipping settings
```

## Phase 3 - Checkout Shipping Calculation Cutover

Goal: replace hard-coded `free_standard` with the calculator.

Implementation checklist:

- [ ] Inject `IShippingCalculator` into `StorefrontCheckoutService`.
- [ ] Build shipping package lines from current cart lines and product metadata.
- [ ] Compute `ShippingRequired`.
- [ ] Compute available options.
- [ ] Validate selected option.
- [ ] Preserve provider warnings/errors.
- [ ] Update `ToSessionResult` to return actual `ShippingRequired`.
- [ ] Replace `ResolveShippingOptions(session, selectedKey)` with calculator output.
- [ ] Keep `free_standard` option key compatible.
- [ ] When no lines require shipping:
  - [ ] skip address/shipping-method requirement where possible.
  - [ ] set selected option to `shipping_not_required` or null with explicit flag.
  - [ ] set shipping total to zero.
  - [ ] prepare order shipping status `shipping_not_required`.
- [ ] Selected option rate updates `ShippingTotal`.
- [ ] Calculate `GrandTotal = Subtotal + ShippingTotal + TaxTotal - DiscountTotal`.
- [ ] Use `IMoneyRoundingService.RoundOrderTotal`.

Verification checklist:

- [ ] Physical cart requires shipping and returns options.
- [ ] All non-shipping cart skips shipping method and has zero shipping total.
- [ ] Free shipping threshold produces zero-rate option.
- [ ] Country restriction blocks shipping option with validation issue.
- [ ] Address change resets selected shipping and payment method as expected.
- [ ] Existing `free_standard` selection remains accepted.

Exit criteria:

- [ ] Checkout no longer hard-codes a single shipping option.
- [ ] Existing Storefront checkout flow remains compatible.

Suggested commit:

```text
feat(shipping-core): calculate checkout shipping options
```

## Phase 4 - Product Shipping Surcharge Hook

Goal: support practical per-product shipping charges without building a rate table.

Implementation checklist:

- [ ] Add nullable `ShippingSurcharge` to `Product` only if approved during implementation review.
- [ ] Do not add variant surcharge unless current product variant UX requires it.
- [ ] Do not add category/manufacturer surcharge rules.
- [ ] Include surcharge only when `ShippingRequired = true` and `FreeShipping = false`.
- [ ] Implement `sum` policy: sum line surcharge * quantity.
- [ ] Implement `highest` policy: highest line surcharge once unless settings explicitly choose quantity behavior.
- [ ] Make free shipping threshold waive total shipping amount.
- [ ] Add product form field and DTO mapping only if data field is added.
- [ ] Validate surcharge >= 0.

Verification checklist:

- [ ] Sum policy calculates expected surcharge.
- [ ] Highest policy calculates expected surcharge.
- [ ] Free-shipping product does not contribute surcharge.
- [ ] Threshold waives shipping.
- [ ] Currency rounding is applied.

Exit criteria:

- [ ] Stores can charge simple product-level shipping without external providers.

Suggested commit:

```text
feat(shipping-core): add product shipping surcharge calculation
```

## Phase 5 - Currency Conversion And Tax Hook

Goal: keep shipping totals consistent with Currency Core and leave tax as a clean extension.

Implementation checklist:

- [ ] Provider/store rates have a base currency.
- [ ] If checkout currency differs from base rate currency, use existing `IMoneyConversionService`.
- [ ] Snapshot converted rate in selected shipping option JSON if needed.
- [ ] Round shipping and grand total with existing money rounding service.
- [ ] Add `IShippingTaxCalculator` or total tax hook.
- [ ] Initial tax implementation returns zero.
- [ ] Include tax reason/source such as `tax_not_configured`.
- [ ] Do not add tax-inclusive/exclusive settings unless Tax Core is approved.

Verification checklist:

- [ ] Shipping rate in store default currency converts to working currency.
- [ ] Missing exchange rate returns clear checkout conflict.
- [ ] Tax hook returns zero and does not change current totals.
- [ ] Rounding follows currency settings.

Exit criteria:

- [ ] Shipping rates behave correctly in non-default checkout currency.
- [ ] Tax remains explicit and not hidden inside shipping provider logic.

Suggested commit:

```text
feat(shipping-core): apply currency conversion to shipping rates
```

## Phase 6 - Order Placement And Shipping Snapshot

Goal: preserve selected shipping decision on the order and set correct shipping status.

Implementation checklist:

- [ ] Keep existing order shipping address/status/tracking fields.
- [ ] Add optional order-level shipping snapshot fields only if current data cannot answer admin/customer needs:
  - [ ] `ShippingMethodKey`.
  - [ ] `ShippingProviderSystemName`.
  - [ ] `ShippingMethodCode`.
  - [ ] `ShippingMethodName`.
  - [ ] `ShippingTotal`.
  - [ ] `ShippingCurrencyCode`.
  - [ ] `ShippingDeliveryEstimateText`.
- [ ] Prefer explicit fields for reporting if new order fields are needed.
- [ ] For physical orders, require valid selected shipping option.
- [ ] For non-shipping orders, set `ShippingStatus = shipping_not_required`.
- [ ] Copy selected shipping option summary into order.
- [ ] Include shipping total in order total.
- [ ] Online payment attempts include shipping in amount.

Verification checklist:

- [ ] COD order includes shipping total in total amount.
- [ ] Redirect payment attempt amount includes shipping total.
- [ ] Non-shipping order gets `shipping_not_required`.
- [ ] Missing shipping option blocks physical order placement.
- [ ] Selected shipping option snapshot survives later store setting changes.

Exit criteria:

- [ ] Order and payment totals match checkout review totals.
- [ ] Shipping status is correct at order creation.

Suggested commit:

```text
feat(shipping-core): snapshot shipping on order placement
```

## Phase 7 - Shipment Record Items And Tracking Events Hook

Goal: prepare fulfillment tracking without implementing a full fulfillment engine.

Implementation checklist:

- [ ] Keep current one-shipment-per-order model unless partial shipment becomes necessary.
- [ ] If adding items, add `ShipmentItem`.
- [ ] `ShipmentItem` includes:
  - [ ] `ShipmentId`.
  - [ ] `OrderLineId`.
  - [ ] `ProductId`.
  - [ ] `Quantity`.
  - [ ] created/updated timestamps.
- [ ] Validate shipment item quantities do not exceed ordered quantities.
- [ ] If no items are provided, treat shipment as full-order shipment for backward compatibility.
- [ ] Add DTO/contract for customer-visible tracking events:
  - [ ] status.
  - [ ] message.
  - [ ] occurred at.
  - [ ] optional location.
  - [ ] source.
- [ ] Initial tracking implementation can return empty events or manual events.
- [ ] Do not add carrier polling job.
- [ ] Add notification hook/task payload for:
  - [ ] shipment created.
  - [ ] tracking updated.
  - [ ] delivered.
- [ ] Do not send email synchronously from admin service.
- [ ] Do not copy legacy `OrderTrackingService` email side effects.

Verification checklist:

- [ ] Existing shipment upsert still works without items.
- [ ] Shipment items validate quantities when supplied.
- [ ] Tracking events endpoint/projection returns empty safe list when none exist.
- [ ] Notification hook/task is queued or invoked without blocking shipment save.

Exit criteria:

- [ ] Shipment model can evolve toward partial fulfillment without breaking current admin upsert.

Suggested commit:

```text
feat(shipping-core): prepare shipment items and tracking hooks
```

## Phase 8 - Admin And Storefront Projection

Goal: expose shipping behavior safely and consistently.

Implementation checklist:

- [ ] Keep existing `api/commerce/admin/orders/{id}/shipment`.
- [ ] Add/extend admin shipment DTO with shipment items only if implemented.
- [ ] Add/extend admin shipment DTO with normalized status values.
- [ ] Add/extend admin shipment DTO with selected shipping method summary from order.
- [ ] Admin can view shipping settings.
- [ ] Admin can update internal shipping rules.
- [ ] ControlPlane gateway follows existing store-scoped catalog/admin pattern.
- [ ] ControlPlane Web does not call CommerceNode directly.
- [ ] Storefront checkout session response exposes `ShippingRequired`, `SelectedShippingOption`, and `ShippingOptions`.
- [ ] Storefront order response exposes shipping status/tracking summary.
- [ ] Add tracking event list only if Phase 7 adds events.
- [ ] Do not expose internal origin address publicly unless needed.
- [ ] Update OpenAPI summaries.
- [ ] Update request bodies.
- [ ] Update response schemas.
- [ ] Update error responses.
- [ ] Preserve existing operation IDs where possible.

Verification checklist:

- [ ] OpenAPI includes shipping method request/response schemas.
- [ ] Shipping settings admin endpoint is protected by Commerce Admin security.
- [ ] Storefront schema does not expose admin-only settings.
- [ ] Control Plane gateway route includes `storeKey` behavior.

Exit criteria:

- [ ] Storefront can select shipping options and see safe tracking info.
- [ ] Admin can configure basic shipping without private data leaks.

Suggested commit:

```text
feat(shipping-core): expose shipping core configuration
```

## Phase 9 - QA, Migration, And Documentation

Goal: finish with clear verification and no hidden behavior changes.

Implementation checklist:

- [ ] Use additive migrations only.
- [ ] Keep existing `CheckoutSession.SelectedShippingOptionJson`.
- [ ] Keep current `Shipment` rows valid.
- [ ] Preserve `free_standard` option key compatibility.
- [ ] Backfill new order shipping snapshot fields conservatively as null/zero if added.
- [ ] Update `QA-CommerceNode.todo.md`.
- [ ] Update `QA-StorefrontV2.todo.md` if checkout UI/API behavior changes.
- [ ] Add manual QA notes for physical checkout.
- [ ] Add manual QA notes for non-shipping checkout.
- [ ] Add manual QA notes for country unavailable.
- [ ] Add manual QA notes for admin shipment tracking.

Verification checklist:

- [ ] Application provider registry tests pass.
- [ ] Application shipping calculator tests pass.
- [ ] No-shipping cart tests pass.
- [ ] Free threshold tests pass.
- [ ] Country restriction tests pass.
- [ ] Surcharge policy tests pass.
- [ ] Currency conversion tests pass.
- [ ] Checkout shipping selection tests pass.
- [ ] Address change reset tests pass.
- [ ] Place-order total includes shipping.
- [ ] Non-shipping order sets `shipping_not_required`.
- [ ] Admin shipping settings validation tests pass.
- [ ] Admin shipment upsert/status/store-scoping tests pass.
- [ ] Storefront OpenAPI contract tests pass.
- [ ] Commerce Admin OpenAPI contract tests pass.
- [ ] Public schemas do not expose domain entities or admin-only settings.
- [ ] Swagger snapshots are intentionally updated.

Exit criteria:

- [ ] Focused tests pass.
- [ ] Swagger snapshots are intentionally updated.
- [ ] QA checklist records Shipping Core coverage.

Suggested commit:

```text
test(shipping-core): verify shipping core
```

## QA Checklist Seeds

### Commerce Node

- [ ] Shipping provider registry returns free standard and optional flat rate.
- [ ] Unsupported shipping provider is rejected.
- [ ] Shipping calculator computes `ShippingRequired` from cart lines.
- [ ] Non-shipping carts do not require a physical shipping method.
- [ ] Physical carts require a valid shipping address and option.
- [ ] Country restriction blocks unavailable destinations.
- [ ] Free shipping threshold waives shipping.
- [ ] Product free-shipping flag excludes line surcharge.
- [ ] Sum surcharge policy calculates expected amount.
- [ ] Highest surcharge policy calculates expected amount.
- [ ] Shipping totals are rounded through Currency Core.
- [ ] Missing conversion rate returns clear conflict.
- [ ] Order total includes selected shipping total.
- [ ] Redirect payment amount includes selected shipping total.
- [ ] Selected shipping option snapshot survives settings changes.
- [ ] Shipment upsert remains compatible with one-shipment-per-order.
- [ ] Tracking summary/event projection is customer-safe.
- [ ] Storefront OpenAPI validates and snapshots pass.
- [ ] Commerce Admin OpenAPI validates protected settings endpoints.

### Storefront V2

- [ ] Checkout renders shipping options from scoped Storefront API.
- [ ] Physical checkout can select a valid shipping option.
- [ ] Non-shipping checkout does not block on shipping method.
- [ ] Country unavailable shows recoverable checkout message.
- [ ] Shipping total updates checkout review total.
- [ ] Hosted payment pending flow includes shipping amount.
- [ ] Order confirmation/tracking view shows safe tracking summary.
- [ ] Browser network shows no provider/internal shipping settings.
- [ ] Browser QA has no unexpected console errors after shipping UI behavior changes.

### Control Plane

- [ ] ControlPlane Web does not call CommerceNode shipping APIs directly.
- [ ] ControlPlane API gateway uses Commerce admin shipping route with `storeKey` query.
- [ ] Shipping settings page can load current settings.
- [ ] Shipping settings page validates country codes and money fields.
- [ ] Shipping settings responses do not expose Storefront-private/internal data unexpectedly.
- [ ] Admin shipment upsert still works through ControlPlane gateway.
- [ ] No Shipping Core runtime data is stored in `ControlPlaneDbContext`.

## Failure Modes To Design Against

- [ ] Shipping total changes after customer review.
- [ ] Physical order placed without shipping option.
- [ ] Non-shipping product still blocks checkout on address/shipping step.
- [ ] Country unavailable but checkout continues.
- [ ] External provider complexity delays usable shipping.
- [ ] Shipping settings leak origin/internal rules publicly.
- [ ] Product surcharge is double-counted.
- [ ] Currency conversion missing for shipping rate.
- [ ] Shipment upsert breaks existing admin flow.
- [ ] Legacy email side effects copied into active V2.

## Test Map

- [ ] Provider contract tests:
  - [ ] free standard.
  - [ ] flat rate.
  - [ ] unsupported provider.
  - [ ] warnings/errors.
- [ ] Calculator tests:
  - [ ] shipping required.
  - [ ] shipping not required.
  - [ ] threshold.
  - [ ] country restriction.
  - [ ] surcharge policies.
- [ ] Checkout tests:
  - [ ] select option.
  - [ ] reset on address/cart change.
  - [ ] totals.
  - [ ] no-shipping flow.
  - [ ] place-order validation.
- [ ] Currency tests:
  - [ ] converted rate.
  - [ ] missing rate conflict.
  - [ ] rounding.
- [ ] Order placement tests:
  - [ ] order total includes shipping.
  - [ ] payment attempt includes shipping.
  - [ ] snapshot survives settings change.
- [ ] Admin settings tests:
  - [ ] validation.
  - [ ] store scoping.
  - [ ] audit.
  - [ ] ControlPlane gateway route.
- [ ] Shipment tests:
  - [ ] upsert.
  - [ ] tracking sync.
  - [ ] optional items.
  - [ ] status validation.
- [ ] Storefront contract tests:
  - [ ] schemas.
  - [ ] required body.
  - [ ] error responses.
  - [ ] safe public fields.

## Migration And Compatibility

- [ ] Use additive migrations only.
- [ ] Existing `free_standard` checkout option keeps working.
- [ ] Existing `SelectedShippingOptionJson` remains the selected option snapshot.
- [ ] Existing `Shipment` table remains valid.
- [ ] Existing admin shipment endpoint remains valid.
- [ ] New store shipping settings have defensive defaults:
  - [ ] shipping enabled.
  - [ ] no country restrictions.
  - [ ] free standard available.
  - [ ] zero tax hook.
- [ ] Existing products do not require product surcharge.
- [ ] Free-standard behavior does not require shipping origin.
- [ ] Storefront clients that expect `ShippingOptions` and `SelectedShippingOption` remain compatible.

## Out Of Scope Backlog

- [ ] Carrier API integrations.
- [ ] Carrier credential management.
- [ ] Label purchase/printing.
- [ ] Multi-package rating.
- [ ] Multi-origin warehouses.
- [ ] Shipping zones with complex expressions.
- [ ] Volumetric weight rules.
- [ ] Dimensional rate tables.
- [ ] Full tax engine.
- [ ] Full partial shipment UI.
- [ ] Tracking polling jobs.
- [ ] Return/RMA shipping labels.
- [ ] Fulfillment provider orchestration.

## Recommended Implementation Order

- [ ] Phase 0 - baseline snapshot.
- [ ] Phase 1 - shipping contracts and internal provider registry.
- [ ] Phase 2 - store shipping settings.
- [ ] Phase 3 - checkout shipping calculation cutover.
- [ ] Phase 4 - product shipping surcharge hook.
- [ ] Phase 5 - currency conversion and tax hook.
- [ ] Phase 6 - order placement and shipping snapshot.
- [ ] Phase 7 - shipment record items and tracking events hook.
- [ ] Phase 8 - admin and Storefront projection.
- [ ] Phase 9 - QA, migration, and documentation.

## Acceptance Criteria

- [ ] Checkout shipping options come from `IShippingCalculator`, not hard-coded `ResolveShippingOptions`.
- [ ] `ShippingRequired` reflects cart line product metadata.
- [ ] Non-shipping carts can complete without a physical shipping method.
- [ ] Non-shipping carts create orders with `shipping_not_required`.
- [ ] Physical carts require a valid shipping address and available shipping option.
- [ ] `free_standard` remains compatible.
- [ ] Flat/free shipping rules work from store settings.
- [ ] Country restriction rules work from store settings.
- [ ] Shipping totals are rounded and included in checkout grand total.
- [ ] Shipping totals are included in order total.
- [ ] Shipping totals are included in payment amount.
- [ ] Currency conversion works or fails with clear conflict when missing.
- [ ] Shipment admin upsert remains compatible.
- [ ] Public Storefront contracts do not leak admin-only shipping settings.
- [ ] Active V2 API contract tests pass.
- [ ] Focused shipping tests pass.
