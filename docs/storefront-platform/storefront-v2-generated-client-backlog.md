# Storefront V2 Generated Client Migration Backlog

Storefront V2 is compile-time decoupled from backend/core projects and no longer contains handwritten Commerce Node Storefront API transport. Any remaining generated-client work belongs in `BlazorShop.Storefront.Presentation`, `BlazorShop.Storefront.Runtime`, or `BlazorShop.Storefront.Client`; do not add a V2 manual client replacement.

| Capability | Current state | Required before Sample QA | Notes |
| --- | --- | --- | --- |
| address | retired V2 manual path | no | Address lookup uses Runtime/Presentation generated path; account address book uses same-origin Presentation BFF endpoints. |
| cart | retired V2 manual path | no | Browser mutations stay same-origin `/api/cart/*`; Runtime cart facade owns Commerce Node calls and cart-token behavior. |
| checkout | retired V2 manual path | no | Checkout state/order placement stays backend-authoritative through Runtime checkout/payment facades and Presentation BFF endpoints. |
| consent | retired V2 manual path | no | Consent visitor cookie remains server-owned through Presentation consent endpoints and generated consent adapter. |
| customer/account | retired V2 manual path | no | Auth/session/customer defaults live in Presentation; generated customer/order adapters create authorized generated clients per bearer token. |
| payment | retired V2 manual path | no | Payment methods and attempts use Runtime/Presentation generated path; provider callbacks stay excluded from frontend clients. |
