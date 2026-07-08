using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinula.API.Migrations
{
    /// <inheritdoc />
    public partial class IngredientCreatorAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "ingredients",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE ingredients SET user_id = 'e9b6415d-6b09-48de-86ee-03754dba3ae1' WHERE user_id IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "ingredients",
                type: "uuid",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "ix_ingredients_user_id",
                table: "ingredients",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ingredients_users_user_id",
                table: "ingredients",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ingredients_users_user_id",
                table: "ingredients");

            migrationBuilder.DropIndex(
                name: "ix_ingredients_user_id",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "ingredients");
        }
    }
}
