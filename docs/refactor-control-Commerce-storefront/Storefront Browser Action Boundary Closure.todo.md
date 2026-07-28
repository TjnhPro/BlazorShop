# Storefront Browser Action Boundary Closure Todo

Status: In Progress
Owner: Storefront Platform
Created: 2026-07-28
Related plans:
- `Storefront Foundation Blocker Closure.todo.md`
- `Storefront Visual Only Phase 1 Boundary.todo.md`
- `Storefront Playwright E2E Release.todo.md`

Scope: close the final browser action boundary blockers after the initial Storefront Foundation split. The goal is to move browser application controllers out of visual hosts and generated storefronts while keeping existing cart, product-selection, consent, gallery, checkout, and account browser behavior intact.

## Problem Statement

Presentation now owns the browser transport layer:

- same-origin request wrapper.
- antiforgery lookup.
- `/api/cart` commands.
- `/api/consent` commands.
- `/api/product-selection-preview` command.
- application event names.

However, V2 and generated storefront scripts can still own browser application orchestration above transport:

- reading product/variant/attribute/quantity descriptors.
- constructing product-selection preview payloads.
- interpreting preview response into purchase state.
- validating purchase eligibility before calling cart commands.
- constructing add-to-cart payloads.
- invoking `window.blazorShopStorefront.application.cart.*`.
- invoking `window.blazorShopStorefront.application.productSelection.*`.
- updating cart badge based on direct command result.

That means the current state moved:

```text
HTTP transport
    V2/generated -> Presentation
```

but has not fully moved:

```text
browser application controller
    V2/generated -> Presentation
```

For the visual-only boundary to be real, V2, Starter, and generated `Storefront.{Name}` projects must only declare semantic descriptors and render visual feedback. Presentation must own the fixed browser action binders.

## Current Evidence To Reconfirm During F1.54

Baseline evidence was reconfirmed before the F1.54 implementation. The V2 browser-action items are resolved by F1.54; generated, validator, hosting, session, CI, naming, and documentation items remain scheduled for F1.55/F1.56.

- [x] V2 script uses `window.blazorShopStorefront?.application`.
- [x] V2 script calls `getStorefrontApplication().cart.current()`.
- [x] V2 script calls `getStorefrontApplication().productSelection.preview(...)`.
- [x] V2 script calls `getStorefrontApplication().cart.addLine(...)`.
- [x] V2 script builds `ProductId`, `ProductVariantId`, `SelectedAttributes`, `Quantity`, and `CurrencyCode` payloads.
- [x] V2 script maps `preview.canAddToCart`, `preview.stockQuantity`, price, SKU, gallery, and button state.
- [x] V2 script validates `canAddToCart`, stock, variant selection, and variant stock before calling cart.
- [x] StorefrontBuilder `apply-composition.mjs` writes `wwwroot/js/storefront-builder.functional.js`.
- [x] Generated functional bridge creates cart payload and calls `app.cart.addLine(payload)`.
- [x] Generated script is injected into `ApplicationHead` instead of `VisualScripts`.
- [x] Starter contract does not allow generated `wwwroot/js`, but generator writes generated JS there.
- [x] Shared validator forbids transport tokens but does not forbid application command invocation.
- [x] Shared validator still has `AllowRuntimeClientPackageMetadata`.
- [x] Presentation middleware still runs `UseHttpsRedirection()` before `UseForwardedHeaders()`.
- [x] `StorefrontShellContextService` still treats session summary as critical for shell render.
- [x] PR CI runs generated `Structure` proof but not required `FoundationFunctional` proof.
- [x] Starter namespaces still use `BlazorShop.Storefront.Starter.Theme.Pages.*`.
- [x] `StorefrontFoundationViewSet.CreateMinimal()` is still public.
- [x] Closure docs status is inconsistent with the remaining browser-action blocker.

## Architecture Decision

Use this final browser boundary:

```text
Browser DOM descriptors
    owned by visual host markup
        |
        v
Presentation fixed browser binders
    read descriptors
    build command payloads
    call Presentation application transport
    publish semantic events
        |
        v
Visual host JS
    listen to semantic events
    update CSS classes
    show toast/animation/focus/gallery state
```

Visual host JS may:

- [ ] subscribe to semantic events.
- [ ] toggle CSS classes.
- [ ] update visual-only text supplied by descriptor/event detail.
- [ ] show toast/animation.
- [ ] run gallery interaction.
- [ ] manage focus/keyboard visual behavior.
- [ ] read visual descriptors such as feedback target, animation target, and CSS class names.

Visual host JS must not:

- [ ] call `window.blazorShopStorefront.application.cart.*`.
- [ ] call `window.blazorShopStorefront.application.consent.*`.
- [ ] call `window.blazorShopStorefront.application.productSelection.*`.
- [ ] construct cart or product-selection command payloads.
- [ ] decide purchasability from stock or variant state.
- [ ] interpret `canAddToCart`, `stockQuantity`, SKU, GTIN, price, or availability as business state.
- [ ] call same-origin BFF routes directly through `fetch`.
- [ ] inject functional scripts through `ApplicationHead`.

## Non-goals

- [ ] Do not change Commerce Node Storefront API contracts.
- [ ] Do not change server-side sellability, pricing, stock, cart, checkout, or order rules.
- [ ] Do not redesign V2 product page/gallery/purchase panel.
- [ ] Do not rewrite all V2 JavaScript; split it into controller-owned and visual-owned pieces.
- [ ] Do not require generated stores to write JavaScript for cart/product purchase.
- [ ] Do not add React/Vue/etc.
- [ ] Do not make `FoundationFunctional` run live payment/order E2E on every PR.
- [ ] Do not remove Playwright live/full release checks; keep them for nightly/manual/release.

## Phase F1.54 - Browser Action Binding Ownership

Goal: Presentation owns product purchase, product-selection, add-to-cart, and cart badge browser controllers. V2 JS becomes visual-only for these flows.

### Target Components

Presentation should expose fixed browser binders under `window.blazorShopStorefront.bindings` or an equivalent stable namespace:

- [x] `productPurchase`
- [x] `productSelection`
- [x] `addToCart`
- [x] `cartBadge`
- [x] `consent` if current consent binder still mixes command and visual concerns.

Preferred shape:

```javascript
window.blazorShopStorefront.bindings.productPurchase.bindAll();
window.blazorShopStorefront.bindings.cartBadge.bindAll();
```

or automatic initialization inside Presentation core script after DOM ready.

### Descriptor Contract

Presentation binder reads semantic descriptors from markup. V2/Starter/generated markup may set values, but does not interpret them.

- [x] Product purchase root:

```html
<div data-storefront-product-purchase
     data-product-id="..."
     data-currency-code="..."
     data-selection-preview-route="...">
</div>
```

- [x] Quantity input:

```html
<input data-storefront-purchase-quantity
       min="1"
       max="..."
       step="..." />
```

- [x] Attribute control:

```html
<input data-storefront-purchase-attribute
       data-attribute-name="Size"
       value="M" />
```

- [x] Variant select:

```html
<select data-storefront-purchase-variant>
</select>
```

- [x] Add-to-cart command:

```html
<button data-storefront-command="cart.add-line"
        data-storefront-product-purchase-submit>
</button>
```

- [x] Feedback targets:

```html
<p data-storefront-purchase-feedback aria-live="polite"></p>
<span data-storefront-cart-badge></span>
```

### Binder Responsibilities

- [x] Find all `data-storefront-product-purchase` roots.
- [x] Read product descriptor.
- [x] Read quantity.
- [x] Read selected attributes.
- [x] Read selected/resolved variant descriptor.
- [x] Build `StorefrontProductSelectionPreviewRequest` payload.
- [x] Call `application.productSelection.preview(...)`.
- [x] Normalize preview response.
- [x] Store normalized selection state on the purchase root.
- [x] Publish semantic event:

```text
storefront:product-purchase:selection-changed
```

- [x] Publish semantic error:

```text
storefront:product-purchase:selection-error
```

- [x] Build `StorefrontAddCartLineLocalRequest` payload.
- [x] Call `application.cart.addLine(...)`.
- [x] Publish semantic success:

```text
storefront:product-purchase:add-line-succeeded
```

- [x] Publish semantic failure:

```text
storefront:product-purchase:add-line-failed
```

- [x] Refresh or update cart badge through a Presentation-owned cart badge binder.
- [x] Preserve existing legacy cart changed event only if existing V2 browser code or QA still depends on it.

### Move From V2 JS

Move these out of `BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js`:

- [x] `getStorefrontApplication()` for purchase/cart command calls.
- [x] `refreshCartSummary()`.
- [x] `applyCartSummary()`.
- [x] `collectSelectedAttributes()`.
- [x] `readSelectionQuantity()`.
- [x] `resolveSelectedVariantId()` if it affects command payload.
- [x] `buildSelectionPreviewPayload()`.
- [x] `previewSelection()`.
- [x] `scheduleSelectionPreview()` if it directly calls preview command.
- [x] `applySelectionPreview()` business/result interpretation.
- [x] `buildCartPayload()`.
- [x] `addToCart()`.
- [x] direct checks of `canAddToCart`.
- [x] direct checks of `stockQuantity`/stock.
- [x] direct checks of variant stock.
- [x] direct construction of `ProductId`, `ProductVariantId`, `SelectedAttributes`, `Quantity`, `CurrencyCode`.

Keep or rewrite in V2 JS as visual-only listeners:

- [x] gallery thumbnail selection.
- [x] gallery keyboard navigation.
- [x] gallery fallback image display.
- [x] checkout manual address visual toggle.
- [x] toast presentation.
- [x] button flash animation.
- [x] visual feedback rendering from semantic events.
- [x] CSS class toggles.

### V2 Markup Changes

- [x] Update `StorefrontProductPurchasePanel.razor` descriptors to match binder contract.
- [x] Update product summary/direct add buttons if they currently rely on V2 JS payload construction.
- [x] Keep server-provided data attributes only for declarative descriptors.
- [x] Ensure descriptor values are generated from Presentation context/model, not from duplicated V2 business logic.
- [x] Keep existing visual classes and layout stable.

### Compatibility Strategy

- [x] Keep old data attributes only as compatibility aliases inside Presentation binder for one phase if needed.
- [x] Mark compatibility aliases with comments and tests.
- [x] Remove aliases in the same phase if Playwright proves all V2/Starter/generated markup migrated. Not applicable for F1.54 because generated markup migration is scheduled for F1.55, so aliases are intentionally retained in Presentation only.
- [x] Do not leave V2 command invocation as fallback.

### Tests

- [x] Update `StorefrontCommerceScriptRegressionTests` so V2 script no longer contains:
  - [x] `getStorefrontApplication().cart.addLine`
  - [x] `getStorefrontApplication().productSelection.preview`
  - [x] `ProductId:`
  - [x] `ProductVariantId:`
  - [x] `SelectedAttributes:`
  - [x] `CurrencyCode:`
  - [x] `canAddToCart`
  - [x] `stockQuantity`
- [x] Add Presentation JS source test proving binder contains:
  - [x] product descriptor reader.
  - [x] selected attribute reader.
  - [x] preview payload builder.
  - [x] add-line payload builder.
  - [x] `application.productSelection.preview`.
  - [x] `application.cart.addLine`.
  - [x] semantic event dispatch.
- [x] Add negative architecture test: visual host JS cannot invoke application commands.
- [x] Add Playwright V2 test:
  - [x] product page loads.
  - [x] changing quantity/attribute triggers preview through same-origin route.
  - [x] UI updates price/availability/button state through event-driven visual listener.
  - [x] add-to-cart succeeds.
  - [x] cart badge updates.
  - [x] cart page contains added line.
- [x] Add Playwright no-direct-transport assertion:
  - [x] browser network calls use same-origin `/api/*`.
  - [x] no browser call goes to Commerce Node base URL.

### Definition of Done

- [x] V2 JS no longer constructs product-selection or cart command payloads.
- [x] V2 JS no longer invokes application cart/product-selection commands.
- [x] Presentation owns browser product purchase command orchestration.
- [x] V2 still renders the same visual purchase/gallery/cart behavior.
- [x] Focused tests and Playwright product/add-to-cart regression pass.

### F1.54 Evidence

- [x] `node --check BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js`
- [x] `node --check BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js`
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj -v:minimal`
- [x] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontCommerceScriptRegressionTests|FullyQualifiedName~StorefrontBrandingMarkupTests|FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests.ProductPurchasePanel_UsesHostActionDescriptorAfterHpr6Migration" -v:minimal`
- [x] `node scripts/qa/storefront-browser-action-boundary-proof.js`

## Phase F1.55 - Generated Action Boundary

Goal: generated storefronts never receive copied browser application controllers. Generated markup declares descriptors; Presentation binders execute commands.

### Remove Generated Functional Bridge

- [ ] Delete `writeFunctionalBrowserBridge()` from `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/apply-composition.mjs`.
- [ ] Remove the call to `writeFunctionalBrowserBridge(projectRoot)`.
- [ ] Remove generated `wwwroot/js/storefront-builder.functional.js`.
- [ ] Remove any generated artifact manifest entries for `storefront-builder.functional.js`.
- [ ] Remove generated script injection into `Components/Layout/ApplicationHead.razor`.
- [ ] Update `transformApplicationHead()` so it only injects generated CSS/metadata, not script.

### Generated Markup Contract

- [ ] Update generated purchase panel output to emit Presentation binder descriptors:

```html
<aside data-storefront-product-purchase
       data-product-id="..."
       data-currency-code="..."
       data-selection-preview-route="...">
```

- [ ] Replace generated `data-storefront-generated-add-to-cart` with stable command descriptor:

```html
<button data-storefront-command="cart.add-line"
        data-storefront-product-purchase-submit>
</button>
```

- [ ] Replace `data-quantity-selector` with binder-owned quantity source descriptor:

```html
<input data-storefront-purchase-quantity>
```

- [ ] Keep generated classes and labels visual-only.
- [ ] Keep generated feedback container declarative:

```html
<p data-storefront-purchase-feedback aria-live="polite"></p>
```

### VisualScripts Slot Rules

- [ ] Generated projects should not need generated JS for cart/product purchase.
- [ ] If generated visual JS is needed for animation only, generate:

```text
Components/Layout/VisualScripts.razor
wwwroot/js/visual/storefront.visual.js
```

- [ ] Register it through `FoundationViewRegistration.VisualScripts`.
- [ ] Do not inject generated script through `ApplicationHead`.
- [ ] Generated visual JS may only listen to semantic events and apply visuals.

### Generated Zones

- [ ] Keep `allowedGeneratedZones` without `wwwroot/js` if no generated JS remains.
- [ ] If visual JS remains needed, add only:

```yaml
allowedGeneratedZones:
  - wwwroot/js/visual
```

- [ ] Validator must reject:
  - [ ] `wwwroot/js/storefront-builder.functional.js`.
  - [ ] `wwwroot/js/*.js` outside `wwwroot/js/visual`.
  - [ ] generated JS invoking application commands.
  - [ ] generated JS constructing command payloads.

### StorefrontBuilder Validation Updates

- [ ] Update `Test-StorefrontBuilderCompositionFiles.ps1`:
  - [ ] stop expecting `storefront-builder.functional.js`.
  - [ ] stop expecting command orchestration JS.
  - [ ] expect descriptor markup.
  - [ ] ensure `ApplicationHead.razor` does not contain `<script src=...functional...>`.
- [ ] Update `Test-StorefrontBuilderStaticGate.ps1`:
  - [ ] reject generated direct Runtime/Client references.
  - [ ] reject functional JS output.
  - [ ] reject generated JS outside allowed visual zone.
- [ ] Update StorefrontBuilder generation tests:
  - [ ] generated composition applies product purchase descriptors.
  - [ ] generated project has no functional browser bridge.
  - [ ] generated project relies on Presentation binder.
- [ ] Update docs/metadata:
  - [ ] remove `storefront-builder.functional.js` from expected generated artifacts.
  - [ ] document generated visual JS policy.

### Tests

- [ ] Run `dotnet test BlazorShop.Tests.V2 --filter StorefrontBuilder`.
- [ ] Run `tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderStaticGate.ps1` against generated proof.
- [ ] Run generated structure proof.
- [ ] Run generated foundation functional proof after F1.56 makes it PR-ready.
- [ ] Inspect generated artifact and verify:
  - [ ] no `wwwroot/js/storefront-builder.functional.js`.
  - [ ] no `app.cart.addLine`.
  - [ ] no generated `ProductId`/`productId` payload construction in JS.
  - [ ] no script injected into `ApplicationHead`.

### Definition of Done

- [ ] StorefrontBuilder no longer generates browser application controller JS.
- [ ] Generated markup uses Presentation action descriptors.
- [ ] Generated zones match actual output.
- [ ] Generated storefront product/add-to-cart behavior still works through Presentation binder.

## Phase F1.56 - Guardrail And Closure Hardening

Goal: make the final boundary hard to regress and fix remaining Foundation hosting/documentation defects.

### Visual Host JS Command Guardrail

- [ ] Extend `StorefrontVisualConsumerBoundaryValidator` to forbid command invocation tokens in visual host JS:
  - [ ] `.application.cart.`
  - [ ] `.application.consent.`
  - [ ] `.application.productSelection.`
  - [ ] `application.cart`
  - [ ] `application.consent`
  - [ ] `application.productSelection`
  - [ ] `cart.addLine`
  - [ ] `cart.updateLine`
  - [ ] `cart.removeLine`
  - [ ] `cart.clear`
  - [ ] `cart.recalculate`
  - [ ] `productSelection.preview`
  - [ ] `consent.accept`
  - [ ] `consent.revoke`
- [ ] Extend validator to forbid command payload construction in visual host JS:
  - [ ] `ProductId:`
  - [ ] `ProductVariantId:`
  - [ ] `SelectedAttributes:`
  - [ ] `CurrencyCode:`
  - [ ] `productId:`
  - [ ] `productVariantId:`
  - [ ] `selectedAttributes:`
  - [ ] `currencyCode:`
- [ ] Extend validator to forbid business result interpretation in visual host JS:
  - [ ] `canAddToCart`
  - [ ] `stockQuantity`
  - [ ] `isAvailable`
  - [ ] `validationMessages`
  - [ ] `unitPrice`
  - [ ] `formattedUnitPrice`
  - [ ] `formattedComparePrice`
  - [ ] `sku`
  - [ ] `gtin`
- [ ] Allow visual host JS to subscribe to semantic events:
  - [ ] `addEventListener("storefront:cart:changed", ...)`
  - [ ] `addEventListener("storefront:cart:error", ...)`
  - [ ] `addEventListener("storefront:product-selection:changed", ...)`
  - [ ] `addEventListener("storefront:product-purchase:add-line-succeeded", ...)`
  - [ ] `addEventListener("storefront:product-purchase:add-line-failed", ...)`
- [ ] Prefer AST-based JS validation if practical. If the phase uses token scan first, document it as a conservative guardrail and add negative fixtures for false-negative cases.

### Runtime/Client Reference Guardrail

- [ ] Remove `AllowRuntimeClientPackageMetadata` from `StorefrontVisualConsumerProfile`.
- [ ] Shared validator must always reject actual `PackageReference Include="BlazorShop.Storefront.Runtime"`.
- [ ] Shared validator must always reject actual `PackageReference Include="BlazorShop.Storefront.Client"`.
- [ ] Compatibility version metadata is allowed only in non-project files such as `.props` or metadata YAML.
- [ ] Update generated profile test to pass without Runtime/Client PackageReference exception.
- [ ] Update failure messages to say metadata is allowed but compile reference is not.

### Hosting Pipeline Fix

- [ ] Change Presentation middleware order:

```csharp
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
```

- [ ] Add architecture test:

```text
IndexOf("UseForwardedHeaders")
    <
IndexOf("UseHttpsRedirection")
```

- [ ] Confirm Control Plane/Commerce Node existing order is not affected by this Storefront phase.

### Shell Session Fallback

- [ ] Treat `displayTask` as critical.
- [ ] Treat shell session summary as optional for public shell rendering.
- [ ] Load session with fallback:

```csharp
var sessionTask = LoadOptionalAsync(
    "storefront shell customer session",
    () => _sessionResolver.GetCurrentUserAsync(cancellationToken),
    StorefrontSessionInfo.Anonymous);
```

- [ ] Keep account/auth/checkout route guards separate and strict.
- [ ] Log session fallback warning without exposing internal details to the user.
- [ ] Ensure authenticated-only pages still reject unauthorized users through page services/BFF guards.

### Required PR Functional Proof

- [ ] Split `FoundationFunctional` into:
  - [ ] `FoundationFunctionalFast` for PR required gate.
  - [ ] `FoundationFunctionalFull` for nightly/manual/release.
- [ ] Fast proof should use deterministic fixture or mocked local generated host data and cover:
  - [ ] product page renders.
  - [ ] product purchase descriptors exist.
  - [ ] selection preview command is invoked through same-origin route.
  - [ ] add-to-cart command is invoked through same-origin route.
  - [ ] cart badge changes.
  - [ ] cart page sees current cart.
  - [ ] checkout form/route contract exists.
  - [ ] consent current/save/revoke works.
  - [ ] no browser request goes directly to Commerce Node.
- [ ] Full proof remains manual/scheduled/release and can include:
  - [ ] real COD order placement.
  - [ ] email/message checks.
  - [ ] payment sandbox.
  - [ ] live fixture store.
  - [ ] longer browser visual regression.
- [ ] Update `.github/workflows/storefront-builder.yml` to run fast functional proof on PR.
- [ ] Update `.github/workflows/ci.yml` to run fast functional proof or a clearly equivalent required status check.
- [ ] Keep full proof under `workflow_dispatch` and `schedule`.

### Starter Theme Terminology Cleanup

- [ ] Rename Starter namespaces from:

```text
BlazorShop.Storefront.Starter.Theme.Pages.*
```

to one of:

```text
BlazorShop.Storefront.Starter.Pages.*
BlazorShop.Storefront.Starter.Views.Pages.*
```

- [ ] Update `StarterFoundationViewRegistration.cs` usings.
- [ ] Update Razor `@namespace` directives.
- [ ] Update StorefrontBuilder template/generation assumptions if they depend on the old namespace.
- [ ] Add guardrail:

```text
Starter source must not contain ".Theme.Pages" or "Theme/Pages".
```

### Remove `CreateMinimal()` Production Escape Hatch

- [ ] Replace public `StorefrontFoundationViewSet.CreateMinimal(Type)` with explicit registration only.
- [ ] If tests need it, use one of:
  - [ ] internal `CreateTestFixture(...)` plus `InternalsVisibleTo`.
  - [ ] test helper inside `BlazorShop.Tests.V2`.
  - [ ] explicit fixture view set builder.
- [ ] Remove obsolete `ApplicationScripts` compatibility alias if generated templates no longer use it.
- [ ] Add architecture test:
  - [ ] production source cannot call `CreateMinimal`.
  - [ ] `CreateMinimal` is not public.
  - [ ] `ApplicationScripts` alias is removed or marked with a clear removal gate.

### Documentation Closure

- [ ] Set `Storefront Foundation Blocker Closure.todo.md` status to `Reopened` or equivalent until F1.54-F1.56 complete.
- [ ] Move stale "Verified current context" in `Storefront Visual Only Phase 1 Boundary.todo.md` under a historical heading, for example:

```text
Historical baseline before F1.26
```

- [ ] Ensure original Phase 1 doc does not claim complete while this plan remains open.
- [ ] Update architecture docs:
  - [ ] browser action binder ownership.
  - [ ] visual host JS event-only rule.
  - [ ] generated storefront JS zone rule.
  - [ ] Runtime/Client direct reference rule.
  - [ ] required fast functional proof.
- [ ] Update QA checklists:
  - [ ] `QA-StorefrontV2.todo.md`.
  - [ ] `QA-StorefrontStarter.todo.md`.
  - [ ] `Storefront Playwright E2E Release.todo.md` if release browser cases change.
- [ ] Update old tests that currently assert the obsolete V2 command invocation behavior.

### Tests

- [ ] `dotnet test BlazorShop.Tests.V2 --filter StorefrontVisualConsumerBoundaryValidatorTests`
- [ ] `dotnet test BlazorShop.Tests.V2 --filter StorefrontCommerceScriptRegressionTests`
- [ ] `dotnet test BlazorShop.Tests.V2 --filter StorefrontApplicationBootstrapTests`
- [ ] `dotnet test BlazorShop.Tests.V2 --filter StorefrontBuilder`
- [ ] generated structure proof.
- [ ] generated fast foundation functional proof.
- [ ] V2 Playwright product/add-to-cart/cart browser flow.
- [ ] Starter/generated browser flow for product descriptors/add-to-cart.

### Definition of Done

- [ ] Shared validator fails if V2 JS calls application commands.
- [ ] Shared validator fails if generated JS calls application commands.
- [ ] Shared validator always rejects direct Runtime/Client compile references.
- [ ] Forwarded headers run before HTTPS redirect.
- [ ] Public shell falls back to anonymous session summary when session lookup fails.
- [ ] Fast generated browser functional proof runs on PR.
- [ ] Full generated browser proof remains available for manual/scheduled/release.
- [ ] Starter no longer uses `Theme.Pages` terminology.
- [ ] `CreateMinimal()` is not a public production escape hatch.
- [ ] Closure docs status is honest and linked to this plan.

## Execution Order

1. F1.54 first: move browser action orchestration from V2 to Presentation.
2. F1.55 second: remove copied generated functional JS and switch generated markup to descriptors.
3. F1.56 last: harden guardrails, CI, middleware, session fallback, terminology, and docs.

Do not implement F1.56 command-invocation guardrails before F1.54/F1.55 unless the current violations are explicitly allowlisted with owner and removal condition. Strict guardrails first would either fail immediately or encourage broad allowlists that hide the boundary problem.

## QA Matrix

| Flow | V2 | Starter | Generated |
| --- | --- | --- | --- |
| Home render | Required | Required | Required |
| Product render | Required | Required | Required |
| Product descriptors present | Required | Required | Required |
| Selection preview through Presentation binder | Required | If product page supports descriptor | Required |
| Add to cart through Presentation binder | Required | Required when purchasable fixture exists | Required |
| Cart badge event update | Required | Required | Required |
| Cart page reflects item | Required | Required | Required |
| Checkout entry route exists | Required | Required | Required |
| Consent current/save/revoke | Required | Explicit required or disabled state | Required |
| Direct Commerce Node browser call rejected/not present | Required | Required | Required |
| Visual JS contains no command invocation | Required | Required | Required |
| Generated functional JS absent | N/A | N/A | Required |

## Failure Modes Registry

| Failure Mode | Severity | Detection | Required Fix |
| --- | --- | --- | --- |
| V2 JS still calls `cart.addLine` | P0 | Source guardrail and Playwright | Move call into Presentation binder. |
| Generated script builds payload | P0 | Static gate and generated proof | Delete generated functional bridge. |
| Binder cannot find descriptors | P0 | Playwright product/add-to-cart | Align markup descriptor contract. |
| Visual listener stops updating UI | P1 | Playwright visual assertions | Keep semantic event detail stable and update listener. |
| Runtime/Client package reference reappears | P1 | Shared validator | Remove direct reference; keep only metadata. |
| PR skips functional proof | P1 | Workflow tests | Add fast functional proof status check. |
| Session lookup outage kills public home | P1 | Unit test with failing session resolver | Use anonymous fallback for shell only. |
| `CreateMinimal` used in production registration | P2 | Architecture test | Replace with explicit slot registration. |

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | Scope | Create a new F1.54-F1.56 closure plan | Auto-decided | Clarity | The existing Foundation closure file covers F1.45-F1.53 and should not silently absorb a new P0 browser action boundary. | Edit the old file only. |
| 2 | Architecture | Presentation owns browser action binders, visual hosts own event presentation | Auto-decided | Boundary safety | Transport-only migration still leaves application controller logic in V2/generated JS. | Let V2/generated call `application.cart.*` directly. |
| 3 | Generator | Delete generated functional JS instead of moving it to VisualScripts | Auto-decided | Simpler over clever | Generated stores should not need copied cart controller code once Presentation binders exist. | Keep generated functional JS as visual script. |
| 4 | QA | Add fast functional generated proof to PR gates | Auto-decided | Production readiness | Structure proof cannot catch missing browser command behavior. | Keep functional proof manual/nightly only. |
| 5 | Guardrail | Always reject direct Runtime/Client compile references in visual consumers | Auto-decided | Contract ownership | Metadata can pin versions without granting compile-time access to transport packages. | Keep `AllowRuntimeClientPackageMetadata` as an escape hatch. |

## GSTACK REVIEW REPORT

| Area | Status | Findings | Plan Response |
| --- | --- | --- | --- |
| CEO/Product | Pass with blocker | MVP cannot close Foundation while generated stores can ship copied cart controller JS. | F1.55 removes generated functional JS and requires descriptors. |
| Design | Pass | No redesign needed; visual behavior should remain in V2/generated event listeners. | F1.54 keeps gallery/toast/CSS behavior visual-only. |
| Engineering | Issues open | V2/generated JS still owns browser application orchestration, validator is too permissive, middleware/session/CI gaps remain. | F1.54-F1.56 split ownership and add guardrails. |
| DX | Issues open | AI-generated stores will be hard to maintain if every store copies command logic. | Generated stores declare descriptors and rely on Presentation binders. |

VERDICT: Approved as the final browser action boundary closure plan. Storefront Foundation Phase 1 should remain open/reopened until this file is complete.

NO UNRESOLVED DECISIONS
