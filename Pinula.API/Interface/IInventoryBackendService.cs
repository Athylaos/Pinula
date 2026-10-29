using System.Security.Claims;
using Pinula.Shared.DTOs;

namespace Pinula.API.Interface;

public interface IInventoryBackendService
{
    Task<(IResult AuthResult, Guid GroupId)> ValidateAuthUserInGroup(ClaimsPrincipal user);
    
    Task<List<InventoryItemDisplayDto>> GetAllInventoryItemsAsync(Guid groupId, string baseUrl, string lang);
    Task<GeneralResponse> CreateInventoryItemAsync(Guid groupId, InventoryItemCreateDto dto);
    Task<GeneralResponse> UpdateInventoryItemAsync(Guid groupId, InventoryItemUpdateDto dto);
    Task<GeneralResponse> DeleteInventoryItemAsync(Guid groupId, Guid id);
    
    Task<List<ShoppingItemDisplayDto>> GetAllShoppingListItemsAsync(Guid groupId, string baseUrl, string lang);
    Task<GeneralResponse> CreateShoppingListItemAsync(Guid groupId, ShoppingItemCreateDto dto);
    Task<GeneralResponse> DeleteShoppingListItemAsync(Guid groupId, Guid id);
    Task<GeneralResponse> ToggleShoppingListItemPurchasedAsync(Guid groupId, Guid id);
    
    Task<GeneralResponse> ConvertShoppingItemsToPantryAsync(Guid groupId, List<InventoryItemCreateDto> inventoryItems);
}