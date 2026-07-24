# V2 Contract Ownership

This page records the current contract ownership boundary while Storefront V2 moves away from `Web.SharedV2` as a business DTO bucket.

## Rules

- Public HTTP contracts live at the API boundary that exposes them.
- Commerce Node Storefront public HTTP contracts are canonical at the `api/storefront/stores/{storeKey}/*` boundary.
- Generated Storefront clients are frontend-readable contracts and should be regenerated from Commerce Node Storefront OpenAPI instead of hand-copied into frontend packages.
- Storefront frontend view models are allowed when they are presentation or composition models.
- Storefront frontend code must not add handwritten duplicate API DTO clones when the schema should come from OpenAPI-generated contracts.
- Storefront browser/local endpoint contracts live in `BlazorShop.Storefront.V2/Services/Contracts`.
- Storefront portable feature component models live with the component feature under `BlazorShop.Storefront.Components/Features/*` when they are presentation-only.
- `Web.SharedV2` may keep browser helpers and transitional model folders during migration, but new business model folders are not allowed.
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

`BlazorShop.Storefront.V2/Services/Contracts` already owns Storefront-local contracts for:

- Address lookup.
- Cart and product-selection preview.
- Checkout browser state and checkout commands.
- Consent.
- Current store/configuration/currency.
- Customer account, addresses, and orders.
- Payment attempts/methods.
- SEO, sitemap, pages, catalog, and rendering helpers.

`BlazorShop.PresentationV2/BlazorShop.Storefront.Client` is the generated Storefront HTTP client package. It is generated from the Commerce Node Storefront OpenAPI snapshot and must not reference backend/core/API projects or `Storefront.V2`. Storefront V2 migration should consume this generated client instead of adding handwritten API DTO clones.

## Portable Component Models

`BlazorShop.Storefront.Components/Features/*` may define small render-facing models such as product summary cards, product gallery items, and purchase panel snapshots.

These models are not public HTTP contracts. The Storefront V2 host maps API DTOs or local endpoint contracts into them before composition. They must not reference `Web.SharedV2`, `Application`, `Domain`, `Infrastructure`, Control Plane, Commerce Node runtime projects, Storefront API clients, or Storefront route helpers.

Do not add admin-owned fields, store ownership fields, credentials, tokens, passwords, server-owned publication flags, or cost/internal accounting fields to these component models.

## Guardrails

- `V2ArchitectureBoundaryBaselineTests.WebSharedV2BusinessModelFolders_AreFrozenDuringContractMigration` freezes the allowed `Web.SharedV2/Models` folders.
- `StorefrontPageCompositionGuardrailTests.StorefrontComponentFeatures_DoNotDependOnBackendOrRouteContracts` keeps portable feature components presentation-only.
- `StorefrontPageCompositionGuardrailTests.StorefrontComponentFeatureModels_DoNotExposeAdminOwnedFields` blocks admin-owned/server-owned fields from component-facing models.
- `StorefrontEndpointDependencyBoundaryTests.StorefrontLocalEndpointMappings_DoNotInjectConcreteStorefrontApiClient` keeps endpoint mappings behind capability interfaces.
- `CommerceNodeStorefrontOpenApiContractTests.StorefrontSwagger_GeneratesAndCompilesTypeScriptClientWithNswag` proves Storefront OpenAPI remains generator-safe enough for non-.NET clients.
- `StorefrontGeneratedClientFoundationTests` proves the generated C# Storefront client compiles, uses generated-source guardrails, and has no backend/core project references.
