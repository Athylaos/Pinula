using System;
using System.Collections.Generic;
using Pinula.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace Pinula.API.Context;

public partial class CookRecipesDbContext : DbContext
{
    public CookRecipesDbContext()
    {
    }

    public CookRecipesDbContext(DbContextOptions<CookRecipesDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<IngredientUnit> IngredientUnits { get; set; }

    public virtual DbSet<Recipe> Recipes { get; set; }

    public virtual DbSet<RecipeIngredient> RecipeIngredients { get; set; }

    public virtual DbSet<RecipeStep> RecipeSteps { get; set; }

    public virtual DbSet<RecipesUser> RecipesUsers { get; set; }

    public virtual DbSet<Unit> Units { get; set; }

    public virtual DbSet<User> Users { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("categories_pkey");

            entity.ToTable("categories");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.ParentCategory).HasColumnName("parent_category");
            entity.Property(e => e.PictureUrl)
                .HasDefaultValueSql("'default_category_picture.png'::text")
                .HasColumnName("picture_url");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");

            entity.HasOne(d => d.ParentCategoryNavigation).WithMany(p => p.ChildCategories)
                .HasForeignKey(d => d.ParentCategory)
                .HasConstraintName("categories_parent_category_fkey");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("comments_pkey");

            entity.ToTable("comments");

            entity.Property(e => e.RecipeId).HasColumnName("recipe_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.Text).HasColumnName("text");
            entity.Property(e => e.IsApproved).HasDefaultValue(true).HasColumnName("is_approved");

            entity.HasOne(d => d.ParentComment)
                .WithMany(p => p.Replies)
                .HasForeignKey(d => d.ParentCommentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("comments_parent_fkey");

            entity.HasOne(d => d.Recipe).WithMany(p => p.Comments)
                .HasForeignKey(d => d.RecipeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("comments_recipe_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("comments_user_id_fkey");
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ingredients_pkey");

            entity.ToTable("ingredients");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Calories)
                .HasPrecision(10, 3)
                .HasColumnName("calories");
            entity.Property(e => e.Carbohydrates)
                .HasPrecision(10, 3)
                .HasColumnName("carbohydrates");
            entity.Property(e => e.DefaultUnit).HasColumnName("default_unit");
            entity.Property(e => e.Fats)
                .HasPrecision(10, 3)
                .HasColumnName("fats");
            entity.Property(e => e.Fiber)
                .HasPrecision(10, 3)
                .HasColumnName("fiber");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Proteins)
                .HasPrecision(10, 3)
                .HasColumnName("proteins");

            entity.HasOne(d => d.DefaultUnitNavigation).WithMany(p => p.Ingredients)
                .HasForeignKey(d => d.DefaultUnit)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ingredients_default_unit_fkey");
        });

        modelBuilder.Entity<IngredientUnit>(entity =>
        {
            entity.HasKey(e => new { e.UnitId, e.IngredientId }).HasName("ingredientUnits_pkey");

            entity.ToTable("ingredientUnits");

            entity.Property(e => e.UnitId).HasColumnName("unit_id");
            entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
            entity.Property(e => e.ToDefaultUnit)
                .HasPrecision(12, 6)
                .HasColumnName("to_default_unit");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.IngredientUnits)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ingredientUnits_ingredient_id_fkey");

            entity.HasOne(d => d.Unit).WithMany(p => p.IngredientUnits)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ingredientUnits_unit_id_fkey");
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("recipes_pkey");

            entity.ToTable("recipes");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Calories)
                .HasPrecision(10, 3)
                .HasColumnName("calories");
            entity.Property(e => e.Carbohydrates)
                .HasPrecision(10, 3)
                .HasColumnName("carbohydrates");
            entity.Property(e => e.CookingTime).HasColumnName("cooking_time");
            entity.Property(e => e.Difficulty).HasColumnName("difficulty");
            entity.Property(e => e.Fats)
                .HasPrecision(10, 3)
                .HasColumnName("fats");
            entity.Property(e => e.Fiber)
                .HasPrecision(10, 3)
                .HasColumnName("fiber");
            entity.Property(e => e.PhotoUrl)
                .HasDefaultValueSql("'default_recipe_picture.png'::text")
                .HasColumnName("photo_url");
            entity.Property(e => e.Proteins)
                .HasPrecision(10, 3)
                .HasColumnName("proteins");
            entity.Property(e => e.Rating)
                .HasPrecision(3, 2)
                .HasColumnName("rating");
            entity.Property(e => e.RecipeCreated)
                .HasPrecision(0)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("recipe_created");
            entity.Property(e => e.ServingUnit).HasColumnName("serving_unit");
            entity.Property(e => e.ServingsAmount).HasColumnName("servings_amount");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.UsersRated).HasColumnName("users_rated");
            entity.Property(e => e.IsApproved).HasDefaultValue(true).HasColumnName("is_approved");

            entity.HasOne(d => d.ServingUnitNavigation).WithMany(p => p.Recipes)
                .HasForeignKey(d => d.ServingUnit)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("recipes_serving_unit_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Recipes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("recipes_user_id_fkey");

            entity.HasMany(d => d.Categories).WithMany(p => p.Recipes)
                .UsingEntity<Dictionary<string, object>>(
                    "RecipeCategory",
                    r => r.HasOne<Category>().WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("recipeCategories_category_id_fkey"),
                    l => l.HasOne<Recipe>().WithMany()
                        .HasForeignKey("RecipeId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("recipeCategories_recipe_id_fkey"),
                    j =>
                    {
                        j.HasKey("RecipeId", "CategoryId").HasName("recipeCategories_pkey");
                        j.ToTable("recipeCategories");
                        j.IndexerProperty<Guid>("RecipeId").HasColumnName("recipe_id");
                        j.IndexerProperty<Guid>("CategoryId").HasColumnName("category_id");
                    });
        });

        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.HasKey(e => new { e.RecipeId, e.IngredientId }).HasName("recipeIngredients_pkey");

            entity.ToTable("recipeIngredients");

            entity.Property(e => e.RecipeId).HasColumnName("recipe_id");
            entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
            entity.Property(e => e.ConversionFactor)
                .HasPrecision(12, 6)
                .HasColumnName("conversion_factor");
            entity.Property(e => e.Quantity)
                .HasPrecision(10, 3)
                .HasColumnName("quantity");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.RecipeIngredients)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("recipeIngredients_ingredient_id_fkey");

            entity.HasOne(d => d.Recipe).WithMany(p => p.RecipeIngredients)
                .HasForeignKey(d => d.RecipeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("recipeIngredients_recipe_id_fkey");

            entity.HasOne(d => d.Unit).WithMany(p => p.RecipeIngredients)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("recipeIngredients_unit_id_fkey");
        });

        modelBuilder.Entity<RecipeStep>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("recipeSteps_pkey");

            entity.ToTable("recipeSteps");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.RecipeId).HasColumnName("recipe_id");
            entity.Property(e => e.StepNumber)
                .HasDefaultValue((short)1)
                .HasColumnName("step_number");

            entity.HasOne(d => d.Recipe).WithMany(p => p.RecipeSteps)
                .HasForeignKey(d => d.RecipeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("recipeSteps_recipe_id_fkey");
        });

        modelBuilder.Entity<RecipesUser>(entity =>
        {
            entity.HasKey(e => new { e.RecipesId, e.UsersId }).HasName("recipes_users_pkey");

            entity.ToTable("recipes_users");

            entity.Property(e => e.RecipesId).HasColumnName("recipes_id");
            entity.Property(e => e.UsersId).HasColumnName("users_id");
            entity.Property(e => e.IsFavorite).HasColumnName("isFavorite");

            entity.HasOne(d => d.Recipes).WithMany(p => p.RecipesUsers)
                .HasForeignKey(d => d.RecipesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("recipes_users_recipes_id_fkey");

            entity.HasOne(d => d.Users).WithMany(p => p.RecipesUsers)
                .HasForeignKey(d => d.UsersId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("recipes_users_users_id_fkey");
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("units_pkey");

            entity.ToTable("units");

            entity.HasIndex(e => e.Name, "units_name_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.IsServingUnit).HasColumnName("is_serving_unit");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.AvatarUrl)
                .HasDefaultValueSql("'default_avatar.png'::text")
                .HasColumnName("avatar_url");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.Role)
                .HasDefaultValueSql("'user'::text")
                .HasColumnName("role");
            entity.Property(e => e.Surname).HasColumnName("surname");
            entity.Property(e => e.UserCreated)
                .HasPrecision(0)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("user_created");
            entity.Property(e => e.CanComment).HasDefaultValue(true).HasColumnName("can_comment");
            entity.Property(e => e.CanCreateRecipes).HasDefaultValue(true).HasColumnName("can_create_recipes");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
