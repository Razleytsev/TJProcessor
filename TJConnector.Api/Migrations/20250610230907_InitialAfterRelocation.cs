using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TJConnector.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialAfterRelocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CodeOrders_Products_ProductId",
                table: "CodeOrders");

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Packages",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "User",
                table: "PackageRequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "CodeOrders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_CodeOrders_Products_ProductId",
                table: "CodeOrders",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CodeOrders_Products_ProductId",
                table: "CodeOrders");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "Packages");

            migrationBuilder.AlterColumn<string>(
                name: "User",
                table: "PackageRequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "CodeOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CodeOrders_Products_ProductId",
                table: "CodeOrders",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
