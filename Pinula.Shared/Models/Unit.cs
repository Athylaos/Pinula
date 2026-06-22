using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Pinula.Shared.Models;

public partial class Unit
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Dictionary<string, string> Names { get; set; } = new();

    public bool IsServingUnit { get; set; }
    [JsonIgnore]
    public virtual ICollection<IngredientUnit> IngredientUnits { get; set; } = new List<IngredientUnit>();
    [JsonIgnore]
    public virtual ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
    [JsonIgnore]
    public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    [JsonIgnore]
    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}
