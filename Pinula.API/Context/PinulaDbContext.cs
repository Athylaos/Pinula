using System;
using System.Collections.Generic;
using Pinula.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Internal;

namespace Pinula.API.Context;

public partial class PinulaDbContext : DbContext
{
    public PinulaDbContext()
    {
    }

    public PinulaDbContext(DbContextOptions<PinulaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<IngredientUnit> IngredientUnits { get; set; }

    public virtual DbSet<ShoppingCategory> ShoppingCategories { get; set; }

    public virtual DbSet<Recipe> Recipes { get; set; }

    public virtual DbSet<RecipeIngredient> RecipeIngredients { get; set; }

    public virtual DbSet<RecipeStep> RecipeSteps { get; set; }

    public virtual DbSet<RecipeUser> RecipeUsers { get; set; }

    public virtual DbSet<Unit> Units { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<MealPlan> MealPlans { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(e => e.Names).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.PictureUrl).HasDefaultValue("default_category_picture.png");

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.ChildCategories).HasForeignKey(d => d.ParentCategoryId);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasPrecision(0).HasDefaultValueSql("timezone('utc', now())");
            entity.Property(e => e.IsApproved).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.IsEdited).HasDefaultValue(false);

            entity.HasOne(d => d.ParentComment).WithMany(p => p.Replies).HasForeignKey(d => d.ParentCommentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Recipe).WithMany(p => p.Comments).HasForeignKey(d => d.RecipeId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.User).WithMany(p => p.Comments).HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.Property(e => e.Names).HasColumnType("jsonb").IsRequired();

            entity.Property(e => e.EdibleRatio).HasPrecision(4, 2);

            entity.Property(e => e.Calories).HasPrecision(10, 3);
            entity.Property(e => e.Fats).HasPrecision(10, 3);
            entity.Property(e => e.SaturatedFats).HasPrecision(10, 3);
            entity.Property(e => e.Carbohydrates).HasPrecision(10, 3);
            entity.Property(e => e.Sugars).HasPrecision(10, 3);
            entity.Property(e => e.Proteins).HasPrecision(10, 3);
            entity.Property(e => e.Fiber).HasPrecision(10, 3);
            entity.Property(e => e.Salt).HasPrecision(10, 3);

            entity.Property(e => e.IsVegan).HasDefaultValue(false);
            entity.Property(e => e.IsVegetarian).HasDefaultValue(false);
            entity.Property(e => e.IsGlutenFree).HasDefaultValue(false);
            entity.Property(e => e.IsLactoseFree).HasDefaultValue(false);

            entity.HasOne(d => d.DefaultUnit).WithMany(p => p.Ingredients).HasForeignKey(d => d.DefaultUnitId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.BaseIngredient).WithMany(p => p.BrandedProducts).HasForeignKey(d => d.BaseIngredientId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.ShoppingCategory).WithMany(p => p.Ingredients).HasForeignKey(d => d.ShoppingCategoryId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<IngredientUnit>(entity =>
        {
            entity.HasKey(e => new { e.UnitId, e.IngredientId });
            entity.Property(e => e.AmountInGrams).HasPrecision(12, 6);

            entity.HasOne(d => d.Ingredient).WithMany(p => p.IngredientUnits).HasForeignKey(d => d.IngredientId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Unit).WithMany(p => p.IngredientUnits).HasForeignKey(d => d.UnitId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ShoppingCategory>(entity =>
        {
            entity.Property(e => e.Names).HasColumnType("jsonb").IsRequired();
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.Property(e => e.Titles).HasColumnType("jsonb").IsRequired();

            entity.Property(e => e.Calories).HasPrecision(10, 3);
            entity.Property(e => e.Carbohydrates).HasPrecision(10, 3);
            entity.Property(e => e.Fats).HasPrecision(10, 3);
            entity.Property(e => e.Proteins).HasPrecision(10, 3);
            entity.Property(e => e.Fiber).HasPrecision(10, 3);
            entity.Property(e => e.Rating).HasPrecision(3, 2);
            entity.Property(e => e.RecipeCreated).HasPrecision(0).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsApproved).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasOne(d => d.ServingUnit).WithMany(p => p.Recipes).HasForeignKey(d => d.ServingUnitId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.User).WithMany(p => p.Recipes).HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasMany(d => d.Categories).WithMany(p => p.Recipes);
        });

        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.HasKey(e => new { e.RecipeId, e.IngredientId });
            entity.Property(e => e.ConversionFactor).HasPrecision(12, 6);
            entity.Property(e => e.Quantity).HasPrecision(10, 3);

            entity.HasOne(d => d.Ingredient).WithMany(p => p.RecipeIngredients).HasForeignKey(d => d.IngredientId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.Recipe).WithMany(p => p.RecipeIngredients).HasForeignKey(d => d.RecipeId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.Unit).WithMany(p => p.RecipeIngredients).HasForeignKey(d => d.UnitId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<RecipeStep>(entity =>
        {
            entity.Property(e => e.Descriptions).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.StepNumber).HasDefaultValue((short)1);

            entity.HasOne(d => d.Recipe).WithMany(p => p.RecipeSteps).HasForeignKey(d => d.RecipeId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<RecipeUser>(entity =>
        {
            entity.HasKey(e => new { e.RecipeId, e.UserId }); 

            entity.HasOne(d => d.Recipes).WithMany(p => p.RecipeUsers).HasForeignKey(d => d.RecipeId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.Users).WithMany(p => p.RecipeUsers).HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Names).HasColumnType("jsonb").IsRequired();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.AvatarUrl).HasDefaultValue("default_avatar.png");
            entity.Property(e => e.Role).HasDefaultValue("user");
            entity.Property(e => e.UserCreated).HasPrecision(0).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CanComment).HasDefaultValue(true);
            entity.Property(e => e.CanCreateRecipes).HasDefaultValue(true);

            entity.HasOne(u => u.Group).WithMany(g => g.Users).HasForeignKey(u => u.GroupId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.Property(g => g.Name).IsRequired().HasMaxLength(100);
            entity.Property(g => g.InviteCode).IsRequired().HasMaxLength(25);
        });

        modelBuilder.Entity<MealPlan>(entity =>
        {
            entity.Property(mp => mp.Date).IsRequired();
            entity.Property(mp => mp.MealType).IsRequired();
            entity.Property(mp => mp.Servings).HasDefaultValue(1);

            entity.HasOne(mp => mp.Group).WithMany(g => g.MealPlans).HasForeignKey(mp => mp.GroupId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(mp => mp.Recipe).WithMany(r => r.MealPlans).HasForeignKey(mp => mp.RecipeId).OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(mp => mp.Users).WithMany(u => u.MealPlans);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
