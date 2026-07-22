# System Map

## Solution Projects

`BlazorShop.sln` is the V2 canonical solution. It contains shared core projects, active V2 projects, ServiceDefaults, and `BlazorShop.Tests.V2`. Legacy Presentation/AppHost source may remain on disk temporarily for comparison during removal work, but it is no longer part of the main solution.

| Area | Project | Status | Responsibility |
| --- | --- | --- | --- |
| Core | `BlazorShop.Domain` | Active shared core | Entities and domain contracts used by legacy and V2. |
| Core | `BlazorShop.Application` | Active shared core | DTOs, validation, application services, service contracts, Control Plane interfaces, Commerce Node interfaces. |
| Core | `BlazorShop.Infrastructure` | Active shared infrastructure | EF contexts, repositories, migrations, infrastructure services, auth adapters, seeders, transaction managers. |
| Runtime | `BlazorShop.ServiceDefaults` | Active shared runtime | Common .NET hosting/service defaults. |
| Runtime | `BlazorShop.AppHost` | Legacy-oriented, not in main solution | Aspire host currently references legacy API/Web/Storefront. Do not assume it represents V2 runtime. |
| Legacy | `BlazorShop.Presentation/BlazorShop.API` | Legacy, not in main solution | Original commerce API with mixed admin and storefront concerns. |
| Legacy | `BlazorShop.Presentation/BlazorShop.Web` | Legacy, not in main solution | Original admin/account/customer Blazor Web UI. |
| Legacy | `BlazorShop.Presentation/BlazorShop.Storefront` | Legacy, not in main solution | Original public storefront. |
| Legacy | `BlazorShop.Presentation/BlazorShop.Web.Shared` | Legacy, not in main solution | Original Web shared helpers. |
| V2 | `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API` | Active | Platform API for auth, users, permissions, nodes, stores, credentials, health, actions, audit, Commerce Node gateway calls, and startup migration for `ControlPlaneDbContext`. |
| V2 | `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web` | Active | Blazor WASM Control Plane UI. Calls only Control Plane API. |
| V2 | `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API` | Active | Node-local ecommerce API, admin/control endpoints, scoped Storefront endpoints, task orchestration, deployment support, and startup migration for `CommerceNodeDbContext`. |
| V2 | `BlazorShop.PresentationV2/BlazorShop.Storefront.V2` | Active | Server-side storefront using Commerce Node Storefront APIs and store key route scope. |
| V2 | `BlazorShop.PresentationV2/BlazorShop.Storefront.Components` | Active | Reusable Razor components used by Storefront V2 interactive render modes. |
| V2 | `BlazorShop.PresentationV2/BlazorShop.Storefront.WASM` | Active | Storefront V2 WebAssembly assembly for interactive browser components. |
| V2 | `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2` | Active | Shared V2 browser storage, cookie storage, auth session, toast, and API helper utilities. |
| Tests | `BlazorShop.Tests` | Active but mixed | Test project currently references legacy and selected V2 projects. Treat test ownership by feature area. |

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
- `BlazorShop.Storefront.V2` references `Application`, `ServiceDefaults`, `Storefront.Components`, `Storefront.WASM`, and `Web.SharedV2`.
- `BlazorShop.Storefront.WASM` references `Storefront.Components`.
- `BlazorShop.Storefront.Components` is a Razor component library with no BlazorShop project references.
- `BlazorShop.Web.SharedV2` has no project references.

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
