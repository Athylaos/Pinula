namespace Pinual.API.Models;

public class InventoryItem
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid IngredientId { get; set; }
    public Guid UnitId { get; set; }
    
    public decimal Quantity { get; set; }
    public decimal QuantityInGrams { get; set; }
    
    public DateTime? ExpirationDate { get; set; }
    public bool IsAllocated { get; set; } = false;

    public virtual Group Group { get; set; }
    public virtual Ingredient Ingredient { get; set; }
    public virtual Unit Unit { get; set; }
    
}