using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Pinula.Shared.Models;

public partial class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Surname { get; set; } = null!;

    public DateTime UserCreated { get; set; }

    public string? Role { get; set; }

    public string? AvatarUrl { get; set; }

    public string PasswordHash { get; set; } = null!;

    public bool CanComment { get; set; } = true;

    public bool CanCreateRecipes { get; set; } = true;
    [JsonIgnore]
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    [JsonIgnore]
    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    [JsonIgnore]
    public virtual ICollection<RecipesUser> RecipesUsers { get; set; } = new List<RecipesUser>();
}
