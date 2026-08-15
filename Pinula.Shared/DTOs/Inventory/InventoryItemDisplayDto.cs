namespace Pinula.Shared.DTOs;

public class InventoryItemDisplayDto
{
    public Guid Id { get; set; }
    public IngredientPreviewDto Ingredient { get; set; }
    public UnitPreviewDto Unit { get; set; }
    
    public decimal Quantity { get; set; }
    public decimal QuantityInGrams { get; set; }
    public ShoppingCategoryDisplayDto ShoppingCategory { get; set; } = new();
    public IngredientPreviewDto? BaseIngredient { get; set; }
    
    public DateTime? ExpirationDate { get; set; }
    public bool IsAllocated { get; set; } = false;
}