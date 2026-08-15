using Pinula.Shared.DTOs;

namespace Pinula.Shared.Interface;

public interface IInventoryService
{
    public Task<List<InventoryItemDisplayDto>> GetAllInventoryItemsAsync();
    public Task<bool> CreateInventoryItemAsync(InventoryItemCreateDto dto);
    public Task<bool> UpdateInventoryItemAsync(InventoryItemUpdateDto dto);
    public Task<bool> DeleteInventoryItemAsync(Guid id);

    public Task<List<ShoppingItemDisplayDto>> GetAllShoppingItemsAsync();
    public Task<bool> CreateShoppingItemAsync(ShoppingItemCreateDto dto);
    public Task<bool> DeleteShoppingItemAsync(Guid id);
}