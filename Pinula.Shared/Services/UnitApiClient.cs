using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Pinula.Shared.DTOs;
using Pinula.Shared.Interface;

namespace Pinula.Shared.Services
{
    public class UnitApiClient : IUnitApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UnitApiClient> _logger;
        private const string BaseUrl = "units";

        public UnitApiClient(HttpClient httpClient, ILogger<UnitApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<UnitPreviewDto>> GetAllServingUnitsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<UnitPreviewDto>>($"{BaseUrl}/getServing");
                return response ?? new List<UnitPreviewDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting serving units.");
                return new List<UnitPreviewDto>();
            }
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
                _logger.LogError(ex, "Error while getting all units.");
                return new List<UnitPreviewDto>();
            }
        }

        public async Task<List<UnitDto>> GetAllUnitsAdminAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<UnitDto>>($"{BaseUrl}/getAdmin");
                return response ?? new List<UnitDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting admin units.");
                return new List<UnitDto>();
            }
        }

        public async Task<GeneralResponse> CreateUnitAsync(UnitDto unit)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/create", unit);
                var result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
                return result ?? new GeneralResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating unit with code {Code}", unit.Code);
                return new GeneralResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }

        public async Task<GeneralResponse> DeleteUnitAsync(Guid unitId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/{unitId}");
                var result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
                return result ?? new GeneralResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting unit {UnitId}", unitId);
                return new GeneralResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }
    }
}