namespace Pinula.API.Models;

public class ShoppingListItem
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid IngredientId { get; set; }
    public Guid UnitId { get; set; }

    public decimal Quantity { get; set; }
    public decimal QuantityInGrams { get; set; }
    public Guid? ShoppingCategoryId { get; set; }
    public bool IsPurchased { get; set; }

    public Guid? MealPlanIngredientId { get; set; }
    public MealPlanIngredient MealPlanIngredient { get; set; } = new();

    public virtual Group Group { get; set; } = null!;
    public virtual Ingredient Ingredient { get; set; } = null!;
    public virtual Unit Unit { get; set; } = null!;
    public virtual ShoppingCategory ShoppingCategory { get; set; } = null!;
}
