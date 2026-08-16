using System.Collections.Immutable;
using System.Globalization;
using System.Security.Claims;
using Mapster;
using Microsoft.AspNetCore.Http.HttpResults;
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
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            var ii = db.InventoryItems
                .Where(i => i.GroupId == groupDb.Id)
                .Include(i => i.Ingredient)
                .Include(i => i.Unit)
                .AdaptWithRequest<List<InventoryItemDisplayDto>>(request);

            return Results.Ok(ii);
        }).RequireAuthorization();
        
        //---------------------------------------------------------------Create inventory item
        group.MapPost("/create", async (HttpRequest request, InventoryItemCreateDto dto, ClaimsPrincipal user, PinulaDbContext db) =>
        {
            
        }).RequireAuthorization();
        
        //---------------------------------------------------------------Update inventory item
        group.MapPut("/update", async (HttpRequest request, InventoryItemUpdateDto dto, ClaimsPrincipal user, PinulaDbContext db) =>
        {
        
        }).RequireAuthorization();
        
        //---------------------------------------------------------------Delete inventory item
        group.MapDelete("/delete/{id:guid}", async (HttpRequest request,Guid id, ClaimsPrincipal user, PinulaDbContext db) =>
        {
            
        }).RequireAuthorization();
        
        
        
        
        
        //---------------------------------------------------------------Get all shopping items
        group.MapGet("/shoppingListItems/getAll", async (HttpRequest request, ClaimsPrincipal user, PinulaDbContext db) =>
        {
            
        }).RequireAuthorization();
        
        //---------------------------------------------------------------Create shopping item
        group.MapPost("/shoppingListItems/create", async (HttpRequest request, ShoppingItemCreateDto dto, ClaimsPrincipal user, PinulaDbContext db) =>
        {
            
        }).RequireAuthorization();
        
        //---------------------------------------------------------------Delete shopping item
        group.MapDelete("/shoppingListItems/delete/{id:guid}", async (HttpRequest request,Guid id, ClaimsPrincipal user, PinulaDbContext db) =>
        {
            
        }).RequireAuthorization();
        
    }
}