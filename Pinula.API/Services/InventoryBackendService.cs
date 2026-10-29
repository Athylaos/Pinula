using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Context;
using Pinula.API.Interface;
using Pinula.API.Models;
using Pinula.Shared.DTOs;

namespace Pinula.API.Services;

public class InventoryBackendService : IInventoryBackendService
{
    private readonly PinulaDbContext _db;
    private readonly ILogger<InventoryBackendService> _logger;

    public InventoryBackendService(PinulaDbContext db, ILogger<InventoryBackendService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(IResult AuthResult, Guid GroupId)> ValidateAuthUserInGroup(ClaimsPrincipal user)
    {
        var (authResult, _, groupDb) = await HelperFunctions.AuthUserInGroup(user, _db);
        return (authResult, groupDb?.Id ?? Guid.Empty);
    }
    

    public async Task<List<InventoryItemDisplayDto>> GetAllInventoryItemsAsync(Guid groupId, string baseUrl, string lang)
    {
        var items = await _db.InventoryItems
            .Where(i => i.GroupId == groupId)
            .Include(i => i.Ingredient).ThenInclude(i => i.ShoppingCategory)
            .Include(i => i.Ingredient).ThenInclude(i => i.BaseIngredient)
            .Include(i => i.Ingredient).ThenInclude(i => i.IngredientUnits).ThenInclude(i => i.Unit)
            .Include(i => i.Unit)
            .Include(i => i.Allocations)
            .AsNoTracking()
            .ToListAsync();

        return items.AdaptWithLangAndBaseUrl<List<InventoryItemDisplayDto>>(baseUrl, lang);
    }

    public async Task<GeneralResponse> CreateInventoryItemAsync(Guid groupId, InventoryItemCreateDto dto)
    {
        if (!await _db.Ingredients.AnyAsync(i => i.Id == dto.IngredientId)) 
            return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Ingredient not found" };
            
        if (!await _db.Units.AnyAsync(i => i.Id == dto.UnitId)) 
            return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Unit not found" };
            
        if (dto.QuantityInGrams <= 0 || dto.Quantity <= 0) 
            return new GeneralResponse { Successful = false, StatusCode = 400, Message = "Quantity can't be 0" };

        var newItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            IngredientId = dto.IngredientId,
            UnitId = dto.UnitId,
            Quantity = dto.Quantity,
            QuantityInGrams = dto.QuantityInGrams,
            ExpirationDate = dto.ExpirationDate.HasValue ? DateTime.SpecifyKind(dto.ExpirationDate.Value, DateTimeKind.Utc) : null
        };

        _db.InventoryItems.Add(newItem);
        await _db.SaveChangesAsync();

        return new GeneralResponse { Successful = true, StatusCode = 201, Id = newItem.Id, Message = "Inventory item created successfully" };
    }

    public async Task<GeneralResponse> UpdateInventoryItemAsync(Guid groupId, InventoryItemUpdateDto dto)
    {
        var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.Id == dto.Id);
        if (item is null) 
            return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Inventory item not found" };
            
        if (dto.Quantity <= 0) 
            return new GeneralResponse { Successful = false, StatusCode = 400, Message = "Quantity can't be 0" };

        if (dto.Quantity.HasValue)
        {
            var conversionFactor = item.QuantityInGrams / item.Quantity;
            item.Quantity = dto.Quantity.Value;
            item.QuantityInGrams = dto.Quantity.Value * conversionFactor;
        }
        
        if (dto.ExpirationDate.HasValue) 
        {
            item.ExpirationDate = DateTime.SpecifyKind(dto.ExpirationDate.Value, DateTimeKind.Utc);
        }

        await _db.SaveChangesAsync();
        return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Inventory item updated successfully" };
    }

    public async Task<GeneralResponse> DeleteInventoryItemAsync(Guid groupId, Guid id)
    {
        var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null) 
            return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Inventory item not found" };

        _db.InventoryItems.Remove(item);
        _db.InventoryMealPlanAllocations.RemoveRange(await _db.InventoryMealPlanAllocations.Where(a => a.InventoryItemId == item.Id).ToListAsync());
        
        await _db.SaveChangesAsync();
        return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Inventory item deleted successfully" };
    }   

    public async Task<List<ShoppingItemDisplayDto>> GetAllShoppingListItemsAsync(Guid groupId, string baseUrl, string lang)
    {
        var items = await _db.ShoppingListItems
            .Where(i => i.GroupId == groupId)
            .Include(i => i.Ingredient).ThenInclude(i => i.ShoppingCategory)
            .Include(i => i.Ingredient).ThenInclude(i => i.BaseIngredient)
            .Include(i => i.Unit)
            .Include(i => i.ShoppingCategory)
            .Include(i => i.MealPlanIngredient).ThenInclude(mpi => mpi.MealPlan).ThenInclude(mp => mp.Recipe)
            .AsNoTracking()
            .ToListAsync();

        return items.AdaptWithLangAndBaseUrl<List<ShoppingItemDisplayDto>>(baseUrl, lang);
    }

    public async Task<GeneralResponse> CreateShoppingListItemAsync(Guid groupId, ShoppingItemCreateDto dto)
    {
        var ingredient = await _db.Ingredients.AsNoTracking().FirstOrDefaultAsync(i => i.Id == dto.IngredientId);
        if (ingredient is null) 
            return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Ingredient not found" };
            
        if (!await _db.Units.AnyAsync(i => i.Id == dto.UnitId)) 
            return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Unit not found" };
            
        if (dto.QuantityInGrams <= 0 || dto.Quantity <= 0) 
            return new GeneralResponse { Successful = false, StatusCode = 400, Message = "Quantity can't be 0" };

        var shoppingCategoryId = dto.ShoppingCategoryId ?? ingredient.ShoppingCategoryId;

        var newItem = new ShoppingListItem
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            IngredientId = dto.IngredientId,
            UnitId = dto.UnitId,
            Quantity = dto.Quantity,
            QuantityInGrams = dto.QuantityInGrams,
            ShoppingCategoryId = shoppingCategoryId,
            IsPurchased = false
        };

        _db.ShoppingListItems.Add(newItem);
        await _db.SaveChangesAsync();

        return new GeneralResponse { Successful = true, StatusCode = 201, Id = newItem.Id, Message = "Shopping item created successfully" };
    }

    public async Task<GeneralResponse> DeleteShoppingListItemAsync(Guid groupId, Guid id)
    {
        var item = await _db.ShoppingListItems.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null) 
            return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Shopping list item not found" };

        _db.ShoppingListItems.Remove(item);
        await _db.SaveChangesAsync();
        
        return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Shopping item deleted successfully" };
    }

    public async Task<GeneralResponse> ToggleShoppingListItemPurchasedAsync(Guid groupId, Guid id)
    {
        var item = await _db.ShoppingListItems.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null) 
            return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Shopping list item not found" };

        item.IsPurchased = !item.IsPurchased;
        await _db.SaveChangesAsync();
        
        return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Shopping item status toggled successfully" };
    }

    public async Task<GeneralResponse> ConvertShoppingItemsToPantryAsync(Guid groupId, List<InventoryItemCreateDto> inventoryItems)
    {
        var shoppingItemIds = inventoryItems
            .Where(i => i.ShoppingListItemId.HasValue)
            .Select(i => i.ShoppingListItemId!.Value)
            .ToList();
        
        var shoppingItemsDb = await _db.ShoppingListItems
            .Where(i => i.GroupId == groupId && shoppingItemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id);
        
        foreach (var item in inventoryItems)
        {
            if (!item.ShoppingListItemId.HasValue || !shoppingItemsDb.TryGetValue(item.ShoppingListItemId.Value, out var itemDb))
            {
                continue;
            }

            var itemId = Guid.NewGuid();

            var newInventoryItem = new InventoryItem
            {
                Id = itemId,
                GroupId = groupId,
                IngredientId = item.IngredientId,
                UnitId = item.UnitId,
                ExpirationDate = item.ExpirationDate.HasValue ? DateTime.SpecifyKind(item.ExpirationDate.Value, DateTimeKind.Utc) : null,
                Quantity = item.Quantity,
                QuantityInGrams = item.QuantityInGrams,
            };
            
            _db.InventoryItems.Add(newInventoryItem);

            if (itemDb.MealPlanIngredientId.HasValue)
            {
                _db.InventoryMealPlanAllocations.Add(new InventoryMealPlanAllocation
                {
                    Id = Guid.NewGuid(),
                    InventoryItemId = itemId,
                    MealPlanIngredientId = itemDb.MealPlanIngredientId.Value,
                    AllocatedQuantityInGrams = item.QuantityInGrams
                });
            }

            _db.ShoppingListItems.Remove(itemDb);
        }



        await _db.SaveChangesAsync();
        return new GeneralResponse { Successful = true, StatusCode = 201, Message = "Items successfully moved to pantry" };
    }
}