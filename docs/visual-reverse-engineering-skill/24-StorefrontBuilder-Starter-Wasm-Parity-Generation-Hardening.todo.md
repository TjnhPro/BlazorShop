# StorefrontBuilder Starter WASM Parity And Generation Hardening

## Status

- State: Proposed
- Scope: StorefrontBuilder foundation, Starter server/WASM parity, package freshness, generated multi-project output, visual ownership, regeneration and proof gates.
- Non-goal: Fixing only `BlazorShop.Storefront.Kindredcoast`.
- Target: Generated storefronts and `BlazorShop.Storefront.V2` differ mainly by markup, CSS, layout, assets, view registrations and store configuration, not by browser runtime foundation.

## Autoplan Review Summary

### CEO Review

This phase is necessary before more visual generation work because it removes a structural mismatch instead of continuing to patch one generated storefront at a time.

The business objective is not "make Kindredcoast pass". The objective is to make `BlazorShop.Storefront.Starter` a trustworthy canonical storefront template and make StorefrontBuilder generate the same application shape every time.

Approved decision:

- Build parity into `Starter` and StorefrontBuilder first.
- Use `V2` only as a verified foundation reference.
- Do not copy V2 visual implementation into generated stores.
- Treat Kindredcoast as final pilot evidence, not as the architecture source.

### Engineering Review

Current code confirms the gap:

- `BlazorShop.Storefront.V2` registers `AddStorefrontApplication`, `AddStorefrontBrowserControllers`, `AddV2FoundationViews`, and maps the V2 WASM assembly.
- `BlazorShop.Storefront.V2.WASM` boots with `WebAssemblyHostBuilder` and `AddStorefrontBrowserRuntime`.
- `BlazorShop.Storefront.Browser` owns browser runtime/controller registration.
- `BlazorShop.Storefront.Presentation` already supports `AddInteractiveWebAssemblyComponents` and `AddInteractiveWebAssemblyRenderMode`.
- `BlazorShop.Storefront.Starter` currently maps only server view registration and has no sibling WASM project.
- StorefrontBuilder validators currently assume a single generated project and forbid all `ProjectReference`.
- Package metadata currently covers Client, Runtime, Presentation and Components, but not Browser.

Approved engineering direction:

- Add `BlazorShop.Storefront.Starter.WASM`.
- Update Starter server to use Browser/WASM parity.
- Extend generated output to server + generated sibling WASM.
- Allow exactly one generated-owned ProjectReference from server to sibling WASM.
- Keep external ProjectReference forbidden.
- Add Browser to package versioning, pack scripts, metadata, restore proof and validators.
- Keep Runtime/Client as metadata/transitive package proof only; generated WASM must not directly reference Runtime/Client.

### DX Review

Developer experience risk is high if this phase is vague. Agents must be able to run one command and know:

- which packages were packed;
- which source HEAD produced them;
- which project paths were generated;
- which packages each project restored;
- whether server/WASM resolved the same foundation version;
- whether a failure is caused by stale package, missing Browser wiring, forbidden reference, or visual boundary breach.

Approved DX direction:

- Add clear command output and reports.
- Verify `project.assets.json`, not just `.csproj` text.
- Keep runner failure messages actionable: problem, cause, fix.
- Keep GitHub Actions out of required closure while Actions are disabled in development.

## Architecture Decisions

- Starter is the canonical template source.
- V2 is runtime parity reference only.
- Generated stores are disposable output under approved StorefrontBuilder roots.
- Generated server may reference only its generated sibling WASM project.
- Generated projects consume shared foundation through packages, not monorepo source references.
- Generated browser/WASM code uses `BlazorShop.Storefront.Browser` and browser-safe `BlazorShop.Storefront.Components`; it must not reference `BlazorShop.Storefront.Runtime` or `BlazorShop.Storefront.Client` directly.
- Storefront Presentation remains route/BFF/SEO/media composition owner.
- Generated visual files must not declare `@page`.
- ServiceDefaults/Aspire are not required in generated storefronts for this phase.

## Current Code Anchors

Use these files as baseline during implementation:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Program.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/StorefrontBrowserServiceCollectionExtensions.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontApplicationServiceCollectionExtensions.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontApplicationBuilderExtensions.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Program.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/StorefrontPackageVersions.props`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml`
- `scripts/qa/run-storefront-builder-generated-proof.ps1`
- `scripts/qa/run-storefront-builder-isolation-gate.ps1`
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/new-storefront-project.ps1`
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderGeneratedProject.ps1`
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderStaticGate.ps1`
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderGuard.ps1`
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/record-agent-visual-writes.mjs`
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-visual-qa.mjs`
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/update-generated-files-manifest.mjs`
- `tools/BlazorShop.AI.StorefrontBuilder/regenerate-storefront.ps1`

## Definition Of Done

- [x] `BlazorShop.Storefront.Starter.WASM` exists and builds.
- [x] Starter server references Starter.WASM in monorepo development.
- [x] Starter server registers Browser controllers.
- [x] Starter server maps the Starter.WASM assembly.
- [x] Starter.WASM calls `AddStorefrontBrowserRuntime`.
- [x] StorefrontBuilder generates server and sibling WASM projects.
- [x] Generated server references only generated sibling WASM by ProjectReference.
- [x] Generated server uses package references for Presentation, Components and Browser.
- [x] Generated WASM uses package references for Components and Browser.
- [x] Generated WASM does not direct-reference Runtime or Client.
- [x] Browser package is included in package version props, metadata, pack scripts, proof scripts, static gates and isolation gates.
- [x] Package versions are immutable per build identity.
- [x] Local feed cleanup is scoped and deterministic.
- [x] NuGet global cache cleanup removes only the Storefront package IDs and exact versions for the current run.
- [x] Restore uses `--no-cache --force-evaluate`.
- [x] Server and WASM `project.assets.json` are validated.
- [x] Package ID/version/source/hash in restore output matches generated metadata.
- [x] External ProjectReference remains forbidden.
- [x] V2, V2.WASM, Starter source, Starter.WASM source, backend/core/API and Web.SharedV2 references remain forbidden in generated output.
- [ ] Visual plan/implement/QA tools understand server vs WASM ownership.
- [ ] Regeneration supports server + WASM as one generated artifact.
- [ ] `-WhatIf` reports both server and WASM changes.
- [ ] Generated proof runner can create, restore, build, validate and browser-test a fresh multi-project storefront.
- [ ] Kindredcoast can be freshly generated as pilot proof after foundation gates pass.
- [ ] Final local closure gate passes without requiring GitHub Actions.

## Out Of Scope

- [ ] Do not implement new payment providers.
- [ ] Do not refactor Commerce Node Storefront APIs.
- [ ] Do not change cart, checkout, account, pricing, sellability or auth semantics.
- [ ] Do not introduce a new production deployment topology.
- [ ] Do not add Aspire or ServiceDefaults as a mandatory generated storefront dependency.
- [ ] Do not copy Storefront V2 visual markup into Starter or generated stores.
- [ ] Do not add a new theme system.
- [ ] Do not add a full SSIM/pixel-perfect visual engine.
- [ ] Do not make StorefrontBuilder a production service.

## Phase 0 - Baseline And Contract Freeze

### Goal

Record the application shape that Starter and generated storefronts must match before any implementation changes.

### Tasks

- [x] Inspect V2 server host.
  - [x] Record service registration order.
  - [x] Record `AddStorefrontApplication`.
  - [x] Record `AddStorefrontBrowserControllers`.
  - [x] Record V2 foundation view registration.
  - [x] Record `UseStorefrontApplication`.
  - [x] Record `MapStorefrontApplication`.
  - [x] Record the additional WASM assembly marker.
- [x] Inspect V2 server `.csproj`.
  - [x] Record `Microsoft.AspNetCore.Components.WebAssembly.Server`.
  - [x] Record Browser dependency.
  - [x] Record Components dependency.
  - [x] Record Presentation dependency.
  - [x] Record V2.WASM dependency.
  - [x] Record ServiceDefaults as V2-specific, not generated-storefront required.
- [x] Inspect V2.WASM.
  - [x] Record SDK.
  - [x] Record target framework.
  - [x] Record Browser dependency.
  - [x] Record Components dependency.
  - [x] Record `WebAssemblyHostBuilder`.
  - [x] Record `AddStorefrontBrowserRuntime`.
- [x] Inspect Storefront Presentation host extensions.
  - [x] Confirm Presentation owns interactive render mode support.
  - [x] Confirm additional assemblies are required for WASM mapping.
  - [x] Confirm `EnableInteractiveWebAssembly` behavior.
- [x] Inspect Starter server.
  - [x] Record current missing Browser controllers.
  - [x] Record current missing WASM assembly mapping.
  - [x] Record current package metadata.
  - [x] Record current Starter contract limitations.
- [x] Classify files by ownership.
  - [x] Foundation-owned server files.
  - [x] Foundation-owned WASM files.
  - [x] Generated visual server files.
  - [x] Generated visual WASM files.
  - [x] Protected runtime files.
  - [x] User-owned generated files.
- [x] Write baseline report.
  - [x] Include exact file paths.
  - [x] Include exact required markers.
  - [x] Include decisions for what is V2-specific and not copied.

### Deliverables

- [x] `docs/visual-reverse-engineering-skill/generated-storefront-foundation-contract.md`
- [x] Contract section for server host shape.
- [x] Contract section for WASM host shape.
- [x] Contract section for package ownership.
- [x] Contract section for generated visual ownership.
- [x] Contract section for forbidden references.

### Tests And Gates

- [x] No code changes in this phase except docs.
- [x] `rg -n "AddStorefrontBrowserControllers|AddStorefrontBrowserRuntime|MapStorefrontApplication|InteractiveWebAssembly" BlazorShop.PresentationV2` used as evidence.
- [x] Contract explicitly says V2 visual code is not copied.

### Done When

- [x] A future agent can implement Starter parity from the contract without reading V2 visual implementation.

## Phase 1 - Package Freshness, Browser Package Contract And Provenance

### Goal

Ensure generated projects always consume packages built from the current source state, and add Browser as a first-class generated package dependency.

### Tasks

- [x] Add Browser to package version contract.
  - [x] Add `StorefrontBrowserPackageVersion` to `BlazorShop.Storefront.Starter/StorefrontPackageVersions.props`.
  - [x] Add `BlazorShop.Storefront.Browser` to `starter-generation.contract.yaml` package dependencies.
  - [x] Add Browser to generated metadata package version output.
  - [x] Add Browser to metadata schemas and fixtures.
  - [x] Add Browser to generated-file manifests where package metadata is recorded.
- [x] Pack five packages in every generated proof.
  - [x] Client.
  - [x] Runtime.
  - [x] Presentation.
  - [x] Components.
  - [x] Browser.
- [x] Introduce immutable build identity.
  - [x] Resolve current `HEAD`.
  - [x] Derive short commit SHA.
  - [x] Use version format such as `1.0.0-local.{shortSha}`.
  - [x] Ensure all five packages use the same build identity in a run.
  - [x] Ensure package identity is printed in proof output.
  - [x] Keep existing explicit version parameters for emergency manual override only.
- [x] Scope package cleanup.
  - [x] Delete old run feed under approved `artifacts/storefront-packages` or `obj/storefront-builder/packages`.
  - [x] Delete exact global NuGet package cache folders only for the five package IDs and current versions.
  - [x] Do not clear the whole global NuGet cache.
  - [x] Fail if a resolved cache path is outside `%USERPROFILE%\.nuget\packages`.
- [x] Add package hash provenance.
  - [x] Compute SHA-256 for each `.nupkg`.
  - [x] Write package ID, version, feed path and hash to generated metadata.
  - [x] Write package ID, version, feed path and hash to proof report.
  - [x] Include Browser in all reports.
- [x] Verify restore result.
  - [x] Restore server with `dotnet restore --no-cache --force-evaluate`.
  - [x] Restore WASM with `dotnet restore --no-cache --force-evaluate`.
  - [x] Parse server `project.assets.json`.
  - [x] Parse WASM `project.assets.json`.
  - [x] Validate package IDs.
  - [x] Validate package versions.
  - [x] Validate package source/feed when available.
  - [x] Validate package hashes against metadata when available.
  - [ ] Fail if server and WASM resolve different Storefront package versions.

### Files To Update

- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/StorefrontPackageVersions.props`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml`
- [x] `scripts/qa/run-storefront-builder-generated-proof.ps1`
- [x] `scripts/qa/run-storefront-builder-isolation-gate.ps1`
- [x] `scripts/qa/run-storefront-starter-isolation-gate.ps1`
- [x] `scripts/qa/run-storefront-sample-release-gate.ps1`
- [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/new-storefront-project.ps1`
- [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderGeneratedProject.ps1`
- [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderStaticGate.ps1`
- [x] `tools/BlazorShop.AI.StorefrontBuilder/tests/schemas/fixtures/valid/metadata.json`
- [x] `docs/architecture/11-storefront-builder.md`
- [x] `docs/visual-reverse-engineering-skill/reference.md`

### Negative Tests

- [x] Browser package missing from feed fails.
- [x] Browser package missing from metadata fails.
- [x] Browser package missing from generated server `.csproj` fails.
- [x] Browser package missing from generated WASM `.csproj` fails.
- [x] Stale package with same ID but older version is ignored.
- [ ] Server resolves different version than WASM fails.
- [x] Package hash mismatch fails.
- [x] Package source outside expected local feed fails when local proof requires local feed.
- [x] Cache cleanup path outside NuGet package root fails.

### Done When

- [x] A generated proof cannot pass while using stale Storefront packages from an earlier source state.

## Phase 2 - Create `BlazorShop.Storefront.Starter.WASM`

### Goal

Add a neutral WASM template that generated storefronts can copy and rename.

### Tasks

- [x] Create project folder:
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM`
- [x] Create project file:
  - [x] SDK: `Microsoft.NET.Sdk.BlazorWebAssembly`.
  - [x] Target framework: `net10.0`.
  - [x] Nullable enabled.
  - [x] Implicit usings enabled.
  - [x] `NoDefaultLaunchSettingsFile` set when matching V2.WASM pattern.
  - [x] `StaticWebAssetProjectMode` set consistently with V2.WASM if needed.
  - [x] Root namespace: `BlazorShop.Storefront.Starter.WASM`.
  - [x] Package reference: `Microsoft.AspNetCore.Components.WebAssembly`.
  - [x] ProjectReference to `BlazorShop.Storefront.Browser`.
  - [x] ProjectReference to `BlazorShop.Storefront.Components`.
- [x] Create `Program.cs`.
  - [x] Use `WebAssemblyHostBuilder.CreateDefault(args)`.
  - [x] Call `builder.Services.AddStorefrontBrowserRuntime(builder.HostEnvironment)`.
  - [x] Build and run.
  - [x] No V2 namespace.
  - [x] No Commerce Node direct URL.
  - [x] No Runtime/Client references.
- [x] Create `_Imports.razor`.
  - [x] Use Starter.WASM namespace.
  - [x] Import Components contracts/headless namespaces needed by neutral hosts.
  - [x] Avoid V2 imports.
- [x] Add neutral interactive host components.
  - [x] `Components/Account/StorefrontAccountApp.razor`
  - [x] `Components/Cart/StorefrontCartApp.razor` or existing Starter-compatible name.
  - [x] `Components/Checkout/StorefrontCheckoutApp.razor` or existing Starter-compatible name.
  - [x] Keep host components visually neutral.
  - [x] Use Browser controllers and Components contracts/headless state.
  - [x] Do not copy V2 CSS/class bags.
  - [x] Do not define `@page`.
  - [x] Do not call Commerce Node directly.
- [x] Add stable assembly marker.
  - [x] Prefer existing neutral host component as marker.
  - [x] Document marker in contract.

### Files To Add

- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/BlazorShop.Storefront.Starter.WASM.csproj`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/Program.cs`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/_Imports.razor`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/Components/Account/StorefrontAccountApp.razor`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/Components/Cart/*`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/Components/Checkout/*`

### Tests And Gates

- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/BlazorShop.Storefront.Starter.WASM.csproj`
- [x] `rg -n "BlazorShop.Storefront.V2|CommerceNode|ControlPlane|BlazorShop.Storefront.Runtime|BlazorShop.Storefront.Client" BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM` returns no forbidden runtime references.
- [x] WASM project compiles without server-only dependencies.
- [x] Account, cart and checkout host components compile.

### Done When

- [x] Starter.WASM exists as a neutral, browser-safe template and can be mapped by the Starter server.

## Phase 3 - Starter Server Runtime Parity

### Goal

Upgrade Starter server from SSR shell to canonical generated server host with Browser/WASM foundation.

### Tasks

- [x] Update Starter server `.csproj`.
  - [x] Add ProjectReference to `BlazorShop.Storefront.Browser`.
  - [x] Keep ProjectReference to Presentation for monorepo development.
  - [x] Keep Components as monorepo ProjectReference and rewrite it to PackageReference in independent proof/package mode.
  - [x] Add ProjectReference to `BlazorShop.Storefront.Starter.WASM`.
  - [x] Add `Microsoft.AspNetCore.Components.WebAssembly.Server`.
  - [x] Do not add ServiceDefaults as required dependency.
- [x] Update Starter `Program.cs`.
  - [x] Add Browser using.
  - [x] Register `AddStorefrontApplication(builder.Configuration)`.
  - [x] Register `AddStorefrontBrowserControllers()`.
  - [x] Register `AddStarterFoundationViews()`.
  - [x] Use `UseStorefrontApplication()`.
  - [x] Map `MapStorefrontApplication(typeof(StarterFoundationViewRegistration), typeof(BlazorShop.Storefront.Starter.WASM.Components.Account.StorefrontAccountApp).Assembly)`.
  - [x] Keep namespace/partial Program consistent.
- [x] Confirm route ownership.
  - [x] Starter visual files must not add `@page`.
  - [x] Presentation remains route truth.
  - [x] `starter-generation.contract.yaml` must list route metadata, not route declarations.
- [x] Confirm static web assets.
  - [x] Server can serve WASM static assets.
  - [x] Direct refresh of `/account`, `/cart`, `/checkout` does not fail due to missing WASM assembly.
- [x] Update docs.
  - [x] `docs/architecture/11-storefront-builder.md`
  - [x] `docs/architecture/01-system-map.md`
  - [x] `docs/architecture/05-project-and-folder-guide.md`
  - [x] `docs/architecture/03-runtime-boundaries.md`
  - [x] `docs/agents/storefront-builder.md`

### Tests And Gates

- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj`
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/BlazorShop.Storefront.Starter.WASM.csproj`
- [x] Architecture test confirms Starter server registers Browser controllers.
- [x] Architecture test confirms Starter server maps Starter.WASM assembly.
- [x] Architecture test confirms Starter server does not reference V2/V2.WASM.
- [x] Architecture test confirms Starter.WASM does not reference Runtime/Client.

### Done When

- [x] Starter server and Starter.WASM together represent the canonical generated storefront foundation.

## Phase 4 - Multi-Project Storefront Generation

### Goal

Make StorefrontBuilder generate both server and WASM projects from Starter templates.

### Generated Shape

```text
BlazorShop.Storefront.{Name}/
├── BlazorShop.Storefront.{Name}.csproj
├── Program.cs
├── StorefrontPackageVersions.props
├── starter-generation.contract.yaml
├── Components/
├── Pages/
├── Features/
├── wwwroot/
├── docs/
│   └── storefront-analysis/
└── BlazorShop.Storefront.{Name}.WASM/
    ├── BlazorShop.Storefront.{Name}.WASM.csproj
    ├── Program.cs
    ├── _Imports.razor
    ├── Components/
    │   ├── Account/
    │   ├── Cart/
    │   └── Checkout/
    └── wwwroot/
```

### Tasks

- [x] Update copy/generation flow.
  - [x] Copy Starter server into staging root.
  - [x] Copy Starter.WASM into staging root as a nested sibling.
  - [x] Rename server `.csproj`.
  - [x] Rename WASM `.csproj`.
  - [x] Rewrite server root namespace.
  - [x] Rewrite WASM root namespace.
  - [x] Rewrite server `Program.cs` assembly marker from Starter.WASM to generated WASM.
  - [x] Rewrite `_Imports.razor` namespace.
  - [x] Rewrite any generated project-local namespaces.
  - [x] Rewrite package version props.
  - [x] Rewrite metadata paths.
- [x] Update server references.
  - [x] PackageReference Presentation.
  - [x] PackageReference Components.
  - [x] PackageReference Browser.
  - [x] PackageReference `Microsoft.AspNetCore.Components.WebAssembly.Server`.
  - [x] ProjectReference to generated sibling WASM only.
  - [x] No ProjectReference to monorepo source.
- [x] Update WASM references.
  - [x] PackageReference Browser.
  - [x] PackageReference Components.
  - [x] PackageReference `Microsoft.AspNetCore.Components.WebAssembly`.
  - [x] No ProjectReference.
  - [x] No Runtime package.
  - [x] No Client package.
- [x] Update generated `nuget.config`.
  - [x] Place at generated root.
  - [x] Ensure both server and WASM use same local feed.
  - [x] Use relative local package feed path.
  - [x] Keep nuget.org only for third-party packages.
- [x] Preserve atomic generation.
  - [x] Generate into staging root.
  - [x] Validate staging artifact before replace.
  - [x] Move existing target to backup only after staging validates.
  - [x] Restore backup if move/apply fails.
  - [x] Cleanup staging and backup safely.
  - [x] Verify all resolved paths stay under approved output root.
- [x] Update handoff generation.
  - [x] Handoff plan knows server target project.
  - [x] Handoff plan knows WASM target project.
  - [x] Agent task package includes project ownership.
  - [x] Handoff placeholders can target server or WASM.
  - [x] Handoff generation summary reports server/WASM outputs.

### Files To Update

- [x] `scripts/generate-storefront-sample.ps1`
- [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/new-storefront-project.ps1`
- [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/plan-generation-files.mjs`
- [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/apply-handoff-project-skeleton.mjs`
- [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/write-agent-task-package.mjs`
- [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/generated-file-manifest.mjs`
- [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/update-generated-files-manifest.mjs`

### Tests And Gates

- [x] Generate with friendly name.
- [x] Generate with full `BlazorShop.Storefront.{Name}` name.
- [x] Invalid name fails before file writes.
- [x] Generated server project exists.
- [x] Generated WASM project exists.
- [x] Generated server references generated sibling WASM.
- [x] Generated WASM has no ProjectReference.
- [x] Generated output contains no `BlazorShop.Storefront.Starter` namespace.
- [x] Generated output contains no `BlazorShop.Storefront.V2` namespace.
- [x] Generated server `Program.cs` maps generated WASM assembly.
- [x] Generation fail preserves old target.
- [x] Generated artifact is not added to `BlazorShop.sln`.

### Done When

- [x] One StorefrontBuilder command creates a complete server + WASM generated storefront.

## Phase 5 - Validator, Isolation And Metadata Hardening

### Goal

Update validators from single-project assumptions to precise multi-project generated ownership.

### Tasks

- [x] Update generated project validator.
  - [x] Validate server `.csproj`.
  - [x] Validate WASM `.csproj`.
  - [x] Validate server `Program.cs`.
  - [x] Validate WASM `Program.cs`.
  - [x] Validate server metadata.
  - [x] Validate WASM metadata.
  - [x] Validate generated file manifest project ownership.
- [x] Update static gate.
  - [x] Require Browser package.
  - [x] Require server/WASM paths.
  - [x] Require no generated visual `@page`.
  - [x] Require no direct Commerce Node browser calls.
  - [x] Require no generated functional JavaScript outside approved visual zone.
- [x] Update isolation gate.
  - [x] Allow generated server ProjectReference to `BlazorShop.Storefront.{Name}.WASM`.
  - [x] Reject every ProjectReference whose resolved path leaves generated root.
  - [x] Reject ProjectReference to V2.
  - [x] Reject ProjectReference to V2.WASM.
  - [x] Reject ProjectReference to Starter.
  - [x] Reject ProjectReference to Starter.WASM.
  - [x] Reject ProjectReference to backend/core/API/Web.SharedV2.
  - [x] Reject generated WASM ProjectReference.
  - [x] Reject Runtime/Client direct package references in generated server and WASM.
  - [x] Keep Runtime/Client version metadata validation.
- [x] Update metadata.
  - [x] Add `projects.server.path`.
  - [x] Add `projects.wasm.path`.
  - [x] Add server package references.
  - [x] Add WASM package references.
  - [x] Add Browser package version/hash/source.
  - [x] Add source HEAD.
  - [x] Add package build identity.
  - [x] Add Starter contract hash.
  - [x] Add Starter.WASM contract hash.
  - [x] Add handoff/generation plan hash when applicable.
- [x] Update generated files manifest.
  - [x] Add `project: server|wasm`.
  - [x] Add `owner: generated|managed|protected|user`.
  - [x] Add protected runtime files.
  - [x] Add visual allowed files.
  - [x] Add obsolete reporting across both projects.

### Negative Tests

- [x] Missing WASM project fails.
- [x] Missing server project fails.
- [x] Server missing Browser package fails.
- [x] WASM missing Browser package fails.
- [x] Server missing Browser controller registration fails.
- [x] WASM missing Browser runtime registration fails.
- [x] Server missing WASM assembly mapping fails.
- [x] Generated server ProjectReference points outside root fails.
- [x] Generated WASM ProjectReference exists fails.
- [x] Direct Runtime package reference fails.
- [x] Direct Client package reference fails.
- [x] V2 namespace fails.
- [x] Starter namespace after rewrite fails.
- [x] Browser package hash mismatch fails.
- [x] Missing project ownership in generated file manifest fails.
- [x] Generated `@page` route declaration fails.

### Done When

- [x] Validators distinguish allowed generated sibling reference from forbidden monorepo source reference.

## Phase 6 - Visual Plan, Implement And QA Multi-Project Support

### Goal

Make visual skills and StorefrontBuilder task packages understand server-owned visual files and WASM-owned visual files.

### Tasks

- [x] Update generation plan schema.
  - [x] Add `targetProject`.
  - [x] Values: `server`, `wasm`.
  - [x] Add project-relative path.
  - [x] Add artifact-root-relative path.
  - [x] Add ownership.
  - [x] Add protected markers.
- [x] Update agent task package.
  - [x] Include server project root.
  - [x] Include WASM project root.
  - [x] Include server allowed visual files.
  - [x] Include WASM allowed visual files.
  - [x] Include server protected files.
  - [x] Include WASM protected files.
  - [x] Include package hashes.
  - [x] Include generation plan hash.
  - [x] Include handoff hash when applicable.
- [x] Update `storefront-visual-plan`.
  - [x] Plan server visual slots separately.
  - [x] Plan WASM visual slots separately.
  - [x] Account, cart and checkout visual work targets WASM by default.
  - [x] SSR layout/catalog/content targets server by default.
  - [x] Product/cart descriptors remain Presentation-owned.
- [x] Update `storefront-visual-implement`.
  - [x] Allow server visual CSS and Razor files.
  - [x] Allow WASM visual Razor/CSS files.
  - [x] Reject server `Program.cs`.
  - [x] Reject WASM `Program.cs`.
  - [x] Reject `.csproj`.
  - [x] Reject runtime registration changes.
  - [x] Reject BFF/API transport changes.
  - [x] Reject route declarations.
  - [x] Reject auth/cart/checkout semantic changes.
- [x] Update visual write recorder.
  - [x] Record project for every written file.
  - [x] Validate planned target project.
  - [x] Validate allowed path under correct project.
  - [x] Reject writes crossing project boundary.
  - [x] Reject protected runtime files.
- [x] Update visual QA.
  - [x] Check server SSR routes.
  - [x] Check WASM routes/components.
  - [x] Check WASM bootstrap loads.
  - [x] Check interactive component hydration/startup.
  - [x] Check same-origin network calls.
  - [x] Check direct route refresh.
  - [x] Check browser console errors.
  - [x] Check generated CSS is linked.
  - [x] Check no horizontal overflow.

### Allowed Visual Targets

- [x] Server:
  - [x] `Components/Layout/**`
  - [x] `Components/Catalog/**`
  - [x] `Components/Commerce/**` only when it is server visual wrapper, not action semantics.
  - [x] `Components/States/**`
  - [x] `Pages/Ssr/**`
  - [x] `Pages/Hybrid/**` visual wrappers only, no route declarations.
  - [x] `wwwroot/css/**`
  - [x] `wwwroot/assets/generated/**`
- [x] WASM:
  - [x] `BlazorShop.Storefront.{Name}.WASM/Components/Account/**`
  - [x] `BlazorShop.Storefront.{Name}.WASM/Components/Cart/**`
  - [x] `BlazorShop.Storefront.{Name}.WASM/Components/Checkout/**`
  - [x] `BlazorShop.Storefront.{Name}.WASM/wwwroot/**` visual assets only when needed.

### Forbidden Visual Targets

- [x] Server `Program.cs`
- [x] WASM `Program.cs`
- [x] Server `.csproj`
- [x] WASM `.csproj`
- [x] `StorefrontPackageVersions.props`
- [x] `starter-generation.contract.yaml`
- [x] `nuget.config`
- [x] BFF endpoint code.
- [x] API transport code.
- [x] Auth/cart/checkout action descriptors.
- [x] SEO route behavior.
- [x] Direct Commerce Node calls.
- [x] Direct Runtime/Client package references.

### Done When

- [x] Visual agent can change account/cart/checkout visuals in generated WASM without touching runtime behavior.

## Phase 7 - Regeneration And Runner Hardening

### Goal

Make regeneration and proof runners manage server + WASM as one generated artifact.

### Tasks

- [x] Update regeneration candidate creation.
  - [x] Candidate includes server project.
  - [x] Candidate includes WASM project.
  - [x] Candidate uses current Starter server.
  - [x] Candidate uses current Starter.WASM.
  - [x] Candidate uses current package metadata.
- [x] Update regeneration plan.
  - [x] Track create/update/delete/conflict/obsolete per project.
  - [x] Track generated-owned server files.
  - [x] Track generated-owned WASM files.
  - [x] Track protected files.
  - [x] Track user-owned files.
  - [x] Track out-of-scope files by project.
- [x] Update regeneration scopes.
  - [x] `all` includes server + WASM generated files.
  - [x] `page` applies only server route/view visual files unless plan marks WASM.
  - [x] `component` accepts server or WASM components.
  - [x] `css` includes server and WASM CSS only.
  - [x] `foundation` can update package metadata and Starter contracts after explicit review.
  - [x] `validate` validates both projects.
  - [x] `conflicts` reports both projects.
- [x] Update `-WhatIf`.
  - [x] Report server and WASM file actions.
  - [x] Print stable report path.
  - [x] Keep report outside target project.
  - [x] Include package drift.
  - [x] Include Starter contract drift.
  - [x] Include Starter.WASM contract drift.
  - [x] Include conflict next actions.
- [x] Update runner.
  - [x] Resolve current HEAD.
  - [x] Clean approved package feed.
  - [x] Clear exact NuGet cache entries.
  - [x] Pack five packages.
  - [x] Generate server + WASM.
  - [x] Restore server.
  - [x] Restore WASM.
  - [x] Verify `project.assets.json`.
  - [x] Build server.
  - [x] Build WASM if needed separately.
  - [x] Run static gate.
  - [x] Run isolation gate.
  - [x] Run visual boundary gate.
  - [x] Run fast browser proof.
  - [x] Run regeneration no-op proof.
  - [x] Run manual-edit conflict proof.
- [x] Improve output.
  - [x] Print source HEAD.
  - [x] Print package build identity.
  - [x] Print package versions.
  - [x] Print package hashes.
  - [x] Print server project path.
  - [x] Print WASM project path.
  - [x] Print restore sources.
  - [x] Print resolved package versions.
  - [x] Print runtime proof report paths.
  - [x] Print visual QA report paths.

### Negative Tests

- [x] No-op regeneration changes no files.
- [x] Missing server file is recreated if generated-owned.
- [x] Missing WASM file is recreated if generated-owned.
- [x] Manual edit in generated server file reports conflict.
- [x] Manual edit in generated WASM file reports conflict.
- [x] User-owned server file is preserved.
- [x] User-owned WASM file is preserved.
- [x] Protected file is skipped except foundation scope.
- [x] Obsolete server file is reported.
- [x] Obsolete WASM file is reported.
- [x] Stale package foundation drift blocks visual scope.
- [x] Starter.WASM contract drift requires foundation update.

### Done When

- [x] Developer can run one proof command and get a reliable multi-project generated storefront plus actionable reports.

## Phase 8 - Generated Proof And Kindredcoast Pilot

### Goal

Prove the foundation first with the canonical generated proof, then regenerate Kindredcoast fresh as a real-site pilot.

### Part A - Canonical Generated Proof

- [ ] Start from clean working tree or record intentional dirty files.
- [ ] Run package freshness proof.
- [ ] Generate `BlazorShop.Storefront.GeneratedProof`.
- [ ] Confirm generated server exists.
- [ ] Confirm generated WASM exists.
- [ ] Confirm package metadata includes five packages.
- [ ] Confirm server/WASM restore from current package feed.
- [ ] Build generated server.
- [ ] Run static validator.
- [ ] Run isolation gate.
- [ ] Run structure proof.
- [ ] Run `FoundationFunctionalFast`.
- [ ] Run regeneration no-op proof.
- [ ] Run manual-edit conflict proof.

### Part B - Kindredcoast Fresh Pilot

- [ ] Do not reuse stale Kindredcoast generated output.
- [ ] Do not reuse stale package feed.
- [ ] Do not use stale reverse-engineering `resume` artifacts as final proof.
- [ ] Record current source HEAD.
- [ ] Run fresh reverse-engineering capture for Kindredcoast if needed.
- [ ] Validate Phase 3A readiness.
- [ ] Validate Phase 3B/3C handoff readiness if handoff input is used.
- [ ] Validate portable handoff package.
- [ ] Generate:
  - [ ] `BlazorShop.Storefront.Kindredcoast`
  - [ ] `BlazorShop.Storefront.Kindredcoast.WASM`
- [ ] Restore server.
- [ ] Restore WASM.
- [ ] Verify package hashes.
- [ ] Build.
- [ ] Run visual plan.
- [ ] Run visual implement.
- [ ] Record changed visual files.
- [ ] Run visual QA.
- [ ] Run browser functional proof.

### Browser Proof Cases

- [ ] Home SSR renders.
- [ ] Catalog page renders.
- [ ] Product detail renders.
- [ ] Product image/gallery renders.
- [ ] Product selection preview works if fixture data supports it.
- [ ] Quantity changes work.
- [ ] Add-to-cart uses same-origin route.
- [ ] Cart badge updates.
- [ ] Cart page hydrates.
- [ ] Account page hydrates.
- [ ] Checkout page hydrates.
- [ ] Direct refresh of `/account` works.
- [ ] Direct refresh of `/cart` works.
- [ ] Direct refresh of `/checkout` works.
- [ ] Browser network audit shows no direct Commerce Node calls.
- [ ] Console audit has no blocking errors.

### Done When

- [ ] Canonical generated proof passes first.
- [ ] Kindredcoast passes as pilot evidence, not as architecture source.

## Phase 9 - Final Local Closure Gate

### Goal

Close the phase with local, repeatable proof. GitHub Actions are not required while disabled during development.

### Required Gates

- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/BlazorShop.Storefront.Starter.WASM.csproj`
- [ ] StorefrontBuilder generation tests.
- [ ] StorefrontBuilder static validator tests.
- [ ] StorefrontBuilder isolation tests.
- [ ] Package freshness tests.
- [ ] Metadata/provenance tests.
- [ ] Regeneration ownership tests.
- [ ] Visual schema/example validation.
- [ ] `run-storefront-builder-generated-proof.ps1 -ProofLevel Structure`
- [ ] `run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast`
- [ ] `run-storefront-builder-regeneration-gate.ps1`
- [ ] `run-storefront-phase4-mvp-gate.ps1` where visual/handoff artifacts are involved.
- [ ] Kindredcoast targeted proof if Kindredcoast pilot is part of the same closure.

### Final Gate Must Reject

- [ ] Mutable stale package.
- [ ] Missing Browser package.
- [ ] Missing WASM project.
- [ ] Server missing Browser controllers.
- [ ] WASM missing Browser runtime.
- [ ] Server missing WASM assembly mapping.
- [ ] External ProjectReference.
- [ ] Generated WASM ProjectReference.
- [ ] Direct Runtime package in generated server or WASM.
- [ ] Direct Client package in generated server or WASM.
- [ ] V2/V2.WASM reference.
- [ ] Starter source reference in generated output.
- [ ] Backend/core/API reference.
- [ ] Package hash mismatch.
- [ ] Package source mismatch.
- [ ] Stale runtime evidence.
- [ ] Visual changes outside allowlist.
- [ ] Protected runtime edit.
- [ ] Generated `@page` declarations.
- [ ] Direct Commerce Node browser calls.
- [ ] Non-deterministic regeneration.

### Final Evidence

- [ ] Tested source HEAD.
- [ ] Final source HEAD.
- [ ] Package build identity.
- [ ] Five package versions.
- [ ] Five package hashes.
- [ ] Server generated path.
- [ ] WASM generated path.
- [ ] Server restore proof.
- [ ] WASM restore proof.
- [ ] Static validation report.
- [ ] Isolation report.
- [ ] Runtime browser proof report.
- [ ] Visual QA report when applicable.
- [ ] Regeneration report.
- [ ] Final git status.

### Done When

- [ ] A clean local run proves Starter parity, multi-project generation, package freshness, visual boundary safety and regeneration behavior.

## Implementation Order

1. Phase 0 - Baseline and contract freeze.
2. Phase 1 - Package freshness, Browser package contract and provenance.
3. Phase 2 - Starter.WASM.
4. Phase 3 - Starter server runtime parity.
5. Phase 4 - Multi-project StorefrontBuilder generation.
6. Phase 5 - Validator, isolation and metadata hardening.
7. Phase 6 - Visual plan/implement/QA multi-project support.
8. Phase 7 - Regeneration and runner hardening.
9. Phase 8A - Canonical generated proof.
10. Phase 8B - Kindredcoast pilot.
11. Phase 9 - Final local closure gate.

Do not start Phase 6 visual tool changes before Phase 4 and Phase 5 can generate and validate multi-project output. Do not use Kindredcoast as evidence until the canonical generated proof passes.

## Agent Checklist Before Coding Each Phase

- [ ] Read `AGENTS.md`.
- [ ] Read `docs/architecture/README.md`.
- [ ] Read `docs/architecture/11-storefront-builder.md`.
- [ ] Read `docs/visual-reverse-engineering-skill/README.md`.
- [ ] Read `docs/agents/storefront-builder.md`.
- [ ] Read this todo file.
- [ ] Search existing code with `rg`.
- [ ] Identify exact files touched.
- [ ] Keep changes phase-scoped.
- [ ] Update docs and gates in the same phase as behavior changes.
- [ ] Run focused verification before committing.
- [ ] Do not commit generated artifacts unless the phase explicitly asks for tracked evidence.

## Risk Register

| Risk | Severity | Mitigation |
| --- | --- | --- |
| Generated server accidentally references monorepo Starter.WASM | High | Resolve ProjectReference target path and require it stays under generated root. |
| Generated WASM directly references Runtime/Client | High | Static validator and isolation gate reject Runtime/Client package references in WASM. |
| Browser package remains missing from proof scripts | High | Phase 1 adds Browser to props, pack scripts, validators and metadata fixtures. |
| Stale NuGet package passes tests | High | Immutable build identity, exact cache cleanup and project.assets verification. |
| Visual agent edits runtime files | High | Project-aware allowed/protected file lists and write recorder validation. |
| Kindredcoast pilot hides foundation failure | Medium | Require canonical generated proof before Kindredcoast. |
| Cache cleanup deletes unrelated developer packages | Medium | Delete only exact Storefront package ID/version directories after root safety check. |
| ServiceDefaults leaks into generated template | Medium | Mark ServiceDefaults as V2-specific and test generated output for absence. |
| Route ownership drifts into generated visual files | Medium | Reject generated `@page` declarations and extra route assemblies. |
| Multi-project regeneration misses obsolete files | Medium | Add project field to generated file manifest and obsolete report. |

## Verification Command Set

Use exact commands where applicable and update them as scripts evolve:

```powershell
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\BlazorShop.Storefront.Starter.csproj
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Starter.WASM\BlazorShop.Storefront.Starter.WASM.csproj
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof
.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof -StoreKey sample
```

When Kindredcoast pilot is included:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\reverse-engineering\run-storefront-reverse-engineering-production.ps1 -Url "https://www.kindredcoast.com/" -Name "KindredCoast" -Force -CommandTimeoutSeconds 900
```

## Closure Notes

This plan intentionally keeps the phase focused on shared foundation correctness. After it closes, the AI visual generator can safely build on a generated storefront shape that already has the same server/WASM/browser runtime foundation as V2.
