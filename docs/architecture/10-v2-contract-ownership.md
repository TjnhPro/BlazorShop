# V2 Contract Ownership

This page records the current contract ownership boundary after Storefront V2 moved away from `Web.SharedV2` as a business DTO bucket.

## Rules

- Public HTTP contracts live at the API boundary that exposes them.
- Commerce Node Storefront public HTTP contracts are canonical at the `api/storefront/stores/{storeKey}/*` boundary.
- Generated Storefront clients are frontend-readable contracts and must be regenerated from the canonical Storefront OpenAPI contract at `contracts/storefront/storefront.openapi.json` instead of hand-copied into frontend packages.
- Storefront frontend view models are allowed when they are presentation or composition models.
- Storefront frontend code must not add handwritten duplicate API DTO clones when the schema should come from OpenAPI-generated contracts.
- Storefront browser/local endpoint contracts live in `BlazorShop.Storefront.Presentation/Endpoints/Contracts` or `BlazorShop.Storefront.Presentation/Services/Contracts` when the contract is shared by Presentation route/BFF/page services. A host may own a narrower local extension only when it is documented and not duplicated by Presentation.
- Storefront component contracts are presentation contracts, not public HTTP API contracts. Use `BlazorShop.Storefront.Components/Contracts/*` for stable render/input models, `Headless/*` for state/behavior, and `Browser/*` for same-origin browser primitives. Store-specific visual templates are host-owned; Storefront V2 interactive root components that hydrate in the browser live in its WASM client assembly.
- Storefront Presentation owns shared page contexts, shell contexts, form contexts, local mutation contracts, route/action descriptors, SEO/status contexts, and BFF request/response contracts. Storefront V2, Starter, and generated storefronts render those contracts and submit to the supplied same-origin actions.
- Storefront V2 is a visual host only. Its server project must not directly reference `BlazorShop.Storefront.Runtime` or `BlazorShop.Storefront.Client`, inject `IStorefront*` application services, define shared contracts, or own BFF/manual mutation DTOs.
- Storefront V2 source must not import `BlazorShop.Web.SharedV2`/`Web.SharedV2` or backend/core business namespaces.
- Storefront Starter and generated storefront source must not import `BlazorShop.Web.SharedV2`/`Web.SharedV2` or backend/core business namespaces.
- Storefront Starter must use `AddStorefrontApplication()`, `UseStorefrontApplication()`, and `MapStorefrontApplication()` plus Starter view registrations only. It must consume generated Storefront client contracts through Runtime/Presentation by default and must not copy the manual `StorefrontApiClient` transport from Storefront V2.
- Generated StorefrontBuilder projects must consume `BlazorShop.Storefront.Presentation` and `BlazorShop.Storefront.Components` through package boundaries. Presentation exposes Runtime as the application dependency, and Runtime owns the direct `BlazorShop.Storefront.Client` generated transport dependency. Generated projects keep Client/Runtime package metadata for compatibility proof but must not directly compile against Runtime or Client types unless a documented low-level transport extension explicitly reopens that boundary. They must not reference Storefront V2, `BlazorShop.Web.SharedV2`, or backend/core/API projects.
- Storefront Runtime is server/BFF-only. Server hosts use `AddStorefrontPlatformRuntime` for the full surface or explicit `AddStorefront{Capability}Runtime` methods for narrow composition; compatibility aliases such as `AddStorefrontServerGeneratedClients` and `AddStorefrontGeneratedClients` are not part of the current API surface. Browser/WASM code uses same-origin local endpoints and browser-safe Components primitives instead.
- Starter manual HTTP exceptions are allowed only when documented in an exception registry with reason, owner, test, and revisit trigger.
- The Starter generated-client adoption policy and exception registry live under `docs/storefront-platform/`.
- `Web.SharedV2` may keep browser helpers and transitional model folders during migration, but new business model folders are not allowed.
- `Web.SharedV2` is currently a transitional Control Plane/shared browser-helper bucket. Storefront source must not reuse Control Plane auth/token/JWT helpers from it.
- Do not add Storefront-specific files, namespaces, route helpers, cookie names, or business models to `Web.SharedV2`; Storefront contracts belong in Storefront V2 local contracts, `Storefront.Client`, `Storefront.Runtime`, or `Storefront.Components`.
- If Control Plane Web becomes the only active consumer of `Web.SharedV2`, a later phase should merge the remaining helpers into `BlazorShop.ControlPlane.Web` or extract a small generic helper package only after at least two active consumers need it.
- Generated Storefront clients should target Commerce Node Storefront OpenAPI first. Control Plane generation is a later decision.

## Current `Web.SharedV2/Models` Inventory

| Folder/file group | Current consumers | Classification | Migration direction |
| --- | --- | --- | --- |
| `Models/Authentication` | Control Plane Web imports, legacy shared auth helpers, Storefront auth forms through Application DTOs | Transitional shared auth DTOs | Keep frozen until Control Plane auth contracts are split; do not add Storefront-only auth models here. |
| `Models/Category` | Storefront catalog pages/SEO, Control Plane compatibility tests and legacy-style admin models | Mixed Storefront + Control Plane | Move Storefront read models to generated Storefront client or Storefront V2 contracts; keep admin mutations out of Storefront contracts. |
| `Models/Product` | Storefront catalog/product/search pages, structured data, tests, Control Plane compatibility paths | Mixed Storefront + Control Plane | Storefront product read models should be generated from Storefront OpenAPI; admin create/update models belong to Control Plane/API contracts. |
| `Models/Discovery` | Storefront sitemap/discovery services and tests | Storefront-only | Candidate for Storefront V2 contracts or generated Storefront client. |
| `Models/Pages` | Storefront pages and SEO composition | Storefront-only | Candidate for Storefront V2 contracts or generated Storefront client. |
| `Models/Payment` | Storefront payment/order pages, historical/admin-compatible payment DTOs | Mixed Storefront order/payment + admin shipping/tracking | Split Storefront order/payment responses to generated Storefront client; admin shipping/tracking requests belong behind Control Plane contracts. |
| `Models/Seo` | Storefront SEO composition and Control Plane SEO admin flows | Mixed Storefront + Control Plane | Storefront SEO reads should move to Storefront contracts/generated client; SEO admin mutations belong to Control Plane/API contracts. |
| Root model files (`PagedResult`, `QueryResult`, `ServiceResponse*`, `ApiCall`, `Unit`, `LoginResponse`, `ToastModel`) | Shared browser helpers, Control Plane Web, some Storefront service contracts | Shared utility/transitional | Keep only genuinely shared primitives; do not add business models at root. |

## Existing Storefront V2 Contracts

`BlazorShop.Storefront.Presentation` already owns shared Storefront-local contracts for:

- Address lookup.
- Cart and product-selection preview.
- Checkout browser state and checkout commands.
- Consent.
- Current store/configuration/currency.
- Customer account, addresses, and orders.
- Payment attempts/methods.
- SEO, sitemap, pages, catalog, and rendering helpers.

`BlazorShop.PresentationV2/BlazorShop.Storefront.Client` is the generated Storefront HTTP client package. It is generated from the canonical committed Storefront contract at `contracts/storefront/storefront.openapi.json`, not from test snapshots, and must not reference backend/core/API projects or `Storefront.V2`. Test snapshots remain breaking-change guardrails only. Storefront V2 should consume this generated client through Runtime/Presentation instead of adding handwritten API DTO clones or direct host-view usage.

Generated StorefrontBuilder projects are not contract owners. They are disposable artifacts under ignored generated output roots, consume Storefront Presentation/Components packages directly, hold generated visual output, and keep review artifacts under their local `docs/storefront-analysis/`. Runtime remains a transitive Presentation dependency, and Client remains the generated API transport package owned by Runtime.

Generated storefronts must not infer Storefront API envelopes or field names from screenshots, examples, component models, or `Storefront.Components` render contracts. Server-side Runtime facades map typed generated client envelopes into Presentation/host-facing runtime results; browser components consume Presentation BFF contracts, documented host-local extensions, or `Storefront.Components` presentation/headless contracts.

## Portable Component Models And Headless Contracts

`BlazorShop.Storefront.Components/Contracts/*` is the preferred home for small render-facing models such as product summary cards, product gallery items, and purchase panel snapshots after the headless presentation refactor.

`BlazorShop.Storefront.Components/Headless/*` is the preferred home for browser-safe presentation state and action/event contracts that can be reused without Storefront V2 visual markup.

`BlazorShop.Storefront.Components/Features/*` was a temporary compatibility surface and is retired. Reintroducing shared visual Razor wrappers requires a new architecture decision; normal storefront implementation must keep route composition in Storefront Presentation and keep markup, CSS, layout, copy, and visual templates in V2 host/WASM client, Starter, or generated/custom storefronts.

These models are not public HTTP contracts. Storefront Presentation or a host maps API DTOs or local endpoint contracts into them before composition. They must not reference `Web.SharedV2`, `Application`, `Domain`, `Infrastructure`, Control Plane, Commerce Node runtime projects, Storefront API clients, or Storefront route helpers.

Do not add admin-owned fields, store ownership fields, credentials, tokens, passwords, server-owned publication flags, or cost/internal accounting fields to these component models.

Do not put V2 theme/layout classes, page containers, route strings, or hardcoded same-origin endpoint paths in reusable headless contracts. Host/storefront projects provide visual templates and route/action descriptors; WASM-owned interactive V2 components may consume those descriptors only after the V2 host supplies them.

## Guardrails

- `V2ArchitectureBoundaryBaselineTests.WebSharedV2BusinessModelFolders_AreFrozenDuringContractMigration` freezes the allowed `Web.SharedV2/Models` folders.
- `StorefrontPageCompositionGuardrailTests.StorefrontComponentFeatures_DoNotDependOnBackendOrRouteContracts` keeps portable feature components presentation-only.
- `StorefrontPageCompositionGuardrailTests.StorefrontComponentFeatureModels_DoNotExposeAdminOwnedFields` blocks admin-owned/server-owned fields from component-facing models.
- `StorefrontEndpointDependencyBoundaryTests.StorefrontLocalEndpointMappings_DoNotInjectConcreteStorefrontApiClient` keeps endpoint mappings behind capability interfaces.
- `CommerceNodeStorefrontOpenApiContractTests.StorefrontSwagger_GeneratesAndCompilesTypeScriptClientWithNswag` proves Storefront OpenAPI remains generator-safe enough for non-.NET clients.
- `StorefrontGeneratedClientFoundationTests` proves the generated C# Storefront client compiles, uses generated-source guardrails, and has no backend/core project references.
- `scripts/qa/run-storefront-client-regeneration-gate.ps1` regenerates `BlazorShop.Storefront.Client` from the canonical contract and fails on generated-source drift before package release.
- `StorefrontVisualOnlyBoundaryTests`, `StorefrontPresentationFoundationBoundaryTests`, `StorefrontPresentationCutoverGuardrailTests`, `StorefrontStarterFoundationBoundaryTests`, and `StorefrontApplicationBootstrapTests` protect the final visual-only boundary and host-independent Presentation graph.
- StorefrontBuilder focused tests and `scripts/qa/run-storefront-builder-generated-proof.ps1` prove generated storefronts keep package references, package compatibility metadata, generated artifacts, route uniqueness, browser/network guardrails, and forbidden dependency scans intact.
