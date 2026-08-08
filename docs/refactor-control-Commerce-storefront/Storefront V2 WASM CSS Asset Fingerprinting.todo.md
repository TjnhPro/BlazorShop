# Storefront V2 WASM CSS Asset Fingerprinting

## Goal

Make Storefront V2 and Storefront V2.WASM static visual assets reproducible, project-owned, and fingerprint-resolved without widening scope to Starter, generated storefronts, Control Plane, or Commerce Node.

This phase fixes the current mismatch:

- `BlazorShop.Storefront.V2` has a Tailwind pipeline, but it scans only the V2 project.
- `BlazorShop.Storefront.V2.WASM` owns interactive cart, checkout, and account visual classes, but has no Tailwind pipeline or CSS static asset of its own.
- V2 host head/script entries still use raw paths such as `css/site.css`, `css/storefront.css`, and `js/storefrontCommerce.js`.
- Docker currently runs `npm run tailwind:build` only for V2, so a future V2.WASM CSS file would not be generated in the container image unless Docker is updated.

## Scope

In scope:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM`
- `BlazorShop.Tests.V2` tests that guard Storefront V2 assets, CSS ownership, Docker/publish behavior, and WASM visual classes.
- `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- optional helper scripts under `scripts/qa` if a reusable CSS reproducibility gate is needed.

Out of scope:

- Storefront Starter CSS pipeline.
- Generated storefront CSS pipeline.
- StorefrontBuilder visual generation.
- Merging V2 and V2.WASM Tailwind bundles.
- Shared Tailwind config package.
- CDN/cache policy redesign.
- Runtime asset registry.
- Store-configured arbitrary scripts/styles.
- Redesigning cart, checkout, account, product, or gallery UI.

## Codebase Evidence To Preserve

- V2 `tailwind.config.js` currently scans only `./**/*.razor`, `./**/*.cshtml`, `./**/*.html`, and `./**/*.cs`.
- V2 `package.json` already defines `tailwind:build` and `tailwind:dev`.
- V2.WASM currently has `Components`, `_Imports.razor`, `Program.cs`, and `BlazorShop.Storefront.V2.WASM.csproj`, but no `package.json`, `tailwind.config.js`, or `wwwroot/css`.
- V2.WASM visual option files contain Tailwind classes in C# strings, so V2.WASM Tailwind content scanning must include `.cs` files, not only `.razor`.
- V2 `StorefrontApplicationHead.razor` still links raw `css/site.css` and `css/storefront.css`.
- V2 `StorefrontApplicationScripts.razor` still links raw `js/storefrontCommerce.js`.
- Storefront Presentation maps static assets through `MapStaticAssets()`.
- `LayoutAssetFoundationTests` currently assert the V2 raw asset inventory and must be updated when assets move to `@Assets[...]`.
- `QA-StorefrontV2.todo.md` already records a real prior regression where Tailwind utilities were missing from `site.css` and product gallery sizing had to be moved to `storefront.css`.

## Architecture Decision

Use ASP.NET Core static web assets and Razor `@Assets[...]` resolution for V2/V2.WASM assets. Do not add a custom query-string versioning service.

CSS ownership:

- V2 owns V2 layout/global visual CSS in `BlazorShop.Storefront.V2/wwwroot/css/site.css`.
- V2.WASM owns interactive WASM visual CSS in `BlazorShop.Storefront.V2.WASM/wwwroot/css/site.css`.
- V2 `wwwroot/css/storefront.css` remains a handwritten V2 host override/structural stylesheet and loads after generated Tailwind CSS.

Final intended root load order:

1. V2 generated Tailwind CSS.
2. V2.WASM generated Tailwind CSS.
3. V2 handwritten `storefront.css`.

Do not hard-code the V2.WASM CSS logical asset path until the static web asset manifest confirms it after adding `wwwroot/css/site.css`.

## Phase 0 - Baseline And Guardrail Snapshot

- [x] Confirm the workspace has no unrelated V2/V2.WASM asset changes that would be mixed into this phase.
- [x] Read:
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/tailwind.config.js`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/package.json`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontApplicationHead.razor`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontApplicationScripts.razor`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Dockerfile`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj`
  - [x] V2.WASM `Components/Account`, `Components/Cart`, and `Components/Checkout` option files.
  - [x] `BlazorShop.Tests.V2/PresentationV2/LayoutAssetFoundationTests.cs`
  - [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontV2HostSmokeTests.cs`
  - [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontV2WASMRuntimeFoundationTests.cs`
- [x] Record current raw asset assumptions that must be changed:
  - [x] `css/site.css`
  - [x] `css/storefront.css`
  - [x] `js/storefrontCommerce.js`
- [x] Record current Docker behavior:
  - [x] V2 package files are copied.
  - [x] V2 `npm ci` runs.
  - [x] V2 `npm run tailwind:build` runs.
  - [x] V2.WASM package files do not exist yet and are not built.
- [x] Confirm V2.WASM class inventory includes Tailwind classes in `.cs` files.
- [x] Confirm no phase requirement needs Starter or generated storefront changes.

Definition of done:

- [x] Baseline notes in the PR/commit message identify the current raw URL and missing V2.WASM CSS pipeline cause.
- [x] No code changes have been made outside the approved scope.

## Phase 1 - Add V2.WASM Tailwind Pipeline

- [x] Add `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/package.json`.
  - [x] Use the same Tailwind major/minor baseline as V2 unless there is a documented reason to differ.
  - [x] Add `tailwind:build`.
  - [x] Add `tailwind:dev` only if useful for local WASM visual iteration.
- [x] Add `package-lock.json` by running `npm install` once in the V2.WASM project directory.
- [x] Add `tailwind.config.js`.
  - [x] Include `./**/*.razor`.
  - [x] Include `./**/*.cs`.
  - [x] Include `./**/*.html` if static fragments are possible.
  - [x] Do not scan `../BlazorShop.Storefront.V2`.
  - [x] Do not scan Starter or generated storefront folders.
- [x] Add `wwwroot/css/input.css`.
  - [x] Use the standard Tailwind directives.
  - [x] Keep V2.WASM-specific custom CSS minimal.
  - [x] Do not duplicate V2 `storefront.css`.
- [x] Generate `wwwroot/css/wasm-site.css` from the new V2.WASM pipeline. `wwwroot/css/site.css` was rejected after host build proved hosted WASM static assets merge at root and collide with V2 `css/site.css`.
- [x] Build V2.WASM after adding static assets:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj --no-restore
```

- [x] Inspect static web asset build output for the exact logical path of V2.WASM CSS.
  - [x] Check `obj/**/staticwebassets*.json` or endpoint manifest.
  - [x] Record whether the logical path is `_content/BlazorShop.Storefront.V2.WASM/css/site.css` or another value.
  - [x] Use the verified value in V2 host head composition.

Tests to add or update:

- [x] Add a test that V2.WASM has its own Tailwind pipeline files.
- [x] Add a test that V2.WASM Tailwind config scans `.cs` files.
- [x] Add a test that V2.WASM Tailwind config does not scan V2, Starter, generated storefronts, Control Plane, Commerce Node, or backend/core projects.
- [x] Add a test that V2.WASM `wwwroot/css/wasm-site.css` exists and is non-empty after build.

Definition of done:

- [x] V2.WASM owns a buildable CSS artifact.
- [x] V2.WASM visual classes no longer depend on accidental inclusion in V2 `site.css`.
- [x] Static web asset logical path is verified before being used by V2 host.

## Phase 2 - Convert V2 Host Assets To Fingerprint-Resolved Static Assets

- [x] Update `StorefrontApplicationHead.razor`.
  - [x] Replace raw V2 CSS link with `@Assets[...]`.
  - [x] Add the verified V2.WASM CSS asset via `@Assets[...]`.
  - [x] Keep `StorefrontIconHead` and `StorefrontBrandHead` before `HeadOutlet` behavior intact.
  - [x] Preserve final load order:
    - [x] V2 `site.css`
    - [x] V2.WASM `wasm-site.css`
    - [x] V2 `storefront.css`
- [x] Update `StorefrontApplicationScripts.razor`.
  - [x] Replace raw `js/storefrontCommerce.js` with `@Assets[...]`.
  - [x] Do not change the broader script ordering controlled by Storefront Presentation.
  - [x] Keep V2 `storefrontCommerce.js` visual-only.
- [x] Do not add custom `?v=` query strings.
- [x] Do not add a custom version provider.
- [x] Do not move root asset composition into layout-level `HeadContent`.
- [x] Do not make asset paths store-configurable.

Tests to update:

- [x] Update `LayoutAssetFoundationTests.StorefrontRoot_DefinesExpectedAssetsWithoutDuplicates`.
  - [x] It should no longer require raw `href="css/site.css"` style values if source now uses `@Assets[...]`.
  - [x] It should assert the three intended stylesheet entries exactly once.
  - [x] It should assert script entry for V2 `storefrontCommerce.js` exactly once.
  - [x] It should preserve existing framework and Presentation script ordering expectations.
- [x] Update `StorefrontV2HostSmokeTests`.
  - [x] Replace raw asset string expectations with fingerprint/static-asset-compatible expectations.
  - [x] Ensure host HTML still includes required V2/V2.WASM CSS and V2 script.
- [x] Update any branding/head tests that read `StorefrontApplicationHead.razor` directly.

Definition of done:

- [x] V2 host no longer relies on raw static asset URLs for root V2 assets.
- [x] Browser receives framework-resolved/fingerprint-capable URLs.
- [x] Root asset inventory remains explicit and allowlisted by tests.

## Phase 3 - CSS Reproducibility Gate

- [ ] Create a focused reproducibility script only if existing test infrastructure cannot cover byte-for-byte CSS regeneration.
  - Preferred path: `scripts/qa/run-storefront-v2-css-reproducibility.ps1`.
- [ ] The gate must run from repository root and verify both projects:
  - [ ] `BlazorShop.Storefront.V2`
  - [ ] `BlazorShop.Storefront.V2.WASM`
- [ ] For each project:
  - [ ] Run `npm ci`.
  - [ ] Run the project-local `tailwind:build`.
  - [ ] Compare generated `wwwroot/css/site.css` against tracked file content.
  - [ ] Fail if regenerated CSS differs.
  - [ ] Print the exact project and file that drifted.
- [ ] Avoid writing candidate output over tracked CSS unless the script is explicitly intended to refresh CSS.
- [ ] Use deterministic temp output under `obj/storefront-css-proof/...` if comparing without overwriting tracked files.
- [ ] Ensure temp paths are unique per project:
  - [ ] `obj/storefront-css-proof/v2/site.css`
  - [ ] `obj/storefront-css-proof/v2-wasm/site.css`
- [ ] Clean temp output after success unless a failure needs evidence.
- [ ] Add documentation in the script header explaining:
  - [ ] why `dotnet build` alone is not treated as CSS generation;
  - [ ] why V2 and V2.WASM have separate CSS ownership;
  - [ ] how to intentionally refresh CSS.

Tests to add or update:

- [ ] Add test coverage that the reproducibility script exists and targets both V2 and V2.WASM.
- [ ] Add test coverage that V2 and V2.WASM package-lock files are present.
- [ ] Add test coverage that `tailwind:build` commands write only each project's own `wwwroot/css/site.css`.

Definition of done:

- [ ] A developer or agent can prove CSS is current without guessing.
- [ ] Stale `site.css` becomes a deterministic failing gate.
- [ ] The gate does not silently rewrite project files during normal verification.

## Phase 4 - Docker And Publish Path

- [ ] Update `BlazorShop.Storefront.V2/Dockerfile`.
  - [ ] Copy V2.WASM `package.json` before restore/build if Docker layer caching should preserve Node dependency restore.
  - [ ] Copy V2.WASM `package-lock.json`.
  - [ ] Run `npm ci` in V2.WASM.
  - [ ] Run `npm run tailwind:build` in V2.WASM before `dotnet publish`.
  - [ ] Keep existing V2 Tailwind build behavior.
  - [ ] Avoid installing global Node packages.
  - [ ] Avoid using `npm install` in Docker.
- [ ] Ensure Docker copy order still allows `dotnet restore` before full source copy where possible.
- [ ] Ensure the final published app includes:
  - [ ] V2 generated CSS.
  - [ ] V2.WASM generated CSS.
  - [ ] V2 handwritten `storefront.css`.
  - [ ] V2 `storefrontCommerce.js`.
- [ ] Do not change Commerce Node deployment behavior in this phase.

Tests to add or update:

- [ ] Update `V2ProductionReadinessTests` or another focused architecture test to assert Docker builds V2.WASM Tailwind CSS.
- [ ] Add publish/static web asset test if existing tests do not prove V2.WASM CSS is in the publish/static asset manifest.
- [ ] Keep existing assertion that Docker references `BlazorShop.Storefront.V2.WASM.csproj`.

Definition of done:

- [ ] Local verification and Docker publish path generate the same V2/V2.WASM CSS ownership shape.
- [ ] Container image cannot accidentally ship stale or missing V2.WASM CSS.

## Phase 5 - Browser QA For Real Layout, Not Just Asset 200

- [ ] Run V2 locally through the standard V2 local runner if possible:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

- [ ] Use Playwright browser QA against Storefront V2.
- [ ] Verify asset behavior:
  - [ ] V2 CSS returns HTTP 200.
  - [ ] V2.WASM CSS returns HTTP 200.
  - [ ] V2 `storefront.css` returns HTTP 200.
  - [ ] V2 `storefrontCommerce.js` returns HTTP 200.
  - [ ] No duplicate root stylesheet links.
  - [ ] No missing static asset requests.
  - [ ] No browser console errors from static asset resolution or WASM hydration.
- [ ] Verify layout behavior using computed style or measured dimensions, not only string checks:
  - [ ] Cart page/container uses expected max width and card styling from V2.WASM CSS.
  - [ ] Cart line image frame keeps stable dimensions.
  - [ ] Checkout shell keeps expected grid/step styling.
  - [ ] Account app/navigation/profile sections keep expected responsive layout.
  - [ ] Mobile viewport still loads V2.WASM CSS before visual checks.
- [ ] Verify no direct browser calls to Commerce Node are introduced.
- [ ] Capture evidence:
  - [ ] JSON evidence under `output/playwright`.
  - [ ] at least one desktop screenshot for cart/account or checkout.
  - [ ] at least one mobile screenshot for account/cart or checkout.

Definition of done:

- [ ] QA proves CSS affected real browser layout.
- [ ] QA would catch missing Tailwind utilities in V2.WASM components.
- [ ] QA evidence is referenced in `QA-StorefrontV2.todo.md`.

## Phase 6 - Documentation And QA Checklist Update

- [ ] Update `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`.
  - [ ] Add a new checklist item for V2.WASM CSS static asset presence.
  - [ ] Add a new checklist item for fingerprint/static-asset-resolved root links.
  - [ ] Add a new checklist item for CSS reproducibility gate.
  - [ ] Add a new checklist item for Docker V2.WASM Tailwind build.
  - [ ] Add browser QA evidence paths when completed.
- [ ] Update architecture docs only if implementation changes an architecture rule.
  - [ ] If root asset policy changes, update `docs/architecture/08-agent-decision-rules.md`.
  - [ ] If project ownership wording changes, update `docs/architecture/05-project-and-folder-guide.md`.
  - [ ] Do not update architecture docs just to restate this implementation plan.
- [ ] Add a short implementation note to the PR/commit message:
  - [ ] V2/V2.WASM CSS ownership split.
  - [ ] Asset fingerprint resolution source.
  - [ ] Reproducibility gate command.
  - [ ] Browser QA evidence.

Definition of done:

- [ ] QA checklist tells future agents how to verify the asset behavior.
- [ ] Docs do not imply raw asset URLs are still the intended V2 host behavior.
- [ ] The phase can be audited without re-investigating the whole codebase.

## Phase 7 - Focused Verification Gate

Run focused checks before committing:

```powershell
npm ci --prefix BlazorShop.PresentationV2/BlazorShop.Storefront.V2
npm run tailwind:build --prefix BlazorShop.PresentationV2/BlazorShop.Storefront.V2
npm ci --prefix BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM
npm run tailwind:build --prefix BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM
```

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
```

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~LayoutAssetFoundationTests|FullyQualifiedName~StorefrontV2HostSmokeTests|FullyQualifiedName~StorefrontV2WASMRuntimeFoundationTests|FullyQualifiedName~V2ProductionReadinessTests"
```

If the reproducibility script is added:

```powershell
.\scripts\qa\run-storefront-v2-css-reproducibility.ps1
```

If browser QA is required for closure:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

Then run the Playwright scenario created or updated for this phase.

Definition of done:

- [ ] Focused static tests pass.
- [ ] V2.WASM build passes.
- [ ] V2 host build passes.
- [ ] CSS reproducibility gate passes.
- [ ] Browser QA passes or the blocker is documented with exact failure evidence.

## Final Acceptance Checklist

- [ ] V2.WASM has its own Tailwind pipeline.
- [ ] V2.WASM generated CSS is tracked and reproducible.
- [ ] V2 does not scan V2.WASM source to generate V2 CSS.
- [ ] V2 host loads V2 CSS, V2.WASM CSS, and `storefront.css` in deterministic order.
- [ ] V2 host uses static asset resolver/fingerprint-capable URLs for root V2 assets.
- [ ] V2 visual script uses static asset resolver/fingerprint-capable URL.
- [ ] Docker builds Tailwind for both V2 and V2.WASM.
- [ ] Static tests guard asset inventory and no duplicate root assets.
- [ ] Browser QA validates computed layout for cart/checkout/account WASM surfaces.
- [ ] `QA-StorefrontV2.todo.md` records verification and evidence.
- [ ] No Starter/generated storefront scope was changed.
- [ ] No Control Plane, Commerce Node, Runtime, Client, Browser, or Components package boundaries were widened.
