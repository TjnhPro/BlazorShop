# Storefront Account Recovery and WASM Migration

Status: draft plan  
Date: 2026-07-17  
Scope: account recovery UI, migrate selected cart/account/checkout interactions to Storefront WASM.

## Autoplan Decision Summary

This plan keeps `BlazorShop.Storefront.V2` as the server-side Storefront/BFF boundary and uses `BlazorShop.Storefront.WASM` only for browser interaction. WASM components must call same-origin Storefront V2 endpoints, not Commerce Node directly.

Approved direction:

- Add missing Storefront V2 account recovery UI by using existing Commerce Node Storefront auth endpoints.
- Move cart interactions to WASM first because Storefront V2 already has local `/api/cart` endpoints.
- Move account self-service interactions after cart once a same-origin client layer exists.
- Move checkout last and keep final place-order server-owned because checkout has the highest state, payment, and duplicate-submit risk.

Explicitly not approved for this phase:

- Full SPA rewrite of Storefront V2.
- Direct browser calls from WASM to Commerce Node APIs.
- Storing access tokens, refresh tokens, node credentials, provider secrets, or Commerce Node base URLs in WASM.
- Migrating catalog/product/SEO pages to WASM.
- Replacing all existing `storefrontCommerce.js` behavior before component parity exists.

## Current Codebase Findings

Architecture source of truth:

- `BlazorShop.Storefront.V2` owns public/store-scoped server rendering and calls Commerce Node Storefront APIs.
- `BlazorShop.Storefront.Components` is for reusable public Storefront Razor components shared by server rendering and interactive WASM.
- `BlazorShop.Storefront.WASM` is for interactive browser behavior and must not hold node credentials or duplicate server-owned integration.
- Active Storefront APIs are store-scoped under `api/storefront/stores/{storeKey}/*`.

What already exists:

- Storefront V2 has `AddInteractiveWebAssemblyComponents()` and `AddInteractiveWebAssemblyRenderMode()`.
- `BlazorShop.Storefront.WASM` exists but only bootstraps a default WebAssembly host.
- `BlazorShop.Storefront.Components` exists but currently only contains the diagnostic `WasmProbe`.
- Storefront V2 renders server pages for sign-in, register, cart, checkout, profile, addresses, orders, order detail, and change password.
- Storefront V2 has local cart endpoints:
  - `GET /api/cart`
  - `POST /api/cart/lines`
  - `PUT /api/cart/lines/{lineId}`
  - `DELETE /api/cart/lines/{lineId}`
  - `DELETE /api/cart`
- Existing cart JavaScript in `wwwroot/js/storefrontCommerce.js` already calls those local endpoints and updates the cart badge.
- Commerce Node already has store-scoped auth endpoints for:
  - `POST auth/forgot-password`
  - `POST auth/reset-password`
- Commerce Node already has `StorefrontForgotPasswordRequest`, `StorefrontResetPasswordRequest`, captcha target `PasswordRecovery`, and transactional message template `customer.password_recovery`.

What is missing:

- Storefront V2 routes/pages for forgot password and reset password.
- Storefront V2 `IStorefrontAuthClient` methods for forgot/reset password.
- Storefront route constants for recovery pages.
- Sign-in page link to recovery.
- WASM service registrations such as same-origin `HttpClient`, antiforgery helper, and typed local endpoint client.
- Real WASM cart/account/checkout components.
- Local same-origin JSON endpoints for account and checkout workflows where current pages still use server form posts.
- Playwright or integration coverage for recovery UI and interactive cart/account/checkout flows.

## Boundary Rules

Storefront V2 remains the BFF.

```text
Browser / WASM
  -> same-origin Storefront V2 local endpoint
      -> Storefront V2 service/client
          -> Commerce Node Storefront API: api/storefront/stores/{storeKey}/*
```

Forbidden:

```text
Browser / WASM
  -> Commerce Node API directly
```

Reason:

- Store key resolution, refresh cookies, cart token cookies, CSRF, safe return URL validation, current customer resolution, and Commerce Node base URL configuration already belong to Storefront V2.
- Moving those concerns into WASM would expose sensitive state and duplicate server integration logic.

## Phase 0 - Baseline And Contract Inventory

Goal: lock current behavior before moving UI code.

Tasks:

- Confirm local V2 runtime starts with `.\scripts\run-v2-local.ps1 -StopExisting`.
- Capture current routes for:
  - `/signin`
  - `/register`
  - `/my-cart`
  - `/checkout`
  - `/account/profile`
  - `/account/addresses`
  - `/account/orders`
  - `/account/change-password`
- Inventory Commerce Node OpenAPI metadata for `StorefrontAuth_ForgotPassword` and `StorefrontAuth_ResetPassword`.
- Verify existing local cart endpoints include antiforgery behavior for mutations.
- Add/update QA notes in `QA-StorefrontV2.todo.md` for account recovery and WASM interactive paths.

Acceptance:

- Existing sign-in/register/cart/checkout/account pages still render.
- Existing cart JS flow still works before migration starts.
- No existing route is renamed or removed.

## Phase 1 - Account Recovery UI In Storefront V2

Goal: add practical forgot/reset password UI without making recovery depend on WASM.

Implementation:

- Add Storefront route constants:
  - `/forgot-password`
  - `/reset-password`
- Add request/form models in Storefront V2:
  - `StorefrontForgotPasswordForm`
  - `StorefrontResetPasswordForm`
- Extend `IStorefrontAuthClient` and `StorefrontAuthClient`:
  - `ForgotPasswordAsync(email, captchaToken, cancellationToken)`
  - `ResetPasswordAsync(email, token, password, confirmPassword, cancellationToken)`
- Add server pages:
  - `ForgotPasswordPage.razor`
  - `ResetPasswordPage.razor`
- Add server POST handlers in `Program.cs` near existing sign-in/register/change-password handlers.
- Add "Forgot password?" link to `SignInPage.razor`.
- Preserve anti-enumeration behavior from `AuthenticationService.ForgotPassword`: always show the generic success message for valid request format.
- Pass captcha token through when captcha is enabled, but keep the page usable when `PasswordRecovery` captcha is disabled.
- Ensure reset token is accepted from query string but never logged or rendered into unnecessary hidden/debug text.

Acceptance:

- User can request reset instructions from Storefront V2.
- Unknown email receives the same user-facing message as known email.
- User can open reset link and submit a new password.
- Invalid/expired token shows a generic failure without exposing account existence.
- Existing sign-in/register/change-password behavior is unchanged.

Not in this phase:

- MFA.
- Social login.
- Admin-driven password reset UI.
- WASM-only recovery form.

## Phase 2 - Storefront WASM Runtime Foundation

Goal: create a safe browser interaction layer before migrating business UI.

Implementation:

- Register a same-origin `HttpClient` in `BlazorShop.Storefront.WASM`.
- Add a small Storefront local API client in `BlazorShop.Storefront.Components` or `BlazorShop.Storefront.WASM` for browser-only calls.
- Add an antiforgery token reader that reads the token already emitted by Storefront V2 head/meta infrastructure.
- Define common result models for local endpoint responses where needed.
- Keep DTOs presentation-oriented; do not expose Commerce Node domain entities directly to components.
- Keep `WasmProbe` until the first real interactive component proves the runtime path, then remove or hide it from production layout.

Acceptance:

- WASM can issue same-origin GET/POST/PUT/DELETE calls with CSRF header where required.
- WASM has no Commerce Node base URL, node key, node secret, refresh token, or access token configuration.
- Existing server-rendered Storefront pages still work with JavaScript disabled except for intentionally interactive enhancements.

## Phase 3 - Cart WASM Component

Goal: replace cart page interactions and cart badge updates with WASM while reusing current local cart endpoints.

Implementation:

- Create reusable cart components in `BlazorShop.Storefront.Components`, for example:
  - `StorefrontCartView`
  - `StorefrontCartLine`
  - `StorefrontCartBadge`
- Use existing Storefront V2 local endpoints:
  - `GET /api/cart`
  - `POST /api/cart/lines`
  - `PUT /api/cart/lines/{lineId}`
  - `DELETE /api/cart/lines/{lineId}`
  - `DELETE /api/cart`
- Keep server `CartPage.razor` route `/my-cart` as the host/fallback page.
- Keep checkout allowed, warnings, quantity min/max/step, selected attributes, image URL, product URL, subtotal, grand total, and currency display from the existing cart response.
- Migrate cart badge updates from JS event handling to a component or a small bridge only after the component owns the state.
- Leave product-card add-to-cart JS in place until the product card can call the same cart service cleanly.

Acceptance:

- `/my-cart` loads current cart through WASM after prerender.
- Quantity update, remove, clear, warnings, and checkout allowed state behave the same as current JS/server page.
- Cart badge updates after add/update/remove.
- Cart still fails clearly if cart token resolution fails.
- Existing product detail/card add-to-cart flow is not broken.

Not in this phase:

- Offline cart.
- Client-side price calculation.
- Complex global state library.

## Phase 4 - Account Self-Service WASM Components

Goal: make account pages interactive without moving authentication ownership into WASM.

Implementation:

- Add same-origin Storefront V2 local endpoints where needed for:
  - current profile read/update
  - address create/update/delete
  - order list/detail/receipt read
  - change password
- These local endpoints must resolve current customer server-side through existing session resolver/auth flow.
- Create components:
  - `AccountProfileEditor`
  - `AccountAddressBook`
  - `AccountOrderList`
  - `AccountOrderDetail`
  - `AccountChangePasswordForm`
- Keep server pages/routes as hosts:
  - `/account/profile`
  - `/account/addresses`
  - `/account/orders`
  - `/account/orders/{orderReference}`
  - `/account/change-password`
- Keep sign-in/register/logout/recovery server-owned because they depend on cookies, redirects, and Set-Cookie behavior.

Acceptance:

- Unauthenticated account requests still redirect to sign-in or return 401 from local JSON endpoints.
- Customer cannot read/update another customer's addresses or orders.
- Profile/address/order/change-password UI works after WASM loads.
- Server fallback pages still provide a clear loading/error state.

Not in this phase:

- Moving login/register/logout to WASM.
- Exposing access token to browser code.
- Account dashboard redesign.

## Phase 5 - Checkout WASM Shell

Goal: make checkout steps interactive while keeping final server validation and order placement authoritative.

Implementation:

- Add local Storefront V2 checkout endpoints for browser components if current server form endpoints are not enough:
  - start/resume checkout
  - update billing/shipping address
  - select shipping method
  - select payment method
  - review
  - place order
- Local endpoints call existing Commerce Node checkout endpoints and keep cart token handling server-owned.
- Keep checkout session id, cart version, and step state explicit in the component state.
- Detect stale cart version and force review/refresh when cart changed.
- Keep final place-order idempotency and duplicate-submit protection server-side.
- Preserve provider redirect/client-action behavior from the existing payment flow.

Acceptance:

- Empty cart blocks checkout.
- Cart version mismatch blocks place order with a clear message.
- Shipping/payment options refresh when upstream address/cart state changes.
- Place order cannot be double-submitted from the UI.
- Payment redirect/cancel/success routes still work.

Not in this phase:

- Direct payment provider SDK integration in WASM unless a provider explicitly requires a public client action.
- Client-side order total authority.
- Rewriting payment/checkout core services.

## Phase 6 - Cleanup, QA, And Documentation

Goal: remove duplicated behavior only after parity is proven.

Tasks:

- Remove or reduce cart/account/checkout logic from `storefrontCommerce.js` only after equivalent WASM behavior passes QA.
- Keep any remaining JS as narrow DOM bridge code with clear ownership.
- Update `docs/architecture/05-project-and-folder-guide.md` only if component ownership rules materially change.
- Update `QA-StorefrontV2.todo.md` with completed test cases.
- Add focused tests:
  - Commerce Node contract tests for recovery endpoints if missing.
  - Storefront V2 tests for recovery POST handlers.
  - Component/unit tests where available for cart/account state.
  - Playwright tests for recovery, cart update, account update, and checkout happy-path.

Acceptance:

- No regression in existing Storefront routes.
- Browser QA covers desktop and mobile widths for the migrated flows.
- No secret/token appears in WASM appsettings, static files, browser storage, or rendered HTML.

## API And Security Requirements

- All new Commerce Node API changes must follow `docs/architecture/09-api-contract-standards.md`.
- Storefront V2 local endpoints used by WASM must validate antiforgery on mutations.
- Recovery request must stay anti-enumeration safe.
- Reset token must not be logged.
- Return URLs must use existing safe return URL logic.
- Account/order/address local endpoints must resolve the current customer server-side and never accept arbitrary `customerId` from the browser.
- Checkout place-order must remain idempotent and server-validated.

## Risk Register

| Risk | Impact | Mitigation |
|---|---|---|
| WASM directly calls Commerce Node | Exposes topology/config and bypasses Storefront V2 protections | Only same-origin Storefront V2 local endpoints are allowed |
| Recovery leaks account existence | Security/privacy issue | Use generic success for forgot password and generic reset failure |
| Cart JS and WASM both mutate the same DOM | Double updates or inconsistent badge | Migrate one surface at a time and disable old handlers only after parity |
| Checkout moves too much logic client-side | Duplicate orders or wrong totals | Keep final validation/place-order server-owned |
| Token appears in logs or rendered markup | Account takeover risk | Never log reset query; keep hidden field minimal; do not expose access/refresh tokens to WASM |

## QA Checklist

- Forgot password page renders from `/forgot-password`.
- Sign-in page links to forgot password.
- Forgot password accepts valid email format and shows generic success.
- Forgot password rejects invalid email format with validation.
- Reset password page handles `email` and `token` query values.
- Reset password succeeds with valid token and password.
- Reset password fails generically with invalid/expired token.
- Cart WASM loads existing cart.
- Cart WASM updates quantity.
- Cart WASM removes a line.
- Cart WASM clears cart.
- Cart badge updates after cart mutation.
- Account profile WASM reads and updates current customer only.
- Account addresses WASM create/update/delete current customer's addresses only.
- Account order list/detail only shows current customer's orders.
- Checkout WASM blocks empty cart.
- Checkout WASM detects stale cart version.
- Checkout WASM completes happy-path order placement.
- Mutation endpoints require antiforgery token.
- No direct Commerce Node URL or credential is present in WASM static output.

## Implementation Order

1. Phase 0: Baseline and QA inventory.
2. Phase 1: Account recovery Storefront V2 UI.
3. Phase 2: WASM runtime foundation.
4. Phase 3: Cart WASM.
5. Phase 4: Account WASM.
6. Phase 5: Checkout WASM.
7. Phase 6: Cleanup and QA closure.

This order is intentional: recovery is a missing practical account feature; cart has the lowest migration risk because local endpoints already exist; account is medium risk because authorization must stay server-owned; checkout is highest risk and should move only after the WASM foundation and endpoint pattern are proven.
