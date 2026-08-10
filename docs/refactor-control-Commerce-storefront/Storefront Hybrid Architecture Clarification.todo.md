# Storefront Hybrid Architecture Clarification

Status: planned
Owner: Storefront V2 architecture
Scope: H0 documentation clarification only

## Goal

Clarify what `Hybrid` means in the BlazorShop Storefront architecture before any further Component Mode Foundation or reusable component extraction work.

This phase must separate:

- BlazorShop architectural classification: `Ssr`, `Hybrid`, `WasmHost`.
- ASP.NET Core render modes: `Static`, `InteractiveWebAssembly`, `InteractiveServer`, `InteractiveAuto`.
- Physical project/package layout: `Components.Ssr`, `Components.Hybrid`, `Components.WasmHost`, `V2.WASM`, Starter WASM, and future generated WASM projects.

The target decision for H0:

```text
Hybrid in BlazorShop means:

server-produced/prerendered HTML or page snapshot
  + client-side WebAssembly interactivity after hydration
  + optional progressive enhancement

It does not mean:

InteractiveAuto
InteractiveServer
SignalR/circuit-based storefront interactivity
mandatory Components.Hybrid -> WasmHost child nesting
mandatory server shell -> nested interactive child implementation
```

H0 does not decide the final physical project graph. H1 must re-evaluate that graph after the documentation source of truth is corrected.

## Current Codebase Facts

- `BlazorShop.PresentationV2/COMPONENT-MODES.md` still says mode projects are foundation-only, but `Storefront Reference Components.todo.md` is complete and real reference components now exist.
- `COMPONENT-MODES.md` currently defines `Components.Hybrid` as a server-owned shell that can host a `WasmHost` child.
- `docs/architecture/05-project-and-folder-guide.md` repeats the same old `Components.Hybrid` ownership and project-reference model.
- `docs/architecture/10-v2-contract-ownership.md` repeats the old statement that Hybrid may bridge server-prepared state to a WasmHost child.
- `docs/architecture/03-runtime-boundaries.md` describes Presentation as owning SSR, hybrid, and WASM-host route shells, but does not explicitly define the clarified component runtime meaning.
- `BlazorShop.Storefront.Components.Hybrid/README.md` still says the project may bridge server-prepared state to a WasmHost child and still says the project is foundation-only.
- `Storefront Reference Components.todo.md` records the important later fact that visible V2 contact flow moved to a V2.WASM wrapper because nested Hybrid bridge rendered but did not hydrate submit events in browser QA.
- V2 currently uses `InteractiveWebAssembly` for interactive browser roots and does not need `InteractiveServer` or `InteractiveAuto` for public storefront behavior.
- V2.WASM is already the downloadable client assembly path for interactive V2 components.

## Official ASP.NET Core Facts To Preserve

Use current Microsoft ASP.NET Core Blazor documentation as the source for render-mode facts:

- `InteractiveWebAssembly` is client-side rendering using Blazor WebAssembly and is interactive in the browser.
- Prerendering is enabled by default for interactive components.
- Prerendering initially renders page content statically from the server before the interactive runtime is ready.
- Components using `InteractiveWebAssembly` must be built from a separate client-side project so they are included in the downloaded app bundle.
- `InteractiveServer` uses interactive server rendering and a SignalR/circuit-style server runtime; this is not the desired public storefront interactivity model.
- `InteractiveAuto` initially uses server interactivity and later uses WebAssembly on subsequent visits after the bundle is available.
- `InteractiveAuto` does not dynamically move an existing component instance from server to WASM while it is already on the page.
- A child component cannot switch to a different interactive render mode than an already-interactive parent.
- Parameters from a static parent to an interactive child must be JSON serializable; `RenderFragment` or child content cannot freely cross that boundary.
- `RendererInfo.IsInteractive` and `AssignedRenderMode` exist and can be used in later H1/H2 design when component behavior must know current interactivity.
- JS initializers include `beforeWebAssemblyStart` and `afterWebAssemblyStarted`; these are browser/WASM startup hooks, not a reason to introduce server interactivity.

Reference URLs:

- `https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0`
- `https://learn.microsoft.com/en-us/aspnet/core/blazor/components/prerender?view=aspnetcore-10.0`
- `https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/startup?view=aspnetcore-10.0`

## Hard Scope Lock

Allowed H0 changes:

- [ ] `BlazorShop.PresentationV2/COMPONENT-MODES.md`
- [ ] `docs/architecture/03-runtime-boundaries.md`
- [ ] `docs/architecture/05-project-and-folder-guide.md`
- [ ] `docs/architecture/10-v2-contract-ownership.md`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/README.md`
- [ ] `docs/refactor-control-Commerce-storefront/Storefront Reference Components.todo.md`
- [ ] `docs/refactor-control-Commerce-storefront/Storefront Component Mode Foundation.todo.md`
- [ ] `docs/refactor-control-Commerce-storefront/Storefront Component Mode Foundation Closure Patch.todo.md` only if a short historical/superseded note is required.
- [ ] A new H1 backlog `.todo.md` file if needed.
- [ ] Optional doc index/readme updates only when they point readers to the corrected source of truth.

Forbidden H0 changes:

- [ ] No `.csproj` changes.
- [ ] No production `.cs`, `.razor`, `.js`, `.css`, `.scss`, or static asset changes.
- [ ] No DI, service registration, `Program.cs`, endpoint, BFF, Browser, Runtime, Client, Presentation, V2, V2.WASM, Starter, Builder, Control Plane, Commerce Node, Application, Domain, or Infrastructure source changes.
- [ ] No test implementation changes.
- [ ] No Playwright test changes or runs required.
- [ ] No component movement.
- [ ] No descriptor mode changes.
- [ ] No render-mode behavior changes.
- [ ] No project-reference graph changes.

If a code/test/build blocker appears during H0, stop and split it into H1. Do not fix it inside H0.

## Not In Scope

- Redesigning `Components.Hybrid`.
- Deleting `Components.Hybrid`.
- Moving contact form, discounted rail, cart, checkout, account, product cards, or product gallery components.
- Changing current V2 visible behavior.
- Reworking StorefrontBuilder route folder conventions.
- Renaming route folders such as `Pages/Hybrid`.
- Introducing `InteractiveAuto`.
- Introducing `InteractiveServer`.
- Introducing SignalR/circuit storefront interactivity.
- Changing generated storefront package rules.

## Phase H0.0 - Baseline And Evidence

- [x] Read `AGENTS.md`.
- [x] Read `docs/architecture/README.md`.
- [x] Read `BlazorShop.PresentationV2/COMPONENT-MODES.md`.
- [x] Read `docs/architecture/03-runtime-boundaries.md`.
- [x] Read `docs/architecture/05-project-and-folder-guide.md`.
- [x] Read `docs/architecture/10-v2-contract-ownership.md`.
- [x] Read `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/README.md`.
- [x] Read `docs/refactor-control-Commerce-storefront/Storefront Component Mode Foundation.todo.md`.
- [x] Read `docs/refactor-control-Commerce-storefront/Storefront Reference Components.todo.md`.
- [x] Verify the V2 visible contact implementation path:
  - `BlazorShop.Storefront.V2/Pages/Ssr/Content/StorefrontPage.razor`
  - `BlazorShop.Storefront.V2.WASM/Components/Content/StorefrontContactFormSection.razor`
  - `BlazorShop.Storefront.Components.WasmHost/Content/StorefrontContactFormApp.razor`
- [x] Verify whether `BlazorShop.Storefront.Components.Hybrid/Content/StorefrontContactForm.razor` still exists and whether it is directly used by visible V2 routes.
- [x] Verify the V2 discounted rail/home implementation path:
  - `BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/Home.razor`
  - `BlazorShop.Storefront.V2.WASM` or `Components.WasmHost` rail component usage.
- [x] Run a documentation scan:

```powershell
rg -n "Hybrid|InteractiveAuto|InteractiveServer|Components\.Hybrid|WasmHost child|server-owned shells|@rendermode" BlazorShop.PresentationV2 docs -g "*.md" -g "*.todo.md"
```

Exit criteria:

- [x] The stale Hybrid wording locations are identified.
- [x] The visible V2 usage that forced the clarification is recorded in implementation notes.
- [x] No source code is edited.

## Phase H0.1 - Update `COMPONENT-MODES.md`

Rewrite `BlazorShop.PresentationV2/COMPONENT-MODES.md` as the source of truth.

Required updates:

- [x] Remove or replace the stale statement that mode projects are foundation-only and contain no real storefront feature components.
- [x] State that the original foundation has completed and reference components now exist.
- [x] State that the physical `Components.Hybrid` project is under H1 re-evaluation.
- [x] Define `Ssr` as server/static/prerender-capable rendering with no browser runtime required for primary function.
- [x] Define `WasmHost` as browser-side WebAssembly interactive roots that must be part of a downloadable WASM app graph and use Browser controllers/local BFF routes.
- [x] Define `Hybrid` as BlazorShop's architectural classification for server-produced/prerendered HTML plus client-side WASM interactivity.
- [x] Explicitly say `Hybrid` is not `.NET InteractiveAuto`.
- [x] Explicitly say `Hybrid` is not `InteractiveServer`.
- [x] Explicitly say public Storefront Hybrid must not depend on SignalR/server circuit interactivity.
- [x] Explicitly say `Hybrid` does not require a nested `Components.Hybrid -> WasmHost child` implementation.
- [x] Record that `@rendermode InteractiveWebAssembly` placement is host/composition ownership, not a guarantee that reusable component libraries own render-mode directives.
- [x] Record that static-to-interactive parameters must remain JSON serializable.
- [x] Record that `RenderFragment`/child content cannot be passed freely across static-to-interactive render-mode boundaries.
- [x] Record that interactive components should avoid hard-coupling implementation assumptions to a specific render mode and should degrade gracefully where possible.
- [x] Preserve the base `Storefront.Components` rule: contracts/headless/browser-safe primitives only, no visual ownership.
- [x] Preserve the data path rule:

```text
WASM/browser component
  -> Browser controller
  -> same-origin Presentation/BFF endpoint
  -> Runtime
  -> Commerce Node Storefront API
```

- [x] Add an explicit "H1 Re-evaluation Required" section listing:
  - project-reference graph;
  - `Components.Hybrid` role;
  - descriptor mode ownership;
  - boundary validator allowlists;
  - V2/V2.WASM wrapper pattern;
  - Starter/generated storefront implications.

Exit criteria:

- [x] A future agent can read `COMPONENT-MODES.md` and understand that `Hybrid` is a runtime/classification concept, not a fixed package nesting rule.
- [x] The document no longer suggests `InteractiveAuto` is needed or acceptable for public storefront Hybrid.
- [x] The document does not instruct an agent to implement nested Hybrid bridges as the default pattern.

## Phase H0.2 - Update Architecture Boundary Docs

### `docs/architecture/03-runtime-boundaries.md`

- [ ] Add a Storefront component render/runtime subsection under the Storefront Presentation or Browser/BFF boundary.
- [ ] Define Storefront `Ssr`, `Hybrid`, and `WasmHost` in architecture terms.
- [ ] State that Storefront V2/Starter/generated hosts may group route/page files under `Pages/Ssr`, `Pages/Hybrid`, and `Pages/WasmHost` as ownership folders, not as direct ASP.NET render mode names.
- [ ] State that public interactive storefront behavior should use `InteractiveWebAssembly` with prerender where needed.
- [ ] State that public storefront routes must not use `InteractiveServer` or `InteractiveAuto` unless a later architecture decision explicitly reopens the tradeoff.
- [ ] State that Browser/WASM code still uses same-origin BFF endpoints and must not call Commerce Node directly.

### `docs/architecture/05-project-and-folder-guide.md`

- [ ] Update the `Components.Hybrid` section so it no longer presents `server-owned shell -> WasmHost child` as the canonical meaning.
- [ ] Mark the current physical project as a historical reusable Hybrid shell library pending H1 re-evaluation.
- [ ] Update `Components.WasmHost` wording so render-mode placement belongs to the host/composition root, not specifically "host or Hybrid shell".
- [ ] Keep route folder descriptions for `Pages/Hybrid` but clarify they are BlazorShop render ownership folders, not `.NET InteractiveAuto` and not necessarily `Components.Hybrid`.
- [ ] Keep V2, Starter, and generated storefront ownership rules unchanged.

### `docs/architecture/10-v2-contract-ownership.md`

- [ ] Replace the old sentence "Hybrid may bridge server-prepared state to a WasmHost child" with the clarified model.
- [ ] State that component contracts/descriptors describe semantic render/runtime classification only.
- [ ] State that physical project ownership and descriptor/project consistency tests must be revisited in H1.
- [ ] Preserve that reusable components must not own V2 theme classes, store copy, generated output, or direct Commerce Node routes.

Exit criteria:

- [ ] All current architecture docs agree on the clarified Hybrid definition.
- [ ] No architecture doc still presents `Components.Hybrid -> WasmHost child` as the required implementation model.
- [ ] No architecture doc suggests public storefront should use `InteractiveAuto` or `InteractiveServer`.

## Phase H0.3 - Update Local Project README And Historical Plans

### `BlazorShop.Storefront.Components.Hybrid/README.md`

- [ ] Remove the stale "foundation-only until later phase adds real shared components" wording.
- [ ] Explain that the project exists from the earlier foundation/reference component work but its future role is pending H1 re-evaluation.
- [ ] Keep current guardrails until H1 changes tests/code:
  - no direct Browser reference;
  - no Browser controller injection;
  - no direct API calls;
  - no theme CSS;
  - no V2 layout/copy ownership.
- [ ] Avoid claiming nested bridge is the desired default.

### `Storefront Reference Components.todo.md`

- [ ] Add a short "H0 Hybrid Clarification Note" near the top.
- [ ] Preserve the historical completion record.
- [ ] State that the phase did implement a Hybrid descriptor/component, but visible V2 contact flow moved to a V2.WASM wrapper after browser QA exposed nested bridge hydration failure.
- [ ] State that the phase proves useful SSR/WASM/BFF/reference behavior, but it does not close the physical Hybrid shell model.
- [ ] Link or name this H0 plan as the follow-up architecture clarification.

### `Storefront Component Mode Foundation.todo.md`

- [ ] Add a short "Historical Note" near the top.
- [ ] Preserve the completed foundation record.
- [ ] State that the original dependency graph and Hybrid shell wording were correct for that historical foundation phase but are being superseded by H0/H1.
- [ ] Do not rewrite all completed checkboxes.

### `Storefront Component Mode Foundation Closure Patch.todo.md`

- [ ] Add a historical note only if scan results show it is likely to be used as current source of truth.
- [ ] Do not rewrite closure evidence.

Exit criteria:

- [ ] Historical docs remain historically accurate.
- [ ] Future agents are warned not to treat old completed plan wording as current architecture source of truth.
- [ ] No implementation evidence is erased.

## Phase H0.4 - Scan And Classify Remaining Hybrid References

Run:

```powershell
rg -n "Hybrid|InteractiveAuto|InteractiveServer|Components\.Hybrid|WasmHost child|server-owned shells|@rendermode" BlazorShop.PresentationV2 docs -g "*.md" -g "*.todo.md"
```

Classify results into:

- [ ] Current source-of-truth docs that must be corrected in H0.
- [ ] Historical implementation plans that need a short superseded note only.
- [ ] Route folder conventions where `Hybrid` remains valid as BlazorShop ownership terminology.
- [ ] StorefrontBuilder docs where `Pages/Hybrid` remains a generated project folder convention and should not be changed in H0.
- [ ] Test/code references that must be deferred to H1.

Rules:

- [ ] Do not mechanically replace every `Hybrid` mention.
- [ ] Do not rename folders.
- [ ] Do not edit generated artifacts.
- [ ] Do not edit visual reverse engineering docs unless they are actively misleading about `.NET InteractiveAuto` or `Components.Hybrid`.
- [ ] Preserve route ownership meaning for `Pages/Hybrid`.

Exit criteria:

- [ ] All stale source-of-truth docs are fixed.
- [ ] Remaining `Hybrid` references are either correct, historical, or explicitly deferred.
- [ ] A final scan command and result summary are recorded in implementation notes.

## Phase H0.5 - Create H1 Backlog

Create a separate follow-up backlog file:

```text
docs/refactor-control-Commerce-storefront/Storefront Component Mode Foundation v2.todo.md
```

H1 must be allowed to touch code/tests only after H0 docs are complete.

H1 backlog must include at least:

- [ ] Re-evaluate whether `BlazorShop.Storefront.Components.Hybrid` should remain, be narrowed, be renamed, or be retired.
- [ ] Re-evaluate whether public descriptors should stay tied to physical mode project assembly names.
- [ ] Re-evaluate `StorefrontComponentModeBoundaryValidator` profiles.
- [ ] Re-evaluate `StorefrontComponentModeDependencyTests` exact project reference assertions.
- [ ] Re-evaluate `StorefrontComponentDescriptorTests` descriptor/project mode consistency.
- [ ] Re-evaluate `StorefrontVisualOnlyBoundaryTests.F1_41_ReferenceComponentModeReferences_AreNarrowAndAdoptedOnlyByV2`.
- [ ] Re-evaluate V2 visible contact flow and decide whether the `Components.Hybrid` contact descriptor remains useful.
- [ ] Re-evaluate whether future shared interactive components should live in downloadable WASM assemblies directly instead of a server shell project.
- [ ] Preserve Browser/BFF data path and no direct Commerce Node calls.
- [ ] Preserve V2/Starter/generated visual ownership.
- [ ] Define browser QA required before closing any code-level change.

Exit criteria:

- [ ] H1 has a concrete code/test review queue.
- [ ] H0 remains docs-only.
- [ ] No implementation agent needs to infer the next code phase from scattered notes.

## Phase H0.6 - QA And Closure

Docs-only verification:

- [ ] Run `git diff -- docs/architecture BlazorShop.PresentationV2/COMPONENT-MODES.md BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/README.md docs/refactor-control-Commerce-storefront`.
- [ ] Run the Hybrid wording scan again:

```powershell
rg -n "Hybrid|InteractiveAuto|InteractiveServer|Components\.Hybrid|WasmHost child|server-owned shells|@rendermode" BlazorShop.PresentationV2 docs -g "*.md" -g "*.todo.md"
```

- [ ] Confirm `InteractiveAuto` appears only as a forbidden/non-target clarification, not as recommended architecture.
- [ ] Confirm `InteractiveServer` appears only as a forbidden/non-target clarification or official-doc comparison, not as recommended public storefront behavior.
- [ ] Confirm source-of-truth docs do not state that `Hybrid` requires nested `WasmHost` child hosting.
- [ ] Confirm historical plans have a superseded note instead of being rewritten.
- [ ] Confirm no non-document source files changed:

```powershell
git diff --name-only | rg -v "^(docs/|BlazorShop\.PresentationV2/COMPONENT-MODES\.md|BlazorShop\.PresentationV2/BlazorShop\.Storefront\.Components\.Hybrid/README\.md)"
```

Expected result:

- no output.

No build/test gate:

- [ ] Do not run `dotnet build` unless a non-doc file was accidentally changed.
- [ ] Do not run `dotnet test` unless a test/source file was accidentally changed.
- [ ] Do not run Playwright because no browser behavior changes in H0.

Exit criteria:

- [ ] Docs-only diff is clean and scoped.
- [ ] H1 backlog exists.
- [ ] The current architecture source of truth is no longer internally contradictory.

## Definition Of Done

- [ ] `COMPONENT-MODES.md` defines Hybrid as BlazorShop architectural classification, not as a mandatory physical shell pattern.
- [ ] `COMPONENT-MODES.md` records official ASP.NET Core render-mode facts relevant to this project.
- [ ] `03-runtime-boundaries.md` explains Storefront SSR/Hybrid/WasmHost boundaries.
- [ ] `05-project-and-folder-guide.md` no longer tells agents that `Components.Hybrid` must be the canonical server shell bridge.
- [ ] `10-v2-contract-ownership.md` no longer treats `Hybrid -> WasmHost child` as the required mode contract.
- [ ] `Components.Hybrid/README.md` no longer claims the project is foundation-only or that nested bridge is the desired default.
- [ ] Historical complete plans are annotated, not rewritten as if they never happened.
- [ ] `InteractiveAuto` is explicitly non-target for public Storefront V2.
- [ ] `InteractiveServer` is explicitly non-target for public Storefront V2.
- [ ] H1 backlog exists and lists every code/test/project-graph area that must be reviewed later.
- [ ] H0 made no code, test, project, runtime, DI, render-mode, API, Browser, Builder, or Playwright changes.

## Implementation Notes

- [x] Record baseline `git status --short`.
  - H0.0 baseline: `git status --short` showed only this untracked plan file: `?? "docs/refactor-control-Commerce-storefront/Storefront Hybrid Architecture Clarification.todo.md"`.
- [x] Record the key stale docs found by `rg`.
  - H0.0 stale source-of-truth docs: `BlazorShop.PresentationV2/COMPONENT-MODES.md`, `docs/architecture/05-project-and-folder-guide.md`, `docs/architecture/10-v2-contract-ownership.md`, and `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/README.md` still described Hybrid as a server-owned shell or WasmHost child bridge.
  - H0.0 clarification target: `docs/architecture/03-runtime-boundaries.md` mentioned SSR/Hybrid/WASM-host route shells but lacked an explicit render/runtime definition.
  - H0.0 historical docs needing notes: `Storefront Component Mode Foundation.todo.md`, `Storefront Component Mode Foundation Closure Patch.todo.md`, and `Storefront Reference Components.todo.md`.
  - H0.0 visible contact path: `StorefrontPage.razor` renders `StorefrontContactFormSection @rendermode="InteractiveWebAssembly"`; the section lives in `BlazorShop.Storefront.V2.WASM` and wraps `Components.WasmHost/Content/StorefrontContactFormApp.razor`.
  - H0.0 `Components.Hybrid/Content/StorefrontContactForm.razor` still exists, but the visible V2 route path does not directly use it.
  - H0.0 discounted rail path: `V2/Pages/Hybrid/Catalog/Home.razor` renders `StorefrontDiscountedProductRailSection @rendermode="InteractiveWebAssembly"`; the section lives in `BlazorShop.Storefront.V2.WASM` and wraps `Components.WasmHost/Catalog/StorefrontDiscountedProductRail.razor`.
- [x] Record the official Microsoft docs checked.
  - H0.0 checked Microsoft docs: ASP.NET Core Blazor render modes, prerendering, and JavaScript initializers/startup docs for .NET 10.
- [ ] Record the final Hybrid wording scan summary.
- [ ] Record why no build/test/Playwright gate was required.

## Decision Audit Trail

| # | Decision | Classification | Rationale | Rejected |
|---|---|---|---|---|
| 1 | Make H0 docs-only. | Risk control | The current problem is conflicting architecture language; code/test changes belong in H1 after the source of truth is corrected. | Change project references or render modes during clarification. |
| 2 | Treat `Hybrid` as a BlazorShop architectural classification, not a fixed .NET render mode. | Architecture | Official Blazor render modes do not map 1:1 to BlazorShop route/component ownership folders. | Equate Hybrid with `InteractiveAuto` or `InteractiveServer`. |
| 3 | Keep `InteractiveWebAssembly` as the intended public storefront interactivity path. | Runtime boundary | V2 already uses WASM/browser/BFF flow; server circuit interactivity is not needed for public storefront MVP. | Adopt `InteractiveServer`/SignalR or `InteractiveAuto`. |
| 4 | Mark `Components.Hybrid` physical role as pending H1. | Scope control | Existing tests/code may still depend on this project; retiring or repurposing it requires code/test work outside H0. | Delete/rename/repurpose the project in H0. |
| 5 | Annotate historical plans instead of rewriting them. | Documentation integrity | Completed plans are evidence of what happened; only current architecture docs should become source of truth. | Mass-edit old plan checkboxes and evidence. |
