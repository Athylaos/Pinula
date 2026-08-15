using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Pinula.Shared.DTOs;
using Pinula.Shared.Interface;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Pinula.Shared.Services
{
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

        public async Task<GeneralResponse> CreateIngredientAsync(IngredientCreateDto? ingredientDto, string? barcode, Stream? photoStream, string? photoName, string? contentType)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(barcode) && ingredientDto == null)
                {
                    var allLocalUnits = await _unitService.GetAllUnitsAsync();
                    var defaultUnit = allLocalUnits.FirstOrDefault(u => u.Code.ToLower() == "g") ?? allLocalUnits.FirstOrDefault();

                    if (defaultUnit == null)
                    {
                        return new GeneralResponse { Successful = false, Message = "No default units found" };
                    }

                    _logger.LogInformation($"Fetching heavy details for barcode {barcode} from OFF");
                    ingredientDto = await _offService.GetFullIngredientDetailsAsync(barcode, defaultUnit.Id);

                    if (ingredientDto == null)
                    {
                        return new GeneralResponse { Successful = false, Message = "Failed to fetch product details from Open Food Facts." };
                    }
                }

                if (ingredientDto == null)
                {
                    return new GeneralResponse { Successful = false, Message = "No ingredient data provided." };
                }


                using var content = new MultipartFormDataContent();
                var recipeJson = JsonSerializer.Serialize(ingredientDto);
                content.Add(new StringContent(recipeJson, Encoding.UTF8, "application/json"), "ingredientData");

                if (photoStream is not null && photoName is not null && contentType is not null)
                {
                    var fileContent = new StreamContent(photoStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    content.Add(fileContent, "image", photoName);
                }

                var response = await _httpClient.PostAsync($"{BaseUrl}/create", content);

                if (response.IsSuccessStatusCode)
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    return new GeneralResponse { Successful = true, Message = msg };
                }
                else
                {
                    return new GeneralResponse { Successful = false, StatusCode = (int)response.StatusCode };
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while creating ingredient: {ex.Message}");
                return new GeneralResponse { Successful = false, Message = "Connection to server failed." };
            }
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

        public async Task<List<AdminIngredientPreviewDto>> AdminGetIngredients(int amount, int skip)
        {
            try
            {
                var url = $"{BaseUrl}/getAdminPreviews?amount={amount}&skip={skip}";
                var response = await _httpClient.GetFromJsonAsync<List<AdminIngredientPreviewDto>>(url);
                if (response is null) return new List<AdminIngredientPreviewDto>();
                return response.ToList();

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching ingredients: {ex.Message}");
                return new List<AdminIngredientPreviewDto>();
            }

        }

        public async Task<AdminIngredientDisplayDto?> AdminGetIngredientDetailsAsync(Guid id)
        {
            try
            {
                var url = $"{BaseUrl}/getAdmin/{id}";
                var response = await _httpClient.GetFromJsonAsync<AdminIngredientDisplayDto>(url);
                if(response is null) return null;
                return response;

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching ingredients: {ex.Message}");
                return null;
            }
        }

        public async Task<GeneralResponse> AdminUpdateIngredientAsync(IngredientCreateDto ingredient, Stream? photoStream, string? photoName, string? contentType)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                var recipeJson = JsonSerializer.Serialize(ingredient);
                content.Add(new StringContent(recipeJson, Encoding.UTF8, "application/json"), "ingredientData");

                if (photoStream is not null && photoName is not null && contentType is not null)
                {
                    var fileContent = new StreamContent(photoStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    content.Add(fileContent, "image", photoName);
                }

                var response = await _httpClient.PutAsync($"{BaseUrl}/updateAdmin", content);

                if (response.IsSuccessStatusCode)
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    return new GeneralResponse { Successful = true, Message = msg };
                }
                else
                {
                    return new GeneralResponse { Successful = false, StatusCode = (int)response.StatusCode };
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while creating ingredient: {ex.Message}");
                return new GeneralResponse { Successful = false, Message = "Connection to server failed." };
            }
        }

        public async Task<GeneralResponse> DeleteIngredientAsync(Guid id)
        {
            try
            {
                var url = $"{BaseUrl}/deleteAdmin/{id}";
                var response = await _httpClient.DeleteAsync(url);

                if (response is null)
                {
                    return new GeneralResponse() { StatusCode = (int)HttpStatusCode.NotFound, Successful = false, Message="Api endpoint not found" };
                }
                else
                {
                    return await response.Content.ReadFromJsonAsync<GeneralResponse>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching ingredients: {ex.Message}");
                return new GeneralResponse() { StatusCode = (int)HttpStatusCode.InternalServerError, Successful = false, Message = ex.Message }; ;
            }
        }

        public async Task<bool> AdminToggleIngredientApprovalAsync(Guid id)
        {
            var response = await _httpClient.PostAsync($"{BaseUrl}/admin/toggleApproval/{id}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AdminToggleIngredientCheckedAsync(Guid id)
        {
            var response = await _httpClient.PostAsync($"{BaseUrl}/admin/toggleChecked/{id}", null);
            return response.IsSuccessStatusCode;
        }
    }
}
