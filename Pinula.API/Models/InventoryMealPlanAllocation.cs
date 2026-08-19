namespace Pinula.API.Models;

public class InventoryMealPlanAllocation
{
    public Guid Id { get; set; }

    public Guid InventoryItemId { get; set; }
    public virtual InventoryItem InventoryItem { get; set; } = null!;

    public Guid MealPlanIngredientId { get; set; }
    public virtual MealPlanIngredient MealPlanIngredient { get; set; } = null!;

    public decimal AllocatedQuantityInGrams { get; set; }
    public DateTime AllocatedAt { get; set; } = DateTime.UtcNow;
}