# Storefront Reference Components

Status: in progress
Owner: Storefront V2 architecture
Scope: Phase 2 reference components only

## Goal

Implement exactly three reusable reference features to prove the current Storefront component mode architecture:

- SSR reference component: `StorefrontBrandLogo`
- Hybrid reference component: `StorefrontContactForm`
- WasmHost reference component: `StorefrontDiscountedProductRail`

This phase proves real V2 usage of `BlazorShop.Storefront.Components.Ssr`, `BlazorShop.Storefront.Components.Hybrid`, and `BlazorShop.Storefront.Components.WasmHost` without reopening the retired `Components/Features` model and without moving visual ownership out of V2.

## Current Codebase Facts

- `Storefront Component Mode Foundation.todo.md` is complete.
- `Storefront Component Mode Foundation Closure Patch.todo.md` is complete.
- `BlazorShop.PresentationV2/COMPONENT-MODES.md` already names the intended first real examples:
  - `StorefrontBrandLogo`
  - `StorefrontContactForm`
  - `StorefrontContactFormApp`
  - `StorefrontDiscountedProductRail`
- `BlazorShop.Storefront.Components` is still the base contracts/headless package and uses `Microsoft.NET.Sdk`.
- `BlazorShop.Storefront.Components/Features` is retired and must not return.
- `BlazorShop.Storefront.Components.Ssr` references only base Components and Presentation.
- `BlazorShop.Storefront.Components.Hybrid` references only base Components, Presentation, and Components.WasmHost.
- `BlazorShop.Storefront.Components.WasmHost` references only base Components and Browser.
- `StorefrontComponentCategory` currently has `Brand`, `Content`, `Catalog`, `Cart`, `Checkout`, `Account`, `Marketing`, and `System`; it does not have `Shell`.
- `StorefrontHeader.razor` currently duplicates the brand/logo markup in desktop and mobile header regions.
- Commerce Node already has a store-scoped public contact endpoint at `api/storefront/stores/{storeKey}/contact`.
- Generated `BlazorShop.Storefront.Client` already contains `IStorefrontContactClient`.
- Runtime already registers the contact generated client through `AddStorefrontContactRuntime`.
- Storefront Presentation currently maps auth, preference, cart, account, checkout, consent, SEO, and media local endpoints, but does not map a contact local endpoint.
- Storefront Browser currently registers cart, checkout, and account controllers, but does not register contact or catalog rail browser controllers.
- Product summaries already expose `ComparePriceDisplay`; there is no current public catalog query flag named `discountedOnly` or equivalent.
- V2 currently renders latest products on Home through `StorefrontDealsSection` and `Context.LatestProductSummaries`.

## Hard Scope Lock

Allowed production areas:

- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/**`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/**`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/**`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/**`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/**`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/**`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/**`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/**`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/**`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/**`
- [x] focused architecture and browser tests under `BlazorShop.Tests.V2/**`
- [x] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- [x] `BlazorShop.PresentationV2/COMPONENT-MODES.md` only if the implemented behavior requires a clarification.

Forbidden scope:

- [x] Do not recreate `BlazorShop.Storefront.Components/Features`.
- [x] Do not move V2 visual markup, Tailwind classes, V2 CSS, or store copy into reusable mode projects.
- [x] Do not introduce a large catalog component suite.
- [x] Do not implement a discount engine or new discount core behavior.
- [x] Do not add a `discountedOnly` Storefront API query unless a later backend phase explicitly approves it.
- [x] Do not rewrite contact messaging, captcha provider, SMTP, or transactional message core.
- [x] Do not modify StorefrontBuilder, Starter, generated storefront output, Control Plane, Commerce Node domain/application/infrastructure logic, or OpenAPI generation unless a compile/test blocker proves it is required.
- [x] Do not let WasmHost components inject `HttpClient` or call `/api/*` directly.
- [x] Do not let Hybrid components inject Browser controllers.
- [x] Do not let SSR components use `@rendermode`, JS interop, Browser, Runtime, or Client.

## Target Component Contracts

### SSR - `StorefrontBrandLogo`

Target project:

- `BlazorShop.Storefront.Components.Ssr`

Base contracts:

- `StorefrontBrandLogoContext`
  - `string HomeUrl`
  - `string BrandName`
  - `string? BrandLabel`
  - `string? LogoUrl`
  - `string? HomeLabel`
- `StorefrontBrandLogoClasses`
  - `string? Root`
  - `string? Image`
  - `string? Mark`
  - `string? Label`

Descriptor:

- Key: `brand-logo`
- Mode: `StorefrontComponentMode.Ssr`
- Category: `StorefrontComponentCategory.Brand`
- Component type: `StorefrontBrandLogo`

Behavior:

- [ ] Render a normal anchor to `HomeUrl`.
- [ ] Use `aria-label` from `HomeLabel` when present; otherwise use `BrandName`.
- [ ] Render an image only when `LogoUrl` is not blank.
- [ ] Render brand name fallback text.
- [ ] Render optional brand label when supplied.
- [ ] Add stable semantic hooks such as `data-storefront-component="brand-logo"` and `data-storefront-brand`.
- [ ] Use only dynamic class slots, for example `class="@Classes.Root"`.
- [ ] Do not use literal `class` values.
- [ ] Do not use V2 CSS class names inside the reusable component.
- [ ] Do not use JS interop, Browser controllers, `HttpClient`, `@rendermode`, or direct route constants.

V2 adoption:

- [ ] Replace the duplicated desktop brand block in `StorefrontHeader.razor`.
- [ ] Replace the duplicated mobile brand block in `StorefrontHeader.razor`.
- [ ] Keep existing V2 class names in V2 by passing `StorefrontBrandLogoClasses`.
- [ ] Preserve current header accessibility and visual output.
- [ ] Do not change footer brand markup in this phase unless there is a direct duplication bug caused by the header adoption.

### Hybrid - `StorefrontContactForm`

Target projects:

- Hybrid shell: `BlazorShop.Storefront.Components.Hybrid`
- Browser child app: `BlazorShop.Storefront.Components.WasmHost`
- Browser controller: `BlazorShop.Storefront.Browser`
- Same-origin BFF endpoint: `BlazorShop.Storefront.Presentation`

Base contracts:

- `StorefrontContactFormState`
  - `string Name`
  - `string Email`
  - `string Subject`
  - `string Message`
  - `bool IsSubmitting`
  - `bool Submitted`
  - `string? ErrorCode`
  - `string? DefaultMessage`
  - `IReadOnlyDictionary<string, IReadOnlyList<string>> FieldErrors`
- `StorefrontContactFormActionDescriptor`
  - same-origin action path supplied by Presentation or host, not hardcoded by the reusable components.
- `StorefrontContactFormLabels`
  - host/store supplied label/copy values.
- `StorefrontContactFormClasses`
  - dynamic class slots only for form, fields, error region, and submit control.
- `StorefrontContactFormSubmitRequest`
  - browser-safe local request shape for `Name`, `Email`, `Subject`, `Message`.
- `StorefrontContactFormSubmitResult`
  - `bool Success`
  - `string? Code`
  - `string? DefaultMessage`
  - `string? TraceId`
  - field errors.

Important contract adjustment:

- [ ] Include `Subject` in the component contract because the current Commerce Node contact request requires it.
- [ ] Do not silently omit `Subject` unless the Presentation endpoint deliberately supplies a documented default subject.
- [ ] Preferred direction: render a subject input in the reference form so UI and backend contract remain honest.

Hybrid shell behavior:

- [ ] Render SSR-first semantic form markup that remains visible before WASM is ready.
- [ ] Host the WasmHost child `StorefrontContactFormApp`.
- [ ] Own `@rendermode` only at the bridge point.
- [ ] Pass initial state, labels, classes, and action descriptor into the child.
- [ ] Do not reference `BlazorShop.Storefront.Browser` directly.
- [ ] Do not inject browser controllers.
- [ ] Do not use `HttpClient`.
- [ ] Do not include final V2 copy or V2/Tailwind classes.

WasmHost child behavior:

- [ ] Implement `StorefrontContactFormApp`.
- [ ] Inject only Browser controller abstractions from `BlazorShop.Storefront.Browser`.
- [ ] Submit through Browser controller to a same-origin Presentation endpoint.
- [ ] Show loading, success, field validation, error, and retry states through state contracts.
- [ ] Do not use `HttpClient`.
- [ ] Do not reference Presentation, Runtime, Client, V2, backend/core/API projects, or direct `/api/storefront/*`.
- [ ] Do not self-declare `@rendermode`.
- [ ] Use dynamic class slots only.

Descriptor:

- Public descriptor key: `contact-form`
- Mode: `StorefrontComponentMode.Hybrid`
- Category: `StorefrontComponentCategory.Content`
- Component type: `StorefrontContactForm`
- [ ] Do not publish a separate public descriptor for internal child `StorefrontContactFormApp`.

Presentation endpoint:

- [ ] Add a same-origin local contact endpoint, for example `POST /api/contact`.
- [ ] Validate antiforgery through the existing Presentation antiforgery pipeline.
- [ ] Use existing Runtime/generated contact client path; do not call Commerce Node directly from Browser/WasmHost.
- [ ] Resolve current store through the existing Presentation/runtime store context pattern.
- [ ] Map local request to generated `StorefrontContactRequest`.
- [ ] Map Commerce Node response to a browser-safe result with `Success`, `Code`, `DefaultMessage`, `TraceId`, and field errors where available.
- [ ] Keep user-facing final copy owned by V2/host labels, not by Runtime or Browser.

Browser controller:

- [ ] Add `IStorefrontBrowserContactController`.
- [ ] Add `StorefrontBrowserContactController`.
- [ ] Register it through `AddStorefrontBrowserControllers()`.
- [ ] Use `StorefrontLocalApiClient`.
- [ ] Use same-origin relative endpoint path only.
- [ ] Preserve cancellation behavior.
- [ ] Return semantic error data instead of hardcoding final UI copy.

V2 adoption:

- [ ] Add or update one V2 contact page/region that uses `StorefrontContactForm`.
- [ ] Provide V2-owned labels, copy, and class slots.
- [ ] Ensure the current contact route still renders before WASM hydration.
- [ ] Ensure submit works after WASM loads.
- [ ] Do not map contact/account/cart/checkout as page-template types in this phase.

### WasmHost - `StorefrontDiscountedProductRail`

Target projects:

- Component: `BlazorShop.Storefront.Components.WasmHost`
- Browser controller: `BlazorShop.Storefront.Browser`
- Same-origin BFF endpoint: `BlazorShop.Storefront.Presentation`
- V2 adoption: Home page or another small V2 region.

Base contracts:

- `StorefrontDiscountedProductRailState`
  - `bool IsLoading`
  - `IReadOnlyList<ProductSummaryItem> Items`
  - `bool IsEmpty`
  - `string? ErrorCode`
  - `string? DefaultMessage`
  - `bool Retryable`
- `StorefrontDiscountedProductRailRequest`
  - `int Limit`
- `StorefrontDiscountedProductRailActionDescriptor`
  - host/Preset supplied same-origin endpoint path.
- `StorefrontDiscountedProductRailLabels`
  - host/store supplied label/copy values.
- `StorefrontDiscountedProductRailClasses`
  - dynamic class slots only for root, heading, list, item wrapper, loading, empty, error, retry.

Data strategy for this phase:

- [ ] Do not add new discount core behavior.
- [ ] Do not add a Storefront API `discountedOnly` query.
- [ ] Use existing catalog query/read model path.
- [ ] In Presentation, request a capped product page using existing catalog services.
- [ ] Filter candidate products where compare/regular price evidence is present, preferably `ComparePriceDisplay` not blank.
- [ ] Return at most `Limit`.
- [ ] Document that this is a reference component strategy, not the final discount engine.
- [ ] If no discounted products exist, return an empty state instead of falling back to unrelated latest products.

WasmHost behavior:

- [ ] Load on component initialization.
- [ ] Support `Limit`.
- [ ] Show loading state.
- [ ] Show success state with product summaries.
- [ ] Show empty state.
- [ ] Show error state.
- [ ] Support retry.
- [ ] Use Browser controller only.
- [ ] Do not inject `HttpClient`.
- [ ] Do not call direct `/api/*` or `api/storefront/*`.
- [ ] Do not self-declare `@rendermode`.
- [ ] Do not own final V2 visual classes.

Descriptor:

- Key: `discounted-product-rail`
- Mode: `StorefrontComponentMode.WasmHost`
- Category: `StorefrontComponentCategory.Catalog`
- Component type: `StorefrontDiscountedProductRail`

Presentation endpoint:

- [ ] Add a same-origin local endpoint, for example `GET /api/catalog/discounted-products?limit=...`.
- [ ] No antiforgery required for GET because it is read-only.
- [ ] Validate `limit` with a safe min/max, for example 1 to 24.
- [ ] Use existing Presentation catalog services and `StorefrontProductSummaryMapper`.
- [ ] Do not expose raw generated API DTOs directly to the browser component.
- [ ] Return browser-safe rail response with product summaries and semantic error data.

Browser controller:

- [ ] Add `IStorefrontBrowserCatalogController` or a narrower `IStorefrontBrowserProductRailController`.
- [ ] Prefer a narrow method such as `GetDiscountedProductRailAsync(limit, cancellationToken)` to avoid creating a browser-side catalog god controller.
- [ ] Register the controller through `AddStorefrontBrowserControllers()`.
- [ ] Use `StorefrontLocalApiClient`.
- [ ] Preserve cancellation behavior and same-origin URL protection.

V2 adoption:

- [ ] Add the rail to V2 Home only as a small proof region.
- [ ] Do not replace the existing latest-products section unless the UX becomes duplicative and the change is explicitly kept small.
- [ ] V2 owns all class values, headings, empty/error copy, and product card visual composition.
- [ ] If a V2 wrapper is needed around `ProductSummaryItem`, keep that wrapper in V2.

## Phase 0 - Baseline And Scope Confirmation

- [x] Read `AGENTS.md`.
- [x] Read `docs/architecture/README.md`.
- [x] Read `docs/architecture/05-project-and-folder-guide.md`.
- [x] Read `docs/architecture/08-agent-decision-rules.md`.
- [x] Read `docs/architecture/10-v2-contract-ownership.md`.
- [x] Read `BlazorShop.PresentationV2/COMPONENT-MODES.md`.
- [x] Read `Storefront Component Mode Foundation.todo.md`.
- [x] Read `Storefront Component Mode Foundation Closure Patch.todo.md`.
- [x] Read `QA-StorefrontV2.todo.md`.
- [x] Run `git status --short` and record unrelated changes.
- [x] Confirm no `Components/Features` folder exists.
- [x] Confirm no real descriptors currently exist in the mode projects.
- [x] Confirm current V2 header brand markup duplication.
- [x] Confirm current contact backend/generated/runtime support.
- [x] Confirm Presentation and Browser contact/catalog local gaps.
- [x] Confirm product summary compare price fields are available.

Exit criteria:

- [x] Scope is locked as exactly three reference components.
- [x] Backend/core behavior expansion is explicitly out of scope.
- [x] V2 is the only consumer host for this phase.
- [x] Starter, StorefrontBuilder, and generated storefronts are unchanged.

## Phase 1 - Base Contracts And Headless State

Add contracts to base `BlazorShop.Storefront.Components` only.

Brand contracts:

- [ ] Add `Contracts/Brand/StorefrontBrandLogoContext.cs`.
- [ ] Add `Contracts/Brand/StorefrontBrandLogoClasses.cs`.

Contact contracts:

- [ ] Add `Contracts/Contact/StorefrontContactFormState.cs`.
- [ ] Add `Contracts/Contact/StorefrontContactFormLabels.cs`.
- [ ] Add `Contracts/Contact/StorefrontContactFormClasses.cs`.
- [ ] Add `Contracts/Contact/StorefrontContactFormActionDescriptor.cs`.
- [ ] Add `Contracts/Contact/StorefrontContactFormSubmitRequest.cs`.
- [ ] Add `Contracts/Contact/StorefrontContactFormSubmitResult.cs`.
- [ ] Add validation/state helpers only if they remain browser-safe and do not duplicate backend validation rules.

Discounted rail contracts:

- [ ] Add `Contracts/Catalog/StorefrontDiscountedProductRailState.cs`.
- [ ] Add `Contracts/Catalog/StorefrontDiscountedProductRailRequest.cs`.
- [ ] Add `Contracts/Catalog/StorefrontDiscountedProductRailResponse.cs`.
- [ ] Add `Contracts/Catalog/StorefrontDiscountedProductRailLabels.cs`.
- [ ] Add `Contracts/Catalog/StorefrontDiscountedProductRailClasses.cs`.
- [ ] Add `Contracts/Catalog/StorefrontDiscountedProductRailActionDescriptor.cs`.
- [ ] Reuse existing `ProductSummaryItem`; do not create a duplicate product card DTO.

General contract rules:

- [ ] No V2 CSS class defaults.
- [ ] No hardcoded same-origin endpoint paths in contracts unless represented as host-supplied descriptors.
- [ ] No `HttpClient`, Runtime, Client, Presentation, V2, backend/core/API, or Web.SharedV2 references.
- [ ] Keep the base Components project on `Microsoft.NET.Sdk`.
- [ ] Keep base Components free of `.razor`, CSS, JS, and `Features`.

Exit criteria:

- [ ] Base Components builds.
- [ ] Existing headless/component architecture tests still pass.
- [ ] Contracts are small, semantic, and reusable.

## Phase 2 - SSR Brand Logo Component

Implement in `BlazorShop.Storefront.Components.Ssr`.

Files:

- [ ] `Brand/StorefrontBrandLogo.razor`
- [ ] `Brand/StorefrontBrandLogoDescriptor.cs` or equivalent descriptor holder.
- [ ] Update `_Imports.razor` only if required.

Component behavior:

- [ ] Accept `StorefrontBrandLogoContext`.
- [ ] Accept `StorefrontBrandLogoClasses`.
- [ ] Render anchor, optional logo image, brand mark, optional label.
- [ ] Use stable `data-storefront-*` hooks.
- [ ] Use only fully dynamic class attributes.
- [ ] Do not include literal visual classes.
- [ ] Do not use `@rendermode`.
- [ ] Do not use JS, Browser, Runtime, Client, `HttpClient`, or direct routes.

Descriptor:

- [ ] Add descriptor key `brand-logo`.
- [ ] Use mode `Ssr`.
- [ ] Use category `Brand`.
- [ ] Ensure descriptor type points at `StorefrontBrandLogo`.

Tests:

- [ ] Add component render/unit test for image present.
- [ ] Add component render/unit test for text fallback when `LogoUrl` is blank.
- [ ] Add descriptor test coverage if existing repository descriptor guard does not discover it automatically.
- [ ] Ensure visual neutrality tests reject any accidental literal classes.

Exit criteria:

- [ ] SSR mode boundary tests pass.
- [ ] Descriptor mode/project consistency tests pass.
- [ ] `StorefrontBrandLogo` can render without browser runtime.

## Phase 3 - Contact Presentation BFF Endpoint

Implement in `BlazorShop.Storefront.Presentation`.

Endpoint:

- [ ] Add `Endpoints/StorefrontPresentationContactEndpoints.cs`.
- [ ] Map endpoint from `MapStorefrontPresentation`.
- [ ] Use a same-origin path such as `POST /api/contact`.
- [ ] Keep the endpoint name/route local to Presentation, not Commerce Node direct route.
- [ ] Require antiforgery through existing `UseAntiforgery` and endpoint metadata pattern.
- [ ] Bind an explicit local request DTO.
- [ ] Validate required fields:
  - Name
  - Email
  - Subject
  - Message
- [ ] Map field validation failures into a browser-safe error result.
- [ ] Resolve store context using existing Presentation/runtime context pattern.
- [ ] Use the existing Runtime/generated contact client path.
- [ ] Map to generated `StorefrontContactRequest`.
- [ ] Map generated envelope response to local `StorefrontContactFormSubmitResult`.
- [ ] Preserve `TraceId` if available in the existing error model.

Tests:

- [ ] Add focused endpoint mapping test proving `MapStorefrontPresentation` includes contact endpoints.
- [ ] Add test proving the endpoint does not inject concrete V2 client or use direct Commerce Node URL.
- [ ] Add validation test for missing required fields.
- [ ] Add success mapping test using mocked/fake contact client or Presentation service.
- [ ] Add failure mapping test for backend rejected/invalid response.

Exit criteria:

- [ ] Browser clients can submit through same-origin Presentation endpoint.
- [ ] No Browser/WasmHost component needs to know Commerce Node route shape.
- [ ] API contract remains truthful about `Subject`.

## Phase 4 - Contact Browser Controller

Implement in `BlazorShop.Storefront.Browser`.

Files:

- [ ] `Contact/IStorefrontBrowserContactController.cs` or `Contact/StorefrontBrowserContactController.cs` plus interface.
- [ ] Update `StorefrontBrowserServiceCollectionExtensions.cs`.

Behavior:

- [ ] Submit `StorefrontContactFormSubmitRequest` through `StorefrontLocalApiClient`.
- [ ] Use only same-origin local path from descriptor or a browser-safe default controlled by Presentation contracts.
- [ ] Attach antiforgery automatically through `StorefrontLocalApiClient`.
- [ ] Preserve cancellation behavior.
- [ ] Surface `Success`, `Code`, `DefaultMessage`, `TraceId`, and field errors.
- [ ] Do not hardcode final user-facing copy except technical fallback if existing Browser pattern requires it.
- [ ] Do not reference Presentation, Runtime, Client, V2, backend/core/API, or Web.SharedV2.

Tests:

- [ ] Add DI registration test for contact controller.
- [ ] Add test that absolute and protocol-relative routes are not accepted if the controller allows route override.
- [ ] Add submit success mapping test.
- [ ] Add submit validation/error mapping test.
- [ ] Add cancellation behavior test.

Exit criteria:

- [ ] Contact Browser controller follows the existing cart/checkout/account controller pattern.
- [ ] WasmHost can submit contact data without direct transport ownership.

## Phase 5 - Hybrid Contact Form And WasmHost Child

Implement Hybrid shell:

- [ ] `BlazorShop.Storefront.Components.Hybrid/Content/StorefrontContactForm.razor`
- [ ] `BlazorShop.Storefront.Components.Hybrid/Content/StorefrontContactFormDescriptor.cs`

Implement WasmHost child:

- [ ] `BlazorShop.Storefront.Components.WasmHost/Content/StorefrontContactFormApp.razor`

Hybrid requirements:

- [ ] Accept state, labels, classes, and action descriptor.
- [ ] Render SSR-first form structure.
- [ ] Host `StorefrontContactFormApp`.
- [ ] Use `@rendermode` only on the child bridge.
- [ ] Do not inject Browser controllers.
- [ ] Do not include literal visual classes.

WasmHost requirements:

- [ ] Inject `IStorefrontBrowserContactController`.
- [ ] Manage submit/loading/success/error state.
- [ ] Respect required field state and server field errors.
- [ ] Do not inject `HttpClient`.
- [ ] Do not call `/api/*` directly.
- [ ] Do not reference Presentation, Runtime, Client, V2, or backend projects.
- [ ] Do not use `@rendermode`.
- [ ] Use only dynamic class slots and semantic data hooks.

Descriptor:

- [ ] Add `contact-form`.
- [ ] Mode is `Hybrid`.
- [ ] Category is `Content`.
- [ ] Do not add a public descriptor for `StorefrontContactFormApp`.

Tests:

- [ ] Hybrid boundary tests pass.
- [ ] WasmHost boundary tests pass.
- [ ] Descriptor mode/project consistency detects `contact-form` as Hybrid.
- [ ] Visual neutrality tests pass.
- [ ] Render test proves the SSR-first form contains the expected fields.
- [ ] Component behavior test proves submit invokes the Browser controller.

Exit criteria:

- [ ] Contact form proves Hybrid shell plus WasmHost child without leaking Browser into Hybrid.
- [ ] Required `Subject` behavior is visible and test-covered.

## Phase 6 - Discounted Product Rail Presentation BFF Endpoint

Implement in `BlazorShop.Storefront.Presentation`.

Endpoint/service:

- [ ] Add a local read endpoint, for example `GET /api/catalog/discounted-products`.
- [ ] Map endpoint from `MapStorefrontPresentation`.
- [ ] Accept `limit`.
- [ ] Validate `limit`, with a safe default and max cap.
- [ ] Use existing catalog query services and product summary mapper.
- [ ] Fetch enough candidates to fill the requested rail without unbounded queries.
- [ ] Filter to products with compare-price evidence, preferably non-empty `ComparePriceDisplay`.
- [ ] Return empty list when no discounted products are found.
- [ ] Do not use latest products fallback in the discounted endpoint.
- [ ] Return browser-safe response contract.
- [ ] Do not expose raw generated client DTOs.

Tests:

- [ ] Endpoint mapping test.
- [ ] Limit validation test.
- [ ] Success test with discounted products.
- [ ] Empty-state test when no compare-price products exist.
- [ ] Error mapping test.
- [ ] Test proving no new Commerce Node API route or discount core query was introduced.

Exit criteria:

- [ ] The rail has a same-origin data source.
- [ ] No backend discount/core scope is added.

## Phase 7 - Discounted Product Rail Browser Controller

Implement in `BlazorShop.Storefront.Browser`.

Preferred files:

- [ ] `Catalog/IStorefrontBrowserProductRailController.cs`
- [ ] `Catalog/StorefrontBrowserProductRailController.cs`

Behavior:

- [ ] Load discounted product rail through `StorefrontLocalApiClient`.
- [ ] Use same-origin relative route only.
- [ ] Pass `limit`.
- [ ] Preserve cancellation behavior.
- [ ] Map success, empty, error, retryable, and default technical message fields.
- [ ] Do not hardcode final V2 copy.
- [ ] Do not reference Presentation, Runtime, Client, V2, backend/core/API, or Web.SharedV2.

Registration:

- [ ] Add `AddStorefrontBrowserCatalog()` or `AddStorefrontBrowserProductRails()`.
- [ ] Include it from `AddStorefrontBrowserControllers()`.

Tests:

- [ ] DI registration test.
- [ ] Success mapping test.
- [ ] Empty mapping test.
- [ ] Error mapping test.
- [ ] Cancellation behavior test.

Exit criteria:

- [ ] WasmHost rail can load data without direct transport ownership.

## Phase 8 - WasmHost Discounted Product Rail Component

Implement in `BlazorShop.Storefront.Components.WasmHost`.

Files:

- [ ] `Catalog/StorefrontDiscountedProductRail.razor`
- [ ] `Catalog/StorefrontDiscountedProductRailDescriptor.cs`

Behavior:

- [ ] Accept labels, classes, action descriptor, and `Limit`.
- [ ] Load data on initialization.
- [ ] Render loading state.
- [ ] Render success state.
- [ ] Render empty state.
- [ ] Render error state.
- [ ] Render retry action.
- [ ] Render product summaries through semantic slots or minimal markup that does not own final visual classes.
- [ ] Prefer `RenderFragment<ProductSummaryItem>` for item template if this avoids shared visual ownership.
- [ ] Use only dynamic class slots.
- [ ] Do not inject `HttpClient`.
- [ ] Do not call direct routes.
- [ ] Do not self-declare `@rendermode`.

Descriptor:

- [ ] Add `discounted-product-rail`.
- [ ] Mode is `WasmHost`.
- [ ] Category is `Catalog`.

Tests:

- [ ] Component state test for loading/success.
- [ ] Component state test for empty.
- [ ] Component state test for error/retry.
- [ ] Descriptor mode/project consistency test.
- [ ] Visual neutrality test.
- [ ] WasmHost boundary test.

Exit criteria:

- [ ] Rail proves a browser-interactive reference component through Browser controller and BFF.

## Phase 9 - V2 And V2.WASM Adoption

Project references:

- [ ] Add `BlazorShop.Storefront.Components.Ssr` reference to `BlazorShop.Storefront.V2`.
- [ ] Add `BlazorShop.Storefront.Components.Hybrid` reference to `BlazorShop.Storefront.V2`.
- [ ] Add `BlazorShop.Storefront.Components.WasmHost` reference where the WASM client must compile/load the child components.
- [ ] Do not add these references to Starter or generated storefront projects.

Component discovery/rendering:

- [ ] Update V2 app component assembly registration if required.
- [ ] Ensure `StorefrontContactFormApp` and `StorefrontDiscountedProductRail` are included in the interactive WASM assembly set.
- [ ] Keep V2 as visual host and class owner.

Brand adoption:

- [ ] Replace both header brand blocks with `StorefrontBrandLogo`.
- [ ] Pass V2 class slots from `StorefrontHeader.razor`.
- [ ] Preserve current layout and header behavior.

Contact adoption:

- [ ] Add/use a V2 contact route or region.
- [ ] Render `StorefrontContactForm`.
- [ ] Supply V2-owned labels/classes.
- [ ] Confirm form is visible before WASM hydration.
- [ ] Confirm submit works after WASM hydration.

Discounted rail adoption:

- [ ] Add `StorefrontDiscountedProductRail` to V2 Home or a small V2 region.
- [ ] Keep existing Home latest-products behavior unless the implementation explicitly replaces only a redundant block.
- [ ] Supply V2-owned labels/classes and product item template.
- [ ] Confirm empty/error/retry states do not break Home layout.

Exit criteria:

- [ ] V2 visibly uses all three reference components.
- [ ] V2 remains the owner of layout, class values, copy, and visual composition.
- [ ] Starter and StorefrontBuilder remain unchanged.

## Phase 10 - Descriptor Repository Guard Upgrade

Upgrade existing descriptor guard from fixture-only to real descriptor validation.

Tasks:

- [ ] Locate current descriptor mode ownership tests.
- [ ] Add repository scan for descriptor declarations in mode projects.
- [ ] Discover descriptors deterministically without requiring production registry behavior.
- [ ] Validate:
  - descriptor key is valid kebab-case.
  - descriptor mode matches owning project.
  - descriptor category is valid.
  - descriptor component type implements `IComponent`.
  - descriptor component type belongs to the same owning mode project or an explicitly allowed internal child exception.
- [ ] Assert expected real descriptors exist:
  - `brand-logo`
  - `contact-form`
  - `discounted-product-rail`
- [ ] Assert `StorefrontContactFormApp` does not have a public descriptor.
- [ ] Keep `StorefrontComponentDescriptorValidator` generic.
- [ ] Do not add production registry/scanning.

Exit criteria:

- [ ] Real descriptors cannot drift from mode/project ownership.
- [ ] Future mode components must be explicitly validated by repository tests.

## Phase 11 - Architecture Guardrails And Test Updates

Mode boundary tests:

- [ ] Ensure SSR component has no Browser/Runtime/Client/direct route/JS/render-mode tokens.
- [ ] Ensure Hybrid shell has no direct Browser controller injection, `HttpClient`, direct API route, or V2 import.
- [ ] Ensure WasmHost components have no Presentation/Runtime/Client/V2/backend imports.
- [ ] Ensure WasmHost components have no `HttpClient` and no direct `/api/*` route strings.
- [ ] Ensure reusable components have no literal `class` attributes.
- [ ] Ensure no CSS, SCSS, theme assets, or Tailwind config appear in mode projects.

V2 boundary tests:

- [ ] Update any Phase 1 test that previously asserted V2 must not reference mode projects.
- [ ] Replace it with a stricter rule: V2 may reference mode projects only because it is adopting real reference components.
- [ ] Assert Starter and generated storefront projects still do not reference mode projects in this phase.
- [ ] Assert V2 still does not reference Runtime/Client directly.
- [ ] Assert V2 still does not own BFF/manual transport DTOs.

Browser/Presentation tests:

- [ ] Add tests proving contact and discounted rail browser controllers use same-origin local endpoints.
- [ ] Add tests proving Presentation endpoint mappings do not inject concrete V2 clients.
- [ ] Add tests proving local endpoint contracts stay in Presentation/BFF or base Components, not V2.

Exit criteria:

- [ ] Existing guardrails are updated instead of weakened.
- [ ] The new V2 exception is narrow, intentional, and tested.

## Phase 12 - QA Checklist Updates

Update `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`.

Add checklist section: `Storefront Reference Components`.

Checklist items:

- [ ] SSR `StorefrontBrandLogo` renders through V2 header without browser dependency.
- [ ] SSR component uses descriptor key `brand-logo`, mode `Ssr`, category `Brand`.
- [ ] V2 owns all brand-logo class values and visual output.
- [ ] Hybrid `StorefrontContactForm` renders SSR-first form before WASM hydration.
- [ ] Contact form includes `Subject` or has a documented Presentation default subject.
- [ ] Contact form submits through Browser controller and same-origin Presentation endpoint.
- [ ] Contact submit uses antiforgery.
- [ ] Contact success, validation failure, backend failure, and retry states are browser-tested.
- [ ] `StorefrontContactFormApp` is not a public descriptor.
- [ ] WasmHost `StorefrontDiscountedProductRail` loads through Browser controller and same-origin Presentation endpoint.
- [ ] Discounted rail loading, success, empty, error, and retry states are browser-tested.
- [ ] Discounted rail does not introduce backend discount core changes.
- [ ] Reusable mode projects still have no literal classes, CSS, theme assets, direct APIs, or forbidden project references.
- [ ] Starter and StorefrontBuilder remain unchanged.
- [ ] Playwright evidence is recorded for visible V2 flows.

Exit criteria:

- [ ] QA checklist is usable without reading this plan.
- [ ] Each item has a clear implementation or verification owner.

## Phase 13 - Build And Focused Test Gates

Build gates:

```powershell
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Components\BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation\BlazorShop.Storefront.Presentation.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Browser\BlazorShop.Storefront.Browser.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Components.Ssr\BlazorShop.Storefront.Components.Ssr.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Components.WasmHost\BlazorShop.Storefront.Components.WasmHost.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Components.Hybrid\BlazorShop.Storefront.Components.Hybrid.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2.WASM\BlazorShop.Storefront.V2.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2\BlazorShop.Storefront.V2.csproj --no-restore
```

Focused architecture test gate:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentModeFoundationTests|FullyQualifiedName~StorefrontComponentModeDependencyTests|FullyQualifiedName~StorefrontComponentModeBoundaryValidatorTests|FullyQualifiedName~StorefrontComponentDescriptorTests|FullyQualifiedName~StorefrontComponentVisualNeutralityTests|FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontPageCompositionGuardrailTests|FullyQualifiedName~StorefrontEndpointDependencyBoundaryTests|FullyQualifiedName~StorefrontPresentationFoundationBoundaryTests|FullyQualifiedName~StorefrontVisualOnlyBoundaryTests"
```

Feature test gate:

- [ ] Add and run focused tests for BrandLogo.
- [ ] Add and run focused tests for contact endpoint/controller/components.
- [ ] Add and run focused tests for discounted rail endpoint/controller/component.
- [ ] Run all new tests by fully qualified name filter and record results here.

Exit criteria:

- [ ] All relevant builds pass.
- [ ] Focused architecture tests pass.
- [ ] New feature tests pass.
- [ ] Known unrelated warnings are recorded but not fixed in this phase.

## Phase 14 - Playwright V2 Browser QA

Use Playwright because this phase changes visible V2 browser behavior.

Preparation:

- [ ] Start V2 local runtime using the repo-preferred script:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

- [ ] Confirm Storefront V2 URL and test store are healthy.
- [ ] Confirm any required test email/contact/message fixtures are configured.
- [ ] Confirm a test product with compare price exists, or record empty-state expectation.

Browser QA scenarios:

- [ ] Header brand logo/text renders on desktop.
- [ ] Header brand logo/text renders on mobile viewport.
- [ ] Header brand link navigates to Home.
- [ ] Contact page/region renders SSR-first fields before interaction.
- [ ] Contact form blocks invalid/missing required fields.
- [ ] Contact form submits valid test message through the browser.
- [ ] Contact success state is visible.
- [ ] Contact backend failure or simulated failure shows a recoverable error state.
- [ ] Discounted rail shows loading then success when discounted products exist.
- [ ] Discounted rail shows empty state when no discounted products exist.
- [ ] Discounted rail retry control works after simulated failure.
- [ ] No browser network request from WASM goes directly to Commerce Node Storefront API.
- [ ] No console errors occur during the tested flows.
- [ ] Mobile layout has no text overlap or unusable controls.

Evidence:

- [ ] Capture Playwright traces or screenshots for desktop and mobile.
- [ ] Record tested URLs, viewport sizes, and key results in `QA-StorefrontV2.todo.md`.
- [ ] Stop local runtime if the implementation session started it.

Exit criteria:

- [ ] Browser QA proves real SSR, Hybrid, and WasmHost components are visible and functional in V2.
- [ ] The browser network path follows WasmHost -> Browser -> same-origin Presentation/BFF.

## Phase 15 - Final Audit And Commit

Final audit:

- [ ] Run `git status --short`.
- [ ] Verify unrelated user changes were not modified.
- [ ] Verify `Components/Features` does not exist.
- [ ] Verify no reusable mode project contains literal visual classes.
- [ ] Verify no reusable mode project contains CSS/theme assets.
- [ ] Verify no reusable mode project references Runtime, Client, V2, Starter, backend/core/API, Control Plane, or Web.SharedV2 outside documented mode allowlists.
- [ ] Verify V2 references mode projects only for these adopted reference components.
- [ ] Verify Starter and StorefrontBuilder were not changed.
- [ ] Verify `StorefrontContactFormApp` has no public descriptor.
- [ ] Verify no discount core or Commerce Node API expansion was introduced.
- [ ] Verify docs and QA checklist include implementation evidence.

Suggested commit message:

```text
feat(storefront): add reference component mode implementations
```

Exit criteria:

- [ ] Commit includes only scoped implementation, tests, docs, and QA evidence.
- [ ] Final response lists changed files, verification results, Playwright evidence, and any skipped optional item.

## Definition Of Done

- [ ] `StorefrontBrandLogo` exists in `Components.Ssr`.
- [ ] `StorefrontBrandLogo` has descriptor `brand-logo`, mode `Ssr`, category `Brand`.
- [ ] V2 header uses `StorefrontBrandLogo` in both desktop and mobile brand locations.
- [ ] `StorefrontContactForm` exists in `Components.Hybrid`.
- [ ] `StorefrontContactFormApp` exists in `Components.WasmHost`.
- [ ] Contact form descriptor is `contact-form`, mode `Hybrid`, category `Content`.
- [ ] `StorefrontContactFormApp` is internal child behavior, not a public descriptor.
- [ ] Contact form submits through Browser controller and same-origin Presentation endpoint.
- [ ] Contact form includes or explicitly handles `Subject`.
- [ ] `StorefrontDiscountedProductRail` exists in `Components.WasmHost`.
- [ ] Discounted rail descriptor is `discounted-product-rail`, mode `WasmHost`, category `Catalog`.
- [ ] Discounted rail uses Browser controller and same-origin Presentation endpoint.
- [ ] Discounted rail does not add discount core or a public backend `discountedOnly` query.
- [ ] Base `Components` remains `Microsoft.NET.Sdk`.
- [ ] Base `Components` still has no `.razor`, CSS, JS, theme assets, or `Features` folder.
- [ ] Reusable mode libraries have no literal visual classes.
- [ ] Reusable mode libraries have no theme CSS/assets.
- [ ] Reusable mode libraries have no forbidden dependencies.
- [ ] V2 owns all final classes, layout, copy, and visual templates.
- [ ] Starter, StorefrontBuilder, and generated storefronts are unchanged.
- [ ] Focused builds pass.
- [ ] Focused architecture and feature tests pass.
- [ ] Playwright V2 browser QA passes.
- [ ] QA checklist is updated with evidence.

## Decision Audit Trail

| # | Decision | Classification | Rationale | Rejected |
|---|---|---|---|---|
| 1 | Implement exactly three reference components. | Scope control | This proves SSR, Hybrid, and WasmHost modes without turning the phase into a catalog/contact rebuild. | Build a large component catalog. |
| 2 | Use `Brand` category for `brand-logo`. | Codebase fit | Current `StorefrontComponentCategory` has no `Shell` value. | Add `Shell` just for this component. |
| 3 | Include `Subject` in contact form contract. | Contract truth | Current Commerce Node contact request requires subject. | Hide the backend requirement without documenting a default. |
| 4 | Add Presentation same-origin BFF endpoints for contact and rail. | Boundary | WasmHost must use Browser -> Presentation/BFF -> Runtime -> Commerce Node. | Let WasmHost call Commerce Node or generated clients directly. |
| 5 | Use existing product summary compare-price evidence for the discounted rail. | Minimality | Current catalog query has no discount-only flag; adding discount core is out of scope for a reference component proof. | Add new discount query/core behavior now. |
| 6 | Let V2 reference mode projects only after real components exist. | Phase progression | Phase 1 forbade host references before adoption; Phase 2 adoption needs a narrow V2 exception. | Keep V2 unaware and leave components unused. |
| 7 | Keep Starter and StorefrontBuilder out of scope. | Risk control | This phase proves real component modes in V2 first. | Update every consumer at once. |

## Implementation Notes

- [x] Record baseline command outputs here during implementation.
- [ ] Record build/test command outputs here during implementation.
- [ ] Record Playwright evidence here during implementation.
- [ ] Record any deviation from this plan with reason and file references.

Phase 0 baseline:

- `git status --short`: pre-existing unrelated `M BlazorShop.sln`; this plan file was untracked before Phase 0 commit.
- `Test-Path BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features`: `False`.
- `rg "StorefrontComponentDescriptor|brand-logo|contact-form|discounted-product-rail"` in Ssr/Hybrid/WasmHost: no existing real descriptors.
- `StorefrontHeader.razor`: desktop and mobile brand/logo markup is duplicated.
- Contact backend/generated/runtime support exists through Commerce Node `StorefrontScopedContactController`, generated `IStorefrontContactClient`, and Runtime contact registration.
- Presentation currently has no mapped local contact/catalog rail endpoint; Browser currently has no contact/catalog rail controller.
- `ProductSummaryItem.ComparePriceDisplay` and existing V2 product-card compare-price rendering are available.
- Scope remains exactly `StorefrontBrandLogo`, `StorefrontContactForm`, and `StorefrontDiscountedProductRail`; backend/core expansion, Starter, StorefrontBuilder, and generated storefront changes remain out of scope.
