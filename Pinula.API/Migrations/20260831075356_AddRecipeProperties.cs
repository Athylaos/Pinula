using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinula.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_gluten_free",
                table: "recipes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_lactose_free",
                table: "recipes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_vegan",
                table: "recipes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_vegetarian",
                table: "recipes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "salt",
                table: "recipes",
                type: "numeric(10,3)",
                precision: 10,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "saturated_fats",
                table: "recipes",
                type: "numeric(10,3)",
                precision: 10,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sugars",
                table: "recipes",
                type: "numeric(10,3)",
                precision: 10,
                scale: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_gluten_free",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "is_lactose_free",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "is_vegan",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "is_vegetarian",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "salt",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "saturated_fats",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "sugars",
                table: "recipes");
        }
    }
}
