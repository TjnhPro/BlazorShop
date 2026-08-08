# Storefront Icon Head Rendering Todo

## Muc tieu

Tap trung sua ownership cua favicon/icon head rendering cho Storefront Presentation, V2, Starter va generated storefront inheritance.

Ket qua mong muon:

- Storefront khong con hard-code favicon V2 `icon-192.png` trong application head.
- Store-specific favicon/icon metadata dung du lieu runtime store da co san.
- V2, Starter va generated storefront co cung mot render primitive cho icon head tags.
- Khong them DB/API/contract moi vi `FaviconUrl`, `PngIconUrl`, `AppleTouchIconUrl`, `MsTileImageUrl`, `MsTileColor` da ton tai trong current store/display context.
- Khong tao duplicate `<link rel="icon">` khi ca `FaviconUrl` va `PngIconUrl` cung duoc cau hinh.
- `/favicon.ico` redirect hien co van hoat dong, nhung khong duoc xem la render source chinh cua HTML head.

## Codebase evidence

Da xac nhan trong codebase:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontApplicationHead.razor` hien hard-code:
  - `<link rel="icon" type="image/png" href="icon-192.png" />`
  - `<StorefrontBrandHead DisplayContext="Context.Display" />`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Seo/StorefrontBrandHead.razor` hien render:
  - `DisplayContext.FaviconUrl`
  - `DisplayContext.PngIconUrl`
  - `DisplayContext.AppleTouchIconUrl`
  - `DisplayContext.MsTileImageUrl`
  - `DisplayContext.MsTileColor`
  - `bs-storefront-language`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Components/Layout/ApplicationHead.razor` hien chi render `css/starter.css`, chua render favicon/icon.
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Contracts/IStorefrontDisplayContextProvider.cs` da co icon fields trong `StorefrontDisplayContext`.
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/StorefrontDisplayContextProvider.cs` da map icon fields tu current store.
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontApplicationBuilderExtensions.cs` da map `/favicon.ico` redirect theo `StorefrontApplicationOptions.FaviconRedirectPath`.
- Control Plane/Commerce Node da co runtime favicon/logo/icon input va validation coverage trong QA files.

## Khong nam trong scope

- Khong them migration/database column.
- Khong them public Storefront API field moi.
- Khong sua generated OpenAPI/client contract.
- Khong sua Control Plane store configuration UI, tru khi test phat hien UI dang map sai field hien co.
- Khong doi SEO page metadata pipeline.
- Khong doi `/favicon.ico` redirect option thanh dynamic store route trong phase nay.
- Khong them image upload/media manager cho favicon.
- Khong sua static asset file `icon-192.png` neu con duoc dung lam fallback redirect/static asset o noi khac.

## Decision lock

- Presentation owns shared icon head render primitive.
- Visual hosts own placement in their `ApplicationHead` component and their CSS/assets.
- V2 `StorefrontBrandHead` must not render favicon/icon links after cutover. It may continue rendering non-icon storefront metadata such as `bs-storefront-language`.
- Starter must call the same shared primitive so generated storefronts inherit the same behavior.
- Prefer explicit, source-readable Razor markup over reflection, dynamic head mutations, or JavaScript DOM patching.

## Phase 1 - Add Presentation shared icon head primitive

### Files

- Add:
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Head/StorefrontIconHead.razor`
    - If `Components/Head` folder does not exist, create it.

### Implementation checklist

- [x] Component must be browser/server safe Razor only.
- [x] Component must not inject services.
- [x] Component must accept icon data from caller.
- [ ] Recommended API:

```razor
<StorefrontIconHead DisplayContext="Context.Display" />
```

- [ ] Alternative acceptable API if implementation prefers explicit primitive inputs:

```razor
<StorefrontIconHead
    FaviconUrl="Context.Display.FaviconUrl"
    PngIconUrl="Context.Display.PngIconUrl"
    AppleTouchIconUrl="Context.Display.AppleTouchIconUrl"
    MsTileImageUrl="Context.Display.MsTileImageUrl"
    MsTileColor="Context.Display.MsTileColor" />
```

- [x] Do not create a new DTO for this phase unless Razor parameter readability becomes poor.
- [x] Reuse `StorefrontDisplayContext`; do not duplicate current store loading.
- [x] Render nothing when all icon fields are null/empty/whitespace.
- [x] Normalize behavior must be render-time whitespace guard only. Do not perform URL validation here because upstream store validation already owns safe public asset URL policy.
- [x] Keep output deterministic and simple for source tests.

### Icon render policy

- [x] If `FaviconUrl` is set, render exactly one primary favicon:

```html
<link rel="icon" href="..." />
```

- [x] If `FaviconUrl` is empty and `PngIconUrl` is set, render one primary png favicon:

```html
<link rel="icon" type="image/png" href="..." />
```

- [x] Do not render both primary `rel="icon"` links at the same time in MVP. This avoids browser-order ambiguity and duplicate favicon assertions.
- [x] Render Apple icon independently when `AppleTouchIconUrl` is set:

```html
<link rel="apple-touch-icon" href="..." />
```

- [x] Render Microsoft tile image independently when `MsTileImageUrl` is set:

```html
<meta name="msapplication-TileImage" content="..." />
```

- [x] Render Microsoft tile color independently when `MsTileColor` is set:

```html
<meta name="msapplication-TileColor" content="..." />
```

### Phase 1 tests

- [x] Add or update source-level test proving `StorefrontIconHead.razor` exists in `BlazorShop.Storefront.Presentation`, not V2.
- [x] Add test proving the component has no `@inject`.
- [x] Add test proving primary favicon precedence:
  - `FaviconUrl` wins over `PngIconUrl`.
  - `PngIconUrl` renders only when `FaviconUrl` is empty.
- [x] Add test proving Apple/MS tags remain supported.
- [x] Add test proving empty context emits no icon tags.

## Phase 2 - Cut over V2 application head

### Files

- Update:
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontApplicationHead.razor`
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Seo/StorefrontBrandHead.razor`

### Implementation checklist

- [x] Remove hard-coded V2 favicon:

```razor
<link rel="icon" type="image/png" href="icon-192.png" />
```

- [x] Add shared icon primitive before `StorefrontBrandHead`.
- [x] Keep stylesheet order unchanged:
  - `css/site.css`
  - `css/storefront.css`
- [x] Keep `StorefrontBrandHead` before `HeadOutlet` through the existing `ApplicationHead` slot.
- [x] Refactor `StorefrontBrandHead` so it no longer renders:
  - `<link rel="icon"...>`
  - `<link rel="apple-touch-icon"...>`
  - `msapplication-TileImage`
  - `msapplication-TileColor`
- [x] Keep `StorefrontBrandHead` for `bs-storefront-language` or rename only if tests/docs are updated in same phase. Prefer no rename in this phase to reduce churn.
- [x] Do not move full V2 `ApplicationHead` into Presentation because V2 still owns visual/static CSS assets.

### Phase 2 tests

- [x] Update `BlazorShop.Tests.V2/PresentationV2/LayoutAssetFoundationTests.cs`.
  - Replace assertion for hard-coded `icon-192.png`.
  - Assert V2 `ApplicationHead` contains shared `StorefrontIconHead`.
  - Assert V2 `ApplicationHead` still contains the two expected stylesheets.
  - Assert V2 `ApplicationHead` still contains `StorefrontBrandHead`.
- [x] Update `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontBrandingMarkupTests.cs`.
  - Stop expecting icon tags inside V2 `StorefrontBrandHead`.
  - Assert `StorefrontBrandHead` keeps `bs-storefront-language`.
  - Assert icon tags are owned by Presentation shared component or V2 `ApplicationHead` calls it.
- [x] Add guardrail test:
  - V2 source must not contain `<link rel="icon" type="image/png" href="icon-192.png" />`.
  - V2 `StorefrontBrandHead.razor` must not contain `rel="icon"`.
- [x] Check architecture tests that currently expect `StorefrontBrandHead.razor` as the only V2 SEO file.
  - If adding no new V2 SEO file, keep expectation unchanged.
  - If moving/renaming, update with explicit rationale.

## Phase 3 - Cut over Starter application head

### Files

- Update:
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Components/Layout/ApplicationHead.razor`

### Implementation checklist

- [ ] Keep `css/starter.css`.
- [ ] Add shared `StorefrontIconHead` using `Context.Display`.
- [ ] Do not add V2 `StorefrontBrandHead` dependency to Starter.
- [ ] Do not copy V2-specific icon markup into Starter.
- [ ] Ensure Starter remains visual-host-only and consumes Presentation contracts.

### Phase 3 tests

- [ ] Update `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontStarterHostSmokeTests.cs` or a focused Starter markup test.
- [ ] Assert Starter root HTML still includes `href="css/starter.css"`.
- [ ] Assert Starter root HTML renders configured favicon when the test fixture provides `FaviconUrl`.
- [ ] Assert Starter root HTML does not render V2-only `StorefrontBrandHead`.
- [ ] Assert no duplicate primary `rel="icon"` in Starter HTML.

## Phase 4 - Preserve Presentation redirect behavior

### Files

- Review only unless tests fail:
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Options/StorefrontApplicationOptions.cs`
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontApplicationBuilderExtensions.cs`

### Implementation checklist

- [ ] Keep `/favicon.ico` redirect mapping as compatibility/browser fallback.
- [ ] Do not make `/favicon.ico` store-dynamic in this phase.
- [ ] Do not remove `FaviconRedirectPath` unless every host/test has been migrated and an explicit follow-up decision approves removal.
- [ ] Ensure the HTML head uses store-specific runtime icon fields, while `/favicon.ico` remains a fallback route.

### Phase 4 tests

- [ ] Keep or update existing test assertion proving:

```csharp
app.MapGet("/favicon.ico", () => Results.Redirect(applicationOptions.FaviconRedirectPath, permanent: false));
```

- [ ] Add note in test name/comment if needed:
  - `/favicon.ico` redirect is compatibility fallback.
  - `StorefrontIconHead` is canonical HTML head rendering path.

## Phase 5 - QA checklist and docs alignment

### Files

- Update if implementation changes behavior:
  - `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
  - `docs/refactor-control-Commerce-storefront/QA-StorefrontStarter.todo.md`
  - `docs/architecture/05-project-and-folder-guide.md`

### Documentation checklist

- [ ] Update Storefront V2 QA line that currently says head icon metadata uses store favicon/png/apple/MS tile values.
  - Keep it true, but mention the new shared Presentation `StorefrontIconHead` owner.
- [ ] Update Starter QA with a small entry that Starter renders store-specific favicon/icon through Presentation primitive.
- [ ] If architecture docs mention `StorefrontBrandHead` as brand/runtime metadata owner, adjust wording:
  - `StorefrontIconHead` owns icon tags.
  - `StorefrontBrandHead` owns non-icon storefront metadata such as language marker.
- [ ] Do not add large architecture essay. Keep docs scoped to ownership and guardrail.

## Phase 6 - Verification commands

Run focused tests first:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~LayoutAssetFoundationTests|FullyQualifiedName~StorefrontBrandingMarkupTests|FullyQualifiedName~StorefrontStarterHostSmokeTests"
```

If the exact test project path differs, use the repo's current V2 test project path and keep the filter equivalent.

Then run architecture/foundation guardrails:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontPresentationFoundationBoundaryTests|FullyQualifiedName~StorefrontStarterFoundationBoundaryTests|FullyQualifiedName~StorefrontVisualOnlyBoundaryTests"
```

Finally run build:

```powershell
dotnet build BlazorShop.sln
```

Optional browser QA if this lands with visible storefront changes:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser
```

Manual/browser assertions:

- [ ] V2 home page head includes one primary favicon from configured store.
- [ ] V2 home page head includes Apple/MS icon metadata when configured.
- [ ] V2 home page head no longer includes hard-coded `icon-192.png` unless store config itself points to that asset.
- [ ] Starter home page head includes one primary favicon when configured.
- [ ] `/favicon.ico` still returns redirect/fallback behavior.

## Regression checklist

- [ ] No new API contract drift.
- [ ] No generated client regeneration required.
- [ ] No Commerce Node migration generated.
- [ ] No Control Plane UI regression.
- [ ] No duplicate primary favicon link in V2.
- [ ] No duplicate primary favicon link in Starter.
- [ ] No `@inject` in shared icon component.
- [ ] No V2-only component used by Starter.
- [ ] `StorefrontBrandHead` no longer owns icon tags.
- [ ] `StorefrontIconHead` lives in Presentation.
- [ ] `StorefrontApplicationHead` still renders before `HeadOutlet`.
- [ ] `StorefrontAntiforgeryHead` order remains before application head.

## Definition of Done

- [ ] Shared Presentation icon head component exists and is covered by tests.
- [ ] V2 removes hard-coded `icon-192.png` from HTML head.
- [ ] V2 uses shared icon head primitive.
- [ ] Starter uses shared icon head primitive.
- [ ] `StorefrontBrandHead` is reduced to non-icon metadata.
- [ ] Existing current-store/display-context favicon fields are reused.
- [ ] `/favicon.ico` fallback route remains intact.
- [ ] Focused tests pass.
- [ ] Architecture/foundation guardrail tests pass.
- [ ] `dotnet build BlazorShop.sln` passes.
- [ ] QA checklist entries are updated with evidence.

## Agent notes

- This is a small ownership cleanup, not a schema feature.
- If implementation discovers missing store icon data, stop and prove it with code evidence before adding contract work.
- If tests show `StorefrontBrandHead` is still needed for previous SEO collision fix, keep its placement but remove only icon ownership.
- If generated storefront proof fails, check Starter `ApplicationHead` first because generated stores inherit from Starter.
