namespace BlazorShop.CommerceNode.API.Swagger
{
    using System.Reflection;
    using System.Text.Json.Nodes;

    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Discovery;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.CommerceNode.API.Contracts.Storefront;
    using BlazorShop.CommerceNode.API.Middleware;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.OpenApi;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public static partial class CommerceNodeSwaggerExtensions
    {
        private sealed class StorefrontOperationMetadataFilter : IOperationFilter
        {
            private static readonly IReadOnlyDictionary<(string Controller, string Action), StorefrontOperationMetadata> Metadata =
                new Dictionary<(string Controller, string Action), StorefrontOperationMetadata>
                {
                    [("StorefrontScopedAddress", "GetCountries")] = new(
                        "StorefrontAddress_ListCountries",
                        "List Storefront address countries.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontAddressCountryResponse>>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedAddress", "GetStates")] = new(
                        "StorefrontAddress_ListStates",
                        "List Storefront address states for a country.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontAddressStateProvinceResponse>>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedAddress", "GetConfiguration")] = new(
                        "StorefrontAddress_GetConfiguration",
                        "Get Storefront address field configuration.",
                        typeof(CommerceNodeApiResponse<StorefrontAddressFieldConfigurationResponse>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCustomerAddresses", "List")] = new(
                        "StorefrontCustomerAddresses_List",
                        "List current customer addresses.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontCustomerAddressResponse>>),
                        [StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedCustomerAddresses", "Create")] = new(
                        "StorefrontCustomerAddresses_Create",
                        "Create a current customer address.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerAddressResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedCustomerAddresses", "Update")] = new(
                        "StorefrontCustomerAddresses_Update",
                        "Update a current customer address.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerAddressResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedCustomerAddresses", "Delete")] = new(
                        "StorefrontCustomerAddresses_Delete",
                        "Delete a current customer address.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedCustomerAddresses", "SetDefaultShipping")] = new(
                        "StorefrontCustomerAddresses_SetDefaultShipping",
                        "Set current customer default shipping address.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerAddressResponse>),
                        [StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedCustomerAddresses", "SetDefaultBilling")] = new(
                        "StorefrontCustomerAddresses_SetDefaultBilling",
                        "Set current customer default billing address.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerAddressResponse>),
                        [StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedCustomerProfile", "GetProfile")] = new(
                        "StorefrontCustomerProfile_Get",
                        "Get current customer profile.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerProfileResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedCustomerProfile", "UpdateProfile")] = new(
                        "StorefrontCustomerProfile_Update",
                        "Update current customer profile.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerProfileResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedAuth", "Register")] = new(
                        "StorefrontAuth_Register",
                        "Register a Storefront customer.",
                        typeof(CommerceNodeApiResponse<StorefrontRegistrationResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedAuth", "GetRegistrationPolicy")] = new(
                        "StorefrontAuth_GetRegistrationPolicy",
                        "Get Storefront registration policy.",
                        typeof(CommerceNodeApiResponse<StorefrontRegistrationPolicyResponse>),
                        [StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedAuth", "Login")] = new(
                        "StorefrontAuth_Login",
                        "Sign in a Storefront customer.",
                        typeof(CommerceNodeApiResponse<StorefrontTokenResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedAuth", "RefreshToken")] = new(
                        "StorefrontAuth_RefreshToken",
                        "Refresh a Storefront access token.",
                        typeof(CommerceNodeApiResponse<StorefrontTokenResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.RefreshCookie),
                    [("StorefrontScopedAuth", "Logout")] = new(
                        "StorefrontAuth_Logout",
                        "Sign out a Storefront customer.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.RefreshCookie),
                    [("StorefrontScopedAuth", "ChangePassword")] = new(
                        "StorefrontAuth_ChangePassword",
                        "Change the current customer's password.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedAuth", "ForgotPassword")] = new(
                        "StorefrontAuth_ForgotPassword",
                        "Request a Storefront password reset.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedAuth", "ResetPassword")] = new(
                        "StorefrontAuth_ResetPassword",
                        "Reset a Storefront customer password.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedAuth", "ConfirmEmail")] = new(
                        "StorefrontAuth_ConfirmEmail",
                        "Confirm a Storefront customer email.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedAuth", "UpdateProfile")] = new(
                        "StorefrontAuth_UpdateProfile",
                        "Update the current customer profile.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),

                    [("StorefrontScopedCatalog", "GetCategories")] = new(
                        "StorefrontCatalog_ListCategories",
                        "List published Storefront categories.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontCategoryResponse>>),
                        [StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetCategoryTree")] = new(
                        "StorefrontCatalog_GetCategoryTree",
                        "Get the published category tree.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontCategoryTreeNodeResponse>>),
                        [StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetCategoryById")] = new(
                        "StorefrontCatalog_GetCategoryById",
                        "Get a published category by ID.",
                        typeof(CommerceNodeApiResponse<StorefrontCategoryResponse>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetCategoryBySlug")] = new(
                        "StorefrontCatalog_GetCategoryBySlug",
                        "Get a published category page by slug.",
                        typeof(CommerceNodeApiResponse<StorefrontCategoryPageResponse>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetProductsByCategory")] = new(
                        "StorefrontCatalog_ListProductsByCategory",
                        "List published products in a category.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontCatalogProductResponse>>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetProductFilterMetadata")] = new(
                        "StorefrontCatalog_GetProductFilterMetadata",
                        "Get Storefront product filter metadata.",
                        typeof(CommerceNodeApiResponse<StorefrontProductFilterMetadataResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetSearchSuggestions")] = new(
                        "StorefrontCatalog_GetSearchSuggestions",
                        "Get Storefront catalog search suggestions.",
                        typeof(CommerceNodeApiResponse<StorefrontSearchSuggestionResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetProducts")] = new(
                        "StorefrontCatalog_QueryProducts",
                        "Query published Storefront products.",
                        typeof(CommerceNodeApiResponse<StorefrontPagedResponse<StorefrontCatalogProductResponse>>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetProductById")] = new(
                        "StorefrontCatalog_GetProductById",
                        "Get a published product by ID.",
                        typeof(CommerceNodeApiResponse<StorefrontProductResponse>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetProductBySlug")] = new(
                        "StorefrontCatalog_GetProductBySlug",
                        "Get a published product by slug.",
                        typeof(CommerceNodeApiResponse<StorefrontProductResponse>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "PreviewProductSelection")] = new(
                        "StorefrontCatalog_PreviewProductSelection",
                        "Preview a Storefront product selection.",
                        typeof(CommerceNodeApiResponse<StorefrontProductSelectionPreviewResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetSitemap")] = new(
                        "StorefrontCatalog_GetSitemap",
                        "Get the published catalog sitemap.",
                        typeof(CommerceNodeApiResponse<GetPublicCatalogSitemap>),
                        [StatusCodes.Status500InternalServerError]),

                    [("StorefrontScopedCart", "CreateSession")] = new(
                        "StorefrontCart_CreateSession",
                        "Create or resume a server cart session.",
                        typeof(CommerceNodeApiResponse<StorefrontCartSessionResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCart", "Get")] = new(
                        "StorefrontCart_Get",
                        "Get the current server cart.",
                        typeof(CommerceNodeApiResponse<StorefrontCartResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCart", "AddLine")] = new(
                        "StorefrontCart_AddLine",
                        "Add a line to the server cart.",
                        typeof(CommerceNodeApiResponse<StorefrontCartResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCart", "UpdateLine")] = new(
                        "StorefrontCart_UpdateLine",
                        "Update a server cart line.",
                        typeof(CommerceNodeApiResponse<StorefrontCartResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCart", "RemoveLine")] = new(
                        "StorefrontCart_RemoveLine",
                        "Remove a server cart line.",
                        typeof(CommerceNodeApiResponse<StorefrontCartResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCart", "Clear")] = new(
                        "StorefrontCart_Clear",
                        "Clear the current server cart.",
                        typeof(CommerceNodeApiResponse<StorefrontCartResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCart", "Validate")] = new(
                        "StorefrontCart_Validate",
                        "Validate the server cart without changing it.",
                        typeof(CommerceNodeApiResponse<StorefrontCartValidationResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCart", "Recalculate")] = new(
                        "StorefrontCart_Recalculate",
                        "Recalculate server cart snapshots.",
                        typeof(CommerceNodeApiResponse<StorefrontCartResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCart", "MergeCurrentCustomer")] = new(
                        "StorefrontCart_MergeCurrentCustomer",
                        "Merge the current guest cart into the authenticated customer cart.",
                        typeof(CommerceNodeApiResponse<StorefrontCartResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedCurrency", "SetPreference")] = new(
                        "StorefrontCurrency_SetPreference",
                        "Set a Storefront currency preference.",
                        typeof(CommerceNodeApiResponse<StorefrontCurrencyPreferenceResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "Start")] = new(
                        "StorefrontCheckout_Start",
                        "Start or resume a Storefront checkout session.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutSessionResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "Load")] = new(
                        "StorefrontCheckout_Load",
                        "Load a Storefront checkout session.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutSessionResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "Cancel")] = new(
                        "StorefrontCheckout_Cancel",
                        "Cancel a Storefront checkout session.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutSessionResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "UpdateAddresses")] = new(
                        "StorefrontCheckout_UpdateAddresses",
                        "Update checkout billing and shipping addresses.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutSessionResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "SelectShippingMethod")] = new(
                        "StorefrontCheckout_SelectShippingMethod",
                        "Select a checkout shipping method.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutSessionResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "SelectPaymentMethod")] = new(
                        "StorefrontCheckout_SelectPaymentMethod",
                        "Select a checkout payment method.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutSessionResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "Review")] = new(
                        "StorefrontCheckout_Review",
                        "Review a checkout before placing an order.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutReviewResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "Preview")] = new(
                        "StorefrontCheckout_Preview",
                        "Preview and validate a Storefront checkout session.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutPreviewResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "PlaceOrder")] = new(
                        "StorefrontCheckout_PlaceOrder",
                        "Place a COD order from a checkout session.",
                        typeof(CommerceNodeApiResponse<StorefrontPlaceOrderResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedNewsletter", "Subscribe")] = new(
                        "StorefrontNewsletter_Subscribe",
                        "Subscribe an email to the Storefront newsletter.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedContact", "Submit")] = new(
                        "StorefrontContact_Submit",
                        "Submit a Storefront contact request.",
                        typeof(CommerceNodeApiResponse<StorefrontContactResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedOrders", "GetCurrentUserOrders")] = new(
                        "StorefrontOrders_ListCurrentUserOrders",
                        "List orders for the current customer.",
                        typeof(CommerceNodeApiResponse<StorefrontPagedResponse<StorefrontCustomerOrderListItemResponse>>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedOrders", "GetCurrentUserOrder")] = new(
                        "StorefrontOrders_GetCurrentUserOrder",
                        "Get an order for the current customer.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerOrderDetailResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedOrders", "GetCurrentUserOrderReceipt")] = new(
                        "StorefrontOrders_GetCurrentUserOrderReceipt",
                        "Get a receipt projection for the current customer order.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerOrderDetailResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedOrders", "GetGuestOrder")] = new(
                        "StorefrontOrders_GetGuestOrder",
                        "Get a guest order by reference and access token.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerOrderDetailResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedPages", "GetBySlug")] = new(
                        "StorefrontPages_GetBySlug",
                        "Get a published Storefront page by slug.",
                        typeof(CommerceNodeApiResponse<StorefrontPagePublicDto>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedPages", "ListNavigation")] = new(
                        "StorefrontPages_ListNavigation",
                        "List published Storefront content navigation links.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontPageNavigationLinkDto>>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedNavigation", "GetMenu")] = new(
                        "StorefrontNavigation_GetMenu",
                        "Get a Storefront navigation menu.",
                        typeof(CommerceNodeApiResponse<StoreNavigationPublicMenuDto>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedConfiguration", "Get")] = new(
                        "StorefrontConfiguration_Get",
                        "Get public Storefront configuration.",
                        typeof(CommerceNodeApiResponse<StorefrontPublicConfigurationResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedConsent", "Current")] = new(
                        "StorefrontConsent_Current",
                        "Get the current Storefront consent state.",
                        typeof(CommerceNodeApiResponse<StorefrontConsentResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedConsent", "Save")] = new(
                        "StorefrontConsent_Save",
                        "Save Storefront consent category selections.",
                        typeof(CommerceNodeApiResponse<StorefrontConsentResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status429TooManyRequests, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedConsent", "Revoke")] = new(
                        "StorefrontConsent_Revoke",
                        "Revoke Storefront optional consent categories.",
                        typeof(CommerceNodeApiResponse<StorefrontConsentResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status429TooManyRequests, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedPayments", "GetPaymentMethods")] = new(
                        "StorefrontPayments_ListMethods",
                        "List enabled Storefront payment methods.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontPaymentMethodResponse>>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedPayments", "GetAttempt")] = new(
                        "StorefrontPayments_GetAttempt",
                        "Get a Storefront payment attempt.",
                        typeof(CommerceNodeApiResponse<StorefrontPaymentAttemptResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedPayments", "HandleProviderCallback")] = new(
                        "StorefrontPayments_HandleProviderCallback",
                        "Accept a Storefront payment provider callback.",
                        typeof(CommerceNodeApiResponse<StorefrontPaymentWebhookAcceptedResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedPayments", "HandleWebhook")] = new(
                        "StorefrontPayments_HandleWebhook",
                        "Accept a Storefront payment provider webhook.",
                        typeof(CommerceNodeApiResponse<StorefrontPaymentWebhookAcceptedResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedRecommendations", "GetRecommendations")] = new(
                        "StorefrontRecommendations_ListProductRecommendations",
                        "List Storefront product recommendations.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontProductRecommendationResponse>>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedSeo", "GetSettings")] = new(
                        "StorefrontSeo_GetSettings",
                        "Get Storefront SEO settings.",
                        typeof(CommerceNodeApiResponse<SeoSettingsDto>),
                        [StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedSeo", "ResolveRedirect")] = new(
                        "StorefrontSeo_ResolveRedirect",
                        "Resolve a Storefront SEO redirect.",
                        typeof(CommerceNodeApiResponse<SeoRedirectResolutionDto>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedStore", "Current")] = new(
                        "StorefrontStore_GetCurrent",
                        "Get the current Storefront store.",
                        typeof(CommerceNodeApiResponse<StorefrontCurrentStoreResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedStore", "Maintenance")] = new(
                        "StorefrontStore_GetMaintenance",
                        "Get the current Storefront maintenance state.",
                        typeof(CommerceNodeApiResponse<StorefrontMaintenanceResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                };

            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                var relativePath = NormalizePath(context.ApiDescription.RelativePath);
                if (!relativePath.StartsWith("api/storefront/stores/{storekey}/", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
                {
                    return;
                }

                var controllerName = actionDescriptor.ControllerName;
                var actionName = actionDescriptor.ActionName;
                if (!Metadata.TryGetValue((controllerName, actionName), out var metadata))
                {
                    return;
                }

                operation.OperationId = metadata.OperationId;
                operation.Summary = metadata.Summary;
                operation.Description = metadata.Description;
                operation.Deprecated = metadata.Deprecated;

                if (operation.RequestBody is OpenApiRequestBody requestBody)
                {
                    requestBody.Required = true;
                }

                ApplySuccessResponse(operation, context, metadata.SuccessResponseType);
                ApplyErrorResponses(operation, context, metadata.ErrorStatusCodes);
                ApplySecurity(operation, context, metadata.Security);
                ApplyParameterMetadata(operation, metadata.OperationId);
            }

            private static void ApplySuccessResponse(
                OpenApiOperation operation,
                OperationFilterContext context,
                Type responseType)
            {
                operation.Responses ??= new OpenApiResponses();
                operation.Responses["200"] = new OpenApiResponse
                {
                    Description = "OK",
                    Content = CommerceNodeSwaggerResponseHelpers.CreateJsonContent(context, responseType),
                };
            }

            private static void ApplyErrorResponses(
                OpenApiOperation operation,
                OperationFilterContext context,
                IReadOnlyList<int> statusCodes)
            {
                foreach (var statusCode in statusCodes.Distinct())
                {
                    operation.Responses ??= new OpenApiResponses();
                    operation.Responses[statusCode.ToString()] = new OpenApiResponse
                    {
                        Description = GetErrorDescription(statusCode),
                        Content = CommerceNodeSwaggerResponseHelpers.CreateJsonContent(context, typeof(CommerceNodeApiErrorResponse)),
                    };
                }
            }

            private static void ApplySecurity(
                OpenApiOperation operation,
                OperationFilterContext context,
                StorefrontSecurityRequirement explicitSecurity)
            {
                var security = explicitSecurity;
                if (security == StorefrontSecurityRequirement.None
                    && RequiresAuthorization(context.MethodInfo))
                {
                    security = StorefrontSecurityRequirement.Bearer;
                }

                if (security == StorefrontSecurityRequirement.None)
                {
                    return;
                }

                operation.Security ??= [];
                operation.Security.Clear();
                operation.Security.Add(CreateSecurityRequirement(security));
            }

            private static void ApplyParameterMetadata(OpenApiOperation operation, string operationId)
            {
                if (!string.Equals(operationId, "StorefrontCatalog_QueryProducts", StringComparison.Ordinal))
                {
                    return;
                }

                var sortByParameter = operation.Parameters?
                    .FirstOrDefault(parameter => string.Equals(parameter.Name, "sortBy", StringComparison.OrdinalIgnoreCase));
                if (sortByParameter?.Schema is not OpenApiSchema sortBySchema)
                {
                    return;
                }

                sortBySchema.Type = JsonSchemaType.String;
                sortBySchema.Enum = StorefrontProductCatalogSortValues.All
                    .Select(value => JsonValue.Create(value)!)
                    .Cast<JsonNode>()
                    .ToList();
            }

            private static bool RequiresAuthorization(MethodInfo methodInfo)
            {
                var controllerType = methodInfo.DeclaringType;
                var hasAllowAnonymous = methodInfo.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any()
                    || controllerType?.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any() == true;
                if (hasAllowAnonymous)
                {
                    return false;
                }

                return methodInfo.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any()
                    || controllerType?.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any() == true;
            }

            private static OpenApiSecurityRequirement CreateSecurityRequirement(StorefrontSecurityRequirement security)
            {
                var schemeName = security == StorefrontSecurityRequirement.RefreshCookie ? "RefreshCookie" : "Bearer";
                return new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(schemeName, null!, null)
                    {
                        Reference = new OpenApiReferenceWithDescription
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = schemeName,
                        },
                    }] = [],
                };
            }

            private static string GetErrorDescription(int statusCode)
            {
                return statusCode switch
                {
                    StatusCodes.Status400BadRequest => "Bad Request",
                    StatusCodes.Status401Unauthorized => "Unauthorized",
                    StatusCodes.Status403Forbidden => "Forbidden",
                    StatusCodes.Status404NotFound => "Not Found",
                    StatusCodes.Status409Conflict => "Conflict",
                    StatusCodes.Status500InternalServerError => "Internal Server Error",
                    _ => "Error",
                };
            }
        }
    }
}
