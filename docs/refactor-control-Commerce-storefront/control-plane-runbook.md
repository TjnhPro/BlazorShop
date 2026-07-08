# Control Plane Runbook

## Local Development Database

Use one PostgreSQL database for Control Plane:

```text
Host=localhost;Port=5433;Database=blazorshop_controlplane;Username=blazorshop_controlplane
```

Control Plane auth tables are part of `ControlPlaneDbContext`. Do not run `AppDbContext` migrations against this database.

To create development admin and standard-user accounts, set seed values outside source control before starting the API:

```powershell
$env:ControlPlane__SeedAdmin__Enabled = 'true'
$env:ControlPlane__SeedAdmin__Email = 'admin@example.local'
$env:ControlPlane__SeedAdmin__Password = '<dev-only-password>'
$env:ControlPlane__SeedAdmin__DisplayName = 'Control Plane Admin'
$env:ControlPlane__SeedAdmin__ControlPlaneRoleKey = 'platform_owner'
$env:ControlPlane__SeedUser__Enabled = 'true'
$env:ControlPlane__SeedUser__Email = 'user@example.local'
$env:ControlPlane__SeedUser__Password = '<dev-only-password>'
$env:ControlPlane__SeedUser__DisplayName = 'Control Plane User'
$env:ControlPlane__SeedUser__ControlPlaneRoleKey = 'auditor'
```

Then start `BlazorShop.ControlPlane.API`. Development startup applies `ControlPlaneDbContext` migrations and seeds the configured accounts when their seed passwords are present.

If the local database was previously contaminated by `AppDbContext` migrations, reset it only after confirming local data can be discarded:

```powershell
docker exec blazorshop-controlplane-postgres dropdb -U blazorshop_controlplane --if-exists blazorshop_controlplane
docker exec blazorshop-controlplane-postgres createdb -U blazorshop_controlplane blazorshop_controlplane
```

Expected table groups after a clean migration:

- `AspNet*`
- `RefreshTokens`
- `control_plane_*`
- `commerce_node*`
- `node_*`
- `store_*`

Forbidden table groups:

- legacy `Products`, `Categories`, `Orders`, `PaymentMethods`
- legacy `Seo*`, `AdminSettings`, `Newsletter*`

## First Node Registration

1. Start PostgreSQL and apply the Control Plane migration.
2. Start `BlazorShop.ControlPlane.API` and `BlazorShop.ControlPlane.Web`.
3. Sign in with an operator account that has `nodes.write`.
4. Open `Nodes`, create a node with a unique node key and its Control API base URL.
5. Open `Credentials`, select the node, create an API key, and install the one-time secret on the Commerce Node.
6. Open `Health`, select the node, and run a probe.
7. Confirm the node status becomes `healthy` or inspect the persisted error if it becomes `warning` or `down`.

## Credential Rotation

1. Open `Credentials` and select the target node.
2. Use `Rotate` on the active key.
3. Copy the new one-time secret immediately.
4. Install the new key on the Commerce Node.
5. Run a manual health probe.
6. Revoke the old key after the node validates successfully.
7. Check `Audit Logs` for create, rotate, reveal, and revoke records.

## Failed Probe Triage

1. Open `Health` and inspect the latest snapshot for status, HTTP code, latency, dependency JSON, and error code.
2. Check the Control Plane API logs using the response `X-Correlation-ID`.
3. If the error is `timeout`, verify network path, DNS, reverse proxy, and the Commerce Node process.
4. If the error is `http_status`, open the node Control API URL and verify `/health` returns success.
5. If the error is `malformed_payload`, compare the node `/health` response with the expected control contract.
6. If capabilities are missing or stale, confirm `/capabilities` returns JSON and run another probe.
7. Queue a Control Action only after node health and credentials are known good.

## Production Startup Checklist

1. Set `ConnectionStrings:ControlPlaneConnection`.
2. Set `Jwt:Key` to a strong production secret.
3. Set `ControlPlane:Cors:AllowedOrigins` to the Control Plane Web origin.
4. Set `ControlPlane:ForwardedHeaders:KnownProxies` when running behind a trusted reverse proxy.
5. Review `ControlPlane:RateLimiting` for the deployment topology.
6. Verify `/health` reports the Control Plane database readiness check as healthy.
