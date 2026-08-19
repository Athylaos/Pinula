using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using Pinula.API.Context;
using Pinula.Shared.DTOs;
using Pinula.API.Models;
using System.Globalization;
using System.Security.Claims;

namespace Pinula.API.Endpoints
{
    public static class MealPlanEndpoint
    {
        public static void MapMealPlanEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/mealplan");



            //---------------------------------------------------------------Get meal plans
            group.MapGet("/get", async (HttpRequest request,DateTime fromDate, DateTime toDate, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/recipes/";
                var defaultImage = "default_recipe.png";

                var userImageBaseUrl = $"{request.Scheme}://{request.Host}/images/avatars/";
                var userDefaultImage = "default_avatar.png";

                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                var userId = user.GetUserId();
                var userDb = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");

                var groupId = userDb.GroupId;
                if (groupId is null) return Results.BadRequest("User is not in group");

                DateTime utcFromDate = DateTime.SpecifyKind(fromDate, DateTimeKind.Utc);
                DateTime utcToDate = DateTime.SpecifyKind(toDate, DateTimeKind.Utc);

                var mealplans = await db.MealPlans
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
                        RecipeName = mp.Recipe.Titles.GetValueOrDefault(languageCode) ?? mp.Recipe.Titles.GetValueOrDefault("en") ?? "Recipe title",
                        RecipePhotoUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(mp.Recipe.PhotoUrl) ? defaultImage : mp.Recipe.PhotoUrl)}",
                        UsersPreviews = mp.Users.Select(u => new UserDisplayDto
                        {
                            Id = u.Id,
                            Name = u.Name,
                            Surname = u.Surname,
                            AvatarUrl = $"{userImageBaseUrl}{(string.IsNullOrWhiteSpace(u.AvatarUrl) ? userDefaultImage : u.AvatarUrl)}",
                        }).ToList(),
                        Ingredients = mp.MealPlanIngredients.Select(i => new RecipeIngredientPreviewDto()
                        {
                            IngredientId = i.IngredientId,
                            IngredientName = i.Ingredient.Names.GetValueOrDefault(languageCode) ?? i.Ingredient.Names.GetValueOrDefault("en") ?? "Ingredient name",
                            Quantity = i.Quantity,
                            UnitId = i.UnitId,
                            UnitName = i.Unit.Names.GetValueOrDefault(languageCode) ?? i.Unit.Names.GetValueOrDefault("en") ?? "Unit",
                        }).ToList(),
                    }).ToListAsync();

                return Results.Ok(mealplans);

            }).RequireAuthorization();

            //---------------------------------------------------------------Create meal plan
            group.MapPost("/add", async (MealPlanCreateDto dto, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var userDb = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");

                var groupId = userDb.GroupId;
                if (groupId is null) return Results.BadRequest("User is not in group");

                var users = await db.Users.Where(u => dto.UsersId.Contains(u.Id) && u.GroupId == groupId).ToListAsync();
                if (!users.Any()) return Results.BadRequest("At least one user from the group has to be selected.");
                
                var ingredientIds = dto.Ingredients.Select(i => i.Ingredient.Id).Distinct().ToList();
                var unitIds = dto.Ingredients.Select(i => i.Unit.Id).Distinct().ToList();

                var existingIngredients = await db.Ingredients.Where(i => ingredientIds.Contains(i.Id)).Include(i => i.IngredientUnits).ToListAsync();
                if (existingIngredients.Count != ingredientIds.Count)
                {
                    return Results.NotFound("Some ingredients do not exist in the database.");
                }
                
                var existingUnits = await db.Units.Where(u => unitIds.Contains(u.Id)).ToListAsync();
                if (!unitIds.All(unitId => existingUnits.Any(eu => eu.Id == unitId)))
                {
                    return Results.NotFound("Some units do not exist in the database.");
                }

                var newMPId = Guid.NewGuid();
                var mealPlan = new MealPlan
                {
                    Id = newMPId,
                    Date = DateTime.SpecifyKind(dto.Date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
                    MealType = dto.MealType,
                    RecipeId = dto.RecipeId,
                    GroupId = groupId.Value,
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

                db.MealPlans.Add(mealPlan);
                
                foreach (var ingredient in mealPlan.MealPlanIngredients)
                {
                    var dbIngredient = existingIngredients.First(i => i.Id == ingredient.IngredientId);
                    var dbConversionFactor = dbIngredient.IngredientUnits.Where(u => u.UnitId == ingredient.UnitId)
                        .Select(u => u.AmountInGrams).FirstOrDefault();
                    
                    var inventoryItems = await db.InventoryItems
                        .Include(ii => ii.Allocations)
                        .Where(ii => ii.GroupId == groupId.Value && (ii.IngredientId == ingredient.IngredientId || ii.Ingredient.BaseIngredientId == ingredient.IngredientId))
                        .ToListAsync();

                    var sortedItems = inventoryItems
                        .OrderBy(i => !i.ExpirationDate.HasValue)
                        .ThenBy(i => i.ExpirationDate)
                        .ToList();

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
                            db.InventoryMealPlanAllocations.Add(new InventoryMealPlanAllocation
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
                            db.InventoryMealPlanAllocations.Add(new InventoryMealPlanAllocation
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
                        db.ShoppingListItems.Add(new ShoppingListItem
                        {
                            Id = Guid.NewGuid(),
                            GroupId = groupId.Value,
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

                await db.SaveChangesAsync();
                return Results.Ok();

            }).RequireAuthorization();

            //---------------------------------------------------------------Delete meal plan
            group.MapDelete("/delete/{id:guid}", async (Guid id, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var userDb = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");

                var groupId = userDb.GroupId;
                if (groupId is null) return Results.BadRequest("User is not in group");

                var mealPlan = await db.MealPlans.Include(mp => mp.MealPlanIngredients).FirstOrDefaultAsync(mp => mp.Id == id);
                if (mealPlan is null) return Results.NotFound();

                if (mealPlan.GroupId != groupId) return Results.Unauthorized();
                
                var mpiIds = mealPlan.MealPlanIngredients.Select(mpi => mpi.Id).ToList();

                if (mpiIds.Any())
                {
                    var allocations = await db.InventoryMealPlanAllocations
                        .Where(a => mpiIds.Contains(a.MealPlanIngredientId))
                        .ToListAsync();

                    if (allocations.Any())
                    {
                        db.InventoryMealPlanAllocations.RemoveRange(allocations);
                    }
                    
                    var shoppingListItems = await db.ShoppingListItems
                        .Where(si => si.MealPlanIngredientId.HasValue && mpiIds.Contains(si.MealPlanIngredientId.Value) && !si.IsPurchased)
                        .ToListAsync();

                    if (shoppingListItems.Any())
                    {
                        db.ShoppingListItems.RemoveRange(shoppingListItems);
                    }
                }
                db.MealPlans.Remove(mealPlan);
                await db.SaveChangesAsync();

                return Results.Ok();

            }).RequireAuthorization();

            //---------------------------------------------------------------Update meal plan
            group.MapPut("/update/{id:guid}", async (Guid id, MealPlanUpdateDto dto, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var userDb = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");

                var groupId = userDb.GroupId;
                if (groupId is null) return Results.BadRequest("User is not in group");

                var mealPlan = await db.MealPlans
                    .Include(mp => mp.Users)
                    .Include(mp => mp.MealPlanIngredients)
                    .FirstOrDefaultAsync(mp => mp.Id == id);

                if (mealPlan is null) return Results.NotFound();
                if (mealPlan.GroupId != groupId) return Results.Unauthorized();

                var newUsers = await db.Users.Where(u => dto.UsersIds.Contains(u.Id) && u.GroupId == groupId).ToListAsync();
                if (!newUsers.Any()) return Results.BadRequest("At least one user must be selected.");

                if (!dto.Ingredients.Any()) return Results.BadRequest("At least one ingredient must be selected.");
                
                var ingredientIds = dto.Ingredients.Select(i => i.Ingredient.Id).Distinct().ToList();
                var unitIds = dto.Ingredients.Select(i => i.Unit.Id).Distinct().ToList();

                var existingIngredients = await db.Ingredients.Where(i => ingredientIds.Contains(i.Id)).ToListAsync();
                if (existingIngredients.Count != ingredientIds.Count)
                {
                    return Results.NotFound("Some ingredients do not exist in the database.");
                }

                var existingUnits = await db.Units.Where(u => unitIds.Contains(u.Id)).ToListAsync();
                if (!unitIds.All(unitId => existingUnits.Any(eu => eu.Id == unitId)))
                {
                    return Results.NotFound("Some units do not exist in the database.");
                }
                
                var mpiToAllocate = new List<MealPlanIngredient>();
                
                foreach (var mpi in mealPlan.MealPlanIngredients.ToList())
                {
                    var dtoIngredient = dto.Ingredients.FirstOrDefault(i => i.Ingredient.Id == mpi.IngredientId);

                    if (dtoIngredient is null)
                    {
                        //deleted ingredients
                        var allocations = await db.InventoryMealPlanAllocations.Where(a => a.MealPlanIngredientId == mpi.Id).ToListAsync();
                        db.InventoryMealPlanAllocations.RemoveRange(allocations);

                        var shoppingItems = await db.ShoppingListItems.Where(s => s.MealPlanIngredientId == mpi.Id && !s.IsPurchased).ToListAsync();
                        db.ShoppingListItems.RemoveRange(shoppingItems);

                        db.MealPlanIngredients.Remove(mpi);
                    }
                    else
                    {
                        var oldQuantityGrams = mpi.Quantity * mpi.ConversionFactor;
                        var newQuantityGrams = dtoIngredient.Quantity * dtoIngredient.ConversionFactor;

                        if (oldQuantityGrams != newQuantityGrams || mpi.UnitId != dtoIngredient.Unit.Id)
                        {
                            //changed ingredients
                            var allocations = await db.InventoryMealPlanAllocations.Where(a => a.MealPlanIngredientId == mpi.Id).ToListAsync();
                            db.InventoryMealPlanAllocations.RemoveRange(allocations);

                            var shoppingItems = await db.ShoppingListItems.Where(s => s.MealPlanIngredientId == mpi.Id && !s.IsPurchased).ToListAsync();
                            db.ShoppingListItems.RemoveRange(shoppingItems);
                            
                            mpi.ConversionFactor = dtoIngredient.ConversionFactor;
                            mpi.Quantity = dtoIngredient.Quantity;
                            mpi.UnitId = dtoIngredient.Unit.Id;
                            
                            mpiToAllocate.Add(mpi);
                        }
                        //unchanged ingredients
                    }
                }
                

                foreach (var ingredientDto in dto.Ingredients)
                {
                    if (!mealPlan.MealPlanIngredients.Any(mpi => mpi.IngredientId == ingredientDto.Ingredient.Id))
                    {
                        //new ingredients
                        var newMPI = new MealPlanIngredient
                        {
                            Id = Guid.NewGuid(),
                            MealPlanId = mealPlan.Id,
                            IngredientId = ingredientDto.Ingredient.Id,
                            UnitId = ingredientDto.Unit.Id,
                            ConversionFactor = ingredientDto.ConversionFactor,
                            Quantity = ingredientDto.Quantity
                        };
                        db.MealPlanIngredients.Add(newMPI);
                        mpiToAllocate.Add(newMPI);
                    }
                }
                
                mealPlan.Date = DateTime.SpecifyKind(dto.Date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
                mealPlan.MealType = dto.MealType;
                mealPlan.Servings = dto.Servings;
                mealPlan.Users = newUsers;
                
                //allocation of new and changed ingredients
                foreach (var ingredient in mpiToAllocate)
                {
                    var dbIngredient = existingIngredients.First(i => i.Id == ingredient.IngredientId);

                    var inventoryItems = await db.InventoryItems
                        .Include(ii => ii.Allocations)
                        .Where(ii => ii.GroupId == groupId.Value && (ii.IngredientId == ingredient.IngredientId || ii.Ingredient.BaseIngredientId == ingredient.IngredientId))
                        .ToListAsync();

                    var sortedItems = inventoryItems
                        .OrderBy(i => !i.ExpirationDate.HasValue)
                        .ThenBy(i => i.ExpirationDate)
                        .ToList();

                    var quantityToAllocate = ingredient.Quantity * ingredient.ConversionFactor;

                    foreach (var sortedItem in sortedItems)
                    {
                        if (quantityToAllocate <= 0) break;
                        var activeAllocations = sortedItem.Allocations.Where(a => db.Entry(a).State != EntityState.Deleted); //due to staged changes
                        var currentAllocatedGrams = activeAllocations.Sum(a => a.AllocatedQuantityInGrams);
                        
                        var itemAvailableQuantity = sortedItem.QuantityInGrams - currentAllocatedGrams;

                        if (itemAvailableQuantity <= 0) continue;

                        if (itemAvailableQuantity <= quantityToAllocate)
                        {
                            quantityToAllocate -= itemAvailableQuantity;
                            db.InventoryMealPlanAllocations.Add(new InventoryMealPlanAllocation
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
                            db.InventoryMealPlanAllocations.Add(new InventoryMealPlanAllocation
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
                        db.ShoppingListItems.Add(new ShoppingListItem
                        {
                            Id = Guid.NewGuid(),
                            GroupId = groupId.Value,
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

                await db.SaveChangesAsync();
                return Results.Ok();

            }).RequireAuthorization();

            //---------------------------------------------------------------Create group
            group.MapPost("/group/create", async (GroupCreateDto dto, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var userDb = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");
         
                if (userDb.GroupId is not null) return Results.BadRequest("User is already in group");

                var rawCode = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 12).ToUpper();
                var inviteCode = $"{rawCode.Substring(0, 4)}-{rawCode.Substring(4, 4)}-{rawCode.Substring(8, 4)}";

                var newGroup = new Group
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    InviteCode = inviteCode
                };

                db.Groups.Add(newGroup);
                userDb.GroupId = newGroup.Id;
                await db.SaveChangesAsync();

                return Results.Ok(new GroupDetailDto
                {
                    Id = newGroup.Id,
                    Name = newGroup.Name,
                    InviteCode = newGroup.InviteCode
                });

            }).RequireAuthorization();

            //---------------------------------------------------------------Join group
            group.MapPost("/group/join/{code}", async (string code, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var userDb = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");

                if (userDb.GroupId is not null) return Results.BadRequest("User is already in group");

                var group = await db.Groups.FirstOrDefaultAsync(g => g.InviteCode.Replace("-", "").ToLower() == code.Replace("-", "").Trim().ToLower());
                if (group is null) return Results.NotFound("Invalid invitation code");

                userDb.GroupId = group.Id;
                await db.SaveChangesAsync();

                return Results.Ok();

            }).RequireAuthorization();

            //---------------------------------------------------------------Get my group
            group.MapGet("/group/my", async (ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var userDb = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");

                var groupId = userDb.GroupId;
                if (groupId is null) return Results.BadRequest("User is not in group");

                var group = await db.Groups
                    .AsNoTracking()
                    .Where(g => g.Id == userDb.GroupId)
                    .Select(g => new GroupDetailDto
                    {
                        Id = g.Id,
                        Name = g.Name,
                        InviteCode = g.InviteCode
                    })
                    .FirstOrDefaultAsync();

                return Results.Ok(group);

            }).RequireAuthorization();

            //---------------------------------------------------------------Leave my group
            group.MapPost("/group/leave", async (ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var userDb = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");

                var groupId = userDb.GroupId;
                if (groupId is null) return Results.BadRequest("User is not in group");

                userDb.GroupId = null;

                if (!await db.Users.AnyAsync(u => u.GroupId == groupId && u.Id != userId))
                {
                    var groupToDelete = await db.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
                    if (groupToDelete is not null)
                    {
                        db.Groups.Remove(groupToDelete);
                    }
                }

                await db.SaveChangesAsync();

                return Results.Ok();

            }).RequireAuthorization();

            //---------------------------------------------------------------Get members
            group.MapGet("/group/members", async (HttpRequest request, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userImageBaseUrl = $"{request.Scheme}://{request.Host}/images/avatars/";
                var userDefaultImage = "default_avatar.png";

                var userId = user.GetUserId();
                var userDb = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");

                var groupId = userDb.GroupId;
                if (groupId is null) return Results.BadRequest("User is not in group");

                var members = await db.Users
                    .AsNoTracking()
                    .Where(u => u.GroupId == groupId)
                    .Select(u => new UserDisplayDto
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Surname = u.Surname,
                        AvatarUrl = $"{userImageBaseUrl}{(string.IsNullOrWhiteSpace(u.AvatarUrl) ? userDefaultImage : u.AvatarUrl)}",
                    }).ToListAsync();

                return Results.Ok(members);

            }).RequireAuthorization();

            //---------------------------------------------------------------Rename my group
            group.MapPost("/group/rename/{name}", async (string name, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var userDb = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");

                var groupId = userDb.GroupId;
                if (groupId is null) return Results.BadRequest("User is not in group");

                if(string.IsNullOrWhiteSpace(name)) Results.BadRequest("Invalid group name");

                var newName = name.Trim();

                var groupDb = await db.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
                if(groupDb is not null)
                {
                    groupDb.Name = newName;
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }

                return Results.NotFound("Group not found");

            }).RequireAuthorization();


        }
    }
}
