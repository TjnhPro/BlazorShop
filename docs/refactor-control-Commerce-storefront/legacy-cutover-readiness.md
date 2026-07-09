# Legacy Cutover Readiness

## Current Decision

Legacy `BlazorShop.Presentation` remains in the solution until Commerce Node V2 and Storefront V2 have explicit parity plans. Control Plane V2 can ship independently because it owns platform management data only.

## Dependency Boundary

- `BlazorShop.ControlPlane.API` must not reference legacy presentation projects.
- V2 web projects must use `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2`, not legacy `BlazorShop.Presentation/BlazorShop.Web.Shared`.
- Legacy `BlazorShop.Web.Shared` remains available only to legacy `BlazorShop.Presentation` projects.
- Control Plane feature code belongs under explicit `ControlPlane` namespaces in Application, Infrastructure, Domain, API, Web, and tests.
- Product, cart, order, payment, category, inventory, media, SEO, and storefront clients remain outside Control Plane.

## Commerce Node V2 Start Conditions

Start Commerce Node V2 after:

1. Control Plane can register nodes and credentials.
2. Health and capability snapshots are persisted.
3. Store registry metadata can be assigned to nodes.
4. Control actions can be queued and inspected.
5. The first node control contract is documented and testable.

## Legacy Removal Conditions

Do not remove `BlazorShop.Presentation` until:

1. Storefront V2 has route, SEO, catalog, cart, checkout, and account parity decisions.
2. Commerce Node V2 owns commerce APIs without depending on legacy presentation controllers.
3. Control Plane operators can manage nodes, credentials, health, stores, actions, and audit logs.
4. Deployment runbooks and rollback plans exist for Control Plane, Commerce Node V2, and Storefront V2.
5. Architecture boundary tests pass and no V2 project references legacy presentation code.

## Verification Gate

Phase 11 adds architecture tests that fail if:

- PresentationV2 projects reference any legacy presentation project.
- Control Plane Web or Storefront V2 uses legacy `BlazorShop.Web.Shared` namespaces instead of `BlazorShop.Web.SharedV2`.
- Control Plane tests reference legacy presentation UI or storefront code.
