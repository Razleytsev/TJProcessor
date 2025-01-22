using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TJConnector.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveColumnsFixClasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CodeOrders_Products_ProductId1",
                table: "CodeOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Packages_PackageRequests_PackageRequestId1",
                table: "Packages");

            migrationBuilder.DropIndex(
                name: "IX_Packages_PackageRequestId1",
                table: "Packages");

            migrationBuilder.DropIndex(
                name: "IX_CodeOrders_ProductId1",
                table: "CodeOrders");

            migrationBuilder.DropColumn(
                name: "PackageRequestId1",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "CodeOrders");

            migrationBuilder.DropColumn(
                name: "StatusHistory",
                table: "CodeOrders");

            migrationBuilder.AddColumn<string[]>(
                name: "OrderContent",
                table: "CodeOrdersContents",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AlterColumn<Guid>(
                name: "ExternalGuid",
                table: "CodeOrders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "StatusHistoryJson",
                table: "CodeOrders",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StatusMessage",
                table: "CodeOrders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderContent",
                table: "CodeOrdersContents");

            migrationBuilder.DropColumn(
                name: "StatusHistoryJson",
                table: "CodeOrders");

            migrationBuilder.DropColumn(
                name: "StatusMessage",
                table: "CodeOrders");

            migrationBuilder.AddColumn<int>(
                name: "PackageRequestId1",
                table: "Packages",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ExternalGuid",
                table: "CodeOrders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductId1",
                table: "CodeOrders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusHistory",
                table: "CodeOrders",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Packages_PackageRequestId1",
                table: "Packages",
                column: "PackageRequestId1");

            migrationBuilder.CreateIndex(
                name: "IX_CodeOrders_ProductId1",
                table: "CodeOrders",
                column: "ProductId1");

            migrationBuilder.AddForeignKey(
                name: "FK_CodeOrders_Products_ProductId1",
                table: "CodeOrders",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Packages_PackageRequests_PackageRequestId1",
                table: "Packages",
                column: "PackageRequestId1",
                principalTable: "PackageRequests",
                principalColumn: "Id");
        }
    }
}
