namespace Pinula.Shared.DTOs.ShoppingList;

public class ShoppingListItemDisplayDto
{
    public Guid Id { get; set; }
    public IngredientPreviewDto Ingredient { get; set; }
    public UnitPreviewDto Unit { get; set; }
    
    public decimal Quantity { get; set; }
    public decimal QuantityInGrams { get; set; }
    public ShoppingCategoryDisplayDto ShoppingCategory { get; set; }
    public bool IsPurchased { get; set; }
}