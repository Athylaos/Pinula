namespace Pinula.Shared.DTOs;

public class ShoppingItemCreateDto
{
    public Guid IngredientId { get; set; }
    public Guid UnitId { get; set; }
    
    public decimal Quantity { get; set; }
    public decimal QuantityInGrams { get; set; }
    
    public Guid? ShoppingCategoryId { get; set; }
    
}