using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantOrderingPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Addresses_AddressId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_RestaurantId_ModerationStatus",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_OrderStatusHistories_OrderId",
                table: "OrderStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DriverId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_CouponUsages_CouponId",
                table: "CouponUsages");

            migrationBuilder.DropIndex(
                name: "IX_CouponUsages_OrderId",
                table: "CouponUsages");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_CustomerId",
                table: "Addresses");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "Payments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymobOrderId",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChangedByUserId",
                table: "OrderStatusHistories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "OrderStatusHistories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AddressId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "AddressCitySnapshot",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AddressLabelSnapshot",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AddressLandmarkSnapshot",
                table: "Orders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLineSnapshot",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                table: "Orders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Orders",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "VerificationRejectionReason",
                table: "Drivers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationReviewedAt",
                table: "Drivers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationStatus",
                table: "Drivers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotSubmitted");

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationSubmittedAt",
                table: "Drivers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceAtAdd",
                table: "CartItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Floor",
                table: "Addresses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Apartment",
                table: "Addresses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Addresses",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.CreateTable(
                name: "ProcessedWebhooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExternalEventId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedWebhooks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_RestaurantId_ModerationStatus_CreatedAt",
                table: "Reviews",
                columns: new[] { "RestaurantId", "ModerationStatus", "CreatedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Review_Rating",
                table: "Reviews",
                sql: "[Rating] BETWEEN 1 AND 5");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewReports_ReviewId_RestaurantOwnerId",
                table: "ReviewReports",
                columns: new[] { "ReviewId", "RestaurantOwnerId" },
                unique: true,
                filter: "[Status] = 'Open'");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymobTransactionReference",
                table: "Payments",
                column: "PaymobTransactionReference",
                unique: true,
                filter: "[PaymobTransactionReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_OrderId_ChangedAt",
                table: "OrderStatusHistories",
                columns: new[] { "OrderId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_Status_ChangedAt",
                table: "OrderStatusHistories",
                columns: new[] { "Status", "ChangedAt" })
                .Annotation("SqlServer:Include", new[] { "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId_CreatedAt",
                table: "Orders",
                columns: new[] { "CustomerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_RestaurantId_CreatedAt",
                table: "Orders",
                columns: new[] { "RestaurantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_DriverId",
                table: "Orders",
                columns: new[] { "Status", "DriverId" });

            migrationBuilder.CreateIndex(
                name: "UX_Orders_DriverId_ActiveDelivery",
                table: "Orders",
                column: "DriverId",
                unique: true,
                filter: "[DriverId] IS NOT NULL AND [Status] IN ('DriverAssigned','PickedUp','OnTheWay')");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_VerificationStatus",
                table: "Drivers",
                column: "VerificationStatus");

            migrationBuilder.CreateIndex(
                name: "UX_Drivers_NationalID",
                table: "Drivers",
                column: "NationalID",
                unique: true,
                filter: "[VerificationStatus] <> 'NotSubmitted'");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_CouponId_CustomerId",
                table: "CouponUsages",
                columns: new[] { "CouponId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_OrderId",
                table: "CouponUsages",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_CustomerId",
                table: "Addresses",
                column: "CustomerId",
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedWebhooks_OrderId",
                table: "ProcessedWebhooks",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "UX_ProcessedWebhooks_Provider_ExternalEventId",
                table: "ProcessedWebhooks",
                columns: new[] { "Provider", "ExternalEventId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Addresses_AddressId",
                table: "Orders",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Addresses_AddressId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "ProcessedWebhooks");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_RestaurantId_ModerationStatus_CreatedAt",
                table: "Reviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Review_Rating",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_ReviewReports_ReviewId_RestaurantOwnerId",
                table: "ReviewReports");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymobTransactionReference",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_OrderStatusHistories_OrderId_ChangedAt",
                table: "OrderStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_OrderStatusHistories_Status_ChangedAt",
                table: "OrderStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerId_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_RestaurantId_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_DriverId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "UX_Orders_DriverId_ActiveDelivery",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_VerificationStatus",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "UX_Drivers_NationalID",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_CouponUsages_CouponId_CustomerId",
                table: "CouponUsages");

            migrationBuilder.DropIndex(
                name: "IX_CouponUsages_OrderId",
                table: "CouponUsages");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_CustomerId",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymobOrderId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ChangedByUserId",
                table: "OrderStatusHistories");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "OrderStatusHistories");

            migrationBuilder.DropColumn(
                name: "AddressCitySnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AddressLabelSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AddressLandmarkSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AddressLineSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VerificationRejectionReason",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "VerificationReviewedAt",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "VerificationSubmittedAt",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "PriceAtAdd",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Addresses");

            migrationBuilder.AlterColumn<Guid>(
                name: "AddressId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Floor",
                table: "Addresses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Apartment",
                table: "Addresses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_RestaurantId_ModerationStatus",
                table: "Reviews",
                columns: new[] { "RestaurantId", "ModerationStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_OrderId",
                table: "OrderStatusHistories",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DriverId",
                table: "Orders",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_CouponId",
                table: "CouponUsages",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_OrderId",
                table: "CouponUsages",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_CustomerId",
                table: "Addresses",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Addresses_AddressId",
                table: "Orders",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
