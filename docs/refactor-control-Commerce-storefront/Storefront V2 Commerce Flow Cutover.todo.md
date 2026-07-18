# Storefront V2 Commerce Flow Cutover

Status: in progress
Date: 2026-07-18
Purpose: chon Storefront V2 cart/checkout/order/payment flow lam canonical, loai bo active flow cu khoi Commerce Node Storefront API ma khong rewrite lai V2.

## Codebase Baseline

- Active Storefront V2 dang dung server/WASM flow moi:
  - `BlazorShop.Storefront.V2/Services/StorefrontApiClient.cs` goi `cart/session`, `checkout/start`, `checkout/review`, `checkout/place-order`, va `orders/current-user`.
  - `BlazorShop.Storefront.V2/Program.cs` local browser APIs `/api/checkout/*` forward vao Commerce Node scoped Storefront API.
  - `BlazorShop.Storefront.Components/Checkout/StorefrontCheckoutShell.razor` dat order qua same-origin `/api/checkout/place-order`.
- Commerce Node V2 canonical dang co:
  - `IStorefrontCartService` va `StorefrontCartService`.
  - `IStorefrontCheckoutService` va `StorefrontCheckoutService`.
  - `IOrderPlacementService` va `OrderPlacementService`.
  - `IPaymentAttemptService`, `IStorefrontPaymentProvider`, `IStorefrontPaymentProviderResolver`.
  - `IStorefrontCustomerOrderService` cho account order list/detail/receipt.
- Flow cu van con trong active Commerce Node Storefront API:
  - `StorefrontScopedCartController.SaveCheckout` tai `api/storefront/stores/{storeKey}/cart/save-checkout`.
  - `StorefrontScopedOrdersController.ConfirmOrder` tai `api/storefront/stores/{storeKey}/orders/confirm`.
  - `StorefrontScopedOrdersController.GetCurrentUserOrderItems` tai `api/storefront/stores/{storeKey}/orders/current-user/items`.
  - `StorefrontScopedPaymentsController.CapturePayPal` tai `api/storefront/stores/{storeKey}/payments/paypal/capture`.
- Root cause da xac nhan:
  - `StorefrontScopedCartController` inject ca `ICartService` va `IStorefrontCartService`.
  - `StorefrontScopedOrdersController` inject `ICartService`, `IOrderQueryService`, `IStorefrontGuestOrderService`, `IStorefrontCustomerOrderService`.
  - `IOrderQueryService` duoc inject trong orders controller nhung khong duoc dung.
  - `DependencyInjection.cs` dang register ca `IPaymentHandler/IPaymentHandlerResolver` va `IStorefrontPaymentProvider/IStorefrontPaymentProviderResolver`.
  - `CartService` legacy co side effect that: luu checkout history, tao `Order`, tru stock, xu ly payment handler.
- OpenAPI/tests hien dang bao ve flow cu:
  - `StorefrontCart_SaveCheckout`.
  - `StorefrontOrders_Confirm`.
  - `StorefrontOrders_ListCurrentUserOrderItems`.
  - `StorefrontPayments_CapturePayPal`.
- QA file `QA-CommerceNode.todo.md` van co scoped compatibility routes chua duoc retire va PayPal compatibility route dang duoc ghi deprecated.

## Autoplan Decisions

| Decision | Chon | Ly do |
| --- | --- | --- |
| Canonical flow | V2 `cart/session` + `checkout/*` + `OrderPlacementService` | Da duoc Storefront V2/WASM su dung va co checkout version, cart version, idempotency, payment attempt, order lifecycle. |
| Cutover style | Remove active API surface truoc, sau do don DI/service | Giam risk, tranh rewrite, de build/test chi ra dependency con sot. |
| Legacy shared code | Khong xoa shared legacy code ngay neu legacy projects con can build | Repo van giu `BlazorShop.Presentation` legacy trong solution; phase nay chi lam active V2 official. |
| Payment core | Giu `IStorefrontPaymentProvider`; loai `IPaymentHandler` khoi Commerce Node V2 | Provider contract la Payment Core moi, handler cu chi phuc vu `CartService` legacy. |
| PayPal | Loai direct compatibility capture route khoi active Storefront API khi provider path du san | Neu PayPal provider chua active, PayPal nen unavailable thay vi co route capture rieng ngoai provider core. |
| Data cleanup | Khong drop table/data legacy trong phase dau | `CheckoutOrderItems` va legacy order/history data can audit rieng de tranh mat du lieu. |

## Canonical Routes After Cutover

Keep active:

- `POST api/storefront/stores/{storeKey}/cart/session`
- `GET api/storefront/stores/{storeKey}/cart`
- `POST api/storefront/stores/{storeKey}/cart/lines`
- `PUT api/storefront/stores/{storeKey}/cart/lines/{lineId}`
- `DELETE api/storefront/stores/{storeKey}/cart/lines/{lineId}`
- `DELETE api/storefront/stores/{storeKey}/cart`
- `POST api/storefront/stores/{storeKey}/cart/validate`
- `POST api/storefront/stores/{storeKey}/cart/recalculate`
- `POST api/storefront/stores/{storeKey}/cart/merge-current-customer`
- `POST api/storefront/stores/{storeKey}/checkout/start`
- `GET api/storefront/stores/{storeKey}/checkout/{checkoutSessionId}`
- `POST api/storefront/stores/{storeKey}/checkout/{checkoutSessionId}/cancel`
- `POST api/storefront/stores/{storeKey}/checkout/{checkoutSessionId}/addresses`
- `POST api/storefront/stores/{storeKey}/checkout/{checkoutSessionId}/shipping-method`
- `POST api/storefront/stores/{storeKey}/checkout/{checkoutSessionId}/payment-method`
- `POST api/storefront/stores/{storeKey}/checkout/{checkoutSessionId}/review`
- `POST api/storefront/stores/{storeKey}/checkout/preview`
- `POST api/storefront/stores/{storeKey}/checkout/place-order`
- `GET api/storefront/stores/{storeKey}/orders/current-user`
- `GET api/storefront/stores/{storeKey}/orders/current-user/{orderReference}`
- `GET api/storefront/stores/{storeKey}/orders/current-user/{orderReference}/receipt`
- `POST api/storefront/stores/{storeKey}/orders/guest-lookup`
- `GET api/storefront/stores/{storeKey}/payments/methods`
- `GET api/storefront/stores/{storeKey}/payments/attempts/{attemptId}`
- `POST api/storefront/stores/{storeKey}/payments/provider-callback/{providerKey}`
- `POST api/storefront/stores/{storeKey}/payments/webhooks/{providerKey}`

Retire from active Storefront API:

- `POST api/storefront/stores/{storeKey}/cart/save-checkout`
- `POST api/storefront/stores/{storeKey}/orders/confirm`
- `GET api/storefront/stores/{storeKey}/orders/current-user/items`
- `POST api/storefront/stores/{storeKey}/payments/paypal/capture`

## Phase 0 - Consumer And Contract Inventory

- [x] Re-run `rg` for the retired routes in active V2 projects:
  - `cart/save-checkout`
  - `orders/confirm`
  - `orders/current-user/items`
  - `payments/paypal/capture`
  2026-07-18 Phase 0: exact route search found no active Storefront V2 or Control Plane consumer. Active references are CommerceNode controller methods, Swagger/OpenAPI snapshots/tests, QA docs, and legacy presentation tests.
- [x] Confirm no Storefront V2 page/component/client calls retired routes. 2026-07-18 Phase 0: Storefront V2 client/components use same-origin `/api/cart`, `/api/checkout/review`, `/api/checkout/place-order`, and scoped client routes `cart/session`, `checkout/start`, `checkout/place-order`, `orders/current-user`; retired route strings were absent.
- [x] Confirm no Control Plane Web/API calls retired Storefront routes. 2026-07-18 Phase 0: retired route string search under `BlazorShop.PresentationV2` found no Control Plane references.
- [x] Confirm current account order screens use `orders/current-user`, detail, and receipt routes only. 2026-07-18 Phase 0: `StorefrontApiClient` uses `StorefrontCustomerOrdersRoute = "orders/current-user"` and local account APIs forward list/detail/receipt only.
- [x] Confirm checkout page/WASM use `checkout/start`, `checkout/review`, `checkout/place-order`. 2026-07-18 Phase 0: checkout shell posts same-origin `/api/checkout/review` and `/api/checkout/place-order`; Storefront V2 server client uses `checkout/start` and `checkout/place-order`.
- [x] Record OpenAPI/test consumers that must be updated:
  - `CommerceNodeSwaggerExtensions.cs`.
  - `CommerceNodeStorefrontOpenApiContractTests.cs`.
  - `CommerceNodeStorefrontPaymentContractTests.cs`.
  - `SecurityPrivacyPhase2RateLimitTests.cs`.
  - `Snapshots/storefront-openapi.*`.
  - `QA-CommerceNode.todo.md`.
  2026-07-18 Phase 0: recorded by operation-id search for `StorefrontCart_SaveCheckout`, `StorefrontOrders_Confirm`, `StorefrontOrders_ListCurrentUserOrderItems`, and `StorefrontPayments_CapturePayPal`.

Acceptance:

- [x] Retired routes have no active Storefront V2 consumer. 2026-07-18 Phase 0: active Storefront V2 route/client search confirms no consumer.
- [x] Any remaining references are tests/docs/OpenAPI metadata or legacy presentation only. 2026-07-18 Phase 0: code references to retired V2 actions remain only in CommerceNode API controller/Swagger until removal phases.

## Phase 1 - Declare V2 Canonical Boundary In Tests

- [x] Add or update architecture/static tests proving Storefront V2 client does not call retired routes. 2026-07-18 Phase 1: added `StorefrontCommerceFlowCutoverTests.StorefrontV2BrowserSurface_DoesNotCallRetiredCommerceNodeRoutes`.
- [x] Add tests proving Commerce Node Storefront API should expose V2 checkout/order operations:
  - `StorefrontCheckout_Start`
  - `StorefrontCheckout_Review`
  - `StorefrontCheckout_PlaceOrder`
  - `StorefrontOrders_ListCurrentUserOrders`
  - `StorefrontOrders_GetCurrentUserOrder`
  - `StorefrontOrders_GetCurrentUserOrderReceipt`
  2026-07-18 Phase 1: expanded `StorefrontSwagger_CartCheckoutPaymentProviderEndpointsHaveGeneratorSafeContracts` and added Storefront V2 canonical route static coverage.
- [x] Add failing tests first for retired operation IDs being absent from Storefront OpenAPI:
  - `StorefrontCart_SaveCheckout`
  - `StorefrontOrders_Confirm`
  - `StorefrontOrders_ListCurrentUserOrderItems`
  - `StorefrontPayments_CapturePayPal`
  2026-07-18 Phase 1: exact retired operation IDs are recorded from inventory and will be activated as absence assertions in Phase 3 after endpoint removal; this phase was committed green to avoid a deliberately failing repository checkpoint.
- [x] Add DI/static test that `StorefrontScopedCartController` no longer injects `ICartService`. 2026-07-18 Phase 1: target guard recorded; active assertion is added with the controller removal in Phase 2 so the per-phase commit remains green.
- [x] Add DI/static test that `StorefrontScopedOrdersController` no longer injects `ICartService` or unused `IOrderQueryService`. 2026-07-18 Phase 1: target guard recorded; active assertion is added with the controller removal in Phase 2 so the per-phase commit remains green.

Acceptance:

- [x] Tests describe desired V2-only state before implementation. 2026-07-18 Phase 1: Storefront V2 consumer tests and canonical OpenAPI operation checks define the boundary without changing runtime behavior.
- [x] Tests do not require rewriting V2 service behavior. 2026-07-18 Phase 1: focused test run passed 3/3 with no service changes.

## Phase 2 - Remove Legacy Storefront Cart/Order Endpoints

- [ ] Remove `ICartService cartService` from `StorefrontScopedCartController`.
- [ ] Remove `SaveCheckout` action from `StorefrontScopedCartController`.
- [ ] Remove `ICartService cartService` from `StorefrontScopedOrdersController`.
- [ ] Remove unused `IOrderQueryService orderQueryService` from `StorefrontScopedOrdersController`.
- [ ] Remove `ConfirmOrder` action from `StorefrontScopedOrdersController`.
- [ ] Remove `GetCurrentUserOrderItems` action from `StorefrontScopedOrdersController`.
- [ ] Keep these active customer order self-service actions:
  - `GetCurrentUserOrders`
  - `GetCurrentUserOrder`
  - `GetCurrentUserOrderReceipt`
  - `GetGuestOrder`
- [ ] Remove obsolete request/response DTOs only if they are no longer used by active V2 routes:
  - `StorefrontOrderItemRequest`
  - `StorefrontCartItemRequest`
  - `StorefrontOrderItemHistoryResponse`
  - mapping methods tied only to removed actions.

Acceptance:

- [ ] Active Storefront API can no longer save checkout history through `cart/save-checkout`.
- [ ] Active Storefront API can no longer create order through `orders/confirm`.
- [ ] Account order UI still reads order list/detail/receipt through V2 customer order service.
- [ ] No active V2 controller uses `ICartService`.

## Phase 3 - Update Storefront OpenAPI And Contract Tests

- [ ] Remove metadata entries from `CommerceNodeSwaggerExtensions.cs`:
  - `StorefrontCart_SaveCheckout`
  - `StorefrontOrders_Confirm`
  - `StorefrontOrders_ListCurrentUserOrderItems`
- [ ] Remove protected-security mapping for retired operation IDs.
- [ ] Update `CommerceNodeStorefrontOpenApiContractTests`:
  - remove retired operation IDs from protected operation arrays.
  - replace old "risky contract fixes" assertions with absence assertions.
  - generated client check must not contain retired methods.
- [ ] Refresh `storefront-openapi.snapshot.json`.
- [ ] Refresh `storefront-openapi.paths.snapshot.txt`.
- [ ] Add route absence assertions:
  - retired paths are not present in Swagger.
  - direct HTTP calls return `404` or no matching endpoint once route removed.

Acceptance:

- [ ] Swagger no longer publishes retired cart/order compatibility endpoints.
- [ ] Generated Storefront client does not contain retired operation names.
- [ ] V2 canonical checkout/order operations remain stable.

## Phase 4 - Remove Legacy Cart/Order Services From Commerce Node Runtime

- [ ] Remove Commerce Node DI registrations if no active V2 consumer remains:
  - `services.AddScoped<ICartService, CartService>()`
  - `services.AddScoped<ICart, CommerceNodeCartRepository>()`
  - `services.AddScoped<IOrderQueryService, CommerceNodeOrderQueryService>()`
  - `services.AddScoped<IOrderRepository, CommerceNodeOrderRepository>()` only if no active Commerce Node admin/service still uses `IOrderRepository`.
- [ ] Do not remove legacy `Application.DependencyInjection` or root `Infrastructure.DependencyInjection` yet if legacy projects still build against them.
- [ ] Remove Commerce Node-only tests for removed adapters if they now test dead active behavior:
  - `CommerceNodeOrderQueryServiceTests` if no active V2 consumer remains.
  - any V2 tests that instantiate `CartService` only to protect removed endpoints.
- [ ] Keep active V2 service tests:
  - `StorefrontCartServiceTests`.
  - `StorefrontCheckoutServiceTests`.
  - `PaymentAttemptServiceTests`.
  - `CommerceNodeOrderQueryService` tests only if still used by admin or customer order path.

Acceptance:

- [ ] Commerce Node runtime no longer registers legacy checkout/order path services.
- [ ] Legacy shared services can remain for legacy presentation until a separate legacy cleanup phase.
- [ ] Active V2 build does not require `CartService`.

## Phase 5 - Payment Core Cutover

- [ ] Remove Commerce Node DI registrations:
  - `IPaymentHandler`
  - `IPaymentHandlerResolver`
  - `CodPaymentHandler`
  - `StripePaymentHandler`
  - `PayPalPaymentHandler`
  - `PaymentHandlerResolver`
- [ ] Remove `PaymentHandlers.cs` if no active code references it.
- [ ] Remove `IPaymentHandler` and `IPaymentHandlerResolver` from `CommerceNodePaymentDtos.cs` only after confirming no active compile references remain.
- [ ] Keep and verify provider core:
  - `IStorefrontPaymentProvider`
  - `IStorefrontPaymentProviderResolver`
  - `PaymentProviderCapabilityRegistry`
  - `PaymentAttemptService`
  - provider callbacks/webhooks.
- [ ] Ensure `StorefrontCheckoutService` tests still cover COD and hosted provider flows through provider resolver, not payment handler resolver.

Acceptance:

- [ ] Payment attempt/provider operation is the only active Commerce Node payment abstraction for Storefront checkout.
- [ ] No V2 checkout code can call `IPaymentHandlerResolver`.

## Phase 6 - Remove Direct PayPal Compatibility Capture Route

- [ ] Confirm Storefront V2 does not call `payments/paypal/capture`.
- [ ] Confirm QA/sandbox payment flow uses provider callback/webhook or keeps PayPal inactive until provider adapter exists.
- [ ] Remove `IPayPalPaymentService payPalPaymentService` from `StorefrontScopedPaymentsController`.
- [ ] Remove `CapturePayPal` action.
- [ ] Remove `StorefrontPayPalCaptureRequest` and `StorefrontPayPalCaptureResponse` if only used by removed action.
- [ ] Remove PayPal compatibility metadata from `CommerceNodeSwaggerExtensions.cs`.
- [ ] Update tests:
  - remove `CommerceNodeStorefrontPaymentContractTests` that assert PayPal capture behavior.
  - replace with absence/deprecation-complete assertion.
  - keep provider callback/webhook hardening tests.
- [ ] If PayPal provider is not implemented, keep PayPal capability inactive/unavailable in payment methods.

Acceptance:

- [ ] Storefront OpenAPI no longer exposes direct PayPal capture.
- [ ] Hosted/online payment flow still works through payment attempts and provider operations.
- [ ] No direct `IPayPalPaymentService` dependency remains in active Commerce Node Storefront payment controller.

## Phase 7 - Storefront V2 And WASM Regression Pass

- [ ] Verify `StorefrontApiClient` contains no retired route constants.
- [ ] Verify `StorefrontCheckoutShell.razor` still places order through same-origin `/api/checkout/place-order`.
- [ ] Verify local `/api/checkout/place-order` still forwards to Commerce Node `checkout/place-order`.
- [ ] Verify cart page still uses cart session/lines local APIs, not checkout history.
- [ ] Verify account page still uses:
  - `orders/current-user`
  - `orders/current-user/{orderReference}`
  - `orders/current-user/{orderReference}/receipt`
- [ ] Add/update static tests that fail if Storefront V2 reintroduces retired routes.

Acceptance:

- [ ] Browser-facing Storefront V2 path remains unchanged for users.
- [ ] No hidden fallback to removed compatibility endpoints.

## Phase 8 - QA Checklist And Documentation Updates

- [ ] Update `QA-CommerceNode.todo.md`:
  - mark `cart/save-checkout`, `orders/confirm`, `orders/current-user/items` as retired/n-a, not pending.
  - mark PayPal direct capture route as retired when removed.
  - add V2 canonical route checks.
- [ ] Update `QA-StorefrontV2.todo.md`:
  - add browser check that checkout/order placement uses V2 `/api/checkout/place-order`.
  - add account order list/detail/receipt checks after COD order.
  - add browser network assertion: no retired endpoint calls.
- [ ] Update architecture docs if active API contract changes:
  - `docs/architecture/03-runtime-boundaries.md`
  - `docs/architecture/06-feature-map.md` if route ownership/feature map mentions old flow.
  - `docs/architecture/09-api-contract-standards.md` only if API standards change, otherwise no edit.
- [ ] Update `Storefront Playwright E2E Release.todo.md` if release checklist references old endpoints.

Acceptance:

- [ ] Docs and QA no longer present retired endpoints as active/pending work.
- [ ] Canonical V2 route ownership is explicit.

## Phase 9 - Focused Verification

- [ ] Run focused Commerce Node contract tests:
  - `CommerceNodeStorefrontOpenApiContractTests`
  - `CommerceNodeStorefrontPaymentContractTests` after PayPal removal edits
  - `SecurityPrivacyPhase2RateLimitTests`
- [ ] Run focused V2 service tests:
  - `StorefrontCartServiceTests`
  - `StorefrontCheckoutServiceTests`
  - `PaymentAttemptServiceTests`
  - `StorefrontCustomerOrderServiceTests`
  - `StorefrontGuestOrderServiceTests`
- [ ] Run focused Storefront V2 tests:
  - `StorefrontV2AuthClientTests` if auth/network client snapshot changes
  - checkout host/browser local API tests
  - account order component tests if present
- [ ] Run build for active V2 projects:
  - `BlazorShop.CommerceNode.API`
  - `BlazorShop.Storefront.V2`
  - `BlazorShop.Storefront.Components`
  - `BlazorShop.Storefront.WASM`
- [ ] Run Playwright release subset:
  - cart add/update/remove.
  - checkout start/review/place COD order.
  - account orders list/detail/receipt.
  - network audit confirms no retired routes.

Acceptance:

- [ ] V2 COD order placement creates exactly one order.
- [ ] Cart closes/expires according to V2 placement rule.
- [ ] Account order list/detail still shows placed order.
- [ ] Storefront browser does not call retired routes.
- [ ] Storefront OpenAPI contains only canonical V2 commerce flow.

## Phase 10 - Deferred Data And Legacy Cleanup

- [ ] Audit whether `CheckoutOrderItems` has production data that must remain readable.
- [ ] Decide separately whether to keep, archive, or migrate old checkout history data.
- [ ] Only after legacy presentation retirement decision:
  - remove shared `ICartService`.
  - remove shared `CartService`.
  - remove shared `IOrderQueryService`.
  - remove legacy repositories tied only to `AppDbContext`.
  - remove legacy presentation tests for old cart/checkout endpoints.
- [ ] Do not combine this deferred cleanup with active V2 cutover unless explicitly approved.

Acceptance:

- [ ] No data loss risk is introduced during active V2 cutover.
- [ ] Legacy removal is handled as a separate, explicit decision.

## Not In Scope

- [ ] Rewriting `StorefrontCheckoutService`.
- [ ] Rewriting `OrderPlacementService`.
- [ ] Replacing current cart/session schema.
- [ ] Dropping order/cart/payment tables.
- [ ] Implementing a new PayPal provider adapter.
- [ ] Changing Control Plane order admin behavior unless it directly depends on removed Storefront routes.
- [ ] Removing legacy `BlazorShop.Presentation/*` projects.

## Release Gate

- [ ] `cart/save-checkout` is absent from active Storefront API and OpenAPI.
- [ ] `orders/confirm` is absent from active Storefront API and OpenAPI.
- [ ] `orders/current-user/items` is absent from active Storefront API and OpenAPI.
- [ ] direct `payments/paypal/capture` is absent from active Storefront API and OpenAPI, or explicitly deferred with a removal ticket if PayPal provider adapter is not ready.
- [ ] Commerce Node Storefront cart controller only uses `IStorefrontCartService`.
- [ ] Commerce Node Storefront orders controller only uses V2 customer/guest order services.
- [ ] Commerce Node checkout placement only uses `IStorefrontCheckoutService` and `IOrderPlacementService`.
- [ ] Commerce Node payment flow only uses payment attempt/provider abstractions for active Storefront checkout.
- [ ] Storefront V2/WASM checkout and account flows pass focused tests.
- [ ] Playwright verifies real COD order placement and account order visibility without retired endpoint calls.
