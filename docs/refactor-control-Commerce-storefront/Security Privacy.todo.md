# BlazorShop Security Privacy Todo

Generated: 2026-07-16

Scope:

- 11.1 Storefront security.
- 11.2 Consent.
- 11.3 Captcha abstraction.

Boundary rules:

- [x] Work only in active V2 projects.
- [x] Do not extend legacy presentation projects or `AppDbContext`.
- [x] Keep Storefront V2 browser calls on local Storefront routes or Commerce Node Storefront APIs.
- [x] Keep Control Plane Web behind Control Plane API for admin management.
- [x] Never expose captcha secrets, refresh tokens, raw consent visitor keys, or internal security settings in public config or OpenAPI examples.

## Phase 0 - Baseline Inventory And Guardrail Tests

Goal: lock down current security behavior before changing runtime code.

Implementation checklist:

- [x] Inventory Storefront V2 browser mutation routes:
  - [x] `POST /sign-in`
  - [x] `POST /register`
  - [x] `POST /logout`
  - [x] `POST /currency`
  - [x] `POST /checkout`
  - [x] `POST /api/cart/lines`
  - [x] `PUT /api/cart/lines/{lineId}`
  - [x] `DELETE /api/cart/lines/{lineId}`
  - [x] `DELETE /api/cart`
- [x] Inventory Commerce Node public mutation routes:
  - [x] `auth/register`
  - [x] `auth/login`
  - [x] `auth/refresh-token`
  - [x] `auth/logout`
  - [x] `auth/change-password`
  - [x] `auth/update-profile`
  - [x] `cart/session`
  - [x] `cart/lines`
  - [x] `cart/validate`
  - [x] `checkout/preview`
  - [x] `checkout/place-order`
  - [x] `currency/preference`
  - [x] `newsletter/subscribe`
  - [x] `orders/confirm`
- [x] Add Storefront V2 regression tests proving server form routes still require and accept antiforgery tokens.
- [x] Add tests proving `StorefrontReturnUrl.Normalize` rejects external, protocol-relative, backslash, CR, and LF return URLs.
- [x] Add Commerce Node OpenAPI guardrails for security metadata on protected Storefront endpoints.
- [x] Record cookie inventory:
  - [x] refresh cookie
  - [x] antiforgery cookie
  - [x] cart token cookie
  - [x] legacy cart cleanup cookie
  - [x] currency preference cookie
  - [x] future consent cookie

Verification checklist:

- [x] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~StorefrontV2HostSmokeTests" --no-restore -p:UseSharedCompilation=false`
- [x] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests" --no-restore -p:UseSharedCompilation=false`

Exit criteria:

- [x] Existing auth, cart, checkout, and return URL behavior is documented by tests.
- [x] Public Storefront schemas still do not expose secrets or internal fields.

## Phase 1 - CSRF And Browser Mutation Hardening

Goal: make all browser-driven Storefront V2 mutations CSRF-safe without changing Commerce Node route ownership.

Implementation checklist:

- [x] Add a Storefront V2 antiforgery token projection for JavaScript.
- [x] Prefer rendering the token into a safe meta tag or bootstrap JSON on pages that load cart JS.
- [x] No same-origin token endpoint added because render-time meta projection is covered by host smoke tests.
- [x] Update `storefrontCommerce.js` to send the antiforgery request header on non-GET cart mutations.
- [x] Require antiforgery validation for local `/api/cart/*` mutation routes.
- [x] Keep `GET /api/cart` readable without antiforgery.
- [x] Confirm server form POST routes still work with existing antiforgery tokens.
- [x] Add private/no-store response headers to sensitive local mutation responses where needed.
- [x] Add tests:
  - [x] cart mutation without token fails.
  - [x] cart mutation with valid token succeeds.
  - [x] `GET /api/cart` remains usable.
  - [x] sign-in/register/logout/checkout form paths remain unchanged.

Constraints:

- [x] Do not add node credentials to Storefront V2.
- [x] Do not require Storefront V2 to call Control Plane.
- [x] Do not move cart APIs to direct browser calls against Commerce Node.
- [x] Do not break add-to-cart buttons or cart badge refresh.

Exit criteria:

- [x] Browser JSON mutations and server form mutations have consistent CSRF protection.
- [x] No route shape changes for `/api/cart/*`.

## Phase 2 - Storefront Rate Limits And Bot/Crawler Mutation Restrictions

Goal: add pragmatic abuse controls for public mutation endpoints.

Implementation checklist:

- [x] Add Commerce Node rate limiter registration in `BlazorShop.CommerceNode.API/Program.cs`.
- [x] Add local Storefront V2 rate limiter registration for `/api/cart/*` if Commerce Node-only limiting does not cover local mutation pressure.
- [x] Define named policies:
  - [x] `storefront-auth-strict`
  - [x] `storefront-cart`
  - [x] `storefront-checkout`
  - [x] `storefront-newsletter`
  - [x] `storefront-currency`
- [x] Use IP + route + store key partitioning for anonymous endpoints.
- [x] Use user id + store key partitioning for authenticated endpoints.
- [x] Add `Retry-After` where supported.
- [x] Return consistent error response shape using existing Commerce Node API helpers.
- [x] Add noindex/no-store headers to mutation endpoints where responses could be crawled or cached.
- [x] Confirm robots/indexing policy blocks API/mutation discovery, without relying on robots as security.
- [x] Add tests for rate-limited endpoints and response metadata.

Constraints:

- [x] Keep defaults permissive enough for local development.
- [x] Make limits configurable.
- [x] Do not use CAPTCHA as the only rate-limit mechanism.
- [x] Do not rate-limit static assets, sitemap, robots, or public catalog reads in this phase.

Exit criteria:

- [x] Public mutation endpoints have named, testable rate-limit policies.
- [x] Abuse responses are predictable and contract-safe.

## Phase 3 - Storefront Consent Core

Goal: add store-scoped consent state, category model, and change/revoke behavior.

Implementation checklist:

- [x] Add `StorefrontConsentState` with store scope, consent key, hashed visitor key, category flags, version, timestamps, revoke state, and expiry.
- [x] Add `StorefrontConsentEvent` with store scope, event type, consent version, category JSON, and timestamp.
- [x] Add typed consent options/settings:
  - [x] enabled
  - [x] current version
  - [x] banner required
  - [x] visitor cookie lifetime
  - [x] event retention days
  - [x] default optional categories disabled
- [x] Add consent category constants:
  - [x] essential
  - [x] preferences
  - [x] analytics
  - [x] marketing
- [x] Classify existing cookies:
  - [x] essential: refresh token, antiforgery, cart token, checkout/session.
  - [x] preference: currency.
  - [x] optional: future analytics and marketing.
- [x] Add Commerce Node Storefront APIs:
  - [x] `GET api/storefront/stores/{storeKey}/consent/current`
  - [x] `POST api/storefront/stores/{storeKey}/consent`
  - [x] `POST api/storefront/stores/{storeKey}/consent/revoke`
- [x] Add public-safe configuration projection fields:
  - [x] consent enabled
  - [x] consent version
  - [x] categories
  - [x] policy page slug/path
  - [x] safe cookie lifetimes
- [x] Add Storefront V2 consent banner/component.
- [x] Allow accept essential only.
- [x] Allow accept selected categories.
- [x] Allow change/revoke from footer or privacy page link.
- [x] Currency preference remains allowed because no active store policy currently requires preferences consent; policy-gated enforcement is left for the admin settings phase.
- [x] Keep essential auth/cart/antiforgery cookies functional.
- [x] Add newsletter consent hook.

Constraints:

- [x] Storefront consent cookie must not contain raw email, user id, or internal row id.
- [x] Visitor key must be random and hashed server-side before storage.
- [x] Consent APIs must be store-scoped by route.
- [x] Admin editing must go through Control Plane gateway when implemented.

Exit criteria:

- [x] Storefront can display, persist, change, and revoke consent.
- [x] Public config does not expose private settings.
- [x] Essential site behavior continues without optional consent.

## Phase 4 - Captcha Abstraction

Goal: provide provider-neutral captcha verification for high-abuse Storefront targets.

Implementation checklist:

- [ ] Add `ICaptchaVerifier`.
- [ ] Add `CaptchaVerificationRequest`.
- [ ] Add `CaptchaVerificationResult`.
- [ ] Add target constants:
  - [ ] login
  - [ ] registration
  - [ ] newsletter
  - [ ] password-recovery future
  - [ ] contact future
  - [ ] review future
  - [ ] checkout optional only if abuse appears
- [ ] Add typed captcha settings:
  - [ ] enabled globally
  - [ ] provider system name
  - [ ] per-target activation
  - [ ] minimum score where supported
  - [ ] public site/widget key
  - [ ] private secret reference outside public projection
- [ ] Add `NoopCaptchaVerifier` for disabled/default behavior.
- [ ] Add provider adapter shape for future reCAPTCHA/hCaptcha without provider-specific types in Domain.
- [ ] Add server-side verification before enabled login/register/newsletter actions.
- [ ] Add public-safe config projection:
  - [ ] captcha enabled targets
  - [ ] provider system name
  - [ ] public site key when needed
  - [ ] action names
- [ ] Add Storefront V2 rendering hook for captcha metadata/token submission.
- [ ] Add tests:
  - [ ] disabled captcha does not block existing flows.
  - [ ] enabled captcha with missing token fails.
  - [ ] enabled captcha with failed verifier fails.
  - [ ] enabled captcha with successful verifier allows action.
  - [ ] public config never returns secret key.

Constraints:

- [ ] Provider secrets must never appear in DTOs, public config, logs, or OpenAPI examples.
- [ ] Captcha failure messages must be safe and generic.
- [ ] Do not add captcha to every endpoint by default.
- [ ] Do not use captcha as a substitute for rate limiting.

Exit criteria:

- [ ] Captcha can be enabled per store/target without route rewrites.
- [ ] Provider details stay behind server-side abstraction.

## Phase 5 - Privacy Retention And Anti-Enumeration Policy

Goal: make privacy-sensitive retention and account enumeration behavior explicit.

Implementation checklist:

- [ ] Add privacy/security settings:
  - [ ] refresh token IP retention days
  - [ ] refresh token user-agent retention days
  - [ ] consent event retention days
  - [ ] captcha verification log retention days
  - [ ] newsletter consent evidence retention days
  - [ ] anonymize IP after retention window
- [ ] Add retention cleanup service or task using existing Commerce Node task/worker pattern only if recurring cleanup is needed.
- [ ] Prefer existing task orchestration before adding a new worker.
- [ ] Add policy tests for IP/user-agent truncation/anonymization helpers.
- [ ] Define anti-enumeration rules:
  - [ ] login remains generic for invalid user/password.
  - [ ] password recovery returns generic success-like response when implemented.
  - [ ] registration duplicate-user response is decided explicitly before changing behavior.
- [ ] Add future secure file-download authorization docs:
  - [ ] require `[Authorize]`.
  - [ ] verify current user owns resource or has active download entitlement.
  - [ ] verify store id.
  - [ ] never serve by path alone.

Constraints:

- [ ] Do not add private download tables until a real private file feature exists.
- [ ] Do not remove useful audit data without explicit retention migration.
- [ ] Do not log captcha tokens, refresh tokens, or raw consent visitor keys.

Exit criteria:

- [ ] Privacy-sensitive data retention is policy-driven.
- [ ] Future password recovery and private download work has clear security requirements.

## Phase 6 - Admin Management And Permissions

Goal: expose security/privacy settings through the approved Control Plane path.

Implementation checklist:

- [ ] Add Commerce Node admin APIs under `api/commerce/admin/security-privacy` or similarly explicit route.
- [ ] Add Control Plane API gateway methods.
- [ ] Add Control Plane Web pages only after backend contracts are stable.
- [ ] Add permissions:
  - [ ] security/privacy settings view
  - [ ] security/privacy settings edit
  - [ ] captcha settings edit
  - [ ] consent settings edit
- [ ] Add audit events for every settings change.
- [ ] Mask secret state:
  - [ ] `SecretConfigured`
  - [ ] `LastRotatedAt`
  - [ ] provider display name
  - [ ] never return secret value after save
- [ ] Invalidate public configuration projection when consent/captcha/security settings change.

Constraints:

- [ ] Control Plane Web must not call Commerce Node directly.
- [ ] Control Plane API must not expose provider secrets to Web after save.
- [ ] Use `CommerceNodeDbContext` for node-local runtime settings.

Exit criteria:

- [ ] Admins can manage store security/privacy settings safely.
- [ ] Permissions are not permanently bundled under broad store write.

## Phase 7 - QA, Contract Tests, And Documentation Closeout

Goal: finish the phase with regression coverage and QA checklist updates.

Implementation checklist:

- [ ] Update `QA-CommerceNode.todo.md`:
  - [ ] rate limit behavior
  - [ ] captcha verification
  - [ ] consent APIs
  - [ ] public config secret guardrails
  - [ ] privacy retention
- [ ] Update `QA-StorefrontV2.todo.md`:
  - [ ] antiforgery for JSON mutations
  - [ ] consent banner/change/revoke
  - [ ] captcha rendering hook
  - [ ] return URL regression
  - [ ] account/order authorization smoke
- [ ] Update `QA-ControlPlane.todo.md` when admin management phase is implemented.
- [ ] Add or update OpenAPI metadata tests:
  - [ ] security requirements
  - [ ] operation ids
  - [ ] error response schemas
  - [ ] required request bodies
  - [ ] public schema secret exclusion
- [ ] Run focused verification:
  - [ ] Commerce Node API build
  - [ ] Storefront V2 build
  - [ ] Storefront V2 host smoke tests
  - [ ] Commerce Node Storefront OpenAPI tests
  - [ ] new security/privacy unit tests

Exit criteria:

- [ ] All changed API contracts are generator-safe.
- [ ] Public config remains secret-safe.
- [ ] QA checklists describe manual/browser coverage still needed.

## Cross-Phase Guardrails

- [ ] Harden existing auth/browser mutation surfaces before adding broad features.
- [ ] Keep guest cart and guest checkout.
- [ ] Put consent and captcha runtime state in Commerce Node.
- [ ] Verify captcha server-side and expose only public-safe metadata.
- [ ] Defer secure file downloads until a private download feature exists.
- [ ] Defer password recovery implementation but keep anti-enumeration policy ready.
- [ ] Reuse feature state and public configuration projection patterns.
