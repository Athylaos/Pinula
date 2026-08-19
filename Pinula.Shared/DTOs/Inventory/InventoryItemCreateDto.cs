namespace Pinula.Shared.DTOs;

public class InventoryItemCreateDto
{
    public Guid IngredientId { get; set; }
    public Guid UnitId { get; set; }
    
    public decimal Quantity { get; set; }
    public decimal QuantityInGrams { get; set; }
    
    public DateTime? ExpirationDate { get; set; }
    public decimal AllocatedQuantityInGrams { get; set; } = 0;
}