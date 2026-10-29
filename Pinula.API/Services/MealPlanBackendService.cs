using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Mapster;
using Pinula.API;
using Pinula.API.Context;
using Pinula.API.Interface;
using Pinula.API.Models;
using Pinula.API.Services;
using Pinula.Shared.Constants;
using Pinula.Shared.DTOs;

public class MealPlanBackendService : IMealPlanBackendService
{
    private readonly PinulaDbContext _db;
    private readonly ILogger<MealPlanBackendService> _logger;

    public MealPlanBackendService(PinulaDbContext db, ILogger<MealPlanBackendService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(IResult AuthResult, Guid GroupId)> ValidateAuthUserInGroup(ClaimsPrincipal user)
    {
        var (authResult, _, groupDb) = await HelperFunctions.AuthUserInGroup(user, _db);
        return (authResult, groupDb?.Id ?? Guid.Empty);
    }

    public async Task<MealPlanPreviewDto?> GetMealPlanAsync(Guid groupId, Guid id, string baseUrl, string lang)
    {
        var mealPlanDb = await _db.MealPlans
            .AsNoTracking()
            .Include(mp => mp.Recipe)
            .Include(mp => mp.MealPlanIngredients).ThenInclude(i => i.Unit)
            .Include(mp => mp.MealPlanIngredients).ThenInclude(i => i.Ingredient)
            .Include(mp => mp.Users)
            .FirstOrDefaultAsync(mp => mp.Id == id && mp.GroupId == groupId);

        if (mealPlanDb is null || mealPlanDb.Recipe is null) return null;

        var mealplan = mealPlanDb.AdaptWithLangAndBaseUrl<MealPlanPreviewDto>(baseUrl, lang);

        if (mealplan.Ingredients != null && mealplan.Ingredients.Any())
        {
            var ingredientIds = mealplan.Ingredients.Select(i => i.IngredientId).Distinct().ToList();

            var inventoryItemsDb = await _db.InventoryItems
                .AsNoTracking()
                .Include(i => i.Ingredient)
                .Include(i => i.Allocations)
                .Where(i => i.GroupId == groupId && 
                            (ingredientIds.Contains(i.IngredientId) || 
                            (i.Ingredient.BaseIngredientId.HasValue && ingredientIds.Contains(i.Ingredient.BaseIngredientId.Value))))
                .ToListAsync();

            foreach (var ingredient in mealplan.Ingredients)
            {
                var inventoryIngredients = inventoryItemsDb.Where(i => 
                    i.IngredientId == ingredient.IngredientId || 
                    i.Ingredient.BaseIngredientId == ingredient.IngredientId);
                
                decimal totalFreeGrams = 0;
                foreach (var invItem in inventoryIngredients)
                {
                    var allocatedGrams = invItem.Allocations?.Sum(a => a.AllocatedQuantityInGrams) ?? 0;
                    var freeGrams = invItem.QuantityInGrams - allocatedGrams;
                    if (freeGrams > 0)
                    {
                        totalFreeGrams += freeGrams;
                    }
                }
                
                ingredient.QuantityInInventory = totalFreeGrams > 0 && ingredient.ConversionFactor > 0 
                    ? totalFreeGrams / ingredient.ConversionFactor 
                    : 0;
            }
        }

        return mealplan;
    }

    public async Task<List<MealPlanPreviewDto>> GetMealPlansAsync(Guid groupId, DateTime fromDate, DateTime toDate, string baseUrl, string lang)
    {
        var imageBaseUrl = $"{baseUrl}/images/recipes/";
        var defaultImage = ImageConstants.DefaultRecipe;

        var userImageBaseUrl = $"{baseUrl}/images/avatars/";
        var userDefaultImage = ImageConstants.DefaultAvatar;

        DateTime utcFromDate = DateTime.SpecifyKind(fromDate, DateTimeKind.Utc);
        DateTime utcToDate = DateTime.SpecifyKind(toDate, DateTimeKind.Utc);

        return await _db.MealPlans
            .AsNoTracking()
            .Where(mp => mp.GroupId == groupId && mp.Date.Date.ToUniversalTime() >= utcFromDate.Date && mp.Date.Date.ToUniversalTime() <= utcToDate.Date)
            .OrderBy(mp => mp.Date)
            .ThenBy(mp => mp.MealType)
            .Select(mp => new MealPlanPreviewDto
            {
                Id = mp.Id,
                Date = DateOnly.FromDateTime(mp.Date),
                MealType = mp.MealType,
                Servings = mp.Servings,
                RecipeId = mp.RecipeId,
                IsCooked = mp.IsCooked,
                RecipeName = mp.Recipe.Titles.GetValueOrDefault(lang) ?? mp.Recipe.Titles.GetValueOrDefault("en") ?? "Recipe title",
                RecipePhotoUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(mp.Recipe.PhotoUrl) ? defaultImage : mp.Recipe.PhotoUrl)}",
                UsersPreviews = mp.Users.Select(u => new UserDisplayDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Surname = u.Surname,
                    AvatarUrl = $"{userImageBaseUrl}{(string.IsNullOrWhiteSpace(u.AvatarUrl) ? userDefaultImage : u.AvatarUrl)}",
                }).ToList(),
                Ingredients = mp.MealPlanIngredients.Select(i => new RecipeIngredientPreviewDto
                {
                    IngredientId = i.IngredientId,
                    IngredientName = i.Ingredient.Names.GetValueOrDefault(lang) ?? i.Ingredient.Names.GetValueOrDefault("en") ?? "Ingredient name",
                    Quantity = i.Quantity,
                    UnitId = i.UnitId,
                    UnitName = i.Unit.Names.GetValueOrDefault(lang) ?? i.Unit.Names.GetValueOrDefault("en") ?? "Unit",
                }).ToList(),
            }).ToListAsync();
    }

    public async Task<GeneralResponse> CreateMealPlanAsync(Guid groupId, MealPlanCreateDto dto)
    {
        var users = await _db.Users.Where(u => dto.UsersId.Contains(u.Id) && u.GroupId == groupId).ToListAsync();
        if (!users.Any()) return new GeneralResponse { Successful = false, StatusCode = 400, Message = "At least one user from the group has to be selected." };
        
        var ingredientIds = dto.Ingredients.Select(i => i.Ingredient.Id).Distinct().ToList();
        var unitIds = dto.Ingredients.Select(i => i.Unit.Id).Distinct().ToList();

        var existingIngredients = await _db.Ingredients.Where(i => ingredientIds.Contains(i.Id)).Include(i => i.IngredientUnits).ToListAsync();
        if (existingIngredients.Count != ingredientIds.Count) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Some ingredients do not exist." };
        
        var existingUnits = await _db.Units.Where(u => unitIds.Contains(u.Id)).ToListAsync();
        if (!unitIds.All(unitId => existingUnits.Any(eu => eu.Id == unitId))) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Some units do not exist." };

        var newMPId = Guid.NewGuid();
        var mealPlan = new MealPlan
        {
            Id = newMPId,
            Date = DateTime.SpecifyKind(dto.Date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
            MealType = dto.MealType,
            RecipeId = dto.RecipeId,
            GroupId = groupId,
            Servings = dto.Servings,
            Users = users,
            MealPlanIngredients = dto.Ingredients.Select(i => new MealPlanIngredient
            {
                Id = Guid.NewGuid(),
                MealPlanId = newMPId,
                IngredientId = i.Ingredient.Id,
                UnitId = i.Unit.Id,
                ConversionFactor = i.ConversionFactor,
                Quantity = i.Quantity
            }).ToList()
        };

        _db.MealPlans.Add(mealPlan);
        
        foreach (var ingredient in mealPlan.MealPlanIngredients)
        {
            var dbIngredient = existingIngredients.First(i => i.Id == ingredient.IngredientId);
            var dbConversionFactor = dbIngredient.IngredientUnits.Where(u => u.UnitId == ingredient.UnitId).Select(u => u.AmountInGrams).FirstOrDefault();
            
            var inventoryItems = await _db.InventoryItems
                .Include(ii => ii.Allocations)
                .Where(ii => ii.GroupId == groupId && (ii.IngredientId == ingredient.IngredientId || ii.Ingredient.BaseIngredientId == ingredient.IngredientId))
                .ToListAsync();

            var sortedItems = inventoryItems.OrderBy(i => !i.ExpirationDate.HasValue).ThenBy(i => i.ExpirationDate).ToList();
            var quantityToAllocate = ingredient.Quantity * dbConversionFactor;

            foreach (var sortedItem in sortedItems)
            {
                if (quantityToAllocate <= 0) break;

                var currentlyAllocatedQuantity = sortedItem.Allocations.Sum(a => a.AllocatedQuantityInGrams);
                var itemAvailableQuantity = sortedItem.QuantityInGrams - currentlyAllocatedQuantity;

                if (itemAvailableQuantity <= 0) continue;

                if (itemAvailableQuantity <= quantityToAllocate)
                {
                    quantityToAllocate -= itemAvailableQuantity;
                    _db.InventoryMealPlanAllocations.Add(new InventoryMealPlanAllocation
                    {
                        Id = Guid.NewGuid(),
                        InventoryItemId = sortedItem.Id,
                        MealPlanIngredientId = ingredient.Id,
                        AllocatedQuantityInGrams = itemAvailableQuantity,
                        AllocatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    _db.InventoryMealPlanAllocations.Add(new InventoryMealPlanAllocation
                    {
                        Id = Guid.NewGuid(),
                        InventoryItemId = sortedItem.Id,
                        MealPlanIngredientId = ingredient.Id,
                        AllocatedQuantityInGrams = quantityToAllocate,
                        AllocatedAt = DateTime.UtcNow
                    });
                    quantityToAllocate = 0;
                    break;
                }
            }
            
            if (quantityToAllocate > 0)
            {
                _db.ShoppingListItems.Add(new ShoppingListItem
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    IngredientId = ingredient.IngredientId,
                    UnitId = ingredient.UnitId,
                    Quantity = quantityToAllocate / dbConversionFactor,
                    QuantityInGrams = quantityToAllocate,
                    ShoppingCategoryId = dbIngredient.ShoppingCategoryId,
                    IsPurchased = false,
                    MealPlanIngredientId = ingredient.Id
                });
            }
        }

        await _db.SaveChangesAsync();
        return new GeneralResponse { Successful = true, StatusCode = 201, Id = newMPId, Message = "Meal plan created" };
    }

    public async Task<GeneralResponse> DeleteMealPlanAsync(Guid groupId, Guid id)
    {
        var mealPlan = await _db.MealPlans.Include(mp => mp.MealPlanIngredients).FirstOrDefaultAsync(mp => mp.Id == id);
        if (mealPlan is null) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Meal plan not found" };
        if (mealPlan.GroupId != groupId) return new GeneralResponse { Successful = false, StatusCode = 403, Message = "Unauthorized" };
        
        var mpiIds = mealPlan.MealPlanIngredients.Select(mpi => mpi.Id).ToList();

        if (mpiIds.Any())
        {
            var allocations = await _db.InventoryMealPlanAllocations.Where(a => mpiIds.Contains(a.MealPlanIngredientId)).ToListAsync();
            if (allocations.Any()) _db.InventoryMealPlanAllocations.RemoveRange(allocations);
            
            var shoppingListItems = await _db.ShoppingListItems
                .Where(si => si.MealPlanIngredientId.HasValue && mpiIds.Contains(si.MealPlanIngredientId.Value) && !si.IsPurchased)
                .ToListAsync();

            if (shoppingListItems.Any()) _db.ShoppingListItems.RemoveRange(shoppingListItems);
        }
        
        _db.MealPlans.Remove(mealPlan);
        await _db.SaveChangesAsync();

        return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Meal plan deleted" };
    }

    public async Task<GeneralResponse> UpdateMealPlanAsync(Guid groupId, Guid id, MealPlanUpdateDto dto)
    {
        var mealPlan = await _db.MealPlans
            .Include(mp => mp.Users)
            .Include(mp => mp.MealPlanIngredients)
            .FirstOrDefaultAsync(mp => mp.Id == id);

        if (mealPlan is null) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Meal plan not found" };
        if (mealPlan.GroupId != groupId) return new GeneralResponse { Successful = false, StatusCode = 403, Message = "Unauthorized" };

        var newUsers = await _db.Users.Where(u => dto.UsersIds.Contains(u.Id) && u.GroupId == groupId).ToListAsync();
        if (!newUsers.Any()) return new GeneralResponse { Successful = false, StatusCode = 400, Message = "At least one user must be selected." };
        if (!dto.Ingredients.Any()) return new GeneralResponse { Successful = false, StatusCode = 400, Message = "At least one ingredient must be selected." };
        
        var ingredientIds = dto.Ingredients.Select(i => i.Ingredient.Id).Distinct().ToList();
        var unitIds = dto.Ingredients.Select(i => i.Unit.Id).Distinct().ToList();

        var existingIngredients = await _db.Ingredients.Where(i => ingredientIds.Contains(i.Id)).ToListAsync();
        if (existingIngredients.Count != ingredientIds.Count) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Some ingredients do not exist." };

        var existingUnits = await _db.Units.Where(u => unitIds.Contains(u.Id)).ToListAsync();
        if (!unitIds.All(unitId => existingUnits.Any(eu => eu.Id == unitId))) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Some units do not exist." };
        
        var mpiToAllocate = new List<MealPlanIngredient>();
        
        foreach (var mpi in mealPlan.MealPlanIngredients.ToList())
        {
            var dtoIngredient = dto.Ingredients.FirstOrDefault(i => i.Ingredient.Id == mpi.IngredientId);

            if (dtoIngredient is null)
            {
                var allocations = await _db.InventoryMealPlanAllocations.Where(a => a.MealPlanIngredientId == mpi.Id).ToListAsync();
                _db.InventoryMealPlanAllocations.RemoveRange(allocations);

                var shoppingItems = await _db.ShoppingListItems.Where(s => s.MealPlanIngredientId == mpi.Id && !s.IsPurchased).ToListAsync();
                _db.ShoppingListItems.RemoveRange(shoppingItems);

                _db.MealPlanIngredients.Remove(mpi);
            }
            else
            {
                var oldQuantityGrams = mpi.Quantity * mpi.ConversionFactor;
                var newQuantityGrams = dtoIngredient.Quantity * dtoIngredient.ConversionFactor;

                if (oldQuantityGrams != newQuantityGrams || mpi.UnitId != dtoIngredient.Unit.Id)
                {
                    var allocations = await _db.InventoryMealPlanAllocations.Where(a => a.MealPlanIngredientId == mpi.Id).ToListAsync();
                    _db.InventoryMealPlanAllocations.RemoveRange(allocations);

                    var shoppingItems = await _db.ShoppingListItems.Where(s => s.MealPlanIngredientId == mpi.Id && !s.IsPurchased).ToListAsync();
                    _db.ShoppingListItems.RemoveRange(shoppingItems);
                    
                    mpi.ConversionFactor = dtoIngredient.ConversionFactor;
                    mpi.Quantity = dtoIngredient.Quantity;
                    mpi.UnitId = dtoIngredient.Unit.Id;
                    
                    mpiToAllocate.Add(mpi);
                }
            }
        }
        
        foreach (var ingredientDto in dto.Ingredients)
        {
            if (!mealPlan.MealPlanIngredients.Any(mpi => mpi.IngredientId == ingredientDto.Ingredient.Id))
            {
                var newMPI = new MealPlanIngredient
                {
                    Id = Guid.NewGuid(),
                    MealPlanId = mealPlan.Id,
                    IngredientId = ingredientDto.Ingredient.Id,
                    UnitId = ingredientDto.Unit.Id,
                    ConversionFactor = ingredientDto.ConversionFactor,
                    Quantity = ingredientDto.Quantity
                };
                _db.MealPlanIngredients.Add(newMPI);
                mpiToAllocate.Add(newMPI);
            }
        }
        
        mealPlan.Date = DateTime.SpecifyKind(dto.Date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        mealPlan.MealType = dto.MealType;
        mealPlan.Servings = dto.Servings;
        mealPlan.Users = newUsers;
        
        foreach (var ingredient in mpiToAllocate)
        {
            var dbIngredient = existingIngredients.First(i => i.Id == ingredient.IngredientId);
            var inventoryItems = await _db.InventoryItems
                .Include(ii => ii.Allocations)
                .Where(ii => ii.GroupId == groupId && (ii.IngredientId == ingredient.IngredientId || ii.Ingredient.BaseIngredientId == ingredient.IngredientId))
                .ToListAsync();

            var sortedItems = inventoryItems.OrderBy(i => !i.ExpirationDate.HasValue).ThenBy(i => i.ExpirationDate).ToList();
            var quantityToAllocate = ingredient.Quantity * ingredient.ConversionFactor;

            foreach (var sortedItem in sortedItems)
            {
                if (quantityToAllocate <= 0) break;
                var activeAllocations = sortedItem.Allocations.Where(a => _db.Entry(a).State != EntityState.Deleted);
                var currentAllocatedGrams = activeAllocations.Sum(a => a.AllocatedQuantityInGrams);
                
                var itemAvailableQuantity = sortedItem.QuantityInGrams - currentAllocatedGrams;

                if (itemAvailableQuantity <= 0) continue;

                if (itemAvailableQuantity <= quantityToAllocate)
                {
                    quantityToAllocate -= itemAvailableQuantity;
                    _db.InventoryMealPlanAllocations.Add(new InventoryMealPlanAllocation
                    {
                        Id = Guid.NewGuid(),
                        InventoryItemId = sortedItem.Id,
                        MealPlanIngredientId = ingredient.Id,
                        AllocatedQuantityInGrams = itemAvailableQuantity,
                        AllocatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    _db.InventoryMealPlanAllocations.Add(new InventoryMealPlanAllocation
                    {
                        Id = Guid.NewGuid(),
                        InventoryItemId = sortedItem.Id,
                        MealPlanIngredientId = ingredient.Id,
                        AllocatedQuantityInGrams = quantityToAllocate,
                        AllocatedAt = DateTime.UtcNow
                    });
                    quantityToAllocate = 0;
                    break;
                }
            }
            
            if (quantityToAllocate > 0)
            {
                _db.ShoppingListItems.Add(new ShoppingListItem
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    IngredientId = ingredient.IngredientId,
                    UnitId = ingredient.UnitId,
                    Quantity = quantityToAllocate / ingredient.ConversionFactor,
                    QuantityInGrams = quantityToAllocate,
                    ShoppingCategoryId = dbIngredient.ShoppingCategoryId,
                    IsPurchased = false,
                    MealPlanIngredientId = ingredient.Id
                });
            }
        }

        await _db.SaveChangesAsync();
        return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Meal plan updated successfully" };
    }

    public async Task<GeneralResponse> MarkAsCookedAsync(Guid groupId, Guid id)
    {
        var mealPlan = await _db.MealPlans.Include(mp => mp.MealPlanIngredients).FirstOrDefaultAsync(mp => mp.Id == id && mp.GroupId == groupId);
        
        if (mealPlan is null) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Meal plan not found" };
        if (mealPlan.IsCooked) return new GeneralResponse { Successful = false, StatusCode = 400, Message = "Meal plan already marked as cooked." };

        mealPlan.IsCooked = true;
        var ingredientIds = mealPlan.MealPlanIngredients.Select(i => i.Id).ToList();

        if (ingredientIds.Any())
        {
            var allocations = await _db.InventoryMealPlanAllocations
                .Include(a => a.InventoryItem)
                .Where(a => ingredientIds.Contains(a.MealPlanIngredientId))
                .ToListAsync();

            foreach (var allocation in allocations)
            {
                var item = allocation.InventoryItem;
                if (item is not null)
                {
                    if (item.QuantityInGrams > 0 && item.Quantity > 0)
                    {
                        var gramsPerUnit = item.QuantityInGrams / item.Quantity;
                        if (gramsPerUnit > 0)
                        {
                            item.Quantity -= allocation.AllocatedQuantityInGrams / gramsPerUnit;
                        }
                    }

                    item.QuantityInGrams -= allocation.AllocatedQuantityInGrams;
                    
                    if (item.QuantityInGrams <= 0.001m || item.Quantity <= 0.001m)
                    {
                        _db.InventoryItems.Remove(item);
                    }
                }
                
                _db.InventoryMealPlanAllocations.Remove(allocation);
            }
            
            var oldShoppingListItems = await _db.ShoppingListItems
                .Where(i => i.MealPlanIngredientId.HasValue && ingredientIds.Contains(i.MealPlanIngredientId.Value))
                .ToListAsync();

            _db.ShoppingListItems.RemoveRange(oldShoppingListItems);
        }

        await _db.SaveChangesAsync();
        return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Meal plan marked as cooked" };
    }

    public async Task<(int StatusCode, string? Message, GroupDetailDto? Group)> CreateGroupAsync(Guid userId, GroupCreateDto dto)
    {
        var userDb = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (userDb is null) return (404, "User not found", null);
        if (userDb.GroupId is not null) return (400, "User is already in a group", null);

        var rawCode = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 12).ToUpper();
        var inviteCode = $"{rawCode.Substring(0, 4)}-{rawCode.Substring(4, 4)}-{rawCode.Substring(8, 4)}";

        var newGroup = new Group
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            InviteCode = inviteCode
        };

        _db.Groups.Add(newGroup);
        userDb.GroupId = newGroup.Id;
        await _db.SaveChangesAsync();

        return (201, null, new GroupDetailDto { Id = newGroup.Id, Name = newGroup.Name, InviteCode = newGroup.InviteCode });
    }

    public async Task<GeneralResponse> JoinGroupAsync(Guid userId, string code)
    {
        var userDb = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (userDb is null) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "User not found" };
        if (userDb.GroupId is not null) return new GeneralResponse { Successful = false, StatusCode = 400, Message = "User is already in group" };

        var group = await _db.Groups.FirstOrDefaultAsync(g => g.InviteCode.Replace("-", "").ToLower() == code.Replace("-", "").Trim().ToLower());
        if (group is null) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Invalid invitation code" };

        userDb.GroupId = group.Id;
        await _db.SaveChangesAsync();

        return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Successfully joined group" };
    }

    public async Task<GroupDetailDto?> GetMyGroupAsync(Guid userId)
    {
        var userDb = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (userDb?.GroupId is null) return null;

        return await _db.Groups
            .AsNoTracking()
            .Where(g => g.Id == userDb.GroupId)
            .Select(g => new GroupDetailDto { Id = g.Id, Name = g.Name, InviteCode = g.InviteCode })
            .FirstOrDefaultAsync();
    }

    public async Task<GeneralResponse> LeaveGroupAsync(Guid userId)
    {
        var userDb = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (userDb is null) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "User not found" };
        
        var groupId = userDb.GroupId;
        if (groupId is null) return new GeneralResponse { Successful = false, StatusCode = 400, Message = "User is not in a group" };

        userDb.GroupId = null;

        if (!await _db.Users.AnyAsync(u => u.GroupId == groupId && u.Id != userId))
        {
            var groupToDelete = await _db.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
            if (groupToDelete is not null)
            {
                _db.Groups.Remove(groupToDelete);
            }
        }

        await _db.SaveChangesAsync();
        return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Successfully left group" };
    }

    public async Task<List<UserDisplayDto>> GetGroupMembersAsync(Guid groupId, string baseUrl)
    {
        var userImageBaseUrl = $"{baseUrl}/images/avatars/";
        var userDefaultImage = ImageConstants.DefaultAvatar;

        return await _db.Users
            .AsNoTracking()
            .Where(u => u.GroupId == groupId)
            .Select(u => new UserDisplayDto
            {
                Id = u.Id,
                Name = u.Name,
                Surname = u.Surname,
                AvatarUrl = $"{userImageBaseUrl}{(string.IsNullOrWhiteSpace(u.AvatarUrl) ? userDefaultImage : u.AvatarUrl)}",
            }).ToListAsync();
    }

    public async Task<GeneralResponse> RenameGroupAsync(Guid groupId, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return new GeneralResponse { Successful = false, StatusCode = 400, Message = "Invalid group name" };
        
        var groupDb = await _db.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
        if (groupDb is null) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Group not found" };

        groupDb.Name = name.Trim();
        await _db.SaveChangesAsync();

        return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Group renamed successfully" };
    }
}