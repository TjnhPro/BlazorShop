# Storefront Feature Components

Feature components are reusable public Storefront capability blocks.

- Page files compose features; they should keep route parameters, status handling, SEO, auth redirects, and initial snapshots.
- Feature components accept explicit parameters and should not assume one route file owns them.
- Browser-only behavior belongs behind `Storefront.Components/Browser` abstractions.
- Do not add EF, Application, Domain, Control Plane, Commerce Node API, node credential, or admin client dependencies here.

Place new reusable Storefront UI under `Features/{Capability}`. Keep root-level component folders out of this project unless they are infrastructure folders such as `Browser` or `wwwroot`.
