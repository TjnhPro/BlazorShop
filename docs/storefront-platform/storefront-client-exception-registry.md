# Storefront Client Exception Registry

## Starter

| Capability | Exception | Reason | Owner | Test | Revisit trigger |
| --- | --- | --- | --- | --- | --- |
| none | none | Starter currently has no manual transport exceptions. | Storefront Platform | `StorefrontStarterFoundationBoundaryTests.StarterClientPolicy_HasExceptionRegistryAndNoSilentManualContracts` | First Starter manual `HttpClient` or duplicate DTO proposal. |

## Storefront V2

| Capability | Exception | Reason | Owner | Test | Revisit trigger |
| --- | --- | --- | --- | --- | --- |
| none | none | SPF17 moved Presentation contract adapters and default auth/session/customer support to `BlazorShop.Storefront.Presentation`; V2 no longer binds `StorefrontApiClient` to Presentation contracts. | Storefront Platform | `StorefrontPresentationCutoverGuardrailTests.StorefrontPresentation_DIGraph_IsHostIndependent` | A V2-specific override for a Presentation contract is proposed. |

## Retired In SPF17

| Capability | Former exception | Replacement |
| --- | --- | --- |
| Cart merge | `StorefrontApiClient.MergeCurrentCustomerCartAsync` | `GeneratedStorefrontCartClient` delegates to `IStorefrontRuntimeCartFacade.MergeCurrentCustomerAsync` with per-call bearer token support. |
| Saved-address checkout | `StorefrontApiClient.UpdateCheckoutAddressesAsync` with bearer token | `GeneratedStorefrontCheckoutClient` delegates to `IStorefrontRuntimeCheckoutFacade.UpdateAddressesAsync` with per-call bearer token support. |
| Protected customer account | `IStorefrontCustomerClient` through `StorefrontApiClient` | `GeneratedStorefrontCustomerClient` creates generated customer/order clients with the current bearer token. |
| Auth forms/session | V2-owned `StorefrontAuthClient` and `StorefrontSessionResolver` | Presentation-owned default implementations route through the store-scoped Runtime route context and preserve the Set-Cookie bridge. |

Allowed exception candidates are documented in `storefront-client-adoption-policy.md`, but they are not active exceptions until added to this registry with a test and revisit trigger.
