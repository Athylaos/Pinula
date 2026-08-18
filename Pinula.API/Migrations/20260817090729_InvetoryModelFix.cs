using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinula.API.Migrations
{
    /// <inheritdoc />
    public partial class InvetoryModelFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_item_groups_group_id",
                table: "inventory_item");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_item_ingredients_ingredient_id",
                table: "inventory_item");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_item_units_unit_id",
                table: "inventory_item");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_list_item_groups_group_id",
                table: "shopping_list_item");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_list_item_ingredients_ingredient_id",
                table: "shopping_list_item");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_list_item_shopping_categories_shopping_category_id",
                table: "shopping_list_item");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_list_item_units_unit_id",
                table: "shopping_list_item");

            migrationBuilder.DropPrimaryKey(
                name: "pk_shopping_list_item",
                table: "shopping_list_item");

            migrationBuilder.DropPrimaryKey(
                name: "pk_inventory_item",
                table: "inventory_item");

            migrationBuilder.RenameTable(
                name: "shopping_list_item",
                newName: "shopping_list_items");

            migrationBuilder.RenameTable(
                name: "inventory_item",
                newName: "inventory_items");

            migrationBuilder.RenameIndex(
                name: "ix_shopping_list_item_unit_id",
                table: "shopping_list_items",
                newName: "ix_shopping_list_items_unit_id");

            migrationBuilder.RenameIndex(
                name: "ix_shopping_list_item_shopping_category_id",
                table: "shopping_list_items",
                newName: "ix_shopping_list_items_shopping_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_shopping_list_item_ingredient_id",
                table: "shopping_list_items",
                newName: "ix_shopping_list_items_ingredient_id");

            migrationBuilder.RenameIndex(
                name: "ix_shopping_list_item_group_id",
                table: "shopping_list_items",
                newName: "ix_shopping_list_items_group_id");

            migrationBuilder.RenameIndex(
                name: "ix_inventory_item_unit_id",
                table: "inventory_items",
                newName: "ix_inventory_items_unit_id");

            migrationBuilder.RenameIndex(
                name: "ix_inventory_item_ingredient_id",
                table: "inventory_items",
                newName: "ix_inventory_items_ingredient_id");

            migrationBuilder.RenameIndex(
                name: "ix_inventory_item_group_id",
                table: "inventory_items",
                newName: "ix_inventory_items_group_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_shopping_list_items",
                table: "shopping_list_items",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_inventory_items",
                table: "inventory_items",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_items_groups_group_id",
                table: "inventory_items",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_items_ingredients_ingredient_id",
                table: "inventory_items",
                column: "ingredient_id",
                principalTable: "ingredients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_items_units_unit_id",
                table: "inventory_items",
                column: "unit_id",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_list_items_groups_group_id",
                table: "shopping_list_items",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_list_items_ingredients_ingredient_id",
                table: "shopping_list_items",
                column: "ingredient_id",
                principalTable: "ingredients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_list_items_shopping_categories_shopping_category_id",
                table: "shopping_list_items",
                column: "shopping_category_id",
                principalTable: "shopping_categories",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_list_items_units_unit_id",
                table: "shopping_list_items",
                column: "unit_id",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_items_groups_group_id",
                table: "inventory_items");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_items_ingredients_ingredient_id",
                table: "inventory_items");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_items_units_unit_id",
                table: "inventory_items");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_list_items_groups_group_id",
                table: "shopping_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_list_items_ingredients_ingredient_id",
                table: "shopping_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_list_items_shopping_categories_shopping_category_id",
                table: "shopping_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_list_items_units_unit_id",
                table: "shopping_list_items");

            migrationBuilder.DropPrimaryKey(
                name: "pk_shopping_list_items",
                table: "shopping_list_items");

            migrationBuilder.DropPrimaryKey(
                name: "pk_inventory_items",
                table: "inventory_items");

            migrationBuilder.RenameTable(
                name: "shopping_list_items",
                newName: "shopping_list_item");

            migrationBuilder.RenameTable(
                name: "inventory_items",
                newName: "inventory_item");

            migrationBuilder.RenameIndex(
                name: "ix_shopping_list_items_unit_id",
                table: "shopping_list_item",
                newName: "ix_shopping_list_item_unit_id");

            migrationBuilder.RenameIndex(
                name: "ix_shopping_list_items_shopping_category_id",
                table: "shopping_list_item",
                newName: "ix_shopping_list_item_shopping_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_shopping_list_items_ingredient_id",
                table: "shopping_list_item",
                newName: "ix_shopping_list_item_ingredient_id");

            migrationBuilder.RenameIndex(
                name: "ix_shopping_list_items_group_id",
                table: "shopping_list_item",
                newName: "ix_shopping_list_item_group_id");

            migrationBuilder.RenameIndex(
                name: "ix_inventory_items_unit_id",
                table: "inventory_item",
                newName: "ix_inventory_item_unit_id");

            migrationBuilder.RenameIndex(
                name: "ix_inventory_items_ingredient_id",
                table: "inventory_item",
                newName: "ix_inventory_item_ingredient_id");

            migrationBuilder.RenameIndex(
                name: "ix_inventory_items_group_id",
                table: "inventory_item",
                newName: "ix_inventory_item_group_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_shopping_list_item",
                table: "shopping_list_item",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_inventory_item",
                table: "inventory_item",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_item_groups_group_id",
                table: "inventory_item",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_item_ingredients_ingredient_id",
                table: "inventory_item",
                column: "ingredient_id",
                principalTable: "ingredients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_item_units_unit_id",
                table: "inventory_item",
                column: "unit_id",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_list_item_groups_group_id",
                table: "shopping_list_item",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_list_item_ingredients_ingredient_id",
                table: "shopping_list_item",
                column: "ingredient_id",
                principalTable: "ingredients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_list_item_shopping_categories_shopping_category_id",
                table: "shopping_list_item",
                column: "shopping_category_id",
                principalTable: "shopping_categories",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_list_item_units_unit_id",
                table: "shopping_list_item",
                column: "unit_id",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
