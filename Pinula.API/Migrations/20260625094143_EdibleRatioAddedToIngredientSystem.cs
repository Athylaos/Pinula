using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinula.API.Migrations
{
    /// <inheritdoc />
    public partial class EdibleRatioAddedToIngredientSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "edible_ratio",
                table: "ingredients",
                type: "numeric(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "edible_ratio",
                table: "ingredients");
        }
    }
}
