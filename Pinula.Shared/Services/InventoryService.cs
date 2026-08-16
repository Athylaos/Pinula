using System.Net.Http.Json;
using Pinula.Shared.Interface;
using Microsoft.Extensions.Logging;
using Pinula.Shared.DTOs;

namespace Pinula.Shared.Services;


public class InventoryService : IInventoryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryService> _logger;
    private const string BaseUrl = "inventory";

    public InventoryService(HttpClient httpClient, ILogger<InventoryService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<InventoryItemDisplayDto>> GetAllInventoryItemsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/getAll");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<InventoryItemDisplayDto>>();
                return result ?? new();
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to load inventory items. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorBody);

            return new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while loading inventory items");
            return new();
        }
    }

    public async Task<bool> CreateInventoryItemAsync(InventoryItemCreateDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/create",dto);

            if (response.IsSuccessStatusCode) return true;
            
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to create inventory items. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating inventory item");
            return false;
        }
    }

    public async Task<bool> UpdateInventoryItemAsync(InventoryItemUpdateDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/update",dto);

            if (response.IsSuccessStatusCode) return true;
            
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to update inventory items. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating inventory item");
            return false;
        }
    }

    public async Task<bool> DeleteInventoryItemAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/delete/{id}");

            if (response.IsSuccessStatusCode) return true;
            
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to delete inventory item. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting inventory item");
            return false;
        }
    }

    public async Task<List<ShoppingItemDisplayDto>> GetAllShoppingItemsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/shoppingListItems/getAll");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<ShoppingItemDisplayDto>>();
                return result ?? new();
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to load shopping list items. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorBody);

            return new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while loading shopping list items");
            return new();
        }
    }

    public async Task<bool> CreateShoppingItemAsync(ShoppingItemCreateDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/shoppingListItems/create",dto);

            if (response.IsSuccessStatusCode) return true;
            
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to create shopping list item. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating shopping list item");
            return false;
        }
    }

    public async Task<bool> DeleteShoppingItemAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/shoppingListItems/delete/{id}");

            if (response.IsSuccessStatusCode) return true;
            
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to delete shopping list item. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting shopping list item");
            return false;
        }
    }
}