using Microsoft.Extensions.Logging;
using Pinula.Shared.Interface;
using Pinula.Shared.DTOs;
using Pinula.Shared.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;

namespace Pinula.Shared.Services
{
    public class RecipeService : IRecipeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RecipeService> _logger;
        private const string BaseUrl = "recipes";

        public RecipeService(HttpClient httpClient, ILogger<RecipeService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }


        public async Task<bool?> ChangeFavoriteAsync(Guid recipeId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl}/toggleFavorite/{recipeId}", null);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<FavoriteResponse>();
                    return result?.IsFavorite;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Toggle favorite error: {ex.Message}");
                return null;
            }
        }


        public async Task<bool> DeleteRecipeAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/delete/{id}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete recipe error: {ex.Message}");
                return false;
            }
        }

        public async Task<RecipeDetailsDto?> GetRecipeDetailsAsync(Guid recipeId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<RecipeDetailsDto>($"{BaseUrl}/getRecipeDetails/{recipeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while loading recipe details: {ex.Message}");
                return null;
            }
        }

        public async Task<Guid?> SaveRecipeAsync(RecipeCreateDto createDto, Stream? photoStream, string? photoName, string? contentType)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                var recipeJson = JsonSerializer.Serialize(createDto);
                content.Add(new StringContent(recipeJson, Encoding.UTF8, "application/json"), "recipeData");

                if (photoStream is not null && photoName is not null && contentType is not null)
                {
                    var fileContent = new StreamContent(photoStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    content.Add(fileContent, "image", photoName);
                }

                var response = await _httpClient.PostAsync($"{BaseUrl}/create", content);
                if (response.IsSuccessStatusCode)
                {
                    var guid = await response.Content.ReadFromJsonAsync<Guid>();
                    return guid;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while creating recipe: {ex.Message}");
                return null;
            }
        }

        public async Task<List<RecipePreviewDto>> GetFilteredRecipePreviewsAsync(RecipeFilterParameters filter, CancellationToken? ct)
        {
            try
            {
                var url = $"{BaseUrl}/getPreviews/filtered?" +
                          $"amount={filter.Amount}" +
                          $"&skip={filter.Skip}" +
                          $"&onlyFavorites={filter.OnlyFavorites.ToString().ToLower()}" +
                          $"&onlyMine={filter.OnlyMine.ToString().ToLower()}" +
                          $"&sort={(int)filter.Sort}" +
                          $"&sortDescending={filter.SortDescending.ToString().ToLower()}" +
                          $"&includeUnapproved={filter.IncludeUnapproved.ToString().ToLower()}";

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                    url += $"&searchTerm={Uri.EscapeDataString(filter.SearchTerm)}";

                if (filter.CategoryId.HasValue)
                    url += $"&categoryId={filter.CategoryId}";

                if (filter.MinRating.HasValue)
                    url += $"&minRating={filter.MinRating}";

                if (filter.MaxCookingTime.HasValue)
                    url += $"&maxCookingTime={filter.MaxCookingTime}";

                if (filter.MaxDifficulty.HasValue)
                    url += $"&maxDifficulty={filter.MaxDifficulty}";

                if (filter.MaxCalories.HasValue)
                    url += $"&maxCalories={filter.MaxCalories}";

                var response = await _httpClient.GetFromJsonAsync<List<RecipePreviewDto>>(url, ct ?? CancellationToken.None);
                return response ?? new();
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Request was cancelled by user.");
                return new();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Filter Error: {ex.Message}");
                return new();
            }
        }

        public async Task<PostCommentResponse?> PostCommentAsync(Comment comment)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync<Comment>($"{BaseUrl}/postComment", comment);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<PostCommentResponse>();
                }
                _logger.LogError($"Error while posting comment: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while posting comment: {ex.Message}");
                return null;
            }
        }

        public async Task<DeleteCommentResponse?> DeleteCommentAsync(Guid commentId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/deleteComment/{commentId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<DeleteCommentResponse>();
                }
                _logger.LogError($"Error while removing comment: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while removing comment: {ex.Message}");
                return null;
            }
        }

        public async Task<DeleteCommentResponse?> UpdateCommentAsync(Comment comment)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/updateComment/{comment.Id}", comment);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<DeleteCommentResponse>();
                }
                _logger.LogError($"Error while updating comment: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while updating comment: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> AdminToggleRecipeApprovalAsync(Guid recipeId)
        {
            var response = await _httpClient.PostAsync($"{BaseUrl}/admin/toggleApproval/{recipeId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<AdminCommentDto>> GetAdminCommentsAsync()
        {
            var comments =  await _httpClient.GetFromJsonAsync<List<AdminCommentDto>>($"{BaseUrl}/admin/allComments");
            return comments ?? new();
        }

        public async Task<bool> AdminToggleCommentApprovalAsync(Guid commentId)
        {
            var response = await _httpClient.PostAsync($"{BaseUrl}/admin/toggleCommentApproval/{commentId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<Guid?> UpdateRecipeAsync(RecipeCreateDto createDto, Guid recipeId, Stream? photoStream, string? photoName, string? contentType)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                var recipeJson = JsonSerializer.Serialize(createDto);
                content.Add(new StringContent(recipeJson, Encoding.UTF8, "application/json"), "recipeData");

                if (photoStream is not null && photoName is not null && contentType is not null)
                {
                    var fileContent = new StreamContent(photoStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    content.Add(fileContent, "image", photoName);
                }

                var response = await _httpClient.PutAsync($"{BaseUrl}/update/{recipeId}", content);

                if (response.IsSuccessStatusCode)
                {
                    var guid = await response.Content.ReadFromJsonAsync<Guid>();
                    return guid;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while updating recipe: {ex.Message}");
                return null;
            }
        }



        public class FavoriteResponse
        {
            public bool IsFavorite { get; set; }
        }
    }
}
