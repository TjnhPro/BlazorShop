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

- [ ] Add a Storefront V2 antiforgery token projection for JavaScript.
- [ ] Prefer rendering the token into a safe meta tag or bootstrap JSON on pages that load cart JS.
- [ ] Add a same-origin token endpoint only if render-time projection is not reliable.
- [ ] Update `storefrontCommerce.js` to send the antiforgery request header on non-GET cart mutations.
- [ ] Require antiforgery validation for local `/api/cart/*` mutation routes.
- [ ] Keep `GET /api/cart` readable without antiforgery.
- [ ] Confirm server form POST routes still work with existing antiforgery tokens.
- [ ] Add private/no-store response headers to sensitive local mutation responses where needed.
- [ ] Add tests:
  - [ ] cart mutation without token fails.
  - [ ] cart mutation with valid token succeeds.
  - [ ] `GET /api/cart` remains usable.
  - [ ] sign-in/register/logout/checkout form paths remain unchanged.

Constraints:

- [ ] Do not add node credentials to Storefront V2.
- [ ] Do not require Storefront V2 to call Control Plane.
- [ ] Do not move cart APIs to direct browser calls against Commerce Node.
- [ ] Do not break add-to-cart buttons or cart badge refresh.

Exit criteria:

- [ ] Browser JSON mutations and server form mutations have consistent CSRF protection.
- [ ] No route shape changes for `/api/cart/*`.

## Phase 2 - Storefront Rate Limits And Bot/Crawler Mutation Restrictions

Goal: add pragmatic abuse controls for public mutation endpoints.

Implementation checklist:

- [ ] Add Commerce Node rate limiter registration in `BlazorShop.CommerceNode.API/Program.cs`.
- [ ] Add local Storefront V2 rate limiter registration for `/api/cart/*` if Commerce Node-only limiting does not cover local mutation pressure.
- [ ] Define named policies:
  - [ ] `storefront-auth-strict`
  - [ ] `storefront-cart`
  - [ ] `storefront-checkout`
  - [ ] `storefront-newsletter`
  - [ ] `storefront-currency`
- [ ] Use IP + route + store key partitioning for anonymous endpoints.
- [ ] Use user id + store key partitioning for authenticated endpoints.
- [ ] Add `Retry-After` where supported.
- [ ] Return consistent error response shape using existing Commerce Node API helpers.
- [ ] Add noindex/no-store headers to mutation endpoints where responses could be crawled or cached.
- [ ] Confirm robots/indexing policy blocks API/mutation discovery, without relying on robots as security.
- [ ] Add tests for rate-limited endpoints and response metadata.

Constraints:

- [ ] Keep defaults permissive enough for local development.
- [ ] Make limits configurable.
- [ ] Do not use CAPTCHA as the only rate-limit mechanism.
- [ ] Do not rate-limit static assets, sitemap, robots, or public catalog reads in this phase.

Exit criteria:

- [ ] Public mutation endpoints have named, testable rate-limit policies.
- [ ] Abuse responses are predictable and contract-safe.

## Phase 3 - Storefront Consent Core

Goal: add store-scoped consent state, category model, and change/revoke behavior.

Implementation checklist:

- [ ] Add `StorefrontConsentState` with store scope, consent key, hashed visitor key, category flags, version, timestamps, revoke state, and expiry.
- [ ] Add `StorefrontConsentEvent` with store scope, event type, consent version, category JSON, and timestamp.
- [ ] Add typed consent options/settings:
  - [ ] enabled
  - [ ] current version
  - [ ] banner required
  - [ ] visitor cookie lifetime
  - [ ] event retention days
  - [ ] default optional categories disabled
- [ ] Add consent category constants:
  - [ ] essential
  - [ ] preferences
  - [ ] analytics
  - [ ] marketing
- [ ] Classify existing cookies:
  - [ ] essential: refresh token, antiforgery, cart token, checkout/session.
  - [ ] preference: currency.
  - [ ] optional: future analytics and marketing.
- [ ] Add Commerce Node Storefront APIs:
  - [ ] `GET api/storefront/stores/{storeKey}/consent/current`
  - [ ] `POST api/storefront/stores/{storeKey}/consent`
  - [ ] `POST api/storefront/stores/{storeKey}/consent/revoke`
- [ ] Add public-safe configuration projection fields:
  - [ ] consent enabled
  - [ ] consent version
  - [ ] categories
  - [ ] policy page slug/path
  - [ ] safe cookie lifetimes
- [ ] Add Storefront V2 consent banner/component.
- [ ] Allow accept essential only.
- [ ] Allow accept selected categories.
- [ ] Allow change/revoke from footer or privacy page link.
- [ ] Gate currency preference persistence under preferences consent if required by store policy.
- [ ] Keep essential auth/cart/antiforgery cookies functional.
- [ ] Add newsletter consent hook.

Constraints:

- [ ] Storefront consent cookie must not contain raw email, user id, or internal row id.
- [ ] Visitor key must be random and hashed server-side before storage.
- [ ] Consent APIs must be store-scoped by route.
- [ ] Admin editing must go through Control Plane gateway when implemented.

Exit criteria:

- [ ] Storefront can display, persist, change, and revoke consent.
- [ ] Public config does not expose private settings.
- [ ] Essential site behavior continues without optional consent.

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
