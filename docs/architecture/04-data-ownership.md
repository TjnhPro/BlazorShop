# Data Ownership

BlazorShop currently has two active EF context families. Pick the context by product boundary, not by convenience.

## Context Summary

| Context | Connection | Dev Port | Owner | Status |
| --- | --- | --- | --- | --- |
| `ControlPlaneDbContext` | `ControlPlaneConnection` | `5433` | Control Plane | Active V2 platform database. |
| `CommerceNodeDbContext` | `CommerceNodeConnection` | `5434` | Commerce Node | Active V2 ecommerce node database. |

## ControlPlaneDbContext

Location:

```text
BlazorShop.Infrastructure/Data/ControlPlane/ControlPlaneDbContext.cs
```

Connection:

```text
ConnectionStrings:ControlPlaneConnection
```

Local development default:

```text
Host=localhost;Port=5433;Database=blazorshop_controlplane
```

Owns:

- Control Plane identity.
- Control Plane refresh tokens.
- Control Plane admin user profile.
- Platform roles and permissions.
- Commerce node registry.
- Node endpoints.
- Node credentials.
- Node health snapshots.
- Node capability snapshots.
- Store registry.
- Store domain registry.
- Control actions and attempts.
- Control audit logs.

Use this context when the feature is about platform ownership, node/store registry, Control Plane users, Control Plane permissions, Control Plane audit, node credentials, or gateway orchestration metadata.

Do not store node-local ecommerce catalog/order/customer data here.

## CommerceNodeDbContext

Location:

```text
BlazorShop.Infrastructure/Data/CommerceNode/CommerceNodeDbContext.cs
```

Connection:

```text
ConnectionStrings:CommerceNodeConnection
```

Local development default:

```text
Host=localhost;Port=5434;Database=blazorshop_commerce_node
```

Owns:

- Commerce/customer identity.
- Commerce refresh tokens.
- Categories.
- Products.
- Product variants.
- Product media.
- Inventory fields on products/variants.
- Orders and order lines.
- Cart/checkout order items.
- Payment methods.
- Newsletter subscribers.
- SEO settings and redirects.
- Admin settings and audit.
- Commerce stores.
- Commerce store domains.
- Storefront deployment images.
- Store deployments.
- Commerce tasks and task steps.

Use this context when the feature is node-local ecommerce behavior, store runtime, catalog, order, storefront auth, task orchestration, or deployment state.

Current ProductMedia ownership:

- `product_media` rows live only in `CommerceNodeDbContext`.
- Media import work is queued in `commerce_task` with task type `product.media.import`.
- The MVP handler runs under the existing `CommerceTaskWorker`; a separate media worker is a future extraction, not the current architecture.
- `Product.Image` remains the compatibility field for Storefront V2 and points to the primary `/media/products/{mediaPublicId}` URL after import succeeds.

Do not store Control Plane platform users, node credentials, or platform permissions here.

## Identity Separation

Control Plane and Commerce Node may share identity entity classes such as `AppUser`, but they are separate databases and separate auth boundaries.

This is intentional:

- Control Plane users are platform/admin users.
- Commerce Node users are storefront/customer or node-local ecommerce users.
- Permissions and roles may look similar, but they do not mean the same thing across boundaries.

Do not merge the contexts just to avoid a second identity setup. A feature should cross boundaries through APIs, not by sharing DbContext state.

## Startup Migration Ownership

V2 database migration follows the startup migration decision captured in `docs/refactor-control-Commerce-storefront/BlazorShop.V2.StartupDatabaseMigration.todo.md`.

- `BlazorShop.ControlPlane.API` may run EF Core migrations for `ControlPlaneDbContext` when `ControlPlane:Database:MigrateOnStartup=true`.
- `BlazorShop.CommerceNode.API` may run EF Core migrations for `CommerceNodeDbContext` when `CommerceNode:Database:MigrateOnStartup=true`.
- A runtime must fail startup if its own database migration fails.
- Production deploys must backup the database first and avoid starting multiple API instances against the same database while migration is running.
