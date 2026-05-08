using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinula.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    picture_url = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'default_category_picture.png'::text"),
                    sort_order = table.Column<short>(type: "smallint", nullable: false),
                    parent_category = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("categories_pkey", x => x.id);
                    table.ForeignKey(
                        name: "categories_parent_category_fkey",
                        column: x => x.parent_category,
                        principalTable: "categories",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_serving_unit = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("units_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    surname = table.Column<string>(type: "text", nullable: false),
                    user_created = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    role = table.Column<string>(type: "text", nullable: true, defaultValueSql: "'user'::text"),
                    avatar_url = table.Column<string>(type: "text", nullable: true, defaultValueSql: "'default_avatar.png'::text"),
                    password_hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ingredients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    default_unit = table.Column<Guid>(type: "uuid", nullable: false),
                    calories = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    proteins = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    fats = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    carbohydrates = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    fiber = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ingredients_pkey", x => x.id);
                    table.ForeignKey(
                        name: "ingredients_default_unit_fkey",
                        column: x => x.default_unit,
                        principalTable: "units",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "recipes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    photo_url = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'default_recipe_picture.png'::text"),
                    cooking_time = table.Column<short>(type: "smallint", nullable: false),
                    servings_amount = table.Column<short>(type: "smallint", nullable: false),
                    serving_unit = table.Column<Guid>(type: "uuid", nullable: false),
                    difficulty = table.Column<short>(type: "smallint", nullable: false),
                    calories = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    proteins = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    fats = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    carbohydrates = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    fiber = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    recipe_created = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    users_rated = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("recipes_pkey", x => x.id);
                    table.ForeignKey(
                        name: "recipes_serving_unit_fkey",
                        column: x => x.serving_unit,
                        principalTable: "units",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "recipes_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ingredientUnits",
                columns: table => new
                {
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_default_unit = table.Column<decimal>(type: "numeric(12,6)", precision: 12, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ingredientUnits_pkey", x => new { x.unit_id, x.ingredient_id });
                    table.ForeignKey(
                        name: "ingredientUnits_ingredient_id_fkey",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "ingredientUnits_unit_id_fkey",
                        column: x => x.unit_id,
                        principalTable: "units",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "text", nullable: true),
                    rating = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("comments_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "comments_parent_fkey",
                        column: x => x.ParentCommentId,
                        principalTable: "comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "comments_recipe_id_fkey",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "comments_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "recipeCategories",
                columns: table => new
                {
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("recipeCategories_pkey", x => new { x.recipe_id, x.category_id });
                    table.ForeignKey(
                        name: "recipeCategories_category_id_fkey",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "recipeCategories_recipe_id_fkey",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "recipeIngredients",
                columns: table => new
                {
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversion_factor = table.Column<decimal>(type: "numeric(12,6)", precision: 12, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("recipeIngredients_pkey", x => new { x.recipe_id, x.ingredient_id });
                    table.ForeignKey(
                        name: "recipeIngredients_ingredient_id_fkey",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "recipeIngredients_recipe_id_fkey",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "recipeIngredients_unit_id_fkey",
                        column: x => x.unit_id,
                        principalTable: "units",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "recipes_users",
                columns: table => new
                {
                    recipes_id = table.Column<Guid>(type: "uuid", nullable: false),
                    users_id = table.Column<Guid>(type: "uuid", nullable: false),
                    isFavorite = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("recipes_users_pkey", x => new { x.recipes_id, x.users_id });
                    table.ForeignKey(
                        name: "recipes_users_recipes_id_fkey",
                        column: x => x.recipes_id,
                        principalTable: "recipes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "recipes_users_users_id_fkey",
                        column: x => x.users_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "recipeSteps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    step_number = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("recipeSteps_pkey", x => x.id);
                    table.ForeignKey(
                        name: "recipeSteps_recipe_id_fkey",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_categories_parent_category",
                table: "categories",
                column: "parent_category");

            migrationBuilder.CreateIndex(
                name: "IX_comments_ParentCommentId",
                table: "comments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_comments_recipe_id",
                table: "comments",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "IX_comments_user_id",
                table: "comments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ingredients_default_unit",
                table: "ingredients",
                column: "default_unit");

            migrationBuilder.CreateIndex(
                name: "IX_ingredientUnits_ingredient_id",
                table: "ingredientUnits",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "IX_recipeCategories_category_id",
                table: "recipeCategories",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_recipeIngredients_ingredient_id",
                table: "recipeIngredients",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "IX_recipeIngredients_unit_id",
                table: "recipeIngredients",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_recipes_serving_unit",
                table: "recipes",
                column: "serving_unit");

            migrationBuilder.CreateIndex(
                name: "IX_recipes_user_id",
                table: "recipes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_recipes_users_users_id",
                table: "recipes_users",
                column: "users_id");

            migrationBuilder.CreateIndex(
                name: "IX_recipeSteps_recipe_id",
                table: "recipeSteps",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "units_name_key",
                table: "units",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_email_key",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comments");

            migrationBuilder.DropTable(
                name: "ingredientUnits");

            migrationBuilder.DropTable(
                name: "recipeCategories");

            migrationBuilder.DropTable(
                name: "recipeIngredients");

            migrationBuilder.DropTable(
                name: "recipes_users");

            migrationBuilder.DropTable(
                name: "recipeSteps");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "ingredients");

            migrationBuilder.DropTable(
                name: "recipes");

            migrationBuilder.DropTable(
                name: "units");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
