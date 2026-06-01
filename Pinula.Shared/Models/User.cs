using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Pinula.Shared.Models;

public partial class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public required string Surname { get; set; }
    public DateTime UserCreated { get; set; }
    public required string Role { get; set; } = "user";
    public string? AvatarUrl { get; set; }
    public required string PasswordHash { get; set; }
    public bool CanComment { get; set; } = true;
    public bool CanCreateRecipes { get; set; } = true;
    public Guid? GroupId { get; set; }


    [JsonIgnore]
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    [JsonIgnore]
    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    [JsonIgnore]
    public virtual ICollection<RecipeUser> RecipeUsers { get; set; } = new List<RecipeUser>();
    [JsonIgnore]
    public Group? Group { get; set; }
    [JsonIgnore]
    public ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();
}
