using Microsoft.Extensions.Logging;
using Pinula.Shared.DTOs;
using Pinula.Shared.Interface;
using Pinula.Shared.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Pinula.Shared.Services
{
    public class CategoryService : ICategoryService
    {

        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private const string BaseUrl = "categories";

        public CategoryService(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/getAll");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Category>>() ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"error while loading categories: {ex.Message}");
            }
            return new List<Category>();
        }

        public async Task<Category?> GetCategoryByIdAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/get/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Category>() ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"error while loading categories: {ex.Message}");
            }
            return new Category();
        }

        public Task<List<Category>> GetChildCategoriesAsync(Guid parentId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Category>> GetMainCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/getMain");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Category>>() ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"error while loading categories: {ex.Message}");
            }
            return new List<Category>();
        }

        public Task<List<Category>> GetRecepieCategoriesAsync(Guid recepieId)
        {
            throw new NotImplementedException();
        }

        public async Task<Guid?> SaveCategoryAsync(CategoryCreateDto dto, Stream? photoStream, string? photoName, string? contentType)
        {
            try
            {
                using var content = new MultipartFormDataContent();

                var json = JsonSerializer.Serialize(dto);
                content.Add(new StringContent(json, Encoding.UTF8, "application/json"), "categoryData");

                if (photoStream is not null && photoName is not null && contentType is not null)
                {
                    var fileContent = new StreamContent(photoStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    content.Add(fileContent, "image", photoName);
                }

                var response = await _httpClient.PostAsync($"{BaseUrl}/create", content);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Guid>();
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Category creation failed: {error}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while creating category: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteCategoryAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Category delete failed: {error}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while deleting category {id}: {ex.Message}");
                return false;
            }
        }
    }
}
