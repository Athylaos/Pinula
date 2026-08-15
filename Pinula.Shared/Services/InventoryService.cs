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

    public Task<List<InventoryItemDisplayDto>> GetAllInventoryItemsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreateInventoryItemAsync(InventoryItemCreateDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateInventoryItemAsync(InventoryItemUpdateDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteInventoryItemAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<List<ShoppingItemDisplayDto>> GetAllShoppingItemsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreateShoppingItemAsync(ShoppingItemCreateDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteShoppingItemAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}