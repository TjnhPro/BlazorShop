namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.CommerceNode.VariationTemplates;

    public static class StorefrontContractValidation
    {
        public const int DefaultPageSize = 24;
        public const int MaxPageSize = 100;
        public const int EmailMaxLength = 254;
        public const int PasswordMinLength = 8;

        public const string SortByPattern =
            "^(newest|oldest|priceLowToHigh|priceHighToLow|nameAscending|nameDescending|displayOrder|updated)$";
    }

    public sealed class StorefrontRegisterRequest
    {
        [Required]
        [MaxLength(160)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(StorefrontContractValidation.EmailMaxLength)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(StorefrontContractValidation.PasswordMinLength)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [MaxLength(4096)]
        public string? CaptchaToken { get; set; }
    }

    public sealed class StorefrontLoginRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(StorefrontContractValidation.EmailMaxLength)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [MaxLength(4096)]
        public string? CaptchaToken { get; set; }
    }

    public sealed class StorefrontChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(StorefrontContractValidation.PasswordMinLength)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public sealed class StorefrontUpdateProfileRequest
    {
        [Required]
        [MaxLength(160)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(StorefrontContractValidation.EmailMaxLength)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [MaxLength(32)]
        public string? PhoneNumber { get; set; }
    }

    public sealed class StorefrontProductCatalogQuery
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; init; } = 1;

        [Range(1, StorefrontContractValidation.MaxPageSize)]
        public int PageSize { get; init; } = StorefrontContractValidation.DefaultPageSize;

        public Guid? CategoryId { get; init; }

        [MaxLength(256)]
        public string? CategorySlug { get; init; }

        [MaxLength(256)]
        public string? SearchTerm { get; init; }

        [Range(0, double.MaxValue)]
        public decimal? MinPrice { get; init; }

        [Range(0, double.MaxValue)]
        public decimal? MaxPrice { get; init; }

        public bool? InStock { get; init; }

        [RegularExpression(StorefrontContractValidation.SortByPattern)]
        public string SortBy { get; init; } = StorefrontProductCatalogSortValues.Newest;

        public DateTime? CreatedAfterUtc { get; init; }

        [StringLength(3, MinimumLength = 3)]
        public string? CurrencyCode { get; init; }
    }

    public static class StorefrontProductCatalogSortValues
    {
        public const string Newest = "newest";
        public const string Oldest = "oldest";
        public const string PriceLowToHigh = "priceLowToHigh";
        public const string PriceHighToLow = "priceHighToLow";
        public const string NameAscending = "nameAscending";
        public const string NameDescending = "nameDescending";
        public const string DisplayOrder = "displayOrder";
        public const string Updated = "updated";

        public static IReadOnlyList<string> All { get; } =
        [
            Newest,
            Oldest,
            PriceLowToHigh,
            PriceHighToLow,
            NameAscending,
            NameDescending,
            DisplayOrder,
            Updated,
        ];
    }

    public sealed class StorefrontCheckoutShippingAddress
    {
        [Required]
        [MaxLength(160)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(StorefrontContractValidation.EmailMaxLength)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [MaxLength(32)]
        public string? Phone { get; set; }

        [Required]
        [MaxLength(240)]
        public string Address1 { get; set; } = string.Empty;

        [MaxLength(240)]
        public string? Address2 { get; set; }

        [Required]
        [MaxLength(120)]
        public string City { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? State { get; set; }

        [Required]
        [MaxLength(32)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string CountryCode { get; set; } = string.Empty;
    }

    public sealed class StorefrontCheckoutPreviewRequest
    {
        [Range(1, int.MaxValue)]
        public int ExpectedCartVersion { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(StorefrontContractValidation.EmailMaxLength)]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(160)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string PaymentMethodKey { get; set; } = string.Empty;

        [Required]
        public StorefrontCheckoutShippingAddress ShippingAddress { get; set; } = new();
    }

    public sealed record StorefrontCheckoutPreviewResponse(
        Guid CheckoutSessionId,
        Guid CartId,
        int CartVersion,
        string State,
        bool IsValid,
        string NextAction,
        string CustomerEmail,
        string CustomerName,
        string PaymentMethodKey,
        decimal Subtotal,
        decimal ShippingTotal,
        decimal TaxTotal,
        decimal DiscountTotal,
        decimal GrandTotal,
        string CurrencyCode,
        DateTimeOffset ExpiresAtUtc,
        IReadOnlyList<StorefrontCheckoutLineSummaryResponse> Lines,
        IReadOnlyList<StorefrontCheckoutValidationIssueResponse> Issues);

    public sealed class StorefrontPlaceOrderRequest
    {
        [Required]
        public Guid CheckoutSessionId { get; set; }

        [Range(1, int.MaxValue)]
        public int ExpectedCartVersion { get; set; }

        [Required]
        [MaxLength(128)]
        public string IdempotencyKey { get; set; } = string.Empty;
    }

    public sealed record StorefrontPlaceOrderResponse(
        Guid CheckoutSessionId,
        Guid PaymentAttemptId,
        Guid? OrderId,
        string? Reference,
        string? OrderStatus,
        string PaymentStatus,
        string PaymentMethodKey,
        decimal TotalAmount,
        string CurrencyCode,
        string IdempotencyKey,
        DateTime CreatedOn,
        StorefrontPaymentNextActionResponse? NextAction);

    public sealed record StorefrontPaymentAttemptResponse(
        Guid Id,
        Guid CheckoutSessionId,
        Guid? OrderId,
        string PaymentMethodKey,
        string ProviderKey,
        string State,
        decimal Amount,
        string CurrencyCode,
        string? ProviderReference,
        string? ProviderSessionId,
        StorefrontPaymentNextActionResponse? NextAction,
        string? FailureCode,
        string? FailureMessage,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

    public sealed record StorefrontPaymentNextActionResponse(
        string Type,
        string? Url);

    public sealed class StorefrontPaymentCallbackRequest
    {
        public Guid? PaymentAttemptId { get; set; }

        [MaxLength(256)]
        public string? ProviderEventId { get; set; }

        [Required]
        [MaxLength(128)]
        public string EventType { get; set; } = "provider.callback";

        [MaxLength(32)]
        public string? State { get; set; }

        [MaxLength(256)]
        public string? ProviderReference { get; set; }

        [MaxLength(256)]
        public string? ProviderSessionId { get; set; }

        [MaxLength(128)]
        public string? FailureCode { get; set; }

        [MaxLength(512)]
        public string? FailureMessage { get; set; }

        [Required]
        public string PayloadJson { get; set; } = "{}";
    }

    public sealed class StorefrontPaymentWebhookRequest
    {
        public Guid? PaymentAttemptId { get; set; }

        [MaxLength(256)]
        public string? EventId { get; set; }

        [Required]
        [MaxLength(128)]
        public string EventType { get; set; } = string.Empty;

        [MaxLength(32)]
        public string? State { get; set; }

        [Required]
        public string PayloadJson { get; set; } = "{}";
    }

    public sealed record StorefrontPaymentWebhookAcceptedResponse(
        string ProviderKey,
        string? EventId,
        bool Duplicate,
        string PayloadHash,
        DateTimeOffset AcceptedAtUtc);

    public sealed record StorefrontCheckoutLineSummaryResponse(
        Guid LineId,
        Guid ProductId,
        Guid? ProductVariantId,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal,
        string CurrencyCode);

    public sealed record StorefrontCheckoutValidationIssueResponse(
        string Code,
        string Message,
        string? Field,
        Guid? LineId,
        Guid? ProductId);

    public sealed class StorefrontCartItemRequest
    {
        [Required]
        public Guid ProductId { get; set; }

        public Guid? ProductVariantId { get; set; }

        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public sealed class StorefrontCreateCartSessionRequest
    {
        [MaxLength(512)]
        public string? CartToken { get; set; }
    }

    public sealed class StorefrontCartLineCreateRequest
    {
        [Required]
        public Guid ProductId { get; set; }

        public Guid? ProductVariantId { get; set; }

        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }

        [MaxLength(128)]
        public string? PersonalizationHash { get; set; }

        [MaxLength(8192)]
        public string? PersonalizationJson { get; set; }

        public Guid? ArtworkAssetId { get; set; }

        [Range(1, int.MaxValue)]
        public int? ArtworkVersion { get; set; }

        [MaxLength(64)]
        public string? FulfillmentProviderKey { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [StringLength(3, MinimumLength = 3)]
        public string? CurrencyCode { get; set; }
    }

    public sealed class StorefrontCartLineUpdateRequest
    {
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public sealed class StorefrontCartValidateRequest
    {
        [Range(1, int.MaxValue)]
        public int? ExpectedVersion { get; set; }
    }

    public sealed record StorefrontCartSessionResponse(
        Guid CartId,
        string CartToken,
        string State,
        int Version,
        DateTimeOffset ExpiresAtUtc);

    public sealed record StorefrontCartResponse(
        Guid CartId,
        string State,
        int Version,
        DateTimeOffset LastActivityAtUtc,
        DateTimeOffset ExpiresAtUtc,
        IReadOnlyList<StorefrontCartLineResponse> Lines);

    public sealed record StorefrontCartLineResponse(
        Guid LineId,
        Guid ProductId,
        Guid? ProductVariantId,
        string? SelectedAttributesJson,
        string? PersonalizationHash,
        string? PersonalizationJson,
        Guid? ArtworkAssetId,
        int? ArtworkVersion,
        string? FulfillmentProviderKey,
        int Quantity,
        decimal? UnitPriceSnapshot,
        string? CurrencyCodeSnapshot);

    public sealed record StorefrontCartValidationResponse(
        Guid CartId,
        int Version,
        bool IsValid,
        decimal TotalAmount,
        string CurrencyCode,
        IReadOnlyList<StorefrontCartValidationIssueResponse> Issues);

    public sealed record StorefrontCartValidationIssueResponse(
        Guid? LineId,
        Guid? ProductId,
        string Code,
        string Message);

    public sealed class StorefrontOrderItemRequest
    {
        [Required]
        public Guid ProductId { get; set; }

        public Guid? ProductVariantId { get; set; }

        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public sealed class StorefrontNewsletterSubscribeRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(StorefrontContractValidation.EmailMaxLength)]
        public string Email { get; set; } = string.Empty;

        public bool MarketingConsentAccepted { get; set; }

        [MaxLength(4096)]
        public string? CaptchaToken { get; set; }
    }

    public sealed class StorefrontPayPalCaptureRequest
    {
        [Required]
        [MaxLength(512)]
        public string Token { get; set; } = string.Empty;

        [MaxLength(2048)]
        public string? ReturnUrl { get; set; }
    }

    public sealed record StorefrontRegistrationResponse(
        Guid CustomerId);

    public sealed record StorefrontTokenResponse(
        string AccessToken,
        DateTime ExpiresAtUtc);

    public sealed record StorefrontCategoryResponse(
        Guid Id,
        Guid? ParentCategoryId,
        string? Name,
        string? Description,
        string? Slug,
        string? Image,
        int DisplayOrder,
        DateTime? UpdatedAt = null,
        string? MetaTitle = null,
        string? MetaDescription = null,
        string? CanonicalUrl = null,
        string? OgTitle = null,
        string? OgDescription = null,
        string? OgImage = null,
        string? SeoContent = null,
        bool RobotsIndex = true,
        bool RobotsFollow = true);

    public sealed record StorefrontCategoryTreeNodeResponse(
        Guid Id,
        Guid? ParentCategoryId,
        string? Name,
        string? Slug,
        string? Image,
        int DisplayOrder,
        IReadOnlyList<StorefrontCategoryTreeNodeResponse> Children);

    public sealed record StorefrontCategoryPageResponse(
        StorefrontCategoryResponse Category,
        IReadOnlyList<StorefrontCatalogProductResponse> Products);

    public sealed record StorefrontCatalogProductResponse(
        Guid Id,
        string? Slug,
        string? Name,
        string? Description,
        string? Sku,
        string? ShortDescription,
        decimal Price,
        decimal? ComparePrice,
        string? Image,
        Guid? PrimaryMediaPublicId,
        bool HasPrimaryMedia,
        DateTime CreatedOn,
        DateTime UpdatedAt,
        int DisplayOrder,
        bool InStock,
        DateTime? PublishedOn,
        Guid? CategoryId,
        string? CategoryName,
        string? CategorySlug,
        bool HasVariants,
        string ProductType,
        Guid? VariationTemplateId,
        decimal? DisplayPrice = null,
        decimal? DisplayComparePrice = null,
        string? DisplayCurrencyCode = null);

    public sealed record StorefrontProductResponse(
        Guid Id,
        string? Slug,
        string? Name,
        string? Description,
        string? Sku,
        string? ShortDescription,
        string? FullDescription,
        decimal Price,
        decimal? ComparePrice,
        string? Image,
        int Quantity,
        int DisplayOrder,
        DateTime? PublishedOn,
        string ProductType,
        Guid? VariationTemplateId,
        Guid? CategoryId,
        string? MetaTitle,
        string? MetaDescription,
        string? CanonicalUrl,
        string? OgTitle,
        string? OgDescription,
        string? OgImage,
        string? SeoContent,
        bool RobotsIndex,
        bool RobotsFollow,
        StorefrontCategoryResponse? Category,
        StorefrontVariationTemplateDto? VariationTemplate,
        DateTime CreatedOn,
        DateTime UpdatedAt,
        IReadOnlyList<StorefrontProductVariantResponse> Variants,
        decimal? DisplayPrice = null,
        decimal? DisplayComparePrice = null,
        string? DisplayCurrencyCode = null);

    public sealed record StorefrontProductVariantResponse(
        Guid Id,
        Guid ProductId,
        string? Sku,
        IReadOnlyList<StorefrontProductVariantAttributeResponse> Attributes,
        string? AttributeSignature,
        string? DisplayName,
        int SizeScale,
        string SizeValue,
        decimal? Price,
        decimal EffectivePrice,
        int Stock,
        string? Color,
        bool IsDefault,
        decimal? DisplayPrice = null,
        string? DisplayCurrencyCode = null);

    public sealed record StorefrontProductVariantAttributeResponse(
        string Name,
        string Value);

    public sealed record StorefrontPagedResponse<TItem>(
        IReadOnlyList<TItem> Items,
        int PageNumber,
        int PageSize,
        int TotalCount,
        int TotalPages);

    public sealed record StorefrontCheckoutResultResponse(
        Guid OrderId,
        string Reference,
        string OrderStatus,
        string PaymentStatus,
        string PaymentMethodKey,
        DateTime CreatedOn);

    public sealed record StorefrontOrderResponse(
        Guid Id,
        string Reference,
        string Status,
        string OrderStatus,
        string PaymentStatus,
        string PaymentMethodKey,
        DateTime? PaymentAt,
        string? CurrencyCode,
        decimal TotalAmount,
        DateTime CreatedOn,
        string ShippingStatus,
        string? ShippingCarrier,
        string? TrackingNumber,
        string? TrackingUrl,
        DateTime? ShippedOn,
        DateTime? DeliveredOn,
        string? CustomerName,
        string? CustomerEmail,
        StorefrontShippingAddressResponse ShippingAddress,
        DateTime? CompletedAt,
        DateTime? CancelledAt,
        IReadOnlyList<StorefrontOrderLineResponse> Lines);

    public sealed record StorefrontShippingAddressResponse(
        string? FullName,
        string? Email,
        string? Phone,
        string? Address1,
        string? Address2,
        string? City,
        string? State,
        string? PostalCode,
        string? CountryCode);

    public sealed record StorefrontOrderLineResponse(
        Guid ProductId,
        string? ProductName,
        string? Sku,
        string? Image,
        Guid? ProductVariantId,
        IReadOnlyList<StorefrontProductVariantAttributeResponse> VariantAttributes,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal);

    public sealed record StorefrontOrderItemHistoryResponse(
        string? ProductName,
        int QuantityOrdered,
        string? CustomerName,
        string? CustomerEmail,
        decimal AmountPaid,
        DateTime DatePurchased,
        string? TrackingNumber,
        string? TrackingUrl,
        string? ShippingStatus);

    public sealed record StorefrontPaymentMethodResponse(
        Guid Id,
        string Key,
        string Name,
        string? Description,
        string? ShortDisplayText,
        string? IconUrl,
        IReadOnlyList<string> SupportedCurrencyCodes,
        IReadOnlyList<string> SupportedCountryCodes);

    public sealed record StorefrontPayPalCaptureResponse(
        bool Captured,
        string RedirectPath,
        string Message);

    public sealed record StorefrontProductRecommendationResponse(
        Guid Id,
        string? Name,
        string? Image,
        decimal Price,
        string? CategoryName);

    public sealed record StorefrontMaintenanceResponse(
        Guid PublicId,
        string StoreKey,
        string Name,
        bool MaintenanceModeEnabled,
        string? MaintenanceMessage);

    public sealed record StorefrontCurrentStoreResponse(
        Guid PublicId,
        string StoreKey,
        string Name,
        string Status,
        string? BaseUrl,
        string? PrimaryDomain,
        bool ForceHttps,
        string? CdnHost,
        string? LogoUrl,
        string? CompanyName,
        string? CompanyEmail,
        string? CompanyPhone,
        string? CompanyAddress,
        string? FaviconUrl,
        string? PngIconUrl,
        string? AppleTouchIconUrl,
        string? MsTileImageUrl,
        string? MsTileColor,
        string DefaultCurrencyCode,
        string DefaultCulture,
        string? SupportEmail,
        string? SupportPhone,
        bool MaintenanceModeEnabled,
        string? MaintenanceMessage,
        string? HtmlBodyId);

    public sealed record StorefrontPublicConfigurationResponse(
        StorefrontStoreIdentityResponse StoreIdentity,
        StorefrontBrandingResponse Branding,
        StorefrontLocaleOptionsResponse LocaleOptions,
        StorefrontCurrencyOptionsResponse CurrencyOptions,
        StorefrontConsentConfigurationResponse Consent,
        StorefrontCaptchaConfigurationResponse Captcha,
        StorefrontMaintenanceStateResponse MaintenanceState,
        StorefrontFeatureFlagsResponse FeatureFlags,
        IReadOnlyList<StorefrontPaymentMethodResponse> PaymentMethods,
        StorefrontSeoDefaultsResponse SeoDefaults);

    public sealed record StorefrontStoreIdentityResponse(
        Guid PublicId,
        string StoreKey,
        string Name,
        string Status,
        string? BaseUrl,
        string? PrimaryDomain,
        bool ForceHttps);

    public sealed record StorefrontBrandingResponse(
        string? CdnHost,
        string? LogoUrl,
        string? CompanyName,
        string? CompanyEmail,
        string? CompanyPhone,
        string? CompanyAddress,
        string? FaviconUrl,
        string? PngIconUrl,
        string? AppleTouchIconUrl,
        string? MsTileImageUrl,
        string? MsTileColor,
        string? SupportEmail,
        string? SupportPhone,
        string? HtmlBodyId);

    public sealed record StorefrontLocaleOptionsResponse(
        string DefaultCulture,
        IReadOnlyList<string> SupportedCultures);

    public sealed record StorefrontCurrencyOptionsResponse(
        string DefaultCurrencyCode,
        IReadOnlyList<string> SupportedCurrencyCodes);

    public sealed class StorefrontCurrencyPreferenceRequest
    {
        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CurrencyCode { get; set; } = string.Empty;
    }

    public sealed record StorefrontCurrencyPreferenceResponse(
        string CurrencyCode,
        string BaseCurrencyCode,
        string? RequestedCurrencyCode,
        bool RequestedCurrencySupported,
        bool CheckoutCurrencyEnabled,
        string Reason);

    public sealed record StorefrontConsentConfigurationResponse(
        bool Enabled,
        bool BannerRequired,
        string CurrentVersion,
        string PolicyPagePath,
        IReadOnlyList<StorefrontConsentCategoryResponse> Categories,
        int VisitorCookieLifetimeDays);

    public sealed record StorefrontConsentCategoryResponse(
        string Name,
        bool Required,
        bool DefaultEnabled);

    public sealed record StorefrontConsentResponse(
        bool Enabled,
        bool BannerRequired,
        string ConsentVersion,
        string? ConsentKey,
        StorefrontConsentCategorySelectionResponse Categories,
        DateTimeOffset? UpdatedAtUtc,
        DateTimeOffset? RevokedAtUtc,
        DateTimeOffset? ExpiresAtUtc);

    public sealed record StorefrontConsentCategorySelectionResponse(
        bool Essential,
        bool Preferences,
        bool Analytics,
        bool Marketing);

    public sealed class StorefrontConsentSaveRequest
    {
        public bool Preferences { get; set; }

        public bool Analytics { get; set; }

        public bool Marketing { get; set; }
    }

    public sealed record StorefrontCaptchaConfigurationResponse(
        bool Enabled,
        string ProviderSystemName,
        string? PublicSiteKey,
        IReadOnlyList<string> EnabledTargets,
        IReadOnlyDictionary<string, string> ActionNames);

    public sealed record StorefrontMaintenanceStateResponse(
        bool MaintenanceModeEnabled,
        string? MaintenanceMessage);

    public sealed record StorefrontFeatureFlagsResponse(
        bool CustomerAccountsEnabled,
        bool CartEnabled,
        bool CheckoutEnabled,
        bool PaymentsEnabled,
        bool NewsletterEnabled,
        bool RecommendationsEnabled);

    public sealed record StorefrontSeoDefaultsResponse(
        string? SiteName,
        string? DefaultTitleSuffix,
        string? DefaultMetaDescription,
        string? DefaultOgImage,
        string? BaseCanonicalUrl,
        string? CompanyName,
        string? CompanyLogoUrl,
        string? CompanyPhone,
        string? CompanyEmail,
        string? CompanyAddress,
        string? FacebookUrl,
        string? InstagramUrl,
        string? XUrl);
}
