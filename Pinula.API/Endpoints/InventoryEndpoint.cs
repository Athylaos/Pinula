
using System.Security.Claims;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Models;
using Pinula.API.Context;
using Pinula.Shared.DTOs;
using Pinula.API.Services;

namespace Pinula.API.Endpoints;

public static class InventoryEndpoint
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/inventory");
        
        
        //---------------------------------------------------------------Get all inventory items
        group.MapGet("/getAll", async (HttpRequest request, ClaimsPrincipal user, PinulaDbContext db) =>
        {
            var (authResult, userDb, groupDb) = await HelperFunctions.AuthUserInGroup(user, db);
            if (authResult != Results.Ok())
            {
                return authResult;
            }

            var ii = await db.InventoryItems
                .Where(i => i.GroupId == groupDb.Id)
                .Include(i => i.Ingredient)
                .Include(i => i.Unit)
                .ToListAsync();
                
                
            var result = ii.AdaptWithRequest<List<InventoryItemDisplayDto>>(request);

            return Results.Ok(result);
        }).RequireAuthorization();
        
        //---------------------------------------------------------------Create inventory item
        group.MapPost("/create", async (InventoryItemCreateDto dto, ClaimsPrincipal user, PinulaDbContext db) =>
        {
            var (authResult, userDb, groupDb) = await HelperFunctions.AuthUserInGroup(user, db);
            if (authResult != Results.Ok())
            {
                return authResult;
            }

            if (!await db.Ingredients.AnyAsync(i => i.Id == dto.IngredientId))
            {
                return Results.NotFound("Ingredient not found");
            }
            if (!await db.Units.AnyAsync(i => i.Id == dto.UnitId))
            {
                return Results.NotFound("Unit not found");
            }

            if (dto.QuantityInGrams <= 0 || dto.Quantity <= 0)
            {
                return Results.BadRequest("Quantity can't be 0");
            }

            InventoryItem nII = new InventoryItem()
            {
                Id = Guid.NewGuid(),
                GroupId = groupDb.Id,
                IngredientId = dto.IngredientId,
                UnitId = dto.UnitId,
                Quantity = dto.Quantity,
                QuantityInGrams = dto.QuantityInGrams,
                ExpirationDate = dto.ExpirationDate,
                IsAllocated = dto.IsAllocated
            };

            db.InventoryItems.Add(nII);
            await db.SaveChangesAsync();
            
            return Results.Created($"/inventory/getAll", new { id = nII.Id });
        }).RequireAuthorization();
        
        //---------------------------------------------------------------Update inventory item
        group.MapPut("/update", async (InventoryItemUpdateDto dto, ClaimsPrincipal user, PinulaDbContext db) =>
        {
            var (authResult, userDb, groupDb) = await HelperFunctions.AuthUserInGroup(user, db);
            if (authResult != Results.Ok())
            {
                return authResult;
            }

            var item = await db.InventoryItems.FirstOrDefaultAsync(i => i.Id == dto.Id);
            if (item is null)
            {
                return Results.NotFound("Inventory item not found");
            }
            
            if (dto.QuantityInGrams <= 0 || dto.Quantity <= 0)
            {
                return Results.BadRequest("Quantity can't be 0");
            }

            if(dto.Quantity.HasValue) item.Quantity = dto.Quantity.Value;
            if(dto.QuantityInGrams.HasValue) item.QuantityInGrams = dto.QuantityInGrams.Value;
            if(dto.ExpirationDate.HasValue) item.ExpirationDate = dto.ExpirationDate.Value;
            if(dto.IsAllocated.HasValue) item.IsAllocated = dto.IsAllocated.Value;

            await db.SaveChangesAsync();
            return Results.NoContent();
            
        }).RequireAuthorization();
        
        //---------------------------------------------------------------Delete inventory item
        group.MapDelete("/delete/{id:guid}", async (Guid id, ClaimsPrincipal user, PinulaDbContext db) =>
        {
            var (authResult, userDb, groupDb) = await HelperFunctions.AuthUserInGroup(user, db);
            if (authResult != Results.Ok())
            {
                return authResult;
            }
            var item = await db.InventoryItems.FirstOrDefaultAsync(i => i.Id == id);
            if (item is null)
            {
                return Results.NotFound("Inventory item not found");
            }

            db.InventoryItems.Remove(item);
            await db.SaveChangesAsync();

            return Results.NoContent();
        }).RequireAuthorization();
        
        
        
        
        
        //---------------------------------------------------------------Get all shopping items
        group.MapGet("/shoppingListItems/getAll", async (HttpRequest request, ClaimsPrincipal user, PinulaDbContext db) =>
        {
            var (authResult, userDb, groupDb) = await HelperFunctions.AuthUserInGroup(user, db);
            if (authResult != Results.Ok())
            {
                return authResult;
            }

            var si = await db.ShoppingListItems
                .Where(i => i.GroupId == groupDb.Id)
                .Include(i => i.Ingredient)
                .Include(i => i.Unit)
                .Include(i => i.ShoppingCategory)
                .ToListAsync();
            
           var result = si.AdaptWithRequest<List<ShoppingItemDisplayDto>>(request);

            return Results.Ok(result);
        }).RequireAuthorization();
        
        //---------------------------------------------------------------Create shopping item
        group.MapPost("/shoppingListItems/create", async (ShoppingItemCreateDto dto, ClaimsPrincipal user, PinulaDbContext db) =>
        {
            var (authResult, userDb, groupDb) = await HelperFunctions.AuthUserInGroup(user, db);
            if (authResult != Results.Ok())
            {
                return authResult;
            }

            if (!await db.Ingredients.AnyAsync(i => i.Id == dto.IngredientId))
            {
                return Results.NotFound("Ingredient not found");
            }
            if (!await db.Units.AnyAsync(i => i.Id == dto.UnitId))
            {
                return Results.NotFound("Unit not found");
            }

            if (dto.QuantityInGrams <= 0 || dto.Quantity <= 0)
            {
                return Results.BadRequest("Quantity can't be 0");
            }
            
            
            var ingredient = await db.Ingredients.AsNoTracking().FirstOrDefaultAsync(i => i.Id == dto.IngredientId);
            Guid shoppingCategoryId = dto.ShoppingCategoryId ?? ingredient.ShoppingCategoryId;

            ShoppingListItem nSI = new ()
            {
                Id = Guid.NewGuid(),
                GroupId = groupDb.Id,
                IngredientId = dto.IngredientId,
                UnitId = dto.UnitId,
                Quantity = dto.Quantity,
                QuantityInGrams = dto.QuantityInGrams,
                ShoppingCategoryId = shoppingCategoryId,
                IsPurchased = false
            };

            db.ShoppingListItems.Add(nSI);
            await db.SaveChangesAsync();
            
            return Results.Created($"/inventory/shoppingListItems/getAll", new { id = nSI.Id });
        }).RequireAuthorization();
        
        //---------------------------------------------------------------Delete shopping item
        group.MapDelete("/shoppingListItems/delete/{id:guid}", async (Guid id, ClaimsPrincipal user, PinulaDbContext db) =>
        {
            var (authResult, userDb, groupDb) = await HelperFunctions.AuthUserInGroup(user, db);
            if (authResult != Results.Ok())
            {
                return authResult;
            }
            var item = await db.ShoppingListItems.FirstOrDefaultAsync(i => i.Id == id);
            if (item is null)
            {
                return Results.NotFound("Shopping list item not found");
            }

            db.ShoppingListItems.Remove(item);
            await db.SaveChangesAsync();

            return Results.NoContent();
        }).RequireAuthorization();
        
    }
}