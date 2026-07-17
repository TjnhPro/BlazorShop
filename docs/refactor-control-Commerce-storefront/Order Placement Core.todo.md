# Order Placement Core.todo

Generated: 2026-07-17

Source plan: `Order Placement Core.md`

Status: Phase 8 complete. Order Placement Core complete.

Scope: turn the existing Commerce Node order placement flow from a working checkout MVP into a practical order core. The goal is stable historical order snapshots, safe placement guarantees, and enough order state history for real store operations without adding a full OMS, tax engine, discount engine, stock reservation ledger, invoice/accounting system, or fulfillment platform.

## Scope Lock

Approved:

- [x] Order snapshot hardening:
  - [x] store reference and store snapshot. 2026-07-17 Phase 3.
  - [x] customer/guest reference. 2026-07-17 Phase 3/4.
  - [x] billing address snapshot copied from checkout. 2026-07-17 Phase 3.
  - [x] shipping address snapshot kept and normalized. 2026-07-17 Phase 3.
  - [x] order item snapshot with product, variant, selected attributes, personalization, quantity, unit price, line total, and currency. 2026-07-17 Phase 2/3.
  - [x] order total breakdown: subtotal, shipping total, tax total, discount total, grand total. 2026-07-17 Phase 3.
  - [x] currency and exchange-rate snapshot. 2026-07-17 Phase 3.
  - [x] payment method reference. 2026-07-17 Phase 3/6.
  - [x] shipping method/option snapshot. 2026-07-17 Phase 3.
- [x] Order identity:
  - [x] keep internal `Order.Id`. 2026-07-17: preserved.
  - [x] keep public `Order.Reference`. 2026-07-17: preserved.
  - [x] created/updated timestamps. 2026-07-17: preserved.
  - [x] customer-facing guest completion access token stored hashed. 2026-07-17 Phase 4.
- [x] Order statuses:
  - [x] keep existing order, payment, and shipping statuses. 2026-07-17 Phase 5: legacy shipping status casing is accepted and normalized on writes.
  - [x] add centralized transition helper for order lifecycle changes. 2026-07-17 Phase 5: `OrderLifecycleTransitionHelper`.
  - [x] add order note/history rows for status changes and system events. 2026-07-17 Phase 5: `OrderHistoryEntry` plus CommerceNode migration.
  - [x] keep admin note as a separate editable note. 2026-07-17 Phase 5: admin note remains editable on `Order`; timeline receives an append-only event only.
- [x] Placement guarantees:
  - [x] transactional order creation for synchronous placement paths. 2026-07-17 Phase 6: COD path keeps relational transaction.
  - [x] preserve checkout/payment idempotency. 2026-07-17 Phase 6.
  - [x] revalidate cart, products, variants, quantities, payment method, shipping option, currency, and totals at placement. 2026-07-17 Phase 6: existing checkout revalidation preserved.
  - [x] prevent duplicate order on retry. 2026-07-17 Phase 6.
  - [x] create or link `PaymentAttempt`. 2026-07-17 Phase 6.
  - [x] keep existing stock deduction behavior. 2026-07-17 Phase 6: moved into default stock adjustment hook.
  - [x] expose stock reservation hook only. 2026-07-17 Phase 6: `IOrderStockAdjustmentHook`.
  - [x] close cart only after completed/captured order. 2026-07-17 Phase 6.
  - [x] publish order-created event through a lightweight Commerce Node outbox/task hook. 2026-07-17 Phase 6: `commerce_task` row with task type `order.created`.
  - [x] queue notifications outside the main transaction. 2026-07-17 Phase 6: only a durable task row is written during placement.
- [x] API/contract hardening:
  - [x] additive Storefront order response fields. 2026-07-17 Phase 7.
  - [x] additive Commerce Admin order fields. 2026-07-17 Phase 7.
  - [x] stable OpenAPI metadata and contract tests. 2026-07-17 Phase 7.
  - [x] no domain entities in public schemas. 2026-07-17 Phase 7.

Deferred:

- [n/a] Full order management system. Deferred by scope lock.
- [n/a] Invoice/accounting ledger. Deferred by scope lock.
- [n/a] Returns, RMA, exchanges, or refund workflow. Deferred by scope lock.
- [n/a] Full tax calculation engine. Deferred by scope lock.
- [n/a] Full discount/promotion engine. Deferred by scope lock.
- [n/a] Full stock reservation ledger. Deferred by scope lock.
- [n/a] Multi-warehouse fulfillment. Deferred by scope lock.
- [n/a] Multi-shipment workflow beyond the current shipment summary. Deferred by scope lock.
- [n/a] Customer self-service order portal beyond secure guest completion lookup. Deferred by scope lock.
- [n/a] Real notification provider implementation. Deferred by scope lock.
- [n/a] Legacy `AppDbContext` changes. Explicitly out of scope.
- [n/a] Legacy Presentation route changes. Explicitly out of scope.
- [n/a] Active V2 `api/internal/*` changes. Explicitly out of scope.

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
- [n/a] `CreatedAtUtc` alias only if migrating off `CreatedOn` is approved later.

Keep compatible:

- [x] `Reference`. 2026-07-17 Phase 1 preserved.
- [x] `TotalAmount`. 2026-07-17 Phase 1 preserved.
- [x] Existing shipping columns. 2026-07-17 Phase 1 preserved.
- [x] Existing status fields. 2026-07-17 Phase 1 preserved.
- [x] `AdminNote`. 2026-07-17 Phase 1 preserved.
- [x] `TotalAmount` remains the charged/order total. 2026-07-17 Phase 1 preserved.
- [x] `GrandTotalAmount` equals `TotalAmount` for new/backfilled rows. 2026-07-17 Phase 3: new orders set `GrandTotalAmount` from charged total.
- [x] Existing rows render safely with null breakdown fields. 2026-07-17 Phase 8: read models keep legacy-field fallback.

Add `OrderHistoryEntry` or order note:

- [x] Table name `order_history_entries`. 2026-07-17 Phase 5.
- [x] `Id`. 2026-07-17 Phase 5.
- [x] `StoreId`. 2026-07-17 Phase 5.
- [x] `OrderId`. 2026-07-17 Phase 5.
- [x] `EntryType`: implemented as `EventType`. 2026-07-17 Phase 5.
- [x] `OldOrderStatus` / `NewOrderStatus`: implemented as generic `OldValue` / `NewValue`. 2026-07-17 Phase 5.
- [x] `OldPaymentStatus` / `NewPaymentStatus`: implemented as generic `OldValue` / `NewValue`. 2026-07-17 Phase 5.
- [x] `OldShippingStatus` / `NewShippingStatus`: implemented as generic `OldValue` / `NewValue`. 2026-07-17 Phase 5.
- [x] `Message`. 2026-07-17 Phase 5.
- [x] `IsCustomerVisible`: implemented as `VisibleToCustomer`. 2026-07-17 Phase 5.
- [x] `MetadataJson`. 2026-07-17 Phase 5.
- [x] `ActorType`: implemented as `Source`. 2026-07-17 Phase 5.
- [n/a] `ActorId`. Deferred until customer/admin identity binding is needed in order history.
- [x] `CreatedAtUtc`. 2026-07-17 Phase 5.
- [x] Index `(StoreId, OrderId, CreatedAtUtc)`. 2026-07-17 Phase 5.
- [n/a] Index `(OrderId)`. Covered by `(StoreId, OrderId, CreatedAtUtc)` for active store-scoped reads.
- [x] Index `(StoreId, EntryType, CreatedAtUtc)`: implemented as `(StoreId, EventType, CreatedAtUtc)`. 2026-07-17 Phase 5.

Outbox/task direction:

- [x] Prefer existing `commerce_task` for notification queue if only notification uses are needed. 2026-07-17 Phase 6.
- [n/a] Add `commerce_outbox_events` only if order/payment/shipping events need general reuse. Deferred; `commerce_task` is enough for this phase.
- [n/a] If outbox table is added, include `StoreId`, aggregate type/id, event type, payload, idempotency key, occurred/processed timestamps, failure message. No outbox table added.
- [n/a] If outbox table is added, enforce unique `(StoreId, EventType, IdempotencyKey)` when idempotency key exists. No outbox table added.

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

- [x] Introduce `IOrderPlacementService` or equivalent. 2026-07-17 Phase 2.
- [x] Add `OrderPlacementService`. 2026-07-17 Phase 2.
- [x] Add `OrderPlacementRequest`. 2026-07-17 Phase 2.
- [x] Add `OrderPlacementResult`. 2026-07-17 Phase 2.
- [x] Add `OrderSnapshotInput`. 2026-07-17 Phase 2.
- [x] Move checkout/cart state validation into shared service where appropriate. 2026-07-17 Phase 2: cart presence/active/non-empty validation moved into shared placement service.
- [x] Resolve cart lines to product/variant snapshots in shared service. 2026-07-17 Phase 2.
- [x] Validate storefront availability. 2026-07-17 Phase 2: shared placement uses storefront sellability resolver.
- [x] Validate quantity/stock. 2026-07-17 Phase 2.
- [x] Compute rounded line totals. 2026-07-17 Phase 2.
- [x] Resolve currency/rate snapshot. 2026-07-17 Phase 2: caller still resolves rate before provider flow; shared service consumes explicit currency snapshot for order creation.
- [x] Copy customer data. 2026-07-17 Phase 2.
- [x] Copy billing/shipping snapshots. 2026-07-17 Phase 2: existing shipping columns copied; full billing/shipping JSON snapshot remains Phase 3.
- [x] Copy selected shipping option. 2026-07-17 Phase 2: COD path preserves existing selected option fields; full normalized snapshot JSON remains Phase 3.
- [x] Copy store snapshot. 2026-07-17 Phase 2: deferred to Phase 3 so no behavior/data fill changes are mixed into refactor.
- [x] Create `Order` and `OrderLine` entities. 2026-07-17 Phase 2.
- [x] Link optional `PaymentAttempt`. 2026-07-17 Phase 2.
- [x] Close cart. 2026-07-17 Phase 2.
- [x] Complete checkout. 2026-07-17 Phase 2.
- [x] Keep provider-specific behavior outside placement service. 2026-07-17 Phase 2.
- [x] Update COD/offline placement to call shared service. 2026-07-17 Phase 2.
- [x] Update online captured payment path to call shared service. 2026-07-17 Phase 2.

Rules:

- [x] Do not change route shapes. 2026-07-17 Phase 2.
- [x] Do not let browser input supply totals/status/customer/store ownership. 2026-07-17 Phase 2: shared service consumes server-side checkout/payment snapshots only.
- [x] Do not move provider SDK code into order placement service. 2026-07-17 Phase 2.

Verification checklist:

- [x] Existing COD order placement tests pass. 2026-07-17 Phase 2: `StorefrontCheckoutServiceTests` passed in focused run.
- [x] Existing online capture creates exactly one order test passes. 2026-07-17 Phase 2: `PaymentAttemptServiceTests` passed.
- [x] New test proves COD and online captured orders fill the same snapshot fields. 2026-07-17 Phase 2: online capture test now asserts shared order/line/customer snapshot fields; COD snapshot tests already cover the same mapper output.

Exit criteria:

- [x] There is one canonical order construction path. 2026-07-17 Phase 2.
- [x] COD and online capture remain behaviorally compatible. 2026-07-17 Phase 2: focused checkout/payment tests passed 59/59.

Suggested commit:

```text
refactor(order-placement): share order construction
```

## Phase 3 - Fill Permanent Order Snapshots

Goal: copy stable checkout/store/payment/shipping data into order history.

Implementation checklist:

- [x] Load current `CommerceStore` during placement. 2026-07-17 Phase 3: optional snapshot load, preserving legacy tests without seeded stores.
- [x] Copy store public id. 2026-07-17 Phase 3.
- [x] Copy store key. 2026-07-17 Phase 3.
- [x] Copy store name. 2026-07-17 Phase 3.
- [x] Copy store base URL. 2026-07-17 Phase 3.
- [x] Copy company/contact fields needed for order documents. 2026-07-17 Phase 3.
- [x] Copy `CheckoutSession.BillingAddressSnapshotJson`. 2026-07-17 Phase 3.
- [x] If billing equals shipping, preserve both by value. 2026-07-17 Phase 3: billing JSON and normalized shipping JSON are stored independently.
- [x] Keep existing shipping columns. 2026-07-17 Phase 3.
- [x] Optionally store normalized shipping JSON for future schema evolution. 2026-07-17 Phase 3.
- [x] Set `SubtotalAmount`. 2026-07-17 Phase 3.
- [x] Set `ShippingTotalAmount`. 2026-07-17 Phase 3.
- [x] Set `TaxTotalAmount`. 2026-07-17 Phase 3.
- [x] Set `DiscountTotalAmount`. 2026-07-17 Phase 3.
- [x] Set `GrandTotalAmount`. 2026-07-17 Phase 3.
- [x] Keep `TotalAmount = GrandTotalAmount`. 2026-07-17 Phase 3.
- [x] Copy base totals where available. 2026-07-17 Phase 3: base subtotal/grand total copied when checkout/payment snapshots provide them.
- [x] Parse/copy `SelectedShippingOptionJson`. 2026-07-17 Phase 3.
- [x] Fill `ShippingMethodKey`. 2026-07-17 Phase 3.
- [x] Fill `ShippingMethodName`. 2026-07-17 Phase 3.
- [x] Fill `ShippingMethodSnapshotJson`. 2026-07-17 Phase 3.
- [x] Keep `PaymentMethodKey`. 2026-07-17 Phase 3.
- [x] Link `PaymentAttempt.OrderId`. 2026-07-17 Phase 3.
- [x] Do not copy provider secrets. 2026-07-17 Phase 3.
- [x] Do not copy raw webhook payloads. 2026-07-17 Phase 3.

Verification checklist:

- [x] Mutating saved address after order does not change billing/shipping snapshots. 2026-07-17 Phase 3: order placement test mutates checkout snapshots after order.
- [x] Mutating store name/contact after order does not change order store snapshot. 2026-07-17 Phase 3.
- [x] Checkout selected shipping option is preserved on order. 2026-07-17 Phase 3.
- [x] Totals breakdown equals checkout/order result. 2026-07-17 Phase 3.
- [x] Converted currency order fills working and base totals consistently. 2026-07-17 Phase 3: existing converted currency checkout test passed in focused run.

Exit criteria:

- [x] New orders can render confirmation/history without joining mutable store/address/cart data. 2026-07-17 Phase 3.

Suggested commit:

```text
feat(order-placement): fill permanent order snapshots
```

## Phase 4 - Guest Completion Access Token

Goal: allow customer-facing guest order completion lookup without treating order reference as a secret.

Implementation checklist:

- [x] Generate secure random guest access token when placing guest order. 2026-07-17 Phase 4: 32-byte random hex token for guest checkout.
- [x] Store only SHA256 hash or equivalent one-way hash in `Order.GuestAccessTokenHash`. 2026-07-17 Phase 4.
- [x] Return token once in place-order response or Storefront completion redirect flow. 2026-07-17 Phase 4: immediate place-order response includes raw token; duplicate retries do not recover raw token.
- [x] Add/extend Storefront order lookup endpoint under `api/storefront/stores/{storeKey}/orders/*`. 2026-07-17 Phase 4: `POST /guest-lookup`.
- [x] Support lookup by `reference + token` or `order id + token`. 2026-07-17 Phase 4: reference + token supported.
- [x] Authenticated customer lookup may use customer auth context without guest token. 2026-07-17 Phase 4: existing current-user endpoints remain Bearer-protected.
- [x] Storefront V2 keeps token server-side or query-safe only if current routing has no safer state store. 2026-07-17 Phase 4: API uses POST body, not query token.
- [x] Add guest access TTL only if needed for completion/account lookup behavior. 2026-07-17 Phase 4: guest token expiry set to 30 days.
- [x] Keep old reference-only redirect path temporarily if needed, but show minimal confirmation data only. 2026-07-17 Phase 4: no reference-only detail lookup was added.

Rules:

- [x] Never expose token hash in DTOs. 2026-07-17 Phase 4.
- [x] Never allow guest detail lookup by reference alone once token is available. 2026-07-17 Phase 4.
- [x] Store-scoped lookup must not cross stores. 2026-07-17 Phase 4.
- [x] Browser JSON must not supply customer ownership fields. 2026-07-17 Phase 4.

Verification checklist:

- [x] Guest retrieves completion with correct token. 2026-07-17 Phase 4.
- [x] Wrong token returns 404 or forbidden with safe message. 2026-07-17 Phase 4: NotFound.
- [x] Store A token cannot access Store B order. 2026-07-17 Phase 4.
- [x] Authenticated customer cannot request arbitrary customer/order id from browser JSON. 2026-07-17 Phase 4.

Exit criteria:

- [x] Guest completion lookup is safe enough for production use. 2026-07-17 Phase 4.

Suggested commit:

```text
feat(order-placement): secure guest completion lookup
```

## Phase 5 - Order Status Transition And History

Goal: make order lifecycle changes consistent and auditable.

Implementation checklist:

- [x] Add `OrderHistoryEntry` entity and mapping. 2026-07-17 Phase 5: migration `CommerceNodeOrderHistoryEntries`.
- [x] Add `IOrderStatusTransitionService` or internal helper. 2026-07-17 Phase 5: internal `OrderLifecycleTransitionHelper`.
- [x] Centralize place-order transition. 2026-07-17 Phase 5.
- [x] Centralize payment captured transition. 2026-07-17 Phase 5.
- [x] Centralize complete transition. 2026-07-17 Phase 5.
- [x] Centralize cancel transition. 2026-07-17 Phase 5.
- [x] Centralize shipping status changes. 2026-07-17 Phase 5.
- [x] Normalize shipping status writes to constants. 2026-07-17 Phase 5: shipment/tracking writes use canonical snake-case statuses.
- [x] Accept legacy casing in filters and complete checks. 2026-07-17 Phase 5: `PendingShipment`/`Shipped`/`Delivered` aliases are accepted.
- [x] Write history entry for order created. 2026-07-17 Phase 5.
- [x] Write history entry for payment attempt linked/captured/failed where order exists. 2026-07-17 Phase 5: captured payment with order writes `payment.captured`; failed attempts without an order stay in payment-attempt audit.
- [x] Write history entry for shipping status changed. 2026-07-17 Phase 5.
- [x] Write history entry for tracking updated. 2026-07-17 Phase 5.
- [x] Write history entry for order completed. 2026-07-17 Phase 5.
- [x] Write history entry for order cancelled. 2026-07-17 Phase 5.
- [x] Write history entry for admin note updated if useful. 2026-07-17 Phase 5.
- [x] Keep existing `AdminAuditLog` writes in admin services. 2026-07-17 Phase 5: complete/cancel/shipment/admin-note audit calls remain.

Rules:

- [x] Order history is append-only. 2026-07-17 Phase 5: no update/delete service path was added.
- [x] Admin note remains editable and separate from timeline. 2026-07-17 Phase 5.
- [x] Customer-visible flag defaults false except safe public events. 2026-07-17 Phase 5: EF default false and helper opts in safe events.

Verification checklist:

- [x] Complete rules still pass. 2026-07-17 Phase 5: focused `CommerceNodeAdminShipmentServiceTests` covered legacy shipped completion.
- [x] Cancel rules still pass. 2026-07-17 Phase 5.
- [x] Shipment upsert writes normalized `shipped`. 2026-07-17 Phase 5.
- [x] History entries append for create, complete, cancel, and shipping update. 2026-07-17 Phase 5.
- [x] Existing admin audit tests still pass. 2026-07-17 Phase 5.

Exit criteria:

- [x] Support/admin can explain order state changes from order-local history. 2026-07-17 Phase 5.

Suggested commit:

```text
feat(order-placement): add order history and transitions
```

## Phase 6 - Placement Transaction And Event Hook

Goal: make order creation, cart closure, payment link, stock deduction, history, and event enqueue atomic where required.

Implementation checklist:

- [x] Ensure COD/offline placement uses explicit relational transaction. 2026-07-17 Phase 6.
- [x] Wrap online captured order creation in explicit relational transaction. 2026-07-17 Phase 6.
- [x] Inside transaction create order and lines. 2026-07-17 Phase 6.
- [x] Inside transaction link payment attempt. 2026-07-17 Phase 6.
- [x] Inside transaction deduct stock or call reservation hook. 2026-07-17 Phase 6.
- [x] Inside transaction close cart. 2026-07-17 Phase 6.
- [x] Inside transaction complete checkout. 2026-07-17 Phase 6.
- [x] Inside transaction append order history. 2026-07-17 Phase 6.
- [x] Inside transaction create outbox/task row for order-created notification/event. 2026-07-17 Phase 6.
- [x] After commit return response. 2026-07-17 Phase 6.
- [x] Let background worker/task process notification/event. 2026-07-17 Phase 6: existing CommerceTask worker remains owner of dispatch.
- [x] Add `IStockReservationHook` or `IOrderStockAdjustmentHook`. 2026-07-17 Phase 6.
- [x] Default stock hook matches current stock deduction behavior. 2026-07-17 Phase 6.
- [x] Add idempotency guard for event enqueue based on order id/event type. 2026-07-17 Phase 6.

Rules:

- [x] No email/provider notification is sent inside main transaction. 2026-07-17 Phase 6.
- [x] Durable outbox/task row is written inside transaction. 2026-07-17 Phase 6.
- [x] External dispatch happens later. 2026-07-17 Phase 6.
- [x] Retried idempotency key returns original order/payment result. 2026-07-17 Phase 6.

Verification checklist:

- [x] Duplicate place-order creates one order. 2026-07-17 Phase 6.
- [x] Duplicate place-order creates one payment attempt. 2026-07-17 Phase 6.
- [x] Duplicate place-order creates one order-created history entry. 2026-07-17 Phase 6.
- [x] Duplicate place-order creates one outbox/task row. 2026-07-17 Phase 6.
- [x] Simulated exception before commit rolls back order/cart/stock/history/outbox changes. 2026-07-17 Phase 6: failing stock hook leaves no placement side effects.
- [x] Captured online payment replay does not create duplicate order/history/outbox rows. 2026-07-17 Phase 6.
- [x] Unmanaged-stock products still do not deduct below zero. 2026-07-17 Phase 6.

Exit criteria:

- [x] Placement has clear transactional boundaries. 2026-07-17 Phase 6.
- [x] Placement side effects are replay-safe. 2026-07-17 Phase 6.

Suggested commit:

```text
feat(order-placement): harden placement transaction
```

## Phase 7 - API Projection And Storefront/Admin Integration

Goal: expose useful order data safely without breaking existing clients.

Implementation checklist:

- [x] Add Storefront order totals breakdown fields. 2026-07-17 Phase 7: existing additive fields remain in `StorefrontOrderResponse`.
- [x] Add Storefront billing address summary if safe. 2026-07-17 Phase 7: existing safe billing snapshot fields remain public.
- [x] Add Storefront shipping method summary. 2026-07-17 Phase 7: existing shipping method snapshot fields remain public.
- [x] Add Storefront customer-visible history entries. 2026-07-17 Phase 7: `historyEntries` exposes only customer-visible events.
- [x] Add Storefront payment state summary. 2026-07-17 Phase 7: `paymentSummary` exposes status/method/attempt state/amount/currency only.
- [x] Add guest completion token flow where applicable. 2026-07-17 Phase 7: secure lookup from Phase 4 now returns payment summary and customer-visible history.
- [x] Add Admin full snapshot details. 2026-07-17 Phase 7: `GetOrder` carries additive snapshot fields.
- [x] Add Admin history entries. 2026-07-17 Phase 7: admin projection includes all order history entries.
- [x] Add Admin payment attempt references. 2026-07-17 Phase 7: admin projection includes payment attempt public id/provider/status summary.
- [x] Add Admin shipping method snapshot. 2026-07-17 Phase 7: existing `GetOrder` shipping snapshot fields remain populated.
- [x] Add Admin totals breakdown. 2026-07-17 Phase 7: existing `GetOrder` total breakdown fields remain populated.
- [x] Forward new admin order fields through ControlPlane API only. 2026-07-17 Phase 7: ControlPlane Web consumes gateway `GetOrder` DTO.
- [x] Prevent ControlPlane Web from calling CommerceNode directly. 2026-07-17 Phase 7: boundary test passed.
- [x] Keep current Storefront checkout completion route working. 2026-07-17 Phase 7: focused checkout tests passed.
- [x] Storefront V2 uses token-protected order lookup for guest details when available. 2026-07-17 Phase 7: guest lookup test asserts token/store scope and new projection.
- [x] Storefront V2 shows safe totals/status/tracking fields. 2026-07-17 Phase 7: public order DTO remains additive and secret-safe.
- [x] Add/update Swagger operation IDs and summaries for changed endpoints. 2026-07-17 Phase 7: no route or operation id change; snapshot refreshed for response schema.
- [x] Use explicit request/response DTOs. 2026-07-17 Phase 7: `StorefrontOrderResponse` has explicit payment/history response records.
- [x] Add standard errors. 2026-07-17 Phase 7: existing endpoint error contracts remain unchanged and contract tests passed.
- [x] Add security metadata. 2026-07-17 Phase 7: existing protected endpoint security metadata remains unchanged and contract tests passed.
- [x] Mark request bodies required. 2026-07-17 Phase 7: no new request body added; existing body metadata remains covered by contract tests.

Rules:

- [x] Public Storefront responses do not expose provider secrets. 2026-07-17 Phase 7.
- [x] Public Storefront responses do not expose raw payment metadata JSON. 2026-07-17 Phase 7.
- [x] Public Storefront responses do not expose guest token hash. 2026-07-17 Phase 7.
- [x] Public Storefront responses do not expose admin note unless explicitly customer-visible history entry. 2026-07-17 Phase 7.
- [x] Public Storefront responses do not expose internal audit metadata. 2026-07-17 Phase 7.

Verification checklist:

- [x] OpenAPI contract tests pass. 2026-07-17 Phase 7: refreshed Storefront OpenAPI snapshot and focused contract run passed.
- [x] Storefront V2 COD completion smoke still passes. 2026-07-17 Phase 7: covered by focused checkout service placement tests.
- [x] Payment success/cancel smoke tests still pass. 2026-07-17 Phase 7: focused payment attempt tests passed.
- [x] ControlPlane boundary tests still pass. 2026-07-17 Phase 7.

Exit criteria:

- [x] Existing clients remain compatible. 2026-07-17 Phase 7: response changes are additive.
- [x] New order snapshot/history data is consumed safely by Storefront/Admin. 2026-07-17 Phase 7.

Suggested commit:

```text
feat(order-placement): expose order snapshots safely
```

## Phase 8 - QA, Migration Safety, And Documentation

Goal: close the phase with evidence and operational safety notes.

Implementation checklist:

- [x] Update `QA-CommerceNode.todo.md`. 2026-07-17 Phase 8: Order Placement evidence recorded through Phase 7 release gate.
- [x] Update `QA-ControlPlane.todo.md` if gateway/admin fields change. 2026-07-17 Phase 8: order drawer history evidence recorded.
- [x] Update `QA-StorefrontV2.todo.md` if completion UI changes. 2026-07-17 Phase 8: no Storefront V2 UI change in this phase; CommerceNode contract/service tests cover completion API compatibility.
- [x] Add migration notes for new nullable columns. 2026-07-17 Phase 8: see Migration And Compatibility.
- [x] Document that existing rows have null snapshot fields. 2026-07-17 Phase 8.
- [x] Document optional backfill path for `GrandTotalAmount = TotalAmount`. 2026-07-17 Phase 8.
- [x] Document that no legacy database changes were made. 2026-07-17 Phase 8.
- [x] Record known deferred items. 2026-07-17 Phase 8.

Verification checklist:

- [x] Commerce Node checkout/payment/order tests pass. 2026-07-17 Phase 8: focused release gate passed 103/103.
- [x] API contract tests pass. 2026-07-17 Phase 8: Storefront OpenAPI snapshot refreshed and contract tests passed.
- [x] Control Plane boundary/gateway tests pass if touched. 2026-07-17 Phase 8: `ControlPlaneArchitectureBoundaryTests` passed.
- [x] Storefront smoke tests pass if completion route touched. 2026-07-17 Phase 8: route behavior covered by CommerceNode service/contract tests; no Storefront V2 UI edit.
- [x] Focused test output is recorded in implementation summary. 2026-07-17 Phase 8.
- [x] No new V2 behavior depends on legacy `AppDbContext`. 2026-07-17 Phase 8.

Exit criteria:

- [x] QA checklist updated with tested evidence. 2026-07-17 Phase 8.
- [x] Migration safety notes are present. 2026-07-17 Phase 8.
- [x] Focused tests pass. 2026-07-17 Phase 8.

Suggested commit:

```text
test(order-placement): verify order placement core
```

## QA Checklist Seeds

### Commerce Node

- [x] COD placement creates order with store snapshot. 2026-07-17 Phase 3.
- [x] COD placement creates order with billing snapshot. 2026-07-17 Phase 3.
- [x] COD placement creates order with shipping snapshot. 2026-07-17 Phase 3.
- [x] COD placement creates order with totals breakdown. 2026-07-17 Phase 3.
- [x] COD placement creates order with shipping method snapshot. 2026-07-17 Phase 3.
- [x] Online capture creates order with same snapshot fields as COD. 2026-07-17 Phase 3.
- [x] Duplicate idempotency key does not duplicate order. 2026-07-17 Phase 6.
- [x] Duplicate payment webhook/capture does not duplicate order. 2026-07-17 Phase 6.
- [x] Guest order lookup requires token after secure lookup is enabled. 2026-07-17 Phase 4/7.
- [x] Wrong guest token fails safely. 2026-07-17 Phase 4/7.
- [x] Wrong store guest token fails safely. 2026-07-17 Phase 4/7.
- [x] Order status transition helper enforces complete/cancel rules. 2026-07-17 Phase 5.
- [x] Order history appends for create/payment/shipping/complete/cancel. 2026-07-17 Phase 5.
- [x] Shipping status writes normalized constants. 2026-07-17 Phase 5.
- [x] Placement transaction rolls back on injected failure. 2026-07-17 Phase 6.
- [x] Order-created event/outbox/task is idempotent. 2026-07-17 Phase 6.
- [x] Public Storefront schemas do not expose token hash, admin note, raw payment metadata, or domain entities. 2026-07-17 Phase 7.
- [x] Commerce Admin schemas expose safe snapshot/history fields. 2026-07-17 Phase 7.

### Storefront V2

- [x] COD completion still works. 2026-07-17 Phase 8: covered by focused CommerceNode checkout tests; no Storefront V2 UI change.
- [x] Hosted payment success/cancel pages still work. 2026-07-17 Phase 8: covered by focused payment attempt tests; no Storefront V2 UI change.
- [x] Guest completion uses secure lookup token when enabled. 2026-07-17 Phase 4/7.
- [x] Completion page shows safe totals/status/tracking fields. 2026-07-17 Phase 7: public DTO remains additive and safe.
- [x] Browser request bodies do not send store id/customer id/totals/status/payment state/order ownership. 2026-07-17 Phase 8: no request contract expansion added.
- [x] Browser network shows no provider secrets or raw payment metadata. 2026-07-17 Phase 8: response contract excludes secrets/raw metadata.

### Control Plane

- [x] ControlPlane Web does not call CommerceNode order APIs directly. 2026-07-17 Phase 7/8: boundary test passed.
- [x] ControlPlane API gateway forwards order detail/list/status fields. 2026-07-17 Phase 7: existing gateway DTO flow carries additive fields.
- [x] Admin order detail shows snapshot/history fields when added. 2026-07-17 Phase 7: order drawer renders history.
- [x] Admin complete/cancel/status changes still write admin audit. 2026-07-17 Phase 5/8.
- [x] No order placement runtime data is stored in `ControlPlaneDbContext`. 2026-07-17 Phase 8.

## Failure Modes To Design Against

- [x] COD and online capture snapshots drift. 2026-07-17 Phase 2/3: shared placement service and focused tests.
- [x] Order reference is used as secret. 2026-07-17 Phase 4: guest token is separate and stored hashed.
- [x] Duplicate payment webhook creates duplicate order. 2026-07-17 Phase 6: payment capture replay test passed.
- [x] External notification is sent before transaction commits. 2026-07-17 Phase 6: only durable task row is written during placement.
- [x] Snapshot fields expose private metadata. 2026-07-17 Phase 7: public response excludes raw JSON, token hash, admin note, and provider metadata.
- [x] Existing orders break after migration. 2026-07-17 Phase 8: additive nullable columns keep existing rows valid.
- [x] Store/contact changes alter old order history. 2026-07-17 Phase 3: snapshot mutation test passed.
- [x] Billing address missing from order documents. 2026-07-17 Phase 3: billing snapshot persisted.
- [x] Shipping status casing fragments filters. 2026-07-17 Phase 5: status normalization added.
- [x] Stock deduction runs twice on retry. 2026-07-17 Phase 6: duplicate order/payment replay tests passed.
- [x] Online capture has partial state after failure. 2026-07-17 Phase 6: placement transaction/hook failure tests passed.

## Test Map

- [x] Schema tests:
  - [x] EF model/migration compiles. 2026-07-17 Phase 1/5/6.
  - [x] Existing order rows valid with null snapshot fields. 2026-07-17 Phase 8: all new snapshot columns are nullable where required.
- [x] COD placement tests:
  - [x] billing/store/totals/shipping method snapshots. 2026-07-17 Phase 3.
  - [x] cart ordered. 2026-07-17 Phase 2/6.
  - [x] stock deducted. 2026-07-17 Phase 2/6.
  - [x] payment attempt linked. 2026-07-17 Phase 2/6.
- [x] Online capture tests:
  - [x] captured payment creates exactly one order. 2026-07-17 Phase 6.
  - [x] snapshot fields match COD path. 2026-07-17 Phase 3.
- [x] Idempotency tests:
  - [x] duplicate idempotency key returns original result. 2026-07-17 Phase 6.
  - [x] duplicate key does not duplicate order/history/outbox. 2026-07-17 Phase 6.
- [x] Guest access tests:
  - [x] correct token can read completion. 2026-07-17 Phase 4/7.
  - [x] wrong token fails safely. 2026-07-17 Phase 4/7.
  - [x] wrong store fails safely. 2026-07-17 Phase 4/7.
- [x] Store snapshot tests:
  - [x] mutating `CommerceStore` after placement does not change order snapshot projection. 2026-07-17 Phase 3.
- [x] Billing snapshot tests:
  - [x] mutating saved address after placement does not change order snapshot projection. 2026-07-17 Phase 3.
- [x] Totals tests:
  - [x] subtotal + shipping + tax - discount equals grand/total after rounding. 2026-07-17 Phase 3/6.
- [x] Status transition tests: 2026-07-17 Phase 5.
  - [x] complete rules. 2026-07-17 Phase 5.
  - [x] cancel rules. 2026-07-17 Phase 5.
  - [x] shipping transition rules. 2026-07-17 Phase 5.
  - [x] history appended. 2026-07-17 Phase 5.
 - [x] Transaction rollback tests: 2026-07-17 Phase 6.
  - [x] injected placement failure rolls back order/cart/stock/history/outbox. 2026-07-17 Phase 6.
- [x] API contract tests:
  - [x] operation IDs. 2026-07-17 Phase 7.
  - [x] schemas. 2026-07-17 Phase 7.
  - [x] errors. 2026-07-17 Phase 7.
  - [x] security. 2026-07-17 Phase 7.
  - [x] request body metadata. 2026-07-17 Phase 7.
  - [x] no domain entities. 2026-07-17 Phase 7.
- [x] Boundary tests:
  - [x] ControlPlane.Web only calls ControlPlane.API. 2026-07-17 Phase 7/8.
  - [x] Storefront stays store-scoped. 2026-07-17 Phase 4/7.

## Migration And Compatibility

- [x] Use additive migrations only. 2026-07-17 Phase 8: CommerceNode migrations add nullable columns/tables; no destructive migration.
- [x] Existing order rows remain valid. 2026-07-17 Phase 8: existing rows may keep null snapshot/history/payment-summary fields.
- [x] New snapshot columns are nullable where needed. 2026-07-17 Phase 8.
- [x] Existing `Order.Reference` remains public order number. 2026-07-17 Phase 8.
- [x] Existing `TotalAmount` remains charged/order total. 2026-07-17 Phase 8.
- [x] `GrandTotalAmount` equals `TotalAmount` for new/backfilled rows. 2026-07-17 Phase 8: new rows set breakdown totals; optional backfill can set `GrandTotalAmount = TotalAmount`, `SubtotalAmount = TotalAmount`, and tax/discount/shipping totals to `0` only when historical detail is unavailable.
- [x] Existing Storefront checkout completion route remains compatible. 2026-07-17 Phase 8.
- [x] Existing Admin order list/detail routes remain compatible. 2026-07-17 Phase 8.
- [x] Existing payment attempt rows remain valid. 2026-07-17 Phase 8.
- [x] Existing payment provider event rows remain valid. 2026-07-17 Phase 8.
- [x] Existing order lines remain valid. 2026-07-17 Phase 8.
- [x] Existing shipment/tracking behavior remains compatible. 2026-07-17 Phase 8.
- [x] No legacy database changes. 2026-07-17 Phase 8: no `AppDbContext`/legacy Presentation migration or route changes.

Migration safety notes:

- Existing orders created before this refactor can have null store snapshot, billing snapshot, shipping method snapshot, order total breakdown, guest token, and history fields. Read models must keep fallback behavior to legacy order fields.
- Backfill is optional. If needed for reporting, backfill only CommerceNode-owned `orders` rows and use conservative values when historical detail is unavailable: `GrandTotalAmount = TotalAmount`, `SubtotalAmount = TotalAmount`, `ShippingTotalAmount = 0`, `TaxTotalAmount = 0`, `DiscountTotalAmount = 0`.
- Do not backfill raw provider metadata into public/admin projection fields. Payment attempt metadata remains internal.
- Do not create legacy `AppDbContext` migrations for these changes.

## Out Of Scope Backlog

- [n/a] Full tax engine. Deferred.
- [n/a] Full discount engine. Deferred.
- [n/a] Full shipping carrier integration. Deferred.
- [n/a] Full order fulfillment workflow. Deferred.
- [n/a] Multi-shipment and partial shipment UI. Deferred.
- [n/a] Refund accounting and settlement reconciliation. Deferred.
- [n/a] Returns/RMA. Deferred.
- [n/a] Invoice numbering and accounting exports. Deferred.
- [n/a] Subscription/recurring order lifecycle. Deferred.
- [n/a] Event broker infrastructure. Deferred.
- [n/a] Public customer account order center beyond secure completion lookup. Deferred.
- [n/a] Legacy runtime migration. Explicitly out of scope.

## Recommended Implementation Order

- [x] Phase 0 - baseline and safety snapshot. 2026-07-17: current order placement behavior documented and 97 focused baseline tests passed.
- [x] Phase 1 - add order snapshot schema. 2026-07-17: committed after schema/test verification.
- [x] Phase 2 - shared order placement builder. 2026-07-17: committed after checkout/payment focused tests passed.
- [x] Phase 3 - fill permanent order snapshots. 2026-07-17: committed after snapshot mutation and checkout/payment focused tests passed.
- [x] Phase 4 - guest completion access token. 2026-07-17: committed after guest lookup/OpenAPI/model focused tests passed.
- [x] Phase 5 - order status transition and history. 2026-07-17: committed after CommerceNode API build and focused 95/95 test run.
- [x] Phase 6 - placement transaction and event hook. 2026-07-17: committed after CommerceNode API build and focused 88/88 test run.
- [x] Phase 7 - API projection and Storefront/Admin integration. 2026-07-17: committed after CommerceNode/ControlPlane builds and focused 103/103 test run.
- [x] Phase 8 - QA, migration safety, and documentation. 2026-07-17: docs/QA/migration notes completed after focused 103/103 Phase 7 release gate.

## Acceptance Criteria

- [x] New order placement uses one shared builder/service for COD and online capture. 2026-07-17 Phase 2.
- [x] New orders snapshot store data. 2026-07-17 Phase 3.
- [x] New orders snapshot billing data. 2026-07-17 Phase 3.
- [x] New orders snapshot shipping data. 2026-07-17 Phase 3.
- [x] New orders snapshot shipping method. 2026-07-17 Phase 3.
- [x] New orders snapshot totals. 2026-07-17 Phase 3.
- [x] New orders snapshot currency and payment method. 2026-07-17 Phase 3.
- [x] New orders snapshot line item details. 2026-07-17 Phase 2/3.
- [x] Guest completion is protected by a token that is not the order reference. 2026-07-17 Phase 4.
- [x] Order status changes append order-local history. 2026-07-17 Phase 5.
- [x] Placement side effects are transactional and replay-safe. 2026-07-17 Phase 6.
- [x] Public DTOs expose safe additive fields only. 2026-07-17 Phase 7.
- [x] Admin DTOs expose safe additive fields only. 2026-07-17 Phase 7.
- [x] Existing checkout/payment/storefront flows remain green. 2026-07-17 Phase 8: focused 103/103 run passed.
- [x] QA checklists contain evidence for new order placement guarantees. 2026-07-17 Phase 8.
