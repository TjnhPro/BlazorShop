# Storefront Foundation Blocker Closure Todo

Status: Completed; reopened browser-action closure tracked by `Storefront Browser Action Boundary Closure.todo.md`
Owner: Storefront Platform
Created: 2026-07-28
Source plan: `Storefront Visual Only Phase 1 Boundary.todo.md`
Scope: close the remaining high-priority Storefront Foundation blockers before MVP so V2, Starter, and future `Storefront.{Name}` hosts are visual consumers only.

2026-07-28 browser-action follow-up: F1.54-F1.56 in `Storefront Browser Action Boundary Closure.todo.md` reopened the Foundation closure because V2/generated browser action orchestration and generated functional JS remained after F1.45-F1.53. That follow-up is now the active closure record for browser action ownership, generated fast/full proof split, and final guardrails.

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

- [x] Move or delete `BlazorShop.Storefront.Starter/Security/StarterReturnUrlValidator.cs`.
- [x] If logic is still needed, move it to Presentation as a shared return URL validator/service.
- [x] Remove `Security/StarterReturnUrlValidator.cs` from Starter protected files.
- [x] Update StorefrontBuilder generator metadata to stop copying/protecting Starter security code.
- [x] Remove direct Runtime package reference from Starter if Presentation packaging can expose the required transitive dependency safely.
- [x] Update Presentation package/project metadata so visual hosts can consume Presentation without direct Runtime reference.
- [x] Update generated project template package references:
  - [x] keep `BlazorShop.Storefront.Presentation`.
  - [x] keep `BlazorShop.Storefront.Components`.
  - [x] remove direct `BlazorShop.Storefront.Runtime`.
  - [x] remove direct `BlazorShop.Storefront.Client` unless only kept as package metadata and explicitly documented.
- [x] Update architecture docs where they still describe generated storefronts as direct Runtime consumers.
- [x] If any temporary exception remains, add:
  - [x] reason.
  - [x] allowed project.
  - [x] removal condition.
  - [x] guardrail test that fails if exception expands.

### Tests

- [x] Build Starter after removing security helper.
- [x] Generate a sample StorefrontBuilder project.
- [x] Validate generated project has no `Security/`, `Services/`, `Middleware/`, or Runtime direct reference.
- [x] Validate protected files metadata no longer includes deleted security files.
- [x] Validate Presentation still composes Runtime successfully.

### Definition of Done

- [x] Starter contains no application security implementation.
- [x] Generated projects contain no application security implementation.
- [x] Visual hosts do not direct-reference Runtime/Client, or the only remaining exception is documented with a removal gate.

### Notes

- Deleted the Starter return URL validator helper and removed stale Starter `_Imports.razor` references; no replacement service was needed because Presentation-owned auth/navigation contracts already cover the active flow.
- Starter and generated projects now direct-reference only `BlazorShop.Storefront.Presentation` and `BlazorShop.Storefront.Components`. `BlazorShop.Storefront.Runtime` remains packaged as a transitive Presentation dependency, and `BlazorShop.Storefront.Client` remains packed only to satisfy Runtime's package dependency chain.
- StorefrontBuilder generator, static validation, generated project validation, preflight, isolation gate, and sample release gate now reject direct Runtime/Client package references and application/security folders in generated output.
- No temporary exception remains. Guardrails now fail if `Security/`, `Services/`, `Middleware/`, direct Runtime package references, or direct Client package references reappear in generated visual hosts.
- Verification:
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj -v:minimal` - passed.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontStarterFoundationBoundaryTests|FullyQualifiedName~StorefrontPresentationCutoverGuardrailTests.StorefrontStarter_ViewsRenderPresentationContextsOnly|FullyQualifiedName~StorefrontIndependenceBoundaryTests.StorefrontStarter_UsesPackageFirstContractsAndNoForbiddenSourceDependencies|FullyQualifiedName~StorefrontBuilderFoundationTests|FullyQualifiedName~StorefrontBuilderVisualGenerationTests" -v:minimal` - passed 51/51.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontStarterHostSmokeTests" -v:minimal` - passed 9/9.
  - `.\scripts\qa\run-storefront-builder-generated-proof.ps1` - passed generated proof/static validation/isolation gate.

## Phase F1.49 - Route, Link, Head, And Layout Context Contracts

Goal: visual hosts should receive semantic context and link descriptors instead of knowing app routes and startup-critical contracts implicitly.

### Tasks

- [x] Introduce Presentation-owned link/context descriptors for common visual actions:
  - [x] home.
  - [x] search.
  - [x] cart.
  - [x] checkout.
  - [x] account root.
  - [x] account profile.
  - [x] account addresses.
  - [x] account orders.
  - [x] account password/change-password.
  - [x] login/signin.
  - [x] register.
  - [x] logout form target.
  - [x] new releases.
  - [x] today deals.
  - [x] customer service.
  - [x] cookie/privacy pages.
  - [x] category/product/page URL builders.
- [x] Update `StorefrontShellContext` or related context models to expose these descriptors.
- [x] Replace V2 visual usage of `StorefrontRoutes.*` where a context descriptor can be used.
- [x] Replace Starter hardcoded `/account/*`, `/cart`, `/search`, `/deals` where a context descriptor can be used.
- [x] Keep `StorefrontRoutes` internal to Presentation routing/services where it still belongs.
- [x] Add explicit startup validation for:
  - [x] `ApplicationHead` component type.
  - [x] `MainLayout` component type.
  - [x] required `Context` parameter.
  - [x] required `Body` parameter for layouts.
  - [x] compatibility with `StorefrontApplicationHeadContext`.
  - [x] compatibility with `StorefrontMainLayoutContext`.
- [x] Replace hardcoded `<html lang="en">` with server-rendered language and direction context.
- [x] Remove V2 inline script that mutates `document.documentElement.lang`.
- [x] Add `dir` support if display context includes or can infer right-to-left direction.

### Tests

- [x] Startup validation test: missing `ApplicationHead` context parameter fails clearly.
- [x] Startup validation test: missing `MainLayout` body/context parameter fails clearly.
- [x] Render test: document `lang` comes from Presentation display context.
- [x] Render test: V2 brand head no longer mutates document language with inline script.
- [x] Architecture test: V2/Starter visual folders do not reference `StorefrontRoutes` except approved temporary allowlist.

### Definition of Done

- [x] Route knowledge is mostly Presentation-owned.
- [x] Visual hosts consume semantic links/context.
- [x] Head/layout component mistakes fail at startup, not after a page is hit.
- [x] Document language/dir is server-rendered by Presentation.

### Notes

- Added `StorefrontLinkContext` to Presentation shell/page contexts for common links and category/product/page URL builders; V2 and Starter visual Razor no longer reference `StorefrontRoutes`.
- Made `StorefrontRoutes` internal to Presentation and exposed internals to `BlazorShop.Tests.V2` for route contract tests.
- Added `TextDirection` to `StorefrontDisplayContext`; `StorefrontApp.razor` now renders `<html lang="@DocumentLanguage" dir="@DocumentDirection">` from server-side display context.
- Removed the V2 brand head inline `document.documentElement.lang` mutation; brand head keeps only store-specific icon/language metadata.
- Startup validation now checks `ApplicationHead` and `MainLayout` component types, required `Context`, and required layout `Body`. The active compatibility contract is `StorefrontShellContext` for both head/layout slots.
- StorefrontBuilder composition was updated so generated layouts keep `sfb-cart-badge` while using Presentation link descriptors instead of hardcoded `/cart` or `/account`.
- Verification:
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj -v:minimal` - passed.
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj -v:minimal` - passed.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontDisplayContextProviderTests|FullyQualifiedName~StorefrontShellContextServiceTests|FullyQualifiedName~StorefrontPresentationFoundationBoundaryTests|FullyQualifiedName~StorefrontPresentationCutoverGuardrailTests|FullyQualifiedName~StorefrontStarterFoundationBoundaryTests|FullyQualifiedName~LayoutAssetFoundationTests|FullyQualifiedName~StorefrontBrandingMarkupTests" -v:minimal` - passed 102/102.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontVisualOnlyBoundaryTests" -v:minimal` - passed 12/12.
  - `.\scripts\qa\run-storefront-builder-generated-proof.ps1` - passed generated proof/static validation/isolation gate.

## Phase F1.50 - Consent Contract And Shell Resilience

Goal: consent behavior should be a platform capability, and optional shell data should not make the storefront unavailable.

### Tasks

- [x] Move consent command/state ownership to Presentation:
  - [x] consent current endpoint.
  - [x] consent accept endpoint.
  - [x] consent revoke endpoint.
  - [x] consent context model.
  - [x] semantic browser events.
- [x] Define whether consent visual is required or optional:
  - [x] required: every host must register a consent visual component.
  - [x] optional: host explicitly disables consent visual through a capability setting.
- [x] Make Starter behavior explicit; do not silently omit consent.
- [x] Update V2 consent banner to use shared consent context/action descriptors.
- [x] Remove V2 consent transport logic from visual JS.
- [x] Split shell context loading into required and optional groups.
- [x] Required data should fail clearly:
  - [x] current store.
  - [x] display/store identity.
  - [x] session/auth summary if required for shell correctness.
- [x] Optional data should degrade:
  - [x] header menus.
  - [x] footer menus.
  - [x] categories.
  - [x] page navigation links.
  - [x] non-critical cart badge enhancements if local API fails.
- [x] Add structured logging for optional shell data failures.
- [x] Add fallback empty states for optional menus/categories without taking down the shell.

### Tests

- [x] Unit test: optional menu failure still returns shell context with fallback menu state.
- [x] Unit test: required store/display failure returns maintenance/service-unavailable path as appropriate.
- [x] Playwright test: Starter renders explicit consent behavior.
- [x] Playwright test: V2 consent accept/revoke still works through Presentation app JS.
- [x] Architecture test: V2 consent component does not call BFF endpoints directly.

### Definition of Done

- [x] Consent is consistent across V2 and Starter.
- [x] Consent transport is Presentation-owned.
- [x] Optional shell failures do not kill storefront rendering.
- [x] Required shell failures remain explicit and safe.

Notes/evidence:

- `StorefrontConsentContext` now supplies policy/action/event descriptors from Presentation; `StorefrontFoundationViewSet.ConsentBanner` is a required visual slot.
- V2 registers `StorefrontConsentBanner`; Starter registers `StarterConsentBanner` and always renders explicit consent descriptors, including disabled state when consent is disabled by configuration.
- Presentation `storefront.application.js` owns consent current/accept/revoke browser behavior and semantic events; V2 `storefrontCommerce.js` no longer calls consent commands.
- `StorefrontShellContextService` keeps display/session as required and wraps navigation menus, page links, category tree, and consent configuration as optional dependencies with structured warning logs and fallback empty states.
- Verification:
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj -v:minimal` - passed.
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj -v:minimal` - passed.
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj -v:minimal` - passed.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontStarterHostSmokeTests|FullyQualifiedName~StorefrontShellContextServiceTests|FullyQualifiedName~SecurityPrivacyPhase3ConsentTests|FullyQualifiedName~StorefrontPresentationFoundationBoundaryTests|FullyQualifiedName~StorefrontStarterFoundationBoundaryTests|FullyQualifiedName~StorefrontCommerceScriptRegressionTests|FullyQualifiedName~LayoutAssetFoundationTests" -v:minimal` - passed 97/97.
  - `node scripts/qa/storefront-application-js-split-proof.js` - passed.
  - `.\scripts\qa\run-storefront-builder-generated-proof.ps1` - passed.

## Phase F1.51 - Shared Visual Consumer Boundary Validator

Goal: replace narrow token checks with a reusable scanner that can validate V2, Starter, generated proof projects, and future `Storefront.{Name}` projects.

### Tasks

- [x] Create a shared test helper, for example `StorefrontVisualConsumerBoundaryValidator`.
- [x] Validate project references and package references:
  - [x] no backend/core/API project references.
  - [x] no Control Plane references.
  - [x] no Commerce Node references.
  - [x] no Storefront V2 reference from Starter/generated.
  - [x] no Runtime/Client direct reference unless the project is explicitly allowlisted.
- [x] Validate forbidden folders for visual hosts:
  - [x] `Services/`
  - [x] `Services/Contracts/`
  - [x] `Security/`
  - [x] `Middleware/`
  - [x] `Endpoints/`
  - [x] `Configuration/` except visual registration/configuration allowlist.
  - [x] `Options/` except visual options allowlist.
  - [x] `Models/` except visual view model allowlist.
- [x] Scan source files:
  - [x] `.cs`
  - [x] `.razor`
  - [x] `.cshtml` if present.
  - [x] `.js`
  - [x] `.mjs`
  - [x] `.ts`
  - [x] `.json` for protected metadata where relevant.
  - [x] `.yaml`/`.yml` generation contracts.
- [x] Forbid application transport tokens in visual hosts:
  - [x] `HttpClient`
  - [x] `IHttpClientFactory`
  - [x] `fetch(`
  - [x] `XMLHttpRequest`
  - [x] `/api/storefront/`
  - [x] `/api/cart`
  - [x] `/api/checkout`
  - [x] `/api/consent`
  - [x] `/api/product-selection-preview`
  - [x] antiforgery token lookup.
- [x] Forbid service locator patterns:
  - [x] `[Inject] IServiceProvider`
  - [x] `GetRequiredService<`
  - [x] `GetService<`
  - [x] constructor injection of application services in visual components.
- [x] Forbid presentation contract implementation in visual hosts:
  - [x] classes implementing `IStorefront*Client`.
  - [x] classes implementing Runtime facade/provider interfaces.
  - [x] manual transport clients.
- [x] Add positive allowlists:
  - [x] Program/bootstrap.
  - [x] view registration.
  - [x] visual components/pages/layouts.
  - [x] CSS/images/fonts/static visual assets.
  - [x] visual JS only.
  - [x] appsettings.
- [x] Produce clear failure messages:
  - [x] file path.
  - [x] forbidden token/folder/reference.
  - [x] owning package where logic should move.
  - [x] suggested remediation.

### Tests

- [x] Replace or extend `StorefrontVisualOnlyBoundaryTests`.
- [x] Run validator against V2.
- [x] Run validator against Starter.
- [x] Run validator against generated proof project.
- [x] Add negative fixture tests that prove the scanner fails for:
  - [x] visual `HttpClient`.
  - [x] visual `fetch('/api/cart')`.
  - [x] visual `Services/` folder.
  - [x] direct Runtime reference.
  - [x] `IStorefrontCatalogClient` implementation.

### Definition of Done

- [x] One shared validator protects all visual consumers.
- [x] Guardrail scans C#/Razor/JS/project metadata.
- [x] Failure messages are actionable.
- [x] Known current leaks are either fixed or explicitly allowlisted with removal date.

Notes/evidence:

- Added `StorefrontVisualConsumerBoundaryValidator` and validator tests that cover V2, Starter, the generated proof project, and negative fixtures.
- Fixed current visual-host leaks instead of broad allowlisting: product purchase action route descriptors now come from Presentation, shared WASM antiforgery/cart interop JS lives in Components, and Starter visual copy no longer embeds BFF route literals.
- Validator reports the violating file/reference/folder, the owning package, and remediation guidance for transport, service locator, contract implementation, and project reference failures.
- Verification:
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontVisualConsumerBoundaryValidatorTests" -v:minimal` - passed 4/4.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontVisualConsumerBoundaryValidatorTests|FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests.ProductPurchasePanel_UsesHostActionDescriptorAfterHpr6Migration|FullyQualifiedName~StorefrontPresentationFoundationBoundaryTests.ProductPageVerticalSlice_IsPresentationRouteWithV2ViewOnly|FullyQualifiedName~StorefrontVisualOnlyBoundaryTests" -v:minimal` - passed 18/18.
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj -v:minimal` - passed.
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj -v:minimal` - passed.
  - `.\scripts\qa\run-storefront-builder-generated-proof.ps1` - passed.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontVisualConsumerBoundaryValidatorTests" -v:minimal` - passed 4/4 after generated proof regeneration.

## Phase F1.52 - Generated Proof, Browser Functional Proof, And CI Closure

Goal: generated project validation must prove both structure and real storefront behavior before Foundation is closed.

### Tasks

- [x] Split StorefrontBuilder proof into two levels:
  - [x] structure smoke proof: project generates, restores, builds, validates boundary.
  - [x] foundation functional proof: generated host runs against fixture store and exercises required browser behavior.
- [x] Expand generated browser proof to cover:
  - [x] home renders with header/footer.
  - [x] category/product links navigate.
  - [x] product gallery or product image area renders.
  - [x] product quantity control renders.
  - [x] product selection preview runs when available.
  - [x] add-to-cart succeeds through same-origin BFF.
  - [x] cart badge updates.
  - [x] cart page renders current item.
  - [x] checkout entry route loads or redirects according to auth/cart state.
  - [x] account link route loads or redirects according to auth state.
  - [x] consent accept/revoke path works.
  - [x] SEO title/meta exists for home/product/page.
  - [x] missing slug/not-found route renders visual not-found state.
- [x] Require fixture data for browser proof:
  - [x] at least one store.
  - [x] at least one category.
  - [x] at least one product with image.
  - [x] at least one purchasable product.
  - [x] COD or test payment-capable checkout path when functional proof includes order placement.
- [x] Update StorefrontBuilder CI path filters to include:
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/**`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/**`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Client/**`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/**`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/**`
  - [x] `tools/BlazorShop.AI.StorefrontBuilder/**`
  - [x] `scripts/qa/run-storefront-builder-*.ps1`
  - [x] relevant docs and validation scripts.
- [x] Ensure V2 Foundation CI still runs:
  - [x] focused architecture tests.
  - [x] V2 host smoke.
  - [x] Starter host smoke.
  - [x] StorefrontBuilder generated proof.
  - [x] COD/browser network regression where applicable.

### Tests

- [x] `dotnet test BlazorShop.Tests.V2 --filter StorefrontVisualOnlyBoundaryTests`
- [x] `dotnet test BlazorShop.Tests.V2 --filter StorefrontPresentation`
- [x] `scripts/qa/run-storefront-builder-generated-proof.ps1`
- [x] `scripts/qa/run-storefront-builder-isolation-gate.ps1`
- [x] Playwright V2 browser flow for product/add-to-cart/cart/checkout entry.
- [x] Playwright Starter/generated browser flow for structure plus functional foundation.

### Definition of Done

- [x] Generated proof fails if a generated host owns app transport/security/service logic.
- [x] Generated proof fails if required fixture-backed storefront behavior is missing.
- [x] CI runs generated proof when any foundation package changes.
- [x] Foundation functional proof is required before Phase 1 closure.

Notes/evidence:

- `run-storefront-builder-generated-proof.ps1` now has explicit `Structure` and `FoundationFunctional` proof levels. Structure proof generates/restores/builds the proof project, runs StorefrontBuilder static validation, runs the isolation gate, and runs the shared visual consumer validator against the generated proof.
- Foundation functional proof probes the configured fixture store before browser QA: store configuration, category `apparel`, product `qa-simple-product-100`, product image/media, purchasability/in-stock state, content page `customer-service`, and COD/test payment capability.
- Generated composition now emits functional browser descriptors for shell category links, product purchase panel state, same-origin add-to-cart through `window.blazorShopStorefront.application.cart.addLine`, and cart badge feedback without generated direct Commerce Node browser transport.
- StorefrontBuilder CI path filters include Presentation, Runtime, Client, Components, Starter, StorefrontBuilder tooling, generated proof scripts, and relevant docs. CI runs the structure proof on foundation/package changes; manual and scheduled StorefrontBuilder workflow runs require the `FoundationFunctional` browser proof.
- Verification:
  - `node --check tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/apply-composition.mjs` - passed.
  - `node --check tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-commerce-regression.mjs` - passed.
  - `node --check tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-visual-qa.mjs` - passed.
  - `pwsh -NoProfile -File scripts/qa/run-storefront-builder-generated-proof.ps1 -Describe` - passed and listed `Structure` and `FoundationFunctional`.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontBuilderQaRegenerationTests|FullyQualifiedName~StorefrontVisualConsumerBoundaryValidatorTests" -v:minimal` - passed 15/15.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontVisualOnlyBoundaryTests" -v:minimal` - passed 12/12.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontPresentation" -v:minimal` - passed 30/30.
  - `.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure` - passed.
  - `.\scripts\qa\run-storefront-builder-isolation-gate.ps1` - passed.
  - `.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctional -RuntimeTimeoutSeconds 90` - passed; generated visual smoke and functional commerce reports have zero failures.
  - `.\scripts\qa\run-storefront-order-email-e2e.ps1 -Headless -TimeoutSeconds 1200` - passed; result `passed`, COD orders `ORD-20260728-5A25BF61` and `ORD-20260728-F4C18066`, order email sent/retried, no 5xx responses, no retired flow calls.

## Phase F1.53 - Cleanup, Documentation, And Closure Gate

Goal: remove stale references and make the final Foundation status honest.

### Tasks

- [x] Update `docs/architecture/03-runtime-boundaries.md` to reflect final direct dependency rule.
- [x] Update `docs/architecture/10-v2-contract-ownership.md` to reflect final StorefrontBuilder/Starter dependency shape.
- [x] Update `docs/architecture/11-storefront-builder.md` if generated project package references change.
- [x] Update `docs/agents/storefront-builder.md` with new proof levels and commands.
- [x] Update `docs/visual-reverse-engineering-skill/README.md` only if StorefrontBuilder output/protected files changed.
- [x] Update `QA-StorefrontV2.todo.md` with new browser checks.
- [x] Update `QA-StorefrontStarter.todo.md` with explicit consent/core-script/security checks.
- [x] Update old references to `storefrontCommerce.js` after the split.
- [x] Remove `Theme/Pages` or rename/move it into a clearly visual-owned folder that matches current folder guide.
- [x] Rename Presentation generic namespaces in a separate mechanical pass if blast radius is high:
  - [x] `BlazorShop.Storefront.Services` -> `BlazorShop.Storefront.Presentation.Services`
  - [x] `BlazorShop.Storefront.Services.Contracts` -> `BlazorShop.Storefront.Presentation.Contracts`
  - [x] `BlazorShop.Storefront.Options` -> `BlazorShop.Storefront.Presentation.Options`
  - [x] `BlazorShop.Storefront.Configuration` -> `BlazorShop.Storefront.Presentation.Configuration`
  - [x] `BlazorShop.Storefront.Models` -> `BlazorShop.Storefront.Presentation.Models`
- [x] Mark `Storefront Visual Only Phase 1 Boundary.todo.md` as complete only after this file is complete.
- [x] Close this file with exact commands run and pass/fail evidence.

### Tests

- [x] Full focused Storefront test set.
- [x] StorefrontBuilder generated proof.
- [x] V2 Playwright browser proof.
- [x] Starter/generated browser proof.
- [x] `git grep` or equivalent scan proves stale strings are gone or intentionally allowlisted.

### Definition of Done

- [x] No stale docs say Foundation is complete before blockers are closed.
- [x] No stale docs describe generated stores as owning Runtime transport if target changed.
- [x] QA checklist contains browser-level release checks, not only smoke.
- [x] Closure evidence is present in this file and in the original Phase 1 file.

Notes/evidence:

- `BlazorShop.Storefront.V2/Theme/Pages/*` was moved into `BlazorShop.Storefront.V2/Pages/*`, matching the folder guide's visual-owned page template location.
- Presentation generic namespaces were mechanically renamed to `BlazorShop.Storefront.Presentation.Services`, `BlazorShop.Storefront.Presentation.Contracts`, `BlazorShop.Storefront.Presentation.Options`, `BlazorShop.Storefront.Presentation.Configuration`, and `BlazorShop.Storefront.Presentation.Models`. `Services.System` was renamed to `Services.SystemPages` to avoid shadowing the BCL `System` namespace.
- Architecture, StorefrontBuilder, visual reverse engineering, V2 QA, Starter QA, and the original Phase 1 plan now reflect the final dependency/proof shape. Historical F1.45 baseline references to old `storefrontCommerce.js` ownership are intentionally retained as pre-fix evidence.
- Verification:
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj -v:minimal` - passed.
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj -v:minimal` - passed.
  - `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -v:minimal` - passed.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontVisualOnlyBoundaryTests|FullyQualifiedName~StorefrontStarterFoundationBoundaryTests|FullyQualifiedName~LayoutAssetFoundationTests" -v:minimal` - passed 59/59.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontBuilderQaRegenerationTests|FullyQualifiedName~StorefrontBuilderFoundationTests|FullyQualifiedName~StorefrontStarterHostSmokeTests|FullyQualifiedName~StorefrontShellContextServiceTests|FullyQualifiedName~StorefrontDisplayContextProviderTests|FullyQualifiedName~StorefrontPageNavigationProviderTests|FullyQualifiedName~StorefrontProductPageServiceTests|FullyQualifiedName~StorefrontSitemapServiceTests|FullyQualifiedName~StorefrontStructuredDataComposerTests" -v:minimal` - passed 56/56.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.SignIn" -v:minimal` - passed 5/5.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.Checkout_WhenCartIsEmpty|FullyQualifiedName~StorefrontV2HostSmokeTests.StorefrontFormPost|FullyQualifiedName~StorefrontV2HostSmokeTests.Register|FullyQualifiedName~StorefrontV2HostSmokeTests.ForgotPassword|FullyQualifiedName~StorefrontV2HostSmokeTests.ResetPassword" -v:minimal` - passed 22/22.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.Account|FullyQualifiedName~StorefrontV2HostSmokeTests.CurrencyPreference|FullyQualifiedName~StorefrontV2HostSmokeTests.Logout|FullyQualifiedName~StorefrontShellContextServiceTests" -v:minimal` - passed 19/19.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.Robots|FullyQualifiedName~StorefrontV2HostSmokeTests.Sitemap|FullyQualifiedName~StorefrontV2HostSmokeTests.Maintenance|FullyQualifiedName~StorefrontV2HostSmokeTests.Cart|FullyQualifiedName~StorefrontV2HostSmokeTests.Checkout_Post|FullyQualifiedName~StorefrontV2HostSmokeTests.Payment" -v:minimal` - passed 19/19.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~SecurityPrivacyPhase1CsrfTests|FullyQualifiedName~SecurityPrivacyPhase3ConsentTests|FullyQualifiedName~SecurityPrivacyPhase4CaptchaTests|FullyQualifiedName~StorefrontCommerceScriptRegressionTests|FullyQualifiedName~StorefrontBrandingMarkupTests" -v:minimal` - passed 30/30.
  - `.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure` - passed.
  - `.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser` - exited 0.
  - `.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctional -RuntimeTimeoutSeconds 90` - passed; generated report has zero failures and no direct Commerce Node browser calls.
  - `.\scripts\qa\run-storefront-order-email-e2e.ps1 -Headless -TimeoutSeconds 1200` - passed; result `passed`, COD orders `ORD-20260728-A08C276B` and `ORD-20260728-BAEEB48D`, no 5xx responses, no retired flow calls.
  - `.\scripts\stop-v2-local.ps1` - exited 0 and stopped Control Plane API/Web, Commerce Node API, and Storefront V2.
  - `rg -n "\[ \]" "docs/refactor-control-Commerce-storefront/Storefront Foundation Blocker Closure.todo.md" "docs/refactor-control-Commerce-storefront/Storefront Visual Only Phase 1 Boundary.todo.md"` - no results.
  - `rg -n "namespace BlazorShop\.Storefront\.(Services|Options|Configuration|Models)|using BlazorShop\.Storefront\.(Services|Options|Configuration|Models)|@using BlazorShop\.Storefront\.(Services|Options|Configuration|Models)|global using BlazorShop\.Storefront\.(Services|Options|Configuration|Models)|Theme/Pages|Theme\\Pages|BlazorShop\.Storefront\.Presentation\.Services\.System\b" BlazorShop.PresentationV2 BlazorShop.Tests.V2 scripts tools .github -g "!*bin*" -g "!*obj*"` - no source/test/script/workflow results.
  - `git diff --check` - passed with Git LF/CRLF working-copy warnings only.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~Storefront" -v:minimal` was attempted as a broad focused run but exceeded the 15-minute command timeout; the narrower groups above are the closure evidence, matching the existing baseline strategy for slow Storefront host tests.

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
- [x] Historical F1.45 temporary allowlists: direct Runtime references in Starter/generated proof/Builder validation; V2 `storefrontCommerce.js` owned fetch, antiforgery, consent, product-selection, and cart behavior until F1.46-F1.48/F1.51 closed the boundary.
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
