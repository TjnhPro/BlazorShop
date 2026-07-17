# Customer Identity Account.todo

Generated: 2026-07-17

Source plan: `Customer Identity Account.md`

Status: Phase 2 complete. Phase 3 not started.

Scope: move storefront customer identity and account self-service from MVP auth/cart behavior to a practical customer account core. Keep existing V2 boundaries, reuse server-side cart, address, checkout, and order placement foundations, and avoid building a full CRM/customer-group platform.

## Scope Lock

Approved:

- [ ] Guest identity:
  - [ ] keep anonymous cart ownership through opaque cart token.
  - [ ] keep authenticated cart attach/merge after login/register.
  - [ ] define guest checkout policy enforcement point.
  - [ ] add safe guest order completion lookup token support through Order Placement Core.
  - [ ] keep guest order contact identity on order/customer snapshots.
- [ ] Authentication:
  - [ ] keep email login as supported login mode.
  - [ ] keep login/logout/refresh-token/register/change-password/update-profile/confirm-email.
  - [ ] add registration mode setting shape for `disabled` and `standard`.
  - [ ] add password recovery endpoints and service flow.
  - [ ] align change-password validation with registration password complexity.
  - [ ] keep email activation through existing identity confirmation options.
  - [ ] add account access denied/unauthorized response behavior for Storefront account flows.
- [ ] Customer profile:
  - [ ] keep `AppUser` as authentication identity.
  - [ ] keep `CommerceCustomer` as store-scoped customer profile.
  - [ ] add first name.
  - [ ] add last name.
  - [ ] add company.
  - [ ] add preferred language.
  - [ ] add preferred currency.
  - [ ] add active state.
  - [ ] add last activity timestamp.
  - [ ] expose safe authenticated profile read/update projection.
- [ ] Account self-service:
  - [ ] view/edit profile.
  - [ ] change password.
  - [ ] address book using Address Core.
  - [ ] order list with paging.
  - [ ] order detail.
  - [ ] billing/shipping information from order snapshots.
  - [ ] item and totals breakdown.
  - [ ] payment status.
  - [ ] shipment status/tracking summary.
  - [ ] print/receipt projection endpoint or print-friendly Storefront view.
- [ ] Resource authorization:
  - [ ] authenticated customer can only read own profile.
  - [ ] authenticated customer can only read own addresses.
  - [ ] authenticated customer can only read own orders.
  - [ ] guest completion lookup requires non-predictable access token.
  - [ ] manager/admin permissions stay separate from customer self-service.
  - [ ] payment provider identity never replaces internal customer identity.
- [ ] API/contract hardening:
  - [ ] explicit Storefront DTOs.
  - [ ] stable OpenAPI operation IDs and summaries.
  - [ ] standard error responses.
  - [ ] bearer security metadata on protected endpoints.
  - [ ] contract tests for schemas.
  - [ ] contract tests for security metadata.
  - [ ] contract tests for no unsafe fields.

Deferred:

- [ ] Username login mode.
- [ ] Admin approval registration workflow.
- [ ] Customer role/group management.
- [ ] Loyalty tiers.
- [ ] B2B approval.
- [ ] Customer segmentation.
- [ ] Social login.
- [ ] MFA.
- [ ] SSO.
- [ ] Passkeys.
- [ ] External identity federation.
- [ ] Full return/RMA workflow.
- [ ] Full reorder command that recreates cart lines.
- [ ] Full downloadable entitlement system.
- [ ] Stock subscription workflow.
- [ ] Account deletion/export/privacy center.
- [ ] Customer support/ticketing/CRM.
- [ ] Control Plane customer management UI.
- [ ] Legacy `AppDbContext` changes.
- [ ] Legacy Presentation route changes.
- [ ] Active V2 `api/internal/*` changes.

## Current Baseline

Existing customer identity:

- [ ] `CommerceCustomer` exists under `BlazorShop.Domain.Entities.CommerceNode`.
- [ ] `CommerceCustomer` stores `Id`.
- [ ] `CommerceCustomer` stores `StoreId`.
- [ ] `CommerceCustomer` stores `AppUserId`.
- [ ] `CommerceCustomer` stores `Email` and `NormalizedEmail`.
- [ ] `CommerceCustomer` stores `FullName`.
- [ ] `CommerceCustomer` stores `Phone`.
- [ ] `CommerceCustomer` stores created/updated timestamps.
- [ ] `CommerceCustomer` stores `LastCheckoutAt`.
- [ ] `CommerceCustomer` navigates to `CommerceStore`, `AppUser`, `Orders`, and `Addresses`.
- [ ] `StorefrontCustomerService.ResolveOrCreateAsync` resolves by `StoreId + NormalizedEmail`.
- [ ] `StorefrontCustomerService.ResolveOrCreateAsync` attaches `AppUserId` when present.
- [ ] `StorefrontCustomerService.ResolveOrCreateAsync` updates checkout contact fields.

Existing guest cart identity:

- [ ] `CartSession` exists in Commerce Node domain.
- [ ] `CartSession` stores `PublicId`.
- [ ] `CartSession` stores `StoreId`.
- [ ] `CartSession` stores `TokenHash`.
- [ ] `CartSession` stores `CustomerId` and `AppUserId`.
- [ ] `CartSession` stores state/version/activity/expiration fields.
- [ ] `CartSession` stores converted/merged cart references.
- [ ] `StorefrontCartSessionService` creates secure random token and stores only hash.
- [ ] `AttachOrMergeCurrentCustomerAsync` attaches current authenticated customer or merges with active customer cart.
- [ ] Storefront cart `merge-current-customer` derives `AppUserId` from JWT claims.
- [ ] Public browser JSON does not select customer identity.

Existing authentication:

- [ ] Storefront auth routes exist under `api/storefront/stores/{storeKey}/auth`.
- [ ] Auth supports register.
- [ ] Auth supports login.
- [ ] Auth supports refresh-token.
- [ ] Auth supports logout.
- [ ] Auth supports confirm-email.
- [ ] Auth supports change-password.
- [ ] Auth supports update-profile.
- [ ] Storefront V2 local endpoints call Commerce Node auth for sign-in/register/logout.
- [ ] Refresh token uses HTTP-only cookie.
- [ ] Login/register can use captcha targets and rate limiting.
- [ ] Security/privacy settings include `PasswordRecovery` captcha target.
- [ ] Password recovery endpoints are not implemented yet.
- [ ] Registration creates `AppUser` and sets `UserName = Email`.
- [ ] Registration assigns `Admin` to first user and `User` to later users.
- [ ] Email confirmation is controlled by existing `IdentityConfirmationOptions`.

Existing password policy:

- [ ] Registration validation requires stronger password shape.
- [ ] ASP.NET Identity options configure password requirements.
- [ ] Change-password validation exists.
- [ ] Change-password validation is weaker than registration validation and needs alignment.
- [ ] Password hashing remains inside ASP.NET Identity/UserManager.

Existing address book:

- [ ] `CommerceCustomerAddress` exists.
- [ ] `StorefrontCustomerAddressService` supports authenticated list/create/update/delete/default shipping/default billing.
- [ ] Address ownership is resolved by store and current authenticated customer identity.
- [ ] Historical order addresses are snapshots and do not change when address book records change.

Existing orders and current gap:

- [ ] Storefront order routes exist under `api/storefront/stores/{storeKey}/orders`.
- [ ] Existing current-user order list calls `IOrderQueryService.GetOrdersForUserAsync(userId)`.
- [ ] `CommerceNodeOrderRepository.GetByUserIdAsync` filters by `Order.UserId == userId`.
- [ ] V2 checkout/order placement uses `CommerceCustomer` and `Order.CustomerId` as stronger customer link.
- [ ] Current-user order history can miss V2 orders placed through `CustomerId` without legacy `UserId`.
- [ ] No Storefront customer order detail endpoint with owner check exists yet.
- [ ] No paging exists for customer order list.
- [ ] No guest completion token lookup exists yet.
- [ ] Retry payment, reorder, return request, download entitlement, and stock subscription workflows are not ready.

Boundary and ownership:

- [ ] Customer/account/order storefront runtime belongs to Commerce Node.
- [ ] New customer account data belongs in `CommerceNodeDbContext`.
- [ ] Storefront V2 calls Commerce Node Storefront APIs through existing client/local endpoint patterns.
- [ ] Control Plane is not part of customer self-service runtime.
- [ ] Legacy `AppDbContext` remains out of scope.

## Core Decisions

- [ ] D1: Keep `AppUser` and `CommerceCustomer` separate.
- [ ] D2: Keep email login only for this phase.
- [ ] D3: Support registration modes `disabled` and `standard` first.
- [ ] D4: Use `CommerceCustomer` for order self-service ownership.
- [ ] D5: Guest order lookup must be tokenized.
- [ ] D6: Retry payment, reorder, return request, downloadable item, and stock subscription are capability flags/hooks, not workflows.
- [ ] D7: Keep Storefront account UI conservative and contract-driven.

## Boundary Rules

- [ ] Store scope comes from `{storeKey}`.
- [ ] Authenticated identity comes from JWT claims.
- [ ] Browser requests never include `userId`.
- [ ] Browser requests never include `customerId`.
- [ ] Browser requests never include `storeId`.
- [ ] Browser requests never include role assignments.
- [ ] Browser requests never include order status.
- [ ] Browser requests never include payment status.
- [ ] Browser requests never include provider references.
- [ ] Address/order/profile ownership is checked by `StoreId + resolved current customer`.
- [ ] Guest completion lookup requires order reference plus access token.
- [ ] Control Plane APIs are not used for storefront customer self-service.
- [ ] Legacy routes remain reference only.

## Data Model Checklist

Extend `CommerceCustomer` additively:

- [ ] `FirstName`.
- [ ] `LastName`.
- [ ] `Company`.
- [ ] `PreferredLanguage`.
- [ ] `PreferredCurrencyCode`.
- [ ] `IsActive`.
- [ ] `LastActivityAtUtc`.

Keep compatible:

- [ ] `Email`.
- [ ] `NormalizedEmail`.
- [ ] `FullName`.
- [ ] `Phone`.
- [ ] `AppUserId`.
- [ ] `LastCheckoutAt`.
- [ ] timestamps.
- [ ] existing unique key on `StoreId + NormalizedEmail`.
- [ ] `StoreId + AppUserId` index for authenticated account lookup.

Migration behavior:

- [ ] Backfill `FirstName` and `LastName` conservatively from `FullName` only if safe.
- [ ] Keep `FullName` for compatibility and display fallback.
- [ ] Default `IsActive = true`.
- [ ] Preferred language/currency default to null and fall back to store settings.
- [ ] Do not add customer roles/groups table.

Guest order access token direction:

- [ ] Public order reference remains customer-facing.
- [ ] Access token is generated at placement for guest completion lookup.
- [ ] Only token hash is stored.
- [ ] Token is returned only once during completion flow.
- [ ] Authenticated customer order detail does not require guest token.
- [ ] If Order Placement Core has not implemented token storage yet, add only self-service contract after token foundation exists.

## API Contract Checklist

Every new or changed Storefront API must satisfy:

- [ ] Stable `operationId`.
- [ ] Short summary.
- [ ] Explicit request DTO.
- [ ] Explicit response DTO.
- [ ] Standard error response schemas.
- [ ] Required body metadata for command endpoints.
- [ ] Bearer security for authenticated customer endpoints.
- [ ] No domain entities in schemas.
- [ ] No server-owned identity fields in requests.
- [ ] Email/password validation metadata.
- [ ] Named string values for client-facing modes/statuses.
- [ ] Side-effecting commands use `POST` or `PUT`, not `GET`.
- [ ] Contract tests and snapshots updated.

## Target API Direction

Auth routes under `api/storefront/stores/{storeKey}/auth`:

- [ ] Keep `POST /register`.
- [ ] Keep `POST /login`.
- [ ] Keep `POST /refresh-token`.
- [ ] Keep `POST /logout`.
- [ ] Keep `GET /confirm-email`.
- [ ] Keep `POST /change-password`.
- [ ] Keep `POST /update-profile`.
- [ ] Add `POST /forgot-password`.
- [ ] Add `POST /reset-password`.
- [ ] Add `GET /registration-policy`.
- [ ] `forgot-password` returns generic success even when email is unknown.
- [ ] `reset-password` uses ASP.NET Identity token/user identity.
- [ ] Password requirements are published in OpenAPI validation metadata.
- [ ] Protected operations declare bearer security.
- [ ] Logout remains side-effecting `POST`.

Customer profile routes under `api/storefront/stores/{storeKey}/customer`:

- [ ] Add `GET /customer/profile`.
- [ ] Add `PUT /customer/profile`.
- [ ] Profile response exposes customer public id.
- [ ] Profile response exposes email.
- [ ] Profile response exposes first name, last name, full name display value, company, phone.
- [ ] Profile response exposes preferred language and preferred currency.
- [ ] Profile response exposes created and last activity timestamps.
- [ ] Profile response exposes email confirmed flag only if safe/useful.
- [ ] Profile request excludes `customerId`.
- [ ] Profile request excludes `appUserId`.
- [ ] Profile request excludes role fields.
- [ ] Profile request excludes active/disabled field.
- [ ] Email update is explicit and preserves Identity behavior, or returns unsupported until confirmation flow is ready.

Address routes:

- [ ] Reuse Address Core under `api/storefront/stores/{storeKey}/customer/addresses`.
- [ ] Consume or lightly harden address book surface.
- [ ] Do not redesign Address Core.

Customer order routes:

- [ ] Add/replace `GET /orders/current-user?pageNumber=1&pageSize=10`.
- [ ] Add `GET /orders/current-user/{orderReference}`.
- [ ] Add `GET /orders/current-user/{orderReference}/receipt`.
- [ ] Optional guest route `GET /orders/guest-completion/{orderReference}?token=...`.
- [ ] Order list item includes reference, created timestamp, statuses, currency, total amount, item count, and tracking summary.
- [ ] Order detail includes contact/address snapshots, lines, attributes/personalization, totals, currency/rate snapshot, payment method display, payment status, shipping method display, tracking fields, and safe action flags.
- [ ] Default action flags false until workflows exist: `canRetryPayment`, `canReorder`, `canRequestReturn`, `hasDownloads`.

## Storefront V2 UI Checklist

Add conservative account pages/components:

- [ ] `/account`.
- [ ] `/account/profile`.
- [ ] `/account/change-password`.
- [ ] `/account/addresses`.
- [ ] `/account/orders`.
- [ ] `/account/orders/{orderReference}`.
- [ ] `/account/orders/{orderReference}/receipt`.
- [ ] `/forgot-password`.
- [ ] `/reset-password`.
- [ ] Server-rendered forms keep local form posts and antiforgery where applicable.
- [ ] Avoid separate client-side account framework.
- [ ] Keep sections small and contract-driven.
- [ ] Unauthenticated users redirect to sign-in with safe return URL.
- [ ] Authenticated users without access see 403/access denied, not another customer's data.

## Phase 0 - Baseline Guardrails

Goal: capture current behavior and prevent identity regressions.

Implementation checklist:

- [x] Add/confirm tests for existing auth contract operation IDs. 2026-07-17 Phase 0: covered by `CommerceNodeStorefrontOpenApiContractTests` and `CommerceNodeStorefrontAuthContractTests`.
- [x] Add/confirm tests for existing auth security metadata. 2026-07-17 Phase 0: protected auth/customer-address/cart operations are in OpenAPI security metadata tests.
- [x] Add/confirm tests for cart `merge-current-customer` deriving identity from JWT. 2026-07-17 Phase 0: covered by `StorefrontCartSessionServiceTests` plus Storefront API client tests.
- [x] Add/confirm tests for address ownership checks. 2026-07-17 Phase 0: covered by `StorefrontCustomerAddressServiceTests`.
- [x] Add failing/self-documenting test that order self-service should resolve by `CustomerId` for current customer. 2026-07-17 Phase 0: added passing baseline test `GetOrdersForUserAsync_CurrentlyMissesV2OrdersLinkedOnlyByCustomerId` to lock the known gap.
- [x] Document current mismatch between legacy `Order.UserId` query and V2 `Order.CustomerId` placement. 2026-07-17 Phase 0: baseline test and Current Baseline section document the mismatch.
- [x] Update QA checklist entries for Customer Identity Account. 2026-07-17 Phase 0.

Verification checklist:

- [x] Existing login/register/logout tests pass. 2026-07-17 Phase 0: focused auth/OpenAPI/Storefront auth client tests passed inside 60/60 run.
- [x] Existing cart merge tests pass. 2026-07-17 Phase 0: focused cart merge tests passed inside 60/60 run.
- [x] Existing address ownership tests pass. 2026-07-17 Phase 0: focused address service tests passed inside 60/60 run.
- [x] No public Storefront request accepts `customerId` for account ownership. 2026-07-17 Phase 0: contract tests and request DTO review.
- [x] No public Storefront request accepts `userId` for account ownership. 2026-07-17 Phase 0: contract tests and request DTO review.

Exit criteria:

- [x] Order self-service gap is protected by tests before behavior changes. 2026-07-17 Phase 0.
- [x] No legacy runtime is modified. 2026-07-17 Phase 0.

Suggested commit:

```text
test(customer-account): lock identity baseline
```

## Phase 1 - Customer Profile Model

Goal: make `CommerceCustomer` a usable store-scoped customer profile without replacing `AppUser`.

Implementation checklist:

- [x] Extend `CommerceCustomer` with approved profile fields. 2026-07-17 Phase 1.
- [x] Add EF configuration/migration in `CommerceNodeDbContext`. 2026-07-17 Phase 1: migration `CommerceNodeCustomerProfileFields`.
- [x] Update profile DTO and mapping. 2026-07-17 Phase 1.
- [x] Update `StorefrontCustomerService` to keep `FullName` compatibility. 2026-07-17 Phase 1.
- [x] Store first name when supplied. 2026-07-17 Phase 1.
- [x] Store last name when supplied. 2026-07-17 Phase 1.
- [x] Store company/preferences when supplied. 2026-07-17 Phase 1.
- [x] Add `LastActivityAtUtc` update helper for authenticated account operations. 2026-07-17 Phase 1: `TouchLastActivityAsync`.
- [x] Keep checkout resolution behavior compatible. 2026-07-17 Phase 1: focused checkout/customer tests passed.

Verification checklist:

- [x] Existing checkout customer creation still works. 2026-07-17 Phase 1: focused checkout tests passed inside 95/95 run.
- [x] Existing address customer resolution still works. 2026-07-17 Phase 1: address service tests passed inside 95/95 run.
- [x] Existing `FullName` consumers keep working. 2026-07-17 Phase 1: `StorefrontCustomerServiceTests` covers fallback/update.
- [x] New columns default safely for existing rows. 2026-07-17 Phase 1: nullable profile fields, `IsActive=true`.
- [x] Migration is CommerceNode-only. 2026-07-17 Phase 1.

Exit criteria:

- [x] `CommerceCustomer` supports practical profile data while `AppUser` remains auth owner. 2026-07-17 Phase 1.

Suggested commit:

```text
feat(customer-account): add customer profile fields
```

## Phase 2 - Registration Policy And Password Recovery

Goal: complete practical auth behavior while keeping email login only.

Implementation checklist:

- [x] Add Storefront registration policy model with `disabled`. 2026-07-17 Phase 2.
- [x] Add Storefront registration policy model with `standard`. 2026-07-17 Phase 2.
- [x] Resolve policy from typed settings/configuration foundation or Commerce Node runtime setting shape. 2026-07-17 Phase 2: `Runtime:Security:RegistrationMode`.
- [x] Enforce policy in `POST /auth/register`. 2026-07-17 Phase 2.
- [x] Add `GET /auth/registration-policy`. 2026-07-17 Phase 2.
- [x] Add `POST /auth/forgot-password`. 2026-07-17 Phase 2.
- [x] Add `POST /auth/reset-password`. 2026-07-17 Phase 2.
- [x] Use existing email service. 2026-07-17 Phase 2.
- [x] Use ASP.NET Identity token generation. 2026-07-17 Phase 2.
- [x] Add captcha check for password recovery only when enabled. 2026-07-17 Phase 2.
- [x] Align change-password validation with registration password complexity. 2026-07-17 Phase 2.
- [x] Keep forgot-password response generic for anti-enumeration. 2026-07-17 Phase 2.

Verification checklist:

- [x] Registration disabled returns safe documented validation/forbidden response. 2026-07-17 Phase 2: contract test covers 403 `auth.registration_disabled`.
- [x] Standard registration preserves current behavior. 2026-07-17 Phase 2: existing auth tests remained green.
- [x] Unknown email password recovery does not reveal account existence. 2026-07-17 Phase 2: service test covers generic response and no email send.
- [x] Reset token flow uses Identity APIs. 2026-07-17 Phase 2: service test verifies reset token call.
- [x] No custom token storage is introduced for password reset. 2026-07-17 Phase 2.
- [x] OpenAPI includes request bodies. 2026-07-17 Phase 2.
- [x] OpenAPI includes validation metadata. 2026-07-17 Phase 2.
- [x] OpenAPI includes response schemas. 2026-07-17 Phase 2.
- [x] OpenAPI includes security metadata. 2026-07-17 Phase 2.

Exit criteria:

- [x] Storefront auth has practical registration policy and recovery flow without widening login modes. 2026-07-17 Phase 2.

Suggested commit:

```text
feat(customer-account): add registration policy and recovery
```

## Phase 3 - Account Profile API And Storefront Pages

Goal: give authenticated customers safe self-service for profile and password.

Implementation checklist:

- [ ] Add `GET /customer/profile`.
- [ ] Add `PUT /customer/profile`.
- [ ] Synchronize profile updates to `AppUser`.
- [ ] Synchronize profile updates to `CommerceCustomer`.
- [ ] Preserve email confirmation behavior for email changes, or return explicit unsupported response.
- [ ] Add Storefront V2 profile page/form.
- [ ] Add Storefront V2 change-password page/form.
- [ ] Add safe access denied behavior.
- [ ] Add return URL validation.

Verification checklist:

- [ ] Authenticated customer can view profile.
- [ ] Customer can update allowed profile fields.
- [ ] Customer cannot update role.
- [ ] Customer cannot update active state.
- [ ] Customer cannot update store id.
- [ ] Customer cannot update app user id.
- [ ] Customer cannot update customer id.
- [ ] Change password uses existing protected endpoint.
- [ ] Unauthenticated user redirects to sign-in with safe return URL.

Exit criteria:

- [ ] Storefront customers can manage safe profile/password fields.

Suggested commit:

```text
feat(customer-account): expose profile self service
```

## Phase 4 - Customer Order Self-Service API

Goal: replace thin legacy current-user order list with practical store/customer-scoped order history.

Implementation checklist:

- [ ] Add `IStorefrontCustomerOrderService` in application layer.
- [ ] Implement service in Commerce Node infrastructure.
- [ ] Resolve current customer by `StoreId + AppUserId`.
- [ ] Query by `Order.CustomerId` first.
- [ ] Add legacy `UserId` fallback only for compatible old orders.
- [ ] Add paged order list DTO.
- [ ] Add order detail DTO.
- [ ] Add receipt projection DTO or reuse detail with receipt mode.
- [ ] Include safe address snapshots.
- [ ] Include line item snapshots.
- [ ] Include totals.
- [ ] Include statuses.
- [ ] Include tracking summary.
- [ ] Add owner-check for every order detail lookup.
- [ ] Keep admin-only notes out of responses.
- [ ] Keep provider metadata out of responses.

Verification checklist:

- [ ] Customer sees only own orders for current store.
- [ ] Paging metadata is stable.
- [ ] V2 orders linked by `CustomerId` appear in account history.
- [ ] Legacy `UserId` orders can still appear when compatible.
- [ ] Customer cannot access another customer's order by reference.

Exit criteria:

- [ ] Account order history is owned by `CommerceCustomer`, not legacy `UserId` only.

Suggested commit:

```text
feat(customer-account): add customer order self service
```

## Phase 5 - Guest Completion Lookup

Goal: allow guest order completion/detail access without exposing predictable identifiers.

Implementation checklist:

- [ ] Depend on Order Placement Core guest access token fields.
- [ ] Add guest completion lookup by `orderReference + token`.
- [ ] Compare token by hash.
- [ ] Return only completion-safe order detail.
- [ ] Do not expose account-only address book/profile data.
- [ ] Apply expiration/rotation if Order Placement Core defines it.
- [ ] Otherwise document token lifetime as order access lifetime until privacy phase revisits it.

Verification checklist:

- [ ] Guest cannot fetch order by reference alone.
- [ ] Wrong token returns not found or forbidden without leaking order existence.
- [ ] Correct token succeeds.
- [ ] Authenticated customer order detail does not require guest token.
- [ ] Store scope is enforced.

Exit criteria:

- [ ] Guest order completion uses non-predictable access token.

Suggested commit:

```text
feat(customer-account): add guest completion lookup
```

## Phase 6 - Storefront Account UI Integration

Goal: expose practical account pages without creating future WASM migration pain.

Implementation checklist:

- [ ] Add account navigation entries through Menu Navigation Core when available.
- [ ] Build Storefront V2 account profile page.
- [ ] Build Storefront V2 change password page.
- [ ] Build Storefront V2 addresses page using Address Core.
- [ ] Build Storefront V2 orders list page.
- [ ] Build Storefront V2 order detail page.
- [ ] Build Storefront V2 receipt print view.
- [ ] Use existing Storefront API client patterns.
- [ ] Keep components small and data-driven.
- [ ] Add empty state for no addresses.
- [ ] Add empty state for no orders.
- [ ] Add loading/error states for profile requests.
- [ ] Add loading/error states for order requests.

Verification checklist:

- [ ] Account pages render with current layout.
- [ ] Unauthenticated access redirects to sign-in.
- [ ] Authenticated customer can navigate between profile, addresses, and orders.
- [ ] Text and controls fit mobile/desktop viewports.
- [ ] Storefront page does not call Commerce Node directly with unsafe identity fields.

Exit criteria:

- [ ] Storefront V2 has conservative account self-service UI.

Suggested commit:

```text
feat(customer-account): add storefront account pages
```

## Phase 7 - Contract, QA, And Hardening

Goal: finish the phase without weakening V2 contracts or security boundaries.

Implementation checklist:

- [ ] Add/update Commerce Node API contract tests for new auth endpoints.
- [ ] Add/update Commerce Node API contract tests for customer endpoints.
- [ ] Add/update Commerce Node API contract tests for order endpoints.
- [ ] Update OpenAPI snapshots.
- [ ] Add service tests for registration disabled.
- [ ] Add service tests for password recovery anti-enumeration.
- [ ] Add service tests for profile ownership.
- [ ] Add service tests for order list by `CustomerId`.
- [ ] Add service tests for order detail owner-check.
- [ ] Add service tests for guest completion token.
- [ ] Update `QA-CommerceNode.todo.md`.
- [ ] Update `QA-StorefrontV2.todo.md`.
- [ ] Run focused build/tests.
- [ ] Use Playwright only if Storefront UI pages are implemented in this phase.

Verification checklist:

- [ ] OpenAPI validates.
- [ ] New protected endpoints declare bearer security.
- [ ] Public schemas do not expose domain entities.
- [ ] Public schemas do not expose admin/private fields.
- [ ] Storefront V2 account flows pass browser smoke tests when UI is included.
- [ ] No legacy project was modified for new V2 behavior.

Exit criteria:

- [ ] Customer Identity Account is contract-protected.
- [ ] QA checklists contain tested evidence.
- [ ] Focused tests pass.

Suggested commit:

```text
test(customer-account): verify account core
```

## QA Checklist Seeds

### Commerce Node

- [ ] Auth contract operation IDs and summaries are stable.
- [ ] Register/login/logout/refresh/confirm/change-password/update-profile remain compatible.
- [ ] Registration disabled mode rejects safely.
- [ ] Registration standard mode preserves current behavior.
- [ ] Forgot-password response is generic for unknown email.
- [ ] Reset-password uses ASP.NET Identity tokens.
- [ ] Change-password complexity matches registration complexity.
- [ ] Current profile endpoint requires bearer auth.
- [ ] Profile update rejects server-owned identity fields.
- [ ] Cart merge-current-customer derives identity from JWT.
- [ ] Address ownership checks still pass.
- [ ] Order list resolves by `CommerceCustomer.CustomerId`.
- [ ] Legacy `UserId` order fallback works only when compatible.
- [ ] Order detail enforces owner/store scope.
- [ ] Guest completion requires token.
- [ ] Wrong guest token fails safely.
- [ ] Storefront OpenAPI has response schemas/security metadata for account endpoints.

### Storefront V2

- [ ] Unauthenticated account pages redirect to sign-in with safe return URL.
- [ ] Authenticated customer can view profile.
- [ ] Authenticated customer can update allowed profile fields.
- [ ] Authenticated customer can change password.
- [ ] Authenticated customer can manage address book through Address Core.
- [ ] Authenticated customer can view paged order list.
- [ ] Authenticated customer can view own order detail.
- [ ] Guest order completion works only with token.
- [ ] Account pages have mobile/desktop-safe layout.
- [ ] Browser network does not send `userId`, `customerId`, `storeId`, roles, statuses, or provider references.

### Control Plane

- [ ] No Control Plane customer management UI is added in this phase.
- [ ] No customer account runtime data is stored in `ControlPlaneDbContext`.
- [ ] ControlPlane Web does not call Storefront customer/account APIs.

## Failure Modes To Design Against

- [ ] Customer accesses another customer's order by guessing reference.
- [ ] Guest order detail exposed by predictable ID/reference.
- [ ] Public request supplies `customerId` or `userId`.
- [ ] V2 orders missing from account history.
- [ ] Email change breaks login/account identity.
- [ ] Password recovery leaks whether account exists.
- [ ] Custom password hashing weakens Identity security.
- [ ] Registration disabled but API still creates users.
- [ ] Store scope leaks across stores.
- [ ] UI creates future WASM migration pain.

## Test Map

- [ ] Auth contract tests:
  - [ ] operation IDs.
  - [ ] summaries.
  - [ ] request bodies.
  - [ ] response schemas.
  - [ ] bearer security.
- [ ] Registration policy tests:
  - [ ] disabled rejects.
  - [ ] standard succeeds.
  - [ ] email confirmation preserved.
- [ ] Password recovery tests:
  - [ ] unknown email generic response.
  - [ ] known email token generated.
  - [ ] reset validates token.
- [ ] Password complexity tests:
  - [ ] register policy.
  - [ ] change-password policy.
- [ ] Profile tests:
  - [ ] get current profile.
  - [ ] update current profile.
  - [ ] no server-owned fields accepted.
- [ ] Address ownership tests:
  - [ ] customer cannot mutate another customer's address.
- [ ] Order list tests:
  - [ ] paging.
  - [ ] current customer only.
  - [ ] `CustomerId` query path.
- [ ] Order detail tests:
  - [ ] owner-check.
  - [ ] safe fields only.
  - [ ] 404/403 for other customer.
- [ ] Guest completion tests:
  - [ ] reference alone fails.
  - [ ] wrong token fails.
  - [ ] correct token succeeds.
- [ ] Store scope tests:
  - [ ] same reference/customer from another store does not resolve.
- [ ] Storefront UI browser smoke:
  - [ ] sign-in redirect.
  - [ ] account pages.
  - [ ] orders empty/detail states.

## Migration And Compatibility

- [ ] Use additive migrations only.
- [ ] Existing `CommerceCustomer` rows remain valid.
- [ ] `FullName` remains compatibility/display fallback.
- [ ] `IsActive` defaults true.
- [ ] Preferred language/currency can be null.
- [ ] Existing unique `StoreId + NormalizedEmail` remains valid.
- [ ] Existing cart token/merge behavior remains valid.
- [ ] Existing auth route shapes remain valid.
- [ ] Existing Address Core routes remain valid.
- [ ] Existing Storefront V2 sign-in/register/logout pages remain compatible.
- [ ] No legacy database changes.

## Dependency Notes

- [ ] Depends on Cart Core for cart token and merge behavior.
- [ ] Depends on Address Core for authenticated address book.
- [ ] Depends on Checkout Core for guest checkout policy enforcement.
- [ ] Depends on Order Placement Core for durable order snapshots and guest access token.
- [ ] Depends on Security Privacy for captcha/password recovery target configuration.
- [ ] Aligns with Menu Navigation Core for account navigation entries.

## Out Of Scope Backlog

- [ ] Username/customer-number login.
- [ ] Admin approval registration.
- [ ] Customer groups.
- [ ] Customer-role visibility.
- [ ] Customer-role pricing.
- [ ] Full account privacy center.
- [ ] External identity providers.
- [ ] MFA/passkeys.
- [ ] Full return request workflow.
- [ ] Full reorder command.
- [ ] Full download entitlement validation.
- [ ] Stock subscription notification workflow.
- [ ] Customer support/admin CRM surface.
- [ ] Control Plane customer impersonation or management.

## Recommended Implementation Order

- [x] Phase 0 - baseline guardrails. 2026-07-17: committed after focused identity baseline run passed 60/60.
- [x] Phase 1 - customer profile model. 2026-07-17: CommerceNode API build passed, focused customer/model subset passed 37/37, and broader customer/address/checkout/model run passed 95/95.
- [x] Phase 2 - registration policy and password recovery. 2026-07-17: CommerceNode API build passed, focused auth/OpenAPI/captcha tests passed 88/88, and Storefront OpenAPI snapshots were refreshed.
- [ ] Phase 3 - account profile API and Storefront pages.
- [ ] Phase 4 - customer order self-service API.
- [ ] Phase 5 - guest completion lookup.
- [ ] Phase 6 - Storefront account UI integration.
- [ ] Phase 7 - contract, QA, and hardening.

## Acceptance Criteria

- [ ] Storefront customer can register.
- [ ] Storefront customer can sign in.
- [ ] Storefront customer can sign out.
- [ ] Storefront customer can recover password.
- [ ] Storefront customer can view/edit profile.
- [ ] Storefront customer can change password.
- [ ] Storefront customer can manage addresses.
- [ ] Storefront customer can view own order history.
- [ ] Guest cart ownership and merge behavior still work.
- [ ] Guest order completion requires safe token.
- [ ] Customer order history resolves V2 orders through `CommerceCustomer/CustomerId`.
- [ ] New APIs satisfy V2 API contract standards.
- [ ] QA checklists are updated.
- [ ] Focused build/tests pass.
- [ ] No legacy runtime is extended.
