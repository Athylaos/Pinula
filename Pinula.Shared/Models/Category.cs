using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Pinula.Shared.Models;

public partial class Category
{
    public Guid Id { get; set; }

    public required Dictionary<string, string> Names { get; set; } = new();

    public required string PictureUrl { get; set; } = "default_category_picture.png";

    public short SortOrder { get; set; }

    public Guid? ParentCategoryId { get; set; }

    public virtual ICollection<Category> ChildCategories { get; set; } = new List<Category>();

    [JsonIgnore]
    public virtual Category? ParentCategory { get; set; }
    [JsonIgnore]
    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}
