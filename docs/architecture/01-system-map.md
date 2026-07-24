# System Map

## Solution Projects

`BlazorShop.sln` is the V2 canonical solution. It contains shared core projects, active V2 projects, ServiceDefaults, and `BlazorShop.Tests.V2`. Legacy Presentation and AppHost projects have been removed from the active branch; use `scripts/run-v2-local.ps1` for local V2 orchestration.

| Area | Project | Status | Responsibility |
| --- | --- | --- | --- |
| Core | `BlazorShop.Domain` | Active shared core | Entities and domain contracts used by legacy and V2. |
| Core | `BlazorShop.Application` | Active shared core | DTOs, validation, application services, service contracts, Control Plane interfaces, Commerce Node interfaces. |
| Core | `BlazorShop.Infrastructure` | Active shared infrastructure | EF contexts, repositories, migrations, infrastructure services, auth adapters, seeders, transaction managers. |
| Runtime | `BlazorShop.ServiceDefaults` | Active shared runtime | Common .NET hosting/service defaults. |
| V2 | `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API` | Active | Platform API for auth, users, permissions, nodes, stores, credentials, health, actions, audit, Commerce Node gateway calls, and startup migration for `ControlPlaneDbContext`. |
| V2 | `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web` | Active | Blazor WASM Control Plane UI. Calls only Control Plane API. |
| V2 | `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API` | Active | Node-local ecommerce API, admin/control endpoints, scoped Storefront endpoints, task orchestration, deployment support, and startup migration for `CommerceNodeDbContext`. |
| V2 | `BlazorShop.PresentationV2/BlazorShop.Storefront.V2` | Active | Server-side storefront using Commerce Node Storefront APIs and store key route scope. |
| V2 | `BlazorShop.PresentationV2/BlazorShop.Storefront.Components` | Active | Reusable Razor components used by Storefront V2 interactive render modes. |
| V2 | `BlazorShop.PresentationV2/BlazorShop.Storefront.WASM` | Active | Storefront V2 WebAssembly assembly for interactive browser components. |
| V2 | `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2` | Active | Shared V2 browser storage, cookie storage, auth session, toast, and API helper utilities. |
| Storefront Platform | `BlazorShop.PresentationV2/BlazorShop.Storefront.Client` | Active | Generated Storefront API transport and contracts from Commerce Node Storefront OpenAPI. No backend/core/API project references. |
| Future Storefront Platform | `BlazorShop.Storefront.Runtime` | Optional | Neutral Storefront runtime primitives only when proven by V2 decoupling. No backend/core/API project references. |
| Future Storefront Implementation | `BlazorShop.Storefront.Starter` | Deferred | Future neutral skeleton. Not part of the Headless Storefront Platform Foundation and not copied from Storefront V2. |
| Tests | `BlazorShop.Tests.V2` | Active | V2 architecture, API contract, service, and smoke tests. |

## Project References

Core dependency direction:

```text
Domain
  <- Application
      <- Infrastructure
```

Active V2 presentation projects reference shared core projects:

- `BlazorShop.ControlPlane.API` references `Application`, `Infrastructure`, and `ServiceDefaults`.
- `BlazorShop.CommerceNode.API` references `Application`, `Infrastructure`, and `ServiceDefaults`.
- `BlazorShop.ControlPlane.Web` references `Application` and `Web.SharedV2`.
- `BlazorShop.Storefront.V2` references `ServiceDefaults`, `Storefront.Client`, `Storefront.Components`, `Storefront.WASM`, and `Web.SharedV2`; it must not reference `Application`, `Domain`, `Infrastructure`, Commerce Node API, or Control Plane API projects.
- `BlazorShop.Storefront.WASM` references `Storefront.Components`.
- `BlazorShop.Storefront.Components` is a Razor component library with no BlazorShop project references.
- `BlazorShop.Web.SharedV2` has no project references.

Headless Storefront target flow:

```text
Public SSR:
  BlazorShop.Storefront.V2
    -> generated Storefront client
        -> BlazorShop.CommerceNode.API api/storefront/stores/{storeKey}/*

Protected browser/WASM:
  BlazorShop.Storefront.WASM / browser components
    -> same-origin BlazorShop.Storefront.V2 /api/*
        -> generated Storefront client
            -> BlazorShop.CommerceNode.API api/storefront/stores/{storeKey}/*
```

Target dependency rules:

- `BlazorShop.CommerceNode.API` is the headless ecommerce backend and Storefront API platform.
- `BlazorShop.Storefront.V2` is the first real storefront consumer, not the neutral Starter.
- `BlazorShop.Storefront.V2` may use `Web.SharedV2` only for genuinely shared browser utilities, not `Web.SharedV2.Models` business contracts.
- Future generated storefronts consume Storefront OpenAPI/client contracts instead of copying Storefront V2 internals.

Startup migration ownership:

- `BlazorShop.ControlPlane.API` applies pending EF Core migrations for `ControlPlaneDbContext` when `ControlPlane:Database:MigrateOnStartup=true`.
- `BlazorShop.CommerceNode.API` applies pending EF Core migrations for `CommerceNodeDbContext` when `CommerceNode:Database:MigrateOnStartup=true`.
- V2 runtimes do not use a separate migrator image or CommerceNode Agent for MVP database migration.

Legacy presentation projects reference legacy shared/core projects and should not be extended for V2 work:

- `BlazorShop.API` references `Infrastructure` and `ServiceDefaults`.
- `BlazorShop.Storefront` references `Application`, `ServiceDefaults`, and `Web.Shared`.
- `BlazorShop.Web` references `Web.Shared`.

## Reference Source

`Smartstore/` is not part of BlazorShop runtime. Use it to study ecommerce concepts, property sets, workflows, and module boundaries. Do not copy Smartstore code into BlazorShop and do not add project references to Smartstore projects.

When using Smartstore for a new capability:

1. Inspect the relevant Smartstore domain area.
2. Extract business concepts, properties, validation rules, and workflows.
3. Compare with existing BlazorShop entities/services.
4. Propose a small staged plan.
5. Implement only selected MVP scope.
