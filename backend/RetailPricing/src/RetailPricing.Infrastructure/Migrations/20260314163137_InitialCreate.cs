using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailPricing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UploadBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoreId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    ProcessedRows = table.Column<int>(type: "int", nullable: false),
                    FailedRows = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ErrorSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    BlobStoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PricingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    UploadBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricingRecords_UploadBatches_UploadBatchId",
                        column: x => x.UploadBatchId,
                        principalTable: "UploadBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PricingRecords_EffectiveDate_Price",
                table: "PricingRecords",
                columns: new[] { "EffectiveDate", "Price" });

            migrationBuilder.CreateIndex(
                name: "IX_PricingRecords_Sku",
                table: "PricingRecords",
                column: "Sku");

            migrationBuilder.CreateIndex(
                name: "IX_PricingRecords_StoreId_EffectiveDate",
                table: "PricingRecords",
                columns: new[] { "StoreId", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PricingRecords_StoreId_Sku_EffectiveDate",
                table: "PricingRecords",
                columns: new[] { "StoreId", "Sku", "EffectiveDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PricingRecords_UploadBatchId",
                table: "PricingRecords",
                column: "UploadBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadBatches_CreatedAt",
                table: "UploadBatches",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UploadBatches_StoreId",
                table: "UploadBatches",
                column: "StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PricingRecords");

            migrationBuilder.DropTable(
                name: "UploadBatches");
        }
    }
}
