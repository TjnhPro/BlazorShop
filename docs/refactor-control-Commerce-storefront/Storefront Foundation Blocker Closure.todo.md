# Storefront Foundation Blocker Closure Todo

Status: Proposed
Owner: Storefront Platform
Created: 2026-07-28
Source plan: `Storefront Visual Only Phase 1 Boundary.todo.md`
Scope: close the remaining high-priority Storefront Foundation blockers before MVP so V2, Starter, and future `Storefront.{Name}` hosts are visual consumers only.

## Why this exists

`Storefront Visual Only Phase 1 Boundary.todo.md` has most tasks checked, but repo review found remaining boundary leaks that can still let a store host own application behavior. These blockers should be handled as a separate closure plan instead of silently adding more items to the already-large Phase 1 file.

The target is not a rewrite. The implementation should be mechanical and incremental:

- Presentation owns app bootstrap, security head, same-origin browser application commands, BFF endpoints, runtime registration, middleware, route/context contracts, document metadata, and platform guardrails.
- V2 owns V2 visual markup, CSS, images, copy, visual-only JavaScript, view registration, and component placement.
- Starter owns neutral visual markup and starter-specific copy/assets only.
- Generated `Storefront.{Name}` projects follow the same visual-consumer rules as Starter.

## Verified blockers

Checked items in this section mean "verified as present in the F1.45 baseline"; they are not fix/closure status.

- [x] V2 still loads application JavaScript from `BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js`.
- [x] `storefrontCommerce.js` owns consent, cart, product-selection preview, add-to-cart state, BFF `fetch` calls, antiforgery header lookup, and user-facing copy.
- [x] `StorefrontApp.razor` still delegates `ApplicationScripts` to the host view set.
- [x] Core antiforgery head can be omitted because host `ApplicationHead` must remember to render it.
- [x] Starter still owns `Security/StarterReturnUrlValidator.cs`.
- [x] StorefrontBuilder still protects/copies `Security/StarterReturnUrlValidator.cs`.
- [x] Generated proof still expects generated projects to reference `BlazorShop.Storefront.Runtime` directly.
- [x] Generated proof boundary scanner is too narrow and does not scan JavaScript, TypeScript, service folders, security folders, middleware folders, `HttpClient`, `fetch`, or DI service-locator patterns.
- [x] V2 and Starter visual views still know too many app routes directly.
- [x] `ApplicationHead` and `MainLayout` context requirements are validated at render time, not startup.
- [x] `UseHttpsRedirection` runs before `UseForwardedHeaders`.
- [x] Consent UI/behavior is inconsistent between V2 and Starter.
- [x] Presentation namespaces remain too generic: `BlazorShop.Storefront.Services`, `Options`, `Configuration`, `Models`.
- [x] V2 still has `Theme/Pages`.
- [x] Document language is hardcoded in `StorefrontApp.razor`; V2 patches it with an inline script.
- [x] Shell context loads optional and required data through one `Task.WhenAll`, so optional failure can kill the whole shell.
- [x] CI path filters for StorefrontBuilder do not include all foundation packages that can break generated proof.
- [x] Phase 1 documentation status is inconsistent with the remaining blockers.

## Architecture decision

Choose the stricter visual-consumer boundary:

```text
Storefront.V2
Storefront.Starter
Storefront.{Name}
    -> BlazorShop.Storefront.Presentation
    -> BlazorShop.Storefront.Components

Storefront.Presentation
    -> BlazorShop.Storefront.Runtime
    -> BlazorShop.Storefront.Client
```

Hosts should not reference Runtime or Client directly. If package constraints make this impossible in the current repo state, the implementation must explicitly document the temporary exception and add an obsolete/removal gate, but the preferred target is no direct Runtime/Client reference from visual hosts.

## Non-goals

Checked non-goals mean acknowledged constraints for this closure plan.

- [x] Do not rewrite Storefront Runtime or generated client behavior.
- [x] Do not change Commerce Node Storefront API routes.
- [x] Do not change cart, checkout, account, payment, or catalog business behavior.
- [x] Do not redesign V2 visuals.
- [x] Do not introduce a new frontend framework.
- [x] Do not move Control Plane or Commerce Node responsibilities.
- [x] Do not remove existing browser flows until replacement behavior is verified by Playwright.

## Phase F1.45 - Current State Lock And Regression Baseline

Goal: make the current leaks explicit before moving code so the implementation can prove each leak is closed.

### Tasks

- [x] Add a short pre-implementation evidence section to `Storefront Visual Only Phase 1 Boundary.todo.md` that points to this closure plan and marks the old plan as not closable yet.
- [x] Capture current source evidence for:
  - [x] `StorefrontApplicationScripts.razor` loading V2 application JS.
  - [x] `storefrontCommerce.js` BFF fetch calls.
  - [x] `StorefrontApp.razor` rendering `ApplicationScripts`.
  - [x] V2 `StorefrontApplicationHead.razor` owning antiforgery head.
  - [x] Starter `ApplicationHead.razor` missing antiforgery head.
  - [x] Starter `Security/StarterReturnUrlValidator.cs`.
  - [x] StorefrontBuilder generator metadata and protected files.
  - [x] Starter direct Runtime package reference.
  - [x] Presentation Runtime reference metadata.
  - [x] Middleware order in Presentation hosting extension.
  - [x] CI path filters.
- [x] Run focused baseline tests before code movement:
  - [x] `dotnet test BlazorShop.Tests.V2 --filter StorefrontVisualOnlyBoundaryTests`
  - [x] Existing StorefrontBuilder generated project validation script.
  - [x] Existing V2/Starter smoke tests if present.
- [x] Record failing or insufficient tests in this file under "Baseline notes".

### Definition of Done

- [x] Baseline is documented.
- [x] Existing tests either pass or known gaps are documented.
- [x] No implementation files changed in this phase except docs/checklist metadata.

## Phase F1.46 - Presentation-Owned Security Head And Core Scripts

Goal: Presentation must render mandatory security/application bootstrap pieces regardless of host view registration.

### Tasks

- [x] Move mandatory antiforgery/security head rendering into `BlazorShop.Storefront.Presentation/App/StorefrontApp.razor`.
- [x] Ensure security head renders before host visual head.
- [x] Keep host `ApplicationHead` for branding, SEO visuals, CSS links, and host-specific metadata only.
- [x] Add Presentation-owned core script component, for example:
  - [x] `StorefrontFoundationCoreScripts.razor`
  - [x] or equivalent internal component under `Views/Foundation`.
- [x] Load Blazor script and Presentation-owned core application script from Presentation, not from V2/Starter.
- [x] Rename `ApplicationScripts` slot to `VisualScripts` or add `VisualScripts` first with an obsolete compatibility alias if direct rename is too risky in one commit.
- [x] Update V2 and Starter view registration to use the visual script slot only.
- [x] Add startup validation that core security/scripts cannot be replaced by a host view.

### Tests

- [x] Unit/component test: `StorefrontApp` output contains antiforgery meta/component for V2 and Starter.
- [x] Unit/component test: V2 and Starter still render host `ApplicationHead`.
- [x] Architecture test: no host view set can omit Presentation core scripts.
- [x] Browser test: cart/consent mutation still sends antiforgery token after script move.

### Definition of Done

- [x] Security head is Presentation-owned.
- [x] Core application scripts are Presentation-owned.
- [x] Host script slot is visual-only.
- [x] V2/Starter cannot accidentally omit antiforgery or core app JS.

### F1.46 Notes

- Added `StorefrontFoundationCoreScripts.razor` and `wwwroot/js/storefront.application.js` in Presentation. F1.47 will move cart/consent/product-selection behavior into that static asset.
- `StorefrontFoundationViewSet.VisualScripts` is the active host slot. `ApplicationScripts` remains as an obsolete compatibility alias for generated/template transition only.
- Browser-level Playwright is deferred to F1.47 because behavior has not been split yet. F1.46 proof uses host smoke tests plus cart/consent mutation antiforgery smoke coverage.
- Verification:
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontPresentationFoundationBoundaryTests|FullyQualifiedName~SecurityPrivacyPhase1CsrfTests|FullyQualifiedName~LayoutAssetFoundationTests.StorefrontRoot_DefinesExpectedAssetsWithoutDuplicates" -v:minimal` - passed 26/26.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.SignIn_RendersPresentationOwnedSecurityHeadAndCoreScriptsWithV2VisualHead|FullyQualifiedName~StorefrontStarterHostSmokeTests.StarterRoot_RendersPresentationOwnedSecurityHeadAndCoreScripts" -v:minimal` - passed 2/2.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.CartApi_PostLine_SetsHttpOnlyCartToken_AndDoesNotSendUnitPrice|FullyQualifiedName~StorefrontV2HostSmokeTests.CartApi_MutationWithoutAntiforgeryToken_ReturnsBadRequest|FullyQualifiedName~StorefrontV2HostSmokeTests.ConsentApi_PostWithAntiforgeryToken_ReturnsSavedState" -v:minimal` - passed 6/6.
  - `.\scripts\qa\run-storefront-builder-generated-proof.ps1` - passed generated proof/static validation/isolation gate.

## Phase F1.47 - Split Application JS From Visual JS

Goal: move browser application behavior out of V2 while preserving existing browser flows.

### Tasks

- [x] Create Presentation-owned static asset path for core app JS, for example `BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js`.
- [x] Move these behaviors from V2 JS into Presentation JS:
  - [x] same-origin local API client wrapper.
  - [x] antiforgery header/meta lookup.
  - [x] consent current/accept/revoke commands.
  - [x] cart get/add/update/remove/clear/recalculate commands.
  - [x] product-selection preview command.
  - [x] add-to-cart command orchestration.
  - [x] consistent error payload handling.
  - [x] semantic event dispatch for visual code.
- [x] Keep only V2 visual DOM behavior in V2 JS, for example:
  - [x] menu toggles.
  - [x] visual feedback animations.
  - [x] V2-specific CSS class toggles.
  - [x] V2-specific visual hooks that subscribe to Presentation app events.
- [x] Remove user-facing final copy from core JS where possible; emit status/code/default technical fallback.
- [x] Define a stable browser event contract:
  - [x] `storefront:cart:changed`
  - [x] `storefront:cart:error`
  - [x] `storefront:consent:changed`
  - [x] `storefront:product-selection:changed`
  - [x] `storefront:product-selection:error`
- [x] Ensure empty 204/empty-success responses are handled without JSON parse failure.
- [x] Ensure absolute/protocol-relative URLs remain rejected for local BFF calls.

### Tests

- [x] JavaScript-focused Playwright test: consent accept/revoke/current flow still works.
- [x] JavaScript-focused Playwright test: product selection preview updates availability/quantity/add-to-cart state.
- [x] JavaScript-focused Playwright test: add-to-cart updates cart badge and cart page.
- [x] Architecture test: V2 `wwwroot/js` cannot contain `/api/cart`, `/api/consent`, `/api/product-selection-preview`, `fetch(`, or antiforgery token lookup unless explicitly allowlisted as visual-only.
- [x] Architecture test: Presentation owns same-origin BFF fetch calls.

### Definition of Done

- [x] V2 has no application transport JavaScript.
- [x] Presentation JS owns browser application commands.
- [x] V2 JS is visual-only and replaceable by Starter/generated stores.
- [x] Existing user flows continue passing in browser.

### F1.47 Notes

- `BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js` now owns same-origin `fetch`, antiforgery lookup, local route rejection, cart/consent/product-selection command APIs, empty-response-safe JSON parsing, and the `storefront:*` event contract.
- `BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js` keeps DOM collection, visual feedback, toasts, gallery/address behavior, and calls `window.blazorShopStorefront.application`; it no longer contains `fetch(`, `/api/cart`, `/api/consent`, `/api/product-selection-preview`, or antiforgery meta lookup.
- Verification:
  - `node --check BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js` - passed.
  - `node --check BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js` - passed.
  - `node scripts/qa/storefront-application-js-split-proof.js` - passed Playwright JS proof for consent current/save/revoke, product-selection preview, add-to-cart badge update, antiforgery headers, and event dispatch.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontCommerceScriptRegressionTests|FullyQualifiedName~SecurityPrivacyPhase1CsrfTests|FullyQualifiedName~SecurityPrivacyPhase3ConsentTests.StorefrontV2_RendersConsentBannerAndUsesLocalProxyEndpoints|FullyQualifiedName~StorefrontBrandingMarkupTests.StorefrontLocalCart_PostsCurrencyCode|FullyQualifiedName~StorefrontBrandingMarkupTests.ProductPage_UsesBackendSelectionPreviewForVariantAttributes" -v:minimal` - passed 9/9.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.SignIn_RendersPresentationOwnedSecurityHeadAndCoreScriptsWithV2VisualHead|FullyQualifiedName~StorefrontV2HostSmokeTests.CartApi_PostLine_SetsHttpOnlyCartToken_AndDoesNotSendUnitPrice|FullyQualifiedName~StorefrontV2HostSmokeTests.CartApi_MutationWithoutAntiforgeryToken_ReturnsBadRequest|FullyQualifiedName~StorefrontV2HostSmokeTests.ConsentApi_PostWithAntiforgeryToken_ReturnsSavedState" -v:minimal` - passed 7/7.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontVisualOnlyBoundaryTests" -v:minimal` - passed 12/12.
  - `.\scripts\qa\run-storefront-builder-generated-proof.ps1` - passed generated proof/static validation/isolation gate.

## Phase F1.48 - Starter And StorefrontBuilder Hardening

Goal: Starter and generated projects must not carry application/security/transport implementation.

### Tasks

- [ ] Move or delete `BlazorShop.Storefront.Starter/Security/StarterReturnUrlValidator.cs`.
- [ ] If logic is still needed, move it to Presentation as a shared return URL validator/service.
- [ ] Remove `Security/StarterReturnUrlValidator.cs` from Starter protected files.
- [ ] Update StorefrontBuilder generator metadata to stop copying/protecting Starter security code.
- [ ] Remove direct Runtime package reference from Starter if Presentation packaging can expose the required transitive dependency safely.
- [ ] Update Presentation package/project metadata so visual hosts can consume Presentation without direct Runtime reference.
- [ ] Update generated project template package references:
  - [ ] keep `BlazorShop.Storefront.Presentation`.
  - [ ] keep `BlazorShop.Storefront.Components`.
  - [ ] remove direct `BlazorShop.Storefront.Runtime`.
  - [ ] remove direct `BlazorShop.Storefront.Client` unless only kept as package metadata and explicitly documented.
- [ ] Update architecture docs where they still describe generated storefronts as direct Runtime consumers.
- [ ] If any temporary exception remains, add:
  - [ ] reason.
  - [ ] allowed project.
  - [ ] removal condition.
  - [ ] guardrail test that fails if exception expands.

### Tests

- [ ] Build Starter after removing security helper.
- [ ] Generate a sample StorefrontBuilder project.
- [ ] Validate generated project has no `Security/`, `Services/`, `Middleware/`, or Runtime direct reference.
- [ ] Validate protected files metadata no longer includes deleted security files.
- [ ] Validate Presentation still composes Runtime successfully.

### Definition of Done

- [ ] Starter contains no application security implementation.
- [ ] Generated projects contain no application security implementation.
- [ ] Visual hosts do not direct-reference Runtime/Client, or the only remaining exception is documented with a removal gate.

## Phase F1.49 - Route, Link, Head, And Layout Context Contracts

Goal: visual hosts should receive semantic context and link descriptors instead of knowing app routes and startup-critical contracts implicitly.

### Tasks

- [ ] Introduce Presentation-owned link/context descriptors for common visual actions:
  - [ ] home.
  - [ ] search.
  - [ ] cart.
  - [ ] checkout.
  - [ ] account root.
  - [ ] account profile.
  - [ ] account addresses.
  - [ ] account orders.
  - [ ] account password/change-password.
  - [ ] login/signin.
  - [ ] register.
  - [ ] logout form target.
  - [ ] new releases.
  - [ ] today deals.
  - [ ] customer service.
  - [ ] cookie/privacy pages.
  - [ ] category/product/page URL builders.
- [ ] Update `StorefrontShellContext` or related context models to expose these descriptors.
- [ ] Replace V2 visual usage of `StorefrontRoutes.*` where a context descriptor can be used.
- [ ] Replace Starter hardcoded `/account/*`, `/cart`, `/search`, `/deals` where a context descriptor can be used.
- [ ] Keep `StorefrontRoutes` internal to Presentation routing/services where it still belongs.
- [ ] Add explicit startup validation for:
  - [ ] `ApplicationHead` component type.
  - [ ] `MainLayout` component type.
  - [ ] required `Context` parameter.
  - [ ] required `Body` parameter for layouts.
  - [ ] compatibility with `StorefrontApplicationHeadContext`.
  - [ ] compatibility with `StorefrontMainLayoutContext`.
- [ ] Replace hardcoded `<html lang="en">` with server-rendered language and direction context.
- [ ] Remove V2 inline script that mutates `document.documentElement.lang`.
- [ ] Add `dir` support if display context includes or can infer right-to-left direction.

### Tests

- [ ] Startup validation test: missing `ApplicationHead` context parameter fails clearly.
- [ ] Startup validation test: missing `MainLayout` body/context parameter fails clearly.
- [ ] Render test: document `lang` comes from Presentation display context.
- [ ] Render test: V2 brand head no longer mutates document language with inline script.
- [ ] Architecture test: V2/Starter visual folders do not reference `StorefrontRoutes` except approved temporary allowlist.

### Definition of Done

- [ ] Route knowledge is mostly Presentation-owned.
- [ ] Visual hosts consume semantic links/context.
- [ ] Head/layout component mistakes fail at startup, not after a page is hit.
- [ ] Document language/dir is server-rendered by Presentation.

## Phase F1.50 - Consent Contract And Shell Resilience

Goal: consent behavior should be a platform capability, and optional shell data should not make the storefront unavailable.

### Tasks

- [ ] Move consent command/state ownership to Presentation:
  - [ ] consent current endpoint.
  - [ ] consent accept endpoint.
  - [ ] consent revoke endpoint.
  - [ ] consent context model.
  - [ ] semantic browser events.
- [ ] Define whether consent visual is required or optional:
  - [ ] required: every host must register a consent visual component.
  - [ ] optional: host explicitly disables consent visual through a capability setting.
- [ ] Make Starter behavior explicit; do not silently omit consent.
- [ ] Update V2 consent banner to use shared consent context/action descriptors.
- [ ] Remove V2 consent transport logic from visual JS.
- [ ] Split shell context loading into required and optional groups.
- [ ] Required data should fail clearly:
  - [ ] current store.
  - [ ] display/store identity.
  - [ ] session/auth summary if required for shell correctness.
- [ ] Optional data should degrade:
  - [ ] header menus.
  - [ ] footer menus.
  - [ ] categories.
  - [ ] page navigation links.
  - [ ] non-critical cart badge enhancements if local API fails.
- [ ] Add structured logging for optional shell data failures.
- [ ] Add fallback empty states for optional menus/categories without taking down the shell.

### Tests

- [ ] Unit test: optional menu failure still returns shell context with fallback menu state.
- [ ] Unit test: required store/display failure returns maintenance/service-unavailable path as appropriate.
- [ ] Playwright test: Starter renders explicit consent behavior.
- [ ] Playwright test: V2 consent accept/revoke still works through Presentation app JS.
- [ ] Architecture test: V2 consent component does not call BFF endpoints directly.

### Definition of Done

- [ ] Consent is consistent across V2 and Starter.
- [ ] Consent transport is Presentation-owned.
- [ ] Optional shell failures do not kill storefront rendering.
- [ ] Required shell failures remain explicit and safe.

## Phase F1.51 - Shared Visual Consumer Boundary Validator

Goal: replace narrow token checks with a reusable scanner that can validate V2, Starter, generated proof projects, and future `Storefront.{Name}` projects.

### Tasks

- [ ] Create a shared test helper, for example `StorefrontVisualConsumerBoundaryValidator`.
- [ ] Validate project references and package references:
  - [ ] no backend/core/API project references.
  - [ ] no Control Plane references.
  - [ ] no Commerce Node references.
  - [ ] no Storefront V2 reference from Starter/generated.
  - [ ] no Runtime/Client direct reference unless the project is explicitly allowlisted.
- [ ] Validate forbidden folders for visual hosts:
  - [ ] `Services/`
  - [ ] `Services/Contracts/`
  - [ ] `Security/`
  - [ ] `Middleware/`
  - [ ] `Endpoints/`
  - [ ] `Configuration/` except visual registration/configuration allowlist.
  - [ ] `Options/` except visual options allowlist.
  - [ ] `Models/` except visual view model allowlist.
- [ ] Scan source files:
  - [ ] `.cs`
  - [ ] `.razor`
  - [ ] `.cshtml` if present.
  - [ ] `.js`
  - [ ] `.mjs`
  - [ ] `.ts`
  - [ ] `.json` for protected metadata where relevant.
  - [ ] `.yaml`/`.yml` generation contracts.
- [ ] Forbid application transport tokens in visual hosts:
  - [ ] `HttpClient`
  - [ ] `IHttpClientFactory`
  - [ ] `fetch(`
  - [ ] `XMLHttpRequest`
  - [ ] `/api/storefront/`
  - [ ] `/api/cart`
  - [ ] `/api/checkout`
  - [ ] `/api/consent`
  - [ ] `/api/product-selection-preview`
  - [ ] antiforgery token lookup.
- [ ] Forbid service locator patterns:
  - [ ] `[Inject] IServiceProvider`
  - [ ] `GetRequiredService<`
  - [ ] `GetService<`
  - [ ] constructor injection of application services in visual components.
- [ ] Forbid presentation contract implementation in visual hosts:
  - [ ] classes implementing `IStorefront*Client`.
  - [ ] classes implementing Runtime facade/provider interfaces.
  - [ ] manual transport clients.
- [ ] Add positive allowlists:
  - [ ] Program/bootstrap.
  - [ ] view registration.
  - [ ] visual components/pages/layouts.
  - [ ] CSS/images/fonts/static visual assets.
  - [ ] visual JS only.
  - [ ] appsettings.
- [ ] Produce clear failure messages:
  - [ ] file path.
  - [ ] forbidden token/folder/reference.
  - [ ] owning package where logic should move.
  - [ ] suggested remediation.

### Tests

- [ ] Replace or extend `StorefrontVisualOnlyBoundaryTests`.
- [ ] Run validator against V2.
- [ ] Run validator against Starter.
- [ ] Run validator against generated proof project.
- [ ] Add negative fixture tests that prove the scanner fails for:
  - [ ] visual `HttpClient`.
  - [ ] visual `fetch('/api/cart')`.
  - [ ] visual `Services/` folder.
  - [ ] direct Runtime reference.
  - [ ] `IStorefrontCatalogClient` implementation.

### Definition of Done

- [ ] One shared validator protects all visual consumers.
- [ ] Guardrail scans C#/Razor/JS/project metadata.
- [ ] Failure messages are actionable.
- [ ] Known current leaks are either fixed or explicitly allowlisted with removal date.

## Phase F1.52 - Generated Proof, Browser Functional Proof, And CI Closure

Goal: generated project validation must prove both structure and real storefront behavior before Foundation is closed.

### Tasks

- [ ] Split StorefrontBuilder proof into two levels:
  - [ ] structure smoke proof: project generates, restores, builds, validates boundary.
  - [ ] foundation functional proof: generated host runs against fixture store and exercises required browser behavior.
- [ ] Expand generated browser proof to cover:
  - [ ] home renders with header/footer.
  - [ ] category/product links navigate.
  - [ ] product gallery or product image area renders.
  - [ ] product quantity control renders.
  - [ ] product selection preview runs when available.
  - [ ] add-to-cart succeeds through same-origin BFF.
  - [ ] cart badge updates.
  - [ ] cart page renders current item.
  - [ ] checkout entry route loads or redirects according to auth/cart state.
  - [ ] account link route loads or redirects according to auth state.
  - [ ] consent accept/revoke path works.
  - [ ] SEO title/meta exists for home/product/page.
  - [ ] missing slug/not-found route renders visual not-found state.
- [ ] Require fixture data for browser proof:
  - [ ] at least one store.
  - [ ] at least one category.
  - [ ] at least one product with image.
  - [ ] at least one purchasable product.
  - [ ] COD or test payment-capable checkout path when functional proof includes order placement.
- [ ] Update StorefrontBuilder CI path filters to include:
  - [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/**`
  - [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/**`
  - [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Client/**`
  - [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/**`
  - [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/**`
  - [ ] `tools/BlazorShop.AI.StorefrontBuilder/**`
  - [ ] `scripts/qa/run-storefront-builder-*.ps1`
  - [ ] relevant docs and validation scripts.
- [ ] Ensure V2 Foundation CI still runs:
  - [ ] focused architecture tests.
  - [ ] V2 host smoke.
  - [ ] Starter host smoke.
  - [ ] StorefrontBuilder generated proof.
  - [ ] COD/browser network regression where applicable.

### Tests

- [ ] `dotnet test BlazorShop.Tests.V2 --filter StorefrontVisualOnlyBoundaryTests`
- [ ] `dotnet test BlazorShop.Tests.V2 --filter StorefrontPresentation`
- [ ] `scripts/qa/run-storefront-builder-generated-proof.ps1`
- [ ] `scripts/qa/run-storefront-builder-isolation-gate.ps1`
- [ ] Playwright V2 browser flow for product/add-to-cart/cart/checkout entry.
- [ ] Playwright Starter/generated browser flow for structure plus functional foundation.

### Definition of Done

- [ ] Generated proof fails if a generated host owns app transport/security/service logic.
- [ ] Generated proof fails if required fixture-backed storefront behavior is missing.
- [ ] CI runs generated proof when any foundation package changes.
- [ ] Foundation functional proof is required before Phase 1 closure.

## Phase F1.53 - Cleanup, Documentation, And Closure Gate

Goal: remove stale references and make the final Foundation status honest.

### Tasks

- [ ] Update `docs/architecture/03-runtime-boundaries.md` to reflect final direct dependency rule.
- [ ] Update `docs/architecture/10-v2-contract-ownership.md` to reflect final StorefrontBuilder/Starter dependency shape.
- [ ] Update `docs/architecture/11-storefront-builder.md` if generated project package references change.
- [ ] Update `docs/agents/storefront-builder.md` with new proof levels and commands.
- [ ] Update `docs/visual-reverse-engineering-skill/README.md` only if StorefrontBuilder output/protected files changed.
- [ ] Update `QA-StorefrontV2.todo.md` with new browser checks.
- [ ] Update `QA-StorefrontStarter.todo.md` with explicit consent/core-script/security checks.
- [ ] Update old references to `storefrontCommerce.js` after the split.
- [ ] Remove `Theme/Pages` or rename/move it into a clearly visual-owned folder that matches current folder guide.
- [ ] Rename Presentation generic namespaces in a separate mechanical pass if blast radius is high:
  - [ ] `BlazorShop.Storefront.Services` -> `BlazorShop.Storefront.Presentation.Services`
  - [ ] `BlazorShop.Storefront.Services.Contracts` -> `BlazorShop.Storefront.Presentation.Contracts`
  - [ ] `BlazorShop.Storefront.Options` -> `BlazorShop.Storefront.Presentation.Options`
  - [ ] `BlazorShop.Storefront.Configuration` -> `BlazorShop.Storefront.Presentation.Configuration`
  - [ ] `BlazorShop.Storefront.Models` -> `BlazorShop.Storefront.Presentation.Models`
- [ ] Mark `Storefront Visual Only Phase 1 Boundary.todo.md` as complete only after this file is complete.
- [ ] Close this file with exact commands run and pass/fail evidence.

### Tests

- [ ] Full focused Storefront test set.
- [ ] StorefrontBuilder generated proof.
- [ ] V2 Playwright browser proof.
- [ ] Starter/generated browser proof.
- [ ] `git grep` or equivalent scan proves stale strings are gone or intentionally allowlisted.

### Definition of Done

- [ ] No stale docs say Foundation is complete before blockers are closed.
- [ ] No stale docs describe generated stores as owning Runtime transport if target changed.
- [ ] QA checklist contains browser-level release checks, not only smoke.
- [ ] Closure evidence is present in this file and in the original Phase 1 file.

## Implementation Order

1. F1.45 baseline.
2. F1.46 fixed security/core script ownership.
3. F1.47 JS split with Playwright verification.
4. F1.48 Starter/generated hardening.
5. F1.49 context/head/layout contract.
6. F1.50 consent/shell resilience.
7. F1.51 shared guardrail validator.
8. F1.52 generated proof and CI closure.
9. F1.53 documentation cleanup and closure gate.

Do not merge F1.47 with F1.51. First move the behavior, then teach guardrails to detect it. Otherwise the test changes can hide a partial migration.

## Baseline Notes

Recorded during F1.45 before implementation changes.

- [x] Current branch: `master`.
- [x] Current commit before F1.45 commit: `27448a3767fd`.
- [x] Source evidence:
  - `BlazorShop.Storefront.V2/Components/Layout/StorefrontApplicationScripts.razor:2` loads `js/storefrontCommerce.js` from V2.
  - `BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js:75` and `:161` call `fetch(route, options)`.
  - `BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js:19` and `:20` read antiforgery meta tags.
  - `BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js:112`, `:126`, and `:136` call consent BFF endpoints.
  - `BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js:539` calls product-selection preview.
  - `BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js:642` posts cart lines to `/api/cart/lines`.
  - `BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js:638` owns add-to-cart user-facing copy.
  - `BlazorShop.Storefront.Presentation/App/StorefrontApp.razor:15` renders host-owned `ViewSet.ApplicationScripts`.
  - `BlazorShop.Storefront.V2/Components/Layout/StorefrontApplicationHead.razor:7` renders `StorefrontAntiforgeryHead`; Starter `Components/Layout/ApplicationHead.razor` does not.
  - `BlazorShop.Storefront.Starter/Security/StarterReturnUrlValidator.cs:3` defines Starter-owned return URL security logic.
  - `BlazorShop.Storefront.Starter/starter-generation.contract.yaml:33` and `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/new-storefront-project.ps1:69` protect/copy `Security/StarterReturnUrlValidator.cs`.
  - `BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj:14` references `BlazorShop.Storefront.Runtime` directly.
  - `BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj:24` references Runtime with `PrivateAssets="all"`.
  - `BlazorShop.Storefront.Presentation/Hosting/StorefrontApplicationBuilderExtensions.cs:29` calls `UseHttpsRedirection()` before `UseForwardedHeaders()`.
  - `.github/workflows/storefront-builder.yml` still omits foundation package paths such as Storefront Presentation, Runtime, Client, Components, and Starter.
- [x] Baseline test commands:
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontVisualOnlyBoundaryTests" -v:minimal` - passed 12/12.
  - `.\scripts\qa\run-storefront-builder-generated-proof.ps1` - passed generated restore/build, static validation, and isolation gate.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontStarterHostSmokeTests" -v:minimal` - passed 8/8.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.SignIn" -v:minimal` - passed 4/4.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.Checkout_WhenCartIsEmpty" -v:minimal` - passed 1/1.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.StorefrontFormPost|FullyQualifiedName~StorefrontV2HostSmokeTests.Register|FullyQualifiedName~StorefrontV2HostSmokeTests.ForgotPassword|FullyQualifiedName~StorefrontV2HostSmokeTests.ResetPassword" -v:minimal` - passed 21/21.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.Account|FullyQualifiedName~StorefrontV2HostSmokeTests.CurrencyPreference|FullyQualifiedName~StorefrontV2HostSmokeTests.Logout|FullyQualifiedName~StorefrontShellContextServiceTests" -v:minimal` - passed 17/17.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.Robots|FullyQualifiedName~StorefrontV2HostSmokeTests.Sitemap|FullyQualifiedName~StorefrontV2HostSmokeTests.Maintenance|FullyQualifiedName~StorefrontV2HostSmokeTests.Cart|FullyQualifiedName~StorefrontV2HostSmokeTests.Checkout_Post|FullyQualifiedName~StorefrontV2HostSmokeTests.Payment" -v:minimal` - passed 19/19.
- [x] Baseline failures: no baseline command failed. Known insufficiencies remain: generated scanner does not yet scan JS/security/service/middleware transport ownership broadly; generated proof still expects direct Runtime references; host smoke does not prove visual JS/browser event behavior; V2 smoke was split by filter because the full class is slow.
- [x] Temporary allowlists: direct Runtime references in Starter/generated proof/Builder validation; V2 `storefrontCommerce.js` owns fetch, antiforgery, consent, product-selection, and cart behavior until F1.46-F1.48/F1.51 close the boundary.
- [x] Required fixture setup: generated proof creates an on-demand generated project under `artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof`; V2/Starter smoke tests use in-test host fixtures and stubs; browser proof for later phases requires the local V2 stack or explicit Playwright harness.
- [x] Baseline warnings: `MessagePack` NU1902/NU1903 advisories and outdated `caniuse-lite` warnings appear during restore/build; they pre-exist this docs-only baseline.

## Risk Register

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Moving JS breaks cart/consent/product selection | High | Split behavior under Playwright coverage; keep event names stable; run browser regression before cleanup. |
| Removing Runtime direct reference breaks Starter/generated build | High | Adjust Presentation package metadata first; validate generated project restore/build before removing references broadly. |
| Guardrail false positives block visual code | Medium | Use positive allowlists and failure messages; add negative fixture tests; allow temporary exceptions with owner/removal condition. |
| Route descriptor refactor touches too many views | Medium | Start with shell/header/footer/account links; keep `StorefrontRoutes` internal to Presentation; defer non-critical product promo links if needed. |
| Optional shell fallback hides real production outage | Medium | Only degrade optional menu/category/page data; required store/display/session failures remain explicit. |
| Documentation drifts from implementation | Medium | Closure phase updates architecture docs and QA checklists in the same PR. |

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | Scope | Add a new closure plan instead of mutating the old plan only | Auto-decided | Clear ownership | The old Phase 1 plan is large and near-closed; a focused blocker plan gives implementers a clean target. | Hide blockers inside existing checked sections. |
| 2 | Architecture | Use stricter visual-consumer dependency boundary | Auto-decided | Boundary safety | Future generated stores should not accidentally own Runtime/Client transport; Presentation is the correct facade. | Keep Starter/generated direct Runtime references as default. |
| 3 | Implementation | Move behavior before adding strict guardrails | Auto-decided | Incremental safety | Guardrails should verify the new boundary after code has moved, not force a half-migrated state into broad allowlists. | Add broad scanner first with many temporary exceptions. |
| 4 | QA | Require browser functional proof, not smoke-only proof | Auto-decided | Production readiness | Smoke tests do not catch missing cart/consent/product-selection behavior; Playwright must verify real flows. | Treat generated build success as enough. |

## GSTACK REVIEW REPORT

| Area | Status | Findings | Plan Response |
| --- | --- | --- | --- |
| CEO/Product | Pass with blocker | Foundation cannot be called complete while visual hosts can still own application transport/security behavior. | Add closure plan with P0/P1 blockers before MVP. |
| Design | Pass | No visual redesign needed; key design issue is ownership of visual vs application behavior. | Keep V2/Starter visual output intact; move only behavior/contracts. |
| Engineering | Issues open | Application JS, antiforgery head, Runtime dependency, route contracts, middleware order, shell failure coupling, and weak guardrails are real source-level issues. | Split into F1.45-F1.53 with focused tests and DoD. |
| DX | Issues open | Generated storefront authoring remains confusing if package/reference boundaries and proof rules are inconsistent. | Strengthen generated template metadata, docs, validation, and CI path filters. |

VERDICT: Approved as a closure plan. Do not close Storefront Foundation Phase 1 until this plan is complete.

NO UNRESOLVED DECISIONS
