namespace Pinula.API.Models;

public class MealPlanIngredient
{
    public Guid MealPlanId { get; set; }

    public Guid IngredientId { get; set; }

    public decimal? Quantity { get; set; }

    public Guid UnitId { get; set; }

    public decimal? ConversionFactor { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual MealPlan MealPlan { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}