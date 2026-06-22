using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinula.API.Migrations
{
    /// <inheritdoc />
    public partial class MigrateNamesToDictionary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE units ADD COLUMN IF NOT EXISTS names jsonb NULL;");
            migrationBuilder.Sql("ALTER TABLE recipes ADD COLUMN IF NOT EXISTS titles jsonb NULL;");
            migrationBuilder.Sql("ALTER TABLE recipe_steps ADD COLUMN IF NOT EXISTS descriptions jsonb NULL;");
            migrationBuilder.Sql("ALTER TABLE categories ADD COLUMN IF NOT EXISTS names jsonb NULL;");
            migrationBuilder.Sql("ALTER TABLE comments ADD COLUMN IF NOT EXISTS language_code text NULL;");

            migrationBuilder.Sql("UPDATE categories SET names = json_build_object('en', name) WHERE name IS NOT NULL;");
            migrationBuilder.Sql("UPDATE recipe_steps SET descriptions = json_build_object('en', description) WHERE description IS NOT NULL;");
            migrationBuilder.Sql("UPDATE recipes SET titles = json_build_object('en', title) WHERE title IS NOT NULL;");
            migrationBuilder.Sql("UPDATE units SET names = json_build_object('en', name) WHERE name IS NOT NULL;");
            migrationBuilder.Sql("UPDATE comments SET language_code = 'en';");

            migrationBuilder.Sql("UPDATE categories SET names = '{}'::jsonb WHERE names IS NULL;");
            migrationBuilder.Sql("UPDATE recipe_steps SET descriptions = '{}'::jsonb WHERE descriptions IS NULL;");
            migrationBuilder.Sql("UPDATE recipes SET titles = '{}'::jsonb WHERE titles IS NULL;");
            migrationBuilder.Sql("UPDATE units SET names = '{}'::jsonb WHERE names IS NULL;");

            migrationBuilder.Sql("ALTER TABLE units ALTER COLUMN names SET NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE recipes ALTER COLUMN titles SET NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE recipe_steps ALTER COLUMN descriptions SET NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE categories ALTER COLUMN names SET NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE comments ALTER COLUMN language_code SET NOT NULL;");

            migrationBuilder.Sql("ALTER TABLE recipe_steps DROP COLUMN IF EXISTS description;");
            migrationBuilder.Sql("ALTER TABLE categories DROP COLUMN IF EXISTS name;");

            migrationBuilder.Sql("ALTER TABLE recipes RENAME COLUMN title TO original_language;");
            migrationBuilder.Sql("UPDATE recipes SET original_language = 'en';");
            migrationBuilder.Sql("ALTER TABLE recipes ALTER COLUMN original_language SET NOT NULL;");

            migrationBuilder.Sql("ALTER TABLE units RENAME COLUMN name TO code;");
            migrationBuilder.Sql("ALTER TABLE units ALTER COLUMN code SET NOT NULL;");

            migrationBuilder.Sql("ALTER INDEX IF EXISTS ix_units_name RENAME TO ix_units_code;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "names",
                table: "units");

            migrationBuilder.DropColumn(
                name: "titles",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "descriptions",
                table: "recipe_steps");

            migrationBuilder.DropColumn(
                name: "language_code",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "names",
                table: "categories");

            migrationBuilder.RenameColumn(
                name: "code",
                table: "units",
                newName: "name");

            migrationBuilder.RenameIndex(
                name: "ix_units_code",
                table: "units",
                newName: "ix_units_name");

            migrationBuilder.RenameColumn(
                name: "original_language",
                table: "recipes",
                newName: "title");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "recipe_steps",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "categories",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
