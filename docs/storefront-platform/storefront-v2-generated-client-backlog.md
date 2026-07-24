# Storefront V2 Generated Client Migration Backlog

Storefront V2 is already compile-time decoupled from backend/core projects. The remaining handwritten transport is Track B and does not block Starter unless it reveals a Storefront API contract gap.

| Capability | Current state | Required before Sample QA | Notes |
| --- | --- | --- | --- |
| address | manual `StorefrontApiClient` path | no | Generated address clients exist; migrate when account/checkout address flows are next edited. |
| cart | manual `StorefrontApiClient` path | no | Starter BFF tracer already uses generated cart client; V2 migration remains behavior-sensitive because cart-token handling is host-owned. |
| checkout | manual `StorefrontApiClient` path | no | Keep checkout state/order placement backend-authoritative; migrate only with focused COD/browser QA. |
| consent | manual `StorefrontApiClient` path | no | Consent visitor cookie remains server-owned. |
| customer/account | manual auth/account transports | no | Auth remains the most likely exception because refresh-cookie behavior needs header/cookie handling. |
| payment | manual payment/result path | no | Provider callbacks stay excluded from frontend client; result reads may migrate separately. |
