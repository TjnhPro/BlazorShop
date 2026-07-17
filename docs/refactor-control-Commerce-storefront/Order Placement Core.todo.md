# Order Placement Core.todo

Generated: 2026-07-17

Source plan: `Order Placement Core.md`

Status: Phase 1 complete. Phase 2 not started.

Scope: turn the existing Commerce Node order placement flow from a working checkout MVP into a practical order core. The goal is stable historical order snapshots, safe placement guarantees, and enough order state history for real store operations without adding a full OMS, tax engine, discount engine, stock reservation ledger, invoice/accounting system, or fulfillment platform.

## Scope Lock

Approved:

- [ ] Order snapshot hardening:
  - [ ] store reference and store snapshot.
  - [ ] customer/guest reference.
  - [ ] billing address snapshot copied from checkout.
  - [ ] shipping address snapshot kept and normalized.
  - [ ] order item snapshot with product, variant, selected attributes, personalization, quantity, unit price, line total, and currency.
  - [ ] order total breakdown: subtotal, shipping total, tax total, discount total, grand total.
  - [ ] currency and exchange-rate snapshot.
  - [ ] payment method reference.
  - [ ] shipping method/option snapshot.
- [ ] Order identity:
  - [ ] keep internal `Order.Id`.
  - [ ] keep public `Order.Reference`.
  - [ ] created/updated timestamps.
  - [ ] customer-facing guest completion access token stored hashed.
- [ ] Order statuses:
  - [ ] keep existing order, payment, and shipping statuses.
  - [ ] add centralized transition helper for order lifecycle changes.
  - [ ] add order note/history rows for status changes and system events.
  - [ ] keep admin note as a separate editable note.
- [ ] Placement guarantees:
  - [ ] transactional order creation for synchronous placement paths.
  - [ ] preserve checkout/payment idempotency.
  - [ ] revalidate cart, products, variants, quantities, payment method, shipping option, currency, and totals at placement.
  - [ ] prevent duplicate order on retry.
  - [ ] create or link `PaymentAttempt`.
  - [ ] keep existing stock deduction behavior.
  - [ ] expose stock reservation hook only.
  - [ ] close cart only after completed/captured order.
  - [ ] publish order-created event through a lightweight Commerce Node outbox/task hook.
  - [ ] queue notifications outside the main transaction.
- [ ] API/contract hardening:
  - [ ] additive Storefront order response fields.
  - [ ] additive Commerce Admin order fields.
  - [ ] stable OpenAPI metadata and contract tests.
  - [ ] no domain entities in public schemas.

Deferred:

- [ ] Full order management system.
- [ ] Invoice/accounting ledger.
- [ ] Returns, RMA, exchanges, or refund workflow.
- [ ] Full tax calculation engine.
- [ ] Full discount/promotion engine.
- [ ] Full stock reservation ledger.
- [ ] Multi-warehouse fulfillment.
- [ ] Multi-shipment workflow beyond the current shipment summary.
- [ ] Customer self-service order portal beyond secure guest completion lookup.
- [ ] Real notification provider implementation.
- [ ] Legacy `AppDbContext` changes.
- [ ] Legacy Presentation route changes.
- [ ] Active V2 `api/internal/*` changes.

## Current Baseline

Existing order entity:

- [x] `Order` stores internal `Id`. 2026-07-17 Phase 0: reviewed `Order`.
- [x] `Order` stores `UserId` and `CustomerId`. 2026-07-17 Phase 0.
- [x] `Order` stores `OrderStatus`, `PaymentStatus`, and shipping status fields. 2026-07-17 Phase 0.
- [x] `Order` stores `PaymentMethodKey`, `PaymentAt`, and `PaymentMetadataJson`. 2026-07-17 Phase 0.
- [x] `Order` stores public `Reference`. 2026-07-17 Phase 0.
- [x] `Order` stores `StoreId`. 2026-07-17 Phase 0.
- [x] `Order` stores `CurrencyCode`, `TotalAmount`, base currency, and exchange-rate fields. 2026-07-17 Phase 0.
- [x] `Order` stores created/updated/completed/cancelled timestamps. 2026-07-17 Phase 0.
- [x] `Order` stores customer name/email. 2026-07-17 Phase 0.
- [x] `Order` stores shipping address snapshot columns. 2026-07-17 Phase 0.
- [x] `Order` stores carrier/tracking fields. 2026-07-17 Phase 0.
- [x] `Order` stores `AdminNote`. 2026-07-17 Phase 0.
- [x] `CommerceNodeDbContext` owns `Orders` and `OrderLines`. 2026-07-17 Phase 0.
- [x] `Order.Reference` has a unique index. 2026-07-17 Phase 0: EF mapping reviewed.
- [x] Store-scoped admin queries filter orders by current Commerce Node store context. 2026-07-17 Phase 0: `CommerceNodeAdminOrderService.GetCurrentStoreOrdersAsync`.

Existing order line snapshot:

- [x] `OrderLine` stores order/product IDs. 2026-07-17 Phase 0: reviewed `OrderLine`.
- [x] `OrderLine` stores product name, SKU, and image. 2026-07-17 Phase 0.
- [x] `OrderLine` stores variant ID and `VariantAttributesJson`. 2026-07-17 Phase 0.
- [x] `OrderLine` stores personalization and artwork fields. 2026-07-17 Phase 0.
- [x] `OrderLine` stores fulfillment provider key. 2026-07-17 Phase 0.
- [x] `OrderLine` stores quantity. 2026-07-17 Phase 0.
- [x] `OrderLine` stores unit price and currency code. 2026-07-17 Phase 0.
- [x] `OrderLine` stores base/converted unit price. 2026-07-17 Phase 0.
- [x] `OrderLine` stores line total and base line total. 2026-07-17 Phase 0.
- [x] Product/variant long descriptions are not snapshotted and remain deferred unless invoice/email rendering requires them. 2026-07-17 Phase 0.

Existing checkout snapshot:

- [x] `CheckoutSession` stores public ID, store/cart/customer/order references. 2026-07-17 Phase 0: reviewed `CheckoutSession`.
- [x] `CheckoutSession` stores state, version, current step, and completed steps. 2026-07-17 Phase 0.
- [x] `CheckoutSession` stores cart version and last validated cart version. 2026-07-17 Phase 0.
- [x] `CheckoutSession` stores customer email/name/phone. 2026-07-17 Phase 0.
- [x] `CheckoutSession` stores `BillingAddressSnapshotJson`. 2026-07-17 Phase 0.
- [x] `CheckoutSession` stores shipping address snapshot columns. 2026-07-17 Phase 0.
- [x] `CheckoutSession` stores `SelectedShippingOptionJson`. 2026-07-17 Phase 0.
- [x] `CheckoutSession` stores payment method key. 2026-07-17 Phase 0.
- [x] `CheckoutSession` stores subtotal, shipping, tax, discount, and grand total. 2026-07-17 Phase 0.
- [x] `CheckoutSession` stores currency and exchange-rate snapshot fields. 2026-07-17 Phase 0.
- [x] `CheckoutSession` stores validation issues, terms acknowledgement, next action, idempotency key, placed timestamp, expiration, and audit timestamps. 2026-07-17 Phase 0.

Existing payment foundation:

- [x] `PaymentAttempt` stores public attempt id. 2026-07-17 Phase 0: reviewed `PaymentAttempt`.
- [x] `PaymentAttempt` stores store id, checkout session id, and optional order id. 2026-07-17 Phase 0.
- [x] `PaymentAttempt` stores method/provider key, state, amount/currency, exchange-rate snapshots, idempotency key, provider references, next action, safe failure fields, metadata, expiration, and timestamps. 2026-07-17 Phase 0.
- [x] `PaymentProviderEvent` records provider event ledger data. 2026-07-17 Phase 0: EF mapping reviewed.
- [x] `PaymentAttemptAuditLog` records payment attempt state transitions and links to order when available. 2026-07-17 Phase 0.

Existing placement flow:

- [x] `StorefrontCheckoutService.PlaceOrderAsync` validates store id. 2026-07-17 Phase 0: source review.
- [x] `StorefrontCheckoutService.PlaceOrderAsync` validates checkout feature state. 2026-07-17 Phase 0.
- [x] `StorefrontCheckoutService.PlaceOrderAsync` validates checkout session id. 2026-07-17 Phase 0.
- [x] `StorefrontCheckoutService.PlaceOrderAsync` validates expected checkout/cart versions. 2026-07-17 Phase 0.
- [x] `StorefrontCheckoutService.PlaceOrderAsync` validates idempotency and duplicate idempotency result. 2026-07-17 Phase 0.
- [x] `StorefrontCheckoutService.PlaceOrderAsync` validates checkout state and expiration. 2026-07-17 Phase 0.
- [x] `StorefrontCheckoutService.PlaceOrderAsync` validates active cart, stale cart version, empty cart, shipping readiness, selected shipping option, validation issues, payment provider, product/variant availability, stock, currency snapshot, positive total, and payment method availability. 2026-07-17 Phase 0.
- [x] COD/offline flow creates payment attempt, order, order lines, payment audit rows, stock deduction, ordered cart state, and completed checkout state. 2026-07-17 Phase 0.
- [x] Redirect/online flow creates payment attempt first and marks checkout `order_pending`. 2026-07-17 Phase 0.
- [x] Redirect/online flow does not create an order before provider capture. 2026-07-17 Phase 0.
- [x] `PaymentAttemptService.TransitionAsync` creates order exactly once when online attempt transitions to captured. 2026-07-17 Phase 0.

Existing admin order behavior:

- [x] `CommerceNodeAdminOrderService` supports list/search/filter orders. 2026-07-17 Phase 0: source review.
- [x] `CommerceNodeAdminOrderService` supports order detail. 2026-07-17 Phase 0.
- [x] `CommerceNodeAdminOrderService` supports tracking updates. 2026-07-17 Phase 0.
- [x] `CommerceNodeAdminOrderService` supports shipping status updates. 2026-07-17 Phase 0.
- [x] `CommerceNodeAdminOrderService` supports admin note updates. 2026-07-17 Phase 0.
- [x] `CommerceNodeAdminOrderService` supports complete and cancel. 2026-07-17 Phase 0.
- [x] Complete requires paid order and shipped/delivered/shipping-not-required status. 2026-07-17 Phase 0.
- [x] Cancel rejects completed orders and is idempotent for already-cancelled orders. 2026-07-17 Phase 0.
- [x] Admin changes write `AdminAuditLog`. 2026-07-17 Phase 0.
- [x] No order-local timeline/history entity exists yet. 2026-07-17 Phase 0.

Existing store data for snapshot:

- [x] `CommerceStore` stores public id, store key, name, status, base URL, company name/email/phone/address, default currency/culture, support email/phone, and maintenance fields. 2026-07-17 Phase 0: reviewed `CommerceStore`.
- [x] Order currently stores only `StoreId`; it does not snapshot store name/key/contact at placement time. 2026-07-17 Phase 0.

## Core Decisions

- [x] D1: Keep `Order` and `OrderLine` as the permanent order snapshot root. 2026-07-17 Phase 0 decision confirmed.
- [x] D2: Add order snapshot fields additively and nullable where needed. 2026-07-17 Phase 0 decision confirmed.
- [x] D3: Copy from `CheckoutSession` into `Order` at placement. 2026-07-17 Phase 0 decision confirmed.
- [x] D4: Keep `Order.Reference` as the public order number. 2026-07-17 Phase 0 decision confirmed.
- [x] D5: Add guest access token separately from `Reference`. 2026-07-17 Phase 0 decision confirmed for later phase.
- [x] D6: Create shared placement builder/service for COD and online capture. 2026-07-17 Phase 0 decision confirmed.
- [x] D7: Keep payment attempt as the payment lifecycle aggregate. 2026-07-17 Phase 0 decision confirmed.
- [x] D8: Add order-local notes/history, but keep admin audit. 2026-07-17 Phase 0 decision confirmed.
- [x] D9: Use lightweight outbox/task hook, not a new message bus. 2026-07-17 Phase 0 decision confirmed.
- [x] D10: Defer full reservation ledger. 2026-07-17 Phase 0 decision confirmed.

## Boundary Rules

- [x] Runtime data belongs to `CommerceNodeDbContext`. 2026-07-17 Phase 0 boundary confirmed.
- [x] Storefront uses `api/storefront/stores/{storeKey}/checkout/*`, `/payments/*`, and `/orders/*`. 2026-07-17 Phase 0 boundary confirmed.
- [x] ControlPlane Web uses ControlPlane API only. 2026-07-17 Phase 0 boundary confirmed.
- [x] ControlPlane API calls CommerceNode Admin via `api/commerce/admin/orders?storeKey={storeKey}`. 2026-07-17 Phase 0 boundary confirmed.
- [x] Do not add order placement behavior to legacy `AppDbContext`. 2026-07-17 Phase 0 preserved.
- [x] Do not add order placement behavior to legacy `BlazorShop.Presentation`. 2026-07-17 Phase 0 preserved.
- [x] Do not add active V2 `api/internal/*`. 2026-07-17 Phase 0 preserved.
- [x] Do not add direct `ControlPlane.Web -> CommerceNode.API` calls. 2026-07-17 Phase 0 preserved.
- [x] Storefront browser request bodies must not choose customer id, store id, totals, status, payment state, or order ownership. 2026-07-17 Phase 0 boundary confirmed.

## Data Model Checklist

Add to `Order` additively/nullable where needed:

- [x] `PublicId` if a stable non-internal public order id is desired beyond `Reference`. 2026-07-17 Phase 1: not added; Phase 0 D4 keeps `Order.Reference` as the public order number.
- [x] `StorePublicId`. 2026-07-17 Phase 1.
- [x] `StoreKeySnapshot`. 2026-07-17 Phase 1.
- [x] `StoreNameSnapshot`. 2026-07-17 Phase 1.
- [x] `StoreBaseUrlSnapshot`. 2026-07-17 Phase 1.
- [x] `StoreCompanyNameSnapshot`. 2026-07-17 Phase 1.
- [x] `StoreCompanyEmailSnapshot`. 2026-07-17 Phase 1.
- [x] `StoreCompanyPhoneSnapshot`. 2026-07-17 Phase 1.
- [x] `StoreCompanyAddressSnapshot`. 2026-07-17 Phase 1.
- [x] `BillingAddressSnapshotJson`. 2026-07-17 Phase 1.
- [x] `ShippingAddressSnapshotJson` or keep existing columns plus optional normalized JSON. 2026-07-17 Phase 1.
- [x] `SubtotalAmount`. 2026-07-17 Phase 1.
- [x] `ShippingTotalAmount`. 2026-07-17 Phase 1.
- [x] `TaxTotalAmount`. 2026-07-17 Phase 1.
- [x] `DiscountTotalAmount`. 2026-07-17 Phase 1.
- [x] `GrandTotalAmount`. 2026-07-17 Phase 1.
- [x] `BaseSubtotalAmount`. 2026-07-17 Phase 1.
- [x] `BaseShippingTotalAmount`. 2026-07-17 Phase 1.
- [x] `BaseTaxTotalAmount`. 2026-07-17 Phase 1.
- [x] `BaseDiscountTotalAmount`. 2026-07-17 Phase 1.
- [x] `BaseGrandTotalAmount`. 2026-07-17 Phase 1.
- [x] `ShippingMethodKey`. 2026-07-17 Phase 1: kept existing Shipping Core field.
- [x] `ShippingMethodName`. 2026-07-17 Phase 1: kept existing Shipping Core field.
- [x] `ShippingMethodSnapshotJson`. 2026-07-17 Phase 1.
- [x] `GuestAccessTokenHash`. 2026-07-17 Phase 1.
- [x] `GuestAccessTokenExpiresAtUtc`. 2026-07-17 Phase 1.
- [ ] `CreatedAtUtc` alias only if migrating off `CreatedOn` is approved later.

Keep compatible:

- [x] `Reference`. 2026-07-17 Phase 1 preserved.
- [x] `TotalAmount`. 2026-07-17 Phase 1 preserved.
- [x] Existing shipping columns. 2026-07-17 Phase 1 preserved.
- [x] Existing status fields. 2026-07-17 Phase 1 preserved.
- [x] `AdminNote`. 2026-07-17 Phase 1 preserved.
- [x] `TotalAmount` remains the charged/order total. 2026-07-17 Phase 1 preserved.
- [ ] `GrandTotalAmount` equals `TotalAmount` for new/backfilled rows.
- [ ] Existing rows render safely with null breakdown fields.

Add `OrderHistoryEntry` or order note:

- [ ] Table name `order_history_entries`.
- [ ] `Id`.
- [ ] `StoreId`.
- [ ] `OrderId`.
- [ ] `EntryType`: `system`, `status`, `payment`, `shipping`, `admin_note`, `notification`.
- [ ] `OldOrderStatus` / `NewOrderStatus`.
- [ ] `OldPaymentStatus` / `NewPaymentStatus`.
- [ ] `OldShippingStatus` / `NewShippingStatus`.
- [ ] `Message`.
- [ ] `IsCustomerVisible`.
- [ ] `MetadataJson`.
- [ ] `ActorType`: `system`, `admin`, `customer`, `provider`.
- [ ] `ActorId`.
- [ ] `CreatedAtUtc`.
- [ ] Index `(StoreId, OrderId, CreatedAtUtc)`.
- [ ] Index `(OrderId)`.
- [ ] Index `(StoreId, EntryType, CreatedAtUtc)`.

Outbox/task direction:

- [ ] Prefer existing `commerce_task` for notification queue if only notification uses are needed.
- [ ] Add `commerce_outbox_events` only if order/payment/shipping events need general reuse.
- [ ] If outbox table is added, include `StoreId`, aggregate type/id, event type, payload, idempotency key, occurred/processed timestamps, failure message.
- [ ] If outbox table is added, enforce unique `(StoreId, EventType, IdempotencyKey)` when idempotency key exists.

## Phase 0 - Baseline And Safety Snapshot

Goal: lock current behavior before changing order placement.

Implementation checklist:

- [x] Inventory current order files. 2026-07-17 Phase 0: `Order`, `OrderLine`, CommerceNode order repository/query/admin services reviewed.
- [x] Inventory current checkout files. 2026-07-17 Phase 0: `CheckoutSession` and `StorefrontCheckoutService` placement path reviewed.
- [x] Inventory current payment files. 2026-07-17 Phase 0: `PaymentAttempt`, `PaymentAttemptService`, provider event/audit mappings reviewed.
- [x] Inventory current cart files. 2026-07-17 Phase 0: cart session/line ownership reviewed through checkout placement path.
- [x] Inventory current shipping files. 2026-07-17 Phase 0: selected shipping option and shipment/tracking hooks reviewed.
- [x] Inventory current admin order files. 2026-07-17 Phase 0: `CommerceNodeAdminOrderService` and `CommerceOrdersController` reviewed.
- [x] Record COD/offline path in `StorefrontCheckoutService.PlaceOrderAsync`. 2026-07-17 Phase 0: baseline recorded in Current Baseline.
- [x] Record online capture path in `PaymentAttemptService.CreateCapturedOrderAsync`. 2026-07-17 Phase 0: baseline recorded.
- [x] Record current Storefront order response fields. 2026-07-17 Phase 0: `StorefrontOrderResponse` reviewed.
- [x] Record current Admin order DTO fields. 2026-07-17 Phase 0: `GetOrder` and admin mapping reviewed.
- [x] Record EF mapping/indexes for orders and order lines. 2026-07-17 Phase 0: `CommerceNodeDbContext` reviewed.
- [x] Record EF mapping/indexes for checkout sessions. 2026-07-17 Phase 0: `CommerceNodeDbContext` reviewed.
- [x] Record EF mapping/indexes for payment attempts and payment audit logs. 2026-07-17 Phase 0: `CommerceNodeDbContext` reviewed.
- [x] Add/update `QA-CommerceNode.todo.md` entries for Order Placement Core. 2026-07-17 Phase 0.

Verification checklist:

- [x] `StorefrontCheckoutServiceTests` pass. 2026-07-17 Phase 0: focused baseline run passed.
- [x] `PaymentAttemptServiceTests` pass. 2026-07-17 Phase 0: focused baseline run passed.
- [x] Admin order service tests pass if available. 2026-07-17 Phase 0: shipment/admin order adjacent focused tests passed.
- [x] Storefront checkout/payment/order contract tests pass. 2026-07-17 Phase 0: Storefront OpenAPI/payment contract focused tests passed.
- [x] No legacy runtime code is edited. 2026-07-17 Phase 0: docs-only phase.

Exit criteria:

- [x] Current behavior is documented. 2026-07-17 Phase 0.
- [x] Existing tests protecting placement pass before refactor. 2026-07-17 Phase 0: 97 focused tests passed.

Suggested commit:

```text
test(order-placement): lock baseline
```

## Phase 1 - Add Order Snapshot Schema

Goal: make the order row capable of preserving checkout/store/totals data without changing placement behavior yet.

Implementation checklist:

- [x] Add nullable snapshot fields to `Order`. 2026-07-17 Phase 1.
- [x] Map fields in `CommerceNodeDbContext`. 2026-07-17 Phase 1.
- [x] Add EF migration under Commerce Node migrations only. 2026-07-17 Phase 1: `20260717064548_CommerceNodeOrderPlacementSnapshots`.
- [x] Preserve existing `Order.Reference` unique index. 2026-07-17 Phase 1: no index changes.
- [x] Add `GuestAccessTokenHash` filtered unique index only if guest lookup endpoint is added in this phase. 2026-07-17 Phase 1: endpoint not added; no token index added yet.
- [x] Avoid over-indexing snapshot display fields. 2026-07-17 Phase 1: no display-field indexes added.
- [x] Extend `GetOrder` additively with safe snapshot fields. 2026-07-17 Phase 1.
- [x] Extend `StorefrontOrderResponse` additively with safe snapshot fields. 2026-07-17 Phase 1.
- [x] Extend Admin order DTO additively with historical fields. 2026-07-17 Phase 1.
- [x] Add total breakdown fields to public/admin responses. 2026-07-17 Phase 1.
- [x] Add billing address projection only when safe. 2026-07-17 Phase 1: structured address DTO only; raw JSON is not exposed.
- [x] Add shipping method summary projection. 2026-07-17 Phase 1.
- [x] Add store snapshot summary if needed for confirmation/email. 2026-07-17 Phase 1.

Rules:

- [x] Existing rows remain valid. 2026-07-17 Phase 1: new columns are nullable.
- [x] Null snapshot fields render safely. 2026-07-17 Phase 1: projection returns nullable summary objects.
- [x] Public API does not expose token hashes. 2026-07-17 Phase 1.
- [x] Public API does not expose internal metadata JSON. 2026-07-17 Phase 1.

Verification checklist:

- [x] EF model/migration compiles. 2026-07-17 Phase 1: CommerceNode API build passed.
- [x] Contract tests cover additive schemas. 2026-07-17 Phase 1: Storefront OpenAPI snapshot refreshed and focused contract tests passed.
- [x] Existing order list/detail tests pass. 2026-07-17 Phase 1: focused order/payment/checkout run passed 120/120.
- [x] Existing Storefront order consumers remain compatible. 2026-07-17 Phase 1: response changes are additive and Storefront V2 build passed inside test run.

Exit criteria:

- [x] Database can store new snapshot fields. 2026-07-17 Phase 1.
- [x] Existing order API consumers are not broken. 2026-07-17 Phase 1.

Suggested commit:

```text
feat(order-placement): add order snapshot schema
```

## Phase 2 - Shared Order Placement Builder

Goal: remove duplicate order construction logic before adding more behavior.

Implementation checklist:

- [ ] Introduce `IOrderPlacementService` or equivalent.
- [ ] Add `OrderPlacementService`.
- [ ] Add `OrderPlacementRequest`.
- [ ] Add `OrderPlacementResult`.
- [ ] Add `OrderSnapshotInput`.
- [ ] Move checkout/cart state validation into shared service where appropriate.
- [ ] Resolve cart lines to product/variant snapshots in shared service.
- [ ] Validate storefront availability.
- [ ] Validate quantity/stock.
- [ ] Compute rounded line totals.
- [ ] Resolve currency/rate snapshot.
- [ ] Copy customer data.
- [ ] Copy billing/shipping snapshots.
- [ ] Copy selected shipping option.
- [ ] Copy store snapshot.
- [ ] Create `Order` and `OrderLine` entities.
- [ ] Link optional `PaymentAttempt`.
- [ ] Close cart.
- [ ] Complete checkout.
- [ ] Keep provider-specific behavior outside placement service.
- [ ] Update COD/offline placement to call shared service.
- [ ] Update online captured payment path to call shared service.

Rules:

- [ ] Do not change route shapes.
- [ ] Do not let browser input supply totals/status/customer/store ownership.
- [ ] Do not move provider SDK code into order placement service.

Verification checklist:

- [ ] Existing COD order placement tests pass.
- [ ] Existing online capture creates exactly one order test passes.
- [ ] New test proves COD and online captured orders fill the same snapshot fields.

Exit criteria:

- [ ] There is one canonical order construction path.
- [ ] COD and online capture remain behaviorally compatible.

Suggested commit:

```text
refactor(order-placement): share order construction
```

## Phase 3 - Fill Permanent Order Snapshots

Goal: copy stable checkout/store/payment/shipping data into order history.

Implementation checklist:

- [ ] Load current `CommerceStore` during placement.
- [ ] Copy store public id.
- [ ] Copy store key.
- [ ] Copy store name.
- [ ] Copy store base URL.
- [ ] Copy company/contact fields needed for order documents.
- [ ] Copy `CheckoutSession.BillingAddressSnapshotJson`.
- [ ] If billing equals shipping, preserve both by value.
- [ ] Keep existing shipping columns.
- [ ] Optionally store normalized shipping JSON for future schema evolution.
- [ ] Set `SubtotalAmount`.
- [ ] Set `ShippingTotalAmount`.
- [ ] Set `TaxTotalAmount`.
- [ ] Set `DiscountTotalAmount`.
- [ ] Set `GrandTotalAmount`.
- [ ] Keep `TotalAmount = GrandTotalAmount`.
- [ ] Copy base totals where available.
- [ ] Parse/copy `SelectedShippingOptionJson`.
- [ ] Fill `ShippingMethodKey`.
- [ ] Fill `ShippingMethodName`.
- [ ] Fill `ShippingMethodSnapshotJson`.
- [ ] Keep `PaymentMethodKey`.
- [ ] Link `PaymentAttempt.OrderId`.
- [ ] Do not copy provider secrets.
- [ ] Do not copy raw webhook payloads.

Verification checklist:

- [ ] Mutating saved address after order does not change billing/shipping snapshots.
- [ ] Mutating store name/contact after order does not change order store snapshot.
- [ ] Checkout selected shipping option is preserved on order.
- [ ] Totals breakdown equals checkout/order result.
- [ ] Converted currency order fills working and base totals consistently.

Exit criteria:

- [ ] New orders can render confirmation/history without joining mutable store/address/cart data.

Suggested commit:

```text
feat(order-placement): fill permanent order snapshots
```

## Phase 4 - Guest Completion Access Token

Goal: allow customer-facing guest order completion lookup without treating order reference as a secret.

Implementation checklist:

- [ ] Generate secure random guest access token when placing guest order.
- [ ] Store only SHA256 hash or equivalent one-way hash in `Order.GuestAccessTokenHash`.
- [ ] Return token once in place-order response or Storefront completion redirect flow.
- [ ] Add/extend Storefront order lookup endpoint under `api/storefront/stores/{storeKey}/orders/*`.
- [ ] Support lookup by `reference + token` or `order id + token`.
- [ ] Authenticated customer lookup may use customer auth context without guest token.
- [ ] Storefront V2 keeps token server-side or query-safe only if current routing has no safer state store.
- [ ] Add guest access TTL only if needed for completion/account lookup behavior.
- [ ] Keep old reference-only redirect path temporarily if needed, but show minimal confirmation data only.

Rules:

- [ ] Never expose token hash in DTOs.
- [ ] Never allow guest detail lookup by reference alone once token is available.
- [ ] Store-scoped lookup must not cross stores.
- [ ] Browser JSON must not supply customer ownership fields.

Verification checklist:

- [ ] Guest retrieves completion with correct token.
- [ ] Wrong token returns 404 or forbidden with safe message.
- [ ] Store A token cannot access Store B order.
- [ ] Authenticated customer cannot request arbitrary customer/order id from browser JSON.

Exit criteria:

- [ ] Guest completion lookup is safe enough for production use.

Suggested commit:

```text
feat(order-placement): secure guest completion lookup
```

## Phase 5 - Order Status Transition And History

Goal: make order lifecycle changes consistent and auditable.

Implementation checklist:

- [ ] Add `OrderHistoryEntry` entity and mapping.
- [ ] Add `IOrderStatusTransitionService` or internal helper.
- [ ] Centralize place-order transition.
- [ ] Centralize payment captured transition.
- [ ] Centralize complete transition.
- [ ] Centralize cancel transition.
- [ ] Centralize shipping status changes.
- [ ] Normalize shipping status writes to constants.
- [ ] Accept legacy casing in filters and complete checks.
- [ ] Write history entry for order created.
- [ ] Write history entry for payment attempt linked/captured/failed where order exists.
- [ ] Write history entry for shipping status changed.
- [ ] Write history entry for tracking updated.
- [ ] Write history entry for order completed.
- [ ] Write history entry for order cancelled.
- [ ] Write history entry for admin note updated if useful.
- [ ] Keep existing `AdminAuditLog` writes in admin services.

Rules:

- [ ] Order history is append-only.
- [ ] Admin note remains editable and separate from timeline.
- [ ] Customer-visible flag defaults false except safe public events.

Verification checklist:

- [ ] Complete rules still pass.
- [ ] Cancel rules still pass.
- [ ] Shipment upsert writes normalized `shipped`.
- [ ] History entries append for create, complete, cancel, and shipping update.
- [ ] Existing admin audit tests still pass.

Exit criteria:

- [ ] Support/admin can explain order state changes from order-local history.

Suggested commit:

```text
feat(order-placement): add order history and transitions
```

## Phase 6 - Placement Transaction And Event Hook

Goal: make order creation, cart closure, payment link, stock deduction, history, and event enqueue atomic where required.

Implementation checklist:

- [ ] Ensure COD/offline placement uses explicit relational transaction.
- [ ] Wrap online captured order creation in explicit relational transaction.
- [ ] Inside transaction create order and lines.
- [ ] Inside transaction link payment attempt.
- [ ] Inside transaction deduct stock or call reservation hook.
- [ ] Inside transaction close cart.
- [ ] Inside transaction complete checkout.
- [ ] Inside transaction append order history.
- [ ] Inside transaction create outbox/task row for order-created notification/event.
- [ ] After commit return response.
- [ ] Let background worker/task process notification/event.
- [ ] Add `IStockReservationHook` or `IOrderStockAdjustmentHook`.
- [ ] Default stock hook matches current stock deduction behavior.
- [ ] Add idempotency guard for event enqueue based on order id/event type.

Rules:

- [ ] No email/provider notification is sent inside main transaction.
- [ ] Durable outbox/task row is written inside transaction.
- [ ] External dispatch happens later.
- [ ] Retried idempotency key returns original order/payment result.

Verification checklist:

- [ ] Duplicate place-order creates one order.
- [ ] Duplicate place-order creates one payment attempt.
- [ ] Duplicate place-order creates one order-created history entry.
- [ ] Duplicate place-order creates one outbox/task row.
- [ ] Simulated exception before commit rolls back order/cart/stock/history/outbox changes.
- [ ] Captured online payment replay does not create duplicate order/history/outbox rows.
- [ ] Unmanaged-stock products still do not deduct below zero.

Exit criteria:

- [ ] Placement has clear transactional boundaries.
- [ ] Placement side effects are replay-safe.

Suggested commit:

```text
feat(order-placement): harden placement transaction
```

## Phase 7 - API Projection And Storefront/Admin Integration

Goal: expose useful order data safely without breaking existing clients.

Implementation checklist:

- [ ] Add Storefront order totals breakdown fields.
- [ ] Add Storefront billing address summary if safe.
- [ ] Add Storefront shipping method summary.
- [ ] Add Storefront customer-visible history entries.
- [ ] Add Storefront payment state summary.
- [ ] Add guest completion token flow where applicable.
- [ ] Add Admin full snapshot details.
- [ ] Add Admin history entries.
- [ ] Add Admin payment attempt references.
- [ ] Add Admin shipping method snapshot.
- [ ] Add Admin totals breakdown.
- [ ] Forward new admin order fields through ControlPlane API only.
- [ ] Prevent ControlPlane Web from calling CommerceNode directly.
- [ ] Keep current Storefront checkout completion route working.
- [ ] Storefront V2 uses token-protected order lookup for guest details when available.
- [ ] Storefront V2 shows safe totals/status/tracking fields.
- [ ] Add/update Swagger operation IDs and summaries for changed endpoints.
- [ ] Use explicit request/response DTOs.
- [ ] Add standard errors.
- [ ] Add security metadata.
- [ ] Mark request bodies required.

Rules:

- [ ] Public Storefront responses do not expose provider secrets.
- [ ] Public Storefront responses do not expose raw payment metadata JSON.
- [ ] Public Storefront responses do not expose guest token hash.
- [ ] Public Storefront responses do not expose admin note unless explicitly customer-visible history entry.
- [ ] Public Storefront responses do not expose internal audit metadata.

Verification checklist:

- [ ] OpenAPI contract tests pass.
- [ ] Storefront V2 COD completion smoke still passes.
- [ ] Payment success/cancel smoke tests still pass.
- [ ] ControlPlane boundary tests still pass.

Exit criteria:

- [ ] Existing clients remain compatible.
- [ ] New order snapshot/history data is consumed safely by Storefront/Admin.

Suggested commit:

```text
feat(order-placement): expose order snapshots safely
```

## Phase 8 - QA, Migration Safety, And Documentation

Goal: close the phase with evidence and operational safety notes.

Implementation checklist:

- [ ] Update `QA-CommerceNode.todo.md`.
- [ ] Update `QA-ControlPlane.todo.md` if gateway/admin fields change.
- [ ] Update `QA-StorefrontV2.todo.md` if completion UI changes.
- [ ] Add migration notes for new nullable columns.
- [ ] Document that existing rows have null snapshot fields.
- [ ] Document optional backfill path for `GrandTotalAmount = TotalAmount`.
- [ ] Document that no legacy database changes were made.
- [ ] Record known deferred items.

Verification checklist:

- [ ] Commerce Node checkout/payment/order tests pass.
- [ ] API contract tests pass.
- [ ] Control Plane boundary/gateway tests pass if touched.
- [ ] Storefront smoke tests pass if completion route touched.
- [ ] Focused test output is recorded in implementation summary.
- [ ] No new V2 behavior depends on legacy `AppDbContext`.

Exit criteria:

- [ ] QA checklist updated with tested evidence.
- [ ] Migration safety notes are present.
- [ ] Focused tests pass.

Suggested commit:

```text
test(order-placement): verify order placement core
```

## QA Checklist Seeds

### Commerce Node

- [ ] COD placement creates order with store snapshot.
- [ ] COD placement creates order with billing snapshot.
- [ ] COD placement creates order with shipping snapshot.
- [ ] COD placement creates order with totals breakdown.
- [ ] COD placement creates order with shipping method snapshot.
- [ ] Online capture creates order with same snapshot fields as COD.
- [ ] Duplicate idempotency key does not duplicate order.
- [ ] Duplicate payment webhook/capture does not duplicate order.
- [ ] Guest order lookup requires token after secure lookup is enabled.
- [ ] Wrong guest token fails safely.
- [ ] Wrong store guest token fails safely.
- [ ] Order status transition helper enforces complete/cancel rules.
- [ ] Order history appends for create/payment/shipping/complete/cancel.
- [ ] Shipping status writes normalized constants.
- [ ] Placement transaction rolls back on injected failure.
- [ ] Order-created event/outbox/task is idempotent.
- [ ] Public Storefront schemas do not expose token hash, admin note, raw payment metadata, or domain entities.
- [ ] Commerce Admin schemas expose safe snapshot/history fields.

### Storefront V2

- [ ] COD completion still works.
- [ ] Hosted payment success/cancel pages still work.
- [ ] Guest completion uses secure lookup token when enabled.
- [ ] Completion page shows safe totals/status/tracking fields.
- [ ] Browser request bodies do not send store id/customer id/totals/status/payment state/order ownership.
- [ ] Browser network shows no provider secrets or raw payment metadata.

### Control Plane

- [ ] ControlPlane Web does not call CommerceNode order APIs directly.
- [ ] ControlPlane API gateway forwards order detail/list/status fields.
- [ ] Admin order detail shows snapshot/history fields when added.
- [ ] Admin complete/cancel/status changes still write admin audit.
- [ ] No order placement runtime data is stored in `ControlPlaneDbContext`.

## Failure Modes To Design Against

- [ ] COD and online capture snapshots drift.
- [ ] Order reference is used as secret.
- [ ] Duplicate payment webhook creates duplicate order.
- [ ] External notification is sent before transaction commits.
- [ ] Snapshot fields expose private metadata.
- [ ] Existing orders break after migration.
- [ ] Store/contact changes alter old order history.
- [ ] Billing address missing from order documents.
- [ ] Shipping status casing fragments filters.
- [ ] Stock deduction runs twice on retry.
- [ ] Online capture has partial state after failure.

## Test Map

- [ ] Schema tests:
  - [ ] EF model/migration compiles.
  - [ ] Existing order rows valid with null snapshot fields.
- [ ] COD placement tests:
  - [ ] billing/store/totals/shipping method snapshots.
  - [ ] cart ordered.
  - [ ] stock deducted.
  - [ ] payment attempt linked.
- [ ] Online capture tests:
  - [ ] captured payment creates exactly one order.
  - [ ] snapshot fields match COD path.
- [ ] Idempotency tests:
  - [ ] duplicate idempotency key returns original result.
  - [ ] duplicate key does not duplicate order/history/outbox.
- [ ] Guest access tests:
  - [ ] correct token can read completion.
  - [ ] wrong token fails safely.
  - [ ] wrong store fails safely.
- [ ] Store snapshot tests:
  - [ ] mutating `CommerceStore` after placement does not change order snapshot projection.
- [ ] Billing snapshot tests:
  - [ ] mutating saved address after placement does not change order snapshot projection.
- [ ] Totals tests:
  - [ ] subtotal + shipping + tax - discount equals grand/total after rounding.
- [ ] Status transition tests:
  - [ ] complete rules.
  - [ ] cancel rules.
  - [ ] shipping transition rules.
  - [ ] history appended.
- [ ] Transaction rollback tests:
  - [ ] injected placement failure rolls back order/cart/stock/history/outbox.
- [ ] API contract tests:
  - [ ] operation IDs.
  - [ ] schemas.
  - [ ] errors.
  - [ ] security.
  - [ ] request body metadata.
  - [ ] no domain entities.
- [ ] Boundary tests:
  - [ ] ControlPlane.Web only calls ControlPlane.API.
  - [ ] Storefront stays store-scoped.

## Migration And Compatibility

- [ ] Use additive migrations only.
- [ ] Existing order rows remain valid.
- [ ] New snapshot columns are nullable where needed.
- [ ] Existing `Order.Reference` remains public order number.
- [ ] Existing `TotalAmount` remains charged/order total.
- [ ] `GrandTotalAmount` equals `TotalAmount` for new/backfilled rows.
- [ ] Existing Storefront checkout completion route remains compatible.
- [ ] Existing Admin order list/detail routes remain compatible.
- [ ] Existing payment attempt rows remain valid.
- [ ] Existing payment provider event rows remain valid.
- [ ] Existing order lines remain valid.
- [ ] Existing shipment/tracking behavior remains compatible.
- [ ] No legacy database changes.

## Out Of Scope Backlog

- [ ] Full tax engine.
- [ ] Full discount engine.
- [ ] Full shipping carrier integration.
- [ ] Full order fulfillment workflow.
- [ ] Multi-shipment and partial shipment UI.
- [ ] Refund accounting and settlement reconciliation.
- [ ] Returns/RMA.
- [ ] Invoice numbering and accounting exports.
- [ ] Subscription/recurring order lifecycle.
- [ ] Event broker infrastructure.
- [ ] Public customer account order center beyond secure completion lookup.
- [ ] Legacy runtime migration.

## Recommended Implementation Order

- [x] Phase 0 - baseline and safety snapshot. 2026-07-17: current order placement behavior documented and 97 focused baseline tests passed.
- [x] Phase 1 - add order snapshot schema. 2026-07-17: committed after schema/test verification.
- [ ] Phase 2 - shared order placement builder.
- [ ] Phase 3 - fill permanent order snapshots.
- [ ] Phase 4 - guest completion access token.
- [ ] Phase 5 - order status transition and history.
- [ ] Phase 6 - placement transaction and event hook.
- [ ] Phase 7 - API projection and Storefront/Admin integration.
- [ ] Phase 8 - QA, migration safety, and documentation.

## Acceptance Criteria

- [ ] New order placement uses one shared builder/service for COD and online capture.
- [ ] New orders snapshot store data.
- [ ] New orders snapshot billing data.
- [ ] New orders snapshot shipping data.
- [ ] New orders snapshot shipping method.
- [ ] New orders snapshot totals.
- [ ] New orders snapshot currency and payment method.
- [ ] New orders snapshot line item details.
- [ ] Guest completion is protected by a token that is not the order reference.
- [ ] Order status changes append order-local history.
- [ ] Placement side effects are transactional and replay-safe.
- [ ] Public DTOs expose safe additive fields only.
- [ ] Admin DTOs expose safe additive fields only.
- [ ] Existing checkout/payment/storefront flows remain green.
- [ ] QA checklists contain evidence for new order placement guarantees.
