using System.Globalization;
using System.Security.Claims;
using Pinula.API.Context;
using Pinula.Shared.DTOs;

namespace Pinula.API.Endpoints;

public static class InventoryEndpoint
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/inventory");
        
        //---------------------------------------------------------------Get all inventory items
        group.MapGet("/getAll", async (HttpRequest request, ClaimsPrincipal user, PinulaDbContext db) =>
        {
            
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