using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TJConnector.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixCodeOrderContentRelationship2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderContentId",
                table: "CodeOrders",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderContentId",
                table: "CodeOrders");
        }
    }
}
