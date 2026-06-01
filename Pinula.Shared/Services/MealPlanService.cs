using Microsoft.Extensions.Logging;
using Pinula.Shared.DTOs;
using Pinula.Shared.Interface;
using Pinula.Shared.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace Pinula.Shared.Services
{
    public class MealPlanService : IMealPlanService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private const string BaseUrl = "mealplan";

        public MealPlanService(HttpClient httpClient, ILogger logger)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<bool> AddRecipeToPlanAsync(CreateMealPlanDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/add", dto);

                if (response.IsSuccessStatusCode) return true;

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to add recipe to plan.Code: {response.StatusCode} error: {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while adding recipe to plan: {ex.Message}");
                return false;
            }
        }

        public async Task<List<MealPlanPreviewDto>?> GetMyGroupMealPlansAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<MealPlanPreviewDto>>($"{BaseUrl}/get?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}");
                return response ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while getting meal plans: {ex.Message}");
                return null;
            }

        }

        public async Task<bool> RemoveRecipeFromPlanAsync(Guid mealPlanId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/delete/{mealPlanId}");

                if (response.IsSuccessStatusCode) return true;

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to add recipe to plan.Code: {response.StatusCode} error: {error}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while deleting meal plan {mealPlanId}: {ex.Message}");
                return false;
            }
        }

        public async Task<GroupDetailDto?> CreateGroupAsync(CreateGroupDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/group/create", dto);
                if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<GroupDetailDto>();

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to create group: {response.StatusCode} error: {error}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while creating group: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> JoinGroupAsync(string code)
        {
            try
            {
                var trimmedCode = Uri.EscapeDataString(code.Trim());
                var response = await _httpClient.PostAsync($"{BaseUrl}/group/join/{trimmedCode}", null);

                if (response.IsSuccessStatusCode) return true;

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to join group: {response.StatusCode} error: {error}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while joining group: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RenameGroupAsync(string name)
        {
            try
            {
                var trimmedName = Uri.EscapeDataString(name.Trim());
                var response = await _httpClient.PostAsync($"{BaseUrl}/group/rename/{trimmedName}", null);

                if (response.IsSuccessStatusCode) return true;

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to rename group: {response.StatusCode} error: {error}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while renaming group: {ex.Message}");
                return false;
            }
        }

        public async Task<GroupDetailDto?> GetMyGroupAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/group/my");

                if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<GroupDetailDto>();

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"User group status: {response.StatusCode} error: {error}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while getting user group: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> LeaveGroupAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl}/group/leave", null);

                if (response.IsSuccessStatusCode) return true;

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to leave group: {response.StatusCode} error: {error}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while leaving group: {ex.Message}");
                return false;
            }
        }

        public async Task<List<UserDisplayDto>> GetMembersAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<UserDisplayDto>>($"{BaseUrl}/group/members");

                return response ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while getting group members: {ex.Message}");
                return new();
            }
        }

        public async Task<bool> UpdateMealPlanAsync(UpdateMealPlanDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/update/{dto.Id}", dto);
                if (response.IsSuccessStatusCode) return true;

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to update meal plan {response.StatusCode} error: {error}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while updating meal plan {dto.Id}: {ex.Message}");
                return false;
            }
        }

    }
}
