using System;
using System.Collections.Generic;

namespace Pinual.API.Models;

public partial class RecipeIngredient
{
    public Guid RecipeId { get; set; }

    public Guid IngredientId { get; set; }

    public decimal? Quantity { get; set; }

    public Guid UnitId { get; set; }

    public decimal? ConversionFactor { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual Recipe Recipe { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}
