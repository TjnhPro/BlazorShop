# Storefront Shared Semantic Render Extraction

Status: planned
Track: Phase 3.4 - V2 Shared Semantic Render Extraction
Owner boundary: Storefront Components / Storefront Components.Primitives / Storefront Components.Ssr / Storefront Presentation / Storefront V2 / Storefront V2.WASM
Primary goal: extract the final approved reusable semantic rendering from Storefront V2 while preserving the current V2 markup contract, browser behavior, routes, final Tailwind styling, copy, and runtime boundaries.

## Decision

Phase 3.4 extracts exactly these three render surfaces:

- [ ] `StorefrontProductPurchasePanel` semantic rendering into `BlazorShop.Storefront.Components.Primitives/Product`.
- [ ] `StorefrontConsentPanel` semantic rendering into `BlazorShop.Storefront.Components.Ssr/Security`.
- [ ] `StorefrontToastRegion` semantic rendering into `BlazorShop.Storefront.Components.Primitives/System`.

Target ownership:

```text
BlazorShop.Storefront.Components
├── Contracts/Product
│   ├── ProductPurchaseLabels.cs              # extend existing contract
│   └── ProductPurchasePanelClasses.cs         # new neutral class slots
└── Contracts/System
    ├── StorefrontToastRegionClasses.cs        # new neutral class slots
    └── StorefrontToastRegionLabels.cs         # new semantic labels

BlazorShop.Storefront.Components.Primitives
├── Product
│   └── StorefrontProductPurchasePanel.razor   # render-only purchase semantics
└── System
    └── StorefrontToastRegion.razor            # region + template, no extra wrapper

BlazorShop.Storefront.Components.Ssr
└── Security
    ├── StorefrontConsentPanel.razor
    ├── StorefrontConsentPanelClasses.cs
    └── StorefrontConsentPanelLabels.cs

BlazorShop.Storefront.V2
├── Components/Product
│   └── ProductPurchasePanelVisuals.cs          # final V2 classes and copy
├── Components/Security
│   ├── StorefrontConsentBanner.razor           # thin registered V2 wrapper
│   └── StorefrontConsentVisuals.cs             # final V2 classes and copy
├── Components/Layout
│   ├── MainLayout.razor                        # owns toast placement
│   └── StorefrontToastVisuals.cs               # final V2 classes and copy
└── wwwroot/js/storefrontCommerce.js            # V2 visual reactions and toast behavior

BlazorShop.Storefront.Presentation
└── wwwroot/js/storefront.application.js        # purchase command transport and consent behavior
```

The final dependency direction must remain:

```text
Components.Contracts
        ^
        |
Components.Primitives        Components.Ssr
        ^                         ^
        |                         |
        +------------ V2 --------+

Presentation JS owns same-origin commands and consent state.
V2 JS owns V2-specific visual reactions, toast presentation, and gallery feedback.
```

Namespace adoption also requires explicit `_Imports.razor` review:

- [ ] Components.Primitives imports the new Product/System contracts used by its Razor files.
- [ ] Components.Ssr imports the Presentation consent context namespace used by its Security Razor file.
- [ ] V2 imports `Components.Contracts.System`, `Components.Primitives.System`, and `Components.Ssr.Security` where needed.
- [ ] The shared purchase component and deleted V2 purchase component use the same short type name only within one atomic cutover; no ambiguous active import remains after the phase.
- [ ] No project-reference change is expected because V2 already references Components.Primitives and Components.Ssr; any proposed `.csproj` change requires a dependency review first.

## Approved Ownership Rules

Shared render packages own:

- [ ] semantic HTML structure;
- [ ] accessibility attributes;
- [ ] stable `data-storefront-*` hooks;
- [ ] parameterized class slots;
- [ ] parameterized labels and accessible text;
- [ ] pure render helpers required to produce stable DOM IDs or select semantic variants;
- [ ] no transport, route construction, runtime service resolution, or final storefront design.

Storefront V2 owns:

- [ ] final Tailwind and `bs-*` class values;
- [ ] final English storefront copy;
- [ ] final component placement and page/layout composition;
- [ ] registered V2 visual wrappers;
- [ ] V2-specific toast visual reactions and gallery feedback;
- [ ] product detail page arrangement;
- [ ] visual fallback treatment for a missing color swatch;
- [ ] any render-mode placement.

Storefront Presentation owns:

- [ ] purchase command binding and same-origin BFF transport;
- [ ] product selection preview command binding;
- [ ] consent current/save/revoke calls;
- [ ] consent state application and event publication;
- [ ] no final V2 classes or final V2 copy.

## Codebase Evidence

Current purchase rendering:

- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductPurchasePanel.razor` is a 225-line V2 Razor component.
- [ ] It consumes `ProductPurchasePanelModel` and `ProductPurchaseActionDescriptor` only.
- [ ] It does not inject `HttpClient`, `IJSRuntime`, Runtime, Client, Browser, or backend services.
- [ ] It owns final Tailwind classes, fixed English labels, semantic purchase hooks, option rendering, quantity rendering, and pure DOM-ID helpers.
- [ ] `ProductPurchaseLabels` already exists in `Components/Contracts/Product`; a second purchase labels type is unnecessary.
- [ ] `ProductPurchasePanelModel.Empty` and `ProductPurchaseActionDescriptor.Empty` remain used by Starter compatibility components, but the new primitive does not need to use those defaults.
- [ ] The current color swatch helper includes the V2 visual fallback `#f5f5f5`; that literal must not move into Primitives.
- [ ] Radio/color group labels currently target a `div`; extraction is the bounded point to correct the group semantics with `fieldset`/`legend` without changing hooks or values.

Current consent rendering:

- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Security/StorefrontConsentBanner.razor` consumes prepared `StorefrontConsentContext` from Presentation.
- [ ] `V2FoundationViewRegistration` registers `StorefrontConsentBanner`; retaining a thin V2 wrapper preserves the registered visual slot.
- [ ] Consent current/save/revoke behavior is implemented in Presentation `storefront.application.js`, not in V2 `storefrontCommerce.js`.
- [ ] Consent visibility currently depends on adding/removing the literal CSS class `hidden`; this is a portability leak.
- [ ] `StarterConsentBanner.razor` is a separate Starter implementation and remains outside the V2/V2.WASM scope of this phase.

Current toast rendering:

- [ ] `MainLayout.razor` owns the current `data-storefront-toast-region` and `data-storefront-toast-template` markup.
- [ ] `storefrontCommerce.js` queries those exact selectors and clones the template for V2 visual notifications.
- [ ] The region and template are sibling nodes; extraction must not add a wrapper or nest the template into the region.
- [ ] V2 JavaScript fallback headings/messages remain V2-owned and are not moved into shared labels.
- [ ] Semantic SVG icons may remain in the primitive because they are selected by stable `data-storefront-toast-icon` hooks and do not establish a generic icon system.

Existing reusable-component precedent:

- [ ] `StorefrontProductGallery` lives in Components.Primitives and receives classes/labels from `ProductGalleryVisuals` in V2.
- [ ] Product pricing, availability, and variant list rendering live in Components.Ssr and receive final V2 classes/labels.
- [ ] Pagination lives in Components.Primitives and receives prepared items, classes, and labels.
- [ ] Breadcrumb and catalog filter rendering live in Components.Ssr and receive prepared Presentation context plus final V2 classes/labels.
- [ ] Existing `HtmlRenderer` tests prove Razor components can be tested as rendered markup without adding bUnit.

## What Already Exists And Must Be Reused

- [ ] `ProductPurchasePanelModel`.
- [ ] `ProductPurchaseOptionItem` and `ProductPurchaseOptionValueItem`.
- [ ] `ProductPurchaseVariantItem`.
- [ ] `ProductPurchaseLabels`.
- [ ] `ProductPurchaseSnapshot` and `ProductPurchaseSelectionState`.
- [ ] `ProductPurchaseActionDescriptor`.
- [ ] `StorefrontConsentContext` and its prepared action/event descriptors.
- [ ] `StorefrontFoundationViewSet` and `V2FoundationViewRegistration`.
- [ ] Presentation purchase and consent JavaScript binders.
- [ ] V2 toast JavaScript behavior and CSS state selectors.
- [ ] Components.Primitives and Components.Ssr dependency guardrails.
- [ ] visual-neutrality source scanning.
- [ ] existing `HtmlRenderer` component test patterns.
- [ ] `scripts/qa/storefront-application-js-split-proof.js`.
- [ ] `scripts/run-v2-local.ps1` for browser QA.

## Explicit Non-Goals

Do not change:

- [ ] cart, checkout, account, payment, shipping, order, or backend commerce behavior;
- [ ] Commerce Node API routes or contracts;
- [ ] Storefront Runtime or generated Storefront Client;
- [ ] Storefront Browser BFF routes;
- [ ] product selection-preview request/response contracts;
- [ ] product sellability, pricing, inventory, variant, or quantity business rules;
- [ ] consent persistence, retention, categories, antiforgery, or API behavior;
- [ ] V2 toast timing, levels, animation state names, event names, or message source;
- [ ] product page layout or responsive design;
- [ ] V2 header, footer, navigation, gallery, pricing, availability, or variant list;
- [ ] database schema or migrations;
- [ ] Control Plane;
- [ ] StorefrontBuilder generation templates;
- [ ] Starter implementation or Starter QA in this phase;
- [ ] generated `Storefront.{Name}` projects;
- [ ] a new component package;
- [ ] a generic form, notification, icon, design-system, or localization framework.

Do not claim:

- [ ] repository-wide consent deduplication while Starter retains `StarterConsentBanner`;
- [ ] that V2 owns purchase command or consent transport JavaScript;
- [ ] that all shared rendering has been extracted until the post-implementation audit confirms it;
- [ ] that this phase redesigns or improves the V2 visual language.

## Compatibility Invariants

Purchase invariants:

- [ ] Preserve `data-storefront-product-purchase`.
- [ ] Preserve `data-storefront-product-purchase-panel`.
- [ ] Preserve selection preview route, product, currency, resolved variant, image, SKU, and GTIN data attributes.
- [ ] Preserve `data-storefront-attribute-group`.
- [ ] Preserve `data-storefront-purchase-attribute` and attribute-name hooks.
- [ ] Preserve radio input names, values, checked state, and deterministic IDs.
- [ ] Preserve variant selector hook and option metadata.
- [ ] Preserve min/max/step/value quantity attributes.
- [ ] Preserve `data-storefront-command="cart.add-line"`.
- [ ] Preserve submit, default-label, success-label, resolved-variant, selector, disabled, and feedback-target attributes.
- [ ] Preserve feedback element ID, `aria-live`, level, and initial message.
- [ ] Preserve the cart link URL supplied by the model.

Consent invariants:

- [ ] Preserve every `data-storefront-consent-*` hook currently consumed by Presentation JavaScript.
- [ ] Preserve current/accept/revoke route and HTTP method attributes from `StorefrontConsentContext`.
- [ ] Preserve changed/manage event attributes.
- [ ] Preserve policy link, preferences, analytics, marketing, essential, selected, all, and revoke controls.
- [ ] Preserve initial no-flash hidden behavior using native `hidden`, not a theme class.
- [ ] Preserve the registered V2 `ConsentBanner` component type.

Toast invariants:

- [ ] Preserve exactly one `data-storefront-toast-region` in the composed V2 layout.
- [ ] Preserve exactly one `data-storefront-toast-template`.
- [ ] Keep region and template as siblings and in the same relative order.
- [ ] Preserve `aria-live="polite"` and `aria-atomic="true"`.
- [ ] Preserve toast root, level, state, accent, icon, heading, message, and close hooks.
- [ ] Preserve all four icon hook values: `info`, `success`, `warning`, and `error`.
- [ ] Preserve V2 CSS selectors and V2 JavaScript selectors without compatibility aliases.

## Autoplan Review Summary

### CEO Review

- [ ] Premise accepted: the phase removes duplicated host-owned semantic markup without expanding ecommerce features.
- [ ] Scope is intentionally limited to three already-approved candidates.
- [ ] User value is indirect but concrete: future V2/Starter/generated storefront composition can reuse behavior contracts without copying transport or business logic.
- [ ] No backend rewrite, framework introduction, or speculative generic system is justified.
- [ ] Dream-state delta: shared packages own stable semantic render contracts; each store owns final presentation and copy.

CEO score: 9/10. Remaining risk is closure language that could overstate Starter deduplication.

### Design Review

- [ ] This is a behavior-preserving extraction, not a redesign.
- [ ] Current DOM order, responsive classes, labels, controls, and feedback placement remain visually equivalent after V2 supplies them through visual configuration.
- [ ] No extra card, wrapper, spacing, palette, typography, animation, or icon-system changes are allowed.
- [ ] Accessibility correction is limited to valid grouping semantics for product radio/color options.
- [ ] Desktop and mobile screenshots must prove no visual regression.

Design score: 9/10. The only intentional markup change is accessibility structure with stable external hooks.

### Engineering Review

- [ ] Dependency direction matches established Primitives and Ssr projects.
- [ ] Existing contracts and prepared contexts are sufficient; no new service interface is needed.
- [ ] Native hidden state removes consent behavior coupling to Tailwind.
- [ ] `HtmlRenderer` tests provide compile-time and rendered-markup coverage.
- [ ] Existing source ownership tests must be migrated rather than deleted.
- [ ] V2.WASM build is mandatory because shared contract changes flow transitively into its component host.

Engineering score: 9/10. Highest regression risk is stale source-string tests that continue reading deleted V2 markup.

### Developer Experience Review

- [ ] File placement follows existing capability folders and naming.
- [ ] V2 visual configuration uses the same `*Visuals` convention as gallery and product detail displays.
- [ ] Each phase names concrete files, consumers, tests, and exit criteria.
- [ ] Ownership documentation must explain the Presentation/V2 JavaScript split so future agents do not move transport behavior into visual packages.
- [ ] Deferred Starter adoption must be explicit and searchable.

DX score: 9/10. The plan avoids introducing a second labels type or an ambiguous generic component registry.

### Cross-Phase Themes

- [ ] Preserve behavior before deleting old markup.
- [ ] Keep final copy and class values in V2.
- [ ] Use rendered tests in addition to source guardrails.
- [ ] State deferred scope truthfully.
- [ ] Close only after browser evidence and a fresh ownership audit.

## Failure Modes Registry

| Failure mode | Prevention | Detection | Recovery |
| --- | --- | --- | --- |
| Primitive contains literal Tailwind classes | Require all classes as parameters/contracts | Visual-neutrality tests | Move value to V2 `*Visuals` configuration |
| Primitive contains English copy | Require labels | Copy scan and rendered tests | Move copy to V2 labels |
| Purchase hooks drift | Characterize exact attributes before move | Rendered semantic tests and JS proof | Restore exact hook/value contract |
| Purchase group semantics break JS | Preserve input names, IDs, values, and hooks | Rendered tests plus variant browser flow | Revert only grouping implementation, not extraction |
| Invalid color becomes inline CSS | Validate dynamic color and avoid shared fallback literal | Unit/render tests for valid/missing color | Render no inline color and use V2 fallback class |
| Consent stays permanently hidden | Replace class coupling with native hidden property | JS tests and browser consent flow | Correct Presentation state application |
| Consent flashes before JS loads | Render native `hidden` initially | Browser reload/video/screenshot check | Restore initial hidden attribute |
| V2 registration breaks | Keep thin `StorefrontConsentBanner` wrapper | architecture and host smoke tests | Restore wrapper registration |
| Starter is accidentally changed | Scope and path guard | git diff audit | Revert only unrelated Starter edits |
| Toast template is nested or wrapped | Specify sibling no-wrapper contract | rendered DOM test | Restore exact fragment order |
| Toast selector count becomes two | Layout composition test | source/render count and browser toast | Remove duplicate host markup |
| V2 CSS/JS no longer finds hooks | Preserve selectors | JS split proof and browser flows | Restore semantic hook contract |
| Tailwind drops classes moved from Razor into `*Visuals.cs` | Keep V2 Tailwind `.cs` content scanning and rebuild generated CSS | Tailwind build plus browser screenshots | Correct content scan or class literals, regenerate `site.css`, never hand-edit generated CSS |
| Source tests pass while rendered markup is wrong | Add HtmlRenderer coverage | rendered assertions | Fix Razor output before adoption |
| Old V2 component remains active | Consumer and duplicate audits | `rg` and architecture tests | remove old active implementation after cutover |
| Deleted source test silently removes coverage | Map every old assertion to new owner | test migration checklist | restore equivalent assertion at correct layer |
| Shared model default leaks final copy | Require primitive parameters | source guard and rendered empty/default tests | remove primitive use of `Empty` defaults |

## Error And Rescue Registry

| Condition | Required signal | Required action |
| --- | --- | --- |
| Required purchase model/actions/classes/labels missing | Razor compile/editor-required warning or explicit test failure | Supply all parameters from V2; do not invent shared final defaults |
| Required consent context/classes/labels missing | Razor compile/editor-required warning or explicit test failure | Supply all parameters from thin V2 wrapper |
| Required toast classes/labels missing | Razor compile/editor-required warning or explicit test failure | Supply all parameters from MainLayout |
| Invalid/missing color value | No unsafe inline style; semantic fallback state remains renderable | Use host class/CSS fallback without a shared color literal |
| Consent API fails | Existing Presentation error/event behavior remains authoritative | Do not add transport handling to SSR component |
| Toast event has no heading/message | Existing V2 JS fallback remains authoritative | Do not add fallback copy to primitive |
| Browser QA cannot produce info/warning toast through a real user flow | Record JS proof for all levels and browser proof for reachable success/error flows | Do not add production-only QA endpoints or hooks |

## Phase 3.4.0 - Baseline And Characterization Lock

Goal: record every active consumer, semantic hook, ownership test, and browser behavior before moving markup.

Required reading:

- [x] Read `AGENTS.md`.
- [x] Read `docs/architecture/README.md`.
- [x] Read `docs/architecture/05-project-and-folder-guide.md`.
- [x] Read `docs/architecture/10-v2-contract-ownership.md`.
- [x] Read `BlazorShop.PresentationV2/COMPONENT-MODES.md`.
- [x] Read Components.Primitives and Components.Ssr READMEs.
- [x] Read `Storefront Product Detail Display Components.todo.md`.
- [x] Read `Storefront Catalog Navigation Controls.todo.md`.
- [x] Read the relevant current entries in `QA-StorefrontV2.todo.md`.

Inventory tasks:

- [x] Record `git status --short` and preserve unrelated changes.
- [x] Record all `StorefrontProductPurchasePanel` consumers.
- [x] Record all `StorefrontConsentBanner` registrations and consumers.
- [x] Record all toast region/template sources and JavaScript selectors.
- [x] Record Starter consent duplication as deferred scope.
- [x] Record every `data-storefront-product-purchase*` attribute.
- [x] Record every consent hook and action/method/event attribute.
- [x] Record every toast hook, icon value, and DOM relationship.
- [x] Record product labels, final V2 class values, and color fallback behavior.
- [x] Record current accessibility structure for option groups.
- [x] Record Presentation-owned purchase and consent JavaScript functions.
- [x] Record V2-owned toast and visual feedback JavaScript functions.
- [x] Record CSS selectors for toast levels and purchase feedback levels.

Characterization tests to add or strengthen before deletion:

- [x] Assert current purchase attributes, values, disabled state, quantity bounds, and feedback target.
- [x] Assert current consent action URLs/methods/events and all controls.
- [x] Assert current toast sibling structure, selector counts, and all four icons.
- [x] Assert `storefront.application.js` owns purchase and consent command behavior.
- [x] Assert `storefrontCommerce.js` does not invoke Presentation application commands.
- [x] Assert V2 registration still resolves the consent wrapper.

Baseline commands:

```powershell
rg -n "StorefrontProductPurchasePanel|StorefrontConsentBanner|StarterConsentBanner|data-storefront-toast" `
  BlazorShop.PresentationV2 BlazorShop.Tests.V2 docs

dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj `
  --filter "FullyQualifiedName~StorefrontBrandingMarkupTests|FullyQualifiedName~SecurityPrivacyPhase3ConsentTests|FullyQualifiedName~LayoutAssetFoundationTests|FullyQualifiedName~StorefrontVisualSourceOwnershipTests|FullyQualifiedName~StorefrontCommerceScriptRegressionTests"
```

Exit criteria:

- [x] all active consumers and selectors are listed;
- [x] current focused tests pass before implementation;
- [x] no implementation source has moved;
- [x] any pre-existing failure is recorded separately and not hidden by this phase.

Baseline evidence (2026-08-11): `git status --short` contained only this newly supplied untracked plan. The consumer/selector scan recorded the V2 product page as the active purchase consumer, `V2FoundationViewRegistration` as the V2 consent wrapper registration, `MainLayout` as the sole inline toast source, Presentation `storefront.application.js` as the purchase/consent binder, and V2 `storefrontCommerce.js` as the toast/visual binder. `StarterConsentBanner` and its registration remain a separately deferred Starter surface. The focused baseline command passed `55/55`; existing MessagePack advisory and Browserslist warnings were unchanged.

## Phase 3.4.1 - Define Neutral Render Contracts

Goal: define the smallest contracts needed to remove final classes and copy from shared markup.

### Product Purchase Contracts

Files:

- [x] Extend `BlazorShop.Storefront.Components/Contracts/Product/ProductPurchaseLabels.cs`.
- [x] Add `BlazorShop.Storefront.Components/Contracts/Product/ProductPurchasePanelClasses.cs`.
- [x] Do not add `ProductPurchasePanelLabels.cs`.

`ProductPurchaseLabels` requirements:

- [ ] Preserve existing `AddToCart`, `AddedToCart`, `ViewCart`, `FreeShipping`, and `Optional` fields.
- [ ] Add a purchase section heading label.
- [ ] Add choose-variant and select-variant labels.
- [ ] Add a quantity label.
- [ ] Add a host-controlled select-option format or equivalent formatter input for `Select {option name}`.
- [ ] Keep `Empty` neutral; it must contain empty strings only.
- [ ] Do not add V2 routes, endpoint names, store-specific wording, or localization services.
- [ ] Review constructor compatibility and update all direct construction sites atomically.

`ProductPurchasePanelClasses` requirements:

- [ ] Cover root, heading, message, blocked message, delivery metadata, badges, option groups, labels, choices, selects, quantity, actions, buttons, cart link, feedback, and color swatch states.
- [ ] Provide separate enabled and disabled add-button slots.
- [ ] Provide separate valid-color and missing-color swatch slots if the rendered state needs different treatment.
- [ ] Keep values neutral/empty in the shared contract.
- [ ] Do not encode Tailwind, `bs-*`, palette, spacing, radius, breakpoint, or layout values.

### Toast Contracts

Files:

- [x] Add `BlazorShop.Storefront.Components/Contracts/System/StorefrontToastRegionClasses.cs`.
- [x] Add `BlazorShop.Storefront.Components/Contracts/System/StorefrontToastRegionLabels.cs`.

Requirements:

- [ ] Classes cover region, toast root, content, accent, icon, text, heading, message, close button, and close icon.
- [ ] Labels contain close-button accessible text and optional region accessible text only if the current rendered contract adopts it.
- [ ] Do not move V2 runtime fallback headings/messages into the labels contract.
- [ ] Do not add duration, animation timing, event names, or JavaScript configuration to the render contract.

### Consent Contracts

Files:

- [x] Add `BlazorShop.Storefront.Components.Ssr/Security/StorefrontConsentPanelClasses.cs`.
- [x] Add `BlazorShop.Storefront.Components.Ssr/Security/StorefrontConsentPanelLabels.cs`.

Requirements:

- [ ] Classes cover root, inner layout, description, heading, body, policy link, choices, choice label/input, actions, secondary button, and primary button.
- [ ] Labels cover aria label, heading, description, policy link, Preferences, Analytics, Marketing, Essential only, Revoke, Save choices, and Accept all.
- [ ] Keep these Ssr-local, matching current catalog filter and breadcrumb precedent.
- [ ] Do not add API routes or HTTP methods; those remain in prepared `StorefrontConsentContext`.
- [ ] Do not add JavaScript service abstractions.

Contract tests:

- [x] Base Components contracts contain no Presentation, Runtime, Client, Browser, V2, V2.WASM, Starter, backend, or API references.
- [x] New contracts contain no literal Tailwind classes or final V2 copy.
- [x] Ssr consent contracts contain no Browser, Runtime, Client, V2, Starter, backend, or API references.
- [x] Public contract shape is covered by focused source or reflection tests where repository convention requires it.

Exit criteria:

- [x] Components builds;
- [x] Components.Ssr builds;
- [x] contracts are sufficient for the current markup without an untyped attribute dictionary;
- [x] no generic design-system or localization abstraction was introduced.

Phase 3.4.1 evidence (2026-08-11): Components and Components.Ssr built successfully. `StorefrontSharedSemanticRenderContractTests` locks the extended purchase label surface, neutral `Empty` values, and class-slot contracts without V2 visual values. `Contracts/System` required the existing descriptor validator to use `global::System.Text.RegularExpressions` so the new contract namespace cannot shadow the framework `System` namespace.

## Phase 3.4.2 - Extract Product Purchase Primitive

Goal: move purchase semantic rendering and pure rendering behavior into Components.Primitives while V2 retains presentation configuration.

Files:

- [ ] Add `BlazorShop.Storefront.Components.Primitives/Product/StorefrontProductPurchasePanel.razor`.
- [ ] Add `BlazorShop.Storefront.V2/Components/Product/ProductPurchasePanelVisuals.cs`.
- [ ] Update `BlazorShop.Storefront.V2/Pages/Product/V2ProductPageView.razor`.
- [ ] Update Components.Primitives and V2 `_Imports.razor` files for the new contract/component namespaces.
- [ ] Remove `BlazorShop.Storefront.V2/Components/Product/StorefrontProductPurchasePanel.razor` after cutover and test migration.

Primitive inputs:

- [ ] `[Parameter, EditorRequired] ProductPurchasePanelModel Model`.
- [ ] `[Parameter, EditorRequired] ProductPurchaseActionDescriptor Actions`.
- [ ] `[Parameter, EditorRequired] ProductPurchaseLabels Labels`.
- [ ] `[Parameter, EditorRequired] ProductPurchasePanelClasses Classes`.
- [ ] Do not use `ProductPurchasePanelModel.Empty` or `ProductPurchaseActionDescriptor.Empty` as active primitive defaults.
- [ ] Use `default!` only to satisfy Razor initialization for required parameters.

Rendering tasks:

- [ ] Move current semantic structure without changing the order of purchase message, blocking message, delivery metadata, variation controls, quantity, actions, and feedback.
- [ ] Replace every fixed English label with `Labels`.
- [ ] Replace every literal class with a class slot.
- [ ] Preserve all hooks and action attributes listed in Compatibility Invariants.
- [ ] Preserve pure stable DOM-ID sanitization behavior.
- [ ] Preserve filtering of empty variation values.
- [ ] Preserve radio, color, select, and legacy variant selector behavior.
- [ ] Preserve initial selected/default values.
- [ ] Preserve server-computed disabled state.
- [ ] Preserve cart URL exactly as supplied by `Model`.

Accessibility correction:

- [ ] Render each radio/color option group with `fieldset` and `legend`, or an equivalent valid accessible grouping.
- [ ] Do not point `<label for>` at a non-labelable `div`.
- [ ] Keep input IDs, names, values, checked state, and `data-storefront-*` attributes unchanged.
- [ ] Keep select and quantity labels associated with their inputs.
- [ ] Keep feedback `aria-live="polite"`.

Color handling:

- [ ] Treat `ColorHex` as untrusted display data.
- [ ] Reuse an existing color validation/normalization helper if one exists.
- [ ] If no helper exists, add the smallest pure validator at the component-contract/headless layer only when required by current data shape.
- [ ] Emit inline `background-color` only for an accepted CSS hex value.
- [ ] Do not move `#f5f5f5` or another final fallback color into Primitives.
- [ ] Render a semantic missing-color class/state supplied by V2 when no valid color exists.

V2 adoption:

- [ ] `ProductPurchasePanelVisuals` supplies all current classes and labels.
- [ ] V2 values preserve current visual output exactly, except the bounded option-group accessibility markup.
- [ ] `V2ProductPageView` composes the shared primitive with model, actions, labels, and classes.
- [ ] Do not add a V2 wrapper unless compilation or a registered visual slot requires it; current purchase usage does not require one.
- [ ] Add the shared primitive and remove the same-named V2 component in one compiling cutover so Razor component resolution is never left ambiguous.

Rendered component tests:

- [ ] Add `StorefrontProductPurchasePanelPrimitiveTests` using `HtmlRenderer`.
- [ ] Render a simple purchasable product.
- [ ] Render a blocked product with a purchase-block message and disabled button.
- [ ] Render variation-template radio options.
- [ ] Render color options with valid and missing color values.
- [ ] Render optional select options and formatted placeholder copy.
- [ ] Render legacy variant selector options.
- [ ] Assert quantity min/max/step/value.
- [ ] Assert all semantic hooks and feedback-target linkage.
- [ ] Assert host labels/classes appear.
- [ ] Assert no hardcoded copy or literal visual class appears in primitive source.
- [ ] Assert missing required parameters are not silently replaced with final-copy defaults.

Exit criteria:

- [ ] Components.Primitives builds;
- [ ] V2 and V2.WASM build;
- [ ] V2 product page uses the shared primitive;
- [ ] old V2 purchase implementation is removed;
- [ ] purchase command and selection-preview behavior are unchanged.

Phase 3.4.2 evidence (2026-08-11): the V2 page now composes the Primitives purchase panel with `ProductPurchasePanelVisuals`; the former V2 component was deleted. The primitive preserves purchase descriptors and pure IDs, uses `fieldset`/`legend` for radio/color groups, validates hex swatches before producing inline color, and defers missing-color presentation to a V2 class slot. Components.Primitives, V2, and V2.WASM builds passed; `StorefrontProductPurchasePanelPrimitiveTests` passed `3/3`.

## Phase 3.4.3 - Extract Consent SSR Rendering And Remove Hidden-Class Coupling

Goal: move consent semantic rendering to Components.Ssr while keeping V2 registration, final visuals, and Presentation-owned consent behavior.

Files:

- [ ] Add `BlazorShop.Storefront.Components.Ssr/Security/StorefrontConsentPanel.razor`.
- [ ] Add `BlazorShop.Storefront.V2/Components/Security/StorefrontConsentVisuals.cs`.
- [ ] Convert `BlazorShop.Storefront.V2/Components/Security/StorefrontConsentBanner.razor` into a thin wrapper.
- [ ] Update Components.Ssr and V2 `_Imports.razor` files for the new Security component namespace.
- [ ] Update `BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js` only for native hidden-state handling.
- [ ] Update consent-focused JavaScript/source tests.

Shared SSR component inputs:

- [ ] `[Parameter, EditorRequired] StorefrontConsentContext Context`.
- [ ] `[Parameter, EditorRequired] StorefrontConsentPanelLabels Labels`.
- [ ] `[Parameter, EditorRequired] StorefrontConsentPanelClasses Classes`.
- [ ] Do not inject services.
- [ ] Do not call consent APIs.
- [ ] Do not contain V2 route literals or fixed English copy.

Rendering tasks:

- [ ] Preserve section semantics and every current consent hook.
- [ ] Preserve prepared action URLs, methods, and event names from `Context`.
- [ ] Preserve policy URL from `Context.PolicyPagePath`.
- [ ] Preserve preferences, analytics, and marketing checkboxes.
- [ ] Preserve essential-only, revoke, save-selected, and accept-all buttons.
- [ ] Render native `hidden` initially to prevent content flash before consent state loads.
- [ ] Remove literal `hidden` from the V2 root class value.

Presentation JavaScript tasks:

- [ ] Replace consent-only `classList.add("hidden")` and `classList.remove("hidden")` visibility operations with the native `banner.hidden` property or equivalent attribute operation.
- [ ] Do not change consent API routes, methods, payloads, visitor identity, antiforgery behavior, events, or error semantics.
- [ ] Do not move consent behavior into V2 JavaScript.
- [ ] Confirm no other unrelated use of class `hidden` is changed.

V2 wrapper tasks:

- [ ] Keep the public V2 component name `StorefrontConsentBanner`.
- [ ] Keep `V2FoundationViewRegistration` mapped to `StorefrontConsentBanner`.
- [ ] Wrapper only supplies Context, V2 labels, and V2 classes to `StorefrontConsentPanel`.
- [ ] Wrapper contains no duplicated semantic banner markup.

Rendered tests:

- [ ] Add `StorefrontConsentPanelSsrTests` using `HtmlRenderer`.
- [ ] Assert the initial native `hidden` state.
- [ ] Assert all action/method/event attributes are rendered from context.
- [ ] Assert all category controls and command hooks.
- [ ] Assert policy link and accessible label.
- [ ] Assert host labels/classes appear.
- [ ] Assert no final copy, V2 classes, `HttpClient`, `IJSRuntime`, or `@rendermode` in the Ssr source.

JavaScript tests:

- [ ] Assert consent binder uses native hidden state.
- [ ] Assert no consent visibility code depends on Tailwind class `hidden`.
- [ ] Assert current/save/revoke calls and changed/manage events remain present.
- [ ] Assert V2 script still does not call Presentation consent commands.

Starter scope:

- [ ] Do not edit `StarterConsentBanner.razor`.
- [ ] Do not edit Starter registration or Starter smoke tests.
- [ ] Add a clear deferred note in closure evidence: Starter remains a separate consumer candidate.
- [ ] Scope duplicate-implementation assertions to V2, not the entire repository.

Exit criteria:

- [ ] Components.Ssr builds;
- [ ] V2 and V2.WASM build;
- [ ] V2 registered consent slot remains functional;
- [ ] V2 wrapper contains no duplicated semantic banner implementation;
- [ ] consent no-flash and change/revoke behavior pass browser QA later;
- [ ] Starter remains unchanged.

## Phase 3.4.4 - Extract Toast Region Primitive

Goal: move the stable toast region/template markup into a render-only primitive while MainLayout remains owner of placement and V2 remains owner of behavior and visuals.

Files:

- [ ] Add `BlazorShop.Storefront.Components.Primitives/System/StorefrontToastRegion.razor`.
- [ ] Add `BlazorShop.Storefront.V2/Components/Layout/StorefrontToastVisuals.cs`.
- [ ] Update `BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor`.
- [ ] Update Components.Primitives and V2 `_Imports.razor` files for the new System contracts/component namespace.
- [ ] Do not change `storefrontCommerce.js` unless a test proves a selector compatibility issue; selector changes are not planned.

Primitive inputs:

- [ ] `[Parameter, EditorRequired] StorefrontToastRegionClasses Classes`.
- [ ] `[Parameter, EditorRequired] StorefrontToastRegionLabels Labels`.
- [ ] No services, routes, events, durations, transport clients, or JS interop parameters.

Rendering tasks:

- [ ] Render the toast region and toast template as sibling top-level fragments.
- [ ] Do not add a containing element.
- [ ] Keep region before template.
- [ ] Preserve `aria-live="polite"` and `aria-atomic="true"`.
- [ ] Preserve all toast hooks and initial data values.
- [ ] Preserve the four semantic SVG icon variants and hook values.
- [ ] Use labels for close-button accessible text.
- [ ] Use class slots for all class attributes.
- [ ] Do not add final fallback heading/message copy.

V2 adoption:

- [ ] `StorefrontToastVisuals` supplies current V2 classes and close-button label.
- [ ] `MainLayout` renders exactly one `StorefrontToastRegion` between header and main content.
- [ ] MainLayout continues to own the placement order: header, toast region/template, main, footer.
- [ ] Existing V2 CSS selectors continue to match.
- [ ] Existing V2 JavaScript selector constants continue to match.

Rendered tests:

- [ ] Add `StorefrontToastRegionPrimitiveTests` using `HtmlRenderer`.
- [ ] Assert exactly one region and one template.
- [ ] Assert the two outputs are siblings with no new wrapper.
- [ ] Assert region precedes template.
- [ ] Assert all hooks and all four icon values.
- [ ] Assert host classes and close label appear.
- [ ] Assert no literal V2 class or final fallback message exists in the primitive.
- [ ] Assert composed MainLayout contains the component once and does not retain inline duplicated markup.

Exit criteria:

- [ ] Components.Primitives builds;
- [ ] V2 and V2.WASM build;
- [ ] composed V2 layout exposes exactly one region/template pair;
- [ ] no V2 CSS or JS compatibility alias was added;
- [ ] toast success/error behavior remains browser-testable.

## Phase 3.4.5 - Migrate Ownership Tests And Guardrails

Goal: move every existing assertion to the correct source owner without deleting behavior coverage.

Tests that must be reviewed and updated:

- [ ] `StorefrontComponentsHeadlessPresentationRefactorTests.cs`.
- [ ] `StorefrontBrandingMarkupTests.cs`.
- [ ] `StorefrontVisualSourceOwnershipTests.cs`.
- [ ] `SecurityPrivacyPhase3ConsentTests.cs`.
- [ ] `LayoutAssetFoundationTests.cs`.
- [ ] `StorefrontPresentationFoundationBoundaryTests.cs`.
- [ ] `StorefrontComponentVisualNeutralityTests.cs`.
- [ ] `StorefrontPrimitiveDependencyTests.cs`.
- [ ] `StorefrontComponentModeDependencyTests.cs`.
- [ ] `StorefrontCommerceScriptRegressionTests.cs`.
- [ ] `StorefrontV2WASMRuntimeFoundationTests.cs` where component-mode ownership or V2.WASM graph expectations are affected.
- [ ] `StorefrontV2HostSmokeTests.cs` for existing consent host/BFF integration coverage; do not replace its API assertions with source checks.
- [ ] `StorefrontBuilderQaRegenerationTests.cs` only if the V2 composition assertion references the moved purchase component contract.

Required assertion migration:

- [ ] Assertions for semantic purchase hooks read/render the new primitive.
- [ ] Assertions for final purchase Tailwind classes and labels read `ProductPurchasePanelVisuals` or rendered V2 composition.
- [ ] Assertions for V2 product page composition require labels and classes to be supplied.
- [ ] Assertions no longer require `ProductPurchaseActionDescriptor.Empty` in the primitive.
- [ ] Assertions for consent semantic hooks render/read `StorefrontConsentPanel`.
- [ ] Assertions for final consent classes/copy read `StorefrontConsentVisuals` or rendered V2 wrapper.
- [ ] Assertions retain the V2 wrapper registration requirement.
- [ ] Assertions for toast semantics render/read `StorefrontToastRegion`.
- [ ] Assertions for final toast class values read `StorefrontToastVisuals` or rendered V2 composition.
- [ ] `ExtractToastTemplate` or equivalent helper no longer assumes the template source is `MainLayout.razor`.
- [ ] MainLayout tests assert component placement and exactly one composed hook, not inline ownership.

Guardrails to add:

- [ ] Components.Primitives new files reference only Components contracts.
- [ ] Components.Ssr consent files reference only Components and Presentation.
- [ ] Shared files contain no `@rendermode`.
- [ ] Shared files contain no `HttpClient`, `IJSRuntime`, local API routes, absolute URLs, or Commerce Node references.
- [ ] Shared files contain no final V2 class literals.
- [ ] Shared files contain no fixed storefront copy from the migrated V2 implementation.
- [ ] V2 contains no second active purchase semantic implementation.
- [ ] V2 contains no second active consent semantic implementation outside the thin wrapper.
- [ ] MainLayout contains no inline duplicate toast region/template markup.
- [ ] Starter duplicate consent implementation is explicitly excluded from the V2-only duplicate rule.

Test quality rules:

- [ ] Do not replace rendered tests with source-string tests only.
- [ ] Do not remove a regression assertion solely because a file moved.
- [ ] Do not assert incidental whitespace or generated Razor output.
- [ ] Assert stable semantic hooks, contracts, ownership, and behavior.
- [ ] Keep final visual value assertions at the V2 layer.

Exit criteria:

- [ ] all stale source paths are removed from active assertions;
- [ ] equivalent or stronger coverage exists at the new owner;
- [ ] focused architecture/component/JS tests pass;
- [ ] no test falsely claims repository-wide Starter deduplication.

## Phase 3.4.6 - Build And Automated Verification Gate

Goal: prove package boundaries, rendered semantics, V2 integration, and V2.WASM compatibility before browser QA.

Build gate:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/BlazorShop.Storefront.Components.Primitives.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/BlazorShop.Storefront.Components.Ssr.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj
```

V2 asset gate:

```powershell
Push-Location BlazorShop.PresentationV2/BlazorShop.Storefront.V2
npm ci
npm run tailwind:build
Pop-Location
```

- [ ] Confirm `tailwind.config.js` still scans `./**/*.cs` before moving final classes into `*Visuals.cs`.
- [ ] Keep all Tailwind class alternatives as complete literal strings in V2 visual configuration; do not construct class names dynamically in a way Tailwind cannot detect.
- [ ] Regenerate `wwwroot/css/site.css` through the package script; do not edit generated CSS manually.
- [ ] Confirm representative purchase, consent, and toast classes remain in generated CSS.
- [ ] Confirm asset generation does not scan or copy final V2 classes into shared packages.

Focused test gate:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj `
  --filter "FullyQualifiedName~StorefrontProductPurchasePanelPrimitiveTests|FullyQualifiedName~StorefrontConsentPanelSsrTests|FullyQualifiedName~StorefrontToastRegionPrimitiveTests|FullyQualifiedName~StorefrontPrimitiveDependencyTests|FullyQualifiedName~StorefrontComponentModeDependencyTests|FullyQualifiedName~StorefrontComponentVisualNeutralityTests|FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontBrandingMarkupTests|FullyQualifiedName~StorefrontVisualSourceOwnershipTests|FullyQualifiedName~SecurityPrivacyPhase3ConsentTests|FullyQualifiedName~LayoutAssetFoundationTests|FullyQualifiedName~StorefrontPresentationFoundationBoundaryTests|FullyQualifiedName~StorefrontCommerceScriptRegressionTests|FullyQualifiedName~StorefrontV2WASMRuntimeFoundationTests|FullyQualifiedName~StorefrontV2HostSmokeTests"
```

JavaScript proof:

```powershell
node scripts/qa/storefront-application-js-split-proof.js
```

Automated checks:

- [ ] All six projects build.
- [ ] V2 Tailwind production CSS build passes after final classes move to `*Visuals.cs`.
- [ ] Rendered component tests pass.
- [ ] dependency and visual-neutrality tests pass.
- [ ] Presentation/V2 JavaScript ownership tests pass.
- [ ] JS split proof passes all configured toast/purchase states.
- [ ] `rg` finds no deleted V2 purchase implementation references.
- [ ] `rg` finds no inline toast region/template left in MainLayout.
- [ ] `rg` confirms V2 consent wrapper has no duplicated banner markup.
- [ ] `git diff --check` passes.

Failure handling:

- [ ] Fix failures within the owning phase; do not weaken a guardrail to obtain green output.
- [ ] If an unrelated baseline test fails, record command/output and prove it existed before this phase.
- [ ] Do not proceed to browser QA with a failed build, rendered test, dependency test, or JS proof.

Exit criteria:

- [ ] automated gate is green;
- [ ] no package boundary changed beyond approved references;
- [ ] V2.WASM remains compatible;
- [ ] worktree contains only intentional phase changes.

## Phase 3.4.7 - Playwright Browser Regression QA

Goal: verify real browser behavior and visual equivalence through V2 user flows, not only source or smoke tests.

Environment:

- [ ] Start the configured V2 local environment with `scripts/run-v2-local.ps1 -StopExisting`.
- [ ] Confirm Commerce Node, Storefront V2, and required local dependencies are healthy.
- [ ] Use the configured current test store; do not introduce fallback store behavior.
- [ ] Record tested Storefront URL, store key, commit SHA, viewport, and browser.

Product purchase scenarios:

- [ ] Open a simple purchasable product detail page.
- [ ] Confirm purchase message, quantity bounds, add button, cart link, and feedback region render.
- [ ] Add the product to cart through the real same-origin command path.
- [ ] Confirm success feedback and cart state update.
- [ ] Open a variant product with radio/select/color attributes.
- [ ] Change selections and confirm server selection-preview recalculates resolved variant, SKU/GTIN, availability, image, and add eligibility as currently supported.
- [ ] Confirm keyboard interaction and focus indication for option groups.
- [ ] Confirm missing color data does not produce invalid inline CSS or broken layout.
- [ ] Open or fixture a blocked/non-purchasable product and confirm disabled state plus reason.

Consent scenarios:

- [ ] Use actual consent BFF endpoints and browser storage/cookies; do not fake only the DOM state.
- [ ] Load a visitor without stored consent and confirm the banner appears after state resolution without an initial flash.
- [ ] Choose Essential only and confirm saved state plus hidden banner.
- [ ] Reopen management from the existing footer action.
- [ ] Save optional Preferences/Analytics/Marketing selections and confirm persisted state after reload.
- [ ] Revoke consent and confirm expected banner/state behavior after reload.
- [ ] Confirm no absolute Commerce Node call is made from the browser.

Toast scenarios:

- [ ] Trigger a reachable success toast through an actual product/cart browser flow.
- [ ] Trigger a reachable error toast through a real invalid/blocking browser flow when the fixture safely supports it.
- [ ] Confirm correct icon, heading, message, level, enter/visible state, close control, and dismissal behavior.
- [ ] Confirm only one toast region exists after enhanced navigation and repeated actions.
- [ ] Confirm no duplicate event listeners create duplicate toasts.
- [ ] Use the existing JavaScript split proof for info/warning template states if no production user flow emits those levels.
- [ ] Do not add a production QA endpoint or global test-only JavaScript hook solely to trigger info/warning.

Viewport and quality coverage:

- [ ] Desktop viewport: 1440x900 or repository-standard equivalent.
- [ ] Mobile viewport: 390x844 or repository-standard equivalent.
- [ ] Product panel content does not overflow or overlap.
- [ ] Consent banner controls remain reachable and do not obscure required content incoherently.
- [ ] Toast region and cards remain within viewport and do not block primary actions after dismissal.
- [ ] No unexpected console errors.
- [ ] No failed purchase/consent requests outside intentionally tested failure scenarios.
- [ ] Capture screenshots for product simple, product variant, consent open, and toast visible states.

Exit criteria:

- [ ] real purchase and consent browser workflows pass;
- [ ] reachable toast success/error workflows pass;
- [ ] JS proof covers remaining semantic toast levels;
- [ ] desktop and mobile evidence exists;
- [ ] no selector, event, layout, or visual regression is observed.

## Phase 3.4.8 - Documentation, Duplication Audit, And Closure

Goal: make ownership discoverable, remove obsolete V2 markup, and close only with reproducible evidence.

Documentation files to review/update:

- [ ] `BlazorShop.PresentationV2/COMPONENT-MODES.md`.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/README.md`.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/README.md`.
- [ ] `docs/architecture/05-project-and-folder-guide.md`.
- [ ] `docs/architecture/10-v2-contract-ownership.md`.
- [ ] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`.
- [ ] `AGENTS.md` only if an active ownership statement is incomplete or stale after extraction.

Required documentation corrections:

- [ ] Document purchase semantic rendering in Primitives and final purchase visuals in V2.
- [ ] Document consent semantic rendering in Ssr, behavior in Presentation JS, and registered visual wrapper/final presentation in V2.
- [ ] Document toast semantic region/template in Primitives and placement/configuration/behavior in V2.
- [ ] Clarify that neutral class-slot contracts are permitted while final class values are forbidden in shared packages.
- [ ] Replace any statement that MainLayout owns inline toast DOM with a statement that MainLayout owns toast placement.
- [ ] Preserve historical QA notes and append current ownership evidence instead of rewriting history.
- [ ] Record Starter consent adoption as deferred and outside Phase 3.4.

Duplication audit:

```powershell
rg -n "data-storefront-product-purchase-panel|data-storefront-consent-banner|data-storefront-toast-region|data-storefront-toast-template" `
  BlazorShop.PresentationV2/BlazorShop.Storefront.V2 `
  BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives `
  BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr
```

- [ ] Purchase semantic implementation exists once in Primitives and is consumed by V2.
- [ ] Consent semantic implementation exists once for V2 in Ssr; V2 retains only the thin wrapper.
- [ ] Toast region/template implementation exists once in Primitives and is placed once by V2.
- [ ] Starter duplicate is reported separately and is not counted as an accidental V2 duplicate.
- [ ] No deleted component remains registered or referenced.
- [ ] No empty compatibility file/folder is retained solely to preserve the old V2 location.

Closure verification:

- [ ] Re-run the complete Phase 3.4.6 automated gate.
- [ ] Re-run the required Phase 3.4.7 browser scenarios from a clean application restart.
- [ ] Run `git diff --check`.
- [ ] Review `git diff --stat` and full diff for scope drift.
- [ ] Record commands, pass counts, browser URLs, viewports, screenshots, and known deferred items in this file or the QA checklist.
- [ ] Change this plan status from `planned` to `complete` only after all required checks pass.

Fresh post-implementation review:

- [ ] Re-run `rg` without assuming the planned paths are the only consumers.
- [ ] Review package project references again.
- [ ] Review rendered output rather than source ownership only.
- [ ] Review Presentation/V2 JavaScript ownership again.
- [ ] Confirm no final copy/class/route/default leaked into shared components.
- [ ] Confirm no follow-up compatibility extraction is required for these three V2 surfaces.
- [ ] If a new issue is found, fix it inside the owning Phase 3.4 task and repeat its gate; do not create an unplanned cleanup phase for omitted checklist work.

Final exit criteria:

- [ ] all three approved semantic render extractions are active in V2;
- [ ] old V2 duplicate implementations are removed;
- [ ] final V2 classes, copy, placement, and visual behavior remain V2-owned;
- [ ] purchase command and consent behavior remain Presentation-owned;
- [ ] shared package dependency and visual-neutrality rules pass;
- [ ] V2 and V2.WASM build and test successfully;
- [ ] Playwright evidence confirms real browser behavior;
- [ ] docs and QA accurately describe current ownership;
- [ ] Starter remains explicitly deferred, not silently forgotten;
- [ ] fresh review finds no unresolved Phase 3.4 blocker.

## Test Coverage Map

```text
ProductPurchasePanelModel + ProductPurchaseActionDescriptor
    -> StorefrontProductPurchasePanel primitive
        -> HtmlRenderer semantic tests
        -> V2 ProductPurchasePanelVisuals ownership tests
        -> V2ProductPageView composition tests
        -> Presentation purchase command JS tests
        -> Playwright simple/variant/blocked purchase flows

StorefrontConsentContext
    -> StorefrontConsentPanel SSR component
        -> HtmlRenderer semantic tests
        -> V2 thin wrapper + visual ownership tests
        -> Presentation native-hidden consent JS tests
        -> Host consent BFF tests
        -> Playwright save/manage/revoke flows

StorefrontToastRegion contracts
    -> StorefrontToastRegion primitive
        -> HtmlRenderer no-wrapper/hook tests
        -> MainLayout placement/count tests
        -> V2 CSS/JS selector tests
        -> JS split proof for all levels
        -> Playwright reachable success/error flows
```

## Implementation Order And Dependencies

```text
3.4.0 Baseline lock
    |
    v
3.4.1 Neutral contracts
    |
    +--> 3.4.2 Purchase primitive
    |
    +--> 3.4.3 Consent SSR + native hidden
    |
    +--> 3.4.4 Toast primitive
              |
              v
       3.4.5 Test/guardrail migration
              |
              v
       3.4.6 Automated gate
              |
              v
       3.4.7 Playwright QA
              |
              v
       3.4.8 Docs/audit/closure
```

- [ ] Do not delete old V2 markup before the corresponding shared implementation, V2 adoption, and migrated tests compile together.
- [ ] Product, consent, and toast extraction commits may remain separate, but Phase 3.4 is not complete until the common test/browser/docs gates pass.
- [ ] A failed phase blocks downstream phases.

## Decision Audit Trail

| # | Review | Decision | Classification | Principle | Rationale | Rejected alternative |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | CEO | Extract exactly three approved surfaces | Auto-decided | Scope discipline | Matches current candidates without ecommerce feature expansion | Broad shared-component sweep |
| 2 | Engineering | Reuse and extend `ProductPurchaseLabels` | Auto-decided | Reuse existing contracts | Avoids duplicate public concepts | New `ProductPurchasePanelLabels` |
| 3 | Engineering | Keep class slots explicit and typed | Auto-decided | Compile-time safety | Matches existing gallery/navigation patterns | Untyped attribute/class dictionary |
| 4 | Engineering | Use Components.Primitives for purchase/toast and Components.Ssr for consent | Auto-decided | Dependency ownership | Matches actual model dependencies | Put all three in one package |
| 5 | Engineering | Keep thin V2 consent wrapper | Auto-decided | Preserve registration contract | `V2FoundationViewRegistration` needs a V2 visual type | Register shared component directly and remove V2 slot |
| 6 | Engineering | Replace consent `hidden` class coupling with native hidden state | Auto-decided | Portability and semantic behavior | Shared behavior must not require Tailwind | Require every host to provide a literal hidden class |
| 7 | Design | Correct product option grouping accessibility during extraction | Auto-decided | Correct semantics | Current label-to-div association is invalid | Preserve invalid association indefinitely |
| 8 | Design | Preserve current final visuals and DOM order | Auto-decided | Regression control | Phase is extraction, not redesign | Visual refresh during move |
| 9 | Engineering | Keep Starter out of implementation scope but disclose it as deferred | Approved user direction | Scope truthfulness | Current work focuses on V2/V2.WASM | Claim repository-wide consent deduplication |
| 10 | Engineering | Require HtmlRenderer tests plus source guardrails | Auto-decided | Behavioral evidence | Source checks alone cannot prove Razor output | Source-string assertions only |
| 11 | QA | Build V2.WASM unconditionally | Auto-decided | Transitive compatibility | Shared contracts affect its host graph | Build only V2 server host |
| 12 | QA | Browser-test real purchase/consent flows | Approved project standard | Production confidence | Smoke/source tests do not prove user behavior | DOM-only smoke test |
| 13 | QA | Cover unreachable toast levels with JS proof, not production QA hooks | Auto-decided | Avoid test-only production surface | Existing proof can validate all templates | Add global production test hook |
| 14 | DX | Reserve completion claim until fresh post-review | Auto-decided | Evidence-based closure | Prevents a final cleanup phase caused by stale assumptions | Mark complete after build only |

## Definition Of Done

```text
Architecture
[ ] Purchase and toast primitives reference Components only.
[ ] Consent SSR component references only approved Components/Presentation contracts.
[ ] No shared component references V2, V2.WASM, Browser, Runtime, Client, or backend projects.
[ ] No shared component owns final V2 classes, copy, routes, JS behavior, or render mode.

Product purchase
[ ] Shared primitive renders the complete current purchase semantic contract.
[ ] V2 supplies final classes and labels.
[ ] Valid accessible grouping replaces label-to-div association.
[ ] Invalid/missing colors do not leak unsafe or V2-specific fallback styles.
[ ] Actual add-to-cart and variant selection-preview flows pass.

Consent
[ ] Shared SSR component renders the complete consent semantic contract.
[ ] Thin V2 wrapper remains registered.
[ ] Presentation owns current/save/revoke and native hidden state.
[ ] Actual save/manage/revoke flows pass without initial flash.
[ ] Starter remains unchanged and explicitly deferred.

Toast
[ ] Shared primitive renders one sibling region/template pair without a wrapper.
[ ] MainLayout owns placement and contains no duplicated inline toast markup.
[ ] V2 owns classes, accessible close copy, CSS, and visual JavaScript behavior.
[ ] JS proof covers all levels and browser QA covers reachable real flows.

Verification
[ ] Components, Primitives, Ssr, Presentation, V2, and V2.WASM build.
[ ] V2 Tailwind asset generation retains the moved visual classes.
[ ] Focused rendered, architecture, ownership, and JavaScript tests pass.
[ ] Playwright desktop/mobile evidence exists.
[ ] No unexpected console/network errors remain.
[ ] Documentation and QA checklist match the final code.
[ ] Fresh review finds no unresolved blocker.
```
