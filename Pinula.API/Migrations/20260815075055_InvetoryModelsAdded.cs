using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinula.API.Migrations
{
    /// <inheritdoc />
    public partial class InvetoryModelsAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_cooked",
                table: "meal_plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "inventory_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    quantity_in_grams = table.Column<decimal>(type: "numeric", nullable: false),
                    expiration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_allocated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_item_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_item_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_item_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shopping_list_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    quantity_in_grams = table.Column<decimal>(type: "numeric", nullable: false),
                    shopping_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_purchased = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_list_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_shopping_list_item_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shopping_list_item_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shopping_list_item_shopping_categories_shopping_category_id",
                        column: x => x.shopping_category_id,
                        principalTable: "shopping_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_shopping_list_item_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_item_group_id",
                table: "inventory_item",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_item_ingredient_id",
                table: "inventory_item",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_item_unit_id",
                table: "inventory_item",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_item_group_id",
                table: "shopping_list_item",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_item_ingredient_id",
                table: "shopping_list_item",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_item_shopping_category_id",
                table: "shopping_list_item",
                column: "shopping_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_item_unit_id",
                table: "shopping_list_item",
                column: "unit_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_item");

            migrationBuilder.DropTable(
                name: "shopping_list_item");

            migrationBuilder.DropColumn(
                name: "is_cooked",
                table: "meal_plans");
        }
    }
}
