
using System.Globalization;
using System.Security.Claims;
using Pinula.API.Interface;
using Pinula.Shared.DTOs;

namespace Pinula.API.Endpoints;

public static class InventoryEndpoint
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/inventory").RequireRateLimiting("GeneralApiPolicy");
        
        
        //---------------------------------------------------------------Get all inventory items
        group.MapGet("/getAll", async (HttpRequest request, ClaimsPrincipal user, IInventoryBackendService inventoryService) =>
        {
            var (authResult, groupId) = await inventoryService.ValidateAuthUserInGroup(user);
            if (authResult != Results.Ok()) return authResult;

            string lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            string baseUrl = $"{request.Scheme}://{request.Host}";

            var result = await inventoryService.GetAllInventoryItemsAsync(groupId, baseUrl, lang);
            return Results.Ok(result);
        }).RequireAuthorization();
        
        //---------------------------------------------------------------Create inventory item
        group.MapPost("/create", async (InventoryItemCreateDto dto, ClaimsPrincipal user, IInventoryBackendService inventoryService) =>
        {
            var (authResult, groupId) = await inventoryService.ValidateAuthUserInGroup(user);
            if (authResult != Results.Ok()) return authResult;

            var result = await inventoryService.CreateInventoryItemAsync(groupId, dto);
    
            return result.StatusCode == 201 
                ? Results.Created("/inventory/getAll", result) 
                : Results.Json(result, statusCode: result.StatusCode);
        }).RequireAuthorization();
        
        //---------------------------------------------------------------Update inventory item
        group.MapPut("/update", async (InventoryItemUpdateDto dto, ClaimsPrincipal user, IInventoryBackendService inventoryService) =>
        {
            var (authResult, groupId) = await inventoryService.ValidateAuthUserInGroup(user);
            if (authResult != Results.Ok()) return authResult;

            var result = await inventoryService.UpdateInventoryItemAsync(groupId, dto);
            return Results.Json(result, statusCode: result.StatusCode);
        }).RequireAuthorization();
        
        //---------------------------------------------------------------Delete inventory item
        group.MapDelete("/delete/{id:guid}", async (Guid id, ClaimsPrincipal user, IInventoryBackendService inventoryService) =>
        {
            var (authResult, groupId) = await inventoryService.ValidateAuthUserInGroup(user);
            if (authResult != Results.Ok()) return authResult;

            var result = await inventoryService.DeleteInventoryItemAsync(groupId, id);
            return Results.Json(result, statusCode: result.StatusCode);
        }).RequireAuthorization();
        
        
        //---------------------------------------------------------------Get all shopping items
        group.MapGet("/shoppingListItems/getAll", async (HttpRequest request, ClaimsPrincipal user, IInventoryBackendService inventoryService) =>
        {
            var (authResult, groupId) = await inventoryService.ValidateAuthUserInGroup(user);
            if (authResult != Results.Ok()) return authResult;

            string lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            string baseUrl = $"{request.Scheme}://{request.Host}";

            var result = await inventoryService.GetAllShoppingListItemsAsync(groupId, baseUrl, lang);
            return Results.Ok(result);
        }).RequireAuthorization();
        
        //---------------------------------------------------------------Create shopping item
        group.MapPost("/shoppingListItems/create", async (ShoppingItemCreateDto dto, ClaimsPrincipal user, IInventoryBackendService inventoryService) =>
        {
            var (authResult, groupId) = await inventoryService.ValidateAuthUserInGroup(user);
            if (authResult != Results.Ok()) return authResult;

            var result = await inventoryService.CreateShoppingListItemAsync(groupId, dto);
    
            return result.StatusCode == 201 
                ? Results.Created("/inventory/shoppingListItems/getAll", result) 
                : Results.Json(result, statusCode: result.StatusCode);
        }).RequireAuthorization();
        
        //---------------------------------------------------------------Delete shopping item
        group.MapDelete("/shoppingListItems/delete/{id:guid}", async (Guid id, ClaimsPrincipal user, IInventoryBackendService inventoryService) =>
        {
            var (authResult, groupId) = await inventoryService.ValidateAuthUserInGroup(user);
            if (authResult != Results.Ok()) return authResult;

            var result = await inventoryService.DeleteShoppingListItemAsync(groupId, id);
            return Results.Json(result, statusCode: result.StatusCode);
        }).RequireAuthorization();
        
        //---------------------------------------------------------------Change purchased
        group.MapPut("/shoppingListItems/changePurchased/{id:guid}", async (Guid id, ClaimsPrincipal user, IInventoryBackendService inventoryService) =>
        {
            var (authResult, groupId) = await inventoryService.ValidateAuthUserInGroup(user);
            if (authResult != Results.Ok()) return authResult;

            var result = await inventoryService.ToggleShoppingListItemPurchasedAsync(groupId, id);
            return Results.Json(result, statusCode: result.StatusCode);
        }).RequireAuthorization();
        
        //---------------------------------------------------------------Make inventory items from shopping items
        group.MapPost("/shoppingListItemsToPantry", async (List<InventoryItemCreateDto> inventoryItems, ClaimsPrincipal user, IInventoryBackendService inventoryService) =>
        {
            var (authResult, groupId) = await inventoryService.ValidateAuthUserInGroup(user);
            if (authResult != Results.Ok()) return authResult;

            var result = await inventoryService.ConvertShoppingItemsToPantryAsync(groupId, inventoryItems);
    
            return result.StatusCode == 201 
                ? Results.Created("/inventory/getAll", result) 
                : Results.Json(result, statusCode: result.StatusCode);
        }).RequireAuthorization();
        
    }
}