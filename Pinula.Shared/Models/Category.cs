using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Pinula.Shared.Models;

public partial class Category
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string PictureUrl { get; set; } = null!;

    public short SortOrder { get; set; }

    public Guid? ParentCategory { get; set; }

    public virtual ICollection<Category> ChildCategories { get; set; } = new List<Category>();

    [JsonIgnore]
    public virtual Category? ParentCategoryNavigation { get; set; }
    [JsonIgnore]

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}
