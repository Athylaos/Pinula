namespace Pinula.Shared.DTOs;

public class InventoryItemUpdateDto
{
    public Guid Id { get; set; }
    
    public decimal? Quantity { get; set; }

    public DateTime? ExpirationDate { get; set; }
}