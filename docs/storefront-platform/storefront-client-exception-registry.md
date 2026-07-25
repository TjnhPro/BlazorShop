# Storefront Client Exception Registry

## Starter

| Capability | Exception | Reason | Owner | Test | Revisit trigger |
| --- | --- | --- | --- | --- | --- |
| none | none | Starter currently has no manual transport exceptions. | Storefront Platform | `StorefrontStarterFoundationBoundaryTests.StarterClientPolicy_HasExceptionRegistryAndNoSilentManualContracts` | First Starter manual `HttpClient` or duplicate DTO proposal. |

## Storefront V2

| Capability | Exception | Reason | Owner | Test | Revisit trigger |
| --- | --- | --- | --- | --- | --- |
| Cart merge | `StorefrontApiClient.MergeCurrentCustomerCartAsync` | Auth-sensitive merge uses the current customer bearer token while Runtime generated cart calls remain bearer-neutral. | Storefront V2 host/BFF | `StorefrontCommerceFlowCutoverTests.CartClientRegistration_UsesGeneratedRuntimeAdapterAndDocumentsMergeException` | Generated client/runtime supports per-call bearer token injection without moving cookie/session policy out of V2. |
| Saved-address checkout | `StorefrontApiClient.UpdateCheckoutAddressesAsync` with bearer token | Saved customer addresses require the current V2 customer bearer token; guest checkout already uses Runtime checkout facade. | Storefront V2 host/BFF | `StorefrontCommerceFlowCutoverTests.CheckoutClientRegistration_UsesGeneratedRuntimeAdapterAndDocumentsSavedAddressException` | Generated checkout client supports per-call bearer token injection for protected saved-address checkout. |
| Protected customer account | `IStorefrontCustomerClient` through `StorefrontApiClient` | Customer profile, address book, orders, and receipts are protected account operations that require V2-owned session/cookie bearer handling. | Storefront V2 account BFF | `StorefrontContractOwnershipTests.StorefrontV2_ManualClientExceptionsRemainDocumented` | Generated protected customer clients can attach current V2 bearer token per call without exposing tokens to browser or Runtime. |
| Auth forms/session | `StorefrontAuthClient` | Sign-in/register/logout/password recovery/session refresh must copy HttpOnly Set-Cookie headers and stay host-owned. | Storefront V2 auth/session host | `StorefrontCommerceFlowCutoverTests.AuthAndCustomerClientsRemainHostOwnedForBearerAndCookieHandling` | Auth cookie/session strategy moves to an explicit host primitive or generated client supports the required Set-Cookie bridge. |

Allowed exception candidates are documented in `storefront-client-adoption-policy.md`, but they are not active exceptions until added to this registry with a test and revisit trigger.
