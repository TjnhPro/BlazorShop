# Checkout Service Decomposition

Status: proposed
Date: 2026-07-19
Purpose: tach `StorefrontCheckoutService` thanh cac thanh phan nho hon, bat buoc DI dependency, va giu V2 checkout/payment/order flow hien co khong bi thay doi behavior.

## Codebase Baseline

- Active checkout flow da la V2 canonical theo `Storefront V2 Commerce Flow Cutover.todo.md`:
  - Storefront V2/WASM dung local `/api/checkout/*`.
  - Commerce Node scoped Storefront API dung `checkout/start`, `checkout/review`, `checkout/place-order`.
  - Flow legacy `cart/save-checkout`, `orders/confirm`, `orders/current-user/items`, va direct `payments/paypal/capture` da duoc retire khoi active Storefront API.
- `BlazorShop.Infrastructure/Data/CommerceNode/Services/StorefrontCheckoutService.cs` hien co 3,162 lines.
- `StorefrontCheckoutService` constructor hien nhan optional dependencies:
  - `IProductSellabilityResolver?`
  - `IAddressValidationService?`
  - `IShippingCalculator?`
  - `IShippingTaxCalculator?`
  - `IOrderPlacementService?`
- Constructor dang fallback bang `new`:
  - `new ProductSellabilityResolver()`
  - `new AddressValidationService()`
  - `new ShippingCalculator([new InternalFreeStandardShippingProvider()])`
  - `new ZeroShippingTaxCalculator()`
  - `new OrderPlacementService(context, moneyRoundingService, sellabilityResolver)`
- Commerce Node DI da register cac dependency can thiet:
  - `IAddressValidationService`
  - `IProductSellabilityResolver`
  - `IShippingCalculator`
  - `IShippingTaxCalculator`
  - `IOrderPlacementService`
  - `IStorefrontCheckoutService`
- Test `StorefrontCheckoutServiceTests` dang tao service thu cong qua helper `CreateCheckoutService(...)` va chi truyen mot phan dependency. Day la ly do ky thuat fallback dang ton tai trong production constructor.
- `StorefrontCheckoutService.PlaceOrderAsync` hien dang gom nhieu responsibility:
  - validate checkout/session/cart version/idempotency.
  - tinh order lines, subtotal, shipping, tax, currency snapshot.
  - resolve payment method availability.
  - tao `PaymentAttempt`.
  - goi `IStorefrontPaymentProvider`.
  - ghi `PaymentAttemptAuditLog`.
  - goi `IOrderPlacementService`.
- `PaymentAttemptService` da ton tai va dang quan ly payment attempt lifecycle, transition, provider event, va captured-order creation cho callback/webhook path.
- `OrderPlacementService` da ton tai va la owner cua order creation transaction, order snapshot, cart close, stock hook, outbox task.

## Autoplan Decisions

| Decision | Chon | Ly do |
| --- | --- | --- |
| Canonical facade | Giu `IStorefrontCheckoutService` | Controller/API contract da on dinh; refactor nen nam ben trong Infrastructure. |
| Dependency policy | Production constructor bat buoc inject tat ca dependency | DI thieu phai fail som; fallback `new` lam test/prod co graph khac nhau. |
| Test defaults | Chuyen defaults vao test builder/factory | Test co the tao service nhanh nhung production khong bi che loi DI. |
| Payment split | Them internal `CheckoutPaymentCoordinator` | Place-order dang gom payment attempt, provider session, idempotency, audit qua nhieu block dai. |
| Pricing split | Them internal `CheckoutPricingCalculator` | Subtotal/shipping/tax/currency snapshot dang lap va rai trong checkout service. |
| State rules | Tach class thuan, khong interface truoc | `Touch`, expiry, next-step, active-state rules la deterministic logic, test thuan duoc. |
| Tax scope | Giu zero tax hien co | Project hien mac dinh tax = 0; phase nay khong mo Tax Core. |
| Payment attempt service | Khong tao payment lifecycle thu ba | Coordinator phai align/reuse `IPaymentAttemptService` hoac shared helpers neu can, khong copy rule moi. |
| Order placement | Giu `OrderPlacementService` lam owner tao order | Checkout coordinator chi chuan bi payment/placement input, khong tao order truc tiep. |

## Target Shape

```text
StorefrontScopedCheckoutController
  -> IStorefrontCheckoutService
      -> StorefrontCheckoutService facade
          -> CheckoutSessionStateRules
          -> CheckoutPricingCalculator
              -> IShippingCalculator
              -> IShippingTaxCalculator
              -> IMoneyRoundingService
              -> IMoneyConversionService
              -> CommerceNodeDbContext read metadata
          -> CheckoutPaymentCoordinator
              -> IPaymentProviderCapabilityRegistry
              -> IStorefrontPaymentProviderResolver
              -> IPaymentAttemptService or shared payment attempt helpers
              -> CommerceNodeDbContext payment reads
          -> IOrderPlacementService
          -> IStorefrontCartService
          -> IStorefrontCustomerService
          -> IStoreFeatureStateService
```

Rule:

- New classes stay internal/concrete under `BlazorShop.Infrastructure/Data/CommerceNode/Services` or `Services/Checkout`.
- Add Application interfaces only if another active runtime needs the abstraction.
- Do not move EF-heavy checkout orchestration into `BlazorShop.Application`.
- Do not change scoped API routes, request DTOs, response DTOs, operation IDs, or Storefront V2/WASM local API behavior unless a test proves a required compatibility fix.

## Phase 0 - Baseline And Guardrails

- [x] Confirm current file sizes and constructor fallback lines:
  - `StorefrontCheckoutService.cs`.
  - `OrderPlacementService.cs`.
  - `PaymentAttemptService.cs`.
- [x] Confirm active V2 flow remains canonical:
  - Storefront V2 calls local `/api/checkout/review` and `/api/checkout/place-order`.
  - Commerce Node exposes scoped `checkout/start`, `checkout/review`, `checkout/place-order`.
  - Retired routes remain absent from active Storefront API/OpenAPI.
- [x] Add or update static guard tests:
  - `StorefrontCheckoutService` constructor must not contain nullable fallback dependencies after Phase 2.
  - `StorefrontCheckoutService` must not instantiate `ProductSellabilityResolver`, `AddressValidationService`, `ShippingCalculator`, `ZeroShippingTaxCalculator`, or `OrderPlacementService`.
  - Commerce Node DI must register all required checkout dependencies.
- [x] Keep existing cutover tests that protect no legacy PayPal capture dependency and V2 canonical routes.

Phase 0 notes:

- Baseline line counts: `StorefrontCheckoutService.cs` 3,171 lines, `OrderPlacementService.cs` 397 lines, `PaymentAttemptService.cs` 660 lines.
- Baseline constructor still has nullable fallback parameters and `?? new ...` fallback construction for sellability, address validation, shipping, shipping tax, and order placement. This is intentionally recorded before Phase 2 removes it.
- Added checkout source/DI guard tests in `StorefrontCheckoutServiceTests`:
  - `StorefrontCheckoutService_ConstructorFallbackBaseline_IsDocumentedBeforeRequiredDiCutover`
  - `CommerceNodeDi_RegistersCheckoutDependenciesRequiredForRequiredDiCutover`
- Active V2 checkout/cutover/contract tests passed with 99 tests.

Acceptance:

- [x] Baseline evidence is recorded in test names or comments where useful.
- [x] No runtime behavior changes in this phase.
- [x] Focused test run still passes before implementation changes.

## Phase 1 - Test Builder And Constructor Preparation

- [x] Introduce a test-only builder/factory for `StorefrontCheckoutServiceTests`.
- [x] Builder must explicitly provide defaults for:
  - `FixedStoreCurrencyResolver`.
  - `MoneyRoundingService`.
  - `FakeMoneyConversionService`.
  - `StorefrontCustomerService`.
  - `StubStoreFeatureStateService`.
  - `PaymentProviderCapabilityRegistry`.
  - `StorefrontPaymentProviderResolver`.
  - `ProductSellabilityResolver`.
  - `AddressValidationService`.
  - `ShippingCalculator` or fake shipping calculator.
  - `ZeroShippingTaxCalculator`.
  - `OrderPlacementService`.
- [x] Preserve overload ergonomics for tests, but implement them through explicit builder fields.
- [x] Add one test proving builder can override shipping calculator.
- [x] Add one test proving builder can override order placement service.

Phase 1 notes:

- Added `CheckoutServiceTestBuilder` inside `StorefrontCheckoutServiceTests`.
- Existing `CreateCheckoutService(...)` overloads now delegate to the builder while preserving test call ergonomics.
- Builder owns explicit defaults for currency, rounding, conversion, customer, feature state, payment capability/resolver, sellability, address validation, shipping, tax, and order placement services.
- Added reflection-backed override tests for `IShippingCalculator` and `IOrderPlacementService`.

Acceptance:

- [x] `StorefrontCheckoutServiceTests` no longer relies on production constructor defaults.
- [x] Existing checkout service tests stay green.
- [x] No production code changed except if needed for accessibility of concrete test inputs.

## Phase 2 - Remove Production Dependency Fallbacks

- [x] Change `StorefrontCheckoutService` constructor parameters from nullable optional to required:
  - `IProductSellabilityResolver sellabilityResolver`
  - `IAddressValidationService addressValidationService`
  - `IShippingCalculator shippingCalculator`
  - `IShippingTaxCalculator shippingTaxCalculator`
  - `IOrderPlacementService orderPlacementService`
- [x] Remove all `?? new ...` fallback logic from `StorefrontCheckoutService`.
- [x] Use `ArgumentNullException.ThrowIfNull(...)` or equivalent constructor guard if repo pattern supports it.
- [x] Verify `BlazorShop.Infrastructure/Data/CommerceNode/DependencyInjection.cs` already registers every dependency.
- [x] Add DI validation/static test for required dependencies and constructor shape.

Phase 2 notes:

- `StorefrontCheckoutService` production constructor now requires all checkout dependencies explicitly and guards them with `ArgumentNullException.ThrowIfNull`.
- Removed production fallback instantiation of `ProductSellabilityResolver`, `AddressValidationService`, `ShippingCalculator`, `ZeroShippingTaxCalculator`, and `OrderPlacementService`.
- Updated static guard test to assert required constructor shape and absence of fallback source.
- Test builder from Phase 1 now owns all test defaults.

Acceptance:

- [x] Commerce Node API build fails if a checkout dependency is removed from DI.
- [x] Test builder owns all default dependencies.
- [x] Storefront checkout tests pass without behavior change.

## Phase 3 - Extract Checkout Session State Rules

- [x] Create internal pure class, proposed name:
  - `CheckoutSessionStateRules`
- [x] Move deterministic rules from `StorefrontCheckoutService`:
  - active state check.
  - expired transition.
  - touch/version increment.
  - next required step resolution.
  - completed steps normalization if it remains pure.
- [x] Keep persistence in `StorefrontCheckoutService`; the state rules class should not call EF or services.
- [x] Add unit tests for:
  - active states: `Draft`, `Ready`, `OrderPending`.
  - expired checkout transition.
  - review allowed vs next required step.
  - version increment behavior.

Phase 3 notes:

- Added internal pure `CheckoutSessionStateRules` under Commerce Node services.
- Added `InternalsVisibleTo("BlazorShop.Tests")` for Infrastructure so pure internal rules can be tested directly without DB setup.
- `StorefrontCheckoutService` now delegates active-state checks, expired transition, touch/version increment, next-step resolution, and completed-step parsing to the rules class.
- Persistence and EF save behavior remain in `StorefrontCheckoutService`.

Acceptance:

- [x] State transition behavior remains unchanged.
- [x] `StorefrontCheckoutService` loses pure helper code without moving EF calls.
- [x] Tests cover state rules without database setup.

## Phase 4 - Extract Checkout Pricing Calculator

- [x] Create internal service, proposed name:
  - `CheckoutPricingCalculator`
- [x] Move pricing/shipping/tax/currency snapshot responsibility:
  - subtotal calculation from cart lines/order line snapshots.
  - shipping rate currency resolution.
  - shipping option calculation and Storefront option mapping.
  - shipping package line construction from product shipping metadata.
  - zero tax calculation through existing `IShippingTaxCalculator`.
  - total comparison data used by review/place-order.
  - currency rate snapshot validation if it is pricing-specific.
- [x] Keep product sellability/order-line product loading out of calculator unless a narrow DTO is already available.
- [x] Keep tax behavior as current `ZeroShippingTaxCalculator`; do not add tax settings, tax tables, or tax UI.
- [x] Register calculator in Commerce Node DI as scoped if it uses `CommerceNodeDbContext`.
- [x] Add tests for:
  - no-shipping cart returns shipping-not-required totals.
  - physical cart returns selected shipping total.
  - shipping provider error becomes checkout validation issue.
  - converted currency shipping total is rounded consistently.
  - tax total remains `0`.

Phase 4 notes:

- Added `CheckoutPricingCalculator` under Commerce Node services and registered it as scoped in Commerce Node DI.
- `StorefrontCheckoutService` now delegates shipping option resolution, shipping package line construction, shipping rate currency selection, shipping total mapping, shipping issue mapping, and shipping tax calculation to the calculator.
- Product sellability and order-line product loading remain in the checkout facade/order placement path; the calculator only reads product shipping metadata needed to build shipping package lines.
- Tax behavior remains the existing zero-tax policy through `IShippingTaxCalculator` / `ZeroShippingTaxCalculator`; no tax settings, schema, or UI were added.
- Added focused `CheckoutPricingCalculatorTests` for no-shipping carts, physical selected shipping, provider failure mapping, converted shipping rounding, and zero tax.
- Verification:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~CheckoutPricingCalculatorTests|FullyQualifiedName~StorefrontCheckoutServiceTests" --no-restore --nologo --verbosity minimal` passed 64/64.
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore --nologo --verbosity minimal` passed.

Acceptance:

- [x] `StorefrontCheckoutService` delegates pricing/shipping/tax calculation.
- [x] Existing shipping core checkout tests still pass.
- [x] No public checkout DTO or API route changes.

## Phase 5 - Extract Checkout Payment Coordinator

- [x] Create internal service, proposed name:
  - `CheckoutPaymentCoordinator`
- [x] Move payment orchestration from `StorefrontCheckoutService`:
  - payment method availability query/filter.
  - next action kind resolution.
  - idempotent existing attempt lookup for checkout placement.
  - payment attempt creation input.
  - provider resolve and `CreatePaymentSessionAsync`.
  - safe failure mapping.
  - payment attempt audit append or delegation.
- [x] Align with existing `IPaymentAttemptService`:
  - Prefer calling `IPaymentAttemptService.CreateAsync` / `TransitionAsync` where it preserves current behavior.
  - If direct EF is still needed for transaction coupling, isolate it in coordinator and document why.
- [x] Keep `OrderPlacementService` as order creation owner.
- [x] Preserve two current payment paths:
  - COD/captured immediate placement creates order in the same execution strategy/transaction.
  - Hosted/redirect payment creates attempt, marks checkout `OrderPending`, and waits for provider callback/webhook.
- [x] Add tests for:
  - duplicate idempotency key returns same order/payment attempt.
  - hosted duplicate idempotency key returns same payment session.
  - provider failure records failed attempt safely.
  - inactive/unavailable provider blocks place order.
  - payment attempt audit is still written.

Phase 5 notes:

- Added `CheckoutPaymentCoordinator` under Commerce Node services and registered it as scoped in Commerce Node DI.
- `StorefrontCheckoutService` now delegates payment method filtering, next-action resolution, payment method availability checks, provider capability checks, provider session creation, failed attempt mapping, online idempotent attempt lookup, and payment audit append to the coordinator.
- The online/redirect path direct EF writes moved into the coordinator because the attempt, checkout session state, and cart activity update must remain idempotent and persisted together before provider redirect recovery.
- The COD/immediate captured path still performs order placement inside the existing EF execution strategy/transaction and still calls `OrderPlacementService` as the only owner of order creation. The coordinator creates the payment attempt object, calls the provider, maps failure, and appends attempt audit entries.
- Existing service-level tests already cover the Phase 5 contract cases:
  - `PlaceOrderAsync_DuplicateIdempotencyKey_ReturnsSameOrder`.
  - `PlaceOrderAsync_StripeCreatesRedirectAttemptWithoutOrder`.
  - `PlaceOrderAsync_StripeProviderFailureReturnsConflictWithoutOrder`.
  - `PlaceOrderAsync_WhenProviderCapabilityInactive_RejectsBeforeOrder`.
  - captured and failed `payment_attempt.*` audit assertions.
- Verification:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~StorefrontCheckoutServiceTests" --no-restore --nologo --verbosity minimal` passed 59/59.
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~PaymentAttemptServiceTests|FullyQualifiedName~StorefrontScopedPaymentWebhookHardeningTests" --no-restore --nologo --verbosity minimal` passed 13/13.
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore --nologo --verbosity minimal` passed.

Acceptance:

- [x] `StorefrontCheckoutService.PlaceOrderAsync` reads as facade orchestration, not direct payment implementation.
- [x] Payment callback/webhook tests remain green.
- [x] No `IPaymentHandler` or direct PayPal compatibility dependency is reintroduced.

## Phase 6 - Reduce Duplicate Order Line Resolution Safely

- [ ] Compare checkout order-line resolution with `OrderPlacementService.ResolveOrderLinesAsync`.
- [ ] Decide whether to:
  - keep duplication temporarily because checkout needs pre-placement validation and payment line data.
  - or extract shared internal `OrderLineSnapshotResolver`.
- [ ] Only extract if it reduces duplication without changing transaction ownership.
- [ ] If extracted, keep it internal to Commerce Node services and inject required dependencies explicitly.
- [ ] Add tests for:
  - product not found.
  - variant not found.
  - product not purchasable.
  - invalid cart line currency.
  - invalid unit price.

Acceptance:

- [ ] No order snapshot field changes.
- [ ] `OrderPlacementService` remains the only owner of creating and completing an order.
- [ ] Checkout pre-validation and placement validation still both run.

## Phase 7 - Optional Follow-up: PaymentAttemptService And OrderPlacementService Fallbacks

- [ ] Investigate nullable fallback constructors in:
  - `OrderPlacementService`.
  - `PaymentAttemptService`.
- [ ] Decide whether to apply the same required-DI policy after checkout service is stable.
- [ ] Do not combine this with Phase 2 unless focused tests are already green and diff remains small.
- [ ] If implemented, add test builders for affected tests first.

Acceptance:

- [ ] Production services do not silently build substitute dependency graphs.
- [ ] Tests own their defaults.
- [ ] No broad service rewrite.

## Phase 8 - QA, Contracts, And Release Gate

- [ ] Run focused service tests:
  - `StorefrontCheckoutServiceTests`.
  - `StorefrontCartServiceTests`.
  - `PaymentAttemptServiceTests`.
  - `StorefrontCustomerOrderServiceTests`.
  - `StorefrontGuestOrderServiceTests`.
- [ ] Run focused Commerce Node contract/payment tests:
  - `CommerceNodeStorefrontOpenApiContractTests`.
  - `CommerceNodeStorefrontPaymentContractTests`.
  - `StorefrontScopedPaymentWebhookHardeningTests`.
- [ ] Run active V2 builds:
  - `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API`.
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`.
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.Components`.
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.WASM`.
- [ ] Run browser Playwright checkout release subset when implementation phases touch place-order behavior:
  - add product to cart.
  - checkout review.
  - place COD order.
  - account order list/detail/receipt.
  - browser network audit confirms no retired routes.
- [ ] Update `QA-StorefrontV2.todo.md` if browser checkout behavior or verification commands change.
- [ ] Update architecture docs only if public runtime boundary or API contract changes. Expected result: no architecture doc change required for internal service decomposition.

Acceptance:

- [ ] COD order placement creates one order.
- [ ] Hosted payment redirect path remains recoverable.
- [ ] Account order self-service still shows placed order.
- [ ] Storefront OpenAPI snapshots do not change unless intentionally refreshed for unrelated metadata.
- [ ] Browser network does not call retired compatibility routes.

## Not In Scope

- [ ] Rewriting checkout/session schema.
- [ ] Changing Storefront V2/WASM local checkout UI flow.
- [ ] Changing public Commerce Node Storefront checkout API contracts.
- [ ] Implementing Tax Core or Tax UI.
- [ ] Adding PayPal/Stripe provider business logic.
- [ ] Reintroducing `IPaymentHandler` or direct PayPal capture route.
- [ ] Removing legacy `BlazorShop.Presentation/*`.
- [ ] Dropping cart, checkout, payment, or order tables.
- [ ] Moving EF orchestration into `BlazorShop.Application`.

## Failure Modes Registry

| Failure mode | Risk | Mitigation |
| --- | --- | --- |
| Test builder defaults differ from old constructor fallback | Medium | Add focused before/after checkout tests and explicit builder defaults. |
| Payment coordinator duplicates `PaymentAttemptService` rules | High | Reuse `IPaymentAttemptService` where possible; document any direct EF exception. |
| COD placement transaction changes | High | Keep `OrderPlacementService` owner and preserve execution strategy transaction tests. |
| Hosted payment attempt idempotency regresses | High | Add duplicate idempotency tests for `OrderPending` attempts. |
| Pricing calculator changes rounding | High | Keep existing `IMoneyRoundingService` calls and add converted-currency tests. |
| Tax accidentally becomes non-zero | Medium | Keep `ZeroShippingTaxCalculator` and assert `TaxTotal == 0`. |
| Storefront API contract changes unintentionally | Medium | Run OpenAPI contract tests and snapshot checks. |
| Browser checkout still passes unit tests but fails real flow | High | Run Playwright COD order placement and account order visibility. |

## Test Diagram

| Codepath | Existing coverage | New/updated coverage |
| --- | --- | --- |
| Start/load/cancel checkout session | `StorefrontCheckoutServiceTests` | State rules pure tests after extraction. |
| Address update and validation | `StorefrontCheckoutServiceTests`, `AddressValidationServiceTests` | Ensure builder injects `IAddressValidationService`; no fallback. |
| Shipping method selection | `StorefrontCheckoutServiceTests`, shipping provider tests | Pricing calculator tests for shipping required/not required/errors. |
| Review totals | `StorefrontCheckoutServiceTests` | Pricing calculator totals and rounding tests. |
| COD place order | `StorefrontCheckoutServiceTests`, Playwright order email runner | Coordinator tests plus browser COD order release subset. |
| Hosted payment redirect | `StorefrontCheckoutServiceTests`, payment return smoke tests | Coordinator duplicate idempotency/provider session tests. |
| Payment callback/webhook captured order | `PaymentAttemptServiceTests`, webhook hardening tests | Regression run after coordinator extraction. |
| Account order visibility | customer/guest order tests and Playwright release flow | Re-run after place-order behavior phases. |

## Implementation Checklist

- [x] Phase 0 complete.
- [x] Phase 1 complete.
- [x] Phase 2 complete.
- [x] Phase 3 complete.
- [x] Phase 4 complete.
- [x] Phase 5 complete.
- [ ] Phase 6 decision complete.
- [ ] Phase 7 decision complete.
- [ ] Phase 8 release gate complete.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Intake | Keep V2 checkout flow canonical | Mechanical | Reuse existing | V2 cutover already retired legacy routes and tests protect canonical flow. | Reopening legacy cart/checkout/order flow. |
| 2 | Scope | Refactor internally behind `IStorefrontCheckoutService` | Mechanical | Explicit over clever | API/controller contracts stay stable while implementation gets smaller. | New public checkout API surface. |
| 3 | DI | Remove production fallback constructors after test builder exists | Mechanical | Explicit over clever | Missing DI should fail at startup/test, not silently create alternate graph. | Keeping `?? new ...` in production service. |
| 4 | Payment | Extract coordinator without replacing `PaymentAttemptService` | Mechanical | DRY | Existing payment attempt lifecycle already exists; coordinator should orchestrate, not fork rules. | Third payment lifecycle abstraction. |
| 5 | Pricing | Extract calculator but keep zero tax | Mechanical | Pragmatic | Current business decision is tax=0; calculator reduces size without expanding tax scope. | Tax Core in this phase. |
| 6 | State | Use pure class, no interface first | Mechanical | Explicit over clever | State transition logic is deterministic and does not need polymorphism. | Interface-heavy state machine. |

## Release Gate

- [x] `StorefrontCheckoutService` constructor has no nullable optional production dependencies.
- [x] `StorefrontCheckoutService` source has no `new ProductSellabilityResolver`, `new AddressValidationService`, `new ShippingCalculator`, `new ZeroShippingTaxCalculator`, or `new OrderPlacementService`.
- [x] Commerce Node DI registers all checkout dependencies.
- [x] Checkout state rules have pure unit tests.
- [ ] Pricing calculator has focused unit tests for shipping/tax/currency behavior.
- [ ] Payment coordinator has focused unit tests for COD, hosted, provider failure, and idempotency.
- [ ] `OrderPlacementService` remains order creation owner.
- [ ] Focused service and contract tests pass.
- [ ] Storefront V2/Components/WASM builds pass.
- [ ] Playwright COD checkout flow passes if place-order code changed.
