# Payment Core.todo

Generated: 2026-07-17

Source plan: `Payment Core.md`

Status: In progress. Phase 0-1 completed.

Scope: turn the existing checkout payment foundation into a practical provider core for active V2. The goal is enough payment behavior for real store usage without moving PayPal/Stripe SDK logic into Domain/Application and without building a full payment platform.

## Scope Lock

Approved:

- [x] Provider discovery contract:
  - [x] provider system name.
  - [x] active/inactive state.
  - [x] display name.
  - [x] icon.
  - [x] display order.
  - [x] supported stores.
  - [x] supported currencies.
  - [x] supported countries.
  - [x] simple availability rule fields.
  - [x] payment method type: `offline`, `redirect`, `immediate`.
  - [x] `recurring_capable` flag.
- [ ] Payment attempt hardening:
  - [ ] keep existing payment attempt ID/public ID.
  - [ ] keep order ID optional until capture/completion.
  - [ ] provider system name.
  - [ ] amount/currency.
  - [ ] internal status.
  - [ ] provider reference/session ID.
  - [ ] safe failure code/message.
  - [ ] timestamps.
  - [ ] idempotency key.
- [ ] Provider operation abstraction:
  - [ ] create payment session/request.
  - [ ] validate provider-specific input shape.
  - [ ] redirect/client action response.
  - [ ] handle return/cancel.
  - [ ] handle webhook/IPN.
  - [ ] authorize/capture/void/refund hooks as optional unsupported operations.
- [ ] Payment state rules:
  - [ ] order placement does not imply payment completed.
  - [ ] provider account/email is never treated as store customer identity.
  - [ ] callback resolves payment attempt by provider reference/session/attempt ID.
  - [ ] webhook signature verification hook.
  - [ ] webhook idempotency.
  - [ ] out-of-order event handling.
  - [ ] order note/audit trail for payment transitions.
- [ ] Safe public/admin projections:
  - [ ] Storefront receives public method metadata only.
  - [ ] Admin can manage provider activation/settings without exposing secrets back in DTOs.
  - [ ] OpenAPI remains generator-safe.

Deferred:

- [ ] Full PayPal or Stripe business behavior in core.
- [ ] Moving Stripe SDK code into Domain or Application.
- [ ] Recurring billing/subscriptions.
- [ ] Saved cards/tokenization.
- [ ] Disputes, chargebacks, payouts, settlement reconciliation.
- [ ] Multi-capture authorization workflows.
- [ ] Full refund UI or accounting ledger.
- [ ] Advanced rule engine DSL for payment availability.
- [ ] Fraud/risk scoring.
- [ ] Extending legacy `AppDbContext`, legacy `BlazorShop.Presentation`, or legacy payment routes.

## Current Baseline

Payment method and provider shape:

- [x] `StorePaymentMethod` stores `StoreId`, `PaymentMethodKey`, `Enabled`, display metadata, display order, currency/country restrictions, min/max total, and `SettingsJson`.
- [x] `PaymentMethod` is the global catalog record with `Key`, `Name`, `Description`, default enabled flag, and sort order.
- [x] `CommerceNodePaymentMethodService` seeds `cod`, `stripe`, and `paypal` store methods.
- [x] Store payment method admin update validates display metadata, currency/country codes, total limits, and settings JSON.
- [x] Payment method updates invalidate Storefront public configuration cache.
- [x] Payment method updates write admin audit metadata without exposing settings JSON.

Payment attempt foundation:

- [x] `PaymentAttempt` stores public ID, store ID, checkout session ID, optional order ID, method/provider keys, state, amount, currency, currency snapshots, idempotency key, provider references, next action, safe failure fields, metadata JSON, expiration, and timestamps.
- [x] `PaymentAttemptStates` has `created`, `requires_action`, `authorized`, `captured`, `failed`, `cancelled`, and `expired`.
- [x] `PaymentProviderEvent` stores provider event ID, event type, payload hash/json, processed timestamp, and payment attempt relation.
- [x] `PaymentAttemptService` supports:
  - [x] get by store and public attempt ID.
  - [x] idempotent create by store/idempotency key.
  - [x] guarded state transition.
  - [x] provider event recording.
  - [x] duplicate event detection.
  - [x] captured online attempt creates order exactly once.

Checkout integration:

- [x] COD checkout creates order synchronously and marks payment attempt captured.
- [x] Stripe checkout creates a payment attempt, calls hosted payment provider, marks attempt `requires_action`, and sets checkout `order_pending`.
- [x] Captured online payment can later create the order through `PaymentAttemptService.TransitionAsync`.
- [x] Storefront V2 payment success/cancel pages poll payment attempt status.

Current API surface:

- [x] Storefront scoped payment routes exist under `api/storefront/stores/{storeKey}/payments`.
- [x] Current routes include `GET methods`.
- [x] Current routes include `GET attempts/{attemptId}`.
- [x] Current routes include `POST provider-callback/{providerKey}`.
- [x] Current routes include `POST webhooks/{providerKey}`.
- [x] Current routes include legacy-shaped `POST paypal/capture`.
- [x] OpenAPI contract tests already cover payment attempt, callback, webhook, and PayPal capture metadata.

## Gaps To Fix

- [ ] Provider discovery lacks explicit method type/capability contract.
- [ ] Provider installed/global active state is not explicit enough.
- [ ] `IStorefrontPaymentProvider` only creates hosted sessions.
- [ ] Checkout special-cases Stripe for hosted payment.
- [ ] Webhook accepts `X-Provider-Signature` but does not verify it.
- [ ] Public webhook/callback request can carry requested state.
- [ ] Callback/webhook mostly uses public payment attempt ID instead of provider session/reference.
- [ ] Out-of-order events return conflict instead of recording safe ignored state.
- [ ] Payment transitions are not clearly attached to order notes/audit trail.
- [ ] PayPal capture remains a special endpoint.

## Core Decisions

- [x] Keep `StorePaymentMethod` as the store-level activation/config row.
- [x] Add provider capability metadata additively.
- [x] Keep provider SDK code in Infrastructure.
- [x] Use explicit method type/capability instead of hard-coded provider names.
- [ ] Treat webhook payload as untrusted until provider handler verifies it.
- [ ] Keep refund/void/authorize as hooks in this phase, not full flows.
- [ ] Preserve existing public endpoints while hardening provider handling behind them.
- [ ] Do not identify customers by provider email/account.

## Target Boundary

```text
ControlPlane.Web
  -> ControlPlane.API
      -> CommerceNode.API api/commerce/admin/*?storeKey={storeKey}

Storefront.V2
  -> CommerceNode.API api/storefront/stores/{storeKey}/*

CommerceNode.API
  -> Application payment contracts
      -> Infrastructure provider adapters
      -> CommerceNodeDbContext
```

Boundary rules:

- [x] Payment core runtime data belongs to `CommerceNodeDbContext`.
- [x] Control Plane Web never calls Commerce Node directly.
- [x] Storefront V2 calls scoped Storefront payment APIs only.
- [x] No new `api/internal/*` payment behavior.
- [x] No legacy `api/admin/*` or `api/public/*` payment behavior.
- [x] No legacy `AppDbContext` payment migration.

## Target Architecture

- [x] `StorePaymentMethod` owns store activation, public metadata, availability filters, settings JSON/secret references.
- [x] `PaymentProviderDefinition` owns system name, method type, capabilities, and installed/global active state.
- [ ] `IPaymentProvider` owns metadata, input validation, session/request creation, return/cancel, webhook, and optional authorize/capture/void/refund hooks.
- [ ] `PaymentAttempt` owns attempt state, amount/currency, provider reference/session, and idempotency.
- [ ] `PaymentProviderEvent` owns verified/raw event ledger, duplicate detection, processed/ignored state.
- [ ] Checkout selects store payment method, asks provider capability for flow type, creates attempt, and completes order only after internal captured/offline success state.

## Phase 0 - Baseline Contract And Safety Snapshot

Goal: lock current behavior before changing provider core.

Implementation checklist:

- [x] Review active payment files:
  - [x] `StorePaymentMethod`.
  - [x] `PaymentAttempt`.
  - [x] `PaymentProviderEvent`.
  - [x] `CommerceNodePaymentDtos`.
  - [x] `PaymentAttemptService`.
  - [x] `CommerceNodePaymentMethodService`.
  - [x] `StorefrontCheckoutService`.
  - [x] `StorefrontScopedPaymentsController`.
  - [x] Storefront payment API client.
  - [x] Storefront payment success/cancel pages.
- [x] Record current OpenAPI operation IDs for payment routes.
- [x] Confirm no active V2 payment feature depends on legacy `PaymentController`.
- [x] Confirm COD checkout baseline.
- [x] Confirm Stripe redirect baseline.
- [x] Confirm payment attempt status polling baseline.
- [x] Add/update QA checklist items for payment provider core.
- [x] Add missing baseline tests only if current risks are not protected.
- [x] Make no behavior/schema/route changes unless needed for guardrails.

Verification checklist:

- [x] Existing payment attempt tests pass.
- [x] Existing Storefront OpenAPI payment contract tests pass.
- [x] Existing checkout/payment focused tests pass.
- [x] Current COD and Stripe redirect behavior is documented as baseline.
- [x] No legacy payment route or `AppDbContext` change is introduced.

Exit criteria:

- [x] Current payment behavior is protected before provider core refactor.
- [x] QA checklist contains baseline evidence.

Phase 0 evidence:

- 2026-07-17: Reviewed active V2 payment entities, DTOs, services, provider adapter/resolver, Storefront payment controller, Storefront API client, and payment return pages.
- 2026-07-17: Current Storefront payment OpenAPI operation IDs are `StorefrontPayments_GetPaymentMethods`, `StorefrontPayments_GetAttempt`, `StorefrontPayments_HandleProviderCallback`, `StorefrontPayments_HandleWebhook`, and `StorefrontPayments_CapturePayPal`.
- 2026-07-17: Confirmed active V2 payment runtime uses `CommerceNodeDbContext`; legacy `PaymentController` remains in legacy presentation tests only and is not extended for V2.
- 2026-07-17: Confirmed current gap for later phases: webhook/callback accepts requested `State` and `X-Provider-Signature` is not verified yet.
- 2026-07-17: Focused payment baseline `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~PaymentAttemptServiceTests|FullyQualifiedName~CommerceNodePaymentMethodSecretBoundaryTests|FullyQualifiedName~StripeStorefrontPaymentProviderTests|FullyQualifiedName~CommerceNodeStorefrontPaymentContractTests|FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests|FullyQualifiedName~StorefrontCheckoutServiceTests|FullyQualifiedName~PaymentSuccess|FullyQualifiedName~PaymentCancel"` passed 86/86.

Suggested commit:

```text
test(payment-core): lock provider baseline
```

## Phase 1 - Provider Discovery And Capability Metadata

Goal: describe provider behavior without hard-coding provider names in checkout.

Implementation checklist:

- [x] Add provider definition/capability model in Application or Domain constants:
  - [x] `SystemName`.
  - [x] `DisplayName`.
  - [x] `Description`.
  - [x] `IconUrl`.
  - [x] `DefaultDisplayOrder`.
  - [x] `MethodType`: `offline`, `redirect`, `immediate`.
  - [x] `RecurringCapable`.
  - [x] `SupportsAuthorize`.
  - [x] `SupportsCapture`.
  - [x] `SupportsVoid`.
  - [x] `SupportsRefund`.
  - [x] `SupportsPartialRefund`.
  - [x] `RequiresWebhookSignature`.
- [x] Keep `StorePaymentMethod` as the store-specific row.
- [n/a] Add store method fields only if needed and additive:
  - [n/a] `PaymentMethodType`.
  - [n/a] `AvailabilityRuleJson`, or defer if currency/country/min/max is enough.
- [x] Prefer code/provider registry for installed capabilities before adding database table.
- [x] Add `IPaymentProviderRegistry` or equivalent resolver.
- [x] Make COD definition explicit as offline.
- [x] Make Stripe definition explicit as redirect.
- [x] Make PayPal definition explicit as disabled/skeleton unless a real adapter exists.
- [n/a] Add non-secret provider capability metadata to admin payment method DTO if needed.
- [n/a] Add safe method type/next-action expectation to Storefront payment method response only if needed by UI.

Verification checklist:

- [x] Provider registry returns COD as offline.
- [x] Provider registry returns Stripe as redirect.
- [x] Provider registry returns PayPal as disabled/skeleton when no adapter exists.
- [x] Unknown provider is rejected with clear validation.
- [x] Store payment methods still order by `DisplayOrder`.
- [x] Public DTOs expose no `SettingsJson` or secret values.
- [x] Existing payment methods endpoint remains backward compatible.

Exit criteria:

- [x] Checkout can ask provider capability by key.
- [x] Capability metadata is safe for public/admin projections.

Phase 1 evidence:

- 2026-07-17: Added `PaymentProviderCapabilityDto`, `PaymentProviderMethodTypes`, and `IPaymentProviderCapabilityRegistry` in the Application payment contract.
- 2026-07-17: Added infrastructure `PaymentProviderCapabilityRegistry` using registered `IStorefrontPaymentProvider` adapters; COD is explicit offline, Stripe is redirect and installed when adapter exists, PayPal is disabled skeleton.
- 2026-07-17: No database table or `StorePaymentMethod` field was added; store-level activation remains in `StorePaymentMethod`.
- 2026-07-17: Added `PaymentProviderCapabilityRegistryTests` and display-order guard in `CommerceNodePaymentMethodSecretBoundaryTests`.
- 2026-07-17: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~PaymentProviderCapabilityRegistryTests|FullyQualifiedName~CommerceNodePaymentMethodSecretBoundaryTests"` passed 7/7.
- 2026-07-17: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontCheckoutServiceTests|FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests|FullyQualifiedName~StripeStorefrontPaymentProviderTests"` passed 70/70.

Suggested commit:

```text
feat(payment-core): add provider capability metadata
```

## Phase 2 - Provider Operation Contract

Goal: define core operations while keeping provider-specific SDK logic outside core.

Implementation checklist:

- [ ] Introduce operation-oriented provider interface:
  - [ ] `ValidateInputAsync`.
  - [ ] `CreatePaymentSessionAsync`.
  - [ ] `HandleReturnAsync`.
  - [ ] `HandleCancelAsync`.
  - [ ] `HandleWebhookAsync`.
  - [ ] `AuthorizeAsync`.
  - [ ] `CaptureAsync`.
  - [ ] `VoidAsync`.
  - [ ] `RefundAsync`.
- [ ] Unsupported operations return typed unsupported result instead of throwing by default.
- [ ] Define result model for provider action type:
  - [ ] `none`.
  - [ ] `redirect`.
  - [ ] `client_secret`.
  - [ ] `offline_instructions`.
- [ ] Define result model fields for provider session/reference values.
- [ ] Define result model fields for safe failure code/message.
- [ ] Define result model fields for provider event transition recommendation.
- [ ] Define ignored/no-op reason for out-of-order events.
- [ ] Adapt current `StripeStorefrontPaymentProvider` to new contract.
- [ ] Add COD provider adapter or bridge current COD handler behind provider contract.
- [ ] Keep old `IPaymentHandler` only as compatibility until checkout cutover is complete.

Verification checklist:

- [ ] Unsupported operation returns `payment.operation_not_supported`.
- [ ] Stripe create session still returns redirect next action.
- [ ] COD operation completes synchronously without hosted session.
- [ ] Provider failures store safe failure details, not raw exception details.
- [ ] Stripe SDK remains in Infrastructure.

Exit criteria:

- [ ] New provider can be added through registry/adapter without editing checkout branching.
- [ ] Core contracts remain provider SDK-free.

Suggested commit:

```text
feat(payment-core): define provider operation contract
```

## Phase 3 - Checkout Cutover From Provider Name To Capability

Goal: remove checkout dependency on `isStripe` and route by payment method type.

Implementation checklist:

- [ ] Replace hard-coded `isStripe` branch in `StorefrontCheckoutService`.
- [ ] Route offline/immediate methods by provider capability.
- [ ] Route redirect/client-action methods by provider capability.
- [ ] Offline/immediate flow:
  - [ ] create attempt.
  - [ ] process synchronously.
  - [ ] create order if captured/paid.
- [ ] Redirect/client-action flow:
  - [ ] create attempt.
  - [ ] request provider session.
  - [ ] set attempt `requires_action`.
  - [ ] keep checkout `order_pending`.
- [ ] Keep current COD response shape compatible.
- [ ] Keep current Stripe redirect response shape compatible.
- [ ] Do not clear/close cart for pending hosted payment until captured/completed.
- [ ] Keep `PlaceOrderAsync` semantics as "start payment/order placement", not always "order completed".
- [ ] Keep `OrderId` null for redirect attempt until provider capture.
- [ ] Duplicate idempotency key returns existing attempt/order state.

Verification checklist:

- [ ] COD still completes order and marks cart ordered.
- [ ] Stripe still creates redirect attempt without order.
- [ ] Unknown unsupported provider fails before order creation.
- [ ] Duplicate idempotency for redirect returns same attempt.
- [ ] Pending redirect does not close cart as completed.
- [ ] Existing Storefront V2 checkout still works.

Exit criteria:

- [ ] Checkout no longer checks provider key `stripe` to choose flow.
- [ ] Flow selection is capability-driven.

Suggested commit:

```text
feat(checkout): route payment by provider capability
```

## Phase 4 - Webhook And Callback Hardening

Goal: make provider callbacks secure and replay-safe enough for real usage.

Implementation checklist:

- [ ] Keep `POST /payments/webhooks/{providerKey}`.
- [ ] Keep `POST /payments/provider-callback/{providerKey}` for browser returns.
- [ ] Stop trusting requested `State` from public request body.
- [ ] Provider handler parses verified payload and returns internal transition command.
- [ ] Add `IPaymentWebhookSignatureVerifier` or provider-level verification method.
- [ ] Reject missing signature for providers requiring signatures.
- [ ] Reject invalid signature for providers requiring signatures.
- [ ] Keep raw payload hash for dedupe/audit.
- [ ] Never log raw provider secrets.
- [ ] Resolve payment attempt by public payment attempt ID when present and valid.
- [ ] Resolve payment attempt by provider session ID.
- [ ] Resolve payment attempt by provider reference/payment intent/order ID.
- [ ] Resolve payment attempt by metadata embedded at provider session creation.
- [ ] Ensure provider account/email does not resolve or create customer identity.
- [ ] Add processed status fields if needed:
  - [ ] `ProcessingStatus`: `recorded`, `processed`, `ignored`, `failed`.
  - [ ] `IgnoreReason`.
  - [ ] `FailureReason`.
- [ ] Duplicate event returns accepted/deduped without reprocessing.
- [ ] Out-of-order event records and no-ops when transition is not currently valid.

Verification checklist:

- [ ] Missing signature is rejected for signature-required provider.
- [ ] Invalid signature is rejected.
- [ ] Duplicate webhook does not create duplicate order.
- [ ] Webhook without attempt ID resolves by provider session/reference.
- [ ] Out-of-order event is recorded and ignored safely.
- [ ] Callback cancel marks attempt cancelled only when provider handler confirms cancellation.
- [ ] Public webhook cannot force arbitrary payment state.

Exit criteria:

- [ ] Replay and out-of-order events are safe.
- [ ] OpenAPI metadata is valid and updated.

Suggested commit:

```text
feat(payment-core): harden webhooks
```

## Phase 5 - Payment Attempt State, Order Notes, And Audit Trail

Goal: make payment state changes explainable to admins and support.

Implementation checklist:

- [ ] Reuse existing order audit/note model if one exists and fits.
- [ ] Add lightweight payment/order note entity only if needed.
- [ ] Minimum note fields:
  - [ ] `StoreId`.
  - [ ] nullable `OrderId`.
  - [ ] `PaymentAttemptId`.
  - [ ] `ProviderKey`.
  - [ ] `EventType`.
  - [ ] `OldState`.
  - [ ] `NewState`.
  - [ ] `Message`.
  - [ ] `CreatedAtUtc`.
  - [ ] safe/sanitized `MetadataJson`.
- [ ] Append note/audit on attempt created.
- [ ] Append note/audit on requires action.
- [ ] Append note/audit on authorized.
- [ ] Append note/audit on captured.
- [ ] Append note/audit on failed.
- [ ] Append note/audit on cancelled.
- [ ] Append note/audit on expired.
- [ ] Add refund hook accepted/rejected audit later only if hook is used.
- [ ] Store raw provider error code separately only if needed and sanitized.
- [ ] Public response uses stable safe code/message.

Verification checklist:

- [ ] Captured transition appends audit/note.
- [ ] Failed transition stores safe failure code/message.
- [ ] No secret values are written to audit metadata.
- [ ] Terminal state cannot be overwritten by late event.
- [ ] Admin/support can understand payment history without raw provider payload.

Exit criteria:

- [ ] Payment transitions are explainable and secret-safe.
- [ ] Public responses remain safe.

Suggested commit:

```text
feat(payment-core): add payment state audit trail
```

## Phase 6 - Admin And Public Projection Cleanup

Goal: make provider configuration manageable and safe.

Implementation checklist:

- [ ] Keep Commerce Admin route under `api/commerce/admin/payment-methods`.
- [ ] Add capability metadata to admin response.
- [ ] Allow enabling/disabling only supported providers.
- [ ] Validate provider settings JSON by provider when validator exists.
- [ ] Preserve omitted `SettingsJson` behavior so secrets are not cleared accidentally.
- [ ] Allow clearing settings only with explicit flag.
- [ ] Continue audit log metadata with settings configured/changed status only.
- [ ] Keep ControlPlane gateway path: `ControlPlane.Web -> ControlPlane.API -> CommerceNode.API`.
- [ ] Reuse existing provider/settings permissions where possible.
- [ ] Do not let ControlPlane Web call CommerceNode directly.
- [ ] Storefront public payment methods response remains allowlisted:
  - [ ] display name.
  - [ ] short description.
  - [ ] icon.
  - [ ] display order.
  - [ ] supported currency/country metadata if useful.
  - [ ] method type/next action hint if needed.
- [ ] Never return `SettingsJson`.
- [ ] Never return secret keys.
- [ ] Never return webhook secrets.
- [ ] Never return provider account internal config.
- [ ] Never return raw provider payload.

Verification checklist:

- [ ] Admin update preserves settings when settings omitted.
- [ ] Admin clear settings works only with explicit flag.
- [ ] Public config never includes secret fields.
- [ ] Payment metadata cache invalidates on update.
- [ ] Control Plane gateway uses Commerce admin route and `storeKey` query.
- [ ] ControlPlane Web has no direct CommerceNode payment calls.

Exit criteria:

- [ ] Admin can manage provider activation safely.
- [ ] Storefront can render payment choices without private settings.

Suggested commit:

```text
feat(payment-core): expose safe provider configuration
```

## Phase 7 - PayPal Compatibility And Provider Route Cleanup

Goal: avoid a permanent special PayPal capture path.

Implementation checklist:

- [ ] Keep `POST /payments/paypal/capture` only as compatibility during migration.
- [ ] Implement PayPal through provider operation contract only when a real PayPal adapter is added.
- [ ] Mark special route as deprecated in code comments/OpenAPI description if retained.
- [ ] Add tests proving special route is not used by new checkout flow.
- [ ] Do not fake successful PayPal capture without verified provider response.
- [ ] Keep Storefront contract stable while route is retained.
- [ ] Document removal criteria for compatibility route.

Verification checklist:

- [ ] New payment flow does not require provider-specific Storefront controller actions.
- [ ] PayPal can be completed later by adding adapter, not rewriting checkout.
- [ ] PayPal compatibility route remains POST-only if retained.
- [ ] OpenAPI clearly describes compatibility/deprecation status if retained.

Exit criteria:

- [ ] Provider-specific route does not drive new checkout architecture.
- [ ] PayPal compatibility is explicit and bounded.

Suggested commit:

```text
refactor(payment-core): align paypal route with provider core
```

## Phase 8 - Contract, QA, And Documentation

Goal: finish the phase with verifiable behavior and clear API guarantees.

Implementation checklist:

- [ ] Update OpenAPI operation summaries where DTOs change.
- [ ] Keep stable operation IDs where possible.
- [ ] Add/refresh explicit request DTO schemas.
- [ ] Add/refresh explicit response DTO schemas.
- [ ] Add required body metadata for webhook/callback routes.
- [ ] Add required header metadata for signature routes.
- [ ] Add standard error responses:
  - [ ] invalid signature.
  - [ ] provider not supported.
  - [ ] attempt not found.
  - [ ] duplicate event accepted.
  - [ ] transition ignored/conflict.
- [ ] Add/update application tests:
  - [ ] provider registry.
  - [ ] operation unsupported result.
  - [ ] attempt transition rules.
  - [ ] event dedupe.
  - [ ] out-of-order event handling.
  - [ ] captured creates order once.
- [ ] Add/update infrastructure tests:
  - [ ] Stripe hosted session adapter.
  - [ ] settings validation.
  - [ ] signature verifier behavior with fake provider.
- [ ] Add/update presentation contract tests:
  - [ ] Swagger valid.
  - [ ] response schemas exist.
  - [ ] request bodies required.
  - [ ] signature header documented.
  - [ ] no domain entities in public schemas.
- [ ] Add/update Storefront smoke/static tests:
  - [ ] COD checkout completes.
  - [ ] Stripe redirect attempt stays pending and can be polled.
  - [ ] payment cancel page shows cancelled/failed state safely.
- [ ] Update `QA-CommerceNode.todo.md`.
- [ ] Update `QA-StorefrontV2.todo.md` only if Storefront V2 behavior changes.
- [ ] Update `QA-ControlPlane.todo.md` only if gateway/admin behavior changes.

Verification checklist:

- [ ] Focused payment tests pass.
- [ ] Focused checkout tests pass.
- [ ] Storefront OpenAPI contract tests pass.
- [ ] Swagger snapshot updated when contract changes are intentional.
- [ ] QA checklist records payment core coverage.

Exit criteria:

- [ ] Payment provider core is contract-protected.
- [ ] Public/admin projections are secret-safe.
- [ ] Deferred payment-platform scope remains deferred.

Suggested commit:

```text
test(payment-core): verify provider core
```

## QA Checklist Seeds

### Commerce Node

- [ ] Provider registry returns COD offline, Stripe redirect, PayPal disabled/skeleton, and rejects unknown provider.
- [ ] Checkout routes payment by provider capability, not hard-coded provider key.
- [ ] COD checkout completes synchronously and marks cart ordered.
- [ ] Stripe redirect checkout creates pending attempt without order until capture.
- [ ] Duplicate idempotency returns same order/payment attempt result.
- [ ] Pending redirect does not clear/close cart as completed.
- [ ] Missing webhook signature is rejected when provider requires signature.
- [ ] Invalid webhook signature is rejected.
- [ ] Duplicate webhook does not create duplicate order.
- [ ] Webhook can resolve attempt by provider session/reference.
- [ ] Out-of-order provider event is recorded and ignored safely.
- [ ] Captured online payment creates one order exactly once.
- [ ] Terminal payment state cannot be overwritten by late event.
- [ ] Payment audit/note metadata contains no secrets.
- [ ] Public payment method response does not expose `SettingsJson` or secrets.
- [ ] Admin payment method update preserves settings when omitted.
- [ ] Admin clear settings requires explicit flag.
- [ ] Storefront OpenAPI validates and snapshots pass.

### Storefront V2

- [ ] COD checkout still completes.
- [ ] Stripe redirect attempt stays pending and can be polled.
- [ ] Payment success page shows captured/completed state.
- [ ] Payment cancel page shows cancelled/failed state safely.
- [ ] Hosted payment pending flow does not lose checkout/cart context prematurely.
- [ ] Public payment method metadata is enough to render choices.
- [ ] Browser sees no provider secrets or raw settings in network payloads.
- [ ] Browser QA has no unexpected console errors after payment UI behavior changes.

### Control Plane

- [ ] ControlPlane Web does not call CommerceNode payment APIs directly.
- [ ] ControlPlane API gateway uses Commerce admin route with `storeKey` query.
- [ ] Provider settings permissions are enforced.
- [ ] Admin provider settings responses do not echo secrets.
- [ ] No payment core runtime data is stored in `ControlPlaneDbContext`.

## Failure Modes To Design Against

- [ ] Webhook can force payment captured.
- [ ] Duplicate webhook creates duplicate order.
- [ ] Late failed event overwrites captured attempt.
- [ ] Provider settings leak to Storefront.
- [ ] Checkout clears cart while redirect payment pending.
- [ ] Adding PayPal requires checkout rewrite.
- [ ] Raw provider error shown to customer.
- [ ] Admin cannot diagnose payment issue.
- [ ] Availability rules become too complex.
- [ ] Legacy payment service accidentally extended.

## Test Map

- [ ] Provider registry tests:
  - [ ] COD offline.
  - [ ] Stripe redirect.
  - [ ] PayPal disabled/skeleton.
  - [ ] unknown provider rejected.
- [ ] Checkout routing tests:
  - [ ] offline complete.
  - [ ] redirect pending.
  - [ ] unsupported provider.
  - [ ] duplicate idempotency.
- [ ] Attempt state tests:
  - [ ] allowed transitions.
  - [ ] terminal rejection.
  - [ ] captured creates order once.
  - [ ] safe failure fields.
- [ ] Webhook security tests:
  - [ ] missing signature.
  - [ ] invalid signature.
  - [ ] valid signature.
  - [ ] duplicate event.
  - [ ] out-of-order event.
- [ ] Attempt resolution tests:
  - [ ] resolve by public attempt ID.
  - [ ] resolve by provider session ID.
  - [ ] resolve by provider reference.
- [ ] Public projection tests:
  - [ ] no settings JSON/secrets.
  - [ ] safe method type.
  - [ ] schema stable.
- [ ] Admin projection tests:
  - [ ] preserve settings.
  - [ ] clear settings.
  - [ ] validate provider settings.
  - [ ] audit safe metadata.
- [ ] OpenAPI tests:
  - [ ] required request bodies.
  - [ ] signature header.
  - [ ] error responses.
  - [ ] generator-safe schemas.

## Migration And Compatibility

- [ ] Use additive migrations only.
- [ ] Keep existing `PaymentAttempt` rows valid.
- [ ] Keep existing `PaymentProviderEvent` rows valid.
- [ ] Keep current `PaymentAttemptStates` names unless new state is unavoidable.
- [ ] Preserve current Storefront response fields.
- [ ] Add optional fields rather than renaming.
- [ ] Existing Stripe redirect flow keeps returning `NextAction.Type = redirect`.
- [ ] COD remains usable without external provider configuration.
- [ ] Existing Control Plane gateway routes continue to work.
- [ ] Do not remove `paypal/capture` until Storefront and contract tests prove it is unused or replaced.

## Later Backlog Not In Scope

- [ ] Full Stripe webhook implementation for every event type.
- [ ] Full PayPal order/capture integration.
- [ ] Refund admin UI.
- [ ] Partial refund accounting.
- [ ] Saved payment methods.
- [ ] Recurring billing.
- [ ] Multi-capture authorization.
- [ ] Dispute and chargeback workflow.
- [ ] Provider reconciliation jobs.
- [ ] Fraud scoring.
- [ ] Payment routing across multiple provider accounts.

## Recommended Implementation Order

- [ ] Phase 0 - baseline contract and safety snapshot.
- [ ] Phase 1 - provider discovery and capability metadata.
- [ ] Phase 2 - provider operation contract.
- [ ] Phase 3 - checkout cutover from provider name to capability.
- [ ] Phase 4 - webhook and callback hardening.
- [ ] Phase 5 - payment attempt state, order notes, and audit trail.
- [ ] Phase 6 - admin and public projection cleanup.
- [ ] Phase 7 - PayPal compatibility and provider route cleanup.
- [ ] Phase 8 - contract, QA, and documentation.

## Definition Of Done

- [ ] COD checkout still completes synchronously.
- [ ] Stripe redirect checkout still creates a pending payment attempt without creating an order until captured.
- [ ] Checkout no longer hard-codes Stripe as the only redirect provider path.
- [ ] Provider discovery returns safe capability metadata.
- [ ] Public APIs never expose provider secrets or raw settings.
- [ ] Webhook events are signature-aware, idempotent, and cannot force arbitrary state.
- [ ] Duplicate and out-of-order provider events are safe.
- [ ] Captured online payment creates one order exactly once.
- [ ] Admin provider settings remain secret-safe and audited.
- [ ] Active V2 API contract tests and focused payment tests pass.
- [ ] No legacy payment route or legacy database is extended.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Scope | Keep core provider-agnostic and SDK logic in Infrastructure. | Auto-decided | Explicit boundary | Matches existing Stripe adapter placement and avoids Domain/Application depending on provider SDKs. | Moving PayPal/Stripe logic into core. |
| 2 | Scope | Use existing `StorePaymentMethod` as store activation model. | Auto-decided | Preserve working code | It already owns store-specific display, enablement, restrictions, settings, cache invalidation, and audit. | Replacing with a new provider table first. |
| 3 | Engineering | Add capability metadata before checkout cutover. | Auto-decided | Reduce blast radius | Checkout can route by method type only after provider capabilities exist. | Directly editing checkout with another provider-name branch. |
| 4 | Security | Do not trust webhook/callback body state. | Auto-decided | Security first | Public mutation endpoints must verify provider-originated events before internal state changes. | Letting request `State` drive transition. |
| 5 | Product | Defer full refunds, recurring payments, tokenization, and disputes. | Auto-decided | Avoid unused complexity | Project is moving from MVP to practical usage, not building a full payment platform. | Implementing a broad payment suite now. |
| 6 | Compatibility | Keep existing payment routes while hardening internals. | Auto-decided | Bias toward safe migration | Storefront and OpenAPI clients should not break during provider core refactor. | Removing or renaming routes in the first phase. |
