using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinula.API.Migrations
{
    /// <inheritdoc />
    public partial class FixesToIngredientSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ingredients_ingredient_type_type_id",
                table: "ingredients");

            migrationBuilder.DropPrimaryKey(
                name: "pk_ingredient_type",
                table: "ingredient_type");

            migrationBuilder.RenameTable(
                name: "ingredient_type",
                newName: "shopping_categories");

            migrationBuilder.RenameColumn(
                name: "type_id",
                table: "ingredients",
                newName: "shopping_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_ingredients_type_id",
                table: "ingredients",
                newName: "ix_ingredients_shopping_category_id");

            migrationBuilder.AddColumn<Guid>(
                name: "base_ingredient_id",
                table: "ingredients",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "off_category_tag",
                table: "ingredients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "shopping_categories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "pk_shopping_categories",
                table: "shopping_categories",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_ingredients_base_ingredient_id",
                table: "ingredients",
                column: "base_ingredient_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ingredients_ingredients_base_ingredient_id",
                table: "ingredients",
                column: "base_ingredient_id",
                principalTable: "ingredients",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_ingredients_shopping_categories_shopping_category_id",
                table: "ingredients",
                column: "shopping_category_id",
                principalTable: "shopping_categories",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ingredients_ingredients_base_ingredient_id",
                table: "ingredients");

            migrationBuilder.DropForeignKey(
                name: "fk_ingredients_shopping_categories_shopping_category_id",
                table: "ingredients");

            migrationBuilder.DropIndex(
                name: "ix_ingredients_base_ingredient_id",
                table: "ingredients");

            migrationBuilder.DropPrimaryKey(
                name: "pk_shopping_categories",
                table: "shopping_categories");

            migrationBuilder.DropColumn(
                name: "base_ingredient_id",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "off_category_tag",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "code",
                table: "shopping_categories");

            migrationBuilder.RenameTable(
                name: "shopping_categories",
                newName: "ingredient_type");

            migrationBuilder.RenameColumn(
                name: "shopping_category_id",
                table: "ingredients",
                newName: "type_id");

            migrationBuilder.RenameIndex(
                name: "ix_ingredients_shopping_category_id",
                table: "ingredients",
                newName: "ix_ingredients_type_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_ingredient_type",
                table: "ingredient_type",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_ingredients_ingredient_type_type_id",
                table: "ingredients",
                column: "type_id",
                principalTable: "ingredient_type",
                principalColumn: "id");
        }
    }
}
