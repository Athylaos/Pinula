using System;
using System.Collections.Generic;

namespace Pinula.Shared.Models;

public partial class Recipe
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = null!;

    public string PhotoUrl { get; set; } = null!;

    public short CookingTime { get; set; }

    public short ServingsAmount { get; set; }

    public Guid ServingUnit { get; set; }

    public short Difficulty { get; set; }

    public decimal? Calories { get; set; }

    public decimal? Proteins { get; set; }

    public decimal? Fats { get; set; }

    public decimal? Carbohydrates { get; set; }

    public decimal? Fiber { get; set; }

    public DateTime RecipeCreated { get; set; }

    public decimal? Rating { get; set; }

    public int? UsersRated { get; set; }

    public bool IsApproved { get; set; } = true;

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

    public virtual ICollection<RecipeStep> RecipeSteps { get; set; } = new List<RecipeStep>();

    public virtual ICollection<RecipesUser> RecipesUsers { get; set; } = new List<RecipesUser>();

    public virtual Unit ServingUnitNavigation { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

}
