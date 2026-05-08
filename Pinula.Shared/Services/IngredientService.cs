using Microsoft.Extensions.Logging;
using Pinula.Shared.Interface;
using Pinula.Shared.DTOs;
using Pinula.Shared.Models;
using System.Net.Http.Json;

namespace Pinula.Shared.Services
{
    public class CreateIngredientResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    public class IngredientService : IIngredientService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private const string BaseUrl = "ingredients";

        public IngredientService(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<CreateIngredientResponse> CreateIngredientAsync(IngredientCreateDto ingredientDto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/create", ingredientDto);

                if (response.IsSuccessStatusCode)
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    return new CreateIngredientResponse { IsSuccess = true, Message = msg };
                }

                if (response.Content != null)
                {
                    try
                    {
                        var errorObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                        if (errorObj != null && errorObj.TryGetValue("message", out var apiMessage))
                        {
                            return new CreateIngredientResponse { IsSuccess = false, Message = apiMessage };
                        }
                    }
                    catch
                    {
                        return new CreateIngredientResponse { IsSuccess = false, Message = $"Error: {response.StatusCode}" };
                    }
                }

                return new CreateIngredientResponse { IsSuccess = false, Message = "Unknown error occurred" };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while creating ingredient: {ex.Message}");
                return new CreateIngredientResponse { IsSuccess = false, Message = "Connection to server failed." };
            }
        }

        public async Task<List<IngredientPreview>> GetIngredientPreviewsAsync(int amount)
        {
            var response = await _httpClient.GetFromJsonAsync<List<IngredientPreview>>($"{BaseUrl}/getPreviews?amount={amount}");
            return response ?? new List<IngredientPreview>();
        }


        public Task<Ingredient?> GetIngredientAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task RemoveIngredientAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task UpdateIngredientAsync(Ingredient ingredient)
        {
            throw new NotImplementedException();
        }

        public async Task<List<IngredientPreview>> GetFilteredIngredientPreviewsAsync(string searchTerm, int? amount)
        {
            var encodedSearch = Uri.EscapeDataString(searchTerm ?? string.Empty);

            var url = $"{BaseUrl}/getFilteredPreviews?searchTerm={encodedSearch}&amount={amount}";

            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<IngredientPreview>>(url);
                return response ?? new List<IngredientPreview>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching ingredients: {ex.Message}");
                return new List<IngredientPreview>();
            }
        }
    }
}
