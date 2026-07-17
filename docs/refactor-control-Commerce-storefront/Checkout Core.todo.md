# Checkout Core.todo

Generated: 2026-07-17

Source plan: `Checkout Core.md`

Status: In progress. Phase 0-1 completed.

Scope: evolve the current V2 checkout from one-shot preview/place-order into a practical stateful checkout core without introducing a full shipping, tax, discount, or workflow engine.

## Scope Lock

Approved:

- [x] Checkout session belongs to current store and current cart.
- [x] Checkout state version.
- [ ] Step guards for address, payment, review, and place order.
- [x] Address step guard.
- [x] Detect cart changes after address/payment selection.
- [x] Reset downstream state when upstream state changes.
- [x] Checkout expiration.
- [x] Resume checkout.
- [ ] Idempotent place-order command.
- [x] Entry validation:
  - [x] cart exists.
  - [x] cart has active lines.
  - [x] cart validation has no blocking issues.
  - [x] checkout feature enabled.
  - [ ] minimum/maximum order total hook.
  - [ ] guest checkout policy shape.
- [x] Billing address and shipping address integration through Address Core.
- [x] Payment method selection from active store payment providers.
- [ ] Review projection before final confirmation.
- [ ] Terms/legal acknowledgement hook.
- [x] Completion handling for order reference, payment state, provider redirect, and cart cleanup rule.

Deferred:

- [ ] Full shipping-rate provider engine.
- [ ] Full tax engine.
- [ ] Full discount engine.
- [ ] Complex multi-step workflow engine.
- [ ] Required frontend wizard rewrite before API state is stable.
- [ ] External fraud/risk scoring.
- [ ] Subscription/recurring checkout.
- [ ] Multi-shipment checkout.
- [ ] Split payment.
- [ ] Partial cart checkout.
- [ ] Provider-specific dynamic payment UI engine beyond metadata hooks.
- [ ] Admin UI for every checkout setting.
- [ ] Extending legacy `AppDbContext` or legacy presentation checkout routes.

## Current Baseline

Checkout session:

- [x] `CheckoutSession` exists in Commerce Node domain.
- [x] It stores store/cart/customer/order state.
- [x] It stores customer and shipping address snapshots.
- [x] It stores payment method key, totals, currency snapshots, validation issues JSON, next action, idempotency key, expiration, placed timestamp, and audit timestamps.
- [x] Session states exist: `draft`, `ready`, `order_pending`, `completed`, `expired`, `cancelled`.
- [x] `checkout_sessions` has public ID, store/cart/state, customer, order, idempotency, expiration, and state indexes/constraints.

Preview and place order:

- [x] `StorefrontCheckoutService.PreviewAsync` validates current checkout feature, cart token, cart state, cart version, cart lines, cart validation issues, customer fields, shipping fields, and payment method availability.
- [x] `PreviewAsync` writes a `CheckoutSession` as `ready` when valid.
- [x] `PreviewAsync` writes a `CheckoutSession` as `draft` with validation issues when invalid.
- [x] `PlaceOrderAsync` validates checkout feature, session, expected cart version, idempotency key, state, expiration, cart, payment method, product availability, stock, currency snapshots, and positive total.
- [x] COD flow creates order/payment attempt, deducts stock, marks cart ordered, marks checkout completed, and returns order reference.
- [x] Stripe hosted flow creates `PaymentAttempt`, calls hosted provider, marks checkout `order_pending`, and returns redirect next action without creating order until provider confirmation.
- [x] Duplicate idempotency key returns the existing order/payment attempt result.

Payment foundation:

- [ ] `PaymentAttempt` exists with state, amount, currency, idempotency key, provider reference/session id, next action, failure fields, metadata, expiration, and timestamps.
- [ ] Payment attempt states exist: `created`, `requires_action`, `authorized`, `captured`, `failed`, `cancelled`, `expired`.
- [ ] `PaymentAttemptService` supports get/create/transition/provider event recording/duplicate event detection/captured-order creation.
- [ ] `IStorefrontPaymentProvider` and `IStorefrontPaymentProviderResolver` exist.
- [ ] Stripe hosted checkout provider exists.
- [ ] Store payment methods support enabled state, display order, public display metadata, supported currencies/countries, and min/max order total.
- [ ] Storefront API exposes payment methods and payment attempt status.
- [ ] Storefront V2 has payment success/cancel pages that read payment attempt status.

Storefront V2 UX:

- [x] Checkout page is currently one form.
- [x] POST `/checkout` gets current cart, calls preview, then immediately calls place order.
- [x] Checkout page renders shipping address fields and payment method radios.
- [x] Checkout page uses antiforgery token.
- [x] Storefront V2 clears cart cookies after completed order result only; hosted provider redirect keeps cart context recoverable.

Missing:

- [x] Checkout state version separate from cart version exists.
- [x] Current step and completed steps exist.
- [x] Resume checkout endpoint exists.
- [ ] Preview creates a new checkout session each time instead of updating/resuming an active session.
- [ ] No downstream reset model when address, payment, or cart changes.
- [x] Billing address step command exists.
- [ ] No saved address selection yet; depends on Address Core.
- [ ] No shipping-required detection.
- [ ] No shipping method/provider/options model.
- [ ] No shipping option selection or revalidation.
- [ ] No checkout-level min/max order total setting.
- [ ] No explicit guest checkout policy enforcement in Storefront checkout service.
- [ ] Admin settings expose guest checkout as unsupported while checkout endpoints are anonymous.
- [ ] No terms/legal acknowledgement.
- [ ] No payment-specific input schema/UI metadata beyond method display metadata.
- [ ] No minimum interval between place-order attempts beyond idempotency.
- [ ] No review projection endpoint.
- [ ] No safe cart cleanup distinction between completed COD orders and provider redirect pending orders.

## Core Decisions

- [x] Keep `CheckoutSession` as the checkout aggregate.
- [x] Add state/version fields additively.
- [ ] Keep the first UI integration as one page.
- [ ] Treat shipping method as a stub/hook in this phase.
- [ ] Reuse existing `StorePaymentMethod` for payment method step.
- [ ] Integrate saved addresses only after Address Core exists.
- [ ] Keep direct guest address supported.
- [x] Do not clear cart token for pending hosted payment.
- [ ] Preserve idempotency as double-submit defense.

## Target Boundary

```text
Storefront V2 browser
  -> Storefront V2 checkout page/local endpoints
      -> StorefrontApiClient
          -> Commerce Node Storefront API
              api/storefront/stores/{storeKey}/checkout/*
              api/storefront/stores/{storeKey}/payments/*
                  -> StorefrontCheckoutService
                  -> PaymentAttemptService
                  -> Address services
                  -> Cart service
                  -> Payment provider resolver
                  -> CommerceNodeDbContext
```

Boundary rules:

- [x] Store scope comes from `{storeKey}`.
- [x] Cart identity comes from current cart token.
- [ ] Customer identity comes from storefront auth context only when required.
- [ ] Browser requests do not send store ID.
- [ ] Browser requests do not send customer ID.
- [ ] Browser requests do not send order status.
- [ ] Browser requests do not send payment status.
- [ ] Browser requests do not send totals or other server-owned fields.
- [ ] Side-effecting checkout commands use POST.
- [ ] No legacy `api/internal/*`, `api/public/*`, or legacy controller routes.

## Target State Model

Keep existing `CheckoutSession.State`:

- [x] `draft`.
- [x] `ready`.
- [x] `order_pending`.
- [x] `completed`.
- [x] `expired`.
- [x] `cancelled`.

Add lightweight progress fields:

- [x] `CheckoutVersion`.
- [x] `CurrentStep`.
- [x] `CompletedStepsJson`.
- [x] `LastValidatedCartVersion`.
- [ ] `BillingAddressSnapshotJson` or explicit billing columns only when billing is implemented.
- [ ] `ShippingAddressSource`.
- [ ] `SelectedShippingOptionJson`.
- [ ] `SelectedPaymentMethodKey`.
- [ ] `TermsAcceptedAtUtc`.
- [ ] `TermsVersion`.

Initial step names:

- [x] `entry`.
- [ ] `billing_address`.
- [ ] `shipping_address`.
- [ ] `shipping_method`.
- [ ] `payment_method`.
- [x] `review`.
- [x] `place_order`.
- [x] `complete`.

Downstream reset rules:

- [ ] Cart changed resets shipping method, payment method, review, totals, and terms acknowledgement.
- [ ] Billing address changed resets shipping address when same-as-billing, shipping method, payment method availability, review, totals, and terms acknowledgement.
- [ ] Shipping address changed resets shipping method, payment method availability, review, totals, and terms acknowledgement.
- [ ] Shipping method changed resets review, totals, and terms acknowledgement.
- [ ] Payment method changed resets payment-specific input, review, and terms acknowledgement.

## Target API Direction

Keep current endpoints:

- [x] `POST /api/storefront/stores/{storeKey}/checkout/preview`.
- [x] `POST /api/storefront/stores/{storeKey}/checkout/place-order`.

Add stateful endpoints:

- [x] `POST /api/storefront/stores/{storeKey}/checkout/start`.
- [x] `GET /api/storefront/stores/{storeKey}/checkout/{checkoutSessionId}`.
- [x] `POST /api/storefront/stores/{storeKey}/checkout/{checkoutSessionId}/addresses`.
- [ ] `POST /api/storefront/stores/{storeKey}/checkout/{checkoutSessionId}/shipping-method`.
- [ ] `POST /api/storefront/stores/{storeKey}/checkout/{checkoutSessionId}/payment-method`.
- [ ] `POST /api/storefront/stores/{storeKey}/checkout/{checkoutSessionId}/review`.
- [x] `POST /api/storefront/stores/{storeKey}/checkout/{checkoutSessionId}/cancel`.

Compatibility:

- [x] Existing `preview` remains compatible and can wrap start/update/review.
- [ ] Existing `place-order` remains final command.
- [ ] Storefront V2 can migrate endpoint usage gradually.

## Phase 0 - Baseline Guardrails

Goal: protect the current working checkout behavior before changing state.

Implementation checklist:

- [x] Re-read checkout service tests.
- [x] Re-read payment attempt tests.
- [x] Re-read Storefront V2 smoke/static tests.
- [x] Re-read Storefront OpenAPI contract tests.
- [x] Add missing baseline test for provider redirect not treated as completed order.
- [x] Add missing baseline test for cart token cleanup completed vs pending provider payment.
- [x] Add missing baseline test for checkout session expiration blocking place order.
- [x] Confirm checkout data lives in `CommerceNodeDbContext`.
- [x] Confirm active V2 checkout does not depend on legacy `AppDbContext`.
- [x] Confirm current checkout request/response contract stays compatible.
- [x] Make no schema, route, or behavior change unless needed to close a baseline gap.

Verification checklist:

- [x] Existing COD checkout tests pass.
- [x] Existing Stripe redirect tests pass.
- [x] Existing payment success/cancel page tests pass.
- [x] Existing checkout OpenAPI contract tests pass.
- [x] No legacy checkout route is extended.
- [x] No `AppDbContext` migration is generated.

Exit criteria:

- [x] Existing checkout behavior is protected before stateful checkout work starts.
- [x] Provider redirect and cart cleanup risks have tests or documented gaps.

Phase 0 evidence:

- 2026-07-17: Existing `StorefrontCheckoutServiceTests.PlaceOrderAsync_StripeCreatesRedirectAttemptWithoutOrder` already proves Stripe hosted checkout creates a pending `PaymentAttempt`, does not create an order, keeps cart active, and marks checkout `order_pending`.
- 2026-07-17: Existing `StorefrontCheckoutServiceTests.PlaceOrderAsync_RejectsStaleCartVersion`, duplicate idempotency, feature-disabled, and product availability tests protect current backend behavior before stateful changes.
- 2026-07-17: Fixed Storefront V2 local checkout completion behavior so hosted provider redirect no longer clears `bs-cart-token`/legacy cart cookies before provider confirmation.
- 2026-07-17: Added `StorefrontV2HostSmokeTests.Checkout_PostCompletedOrderClearsCartCookies` and strengthened `Checkout_PostRedirectsToProviderNextAction` to prove pending redirect keeps cart context while completed order clears cart cookies.
- 2026-07-17: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore` passed.
- 2026-07-17: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.Checkout_PostRedirectsToProviderNextAction|FullyQualifiedName~StorefrontV2HostSmokeTests.Checkout_PostCompletedOrderClearsCartCookies|FullyQualifiedName~StorefrontCheckoutServiceTests"` passed 22/22.

Suggested commit:

```text
test(checkout-core): add baseline guardrails
```

## Phase 1 - Checkout Session Version And Resume

Goal: make checkout session state explicit and resumable.

Implementation checklist:

- [x] Add additive fields to `CheckoutSession`:
  - [x] `CheckoutVersion`.
  - [x] `CurrentStep`.
  - [x] `CompletedStepsJson`.
  - [x] `LastValidatedCartVersion`.
- [x] Add EF Core migration for `CommerceNodeDbContext`.
- [x] Add default values for existing rows:
  - [x] `CheckoutVersion = 1`.
  - [x] `CurrentStep = state-derived value`.
  - [x] `CompletedStepsJson = []`.
  - [x] `LastValidatedCartVersion = CartVersion`.
- [x] Add service method to start checkout.
- [x] Add service method to load checkout.
- [x] Add service method to expire checkout.
- [x] Add service method to cancel checkout.
- [x] Add service helper to touch/increment checkout version.
- [x] Add resume endpoint returning checkout projection.
- [x] Make active checkout lookup store-scoped and cart-scoped.
- [x] Keep old `preview` behavior functional.
- [x] Add contract metadata and tests for resume/start endpoints.

Verification checklist:

- [x] Checkout can be resumed by session public ID for same store/cart context.
- [x] Checkout cannot resume across stores.
- [x] Expired sessions cannot be resumed as active.
- [x] Cancelled/completed sessions cannot be resumed as active.
- [x] Checkout version increments when step data changes.
- [x] Existing preview/place-order clients still work.

Exit criteria:

- [x] Checkout state is resumable without forcing UI rewrite.
- [x] Migration is additive and CommerceNode-only.

Phase 1 evidence:

- 2026-07-17: Added `CheckoutVersion`, `CurrentStep`, `CompletedStepsJson`, and `LastValidatedCartVersion` to `CheckoutSession` with CommerceNode-only migration `CommerceNodeCheckoutSessionResume`.
- 2026-07-17: Migration backfills existing rows with `CheckoutVersion = 1`, `CompletedStepsJson = []`, state-derived `CurrentStep`, and `LastValidatedCartVersion = CartVersion`.
- 2026-07-17: Added `StartAsync`, `LoadAsync`, `CancelAsync`, and `ExpireAsync` to `StorefrontCheckoutService`; active resume lookup is scoped by current store and current cart token.
- 2026-07-17: Added Storefront API endpoints `POST /checkout/start`, `GET /checkout/{checkoutSessionId}`, and `POST /checkout/{checkoutSessionId}/cancel` with explicit response DTOs and OpenAPI metadata.
- 2026-07-17: Focused `StorefrontCheckoutServiceTests|CommerceNodeStorefrontOpenApiContractTests` run passed 53/53 after OpenAPI snapshot refresh.

Suggested commit:

```text
feat(checkout): add session version and resume
```

## Phase 2 - Entry Validation And Cart Change Detection

Goal: centralize entry validation and stale cart handling.

Implementation checklist:

- [x] Add checkout entry validation service or internal method.
- [x] Validate checkout feature enabled.
- [x] Validate cart token present.
- [x] Validate cart exists.
- [x] Validate cart active.
- [x] Validate cart has lines.
- [x] Validate cart validation has no blocking issues.
- [ ] Validate cart currency/rate snapshots are valid.
- [ ] Validate order total is positive.
- [ ] Add optional checkout-level min/max total hook.
- [ ] Add guest checkout policy hook.
- [x] Store `LastValidatedCartVersion`.
- [x] Detect cart version changes after selected address/payment.
- [x] Mark downstream state stale when cart changed.
- [x] Return clear reset details for stale state.
- [ ] Keep payment method min/max total checks in payment step/place-order.
- [x] Reuse entry validation from start and preview.

Verification checklist:

- [x] Missing/inactive/empty/invalid cart cannot start/review/place-order.
- [x] Cart changes after preview are detected before place order.
- [x] Stale cart state returns stable error code.
- [x] Reset guidance identifies downstream state that must be redone.
- [ ] Guest checkout policy hook does not break current anonymous checkout default.

Exit criteria:

- [x] Entry validation is one reusable server-side path.
- [x] Stale cart behavior is deterministic.

Phase 2 evidence:

- 2026-07-17: Added reusable checkout entry validation for cart token, cart existence, active state, line presence, checkout feature state, and cart validation issues.
- 2026-07-17: Checkout `start` now rejects empty/invalid carts instead of creating resumable sessions for invalid entry state.
- 2026-07-17: Checkout `load` detects cart version drift, resets downstream progress to `draft`/`entry`, clears completed steps and payment selection, and returns issue code `cart.version_changed`.
- 2026-07-17: Focused `StorefrontCheckoutServiceTests` run passed 26/26; focused `CommerceNodeStorefrontOpenApiContractTests` run passed 29/29.

Suggested commit:

```text
feat(checkout): centralize entry validation
```

## Phase 3 - Address Steps

Goal: add billing/shipping address commands without breaking direct guest checkout.

Dependencies:

- [ ] Address Core has address validation service before saved-address selection is enabled.
- [ ] Address Core has customer address book before saved address IDs are accepted.

Implementation checklist:

- [x] Add request DTO for billing address update.
- [x] Add request DTO for shipping address update.
- [x] Support direct billing/shipping address entry.
- [x] Support saved address IDs for authenticated customer when Address Core is available.
- [x] Support `useBillingAsShipping`.
- [x] Validate billing fields with Address Core validation service.
- [x] Validate shipping fields with Address Core validation service.
- [x] Snapshot resolved billing data into checkout session when billing is implemented.
- [x] Snapshot resolved shipping data into checkout session.
- [ ] If cart does not require shipping, skip shipping address step.
- [ ] If cart does not require shipping, skip shipping method step.
- [x] Reset shipping method, payment method availability, review, and terms acknowledgement when shipping address changes.
- [x] Enforce saved address ownership by store/customer.

Verification checklist:

- [x] Guest checkout can still submit direct shipping address.
- [x] Authenticated customer can use saved address when available.
- [x] Address ownership is enforced.
- [x] Deleted/invalid address IDs are rejected.
- [x] Order history remains snapshot-safe.
- [x] Direct address path remains backward compatible.

Exit criteria:

- [x] Address steps are explicit server contracts.
- [x] Checkout does not trust browser ownership fields.

Phase 3 evidence:

- 2026-07-17: Added additive CommerceNode migration `CommerceNodeCheckoutAddressStep` with nullable `billing_address_snapshot_json` and non-null `shipping_address_source`.
- 2026-07-17: Added `UpdateAddressesAsync` and `POST /api/storefront/stores/{storeKey}/checkout/{checkoutSessionId}/addresses` with explicit request/response DTOs.
- 2026-07-17: Direct guest address update snapshots billing/shipping, advances to `payment_method`, and clears payment selection for downstream reset.
- 2026-07-17: Saved address IDs require authenticated customer ownership and store scope; anonymous saved-address selection is rejected.
- 2026-07-17: Focused `StorefrontCheckoutServiceTests` run passed 29/29; focused `CommerceNodeStorefrontOpenApiContractTests` run passed 29/29 after OpenAPI snapshot refresh.

Suggested commit:

```text
feat(checkout): add address step commands
```

## Phase 4 - Shipping Method Stub And Hook

Goal: prepare checkout for shipping method state without overbuilding provider engine.

Implementation checklist:

- [ ] Add shipping requirement resolver hook.
- [ ] Set initial default: cart requires shipping unless product metadata later says all lines are non-shipping.
- [ ] Do not treat POD/fulfillment-provider lines as non-shipping unless product metadata supports it.
- [ ] Add basic shipping option DTO:
  - [ ] key.
  - [ ] display name.
  - [ ] description.
  - [ ] price.
  - [ ] currency.
  - [ ] delivery estimate text.
  - [ ] selected flag.
- [ ] Add initial option resolver:
  - [ ] `shipping_not_required` when resolver says no shipping.
  - [ ] `free_standard` with zero price for MVP stores.
- [ ] Add select shipping option command.
- [ ] Revalidate selected shipping option before review.
- [ ] Revalidate selected shipping option before place-order.
- [ ] Store selected shipping option as JSON or explicit fields.
- [ ] Keep shipping total deterministic.

Verification checklist:

- [ ] Checkout can represent shipping method state.
- [ ] Shipping-not-required path is explicit.
- [ ] Free-standard path is deterministic.
- [ ] Invalid/stale shipping option is rejected.
- [ ] No full provider/rate engine is introduced.

Exit criteria:

- [ ] Later shipping providers can replace resolver without changing checkout step contract.
- [ ] Shipping state does not expand scope into carrier/rate management.

Suggested commit:

```text
feat(checkout): add shipping option stub
```

## Phase 5 - Payment Method Step

Goal: make payment selection explicit and reusable by review/place-order.

Implementation checklist:

- [ ] Add available payment methods projection for checkout session.
- [ ] Reuse `StorePaymentMethod` filtering:
  - [ ] enabled.
  - [ ] display order.
  - [ ] currency.
  - [ ] country.
  - [ ] cart total.
  - [ ] min/max order total.
- [ ] Add selected payment method command.
- [ ] Store selected method in checkout session.
- [ ] Return payment display metadata:
  - [ ] display name.
  - [ ] description.
  - [ ] icon.
  - [ ] provider key.
  - [ ] next action kind.
  - [ ] optional input schema placeholder.
- [ ] Do not return provider secrets or private settings.
- [ ] Do not auto-skip step by default.
- [ ] Reset review and terms acknowledgement when payment method changes.
- [ ] Recalculate payment availability server-side before place-order.

Verification checklist:

- [ ] Checkout review uses selected payment method from session.
- [ ] Place-order uses selected payment method from session.
- [ ] Disabled/unsupported payment method is rejected.
- [ ] Currency/country/min/max filters apply.
- [ ] Public response contains no provider secrets.

Exit criteria:

- [ ] Payment step is explicit and server-owned.
- [ ] Existing payment method model remains the source of truth.

Suggested commit:

```text
feat(checkout): add payment method step
```

## Phase 6 - Review Projection And Terms Hook

Goal: give Storefront V2 a stable review contract before final place order.

Implementation checklist:

- [ ] Add review endpoint.
- [ ] Review response includes checkout session ID/version/state.
- [ ] Review response includes cart version.
- [ ] Review response includes customer/contact summary.
- [ ] Review response includes billing address when available.
- [ ] Review response includes shipping address.
- [ ] Review response includes shipping option.
- [ ] Review response includes payment method.
- [ ] Review response includes cart line summaries.
- [ ] Review response includes subtotal.
- [ ] Review response includes shipping total.
- [ ] Review response includes tax total placeholder.
- [ ] Review response includes discount total placeholder.
- [ ] Review response includes grand total.
- [ ] Review response includes currency.
- [ ] Review response includes validation issues.
- [ ] Review response includes `placeOrderAllowed`.
- [ ] Review response includes next required step.
- [ ] Add terms/legal acknowledgement fields:
  - [ ] `TermsAccepted`.
  - [ ] `TermsVersion`.
  - [ ] `TermsAcceptedAtUtc`.
- [ ] Make terms required only when configuration says so.
- [ ] Initial default may keep terms requirement disabled.
- [ ] Keep all final totals server-calculated.

Verification checklist:

- [ ] Review response is enough for Storefront V2 final confirmation.
- [ ] Review blocks invalid checkout.
- [ ] Place-order is blocked when review has blocking issues.
- [ ] Terms hook exists without forcing legal UX until configured.
- [ ] OpenAPI schema is explicit and generator-safe.

Exit criteria:

- [ ] Storefront can render checkout review from server projection.
- [ ] Final totals remain server-owned.

Suggested commit:

```text
feat(checkout): add review projection
```

## Phase 7 - Place Order Hardening And Completion Rules

Goal: keep final order placement safe across COD and hosted payment flows.

Implementation checklist:

- [ ] Require matching checkout session ID.
- [ ] Require matching checkout version.
- [ ] Require matching cart version.
- [ ] Require idempotency key.
- [ ] Re-run final validation:
  - [ ] checkout not expired.
  - [ ] checkout not cancelled.
  - [ ] checkout not completed.
  - [ ] cart active.
  - [ ] selected address state valid.
  - [ ] selected payment state valid.
  - [ ] selected shipping state valid.
  - [ ] cart lines valid.
  - [ ] payment method still available.
  - [ ] shipping option still available.
  - [ ] total unchanged or explicitly recalculated.
- [ ] Preserve existing idempotency behavior.
- [ ] Return same result for pending hosted payment attempt with same idempotency key.
- [ ] COD/captured flow:
  - [ ] create order.
  - [ ] create/capture payment attempt.
  - [ ] deduct stock.
  - [ ] mark cart ordered.
  - [ ] mark checkout completed.
  - [ ] return order reference.
- [ ] Hosted payment redirect flow:
  - [ ] create payment attempt.
  - [ ] mark checkout `order_pending`.
  - [ ] keep cart active until provider confirmation.
  - [ ] return redirect action.
  - [ ] do not clear cart token until provider confirms captured/completed order.
- [ ] Ensure provider callback captured flow marks checkout completed.
- [ ] Ensure provider callback captured flow marks cart ordered.

Verification checklist:

- [ ] Double submit returns same result.
- [ ] Redirect payment can be resumed/retried after cancel.
- [ ] Cart token cleanup does not break hosted payment completion.
- [ ] Order creation remains transactional for COD.
- [ ] Provider callback does not create duplicate order.
- [ ] Disabled payment method after review is rejected.

Exit criteria:

- [ ] Place-order command is idempotent and state-safe.
- [ ] Completion behavior differs correctly for COD/captured vs hosted pending.

Suggested commit:

```text
feat(checkout): harden place-order completion
```

## Phase 8 - Storefront V2 Integration

Goal: improve UX while keeping frontend scope controlled.

Implementation checklist:

- [ ] Update Storefront API client for checkout start.
- [ ] Update Storefront API client for checkout resume.
- [ ] Update Storefront API client for checkout review.
- [ ] Update Storefront API client for address commands.
- [ ] Update Storefront API client for payment command.
- [ ] Keep initial checkout page as one page with server-backed state.
- [ ] Use review projection for totals.
- [ ] Use review projection for final button state.
- [ ] Show stale cart/reset messages clearly.
- [ ] Preserve antiforgery on local POST forms.
- [ ] Do not move checkout to full WASM yet.
- [ ] Keep payment success/cancel pages noindex.
- [ ] Clear cart token only when checkout result is completed/order created.
- [ ] Preserve recoverable context for hosted payment pending/cancel.

Verification checklist:

- [ ] Existing checkout page still works.
- [ ] User can recover from stale cart.
- [ ] User can recover from cancelled hosted payment.
- [ ] Displayed totals come from server review projection.
- [ ] Hosted payment redirect flow does not lose checkout context prematurely.
- [ ] Storefront V2 host/static tests pass.

Exit criteria:

- [ ] Storefront consumes stateful checkout without full wizard rewrite.
- [ ] Frontend does not duplicate checkout business rules.

Suggested commit:

```text
feat(storefront): consume checkout review state
```

## Phase 9 - QA And Contract Coverage

Goal: finish with regression protection.

Implementation checklist:

- [ ] Update OpenAPI metadata:
  - [ ] stable operation IDs.
  - [ ] summaries.
  - [ ] explicit request DTOs.
  - [ ] explicit response DTOs.
  - [ ] required request bodies.
  - [ ] validation metadata.
  - [ ] error responses.
  - [ ] security metadata where auth is required.
  - [ ] refreshed snapshots.
- [ ] Add/update application tests:
  - [ ] start/resume.
  - [ ] checkout version increments.
  - [ ] cart change resets downstream state.
  - [ ] expired session blocks resume/place-order.
  - [ ] address update resets payment/review.
  - [ ] payment method selection/filtering.
  - [ ] review blocks invalid checkout.
  - [ ] hosted payment pending does not clear cart prematurely.
  - [ ] duplicate idempotency returns same result.
- [ ] Add/update Storefront V2 tests:
  - [ ] one-page checkout still posts.
  - [ ] stale cart message.
  - [ ] hosted payment redirect.
  - [ ] payment success resume/status behavior.
  - [ ] payment cancel resume behavior.
- [ ] Update QA checklists:
  - [ ] `QA-CommerceNode.todo.md`.
  - [ ] `QA-StorefrontV2.todo.md`.
  - [ ] `QA-ControlPlane.todo.md` only for boundary evidence if relevant.
- [ ] Run focused tests.
- [ ] Run visible browser QA if UI changed and runtime is available.

Verification checklist:

- [ ] Contract tests protect new checkout endpoints.
- [ ] Existing payment tests still pass.
- [ ] Existing checkout tests still pass.
- [ ] Storefront checkout remains usable.
- [ ] Active V2 projects touched by this work build.
- [ ] No legacy checkout route is added.

Exit criteria:

- [ ] QA checklist files contain evidence.
- [ ] OpenAPI remains generator-safe.
- [ ] Deferred provider engines remain deferred.

Suggested commit:

```text
test(checkout-core): complete release gate
```

## QA Checklist Seeds

### Commerce Node

- [x] Checkout session state/version fields are additive and CommerceNode-only.
- [x] Checkout start rejects missing, inactive, empty, or invalid cart.
- [x] Checkout resume is store-scoped and cart-scoped.
- [x] Expired/cancelled/completed checkout cannot resume as active.
- [x] Checkout version increments on step updates.
- [x] Cart version changes reset downstream checkout state.
- [x] Address changes reset shipping/payment/review/terms as applicable.
- [ ] Shipping method stub returns deterministic `shipping_not_required` or `free_standard`.
- [ ] Payment method step filters by enabled state, currency, country, total, min, and max.
- [ ] Review projection returns server-owned totals and `placeOrderAllowed`.
- [ ] Place-order requires checkout session ID, checkout version, cart version, and idempotency key.
- [ ] Duplicate idempotency returns same order/payment attempt result.
- [ ] Hosted payment pending does not mark cart ordered or clear checkout context prematurely.
- [ ] Provider captured callback creates one order and closes cart.
- [x] Storefront OpenAPI validates and snapshots pass.

### Storefront V2

- [ ] One-page checkout still renders with active cart.
- [ ] Checkout uses server review projection for totals.
- [ ] Checkout POST still works for COD.
- [ ] Stale cart produces actionable message.
- [ ] Hosted payment redirect preserves resume/payment context.
- [ ] Payment success shows captured/completed state.
- [ ] Payment cancel allows return to checkout.
- [ ] Cart token is cleared only after completed order result.
- [x] Browser requests do not send store ID, customer ID, statuses, totals, or server-owned fields.
- [ ] Browser QA has no unexpected console errors after checkout UI changes.

### Control Plane

- [ ] Control Plane is not part of Storefront checkout runtime.
- [ ] ControlPlane Web does not call CommerceNode checkout APIs directly.
- [ ] No checkout state table is added to `ControlPlaneDbContext`.
- [ ] Any future checkout settings gateway remains behind ControlPlane API.

## Failure Modes To Design Against

- [ ] Preview creates unlimited active sessions and place-order uses the wrong one.
- [ ] Checkout session resumes across stores.
- [ ] Browser sends totals/payment status/order status and backend trusts them.
- [ ] Cart changes after address/payment selection but checkout still places order.
- [ ] Shipping/payment selection remains valid after address changes when it should reset.
- [ ] Hosted payment redirect clears cart token before order exists.
- [ ] Provider callback creates duplicate order.
- [ ] Duplicate place-order with same idempotency key creates multiple orders or payment attempts.
- [ ] Expired checkout session can still place order.
- [ ] Payment method becomes disabled after review but place-order still uses it.
- [ ] Guest checkout policy says disabled but anonymous checkout still succeeds.
- [ ] OpenAPI omits body/security/error schemas for new commands.

## Migration And Compatibility

- [x] Migration is additive for checkout session fields only.
- [x] Existing checkout session columns are not removed.
- [x] Existing checkout session columns are not renamed.
- [x] `preview` contract is not broken in early phases.
- [x] `place-order` contract is not broken in early phases.
- [x] New stateful endpoints can be adopted gradually by Storefront V2.
- [ ] Existing orders remain valid.
- [ ] Existing payment attempts remain valid.
- [ ] Existing checkout session rows default safely:
  - [x] `CheckoutVersion = 1`.
  - [x] `CurrentStep = state-derived value`.
  - [x] `CompletedStepsJson = []`.
  - [x] `LastValidatedCartVersion = CartVersion`.

## Recommended Implementation Order

- [x] Phase 0 - baseline guardrails.
- [x] Phase 1 - checkout session version and resume.
- [x] Phase 2 - entry validation and cart change detection.
- [x] Phase 3 - address steps.
- [ ] Phase 4 - shipping method stub and hook.
- [ ] Phase 5 - payment method step.
- [ ] Phase 6 - review projection and terms hook.
- [ ] Phase 7 - place order hardening and completion rules.
- [ ] Phase 8 - Storefront V2 integration.
- [ ] Phase 9 - QA and contract coverage.

## Definition Of Done

- [ ] Checkout can start, resume, validate, review, and place order safely.
- [ ] Cart changes after checkout selection are detected and reset downstream state.
- [ ] Address/payment/review steps have explicit server contracts.
- [ ] Hosted payment pending flow preserves enough context to resume or retry.
- [ ] Completed order flow closes cart and returns order reference.
- [ ] Direct guest address checkout remains compatible.
- [ ] OpenAPI and tests protect the checkout contract.
- [ ] No legacy checkout route or legacy database is extended.
- [ ] Deferred shipping/tax/discount/workflow engines remain unimplemented.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | Scope | Extend `CheckoutSession` instead of adding a workflow engine | Auto-decided | Preserve working architecture | Current session already owns the right state and persistence boundary. | New workflow engine |
| 2 | Scope | Keep first UI integration as one page | Auto-decided | Reduce blast radius | Backend state can improve without forcing a frontend wizard rewrite. | Mandatory checkout wizard rewrite |
| 3 | Shipping | Add shipping method stub/hook only | Auto-decided | Avoid unused complexity | No shipping provider model exists yet. | Full shipping-rate provider engine |
| 4 | Payment | Reuse `StorePaymentMethod` for payment step | Auto-decided | Reuse local patterns | Existing payment availability/filtering is already store-aware. | Replacing payment method model |
| 5 | Completion | Do not clear cart token for hosted payment pending state | Auto-decided | Recoverability | Redirect payment has not created an order yet, so checkout context must remain recoverable. | Clear cart token immediately after redirect creation |
| 6 | Security | Keep server-owned fields out of checkout requests | Auto-decided | API contract safety | Browser must not control totals, status, store, customer, or order-owned fields. | Trusting client-supplied checkout state |
