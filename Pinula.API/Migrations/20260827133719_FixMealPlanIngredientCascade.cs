using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinula.API.Migrations
{
    /// <inheritdoc />
    public partial class FixMealPlanIngredientCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_meal_plan_ingredients_meal_plans_meal_plan_id",
                table: "meal_plan_ingredients");

            migrationBuilder.AddForeignKey(
                name: "fk_meal_plan_ingredients_meal_plans_meal_plan_id",
                table: "meal_plan_ingredients",
                column: "meal_plan_id",
                principalTable: "meal_plans",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_meal_plan_ingredients_meal_plans_meal_plan_id",
                table: "meal_plan_ingredients");

            migrationBuilder.AddForeignKey(
                name: "fk_meal_plan_ingredients_meal_plans_meal_plan_id",
                table: "meal_plan_ingredients",
                column: "meal_plan_id",
                principalTable: "meal_plans",
                principalColumn: "id");
        }
    }
}
