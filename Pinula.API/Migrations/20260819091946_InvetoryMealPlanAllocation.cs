using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinula.API.Migrations
{
    /// <inheritdoc />
    public partial class InvetoryMealPlanAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_meal_plan_ingredients",
                table: "meal_plan_ingredients");

            migrationBuilder.DropColumn(
                name: "allocated_quantity_in_grams",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "is_allocated",
                table: "inventory_items");

            migrationBuilder.AddColumn<Guid>(
                name: "meal_plan_ingredient_id",
                table: "shopping_list_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "recipe_ingredients",
                type: "numeric(10,3)",
                precision: 10,
                scale: 3,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,3)",
                oldPrecision: 10,
                oldScale: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "conversion_factor",
                table: "recipe_ingredients",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,6)",
                oldPrecision: 12,
                oldScale: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "meal_plan_ingredients",
                type: "numeric(10,3)",
                precision: 10,
                scale: 3,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,3)",
                oldPrecision: 10,
                oldScale: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "conversion_factor",
                table: "meal_plan_ingredients",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,6)",
                oldPrecision: 12,
                oldScale: 6,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "meal_plan_ingredients",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddPrimaryKey(
                name: "pk_meal_plan_ingredients",
                table: "meal_plan_ingredients",
                column: "id");

            migrationBuilder.CreateTable(
                name: "inventory_meal_plan_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meal_plan_ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocated_quantity_in_grams = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    allocated_at = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_meal_plan_allocations", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_meal_plan_allocations_inventory_items_inventory_i",
                        column: x => x.inventory_item_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_meal_plan_allocations_meal_plan_ingredients_meal_",
                        column: x => x.meal_plan_ingredient_id,
                        principalTable: "meal_plan_ingredients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_items_meal_plan_ingredient_id",
                table: "shopping_list_items",
                column: "meal_plan_ingredient_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_meal_plan_ingredients_meal_plan_id",
                table: "meal_plan_ingredients",
                column: "meal_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_meal_plan_allocations_inventory_item_id",
                table: "inventory_meal_plan_allocations",
                column: "inventory_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_meal_plan_allocations_meal_plan_ingredient_id",
                table: "inventory_meal_plan_allocations",
                column: "meal_plan_ingredient_id");

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_list_items_meal_plan_ingredients_meal_plan_ingredi",
                table: "shopping_list_items",
                column: "meal_plan_ingredient_id",
                principalTable: "meal_plan_ingredients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_shopping_list_items_meal_plan_ingredients_meal_plan_ingredi",
                table: "shopping_list_items");

            migrationBuilder.DropTable(
                name: "inventory_meal_plan_allocations");

            migrationBuilder.DropIndex(
                name: "ix_shopping_list_items_meal_plan_ingredient_id",
                table: "shopping_list_items");

            migrationBuilder.DropPrimaryKey(
                name: "pk_meal_plan_ingredients",
                table: "meal_plan_ingredients");

            migrationBuilder.DropIndex(
                name: "ix_meal_plan_ingredients_meal_plan_id",
                table: "meal_plan_ingredients");

            migrationBuilder.DropColumn(
                name: "meal_plan_ingredient_id",
                table: "shopping_list_items");

            migrationBuilder.DropColumn(
                name: "id",
                table: "meal_plan_ingredients");

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "recipe_ingredients",
                type: "numeric(10,3)",
                precision: 10,
                scale: 3,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,3)",
                oldPrecision: 10,
                oldScale: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "conversion_factor",
                table: "recipe_ingredients",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,6)",
                oldPrecision: 12,
                oldScale: 6);

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "meal_plan_ingredients",
                type: "numeric(10,3)",
                precision: 10,
                scale: 3,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,3)",
                oldPrecision: 10,
                oldScale: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "conversion_factor",
                table: "meal_plan_ingredients",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,6)",
                oldPrecision: 12,
                oldScale: 6);

            migrationBuilder.AddColumn<decimal>(
                name: "allocated_quantity_in_grams",
                table: "inventory_items",
                type: "numeric(10,3)",
                precision: 10,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "is_allocated",
                table: "inventory_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "pk_meal_plan_ingredients",
                table: "meal_plan_ingredients",
                columns: new[] { "meal_plan_id", "ingredient_id" });
        }
    }
}
