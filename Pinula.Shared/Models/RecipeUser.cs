using System;
using System.Collections.Generic;

namespace Pinula.Shared.Models;

public partial class RecipeUser
{
    public Guid RecipeId { get; set; }

    public Guid UserId { get; set; }

    public bool IsFavorite { get; set; }

    public virtual Recipe Recipes { get; set; } = null!;

    public virtual User Users { get; set; } = null!;
}
