# Storefront Feature Components

`Features` is a temporary compatibility area for legacy shared Storefront visual components while they are migrated to headless contracts and host-owned visual templates.

- Page files compose features; they should keep route parameters, status handling, SEO, auth redirects, and initial snapshots.
- Feature components accept explicit parameters and should not assume one route file owns them.
- Browser-only behavior belongs behind `Storefront.Components/Browser` abstractions.
- New shared Storefront work should prefer `Contracts/{Capability}` for presentation contracts and `Headless/{Capability}` for behavior/state.
- Store-owned visual templates belong in `BlazorShop.Storefront.V2`, `BlazorShop.Storefront.Starter`, or a generated/custom `BlazorShop.Storefront.{Name}` project.
- Shared visual components are allowed here only as temporary compatibility wrappers during this migration.
- Do not add EF, Application, Domain, Control Plane, Commerce Node API, node credential, admin client dependencies, Storefront V2 route helpers, or host endpoint paths here.

Do not add new reusable visual implementations under `Features/{Capability}` unless a migration phase explicitly records why the component cannot yet be headless.
