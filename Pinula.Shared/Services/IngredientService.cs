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
        public required string Message { get; set; }
    }

    public class IngredientService : IIngredientService
    {
        private readonly HttpClient _httpClient;
        private readonly OFFService _offService;
        private readonly ILogger<IngredientService> _logger;
        private readonly IUnitService _unitService;
        private readonly ILocalStorage _localStorage;
        private const string BaseUrl = "ingredients";

        public IngredientService(HttpClient httpClient, ILogger<IngredientService> logger, OFFService offservice, IUnitService unitService, ILocalStorage localStorage)
        {
            _httpClient = httpClient;
            _logger = logger;
            _offService = offservice;
            _unitService = unitService;
            _localStorage = localStorage;
        }

        public async Task<CreateIngredientResponse> CreateIngredientAsync(IngredientCreateDto? ingredientDto, string? barcode)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(barcode) && ingredientDto == null)
                {
                    var allLocalUnits = await _unitService.GetAllUnitsAsync();
                    var defaultUnit = allLocalUnits.FirstOrDefault(u => u.Code.ToLower() == "g") ?? allLocalUnits.FirstOrDefault();

                    if (defaultUnit == null)
                    {
                        return new CreateIngredientResponse { IsSuccess = false, Message = "No default units found" };
                    }

                    _logger.LogInformation($"Fetching heavy details for barcode {barcode} from OFF...");
                    ingredientDto = await _offService.GetFullIngredientDetailsAsync(barcode, defaultUnit.Id);

                    if (ingredientDto == null)
                    {
                        return new CreateIngredientResponse { IsSuccess = false, Message = "Failed to fetch product details from Open Food Facts." };
                    }
                }

                if (ingredientDto == null)
                {
                    return new CreateIngredientResponse { IsSuccess = false, Message = "No ingredient data provided." };
                }

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

        public async Task<List<IngredientPreviewDto>> GetFilteredIngredientPreviewsAsync(IngredientFilterParameters filter)
        {
            string languageCode = await _localStorage.GetStringAsync("culture") ?? "en";
            if (filter.Amount <= 0) filter.Amount = 20;

            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(filter.SearchTerm)}");

            if (!string.IsNullOrWhiteSpace(filter.Barcode))
                queryParams.Add($"barcode={Uri.EscapeDataString(filter.Barcode)}");

            if (filter.Amount > 0)
                queryParams.Add($"amount={filter.Amount}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var localUrl = $"{BaseUrl}/getFilteredPreviews{queryString}";

            var finalResults = new List<IngredientPreviewDto>();

            try
            {
                var localResponse = await _httpClient.GetFromJsonAsync<List<IngredientPreviewDto>>(localUrl);
                if (localResponse != null)
                {
                    finalResults.AddRange(localResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching local ingredients: {ex.Message}");
            }

            int remainingAmount = filter.Amount - finalResults.Count;

            if (remainingAmount > 0)
            {
                try
                {
                    var allLocalUnits = await _unitService.GetAllUnitsAsync();

                    var offFilter = new IngredientFilterParameters
                    {
                        SearchTerm = filter.SearchTerm,
                        Barcode = filter.Barcode,
                        Amount = remainingAmount
                    };

                  var offResults = await _offService.SearchPreviewsAsync(offFilter, allLocalUnits, languageCode);

                    finalResults.AddRange(offResults);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error fetching OFF ingredients: {ex.Message}");
                }
            }

            return finalResults.Take(filter.Amount).ToList();
        }
    }
}
