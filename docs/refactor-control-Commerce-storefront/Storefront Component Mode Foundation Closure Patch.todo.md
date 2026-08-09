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

- [ ] Prefer placing the scanner in `StorefrontComponentVisualNeutralityTests.cs` unless the file becomes hard to read.
- [ ] If extracted, create a focused test helper under `BlazorShop.Tests.V2/PresentationV2/Storefront/`.

Scanner behavior:

- [ ] Scan only reusable mode project source trees:
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr`
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid`
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost`
- [ ] Scan `*.razor` and `*.cshtml`.
- [ ] Exclude generated and build folders:
  - `bin`
  - `obj`
  - `.regeneration-candidate`
  - generated artifacts
  - temporary fixture folders, if any are created by tests.
- [ ] Detect Razor `class` attributes with literal strings.
- [ ] Include support for normal double-quoted attributes.
- [ ] Include single-quoted attributes if the scanner can do so without adding parsing complexity.
- [ ] Ignore `data-storefront-*` attributes.
- [ ] Treat an attribute as allowed only when the complete class value is a dynamic expression.
- [ ] Treat mixed literal plus dynamic class values as violations.
- [ ] Return an actionable violation object with at least:
  - relative path
  - attribute value
  - remediation message
- [ ] Do not use brittle Tailwind-prefix-only matching as the primary rule.
- [ ] Keep old prefix list only if it helps produce a clearer regression message, not as the source of truth.

Allowed dynamic examples:

- [ ] `@CssClass`
- [ ] `@Classes.Container`
- [ ] `@GetCssClass()`
- [ ] `@(BuildCssClass())`

Forbidden mixed examples:

- [ ] `flex @CssClass`
- [ ] `@CssClass selected`
- [ ] `@(BuildCssClass()) selected`

Exit criteria:

- [ ] Literal class detection is generic.
- [ ] The scanner reports path, value, and remediation.
- [ ] Existing mode projects pass the scanner.
- [ ] No production files changed.

## Phase 2 - Add Literal Class Regression Tests

Add focused tests that prove the scanner behavior independently from current repository content.

Positive fixtures:

- [ ] Dynamic `class="@CssClass"` passes.
- [ ] Dynamic `class="@Classes.Container"` passes.
- [ ] Dynamic `class="@GetCssClass()"` passes.
- [ ] Dynamic `class="@(BuildCssClass())"` passes.
- [ ] `data-storefront-region="..."` passes.
- [ ] Markup with no class attribute passes.

Negative fixtures:

- [ ] `class="flex"` fails.
- [ ] `class="p-6"` fails.
- [ ] `class="gap-4"` fails.
- [ ] `class="items-center"` fails.
- [ ] `class="storefront-logo"` fails.
- [ ] `class="rounded-xl bg-white"` fails.
- [ ] `class="flex @CssClass"` fails.
- [ ] `class="@CssClass selected"` fails.
- [ ] `class="@(BuildCssClass()) selected"` fails.

Repository scan:

- [ ] Replace or augment the current selected-prefix scan with a generic literal class scan.
- [ ] Assert that every reusable mode project has zero literal class violations.
- [ ] Failure output must list every violating file and class value so the next agent can fix without re-investigation.

Exit criteria:

- [ ] Positive tests prove legitimate dynamic class usage remains allowed.
- [ ] Negative tests prove literal and mixed class usage cannot slip through.
- [ ] Repository scan proves all current reusable mode projects are visually neutral.

## Phase 3 - Add Test-Side Assembly Mode Resolver

Implementation target:

- [ ] Prefer placing the resolver in `StorefrontComponentDescriptorTests.cs` or a focused test helper file.
- [ ] Do not add this resolver to production projects.
- [ ] Do not modify `StorefrontComponentDescriptorValidator`.

Resolver contract:

- [ ] Input: assembly name or component type assembly.
- [ ] Output: nullable `StorefrontComponentMode`.
- [ ] Exact known mappings:
  - `BlazorShop.Storefront.Components.Ssr` -> `Ssr`
  - `BlazorShop.Storefront.Components.Hybrid` -> `Hybrid`
  - `BlazorShop.Storefront.Components.WasmHost` -> `WasmHost`
- [ ] Unknown assembly returns null.
- [ ] Null or empty assembly name returns null.

Descriptor consistency helper:

- [ ] Accept a descriptor and the resolved owner mode.
- [ ] If owner mode is null, mark as not applicable.
- [ ] If owner mode has a value and differs from descriptor mode, return a mismatch.
- [ ] Include component key, descriptor mode, owner mode, and component type/assembly in the error message.

Exit criteria:

- [ ] Mode/project ownership is enforced in tests.
- [ ] Production validator stays generic.
- [ ] Unknown assemblies do not cause false failures.

## Phase 4 - Add Descriptor Mode Consistency Tests

Resolver tests:

- [ ] Ssr assembly name resolves to `StorefrontComponentMode.Ssr`.
- [ ] Hybrid assembly name resolves to `StorefrontComponentMode.Hybrid`.
- [ ] WasmHost assembly name resolves to `StorefrontComponentMode.WasmHost`.
- [ ] Unknown assembly resolves to null.
- [ ] Null or empty assembly resolves to null.

Descriptor consistency positive tests:

- [ ] Descriptor mode `Ssr` with owner mode `Ssr` passes.
- [ ] Descriptor mode `Hybrid` with owner mode `Hybrid` passes.
- [ ] Descriptor mode `WasmHost` with owner mode `WasmHost` passes.
- [ ] Descriptor from unknown owner mode is skipped or treated as not applicable.

Descriptor consistency negative tests:

- [ ] Descriptor mode `Ssr` with owner mode `Hybrid` fails.
- [ ] Descriptor mode `Hybrid` with owner mode `WasmHost` fails.
- [ ] Descriptor mode `WasmHost` with owner mode `Ssr` fails.
- [ ] Failure message identifies expected mode and actual descriptor mode.

Repository guard:

- [ ] If real descriptors exist in mode projects, add a scan that validates all discovered descriptors.
- [ ] If real descriptors do not yet exist, keep fixture-level proof and document that repository scanning becomes mandatory when real descriptors are introduced.
- [ ] Do not create dummy production components just to test this rule.

Exit criteria:

- [ ] Descriptor mode mismatch cannot be introduced silently once descriptors exist.
- [ ] Current repository remains green without production dummy components.
- [ ] Test names clearly communicate the architecture rule.

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
