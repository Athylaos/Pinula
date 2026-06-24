using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinula.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateToIngredientSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ingredient_units_ingredients_ingredient_id",
                table: "ingredient_units");

            migrationBuilder.RenameColumn(
                name: "to_default_unit",
                table: "ingredient_units",
                newName: "amount_in_grams");

            migrationBuilder.AddColumn<string>(name: "barcode", table: "ingredients", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "image_url", table: "ingredients", type: "text", nullable: true);
            migrationBuilder.AddColumn<int>(name: "nova_classification", table: "ingredients", type: "integer", nullable: true);
            migrationBuilder.AddColumn<string>(name: "nutri_score", table: "ingredients", type: "text", nullable: true);

            migrationBuilder.AddColumn<bool>(name: "is_gluten_free", table: "ingredients", type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "is_lactose_free", table: "ingredients", type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "is_vegan", table: "ingredients", type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "is_vegetarian", table: "ingredients", type: "boolean", nullable: false, defaultValue: false);

            migrationBuilder.AddColumn<decimal>(name: "salt", table: "ingredients", type: "numeric(10,3)", precision: 10, scale: 3, nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "saturated_fats", table: "ingredients", type: "numeric(10,3)", precision: 10, scale: 3, nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "sugars", table: "ingredients", type: "numeric(10,3)", precision: 10, scale: 3, nullable: false, defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ingredient_type",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    names = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingredient_type", x => x.id);
                });

            var defaultTypeId = new Guid("00000000-0000-0000-0000-000000000001");
            migrationBuilder.Sql($"INSERT INTO ingredient_type (id, names) VALUES ('{defaultTypeId}', '{{\"en\": \"Uncategorized\", \"cs\": \"Nezařazeno\"}}'::jsonb) ON CONFLICT DO NOTHING;");

            migrationBuilder.AddColumn<Guid>(name: "type_id", table: "ingredients", type: "uuid", nullable: true);
            migrationBuilder.Sql($"UPDATE ingredients SET type_id = '{defaultTypeId}' WHERE type_id IS NULL;");
            migrationBuilder.AlterColumn<Guid>(name: "type_id", table: "ingredients", type: "uuid", nullable: false);


            migrationBuilder.Sql("ALTER TABLE ingredients ADD COLUMN names jsonb NULL;");

            migrationBuilder.Sql("UPDATE ingredients SET names = json_build_object('en', name) WHERE name IS NOT NULL;");

            migrationBuilder.Sql("UPDATE ingredients SET names = '{}'::jsonb WHERE names IS NULL;");

            migrationBuilder.Sql("ALTER TABLE ingredients ALTER COLUMN names SET NOT NULL;");

            migrationBuilder.Sql("ALTER TABLE ingredients DROP COLUMN name;");

            migrationBuilder.CreateIndex(
                name: "ix_ingredients_type_id",
                table: "ingredients",
                column: "type_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ingredient_units_ingredients_ingredient_id",
                table: "ingredient_units",
                column: "ingredient_id",
                principalTable: "ingredients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_ingredients_ingredient_type_type_id",
                table: "ingredients",
                column: "type_id",
                principalTable: "ingredient_type",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ingredient_units_ingredients_ingredient_id",
                table: "ingredient_units");

            migrationBuilder.DropForeignKey(
                name: "fk_ingredients_ingredient_type_type_id",
                table: "ingredients");

            migrationBuilder.DropTable(
                name: "ingredient_type");

            migrationBuilder.DropIndex(
                name: "ix_ingredients_type_id",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "barcode",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "is_gluten_free",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "is_lactose_free",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "is_vegan",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "is_vegetarian",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "names",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "nova_classification",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "nutri_score",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "salt",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "saturated_fats",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "sugars",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "type_id",
                table: "ingredients");

            migrationBuilder.RenameColumn(
                name: "amount_in_grams",
                table: "ingredient_units",
                newName: "to_default_unit");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "ingredients",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "fk_ingredient_units_ingredients_ingredient_id",
                table: "ingredient_units",
                column: "ingredient_id",
                principalTable: "ingredients",
                principalColumn: "id");
        }
    }
}
