using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TJConnector.Api.Migrations
{
    /// <inheritdoc />
    public partial class MoscowMigrate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderContentId",
                table: "CodeOrders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderContentId",
                table: "CodeOrders",
                type: "integer",
                nullable: true);
        }
    }
}
