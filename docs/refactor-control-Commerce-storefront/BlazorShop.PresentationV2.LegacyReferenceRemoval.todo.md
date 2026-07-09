# BlazorShop PresentationV2 Legacy Reference Removal Todo

## Goal

Remove every direct reference from `BlazorShop.PresentationV2` to legacy `BlazorShop.Presentation`.

This plan keeps the V2 boundary intact:

- Control Plane V2 remains independent.
- Commerce Node remains independent.
- Storefront V2 owns its own static assets, Tailwind input, build pipeline, and runtime static-file provider.
- Legacy `BlazorShop.Presentation` can remain in the solution during migration, but `BlazorShop.PresentationV2` must not depend on its files, projects, package manifests, static assets, or service-discovery names.

## Current Findings

Compile-time project references are already clean.

`dotnet list reference` for all `BlazorShop.PresentationV2` projects shows only:

- `BlazorShop.Application`
- `BlazorShop.Infrastructure`
- `BlazorShop.ServiceDefaults`
- `BlazorShop.Web.SharedV2`

No V2 project references these legacy projects:

- `BlazorShop.Presentation/BlazorShop.API`
- `BlazorShop.Presentation/BlazorShop.Web`
- `BlazorShop.Presentation/BlazorShop.Web.Shared`
- `BlazorShop.Presentation/BlazorShop.Storefront`

Remaining direct legacy references are all in `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`:

| File | Reference | Type | Risk |
| --- | --- | --- | --- |
| `BlazorShop.Storefront.V2.csproj` | `..\..\BlazorShop.Presentation\BlazorShop.Web\wwwroot\**\*` | build/static asset copy | V2 build output depends on legacy Web assets. |
| `Program.cs` | `Path.Combine(..., "BlazorShop.Presentation", "BlazorShop.Web", "wwwroot")` | runtime static file provider | V2 can silently load legacy assets at runtime. |
| `Dockerfile` | copies `BlazorShop.Presentation/BlazorShop.Web` and runs Tailwind there | container build dependency | Container cannot build Storefront V2 without legacy Web project files. |
| `App.razor` | `css/site.css` | asset expectation | Currently satisfied by legacy Web copy/build, not V2-owned pipeline. |
| `StorefrontClientAppUrlResolver.cs` / `StorefrontOptionsValidators.cs` | `adminclient` service-discovery key | naming/config coupling | Not a file dependency, but likely legacy Web naming. |

## Definition Of Done

- `rg "BlazorShop\.Presentation|BlazorShop\.Web|BlazorShop\.API|BlazorShop\.Web\.Shared|adminclient" BlazorShop.PresentationV2 --glob '!**/bin/**' --glob '!**/obj/**'` has no unwanted legacy dependency hits.
- `dotnet list reference` for every V2 project remains free of legacy Presentation projects.
- `BlazorShop.Storefront.V2` builds without copying from `BlazorShop.Presentation/BlazorShop.Web`.
- Storefront V2 Dockerfile builds without copying from `BlazorShop.Presentation/BlazorShop.Web`.
- Storefront V2 can run with the legacy `BlazorShop.Presentation` folder temporarily renamed or unavailable.
- Browser QA confirms `/`, `/signin`, `/register`, `/my-cart`, `/product/{slug}`, static CSS, JS, images, and fonts still load.
- No behavior change to Commerce Node or Control Plane except optional service-discovery naming cleanup if used by shared Aspire/apphost config.

## Not In Scope

- Deleting legacy `BlazorShop.Presentation` from the solution.
- Refactoring legacy projects.
- Redesigning Storefront V2 UI.
- Replacing Commerce Node APIs.
- Rebuilding checkout/account pages.
- Changing auth behavior beyond service-discovery naming if required.

## Architecture Decision

Storefront V2 should own its assets directly.

Do not keep a shared filesystem dependency on legacy Web. If an asset is still required, copy the actual file into a V2-owned location and treat it as part of Storefront V2.

Recommended target structure:

```text
BlazorShop.PresentationV2/
  BlazorShop.Storefront.V2/
    package.json
    package-lock.json
    tailwind.config.js
    wwwroot/
      css/
        input.css
        site.css
        storefront.css
      fonts/
        InterVariable.woff2
        Berlin Sans FB Bold.ttf
      font-awesome/
        ...
      images/
        banner-bg.jpg
        favicon.png
        ...
      js/
        storefrontCommerce.js
```

## Phase 1 - Asset Inventory And Ownership

- [x] List every static asset Storefront V2 actually requests at runtime:
  - [x] `css/site.css`
  - [x] `css/storefront.css`
  - [x] `js/storefrontCommerce.js`
  - [x] images referenced by seeded data such as `/images/banner-bg.jpg`
  - [x] fonts referenced by CSS
  - [x] Font Awesome files if still used by V2 UI
- [x] Compare legacy `BlazorShop.Presentation/BlazorShop.Web/wwwroot` with Storefront V2 `wwwroot`.
- [x] Decide asset copy scope:
  - [x] Copy only assets referenced by V2 pages, CSS, seed data, and QA fixtures.
  - [x] Do not copy legacy `index.html`, `appsettings*.json`, auth/session JS, or unused legacy CSS unless V2 explicitly uses them.
- [x] Record a keep/remove table in this plan before implementation.

Inventory result:

| Asset | Current owner | V2 usage | Decision |
| --- | --- | --- | --- |
| `wwwroot/css/site.css` | Legacy generated Tailwind output | Linked by `Storefront.V2/App.razor` | Move build ownership to Storefront V2 and generate locally. |
| `wwwroot/css/input.css` | Legacy Tailwind input | Needed to generate `site.css` | Copy into Storefront V2 and trim only after visual QA. |
| `wwwroot/css/storefront.css` | Storefront V2 | Linked by `Storefront.V2/App.razor` | Keep in Storefront V2. |
| `wwwroot/js/storefrontCommerce.js` | Storefront V2 | Linked by `Storefront.V2/App.razor` | Keep in Storefront V2. |
| `wwwroot/icon-192.png` | Legacy Web | Linked by `Storefront.V2/App.razor` | Copy into Storefront V2. |
| `wwwroot/images/banner-bg.jpg` | Legacy Web | Seed/QA image URL | Copy into Storefront V2. |
| `wwwroot/images/bg1.png` | Missing from legacy `wwwroot/images` | Development seed URL | Do not copy now; track as seed-data cleanup or provide fallback in a later catalog seed task. |
| `wwwroot/images/bg2.png` | Missing from legacy `wwwroot/images` | Development seed URL | Do not copy now; track as seed-data cleanup or provide fallback in a later catalog seed task. |
| `wwwroot/images/bg.png` | Missing from legacy `wwwroot/images` | Development seed URL | Do not copy now; track as seed-data cleanup or provide fallback in a later catalog seed task. |
| `wwwroot/fonts/InterVariable.woff2` | Legacy Web | Referenced by legacy `fonts.css`, not by Storefront V2 directly | Do not copy unless V2 reintroduces local font-face. |
| `wwwroot/fonts/Berlin Sans FB Bold.ttf` | Legacy Web | No V2 reference found | Do not copy. |
| `wwwroot/font-awesome/**` | Legacy Web | No Storefront V2 reference found; V2 uses inline SVGs | Do not copy. |
| `wwwroot/index.html` | Legacy Web | Blazor WASM host only | Do not copy. |
| `wwwroot/appsettings*.json` | Legacy Web | Blazor WASM config only | Do not copy. |
| `wwwroot/js/app.js`, `authSessionSync.js`, `cookieStorage.js`, `interop.js`, `sessionStorage.js`, `cartBadge.js` | Legacy Web | No Storefront V2 reference found | Do not copy. |

Stop gate:

- We know exactly which files need to move into Storefront V2 and which legacy files should stay behind. 2026-07-09: inventory completed with `rg` over Storefront V2, Web.SharedV2, infrastructure seeds, tests, and QA docs.

## Phase 2 - Move Tailwind Build Ownership To Storefront V2

- [x] Add `package.json` to `BlazorShop.Storefront.V2`.
- [x] Add `package-lock.json` generated from Storefront V2 dependencies.
- [x] Add `tailwind.config.js` owned by Storefront V2.
- [x] Add `wwwroot/css/input.css` owned by Storefront V2.
- [x] Configure Tailwind content globs to scan:
  - [x] `./**/*.razor`
  - [x] `./**/*.cshtml` if present
  - [x] `./**/*.html` if present
  - [x] optional `../BlazorShop.Web.SharedV2/**/*.razor` only if shared components need Tailwind scanning
- [x] Generate `wwwroot/css/site.css` inside Storefront V2.
- [x] Ensure `App.razor` keeps using local `css/site.css` and `css/storefront.css`.

Stop gate:

- `npm ci` and Tailwind build run from `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`, not legacy Web. 2026-07-09: `npm ci` and `npm run tailwind:build` passed in Storefront V2. `dotnet build BlazorShop.Storefront.V2.csproj --no-restore` passed after excluding legacy `css/input.css` and `css/site.css` from the temporary legacy content copy.

## Phase 3 - Copy Required Assets Into Storefront V2

- [x] Copy required fonts into `BlazorShop.Storefront.V2/wwwroot/fonts`.
- [x] Copy required Font Awesome assets into `BlazorShop.Storefront.V2/wwwroot/font-awesome` only if V2 still needs local Font Awesome files.
- [x] Copy required images into `BlazorShop.Storefront.V2/wwwroot/images`.
- [x] Confirm seed/QA image URLs still resolve:
  - [x] `/images/banner-bg.jpg`
  - [x] any product/category placeholder images in QA data
- [x] Remove unused legacy-only assets from the copy plan:
  - [x] `index.html`
  - [x] legacy appsettings under `wwwroot`
  - [x] legacy auth/session JS unless V2 references them
  - [x] legacy `app.js`, `authSessionSync.js`, `cookieStorage.js`, `interop.js`, `sessionStorage.js` unless V2 explicitly uses them

Stop gate:

- V2 has all required runtime static assets under its own project directory. 2026-07-09: copied `icon-192.png` and `images/banner-bg.jpg`. Fonts and Font Awesome were not copied because Storefront V2 has no direct references. `bg1.png`, `bg2.png`, and `bg.png` are referenced by development seed data but do not exist in legacy `wwwroot/images`; this remains a seed-data cleanup follow-up rather than a legacy-removal asset copy.

## Phase 4 - Remove Build And Runtime Legacy File References

- [x] Remove this item from `BlazorShop.Storefront.V2.csproj`:

```xml
<Content Include="..\..\BlazorShop.Presentation\BlazorShop.Web\wwwroot\**\*">
```

- [x] Remove legacy fallback static file provider from `Program.cs`:
  - [x] Delete `CreateStaticFileProvider` legacy path logic.
  - [x] Use Storefront V2 `environment.WebRootFileProvider` only.
  - [x] Remove `Microsoft.Extensions.FileProviders` usings if no longer needed.
- [x] Update Storefront V2 `Dockerfile`:
  - [x] Copy `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/package*.json`.
  - [x] Run `npm ci` from Storefront V2 directory.
  - [x] Run Tailwind from Storefront V2 directory.
  - [x] Remove all `COPY BlazorShop.Presentation/BlazorShop.Web...` statements.
  - [x] Remove `WORKDIR /src/BlazorShop.Presentation/BlazorShop.Web`.

Stop gate:

- `rg "BlazorShop\.Presentation[\\/]" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 --glob '!**/bin/**' --glob '!**/obj/**'` returns no hits. 2026-07-09: no Storefront V2 legacy path hits remain outside generated folders.

## Phase 5 - Replace Legacy Service Discovery Names

Current `StorefrontClientAppUrlResolver` and `StorefrontOptionsValidators` still use `adminclient`.

Decision:

- Storefront V2 customer auth now renders locally.
- Authenticated checkout still temporarily redirects to `ClientApp:BaseUrl` for `/account/checkout`.
- If a client app remains necessary, the service-discovery name should not be `adminclient`; use a V2-specific or neutral name.

Recommended options:

| Option | Service key | Fit |
| --- | --- | --- |
| A | `storefrontclient` | Best if checkout/account client remains separate but V2-owned. |
| B | `customerclient` | Best if this represents customer account/checkout UI. |
| C | remove service discovery fallback and require `ClientApp:BaseUrl` only | Best if external client app is temporary and should be explicit. |

Recommended for MVP independence: Option C.

- [ ] Change `StorefrontClientAppUrlResolver` to use only `ClientApp:BaseUrl`.
- [ ] Change validator error text to remove `Services:adminclient`.
- [ ] If Aspire/apphost later needs service discovery, add a V2-specific key in that phase.
- [ ] Add/update tests proving no `adminclient` string remains under `BlazorShop.PresentationV2`.

Stop gate:

- `rg "adminclient" BlazorShop.PresentationV2 --glob '!**/bin/**' --glob '!**/obj/**'` returns no hits.

## Phase 6 - Verification Guardrails

- [ ] Add or update a boundary test that fails if `BlazorShop.PresentationV2` contains forbidden legacy references.
- [ ] Forbidden strings:
  - [ ] `BlazorShop.Presentation\`
  - [ ] `BlazorShop.Presentation/`
  - [ ] `BlazorShop.Web.Shared`
  - [ ] `BlazorShop.API`
  - [ ] `BlazorShop.Web`
  - [ ] `adminclient`
- [ ] Exclude:
  - [ ] `bin`
  - [ ] `obj`
  - [ ] generated test output if needed
  - [ ] docs only if the test is intended for runtime/source boundary rather than migration history
- [ ] Add a second test for project references:
  - [ ] enumerate V2 `*.csproj`
  - [ ] assert no `ProjectReference` includes `BlazorShop.Presentation`

Stop gate:

- Future commits cannot accidentally reintroduce direct legacy file/project references into V2 source.

## Phase 7 - Build, Runtime, Docker, QA

- [ ] Run:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~PresentationV2"
dotnet test BlazorShop.sln --no-restore
```

- [ ] Run legacy-folder isolation check:

```powershell
Rename-Item -LiteralPath 'BlazorShop.Presentation' -NewName 'BlazorShop.Presentation.__legacy_hidden'
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
Rename-Item -LiteralPath 'BlazorShop.Presentation.__legacy_hidden' -NewName 'BlazorShop.Presentation'
```

Use this only after verifying the path is the intended repo root path. Do not run if other active tasks depend on the legacy folder.

- [ ] Build Docker image:

```powershell
docker build -f BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Dockerfile -t blazorshop-storefront-v2:legacy-free .
```

- [ ] Browser QA:
  - [ ] `/`
  - [ ] `/signin`
  - [ ] `/register`
  - [ ] `/my-cart`
  - [ ] `/product/{slug}`
  - [ ] `/css/site.css`
  - [ ] `/css/storefront.css`
  - [ ] `/js/storefrontCommerce.js`
  - [ ] `/images/banner-bg.jpg`
  - [ ] font URLs referenced by CSS
- [ ] Check browser console and network:
  - [ ] no 404 static assets
  - [ ] no legacy Web/API URLs
  - [ ] no request to legacy service-discovery endpoints

Stop gate:

- Storefront V2 runs and renders with no dependency on the legacy Presentation folder.

## Phase 8 - Documentation And QA Checklist

- [ ] Update `QA-StorefrontV2.todo.md` with a `Legacy Independence QA` checklist.
- [ ] Record:
  - [ ] `rg` no-hit commands
  - [ ] build/test results
  - [ ] Docker build result
  - [ ] browser QA screenshots
  - [ ] legacy-folder isolation result
- [ ] Add an architecture note:
  - [ ] `PresentationV2` owns all V2 presentation assets.
  - [ ] Legacy `Presentation` can coexist but is not a dependency.

Stop gate:

- Future QA has a repeatable checklist for proving V2 independence.

## Implementation Order

Recommended commit sequence:

1. `docs(storefront-v2): plan legacy reference removal`
2. `feat(storefront-v2): own tailwind build assets`
3. `feat(storefront-v2): copy required static assets`
4. `refactor(storefront-v2): remove legacy static file provider`
5. `build(storefront-v2): remove legacy docker asset dependency`
6. `refactor(storefront-v2): remove adminclient service discovery fallback`
7. `test(presentation-v2): guard against legacy references`
8. `docs(storefront-v2): update legacy independence qa`

## Risk Review

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Missing copied image/font/CSS asset | Storefront visual regressions or 404s | Asset inventory first; browser network QA after move. |
| Tailwind output differs from legacy build | Layout changes | Compare screenshots before/after on key pages. |
| Docker build misses Node/Tailwind files | Container build fails | Update Dockerfile in same phase as package ownership. |
| Removing `adminclient` breaks authenticated checkout handoff | Authenticated checkout no longer redirects | Keep `ClientApp:BaseUrl` explicit and test authenticated checkout redirect. |
| Boundary test overmatches docs/history | False positives | Scope test to source/build files or explicitly exclude migration docs. |

## AutoPlan Review Summary

CEO review:

- Direction is correct. V2 cannot be called independent while it copies static files and Docker build inputs from legacy Web.
- The highest-value move is ownership transfer, not broad refactor.
- Do not delete legacy projects as part of this plan; prove V2 independence first.

Design review:

- Main visual risk is Tailwind/static asset drift.
- Browser screenshot comparison is required for home, auth pages, product detail, cart, and static content pages.
- Fonts and image paths must be treated as design-critical assets, not incidental files.

Engineering review:

- Compile graph is already clean; remaining dependency is filesystem/build/runtime.
- Remove dependency in this order: asset inventory -> owned asset pipeline -> csproj/runtime cleanup -> Docker cleanup -> guard tests.
- Add automated forbidden-reference tests so this stays fixed.

DX review:

- After this work, a developer should build Storefront V2 without knowing legacy Web exists.
- Dockerfile should be self-explanatory and copy only V2-owned files.
- Error messages and docs should mention `ClientApp:BaseUrl`, not `adminclient`, if external checkout handoff remains temporary.

## Decision Audit Trail

| # | Decision | Classification | Rationale | Rejected |
| --- | --- | --- | --- | --- |
| 1 | Move required static assets into Storefront V2 instead of sharing legacy Web `wwwroot`. | Accepted direction | This removes the remaining runtime/build dependency while preserving existing UI behavior. | Keeping shared filesystem dependency. |
| 2 | Keep legacy projects in the solution for now. | Scope control | The goal is V2 independence, not deleting legacy. Removing legacy from solution is a separate cutover step. | Delete legacy Presentation immediately. |
| 3 | Remove `adminclient` fallback from Storefront V2 and rely on explicit `ClientApp:BaseUrl` for temporary checkout handoff. | Recommended | Customer auth is now local, so legacy admin/client naming is misleading. Explicit config is safer for MVP. | Keep `adminclient` service discovery. |
| 4 | Add automated boundary tests for forbidden references. | Required guardrail | Without tests, the same dependency can return through Dockerfile/csproj/static file paths. | Manual `rg` only. |

## Completion Definition

This plan is complete when `BlazorShop.PresentationV2` can build, test, Docker-build, and run Storefront V2 without the `BlazorShop.Presentation` directory being present, while browser QA confirms no missing static assets and no legacy navigation/request paths.
