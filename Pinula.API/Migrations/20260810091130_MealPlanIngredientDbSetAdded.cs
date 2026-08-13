using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinula.API.Migrations
{
    /// <inheritdoc />
    public partial class MealPlanIngredientDbSetAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_meal_plan_ingredient_ingredients_ingredient_id",
                table: "meal_plan_ingredient");

            migrationBuilder.DropForeignKey(
                name: "fk_meal_plan_ingredient_meal_plans_meal_plan_id",
                table: "meal_plan_ingredient");

            migrationBuilder.DropForeignKey(
                name: "fk_meal_plan_ingredient_units_unit_id",
                table: "meal_plan_ingredient");

            migrationBuilder.DropPrimaryKey(
                name: "pk_meal_plan_ingredient",
                table: "meal_plan_ingredient");

            migrationBuilder.RenameTable(
                name: "meal_plan_ingredient",
                newName: "meal_plan_ingredients");

            migrationBuilder.RenameIndex(
                name: "ix_meal_plan_ingredient_unit_id",
                table: "meal_plan_ingredients",
                newName: "ix_meal_plan_ingredients_unit_id");

            migrationBuilder.RenameIndex(
                name: "ix_meal_plan_ingredient_ingredient_id",
                table: "meal_plan_ingredients",
                newName: "ix_meal_plan_ingredients_ingredient_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_meal_plan_ingredients",
                table: "meal_plan_ingredients",
                columns: new[] { "meal_plan_id", "ingredient_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_meal_plan_ingredients_ingredients_ingredient_id",
                table: "meal_plan_ingredients",
                column: "ingredient_id",
                principalTable: "ingredients",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_meal_plan_ingredients_meal_plans_meal_plan_id",
                table: "meal_plan_ingredients",
                column: "meal_plan_id",
                principalTable: "meal_plans",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_meal_plan_ingredients_units_unit_id",
                table: "meal_plan_ingredients",
                column: "unit_id",
                principalTable: "units",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_meal_plan_ingredients_ingredients_ingredient_id",
                table: "meal_plan_ingredients");

            migrationBuilder.DropForeignKey(
                name: "fk_meal_plan_ingredients_meal_plans_meal_plan_id",
                table: "meal_plan_ingredients");

            migrationBuilder.DropForeignKey(
                name: "fk_meal_plan_ingredients_units_unit_id",
                table: "meal_plan_ingredients");

            migrationBuilder.DropPrimaryKey(
                name: "pk_meal_plan_ingredients",
                table: "meal_plan_ingredients");

            migrationBuilder.RenameTable(
                name: "meal_plan_ingredients",
                newName: "meal_plan_ingredient");

            migrationBuilder.RenameIndex(
                name: "ix_meal_plan_ingredients_unit_id",
                table: "meal_plan_ingredient",
                newName: "ix_meal_plan_ingredient_unit_id");

            migrationBuilder.RenameIndex(
                name: "ix_meal_plan_ingredients_ingredient_id",
                table: "meal_plan_ingredient",
                newName: "ix_meal_plan_ingredient_ingredient_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_meal_plan_ingredient",
                table: "meal_plan_ingredient",
                columns: new[] { "meal_plan_id", "ingredient_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_meal_plan_ingredient_ingredients_ingredient_id",
                table: "meal_plan_ingredient",
                column: "ingredient_id",
                principalTable: "ingredients",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_meal_plan_ingredient_meal_plans_meal_plan_id",
                table: "meal_plan_ingredient",
                column: "meal_plan_id",
                principalTable: "meal_plans",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_meal_plan_ingredient_units_unit_id",
                table: "meal_plan_ingredient",
                column: "unit_id",
                principalTable: "units",
                principalColumn: "id");
        }
    }
}
