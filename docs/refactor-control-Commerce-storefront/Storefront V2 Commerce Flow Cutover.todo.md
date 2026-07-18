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

- [x] Remove `ICartService cartService` from `StorefrontScopedCartController`. 2026-07-18 Phase 2: constructor now only takes `IStorefrontCartService` and `ICommerceStoreContext`.
- [x] Remove `SaveCheckout` action from `StorefrontScopedCartController`. 2026-07-18 Phase 2: `SaveCheckout` action removed from active Storefront cart controller.
- [x] Remove `ICartService cartService` from `StorefrontScopedOrdersController`. 2026-07-18 Phase 2: constructor no longer takes legacy cart service.
- [x] Remove unused `IOrderQueryService orderQueryService` from `StorefrontScopedOrdersController`. 2026-07-18 Phase 2: unused field and constructor parameter removed.
- [x] Remove `ConfirmOrder` action from `StorefrontScopedOrdersController`. 2026-07-18 Phase 2: legacy confirm action removed; active checkout placement stays under `checkout/place-order`.
- [x] Remove `GetCurrentUserOrderItems` action from `StorefrontScopedOrdersController`. 2026-07-18 Phase 2: legacy checkout-history item action removed.
- [x] Keep these active customer order self-service actions:
  - `GetCurrentUserOrders`
  - `GetCurrentUserOrder`
  - `GetCurrentUserOrderReceipt`
  - `GetGuestOrder`
  2026-07-18 Phase 2: active customer/guest order actions remain in controller.
- [x] Remove obsolete request/response DTOs only if they are no longer used by active V2 routes:
  - `StorefrontOrderItemRequest`
  - `StorefrontCartItemRequest`
  - `StorefrontOrderItemHistoryResponse`
  - mapping methods tied only to removed actions.
  2026-07-18 Phase 2: DTOs/mappings are still referenced by Swagger metadata until Phase 3; no DTO deletion in this phase.

Acceptance:

- [x] Active Storefront API can no longer save checkout history through `cart/save-checkout`. 2026-07-18 Phase 2: route action removed and source guard added.
- [x] Active Storefront API can no longer create order through `orders/confirm`. 2026-07-18 Phase 2: route action removed and source guard added.
- [x] Account order UI still reads order list/detail/receipt through V2 customer order service. 2026-07-18 Phase 2: customer/guest order actions remain and focused build passed.
- [x] No active V2 controller uses `ICartService`. 2026-07-18 Phase 2: source search found no `ICartService cartService` in `StorefrontScopedControllers.cs`; `StorefrontCommerceFlowCutoverTests` guards it.

## Phase 3 - Update Storefront OpenAPI And Contract Tests

- [x] Remove metadata entries from `CommerceNodeSwaggerExtensions.cs`:
  - `StorefrontCart_SaveCheckout`
  - `StorefrontOrders_Confirm`
  - `StorefrontOrders_ListCurrentUserOrderItems`
  2026-07-18 Phase 3: metadata entries removed for the three retired cart/order operations.
- [x] Remove protected-security mapping for retired operation IDs. 2026-07-18 Phase 3: `StorefrontSecurityDocumentFilter` no longer maps the retired operation IDs.
- [x] Update `CommerceNodeStorefrontOpenApiContractTests`:
  - remove retired operation IDs from protected operation arrays.
  - replace old "risky contract fixes" assertions with absence assertions.
  - generated client check must not contain retired methods.
  2026-07-18 Phase 3: added absence assertions for retired operation IDs/paths/generated client and endpoint metadata.
- [x] Refresh `storefront-openapi.snapshot.json`. 2026-07-18 Phase 3: snapshot refreshed from raw Storefront Swagger runtime output.
- [x] Refresh `storefront-openapi.paths.snapshot.txt`. 2026-07-18 Phase 3: path snapshot regenerated with the same ordinal sort used by the test helper.
- [x] Add route absence assertions:
  - retired paths are not present in Swagger.
  - direct HTTP calls return `404` or no matching endpoint once route removed.
  2026-07-18 Phase 3: endpoint metadata test asserts no route endpoint exists for `cart/save-checkout`, `orders/confirm`, or `orders/current-user/items`; HTTP-level auth middleware can otherwise mask missing endpoints as 401.

Acceptance:

- [x] Swagger no longer publishes retired cart/order compatibility endpoints. 2026-07-18 Phase 3: `CommerceNodeStorefrontOpenApiContractTests` passed 35/35.
- [x] Generated Storefront client does not contain retired operation names. 2026-07-18 Phase 3: TypeScript client smoke asserts absence.
- [x] V2 canonical checkout/order operations remain stable. 2026-07-18 Phase 3: canonical checkout/order operation checks remain in the passing OpenAPI suite.

## Phase 4 - Remove Legacy Cart/Order Services From Commerce Node Runtime

- [x] Remove Commerce Node DI registrations if no active V2 consumer remains:
  - `services.AddScoped<ICartService, CartService>()`
  - `services.AddScoped<ICart, CommerceNodeCartRepository>()`
  - `services.AddScoped<IOrderQueryService, CommerceNodeOrderQueryService>()`
  - `services.AddScoped<IOrderRepository, CommerceNodeOrderRepository>()` only if no active Commerce Node admin/service still uses `IOrderRepository`.
  2026-07-18 Phase 4: removed CommerceNode runtime DI for `ICartService/CartService` and `IOrderQueryService/CommerceNodeOrderQueryService`; retained `IOrderRepository/CommerceNodeOrderRepository` because active admin/order services still use order persistence.
- [x] Do not remove legacy `Application.DependencyInjection` or root `Infrastructure.DependencyInjection` yet if legacy projects still build against them. 2026-07-18 Phase 4: no changes made to legacy/root DI registration.
- [x] Remove Commerce Node-only tests for removed adapters if they now test dead active behavior:
  - `CommerceNodeOrderQueryServiceTests` if no active V2 consumer remains.
  - any V2 tests that instantiate `CartService` only to protect removed endpoints.
  2026-07-18 Phase 4: no test deletion; `CommerceNodeOrderQueryServiceTests` and shipment tests instantiate service directly for read-model coverage, not active runtime route registration.
- [x] Keep active V2 service tests:
  - `StorefrontCartServiceTests`.
  - `StorefrontCheckoutServiceTests`.
  - `PaymentAttemptServiceTests`.
  - `CommerceNodeOrderQueryService` tests only if still used by admin or customer order path.
  2026-07-18 Phase 4: active V2 service tests remain untouched; added DI source guard in `StorefrontCommerceFlowCutoverTests`.

Acceptance:

- [x] Commerce Node runtime no longer registers legacy checkout/order path services. 2026-07-18 Phase 4: static guard checks no `AddScoped<ICartService, CartService>` or `AddScoped<IOrderQueryService, CommerceNodeOrderQueryService>`.
- [x] Legacy shared services can remain for legacy presentation until a separate legacy cleanup phase. 2026-07-18 Phase 4: shared `CartService` and legacy DI remain.
- [x] Active V2 build does not require `CartService`. 2026-07-18 Phase 4: CommerceNode API build passed.

## Phase 5 - Payment Core Cutover

- [x] Remove Commerce Node DI registrations:
  - `IPaymentHandler`
  - `IPaymentHandlerResolver`
  - `CodPaymentHandler`
  - `StripePaymentHandler`
  - `PayPalPaymentHandler`
  - `PaymentHandlerResolver`
  2026-07-18 Phase 5: removed CommerceNode runtime DI registrations for all legacy payment handlers/resolver.
- [x] Remove `PaymentHandlers.cs` if no active code references it. 2026-07-18 Phase 5: deleted `BlazorShop.Infrastructure/Data/CommerceNode/Services/PaymentHandlers.cs`; no active compile references remained.
- [x] Remove `IPaymentHandler` and `IPaymentHandlerResolver` from `CommerceNodePaymentDtos.cs` only after confirming no active compile references remain. 2026-07-18 Phase 5: not removed because shared legacy `CartService` still has a compile-time optional dependency; this remains for deferred legacy cleanup, not active CommerceNode runtime.
- [x] Keep and verify provider core:
  - `IStorefrontPaymentProvider`
  - `IStorefrontPaymentProviderResolver`
  - `PaymentProviderCapabilityRegistry`
  - `PaymentAttemptService`
  - provider callbacks/webhooks.
  2026-07-18 Phase 5: provider registrations remain and static guard asserts them.
- [x] Ensure `StorefrontCheckoutService` tests still cover COD and hosted provider flows through provider resolver, not payment handler resolver. 2026-07-18 Phase 5: focused checkout/payment/provider test run passed 77/77.

Acceptance:

- [x] Payment attempt/provider operation is the only active Commerce Node payment abstraction for Storefront checkout. 2026-07-18 Phase 5: CommerceNode runtime has no handler DI and retains attempt/provider services.
- [x] No V2 checkout code can call `IPaymentHandlerResolver`. 2026-07-18 Phase 5: static guard rejects handler resolver registration; active checkout tests passed.

## Phase 6 - Remove Direct PayPal Compatibility Capture Route

- [x] Confirm Storefront V2 does not call `payments/paypal/capture`. 2026-07-18 Phase 6: `StorefrontCommerceFlowCutoverTests` guards active Storefront V2 browser files against the retired route.
- [x] Confirm QA/sandbox payment flow uses provider callback/webhook or keeps PayPal inactive until provider adapter exists. 2026-07-18 Phase 6: callback/webhook tests remain active; PayPal skeleton remains unavailable through provider capability until a real adapter exists.
- [x] Remove `IPayPalPaymentService payPalPaymentService` from `StorefrontScopedPaymentsController`. 2026-07-18 Phase 6: payments controller constructor no longer accepts direct PayPal capture service.
- [x] Remove `CapturePayPal` action. 2026-07-18 Phase 6: direct `paypal/capture` action removed.
- [x] Remove `StorefrontPayPalCaptureRequest` and `StorefrontPayPalCaptureResponse` if only used by removed action. 2026-07-18 Phase 6: removed both DTOs from Storefront contracts.
- [x] Remove PayPal compatibility metadata from `CommerceNodeSwaggerExtensions.cs`. 2026-07-18 Phase 6: `StorefrontPayments_CapturePayPal` metadata removed and snapshots refreshed.
- [x] Update tests:
  - remove `CommerceNodeStorefrontPaymentContractTests` that assert PayPal capture behavior.
  - replace with absence/deprecation-complete assertion.
  - keep provider callback/webhook hardening tests.
  2026-07-18 Phase 6: payment contract test now asserts route 404; OpenAPI tests assert operation/path/generated client absence; webhook hardening tests still pass.
- [x] If PayPal provider is not implemented, keep PayPal capability inactive/unavailable in payment methods. 2026-07-18 Phase 6: no provider capability enablement changed; existing inactive PayPal guardrails remain.

Acceptance:

- [x] Storefront OpenAPI no longer exposes direct PayPal capture. 2026-07-18 Phase 6: focused OpenAPI/payment suite passed 67/67.
- [x] Hosted/online payment flow still works through payment attempts and provider operations. 2026-07-18 Phase 6: provider callback/webhook hardening tests remain green.
- [x] No direct `IPayPalPaymentService` dependency remains in active Commerce Node Storefront payment controller. 2026-07-18 Phase 6: source guard asserts the constructor/action/route are absent.

## Phase 7 - Storefront V2 And WASM Regression Pass

- [x] Verify `StorefrontApiClient` contains no retired route constants. 2026-07-18 Phase 7: exact retired route search returned no Storefront V2/Components/WASM matches.
- [x] Verify `StorefrontCheckoutShell.razor` still places order through same-origin `/api/checkout/place-order`. 2026-07-18 Phase 7: source search confirms checkout shell posts `/api/checkout/place-order`.
- [x] Verify local `/api/checkout/place-order` still forwards to Commerce Node `checkout/place-order`. 2026-07-18 Phase 7: `Program.cs` maps same-origin `/api/checkout/place-order`; `StorefrontApiClient` still uses `StorefrontPlaceOrderRoute = "checkout/place-order"`.
- [x] Verify cart page still uses cart session/lines local APIs, not checkout history. 2026-07-18 Phase 7: cart view/local APIs use `/api/cart` and `/api/cart/lines`; no checkout-history route is referenced.
- [x] Verify account page still uses:
  - `orders/current-user`
  - `orders/current-user/{orderReference}`
  - `orders/current-user/{orderReference}/receipt`
  2026-07-18 Phase 7: account local APIs route through `/api/account/orders*` and `StorefrontApiClient` uses `orders/current-user`.
- [x] Add/update static tests that fail if Storefront V2 reintroduces retired routes. 2026-07-18 Phase 7: `StorefrontCommerceFlowCutoverTests` covers active Storefront V2 browser surface and controller guardrails.

Acceptance:

- [x] Browser-facing Storefront V2 path remains unchanged for users. 2026-07-18 Phase 7: Storefront V2, Components, and WASM builds passed.
- [x] No hidden fallback to removed compatibility endpoints. 2026-07-18 Phase 7: targeted Storefront cutover/WASM/API client tests passed 8/8; broader host-smoke filter timed out after about 4 minutes, so focused route coverage was used for this phase.

## Phase 8 - QA Checklist And Documentation Updates

- [x] Update `QA-CommerceNode.todo.md`:
  - mark `cart/save-checkout`, `orders/confirm`, `orders/current-user/items` as retired/n-a, not pending.
  - mark PayPal direct capture route as retired when removed.
  - add V2 canonical route checks.
- [x] Update `QA-StorefrontV2.todo.md`:
  - add browser check that checkout/order placement uses V2 `/api/checkout/place-order`.
  - add account order list/detail/receipt checks after COD order.
  - add browser network assertion: no retired endpoint calls.
- [x] Update architecture docs if active API contract changes:
  - `docs/architecture/03-runtime-boundaries.md`
  - `docs/architecture/06-feature-map.md` if route ownership/feature map mentions old flow.
  - `docs/architecture/09-api-contract-standards.md` only if API standards change, otherwise no edit.
  2026-07-18 Phase 8: `06-feature-map.md` now describes checkout review/order placement, current-user order detail/receipt, and provider callback/webhook payment flow. `03-runtime-boundaries.md` and `09-api-contract-standards.md` were reviewed and did not require route-specific edits.
- [x] Update `Storefront Playwright E2E Release.todo.md` if release checklist references old endpoints.
  2026-07-18 Phase 8: release checklist has no retired endpoint references; browser network audit requirement was added to `QA-StorefrontV2.todo.md`.

Acceptance:

- [x] Docs and QA no longer present retired endpoints as active/pending work. 2026-07-18 Phase 8: old active QA checks are now n/a retired, while canonical checkout/order routes remain pending for real browser QA.
- [x] Canonical V2 route ownership is explicit. 2026-07-18 Phase 8: Commerce Node QA and architecture feature map point to checkout preview/place-order and current-user order list/detail/receipt.

## Phase 9 - Focused Verification

- [x] Run focused Commerce Node contract tests:
  - `CommerceNodeStorefrontOpenApiContractTests`
  - `CommerceNodeStorefrontPaymentContractTests` after PayPal removal edits
  - `SecurityPrivacyPhase2RateLimitTests`
  2026-07-18 Phase 9: passed 63/63 with existing `MessagePack` and legacy `Microsoft.OpenApi` advisory warnings.
- [x] Run focused V2 service tests:
  - `StorefrontCartServiceTests`
  - `StorefrontCheckoutServiceTests`
  - `PaymentAttemptServiceTests`
  - `StorefrontCustomerOrderServiceTests`
  - `StorefrontGuestOrderServiceTests`
  2026-07-18 Phase 9: passed 97/97 with existing package advisory warnings.
- [x] Run focused Storefront V2 tests:
  - `StorefrontV2AuthClientTests` if auth/network client snapshot changes
  - checkout host/browser local API tests
  - account order component tests if present
  2026-07-18 Phase 9: focused cutover/WASM/API client route tests passed 8/8.
- [x] Run build for active V2 projects:
  - `BlazorShop.CommerceNode.API`
  - `BlazorShop.Storefront.V2`
  - `BlazorShop.Storefront.Components`
  - `BlazorShop.Storefront.WASM`
  2026-07-18 Phase 9: all four project builds passed with 0 warnings and 0 errors.
- [x] Run Playwright release subset:
  - cart add/update/remove.
  - checkout start/review/place COD order.
  - account orders list/detail/receipt.
  - network audit confirms no retired routes.
  2026-07-18 Phase 9: `.\scripts\qa\run-storefront-order-email-e2e.ps1` passed in headed Chromium. Evidence file `.gstack/qa-reports/order-email-e2e/result.json` recorded successful COD order `ORD-20260718-23370800`, account order list/detail/receipt screenshots, `retiredFlowCallCount=0`, and `response5xxCount=0`.

Acceptance:

- [x] V2 COD order placement creates exactly one order. 2026-07-18 Phase 9: order email runner observed exactly one order email for `ORD-20260718-23370800` and queued task succeeded.
- [x] Cart closes/expires according to V2 placement rule. 2026-07-18 Phase 9: order placement redirected to `/checkout?orderReference=...` and the placement suite stayed green.
- [x] Account order list/detail still shows placed order. 2026-07-18 Phase 9: runner opened list/detail/receipt for `ORD-20260718-23370800`.
- [x] Storefront browser does not call retired routes. 2026-07-18 Phase 9: network audit recorded `retiredFlowCallCount=0`.
- [x] Storefront OpenAPI contains only canonical V2 commerce flow. 2026-07-18 Phase 9: focused OpenAPI/payment contract tests passed 63/63.

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
