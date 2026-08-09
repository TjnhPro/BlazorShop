# Storefront Presentation Visual Neutrality Closure

Status: in-progress
Scope: `BlazorShop.Storefront.Presentation`, `BlazorShop.Storefront.V2`, `BlazorShop.Storefront.V2.WASM`
Intent: close the remaining visual ownership leaks after Storefront Visual Source Consolidation without changing ecommerce behavior.

## Goal

`BlazorShop.Storefront.Presentation` must stay a host-neutral application/presentation foundation:

- owns App/Routes/page services/BFF/SEO/media composition;
- owns semantic page state, contracts, route shells, head/status behavior, and browser-safe hooks;
- does not own Storefront V2 visual classes, Tailwind utilities, theme CSS, final layout styling, icon choices, or store-specific visual output.

`BlazorShop.Storefront.V2` and `BlazorShop.Storefront.V2.WASM` must own V2 visual implementation:

- Tailwind/CSS classes;
- concrete layout wrappers;
- visual tone mapping;
- CSS/assets;
- V2-specific markup polish and copy placement.

This plan is intentionally a closure phase. It fixes verified offenders and adds guardrails so Presentation cannot quietly become a visual template owner again.

## Current Evidence

Previous completed work:

- `docs/refactor-control-Commerce-storefront/Storefront Visual Source Consolidation.todo.md` already closed JavaScript/CSS/Razor visual ownership inside `Storefront.V2` and `Storefront.V2.WASM`.
- `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md` already records V2 visual source ownership QA for toast, Font Awesome removal, catalog filter icon slot, and Playwright browser evidence.

Remaining verified offenders in current source:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages/WasmHost/Account/AccountRoutePage.razor`
  - Unauthorized redirect fallback contains Tailwind layout/text classes:
    - `mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8`
    - `text-sm text-neutral-600`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Content/StorefrontPagePresentationResolver.cs`
  - `StorefrontPagePresentation` contains `ArticleClass` and `BodyContainerClass`.
  - Resolver factory methods return V2/Tailwind-style class strings such as:
    - `bs-storefront-content-page bs-storefront-content-page--standard`
    - `rounded-3xl border border-neutral-200/70 bg-white/90 p-6 shadow-lg sm:p-8`
    - `rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm sm:p-8`
  - V2 consumes those class fields in `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Ssr/Content/StorefrontPage.razor`.
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Checkout/StorefrontPaymentResultPageContext.cs`
  - `StorefrontPaymentResultPageContext` exposes visual class fields:
    - `PanelClass`
    - `EyebrowClass`
    - `HeadingClass`
    - `BodyClass`
    - `MutedClass`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Checkout/StorefrontPaymentResultPageService.cs`
  - Service returns V2/Tailwind-style panel and tone classes:
    - `rounded-3xl border border-amber-200 bg-amber-50 px-6 py-10 text-center shadow-sm`
    - `rounded-3xl border border-emerald-200 bg-emerald-50 px-6 py-10 text-center shadow-sm`
    - `rounded-3xl border border-rose-200 bg-rose-50 px-6 py-10 text-center shadow-sm`
    - `text-amber-700`, `text-emerald-700`, `text-rose-700`, etc.
  - V2 consumes those fields in `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/PaymentResultPage.razor`.

Current non-offenders:

- `BlazorShop.Storefront.Presentation/wwwroot` only contains behavior script ownership today, not active theme CSS.
- Presentation form components mostly expose host-provided class parameters with `string.Empty` defaults. That pattern can stay.
- V2 visual classes in `BlazorShop.Storefront.V2` are expected and should not be removed by this phase.

## Non-Goals

- [x] Do not change Commerce Node APIs, Storefront API contracts, generated Client, Runtime facades, payment provider behavior, cart/checkout/order logic, or database schema.
- [x] Do not reopen Starter, StorefrontBuilder, generated storefront, or AI generator architecture in this phase.
- [x] Do not move V2 visual design into shared Components, Runtime, Client, or Browser packages.
- [x] Do not redesign content pages, payment pages, account routes, checkout flow, or copy/localization.
- [x] Do not create a generic visual registry or global design-system abstraction just to move three class mappings.
- [x] Do not ban host-provided class parameters in Presentation components when defaults are neutral.
- [x] Do not treat V2 Tailwind classes as a problem. The problem is Presentation owning those classes.

## Phase 0 - Baseline Audit And Scope Lock

Purpose: confirm the current offender list from source before editing and avoid broad refactors.

- [x] Run a focused Presentation scan:

```powershell
rg -n "bg-|text-neutral-|text-zinc-|text-slate-|text-red-|text-green-|text-emerald-|text-amber-|text-blue-|border-|shadow-|rounded-|ring-|hover:|focus:|sm:|md:|lg:|xl:|2xl:|grid-cols-|gap-|px-|py-|mx-|my-|max-w-|min-h-" `
  BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation `
  --glob "*.razor" `
  --glob "*.cs" `
  --glob "!bin/**" `
  --glob "!obj/**"
```

- [x] Run a focused inline-style/theme-asset scan:

```powershell
rg -n "style=|background-color|box-shadow|font-family|font-size|transition|transform|opacity|padding|margin|color:|border:" `
  BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation `
  --glob "*.razor" `
  --glob "*.cs" `
  --glob "*.js" `
  --glob "*.css" `
  --glob "!bin/**" `
  --glob "!obj/**"
```

- [x] Confirm Presentation does not own theme assets:

```powershell
Get-ChildItem -Recurse BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation `
  -Include *.css,tailwind.config.*,postcss.config.*,*.scss,*.sass,*.less,*.woff,*.woff2,*.ttf,*.otf,*.png,*.jpg,*.jpeg,*.svg `
  -File
```

- [x] Record exact matches in the implementation notes of this file before changing source.
- [x] Treat these files as expected offenders:
  - `Pages/WasmHost/Account/AccountRoutePage.razor`
  - `Services/Content/StorefrontPagePresentationResolver.cs`
  - `Services/Checkout/StorefrontPaymentResultPageContext.cs`
  - `Services/Checkout/StorefrontPaymentResultPageService.cs`
- [x] If additional Presentation visual class ownership is found, classify it before editing:
  - visual class hardcoded in Presentation: fix in this phase;
  - host-provided class parameter defaulted to `string.Empty`: allowed;
  - semantic CSS hook such as `data-*` or neutral classless markup: allowed;
  - browser behavior script with no theme values: out of scope unless it owns visual values.
- [x] Do not proceed if an additional offender changes the required architecture decision. Update this plan first.

Acceptance:

- [x] Baseline scan is captured from current source.
- [x] Scope remains Presentation neutrality plus V2 visual mapping relocation.
- [x] No backend/runtime/client/storefront-builder files are included in the edit list.

Implementation notes:

- 2026-08-09: focused Presentation class-token scan found the expected offenders in `Pages/WasmHost/Account/AccountRoutePage.razor`, `Services/Content/StorefrontPagePresentationResolver.cs`, and `Services/Checkout/StorefrontPaymentResultPageService.cs`.
- 2026-08-09: the same scan found allowed false positives for route/cookie strings only: `StorefrontCookieNames.Cart = "my-cart"`, `StorefrontRoutePatterns.Cart = "/my-cart"`, `StorefrontRoutes.Cart = "/my-cart"`, and `CartRoutePage.razor` route `@page "/my-cart"`.
- 2026-08-09: inline-style/theme-value scan over Presentation `.razor`, `.cs`, `.js`, and `.css` returned no matches.
- 2026-08-09: theme asset scan returned no Presentation-owned CSS, Tailwind/PostCSS config, font, image, or SVG assets.
- 2026-08-09: scope remains limited to Presentation neutrality plus V2-local visual mapping relocation; no backend, Runtime, Client, Browser, Components, Starter, StorefrontBuilder, Control Plane, or database files are included.

## Phase 1 - Remove Account Unauthorized Visual Markup From Presentation

Purpose: account route shell can redirect, but its fallback markup must not define V2 layout or theme.

Files:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages/WasmHost/Account/AccountRoutePage.razor`

Tasks:

- [x] Replace the unauthorized fallback wrapper with neutral markup.
- [x] Remove all Tailwind/layout/text classes from the fallback:
  - `mx-auto`
  - `max-w-7xl`
  - `px-*`
  - `py-*`
  - `sm:*`
  - `lg:*`
  - `text-sm`
  - `text-neutral-*`
- [x] Keep a semantic hook if useful, for example:

```razor
<section data-storefront-account-redirect>
    <h1>Account</h1>
    <p>Redirecting to sign in...</p>
</section>
```

- [x] Preserve existing redirect behavior:
  - server-side redirect through `HttpContext.Response.Redirect(redirect.Url)` when possible;
  - browser fallback through `NavigationManager.NavigateTo(redirect.Url, replace: true)`;
  - `_state` remains `StorefrontPageState.UnauthorizedState`.
- [x] Do not introduce V2-specific account unauthorized view in Presentation.
- [x] Only add a V2 view if the redirect fallback is proven visibly persistent in browser QA. If added, V2 owns its classes and is wired through an existing foundation view slot.

Acceptance:

- [x] `AccountRoutePage.razor` contains no Tailwind or theme class tokens.
- [x] `/account` still redirects unauthorized users to sign-in.
- [x] Authorized account route still renders the host-provided `ViewSet.AccountPage`.

Implementation notes:

- 2026-08-09: replaced the unauthorized fallback with classless semantic markup using `data-storefront-account-redirect`.
- 2026-08-09: removed the `mx-auto`, `max-w-7xl`, `px-*`, `py-*`, `sm:*`, `lg:*`, `text-sm`, and `text-neutral-*` classes from `AccountRoutePage.razor`.
- 2026-08-09: source scan confirmed no `class=` or Tailwind/layout tokens remain in `AccountRoutePage.razor`; `HttpContext.Response.Redirect`, `NavigationManager.NavigateTo(..., replace: true)`, `_state = UnauthorizedState`, and `StorefrontFoundationViewOutlet` for `ViewSet.AccountPage` remain unchanged.

## Phase 2 - Move Content Page Visual Classes Out Of Presentation

Purpose: Presentation can decide content semantics, but V2 must decide how those semantics look.

Files:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Content/StorefrontPagePresentationResolver.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Ssr/Content/StorefrontPage.razor`
- Tests under `BlazorShop.Tests.V2/PresentationV2/Storefront/`

Tasks:

- [x] Change `StorefrontPagePresentation` so it no longer exposes concrete visual classes:
  - remove `ArticleClass`;
  - remove `BodyContainerClass`.
- [x] Keep semantic fields:
  - `TemplateKey`;
  - `LayoutKind`;
  - `StructuredDataKind`;
  - `FaqEntries`;
  - `Eyebrow`.
- [x] If V2 needs more than `LayoutKind` for mapping, add a semantic field only, for example:
  - `ContentContainerKind`;
  - `PresentationVariant`;
  - or reuse `TemplateKey`.
- [x] Do not add `Class`, `CssClass`, `Tailwind`, `Style`, or host-specific naming to the Presentation contract.
- [x] Update `StorefrontPagePresentation.Standard`, `Policy`, `Faq`, and `Support` factories to return semantic data only.
- [x] Update resolver tests so they assert semantic mapping:
  - page key normalization;
  - known policy keys map to `StorefrontPageLayoutKind.Policy`;
  - `faq` maps to `StorefrontPageLayoutKind.Faq`;
  - `customer_service` maps to `StorefrontPageLayoutKind.Support`;
  - unknown keys map to standard;
  - no expectation on CSS class strings remains.
- [x] Update V2 `StorefrontPage.razor` to map semantic fields to V2-local classes:

```csharp
private static string GetArticleClass(StorefrontPageLayoutKind layoutKind)
{
    return layoutKind switch
    {
        StorefrontPageLayoutKind.Policy => "bs-storefront-content-page bs-storefront-content-page--policy",
        StorefrontPageLayoutKind.Faq => "bs-storefront-content-page bs-storefront-content-page--faq",
        StorefrontPageLayoutKind.Support => "bs-storefront-content-page bs-storefront-content-page--support",
        _ => "bs-storefront-content-page bs-storefront-content-page--standard",
    };
}
```

- [x] Put `BodyContainerClass` equivalent mapping in V2-local code:
  - standard keeps the previous standard V2 class;
  - policy/faq/support keep the previous compact V2 class.
- [x] Preserve existing V2 data hooks:
  - `data-storefront-page-template`;
  - `data-storefront-page-layout`.
- [x] Do not change content route resolution, page body rendering, breadcrumb behavior, SEO, structured data, or page publish/store visibility behavior.

Acceptance:

- [x] `StorefrontPagePresentationResolver.cs` contains no Tailwind utility strings.
- [x] `StorefrontPagePresentation` has no `*Class` property.
- [x] V2 content page still renders the same standard/policy/faq/support visual variants.
- [x] Existing content page resolver/composition tests pass after expectation updates.

Implementation notes:

- 2026-08-09: removed `ArticleClass` and `BodyContainerClass` from `StorefrontPagePresentation`; `Standard`, `Policy`, `Faq`, and `Support` now return semantic values only.
- 2026-08-09: V2 `Pages/Ssr/Content/StorefrontPage.razor` now maps `StorefrontPageLayoutKind` through local `GetArticleClass` and `GetBodyContainerClass` helpers while preserving previous standard/policy/faq/support visual class strings and existing `data-storefront-page-template` / `data-storefront-page-layout` hooks.
- 2026-08-09: resolver tests now assert semantic `TemplateKey`, `LayoutKind`, `StructuredDataKind`, `FaqEntries`, and `Eyebrow` values instead of CSS classes.
- 2026-08-09: first focused resolver test run caught a compile import issue for `StorefrontPageLayoutKind`; adding the correct Presentation services using in V2 fixed it.
- 2026-08-09: `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontPagePresentationResolverTests"` passed 12/12. Existing warnings: MessagePack NU1902/NU1903 and Browserslist.
- 2026-08-09: source scan confirmed `ArticleClass` and `BodyContainerClass` no longer exist in Presentation contracts; remaining matching names are V2-local helper names only.

## Phase 3 - Move Payment Result Visual Tone Out Of Presentation

Purpose: Presentation can determine payment result state, but V2 must own visual tone classes.

Files:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Checkout/StorefrontPaymentResultPageContext.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Checkout/StorefrontPaymentResultPageService.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/PaymentResultPage.razor`
- Tests under `BlazorShop.Tests.V2/PresentationV2/Storefront/`

Tasks:

- [x] Add a semantic result enum in Presentation, for example:

```csharp
public enum StorefrontPaymentResultOutcome
{
    Success,
    Pending,
    Failed,
    Cancelled,
    Unavailable,
}
```

- [x] Replace visual class fields in `StorefrontPaymentResultPageContext` with the semantic outcome:
  - remove `PanelClass`;
  - remove `EyebrowClass`;
  - remove `HeadingClass`;
  - remove `BodyClass`;
  - remove `MutedClass`;
  - add `StorefrontPaymentResultOutcome Outcome`.
- [x] Keep existing non-visual fields:
  - `IsCancelRoute`;
  - `PaymentAttemptId`;
  - `Provider`;
  - `Attempt`;
  - `LoadError`;
  - `Eyebrow`;
  - `Heading`;
  - `Body`;
  - `IsPending`;
  - `IsSuccess`;
  - `ShowRetry`;
  - `Links`;
  - `HasAttempt`.
- [x] Update `StorefrontPaymentResultPageService.CreateContext` to compute `Outcome` only:
  - cancel route with no attempt: `Cancelled` or `Unavailable`, depending on the chosen semantic;
  - cancel route with failed attempt: `Cancelled` or `Failed`;
  - successful attempt: `Success`;
  - pending attempt or pending load state: `Pending`;
  - failed/not-found/load-error state: `Failed` or `Unavailable`.
- [x] Preserve current text and behavioral decisions:
  - retry visible for non-success;
  - success and pending detection rules;
  - load error messages;
  - provider/attempt display;
  - links.
- [x] Update V2 `PaymentResultPage.razor` to map `Context.Outcome` to V2-local classes:
  - panel class;
  - eyebrow tone class;
  - heading tone class;
  - body tone class;
  - muted tone class.
- [x] Keep the visible markup and route behavior materially equivalent.
- [x] Do not touch payment provider registry, payment attempt state machine, checkout place-order, return/cancel endpoints, or order placement.

Acceptance:

- [x] Presentation payment result service contains no Tailwind panel/tone class strings.
- [x] `StorefrontPaymentResultPageContext` contains no visual class properties.
- [x] V2 payment result page still renders success/pending/failure/cancel visual states.
- [x] Payment result tests prove semantic outcome selection.

Implementation notes:

- 2026-08-09: added `StorefrontPaymentResultOutcome` with `Success`, `Pending`, `Failed`, `Cancelled`, and `Unavailable`.
- 2026-08-09: removed `PanelClass`, `EyebrowClass`, `HeadingClass`, `BodyClass`, and `MutedClass` from `StorefrontPaymentResultPageContext`; existing text, state booleans, retry, provider/attempt, load-error, and link fields remain.
- 2026-08-09: `StorefrontPaymentResultPageService` now computes only semantic `Outcome`. Cancel routes keep `Cancelled` to preserve the previous amber cancel visual state; non-cancel success/pending/failed/unavailable states map by attempt/load result.
- 2026-08-09: V2 `PaymentResultPage.razor` maps `Context.Outcome` through V2-local panel and tone helpers while keeping visible markup materially equivalent.
- 2026-08-09: added `StorefrontPaymentResultPageServiceTests` covering cancelled missing attempt, unavailable missing attempt, success states, pending states, failed state, cancel route with failed attempt, and unavailable load failure.
- 2026-08-09: `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontPaymentResultPageServiceTests"` passed 9/9. Existing warnings: MessagePack NU1902/NU1903 and Browserslist.
- 2026-08-09: source scan confirmed Presentation payment result service contains no Tailwind panel/tone class strings; remaining `HeadingClass` tokens are allowed host-provided checkout field component parameters defaulted to `string.Empty`, not removed payment result context properties.

## Phase 4 - Compile Impact Repair Without Scope Expansion

Purpose: record and fix direct compile fallout only.

Tasks:

- [x] Run source search for removed members:

```powershell
rg -n "ArticleClass|BodyContainerClass|PanelClass|EyebrowClass|HeadingClass|BodyClass|MutedClass" `
  BlazorShop.PresentationV2 `
  BlazorShop.Tests.V2 `
  --glob "*.cs" `
  --glob "*.razor" `
  --glob "!bin/**" `
  --glob "!obj/**"
```

- [x] Update only valid consumers:
  - V2 visual pages map semantic fields to local classes;
  - tests assert semantic fields;
  - Starter only gets compile-only adaptation if it references removed context fields.
- [x] Do not add compatibility aliases unless a public package compatibility issue is documented. This repo is still in dev mode, so removal is preferred over obsolete visual class properties.
- [x] Do not add `string.Empty` compatibility class fields to Presentation just to reduce compile churn.

Acceptance:

- [x] Removed visual class property names no longer exist in active Presentation contracts.
- [x] Any remaining references are V2-local helper names, not Presentation-owned contract fields.
- [x] Compile fallout is fixed without touching unrelated commerce behavior.

Implementation notes:

- 2026-08-09: removed-member source search found only V2-local helper names in content/payment pages, V2/Starter host-provided checkout `HeadingClass` usage, and existing Presentation checkout field component `HeadingClass` parameters defaulted to `string.Empty`.
- 2026-08-09: no compatibility aliases or replacement class fields were added to Presentation contracts.
- 2026-08-09: `dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation\BlazorShop.Storefront.Presentation.csproj --no-restore` passed with 0 warnings/errors.
- 2026-08-09: `dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2\BlazorShop.Storefront.V2.csproj --no-restore` passed with 0 warnings/errors.
- 2026-08-09: `dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\BlazorShop.Storefront.Starter.csproj --no-restore` passed with 0 warnings/errors, confirming no Starter compile-only adaptation was needed.

## Phase 5 - Add Presentation Visual Neutrality Guardrail Tests

Purpose: make the boundary machine-checkable.

Suggested new test file:

`BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontPresentationVisualNeutralityTests.cs`

Tasks:

- [ ] Add a curated source scanner for `BlazorShop.Storefront.Presentation`.
- [ ] Include active source extensions:
  - `.razor`;
  - `.cs`;
  - `.js`;
  - `.css`;
  - `.csproj`.
- [ ] Exclude:
  - `bin`;
  - `obj`;
  - docs;
  - test fixtures;
  - generated output.
- [ ] Assert Presentation `.razor` and `.cs` files do not contain hardcoded Tailwind/theme class tokens such as:
  - `bg-`;
  - `rounded-`;
  - `shadow-`;
  - `ring-`;
  - `border-neutral-`;
  - `border-zinc-`;
  - `border-slate-`;
  - `text-neutral-`;
  - `text-zinc-`;
  - `text-slate-`;
  - `text-red-`;
  - `text-green-`;
  - `text-emerald-`;
  - `text-amber-`;
  - `text-blue-`;
  - `hover:bg-`;
  - `focus:ring-`;
  - `sm:`;
  - `md:`;
  - `lg:`;
  - `xl:`;
  - `2xl:`.
- [ ] Avoid false positives:
  - do not ban route strings such as `/my-cart`;
  - do not ban cookie names, endpoint names, or `data-*` hooks;
  - do not ban host-provided class parameter names when defaults are `string.Empty`;
  - do not ban behavior-only `.classList` usage such as toggling `hidden` if current source needs it.
- [ ] Add inline style guard for Presentation Razor:
  - reject `style="background`;
  - reject `style="color`;
  - reject `style="font`;
  - reject `style="padding`;
  - reject `style="margin`;
  - reject `style="box-shadow`;
  - allow no current inline visual style exceptions unless the implementation records one.
- [ ] Add CSS/theme asset ownership guard:
  - no `wwwroot/css/site.css`;
  - no `wwwroot/css/theme.css`;
  - no `wwwroot/css/storefront.css`;
  - no `tailwind.config.*`;
  - no `postcss.config.*`;
  - no font files;
  - no theme image assets.
- [ ] Add positive assertions:
  - `StorefrontPagePresentationResolver.cs` does not contain `Class` properties or Tailwind strings;
  - `StorefrontPaymentResultPageService.cs` does not contain Tailwind tone/panel strings;
  - account route unauthorized fallback is classless or semantic-only;
  - existing Presentation form class parameters default to `string.Empty`.
- [ ] Keep `StorefrontVisualSourceOwnershipTests` for V2/V2.WASM source ownership. Do not merge the new Presentation guard into that test if it makes ownership unclear.

Acceptance:

- [ ] Guardrail fails if Presentation reintroduces Tailwind visual class strings.
- [ ] Guardrail fails if Presentation owns theme CSS/assets.
- [ ] Guardrail does not fail on valid route/cookie/endpoint strings.
- [ ] Guardrail names the exact offending file and token in failure output.

## Phase 6 - Strengthen V2 Ownership Tests For Relocated Mapping

Purpose: prove visual mapping moved to the correct host, not just disappeared.

Tasks:

- [ ] Extend existing V2 tests or add focused assertions that V2 owns content visual mapping:
  - `StorefrontPage.razor` maps `StorefrontPageLayoutKind` or semantic equivalent to article/body classes locally;
  - V2 keeps the previous standard/policy/faq/support visual variants.
- [ ] Extend existing V2 tests or add focused assertions that V2 owns payment visual mapping:
  - `PaymentResultPage.razor` maps `StorefrontPaymentResultOutcome` to panel and tone classes locally;
  - success, pending, failed/cancelled/unavailable outcomes have distinct V2 visual classes.
- [ ] Keep tests source-level and focused unless existing component-render tests already exist.
- [ ] Do not require Starter to match V2 visual output.

Acceptance:

- [ ] V2 tests prove the moved visual mappings exist in V2.
- [ ] Presentation tests prove those mappings do not exist in Presentation.
- [ ] The tests make ownership readable for future agents.

## Phase 7 - QA Checklist And Architecture Notes

Purpose: update release QA so production readiness checks include this boundary.

Files:

- `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- `docs/architecture/05-project-and-folder-guide.md` only if terminology needs tightening

Tasks:

- [ ] Add a `Storefront Presentation Visual Neutrality` section to `QA-StorefrontV2.todo.md`.
- [ ] Checklist must include:
  - Presentation has no concrete V2/Tailwind theme class strings;
  - Presentation has no theme CSS/assets;
  - account redirect fallback is classless or semantic-only;
  - content page class mapping is V2-owned;
  - payment result tone mapping is V2-owned;
  - focused guardrail tests pass;
  - browser checks confirm content page, payment result page, and account redirect behavior.
- [ ] Do not add Control Plane or StorefrontBuilder checklist items for this phase.
- [ ] Review `docs/architecture/05-project-and-folder-guide.md`.
- [ ] Update architecture docs only if the current Presentation/V2 ownership language is not explicit enough after implementation.
- [ ] If docs are updated, keep the wording narrow:
  - Presentation may expose semantic variants and state;
  - host visual projects own class strings and styling;
  - generated/custom storefronts are not required to reuse V2 class mappings.

Acceptance:

- [ ] Release checklist describes how to verify the closure.
- [ ] Docs do not imply Starter/generated storefronts must adopt V2 styling.

## Phase 8 - Focused Build And Test Gate

Purpose: prove compile and focused architecture behavior after contract cleanup.

Run builds:

```powershell
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation\BlazorShop.Storefront.Presentation.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2\BlazorShop.Storefront.V2.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2.WASM\BlazorShop.Storefront.V2.WASM.csproj --no-restore
```

Run focused tests:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontPresentationVisualNeutralityTests|FullyQualifiedName~StorefrontVisualSourceOwnershipTests|FullyQualifiedName~StorefrontPagePresentationResolverTests|FullyQualifiedName~StorefrontPageCompositionGuardrailTests|FullyQualifiedName~StorefrontPresentationCutoverGuardrailTests|FullyQualifiedName~LayoutAssetFoundationTests|FullyQualifiedName~StorefrontV2HostSmokeTests"
```

If the filter misses renamed tests:

- [ ] use `rg -n "PresentationVisual|VisualSource|PagePresentation|PaymentResult|HostSmoke|LayoutAsset" BlazorShop.Tests.V2`;
- [ ] rerun the closest exact Storefront V2 filters;
- [ ] record the actual executed filters in implementation notes.

Acceptance:

- [ ] Presentation build passes.
- [ ] V2 build passes.
- [ ] V2.WASM build passes.
- [ ] Focused tests pass.
- [ ] Known unrelated warnings are documented, not hidden.

## Phase 9 - Browser Regression Gate

Purpose: this phase changes V2 class ownership for user-facing pages, so browser checks must exercise real DOM behavior.

Start local V2 runtime:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser
```

Playwright checks:

- [ ] Content page route renders with V2 styling:
  - load at least one known content/page route from seeded data;
  - verify article exists with `data-storefront-page-template`;
  - verify body container has V2-owned styling;
  - verify no console/page errors.
- [ ] Payment result page renders with V2 styling:
  - load a success/pending/failure/cancel route if fixture data exists;
  - if no payment fixture is available, load a missing/invalid attempt route to verify unavailable/failure state;
  - verify panel exists and has V2-owned visual class;
  - verify retry link visibility for non-success;
  - verify no 5xx response.
- [ ] Account unauthorized route still redirects:
  - open `/account` as anonymous;
  - verify redirect or sign-in destination behavior remains unchanged;
  - verify there is no persistent unstyled technical state;
  - verify no console/page errors.
- [ ] Browser network guard remains unchanged:
  - no direct browser calls to Commerce Node Storefront APIs;
  - no direct calls to Control Plane or Commerce Admin routes;
  - no legacy `api/internal/*` calls.

Acceptance:

- [ ] Browser QA is a real Playwright flow, not a smoke-only page load.
- [ ] Content page and payment result visual output remain usable.
- [ ] Account redirect behavior remains intact.
- [ ] Same-origin browser boundary remains intact.

## Phase 10 - Final Closure Scan And Diff Review

Purpose: close the phase with objective evidence and a small diff.

Run final Presentation scan:

```powershell
rg -n "bg-|text-neutral-|text-zinc-|text-slate-|text-red-|text-green-|text-emerald-|text-amber-|text-blue-|border-|shadow-|rounded-|ring-|hover:|focus:|sm:|md:|lg:|xl:|2xl:|grid-cols-|gap-|px-|py-|mx-|my-|max-w-|min-h-|ArticleClass|BodyContainerClass|PanelClass|EyebrowClass|HeadingClass|BodyClass|MutedClass" `
  BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation `
  --glob "*.razor" `
  --glob "*.cs" `
  --glob "!bin/**" `
  --glob "!obj/**"
```

Expected:

- [ ] no V2/Tailwind visual class strings in Presentation;
- [ ] no removed visual class contract fields in Presentation;
- [ ] no concrete payment tone classes in Presentation;
- [ ] no content body/article class strings in Presentation.

Run final theme asset scan:

```powershell
Get-ChildItem -Recurse BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation `
  -Include *.css,tailwind.config.*,postcss.config.*,*.scss,*.sass,*.less,*.woff,*.woff2,*.ttf,*.otf,*.png,*.jpg,*.jpeg,*.svg `
  -File
```

Expected:

- [ ] no Presentation-owned theme CSS, Tailwind config, font, or image assets;
- [ ] existing behavior script files remain allowed if they do not own visual theme values.

Run scoped diff review:

```powershell
git diff -- `
  BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation `
  BlazorShop.PresentationV2/BlazorShop.Storefront.V2 `
  BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM `
  BlazorShop.Tests.V2 `
  docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md `
  docs/refactor-control-Commerce-storefront/Storefront\ Presentation\ Visual\ Neutrality\ Closure.todo.md
```

Acceptance:

- [ ] Diff is limited to this plan's source, tests, and QA docs.
- [ ] Any extra file has a recorded reason.
- [ ] No backend, API, Runtime, Client, Browser, Components, Starter, StorefrontBuilder, Control Plane, or database files changed.

## Definition Of Done

- [ ] `Storefront.Presentation` no longer contains V2/Tailwind visual class strings for account redirect fallback, content page presentation, or payment result state.
- [ ] `StorefrontPagePresentation` exposes semantic presentation data only and has no `ArticleClass` or `BodyContainerClass`.
- [ ] `StorefrontPaymentResultPageContext` exposes semantic outcome/state only and has no visual class properties.
- [ ] V2 owns content page class mapping locally.
- [ ] V2 owns payment result outcome-to-class mapping locally.
- [ ] Account unauthorized fallback in Presentation is classless or semantic-only.
- [ ] Presentation visual neutrality guardrail tests exist and pass.
- [ ] Existing V2 visual source ownership guardrails still pass.
- [ ] `QA-StorefrontV2.todo.md` includes this closure check.
- [ ] Focused builds and tests pass.
- [ ] Playwright verifies content page, payment result page, account redirect, and network boundary behavior.

## Decision Audit Trail

| # | Decision | Classification | Rationale | Rejected |
|---|---|---|---|---|
| 1 | Fix verified Presentation offenders instead of closing with docs/tests only. | Auto-decided | Source scan shows real V2/Tailwind class ownership in Presentation. | Treating current state as already closed. |
| 2 | Move class mapping to V2, not to a new shared visual registry. | Auto-decided | There is one V2 visual host needing these classes; a registry would add indirection without reuse. | Generic theme registry or shared design-system abstraction. |
| 3 | Use semantic fields such as `LayoutKind`, `TemplateKey`, and `PaymentResultOutcome` in Presentation. | Auto-decided | Semantic contracts preserve behavior while keeping host visual control. | `Class`, `CssClass`, `TailwindClass`, or `Style` fields in Presentation. |
| 4 | Remove visual class contract fields outright during dev mode. | Auto-decided | The repo is still pre-public package/API for this surface, and removal prevents accidental reuse. | Obsolete aliases or `string.Empty` compatibility fields. |
| 5 | Keep Starter and StorefrontBuilder out of scope except compile-only fallout. | Auto-decided | The blocker is V2/Presentation boundary closure, not generated storefront visual normalization. | Reopening Starter/generated visual contracts. |
| 6 | Add source guardrails with curated exclusions. | Auto-decided | Broad grep can false-positive on routes, generated CSS, or behavior hooks. | One-off manual review without tests. |
