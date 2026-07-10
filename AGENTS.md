# BlazorShop Agent Guide

## Agent skills

### Issue tracker

Issues are tracked in GitHub Issues for `TjnhPro/BlazorShop`; external PRs are not treated as a triage surface. See `docs/agents/issue-tracker.md`.

### Triage labels

Use the default triage labels: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Use a single-context domain documentation layout for the BlazorShop ecommerce product. See `docs/agents/domain.md`.

## Architecture guardrails

### Control Plane gateway boundary

`BlazorShop.ControlPlane.Web` must only call `BlazorShop.ControlPlane.API`.

Do not make `BlazorShop.ControlPlane.Web` call `BlazorShop.CommerceNode.API` directly, and do not put Commerce Node node keys, node secrets, IP allowlist assumptions, or Commerce Node base URLs into the Web client. The Web client is a UI surface only.

`BlazorShop.ControlPlane.API` is the gateway responsible for distributing and proxying requests to `BlazorShop.CommerceNode.API` according to the security model: node key, node secret, allowed Control Plane IP, store key scope, audit, and permission checks.

When a feature says "Control Plane calls Commerce Node", interpret that as:

```text
BlazorShop.ControlPlane.Web
  -> BlazorShop.ControlPlane.API
      -> BlazorShop.CommerceNode.API
```

Never interpret it as:

```text
BlazorShop.ControlPlane.Web
  -> BlazorShop.CommerceNode.API
```
