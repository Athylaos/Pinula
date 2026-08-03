using System;
using System.Collections.Generic;

namespace Pinual.API.Models;

public partial class Ingredient
{
    public Guid Id { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsApproved { get; set; }
    public bool Checked { get; set; } = false;
    public DateTime IngredientCreated { get; set; }
    public Guid UserId { get; set; }

    public Dictionary<string, string> Names { get; set; } = new();
    public Guid DefaultUnitId { get; set; }
    public Guid ShoppingCategoryId { get; set; }
    public Guid? BaseIngredientId { get; set; }
    public string? OffCategoryTag { get; set; }
    public decimal? EdibleRatio { get; set; }

    public decimal Calories { get; set; }
    public decimal Proteins { get; set; }
    public decimal Fats { get; set; }
    public decimal SaturatedFats { get; set; }
    public decimal Carbohydrates { get; set; }
    public decimal Sugars { get; set; }
    public decimal Fiber { get; set; }
    public decimal Salt { get; set; }

    public bool IsVegan { get; set; }
    public bool IsVegetarian { get; set; }
    public bool IsGlutenFree { get; set; }
    public bool IsLactoseFree { get; set; }

    public string? Barcode { get; set; }
    public string? ImageUrl { get; set; } 

    public string? NutriScore { get; set; }
    public int? NovaClassification { get; set; }

    public virtual Unit DefaultUnit { get; set; } = null!;
    public virtual User Creator { get; set; } = null!;
    public virtual ShoppingCategory ShoppingCategory { get; set; } = null!;
    public virtual Ingredient? BaseIngredient { get; set; }
    public virtual ICollection<Ingredient> BrandedProducts { get; set; } = new List<Ingredient>();
    public virtual ICollection<IngredientUnit> IngredientUnits { get; set; } = new List<IngredientUnit>();
    public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    public virtual ICollection<MealPlanIngredient> MealPlanIngredients { get; set; } = new List<MealPlanIngredient>();
}
