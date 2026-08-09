# Storefront Component Mode Foundation Closure Patch

Status: in-progress
Scope: test-only and documentation-only closure patch
Target area: Storefront component mode architecture guardrails

## Purpose

Close the remaining Phase 1 Component Mode Foundation gaps without changing production behavior:

1. Reusable mode libraries must reject any literal Razor/CSS class ownership, not only selected Tailwind prefixes.
2. Component descriptors must be proven consistent with the project/assembly mode that owns them.

This patch is intentionally narrow. It strengthens tests and documentation so future SSR, Hybrid, and WasmHost component work cannot drift back into shared visual/layout ownership.

## Hard Scope Lock

Allowed code test files:

- [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentVisualNeutralityTests.cs`
- [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentDescriptorTests.cs`
- [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentModeBoundaryValidatorTests.cs`
- [x] Optional focused test/helper file under `BlazorShop.Tests.V2/PresentationV2/Storefront/`

Allowed documentation files:

- [x] `docs/refactor-control-Commerce-storefront/Storefront Component Mode Foundation.todo.md`
- [x] `docs/refactor-control-Commerce-storefront/Storefront Component Mode Foundation Closure Patch.todo.md`
- [x] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- [x] Optional: `BlazorShop.PresentationV2/COMPONENT-MODES.md`

Forbidden production changes:

- [x] No changes under `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/`
- [x] No changes under `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/`
- [x] No changes under `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/`
- [x] No changes under `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/`
- [x] No changes under `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/`
- [x] No changes under `BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/`
- [x] No changes under `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/`
- [x] No changes under `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/`
- [x] No changes under StorefrontBuilder tooling.
- [x] No production descriptor validator changes.
- [x] No new real components.
- [x] No compatibility or runtime behavior changes.

## Baseline Facts From Current Code

- [x] `StorefrontComponentVisualNeutralityTests` currently rejects selected class-token prefixes only, such as `rounded`, `bg-`, `text-`, `shadow`, `grid`, `flex`, `px-`, `mx-`, and responsive prefixes.
- [x] The current prefix list can miss literal classes such as `p-6`, `gap-4`, `font-bold`, `items-center`, `justify-between`, `relative`, `w-full`, `border`, and project-specific classes such as `storefront-logo`.
- [x] `StorefrontComponentDescriptorValidator` is intentionally generic and validates descriptor shape, enum values, and component type only.
- [x] Concrete mode ownership is currently expressed by project/assembly names:
  - `BlazorShop.Storefront.Components.Ssr` owns `Ssr` components.
  - `BlazorShop.Storefront.Components.Hybrid` owns `Hybrid` components.
  - `BlazorShop.Storefront.Components.WasmHost` owns `WasmHost` components.
- [x] Mode/assembly consistency should be enforced by architecture tests, not by the production validator.
- [x] Existing worktree may contain unrelated modifications, such as `BlazorShop.sln`; do not touch or revert them.

## Rule A - Literal Class Ownership

Reusable mode component libraries must not own visual CSS class literals in Razor markup.

Allowed:

- [x] `class="@CssClass"`
- [x] `class="@Classes.Container"`
- [x] `class="@GetCssClass()"`
- [x] `class="@(BuildCssClass())"`
- [x] `data-storefront-*` semantic hooks.

Forbidden:

- [x] `class="flex"`
- [x] `class="p-6"`
- [x] `class="gap-4"`
- [x] `class="items-center"`
- [x] `class="storefront-logo"`
- [x] `class="rounded-xl bg-white"`
- [x] `class="flex @CssClass"`
- [x] `class="@CssClass selected"`
- [x] `class="@(BuildCssClass()) selected"`

Rationale:

- SSR, Hybrid, and WasmHost shared libraries can expose state, contracts, behavior, and semantic hooks.
- Storefront V2, Starter, and generated storefronts own actual layout, visual classes, ordering, and styling.
- A partial prefix scanner is not enough because any literal class can become shared visual ownership.

## Rule B - Descriptor Mode Ownership

Descriptor mode must match the mode inferred from the owning component assembly/project.

Required mapping:

- [x] `BlazorShop.Storefront.Components.Ssr` -> `StorefrontComponentMode.Ssr`
- [x] `BlazorShop.Storefront.Components.Hybrid` -> `StorefrontComponentMode.Hybrid`
- [x] `BlazorShop.Storefront.Components.WasmHost` -> `StorefrontComponentMode.WasmHost`
- [x] Unknown assemblies -> no mode inference, not applicable to this architecture rule.

Rationale:

- `StorefrontComponentDescriptorValidator` must remain reusable and browser-safe.
- Production components should not encode knowledge of the current project layout.
- Architecture tests can enforce repository-specific rules without leaking that policy into shared contracts.

## Phase 0 - Baseline And Scope Audit

- [x] Read `AGENTS.md`.
- [x] Read `docs/architecture/README.md`.
- [x] Read `docs/architecture/05-project-and-folder-guide.md`.
- [x] Read `docs/architecture/08-agent-decision-rules.md`.
- [x] Read `docs/architecture/10-v2-contract-ownership.md`.
- [x] Read `BlazorShop.PresentationV2/COMPONENT-MODES.md`.
- [x] Read current `Storefront Component Mode Foundation.todo.md`.
- [x] Read current `QA-StorefrontV2.todo.md`.
- [x] Run `git status --short` and record pre-existing unrelated changes.
- [x] Confirm this patch will not modify production project paths listed in Hard Scope Lock.
- [x] Confirm no implementation work starts before the scope audit is complete.

Exit criteria:

- [x] Current architecture docs and phase status are understood.
- [x] Existing unrelated worktree changes are identified and left untouched.
- [x] The patch is confirmed as test-only/docs-only.

## Phase 1 - Add Generic Literal Class Scanner

Implementation target:

- [x] Prefer placing the scanner in `StorefrontComponentVisualNeutralityTests.cs` unless the file becomes hard to read.
- [x] If extracted, create a focused test helper under `BlazorShop.Tests.V2/PresentationV2/Storefront/`.

Scanner behavior:

- [x] Scan only reusable mode project source trees:
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr`
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid`
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost`
- [x] Scan `*.razor` and `*.cshtml`.
- [x] Exclude generated and build folders:
  - `bin`
  - `obj`
  - `.regeneration-candidate`
  - generated artifacts
  - temporary fixture folders, if any are created by tests.
- [x] Detect Razor `class` attributes with literal strings.
- [x] Include support for normal double-quoted attributes.
- [x] Include single-quoted attributes if the scanner can do so without adding parsing complexity.
- [x] Ignore `data-storefront-*` attributes.
- [x] Treat an attribute as allowed only when the complete class value is a dynamic expression.
- [x] Treat mixed literal plus dynamic class values as violations.
- [x] Return an actionable violation object with at least:
  - relative path
  - attribute value
  - remediation message
- [x] Do not use brittle Tailwind-prefix-only matching as the primary rule.
- [x] Keep old prefix list only if it helps produce a clearer regression message, not as the source of truth.

Allowed dynamic examples:

- [x] `@CssClass`
- [x] `@Classes.Container`
- [x] `@GetCssClass()`
- [x] `@(BuildCssClass())`

Forbidden mixed examples:

- [x] `flex @CssClass`
- [x] `@CssClass selected`
- [x] `@(BuildCssClass()) selected`

Exit criteria:

- [x] Literal class detection is generic.
- [x] The scanner reports path, value, and remediation.
- [x] Existing mode projects pass the scanner.
- [x] No production files changed.

## Phase 2 - Add Literal Class Regression Tests

Add focused tests that prove the scanner behavior independently from current repository content.

Positive fixtures:

- [x] Dynamic `class="@CssClass"` passes.
- [x] Dynamic `class="@Classes.Container"` passes.
- [x] Dynamic `class="@GetCssClass()"` passes.
- [x] Dynamic `class="@(BuildCssClass())"` passes.
- [x] `data-storefront-region="..."` passes.
- [x] Markup with no class attribute passes.

Negative fixtures:

- [x] `class="flex"` fails.
- [x] `class="p-6"` fails.
- [x] `class="gap-4"` fails.
- [x] `class="items-center"` fails.
- [x] `class="storefront-logo"` fails.
- [x] `class="rounded-xl bg-white"` fails.
- [x] `class="flex @CssClass"` fails.
- [x] `class="@CssClass selected"` fails.
- [x] `class="@(BuildCssClass()) selected"` fails.

Repository scan:

- [x] Replace or augment the current selected-prefix scan with a generic literal class scan.
- [x] Assert that every reusable mode project has zero literal class violations.
- [x] Failure output must list every violating file and class value so the next agent can fix without re-investigation.

Exit criteria:

- [x] Positive tests prove legitimate dynamic class usage remains allowed.
- [x] Negative tests prove literal and mixed class usage cannot slip through.
- [x] Repository scan proves all current reusable mode projects are visually neutral.

## Phase 3 - Add Test-Side Assembly Mode Resolver

Implementation target:

- [x] Prefer placing the resolver in `StorefrontComponentDescriptorTests.cs` or a focused test helper file.
- [x] Do not add this resolver to production projects.
- [x] Do not modify `StorefrontComponentDescriptorValidator`.

Resolver contract:

- [x] Input: assembly name or component type assembly.
- [x] Output: nullable `StorefrontComponentMode`.
- [x] Exact known mappings:
  - `BlazorShop.Storefront.Components.Ssr` -> `Ssr`
  - `BlazorShop.Storefront.Components.Hybrid` -> `Hybrid`
  - `BlazorShop.Storefront.Components.WasmHost` -> `WasmHost`
- [x] Unknown assembly returns null.
- [x] Null or empty assembly name returns null.

Descriptor consistency helper:

- [x] Accept a descriptor and the resolved owner mode.
- [x] If owner mode is null, mark as not applicable.
- [x] If owner mode has a value and differs from descriptor mode, return a mismatch.
- [x] Include component key, descriptor mode, owner mode, and component type/assembly in the error message.

Exit criteria:

- [x] Mode/project ownership is enforced in tests.
- [x] Production validator stays generic.
- [x] Unknown assemblies do not cause false failures.

## Phase 4 - Add Descriptor Mode Consistency Tests

Resolver tests:

- [x] Ssr assembly name resolves to `StorefrontComponentMode.Ssr`.
- [x] Hybrid assembly name resolves to `StorefrontComponentMode.Hybrid`.
- [x] WasmHost assembly name resolves to `StorefrontComponentMode.WasmHost`.
- [x] Unknown assembly resolves to null.
- [x] Null or empty assembly resolves to null.

Descriptor consistency positive tests:

- [x] Descriptor mode `Ssr` with owner mode `Ssr` passes.
- [x] Descriptor mode `Hybrid` with owner mode `Hybrid` passes.
- [x] Descriptor mode `WasmHost` with owner mode `WasmHost` passes.
- [x] Descriptor from unknown owner mode is skipped or treated as not applicable.

Descriptor consistency negative tests:

- [x] Descriptor mode `Ssr` with owner mode `Hybrid` fails.
- [x] Descriptor mode `Hybrid` with owner mode `WasmHost` fails.
- [x] Descriptor mode `WasmHost` with owner mode `Ssr` fails.
- [x] Failure message identifies expected mode and actual descriptor mode.

Repository guard:

- [x] If real descriptors exist in mode projects, add a scan that validates all discovered descriptors.
- [x] If real descriptors do not yet exist, keep fixture-level proof and document that repository scanning becomes mandatory when real descriptors are introduced.
- [x] Do not create dummy production components just to test this rule.

Exit criteria:

- [x] Descriptor mode mismatch cannot be introduced silently once descriptors exist.
- [x] Current repository remains green without production dummy components.
- [x] Test names clearly communicate the architecture rule.

## Phase 5 - Documentation And Checklist Updates

Update this plan:

- [ ] Mark phases complete only after implementation and verification pass.
- [ ] Add exact verification commands and results.
- [ ] Add notes for any skipped optional item with reason.

Update `Storefront Component Mode Foundation.todo.md`:

- [ ] Add a closure patch section instead of reopening completed Phase 1 work.
- [ ] Record that the original prefix-based visual neutrality guard has been replaced or strengthened by a generic literal class scanner.
- [ ] Record that descriptor mode/project consistency is enforced by tests, not production contracts.

Update `QA-StorefrontV2.todo.md`:

- [ ] Add QA item for generic literal class rejection in reusable mode projects.
- [ ] Add QA item for allowing fully dynamic class attributes.
- [ ] Add QA item for rejecting mixed literal/dynamic class attributes.
- [ ] Add QA item for descriptor mode matching owning mode project.
- [ ] Add QA item proving production projects were not changed in this closure patch.

Optional update to `COMPONENT-MODES.md`:

- [ ] Clarify that reusable mode projects may expose semantic `data-storefront-*` hooks.
- [ ] Clarify that reusable mode projects must not own literal class values.
- [ ] Clarify that mode/project consistency is a repository architecture test rule.

Exit criteria:

- [ ] Documentation matches the actual guardrails.
- [ ] QA checklist can be followed by another agent without reading the whole discussion.
- [ ] No completed checkbox is marked before evidence exists.

## Phase 6 - Verification Gates

Run focused tests first:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontComponentVisualNeutralityTests|FullyQualifiedName~StorefrontComponentDescriptorTests|FullyQualifiedName~StorefrontComponentModeBoundaryValidatorTests|FullyQualifiedName~StorefrontComponentModeFoundationTests|FullyQualifiedName~StorefrontComponentModeDependencyTests"
```

Run broader architecture tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontComponentModeFoundationTests|FullyQualifiedName~StorefrontComponentModeDependencyTests|FullyQualifiedName~StorefrontComponentModeBoundaryValidatorTests|FullyQualifiedName~StorefrontComponentDescriptorTests|FullyQualifiedName~StorefrontComponentVisualNeutralityTests|FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontPageCompositionGuardrailTests"
```

Run sequential build gate:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/BlazorShop.Storefront.Components.Ssr.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/BlazorShop.Storefront.Components.Hybrid.csproj
```

Run production diff audit:

```powershell
git diff --name-only -- `
  BlazorShop.PresentationV2/BlazorShop.Storefront.Components `
  BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr `
  BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid `
  BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost `
  BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation `
  BlazorShop.PresentationV2/BlazorShop.Storefront.Browser `
  BlazorShop.PresentationV2/BlazorShop.Storefront.V2 `
  BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM
```

Expected production diff audit:

- [ ] No output for forbidden production paths.
- [ ] Existing unrelated changes, if any, are documented and left untouched.

No Playwright:

- [ ] This patch is test-only and does not change browser behavior.
- [ ] Playwright is not required for closure.

Exit criteria:

- [ ] Focused tests pass.
- [ ] Broader architecture tests pass.
- [ ] Sequential builds pass.
- [ ] Production diff audit is clean for forbidden paths.

## Phase 7 - Final Closure And Commit

Final audit:

- [ ] Re-run `git status --short`.
- [ ] Confirm changed files are only allowed files from Hard Scope Lock.
- [ ] Confirm no production project source, DI, runtime, component, generated client, or StorefrontBuilder code changed.
- [ ] Confirm no new real components were introduced.
- [ ] Confirm no obsolete compatibility path was reintroduced.
- [ ] Confirm docs and QA checklist reflect the final state.

Commit:

- [ ] Commit only the closure patch files.
- [ ] Suggested commit message:

```text
test(storefront): close component mode foundation guardrails
```

Exit criteria:

- [ ] Commit exists.
- [ ] Final response lists changed files, verification results, and any skipped optional item.

## Definition Of Done

- [ ] Generic literal class scanner rejects any literal `class` attribute in reusable SSR, Hybrid, and WasmHost mode Razor views.
- [ ] Fully dynamic class attributes are allowed.
- [ ] Mixed literal/dynamic class attributes are rejected.
- [ ] `data-storefront-*` semantic hooks are allowed.
- [ ] Descriptor mode/project consistency is enforced by architecture tests.
- [ ] Production descriptor validator remains generic.
- [ ] No production Storefront project files changed.
- [ ] No V2, V2.WASM, Starter, Builder, Browser, Presentation, Runtime, Client, or CommerceNode behavior changed.
- [ ] Focused test gate passes.
- [ ] Broader architecture test gate passes.
- [ ] Sequential build gate passes.
- [ ] QA checklist is updated with closure checks.
- [ ] Closure patch is committed separately.

## Implementation Notes

- [x] Phase 0 baseline and scope audit:
  - 2026-08-09: read `AGENTS.md`, ASP.NET Core skill guidance, `references/ui-blazor.md`, `docs/architecture/README.md`, `docs/architecture/05-project-and-folder-guide.md`, `docs/architecture/08-agent-decision-rules.md`, `docs/architecture/10-v2-contract-ownership.md`, `BlazorShop.PresentationV2/COMPONENT-MODES.md`, `Storefront Component Mode Foundation.todo.md`, and `QA-StorefrontV2.todo.md`.
  - 2026-08-09: `git status --short` showed pre-existing `M BlazorShop.sln` plus this untracked closure patch plan. The `BlazorShop.sln` hunk is unrelated and must remain untouched unless a later phase explicitly needs it, which this patch does not.
  - 2026-08-09: patch scope confirmed as test-only/docs-only; no production project paths listed in the hard scope lock will be edited.
- [x] Phase 1 generic literal class scanner:
  - 2026-08-09: replaced selected class-prefix matching in `StorefrontComponentVisualNeutralityTests.cs` with a generic Razor `class` attribute scanner for reusable SSR, Hybrid, and WasmHost mode project markup.
  - 2026-08-09: scanner reads only `.razor` and `.cshtml`, skips `bin`, `obj`, `.regeneration-candidate`, `artifacts`, `generated`, `tmp`, and `temp`, and reports relative path, class value, and remediation.
  - 2026-08-09: scanner allows only empty or fully dynamic class values such as `@CssClass`, `@Classes.Container`, `@GetCssClass()`, and `@(BuildCssClass())`; mixed literal/dynamic values are violations.
  - 2026-08-09: verification passed with `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentVisualNeutralityTests"`: 4 passed, 0 failed. Existing MessagePack NU1902/NU1903 warnings were observed.
- [x] Phase 2 literal class regression fixtures:
  - 2026-08-09: added positive fixtures for fully dynamic class attributes, `data-storefront-region`, and markup without `class`.
  - 2026-08-09: added negative fixtures for literal visual classes and mixed literal/dynamic values including `flex @CssClass`, `@CssClass selected`, and `@(BuildCssClass()) selected`.
  - 2026-08-09: repository scan now emits a full joined violation list through assertion failure output.
  - 2026-08-09: verification passed with `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentVisualNeutralityTests"`: 19 passed, 0 failed. Existing MessagePack NU1902/NU1903 warnings were observed.
- [x] Phase 3 test-side assembly mode resolver:
  - 2026-08-09: added `StorefrontComponentDescriptorModeOwnership` and `StorefrontComponentDescriptorModeConsistencyResult` inside `StorefrontComponentDescriptorTests.cs`.
  - 2026-08-09: resolver maps exact SSR, Hybrid, and WasmHost assembly names to `StorefrontComponentMode`, and returns null for unknown, null, or empty assembly names.
  - 2026-08-09: consistency helper reports not-applicable for unknown owners, valid for matching owner mode, and mismatch errors including descriptor key, descriptor mode, owner mode, component type, and assembly.
  - 2026-08-09: `StorefrontComponentDescriptorValidator` remained unchanged.
  - 2026-08-09: verification passed with `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentDescriptorTests"`: 13 passed, 0 failed. Existing MessagePack NU1902/NU1903 warnings were observed.
- [x] Phase 4 descriptor mode consistency tests:
  - 2026-08-09: added resolver tests for SSR, Hybrid, WasmHost, unknown, empty, null, and non-mode component assemblies.
  - 2026-08-09: added consistency positive tests for matching descriptor/owner modes and not-applicable behavior for unknown owner mode.
  - 2026-08-09: added consistency negative tests for `Ssr` vs `Hybrid`, `Hybrid` vs `WasmHost`, and `WasmHost` vs `Ssr`; failure messages include descriptor key, descriptor mode, owner mode, component type, and assembly.
  - 2026-08-09: `rg -n "StorefrontComponentDescriptor|new\\s*\\(" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost -S` returned no matches, so no real mode-project descriptors exist yet.
  - 2026-08-09: added repository guard `RepositoryModeProjectsCurrentlyHaveNoRealDescriptorsSoFixtureProofIsAuthoritative`; it will fail with file paths if real descriptors are introduced before repository descriptor scanning is implemented.
  - 2026-08-09: verification passed with `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentDescriptorTests"`: 28 passed, 0 failed. Existing MessagePack NU1902/NU1903 warnings were observed.

## Decision Audit Trail

| Decision | Selected Direction | Reason |
| --- | --- | --- |
| Literal class policy | Generic literal scanner | Prefix lists miss valid-but-forbidden visual ownership. |
| Scanner scope | `.razor` and `.cshtml` in mode projects | The rule targets reusable markup ownership, not all source text. |
| Dynamic class policy | Complete dynamic expression only | Prevents `class="@CssClass selected"` and similar mixed ownership. |
| Semantic hooks | Allow `data-storefront-*` | Hooks are stable semantic contracts, not visual styling. |
| Descriptor mode ownership | Test-side resolver | Keeps production descriptors portable and browser-safe. |
| Unknown assembly behavior | Not applicable | Avoids false failures for fixtures and future non-mode projects. |
| Production changes | Forbidden | This is a closure patch for tests and documentation only. |
| Playwright | Not required | No browser behavior changes are part of the patch. |
