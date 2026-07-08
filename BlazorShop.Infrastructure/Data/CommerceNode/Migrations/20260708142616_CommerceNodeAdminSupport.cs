using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeAdminSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ActorEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Action = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    StoreSupportEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StoreSupportPhone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DefaultCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DefaultCulture = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MaintenanceModeEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MaintenanceMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AllowGuestCheckout = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultShippingStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AutoConfirmPaidOrders = table.Column<bool>(type: "boolean", nullable: false),
                    OrderReferencePrefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SmtpHost = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SmtpFromEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SmtpFromDisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MetaTitle = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    MetaDescription = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    CanonicalUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    OgTitle = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    OgDescription = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    OgImage = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    RobotsIndex = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RobotsFollow = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SeoContent = table.Column<string>(type: "text", nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CheckoutOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutOrderItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsletterSubscribers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterSubscribers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ShippingCarrier = table.Column<string>(type: "text", nullable: true),
                    TrackingNumber = table.Column<string>(type: "text", nullable: true),
                    TrackingUrl = table.Column<string>(type: "text", nullable: true),
                    ShippingStatus = table.Column<string>(type: "text", nullable: false),
                    ShippedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastTrackingUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdminNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeoRedirects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OldPath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    NewPath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: false, defaultValue: 301),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeoRedirects", x => x.Id);
                    table.CheckConstraint("CK_SeoRedirects_StatusCode", "\"StatusCode\" IN (301, 302)");
                });

            migrationBuilder.CreateTable(
                name: "SeoSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DefaultTitleSuffix = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DefaultMetaDescription = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    DefaultOgImage = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    BaseCanonicalUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CompanyLogoUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CompanyPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CompanyEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    CompanyAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FacebookUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    InstagramUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    XUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeoSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Image = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MetaTitle = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    MetaDescription = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    CanonicalUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    OgTitle = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    OgDescription = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    OgImage = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    RobotsIndex = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RobotsFollow = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SeoContent = table.Column<string>(type: "text", nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    PublishedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderLines_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SizeScale = table.Column<int>(type: "integer", nullable: false),
                    SizeValue = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "PaymentMethods",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("3604fc1d-cd6a-46ad-ace4-9b5f8e03f43b"), "Credit Card" },
                    { new Guid("6f2c2a7e-9f9b-4a0d-9f7f-2a1b3c4d5e6f"), "Cash on Delivery" },
                    { new Guid("b2e5c1d4-7a9f-4d2c-8f1e-3a4b5c6d7e8f"), "Bank Transfer" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_ActorUserId",
                table: "AdminAuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_CreatedOn",
                table: "AdminAuditLogs",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_EntityType_EntityId",
                table: "AdminAuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscribers_Email",
                table: "NewsletterSubscribers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_OrderId",
                table: "OrderLines",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedOn",
                table: "Orders",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Reference",
                table: "Orders",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId_CreatedOn",
                table: "Orders",
                columns: new[] { "UserId", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId_CreatedOn",
                table: "Products",
                columns: new[] { "CategoryId", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_SizeScale_SizeValue",
                table: "ProductVariants",
                columns: new[] { "ProductId", "SizeScale", "SizeValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeoRedirects_OldPath",
                table: "SeoRedirects",
                column: "OldPath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAuditLogs");

            migrationBuilder.DropTable(
                name: "AdminSettings");

            migrationBuilder.DropTable(
                name: "CheckoutOrderItems");

            migrationBuilder.DropTable(
                name: "NewsletterSubscribers");

            migrationBuilder.DropTable(
                name: "OrderLines");

            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "SeoRedirects");

            migrationBuilder.DropTable(
                name: "SeoSettings");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
