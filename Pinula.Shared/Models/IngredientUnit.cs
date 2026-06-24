using System;
using System.Collections.Generic;

namespace Pinula.Shared.Models;

public partial class IngredientUnit
{
    public Guid UnitId { get; set; }

    public Guid IngredientId { get; set; }

    public decimal AmountInGrams { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}
