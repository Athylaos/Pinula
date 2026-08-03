using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinula.API.Migrations
{
    /// <inheritdoc />
    public partial class MealPlanIngredientAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "meal_plan_ingredient",
                columns: table => new
                {
                    meal_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversion_factor = table.Column<decimal>(type: "numeric(12,6)", precision: 12, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meal_plan_ingredient", x => new { x.meal_plan_id, x.ingredient_id });
                    table.ForeignKey(
                        name: "fk_meal_plan_ingredient_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_meal_plan_ingredient_meal_plans_meal_plan_id",
                        column: x => x.meal_plan_id,
                        principalTable: "meal_plans",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_meal_plan_ingredient_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "units",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_meal_plan_ingredient_ingredient_id",
                table: "meal_plan_ingredient",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_meal_plan_ingredient_unit_id",
                table: "meal_plan_ingredient",
                column: "unit_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "meal_plan_ingredient");
        }
    }
}
