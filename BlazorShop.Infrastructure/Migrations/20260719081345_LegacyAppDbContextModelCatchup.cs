using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LegacyAppDbContextModelCatchup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_SeoRedirects_OldPath",
                table: "SeoRedirects");

            migrationBuilder.DropIndex(
                name: "IX_Products_Slug",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Slug",
                table: "Categories");

            migrationBuilder.AddColumn<Guid>(
                name: "EntityId",
                table: "SeoRedirects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "SeoRedirects",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                table: "SeoRedirects",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "SeoRedirects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttributeSignature",
                table: "ProductVariants",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttributesJson",
                table: "ProductVariants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "ProductVariants",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProductVariants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Products",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableEndUtc",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableStartUtc",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Products",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ComparePrice",
                table: "Products",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "Products",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryEstimateText",
                table: "Products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "FreeShipping",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FullDescription",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gtin",
                table: "Products",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Height",
                table: "Products",
                type: "numeric(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HideWhenOutOfStock",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Length",
                table: "Products",
                type: "numeric(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ManageStock",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ManufacturerPartNumber",
                table: "Products",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxOrderQuantity",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinOrderQuantity",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProductType",
                table: "Products",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Simple");

            migrationBuilder.AddColumn<bool>(
                name: "PurchasingDisabled",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PurchasingDisabledReason",
                table: "Products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityStep",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ShippingRequired",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingSurcharge",
                table: "Products",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "Products",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Products",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<Guid>(
                name: "VariationTemplateId",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "Products",
                type: "numeric(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Width",
                table: "Products",
                type: "numeric(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseDiscountTotalAmount",
                table: "Orders",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseGrandTotalAmount",
                table: "Orders",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseShippingTotalAmount",
                table: "Orders",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseSubtotalAmount",
                table: "Orders",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseTaxTotalAmount",
                table: "Orders",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddressSnapshotJson",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountTotalAmount",
                table: "Orders",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GrandTotalAmount",
                table: "Orders",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GuestAccessTokenExpiresAtUtc",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestAccessTokenHash",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddressSnapshotJson",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingCurrencyCode",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingDeliveryEstimateText",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingMethodCode",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingMethodKey",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingMethodName",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingMethodSnapshotJson",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingProviderSystemName",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingTotal",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingTotalAmount",
                table: "Orders",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreBaseUrlSnapshot",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreCompanyAddressSnapshot",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreCompanyEmailSnapshot",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreCompanyNameSnapshot",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreCompanyPhoneSnapshot",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreKeySnapshot",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreNameSnapshot",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StorePublicId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalAmount",
                table: "Orders",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxTotalAmount",
                table: "Orders",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ArtworkAssetId",
                table: "OrderLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArtworkVersion",
                table: "OrderLines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FulfillmentProviderKey",
                table: "OrderLines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "OrderLines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalizationHash",
                table: "OrderLines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalizationJson",
                table: "OrderLines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "OrderLines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductVariantId",
                table: "OrderLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "OrderLines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantAttributesJson",
                table: "OrderLines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "NewsletterSubscribers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "CheckoutOrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "Categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentCategoryId",
                table: "Categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "Categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Categories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.CreateTable(
                name: "CommerceStore",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    ControlPlaneStorePublicId = table.Column<Guid>(type: "uuid", nullable: true),
                    StoreKey = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    BaseUrl = table.Column<string>(type: "text", nullable: true),
                    ForceHttps = table.Column<bool>(type: "boolean", nullable: false),
                    SslEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SslPort = table.Column<int>(type: "integer", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    HtmlBodyId = table.Column<string>(type: "text", nullable: true),
                    CdnHost = table.Column<string>(type: "text", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    CompanyName = table.Column<string>(type: "text", nullable: true),
                    CompanyEmail = table.Column<string>(type: "text", nullable: true),
                    CompanyPhone = table.Column<string>(type: "text", nullable: true),
                    CompanyAddress = table.Column<string>(type: "text", nullable: true),
                    FaviconUrl = table.Column<string>(type: "text", nullable: true),
                    PngIconUrl = table.Column<string>(type: "text", nullable: true),
                    AppleTouchIconUrl = table.Column<string>(type: "text", nullable: true),
                    MsTileImageUrl = table.Column<string>(type: "text", nullable: true),
                    MsTileColor = table.Column<string>(type: "text", nullable: true),
                    DefaultCurrencyCode = table.Column<string>(type: "text", nullable: false),
                    DefaultCulture = table.Column<string>(type: "text", nullable: false),
                    SupportEmail = table.Column<string>(type: "text", nullable: true),
                    SupportPhone = table.Column<string>(type: "text", nullable: true),
                    MaintenanceModeEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MaintenanceMessage = table.Column<string>(type: "text", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommerceStore", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VariationTemplate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariationTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommerceCustomer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppUserId = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    NormalizedEmail = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    Company = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    PreferredLanguage = table.Column<string>(type: "text", nullable: true),
                    PreferredCurrencyCode = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastActivityAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastCheckoutAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommerceCustomer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommerceCustomer_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommerceCustomer_CommerceStore_StoreId",
                        column: x => x.StoreId,
                        principalTable: "CommerceStore",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommerceStoreDomain",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Domain = table.Column<string>(type: "text", nullable: false),
                    NormalizedDomain = table.Column<string>(type: "text", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DisabledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommerceStoreDomain", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommerceStoreDomain_CommerceStore_StoreId",
                        column: x => x.StoreId,
                        principalTable: "CommerceStore",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "store_seo_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    default_title_suffix = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    default_meta_description = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    default_og_image = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    base_canonical_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    company_logo_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    company_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    company_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    company_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    facebook_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    instagram_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    x_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_seo_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_store_seo_settings_CommerceStore_store_id",
                        column: x => x.store_id,
                        principalTable: "CommerceStore",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VariationTemplateOption",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ControlType = table.Column<string>(type: "text", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariationTemplateOption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VariationTemplateOption_VariationTemplate_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "VariationTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommerceCustomerAddress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Company = table.Column<string>(type: "text", nullable: true),
                    Address1 = table.Column<string>(type: "text", nullable: false),
                    Address2 = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: false),
                    PostalCode = table.Column<string>(type: "text", nullable: false),
                    CountryCode = table.Column<string>(type: "text", nullable: false),
                    StateProvinceCode = table.Column<string>(type: "text", nullable: true),
                    StateProvinceName = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    IsDefaultShipping = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefaultBilling = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommerceCustomerAddress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommerceCustomerAddress_CommerceCustomer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "CommerceCustomer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommerceCustomerAddress_CommerceStore_StoreId",
                        column: x => x.StoreId,
                        principalTable: "CommerceStore",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VariationTemplateValue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ColorHex = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariationTemplateValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VariationTemplateValue_VariationTemplateOption_OptionId",
                        column: x => x.OptionId,
                        principalTable: "VariationTemplateOption",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeoRedirects_StoreId_EntityType_EntityId",
                table: "SeoRedirects",
                columns: new[] { "StoreId", "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SeoRedirects_StoreId_IsActive_OldPath",
                table: "SeoRedirects",
                columns: new[] { "StoreId", "IsActive", "OldPath" });

            migrationBuilder.CreateIndex(
                name: "IX_SeoRedirects_StoreId_OldPath",
                table: "SeoRedirects",
                columns: new[] { "StoreId", "OldPath" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"StoreId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId",
                table: "Products",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId_CategoryId_DisplayOrder_CreatedOn",
                table: "Products",
                columns: new[] { "StoreId", "CategoryId", "DisplayOrder", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId_IsPublished_ArchivedAt",
                table: "Products",
                columns: new[] { "StoreId", "IsPublished", "ArchivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId_IsPublished_ArchivedAt_AvailableStartUtc_A~",
                table: "Products",
                columns: new[] { "StoreId", "IsPublished", "ArchivedAt", "AvailableStartUtc", "AvailableEndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId_ProductType",
                table: "Products",
                columns: new[] { "StoreId", "ProductType" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId_Sku",
                table: "Products",
                columns: new[] { "StoreId", "Sku" },
                unique: true,
                filter: "\"StoreId\" IS NOT NULL AND \"Sku\" IS NOT NULL AND \"ArchivedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId_Slug",
                table: "Products",
                columns: new[] { "StoreId", "Slug" },
                unique: true,
                filter: "\"StoreId\" IS NOT NULL AND \"Slug\" IS NOT NULL AND \"ArchivedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_VariationTemplateId",
                table: "Products",
                column: "VariationTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_StoreId",
                table: "Categories",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_StoreId_IsPublished_ArchivedAt",
                table: "Categories",
                columns: new[] { "StoreId", "IsPublished", "ArchivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_StoreId_ParentCategoryId_DisplayOrder",
                table: "Categories",
                columns: new[] { "StoreId", "ParentCategoryId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_StoreId_Slug",
                table: "Categories",
                columns: new[] { "StoreId", "Slug" },
                unique: true,
                filter: "\"StoreId\" IS NOT NULL AND \"Slug\" IS NOT NULL AND \"ArchivedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommerceCustomer_AppUserId",
                table: "CommerceCustomer",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommerceCustomer_StoreId",
                table: "CommerceCustomer",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_CommerceCustomerAddress_CustomerId",
                table: "CommerceCustomerAddress",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CommerceCustomerAddress_StoreId",
                table: "CommerceCustomerAddress",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_CommerceStoreDomain_StoreId",
                table: "CommerceStoreDomain",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_store_seo_settings_store_id",
                table: "store_seo_settings",
                column: "store_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VariationTemplateOption_TemplateId",
                table: "VariationTemplateOption",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_VariationTemplateValue_OptionId",
                table: "VariationTemplateValue",
                column: "OptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CommerceCustomer_CustomerId",
                table: "Orders",
                column: "CustomerId",
                principalTable: "CommerceCustomer",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_VariationTemplate_VariationTemplateId",
                table: "Products",
                column: "VariationTemplateId",
                principalTable: "VariationTemplate",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Categories_ParentCategoryId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CommerceCustomer_CustomerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_VariationTemplate_VariationTemplateId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "CommerceCustomerAddress");

            migrationBuilder.DropTable(
                name: "CommerceStoreDomain");

            migrationBuilder.DropTable(
                name: "store_seo_settings");

            migrationBuilder.DropTable(
                name: "VariationTemplateValue");

            migrationBuilder.DropTable(
                name: "CommerceCustomer");

            migrationBuilder.DropTable(
                name: "VariationTemplateOption");

            migrationBuilder.DropTable(
                name: "CommerceStore");

            migrationBuilder.DropTable(
                name: "VariationTemplate");

            migrationBuilder.DropIndex(
                name: "IX_SeoRedirects_StoreId_EntityType_EntityId",
                table: "SeoRedirects");

            migrationBuilder.DropIndex(
                name: "IX_SeoRedirects_StoreId_IsActive_OldPath",
                table: "SeoRedirects");

            migrationBuilder.DropIndex(
                name: "IX_SeoRedirects_StoreId_OldPath",
                table: "SeoRedirects");

            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId_CategoryId_DisplayOrder_CreatedOn",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId_IsPublished_ArchivedAt",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId_IsPublished_ArchivedAt_AvailableStartUtc_A~",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId_ProductType",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId_Sku",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId_Slug",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_VariationTemplateId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_StoreId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_StoreId_IsPublished_ArchivedAt",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_StoreId_ParentCategoryId_DisplayOrder",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_StoreId_Slug",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "SeoRedirects");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "SeoRedirects");

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                table: "SeoRedirects");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "SeoRedirects");

            migrationBuilder.DropColumn(
                name: "AttributeSignature",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "AttributesJson",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AvailableEndUtc",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AvailableStartUtc",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ComparePrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Condition",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DeliveryEstimateText",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "FreeShipping",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "FullDescription",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Gtin",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HideWhenOutOfStock",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ManageStock",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ManufacturerPartNumber",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MaxOrderQuantity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MinOrderQuantity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PurchasingDisabled",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PurchasingDisabledReason",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "QuantityStep",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ShippingRequired",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ShippingSurcharge",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ShortDescription",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VariationTemplateId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BaseDiscountTotalAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BaseGrandTotalAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BaseShippingTotalAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BaseSubtotalAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BaseTaxTotalAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BillingAddressSnapshotJson",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountTotalAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "GrandTotalAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "GuestAccessTokenExpiresAtUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "GuestAccessTokenHash",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddressSnapshotJson",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingCurrencyCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingDeliveryEstimateText",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingMethodCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingMethodKey",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingMethodName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingMethodSnapshotJson",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingProviderSystemName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingTotal",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingTotalAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StoreBaseUrlSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StoreCompanyAddressSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StoreCompanyEmailSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StoreCompanyNameSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StoreCompanyPhoneSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StoreKeySnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StoreNameSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StorePublicId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SubtotalAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TaxTotalAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ArtworkAssetId",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "ArtworkVersion",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "FulfillmentProviderKey",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "PersonalizationHash",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "PersonalizationJson",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "VariantAttributesJson",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "NewsletterSubscribers");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "CheckoutOrderItems");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ParentCategoryId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Categories");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeoRedirects_OldPath",
                table: "SeoRedirects",
                column: "OldPath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
