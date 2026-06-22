using Pinula.Shared.Interface;
using Pinula.Shared.DTOs;
using System.Net.Http.Json;
using Pinula.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Pinula.Shared.Services
{
    public class UnitService : IUnitService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private const string BaseUrl = "units";

        public UnitService(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        public async Task<List<UnitPreviewDto>> GetAllServingUnitsAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<List<UnitPreviewDto>>($"{BaseUrl}/getServing");
            return response ?? new List<UnitPreviewDto>();
        }

        public async Task<List<UnitPreviewDto>> GetAllUnitsAsync()
        {

            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<UnitPreviewDto>>($"{BaseUrl}/get");
                return response ?? new List<UnitPreviewDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while getting all units: {ex.Message}");
                return new List<UnitPreviewDto>();
            }
        }

        public async Task<bool> CreateUnitAsync(Unit unit)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/create", unit);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while creating unit: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteUnitAsync(Guid unitId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/{unitId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Error while deleting unit: {error}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while deleting unit {unitId}: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Unit>> GetAllUnitsAdminAsync()
        {

            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<Unit>>($"{BaseUrl}/getAdmin");
                return response ?? new List<Unit>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while getting all units: {ex.Message}");
                return new List<Unit>();
            }
        }
    }
}
