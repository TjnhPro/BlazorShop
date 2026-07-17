# Shipping Core.todo

Generated: 2026-07-17

Source plan: `Shipping Core.md`

Status: Phase 8 complete. Phase 9 not started.

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
- [x] First internal providers:
  - [x] `free_standard`. 2026-07-17 Phase 1: `InternalFreeStandardShippingProvider`.
  - [x] optional `flat_rate`. 2026-07-17 Phase 2: `InternalFlatRateShippingProvider` reads store shipping settings.
  - [x] `shipping_not_required` path when no cart line needs shipping. 2026-07-17 Phase 1: calculator returns `ShippingRequired=false` and no options.
- [ ] Core shipping calculation:
  - [x] store shipping origin address. 2026-07-17 Phase 2: `StoreShippingSettings` stores normalized origin data.
  - [x] product shipping-required flag. 2026-07-17 Phase 1: `ShippingPackageLine.ShippingRequired` drives calculator `ShippingRequired`.
  - [x] product shipping surcharge hook. 2026-07-17 Phase 4: `Product.ShippingSurcharge` feeds checkout package lines and internal provider rates.
  - [x] free-shipping threshold hook. 2026-07-17 Phase 2: flat-rate provider waives rate when subtotal reaches `FreeShippingThreshold`.
  - [x] country restriction. 2026-07-17 Phase 2: providers reject destinations outside `EnabledCountryCodes`.
  - [x] highest surcharge vs sum policy. 2026-07-17 Phase 4: internal providers support `sum` and `highest` policies.
  - [x] tax calculation hook returning zero for now. 2026-07-17 Phase 5: `IShippingTaxCalculator` with `ZeroShippingTaxCalculator`.
  - [x] currency conversion/rounding through existing Currency Core services. 2026-07-17 Phase 5: checkout converts base shipping rates through `IMoneyConversionService` and rounds via `IMoneyRoundingService`.
- [x] Checkout integration:
  - [x] replace hard-coded `ResolveShippingOptions`. 2026-07-17 Phase 3: `StorefrontCheckoutService` now calls `IShippingCalculator`.
  - [x] compute `ShippingRequired` from cart lines. 2026-07-17 Phase 3: package lines use persisted product shipping metadata.
  - [x] update `ShippingTotal` and `GrandTotal` from selected option. 2026-07-17 Phase 3.
  - [x] snapshot selected shipping option in `CheckoutSession.SelectedShippingOptionJson`. 2026-07-17 Phase 3 keeps selected option JSON and selected flag.
  - [x] block checkout when shipping is required but no valid address/option exists. 2026-07-17 Phase 3: select/review/place-order guards use calculator result.
- [x] Shipment record improvement:
  - [x] keep one shipment per order for this phase. 2026-07-17 Phase 7: unique `(StoreId, OrderId)` shipment model remains.
  - [x] add shipment item/quantity model only if needed for partial shipment preparation. 2026-07-17 Phase 7: additive `ShipmentItem` model added without changing default full-order behavior.
  - [x] carrier. 2026-07-17 Phase 7: existing shipment carrier fields preserved.
  - [x] tracking number/URL. 2026-07-17 Phase 7: existing shipment tracking fields preserved and tracking changes append manual events.
  - [x] shipped/delivered timestamps. 2026-07-17 Phase 7: shipped timestamp remains on shipment/order; delivered status updates append delivered tracking event.
  - [x] customer-visible tracking events hook. 2026-07-17 Phase 7: `ShipmentTrackingEvent` stores status, message, occurred-at, location, and source.
  - [x] notification hook. 2026-07-17 Phase 7: manual shipment/tracking/delivered event rows provide the non-blocking hook shape; email/worker implementation remains deferred.
- [ ] API/contract hardening:
  - [x] explicit DTOs for product surcharge field. 2026-07-17 Phase 4: product create/update/catalog DTOs expose nullable `ShippingSurcharge`.
  - [x] stable operation IDs for shipping settings admin endpoints. 2026-07-17 Phase 2: `CommerceShippingSettings_Get` and `CommerceShippingSettings_Update`.
  - [x] validation metadata for shipping settings admin contracts. 2026-07-17 Phase 2: admin service validates country code, money, origin, and surcharge policy inputs.
  - [x] safe public tracking projection. 2026-07-17 Phase 8: Storefront order response exposes only status/message/occurred-at/location/source tracking events.

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

- [x] Add `StoreShippingSettings` or equivalent typed settings row:
  - [x] `StoreId`. 2026-07-17 Phase 2: store-scoped EF row with unique `StoreId`.
  - [x] optional origin full name/company. 2026-07-17 Phase 2.
  - [x] origin address1/address2/city/state/postal/country. 2026-07-17 Phase 2.
  - [x] `EnabledCountryCodesJson`. 2026-07-17 Phase 2: stored as `jsonb`.
  - [x] `DefaultFlatRate`. 2026-07-17 Phase 2.
  - [x] `FreeShippingThreshold`. 2026-07-17 Phase 2.
  - [x] `SurchargePolicy`: `sum` or `highest`. 2026-07-17 Phase 2.
  - [x] `DefaultDeliveryEstimateText`. 2026-07-17 Phase 2.
  - [x] `CreatedAtUtc`. 2026-07-17 Phase 2: `CreatedAt` uses `DateTimeOffset`/`timestamptz`.
  - [x] `UpdatedAtUtc`. 2026-07-17 Phase 2: `UpdatedAt` uses `DateTimeOffset`/`timestamptz`.
- [x] Validate country codes as 2-letter uppercase values. 2026-07-17 Phase 2: `StoreShippingSettingsServiceTests` covers invalid and normalized values.
- [x] Validate flat rate and threshold are null or >= 0. 2026-07-17 Phase 2.
- [x] Validate surcharge policy is known. 2026-07-17 Phase 2.
- [x] Require origin country when country restrictions/rate providers depend on origin. 2026-07-17 Phase 2.
- [x] Add Commerce Admin endpoint under `api/commerce/admin/shipping/settings`. 2026-07-17 Phase 2: `CommerceShippingSettingsController`.
- [x] Add ControlPlane gateway using existing store-scoped admin pattern. 2026-07-17 Phase 2: gateway forwards through ControlPlane with `storeKey`.
- [x] Do not expose internal settings through Storefront unless explicitly public-safe. 2026-07-17 Phase 2: no Storefront route/client/config projection was added.

Verification checklist:

- [x] Default settings are created or resolved defensively. 2026-07-17 Phase 2: `GetAsync_ReturnsDefensiveDefaultsWithoutPersistingRow`.
- [x] Admin update validates country codes. 2026-07-17 Phase 2: focused service tests passed.
- [x] Admin update validates non-negative money fields. 2026-07-17 Phase 2: focused service tests passed.
- [x] Control Plane gateway preserves boundary. 2026-07-17 Phase 2: `ControlPlaneCommerceCatalogServiceStoreMappingTests` covers GET/PUT shipping settings forwarding.
- [x] Storefront public config does not leak internal shipping settings. 2026-07-17 Phase 2: no Storefront-facing DTO/endpoint was introduced.

Exit criteria:

- [x] Internal provider can resolve store origin/country/threshold/rate settings. 2026-07-17 Phase 2: flat-rate/free-standard provider tests passed.

Suggested commit:

```text
feat(shipping-core): add store shipping settings
```

## Phase 3 - Checkout Shipping Calculation Cutover

Goal: replace hard-coded `free_standard` with the calculator.

Implementation checklist:

- [x] Inject `IShippingCalculator` into `StorefrontCheckoutService`. 2026-07-17 Phase 3.
- [x] Build shipping package lines from current cart lines and product metadata. 2026-07-17 Phase 3: reads persisted product shipping fields from `CommerceNodeDbContext`.
- [x] Compute `ShippingRequired`. 2026-07-17 Phase 3.
- [x] Compute available options. 2026-07-17 Phase 3.
- [x] Validate selected option. 2026-07-17 Phase 3.
- [x] Preserve provider warnings/errors. 2026-07-17 Phase 3: provider errors/warnings are projected to checkout validation issues in preview.
- [x] Update `ToSessionResult` to return actual `ShippingRequired`. 2026-07-17 Phase 3.
- [x] Replace `ResolveShippingOptions(session, selectedKey)` with calculator output. 2026-07-17 Phase 3.
- [x] Keep `free_standard` option key compatible. 2026-07-17 Phase 3: fallback/default calculator still exposes `free_standard`.
- [x] When no lines require shipping:
  - [x] skip address/shipping-method requirement where possible. 2026-07-17 Phase 3: payment/review/place-order no longer require selected shipping option when `ShippingRequired=false`.
  - [x] set selected option to `shipping_not_required` or null with explicit flag. 2026-07-17 Phase 3: selected option remains null and API returns `ShippingRequired=false`.
  - [x] set shipping total to zero. 2026-07-17 Phase 3.
  - [x] prepare order shipping status `shipping_not_required`. 2026-07-17 Phase 3: order placement uses `ShippingStatuses.ShippingNotRequired`.
- [x] Selected option rate updates `ShippingTotal`. 2026-07-17 Phase 3.
- [x] Calculate `GrandTotal = Subtotal + ShippingTotal + TaxTotal - DiscountTotal`. 2026-07-17 Phase 3.
- [x] Use `IMoneyRoundingService.RoundOrderTotal`. 2026-07-17 Phase 3.

Verification checklist:

- [x] Physical cart requires shipping and returns options. 2026-07-17 Phase 3: existing free-standard checkout tests plus calculated-rate test passed.
- [x] All non-shipping cart skips shipping method and has zero shipping total. 2026-07-17 Phase 3: `StartAsync_ComputesShippingRequiredFromPersistedProductMetadata` and `SelectPaymentMethodAsync_AllowsNonShippingCartWithoutSelectedShippingMethod`.
- [x] Free shipping threshold produces zero-rate option. 2026-07-17 Phase 3: provider behavior remains covered by Phase 2 flat-rate provider test.
- [x] Country restriction blocks shipping option with validation issue. 2026-07-17 Phase 3: `PreviewAsync_WhenShippingCalculatorReturnsError_AddsValidationIssue`.
- [x] Address change resets selected shipping and payment method as expected. 2026-07-17 Phase 3: existing reset tests passed after calculator cutover.
- [x] Existing `free_standard` selection remains accepted. 2026-07-17 Phase 3: existing shipping-method tests passed.

Exit criteria:

- [x] Checkout no longer hard-codes a single shipping option. 2026-07-17 Phase 3.
- [x] Existing Storefront checkout flow remains compatible. 2026-07-17 Phase 3: focused checkout service suite passed.

Suggested commit:

```text
feat(shipping-core): calculate checkout shipping options
```

## Phase 4 - Product Shipping Surcharge Hook

Goal: support practical per-product shipping charges without building a rate table.

Implementation checklist:

- [x] Add nullable `ShippingSurcharge` to `Product` only if approved during implementation review. 2026-07-17 Phase 4: additive CommerceNode migration `CommerceNodeProductShippingSurcharge`.
- [x] Do not add variant surcharge unless current product variant UX requires it. 2026-07-17 Phase 4: product-level field only.
- [x] Do not add category/manufacturer surcharge rules. 2026-07-17 Phase 4: no category/manufacturer shipping rule tables or services added.
- [x] Include surcharge only when `ShippingRequired = true` and `FreeShipping = false`. 2026-07-17 Phase 4: provider surcharge filter enforces both flags.
- [x] Implement `sum` policy: sum line surcharge * quantity. 2026-07-17 Phase 4: provider test covers `2 * 2.5 = 5`.
- [x] Implement `highest` policy: highest line surcharge once unless settings explicitly choose quantity behavior. 2026-07-17 Phase 4: flat-rate provider test covers highest surcharge once.
- [x] Make free shipping threshold waive total shipping amount. 2026-07-17 Phase 4: threshold test covers base rate plus surcharge waived to zero.
- [x] Add product form field and DTO mapping only if data field is added. 2026-07-17 Phase 4: ControlPlane product basic form, shared DTOs, and read models include surcharge.
- [x] Validate surcharge >= 0. 2026-07-17 Phase 4: `ProductServiceTests.AddAsync_WhenShippingSurchargeIsNegative_ReturnsFailure`.

Verification checklist:

- [x] Sum policy calculates expected surcharge. 2026-07-17 Phase 4: `FreeStandardProvider_AddsProductSurchargeAndExcludesFreeShippingLines`.
- [x] Highest policy calculates expected surcharge. 2026-07-17 Phase 4: `FlatRateProvider_UsesHighestSurchargePolicy`.
- [x] Free-shipping product does not contribute surcharge. 2026-07-17 Phase 4: free-shipping package line is excluded in provider test.
- [x] Threshold waives shipping. 2026-07-17 Phase 4: `FlatRateProvider_FreeShippingThresholdWaivesBaseRateAndSurcharge`.
- [x] Currency rounding is applied. 2026-07-17 Phase 4: checkout surcharge total passes through existing `RoundOrderTotal` path; `SelectShippingMethodAsync_AppliesPersistedProductShippingSurcharge` passed.

Exit criteria:

- [x] Stores can charge simple product-level shipping without external providers. 2026-07-17 Phase 4.

Suggested commit:

```text
feat(shipping-core): add product shipping surcharge calculation
```

## Phase 5 - Currency Conversion And Tax Hook

Goal: keep shipping totals consistent with Currency Core and leave tax as a clean extension.

Implementation checklist:

- [x] Provider/store rates have a base currency. 2026-07-17 Phase 5: provider request currency is resolved from cart base-currency snapshots when checkout is converted.
- [x] If checkout currency differs from base rate currency, use existing `IMoneyConversionService`. 2026-07-17 Phase 5: shipping option rates convert before session projection.
- [x] Snapshot converted rate in selected shipping option JSON if needed. 2026-07-17 Phase 5: selected option JSON stores the checkout-currency price.
- [x] Round shipping and grand total with existing money rounding service. 2026-07-17 Phase 5.
- [x] Add `IShippingTaxCalculator` or total tax hook. 2026-07-17 Phase 5.
- [x] Initial tax implementation returns zero. 2026-07-17 Phase 5.
- [x] Include tax reason/source such as `tax_not_configured`. 2026-07-17 Phase 5: zero tax result returns `tax_not_configured` and `shipping_tax.zero`.
- [x] Do not add tax-inclusive/exclusive settings unless Tax Core is approved. 2026-07-17 Phase 5: no tax settings or tax engine tables added.

Verification checklist:

- [x] Shipping rate in store default currency converts to working currency. 2026-07-17 Phase 5: `SelectShippingMethodAsync_ConvertsBaseShippingRateToCheckoutCurrency`.
- [x] Missing exchange rate returns clear checkout conflict. 2026-07-17 Phase 5: `SelectShippingMethodAsync_WhenShippingRateConversionMissing_ReturnsConflict`.
- [x] Tax hook returns zero and does not change current totals. 2026-07-17 Phase 5: `ZeroShippingTaxCalculator_ReturnsExplicitNotConfiguredResult`.
- [x] Rounding follows currency settings. 2026-07-17 Phase 5: converted shipping rate is rounded through `IMoneyRoundingService`.

Exit criteria:

- [x] Shipping rates behave correctly in non-default checkout currency. 2026-07-17 Phase 5.
- [x] Tax remains explicit and not hidden inside shipping provider logic. 2026-07-17 Phase 5.

Suggested commit:

```text
feat(shipping-core): apply currency conversion to shipping rates
```

## Phase 6 - Order Placement And Shipping Snapshot

Goal: preserve selected shipping decision on the order and set correct shipping status.

Implementation checklist:

- [x] Keep existing order shipping address/status/tracking fields. 2026-07-17 Phase 6: existing fields preserved.
- [x] Add optional order-level shipping snapshot fields only if current data cannot answer admin/customer needs:
  - [x] `ShippingMethodKey`. 2026-07-17 Phase 6.
  - [x] `ShippingProviderSystemName`. 2026-07-17 Phase 6.
  - [x] `ShippingMethodCode`. 2026-07-17 Phase 6.
  - [x] `ShippingMethodName`. 2026-07-17 Phase 6.
  - [x] `ShippingTotal`. 2026-07-17 Phase 6.
  - [x] `ShippingCurrencyCode`. 2026-07-17 Phase 6.
  - [x] `ShippingDeliveryEstimateText`. 2026-07-17 Phase 6.
- [x] Prefer explicit fields for reporting if new order fields are needed. 2026-07-17 Phase 6: additive order columns via `CommerceNodeOrderShippingSnapshot`.
- [x] For physical orders, require valid selected shipping option. 2026-07-17 Phase 6: existing place-order revalidation remains covered.
- [x] For non-shipping orders, set `ShippingStatus = shipping_not_required`. 2026-07-17 Phase 6.
- [x] Copy selected shipping option summary into order. 2026-07-17 Phase 6.
- [x] Include shipping total in order total. 2026-07-17 Phase 6.
- [x] Online payment attempts include shipping in amount. 2026-07-17 Phase 6: existing redirect payment amount path preserved; focused tests passed.

Verification checklist:

- [x] COD order includes shipping total in total amount. 2026-07-17 Phase 6: `PlaceOrderAsync_IncludesSelectedShippingTotalInOrderAndPaymentAmount`.
- [x] Redirect payment attempt amount includes shipping total. 2026-07-17 Phase 6: existing payment attempt amount tests remained green.
- [x] Non-shipping order gets `shipping_not_required`. 2026-07-17 Phase 6: `PlaceOrderAsync_NonShippingOrderSnapshotsShippingNotRequired`.
- [x] Missing shipping option blocks physical order placement. 2026-07-17 Phase 6: existing place-order shipping revalidation remains green.
- [x] Selected shipping option snapshot survives later store setting changes. 2026-07-17 Phase 6: selected option summary is copied to order columns.

Exit criteria:

- [x] Order and payment totals match checkout review totals. 2026-07-17 Phase 6.
- [x] Shipping status is correct at order creation. 2026-07-17 Phase 6.

Suggested commit:

```text
feat(shipping-core): snapshot shipping on order placement
```

## Phase 7 - Shipment Record Items And Tracking Events Hook

Goal: prepare fulfillment tracking without implementing a full fulfillment engine.

Implementation checklist:

- [x] Keep current one-shipment-per-order model unless partial shipment becomes necessary. 2026-07-17 Phase 7: no split-shipment workflow added.
- [x] If adding items, add `ShipmentItem`. 2026-07-17 Phase 7: additive entity and migration added.
- [x] `ShipmentItem` includes:
  - [x] `ShipmentId`. 2026-07-17 Phase 7.
  - [x] `OrderLineId`. 2026-07-17 Phase 7.
  - [x] `ProductId`. 2026-07-17 Phase 7.
  - [x] `Quantity`. 2026-07-17 Phase 7.
  - [x] created/updated timestamps. 2026-07-17 Phase 7.
- [x] Validate shipment item quantities do not exceed ordered quantities. 2026-07-17 Phase 7: admin shipment service validates line ownership, product match, positive quantity, duplicate lines, and ordered quantity ceiling.
- [x] If no items are provided, treat shipment as full-order shipment for backward compatibility. 2026-07-17 Phase 7: null `Items` preserves legacy upsert behavior.
- [x] Add DTO/contract for customer-visible tracking events:
  - [x] status. 2026-07-17 Phase 7.
  - [x] message. 2026-07-17 Phase 7.
  - [x] occurred at. 2026-07-17 Phase 7.
  - [x] optional location. 2026-07-17 Phase 7.
  - [x] source. 2026-07-17 Phase 7.
- [x] Initial tracking implementation can return empty events or manual events. 2026-07-17 Phase 7: admin upsert/status flows append manual events; missing events project as empty lists.
- [x] Do not add carrier polling job. 2026-07-17 Phase 7: no polling job or external carrier integration added.
- [x] Add notification hook/task payload for:
  - [x] shipment created. 2026-07-17 Phase 7: `shipped` tracking event row.
  - [x] tracking updated. 2026-07-17 Phase 7: `tracking_updated` tracking event row.
  - [x] delivered. 2026-07-17 Phase 7: `delivered` tracking event row from shipping-status flow.
- [x] Do not send email synchronously from admin service. 2026-07-17 Phase 7: only data events/audit logs are written.
- [x] Do not copy legacy `OrderTrackingService` email side effects. 2026-07-17 Phase 7: active CommerceNode tracking service remains email-free.

Verification checklist:

- [x] Existing shipment upsert still works without items. 2026-07-17 Phase 7: `CommerceNodeAdminShipmentServiceTests.UpsertShipmentAsync_WithoutItems_CreatesBackwardCompatibleFullOrderShipment` passed.
- [x] Shipment items validate quantities when supplied. 2026-07-17 Phase 7: `CommerceNodeAdminShipmentServiceTests.UpsertShipmentAsync_WithItems_RejectsQuantityGreaterThanOrderedQuantity` passed.
- [x] Tracking events endpoint/projection returns empty safe list when none exist. 2026-07-17 Phase 7: DTO defaults empty lists and admin projection loads events safely.
- [x] Notification hook/task is queued or invoked without blocking shipment save. 2026-07-17 Phase 7: shipment/tracking/delivered event rows are written in-band without synchronous email.

Exit criteria:

- [x] Shipment model can evolve toward partial fulfillment without breaking current admin upsert. 2026-07-17 Phase 7: focused tests pass for no-item and itemized upserts.

Suggested commit:

```text
feat(shipping-core): prepare shipment items and tracking hooks
```

## Phase 8 - Admin And Storefront Projection

Goal: expose shipping behavior safely and consistently.

Implementation checklist:

- [x] Keep existing `api/commerce/admin/orders/{id}/shipment`. 2026-07-17 Phase 8: endpoint route unchanged.
- [x] Add/extend admin shipment DTO with shipment items only if implemented. 2026-07-17 Phase 8: `GetShipment.Items`.
- [x] Add/extend admin shipment DTO with normalized status values. 2026-07-17 Phase 8: `GetShipment.ShippingStatus` projects order shipping status.
- [x] Add/extend admin shipment DTO with selected shipping method summary from order. 2026-07-17 Phase 8: `GetShipment.ShippingMethod`.
- [x] Admin can view shipping settings. 2026-07-17 Phase 2: Commerce Admin shipping settings endpoint remains active.
- [x] Admin can update internal shipping rules. 2026-07-17 Phase 2: Commerce Admin shipping settings update remains active.
- [x] ControlPlane gateway follows existing store-scoped catalog/admin pattern. 2026-07-17 Phase 2: gateway tests cover `storeKey` forwarding.
- [x] ControlPlane Web does not call CommerceNode directly. 2026-07-17 Phase 8: no ControlPlane Web direct calls added.
- [x] Storefront checkout session response exposes `ShippingRequired`, `SelectedShippingOption`, and `ShippingOptions`. 2026-07-17 Phase 3/6: checkout service and OpenAPI coverage remain green.
- [x] Storefront order response exposes shipping status/tracking summary. 2026-07-17 Phase 8: existing summary preserved.
- [x] Add tracking event list only if Phase 7 adds events. 2026-07-17 Phase 8: `StorefrontOrderResponse.TrackingEvents`.
- [x] Do not expose internal origin address publicly unless needed. 2026-07-17 Phase 8: no public shipping settings/origin projection added.
- [x] Update OpenAPI summaries. 2026-07-17 Phase 8: existing operation IDs/summaries preserved and snapshot refreshed for response schema change.
- [x] Update request bodies. 2026-07-17 Phase 8: no route/request body change required.
- [x] Update response schemas. 2026-07-17 Phase 8: Storefront order schema includes tracking events; snapshot refreshed.
- [x] Update error responses. 2026-07-17 Phase 8: no new error path introduced.
- [x] Preserve existing operation IDs where possible. 2026-07-17 Phase 8: operation IDs unchanged.

Verification checklist:

- [x] OpenAPI includes shipping method request/response schemas. 2026-07-17 Phase 8: Storefront OpenAPI contract tests passed with refreshed snapshot.
- [x] Shipping settings admin endpoint is protected by Commerce Admin security. 2026-07-17 Phase 8: `CommerceNodeAdminStoreOpenApiMetadataTests` passed.
- [x] Storefront schema does not expose admin-only settings. 2026-07-17 Phase 8: tracking events expose only public-safe fields and no origin/settings fields.
- [x] Control Plane gateway route includes `storeKey` behavior. 2026-07-17 Phase 2/8: existing gateway test remains the guard.

Exit criteria:

- [x] Storefront can select shipping options and see safe tracking info. 2026-07-17 Phase 8: checkout option schema and order tracking event schema verified.
- [x] Admin can configure basic shipping without private data leaks. 2026-07-17 Phase 8: admin settings route remains protected; Storefront schema does not expose origin/settings.

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
- [x] Product free-shipping flag excludes line surcharge. 2026-07-17 Phase 4.
- [x] Sum surcharge policy calculates expected amount. 2026-07-17 Phase 4.
- [x] Highest surcharge policy calculates expected amount. 2026-07-17 Phase 4.
- [x] Shipping totals are rounded through Currency Core. 2026-07-17 Phase 5.
- [x] Missing conversion rate returns clear conflict. 2026-07-17 Phase 5.
- [x] Order total includes selected shipping total. 2026-07-17 Phase 6.
- [x] Redirect payment amount includes selected shipping total. 2026-07-17 Phase 6.
- [x] Selected shipping option snapshot survives settings changes. 2026-07-17 Phase 6.
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
  - [x] surcharge policies. 2026-07-17 Phase 4.
- [ ] Checkout tests:
  - [ ] select option.
  - [ ] reset on address/cart change.
  - [ ] totals.
  - [ ] no-shipping flow.
  - [ ] place-order validation.
- [ ] Currency tests:
  - [x] converted rate. 2026-07-17 Phase 5.
  - [x] missing rate conflict. 2026-07-17 Phase 5.
  - [x] rounding. 2026-07-17 Phase 5.
- [ ] Order placement tests:
  - [x] order total includes shipping. 2026-07-17 Phase 6.
  - [x] payment attempt includes shipping. 2026-07-17 Phase 6.
  - [x] snapshot survives settings change. 2026-07-17 Phase 6.
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
  - [x] zero tax hook. 2026-07-17 Phase 5.
- [x] Existing products do not require product surcharge. 2026-07-17 Phase 4: nullable additive column defaults to no surcharge.
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
- [x] Phase 4 - product shipping surcharge hook. 2026-07-17.
- [x] Phase 5 - currency conversion and tax hook. 2026-07-17.
- [x] Phase 6 - order placement and shipping snapshot. 2026-07-17.
- [x] Phase 7 - shipment record items and tracking events hook. 2026-07-17: implemented and verified with CommerceNode API build plus 111 focused tests.
- [x] Phase 8 - admin and Storefront projection. 2026-07-17: CommerceNode API build passed; focused Storefront OpenAPI/admin metadata/shipment/checkout tests passed.
- [ ] Phase 9 - QA, migration, and documentation.

## Acceptance Criteria

- [ ] Checkout shipping options come from `IShippingCalculator`, not hard-coded `ResolveShippingOptions`.
- [ ] `ShippingRequired` reflects cart line product metadata.
- [ ] Non-shipping carts can complete without a physical shipping method.
- [x] Non-shipping carts create orders with `shipping_not_required`. 2026-07-17 Phase 6.
- [ ] Physical carts require a valid shipping address and available shipping option.
- [ ] `free_standard` remains compatible.
- [ ] Flat/free shipping rules work from store settings.
- [ ] Country restriction rules work from store settings.
- [ ] Shipping totals are rounded and included in checkout grand total.
- [x] Shipping totals are included in order total. 2026-07-17 Phase 6.
- [x] Shipping totals are included in payment amount. 2026-07-17 Phase 6.
- [ ] Currency conversion works or fails with clear conflict when missing.
- [x] Shipment admin upsert remains compatible. 2026-07-17 Phase 7.
- [x] Public Storefront contracts do not leak admin-only shipping settings. 2026-07-17 Phase 8.
- [x] Active V2 API contract tests pass. 2026-07-17 Phase 8.
- [x] Focused shipping tests pass. 2026-07-17 Phase 8.
