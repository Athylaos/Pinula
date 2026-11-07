using Microsoft.Extensions.Logging;
using Pinula.Shared.Interface;
using Pinula.Shared.DTOs;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Pinula.Shared.Services
{
    public class RecipeApiClient : IRecipeApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RecipeApiClient> _logger;
        private const string BaseUrl = "recipes";

        public RecipeApiClient(HttpClient httpClient, ILogger<RecipeApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<RecipePreviewDto>> GetFilteredRecipePreviewsAsync(RecipeFilterParameters filter, CancellationToken? ct = null)
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
                          $"&includeUnapproved={filter.IncludeUnapproved.ToString().ToLower()}" +
                          $"&includeAdminData={filter.IncludeAdminData.ToString().ToLower()}";

                if (!string.IsNullOrEmpty(filter.SearchTerm)) url += $"&searchTerm={Uri.EscapeDataString(filter.SearchTerm)}";
                if (filter.CategoryId.HasValue) url += $"&categoryId={filter.CategoryId}";
                if (filter.MinRating.HasValue) url += $"&minRating={filter.MinRating}";
                if (filter.MaxCookingTime.HasValue) url += $"&maxCookingTime={filter.MaxCookingTime}";
                if (filter.MaxDifficulty.HasValue) url += $"&maxDifficulty={filter.MaxDifficulty}";
                if (filter.MaxCalories.HasValue) url += $"&maxCalories={filter.MaxCalories}";

                var response = await _httpClient.GetFromJsonAsync<List<RecipePreviewDto>>(url, ct ?? CancellationToken.None);
                return response ?? new List<RecipePreviewDto>();
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("GetFilteredRecipePreviewsAsync was cancelled by user.");
                return new List<RecipePreviewDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching filtered recipe previews.");
                return new List<RecipePreviewDto>();
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
                _logger.LogError(ex, "Error while loading recipe details for {RecipeId}", recipeId);
                return null;
            }
        }

        public async Task<GeneralResponse> SaveRecipeAsync(RecipeCreateDto createDto, Stream? photoStream, string? photoName, string? contentType)
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
                var result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
                return result ?? new GeneralResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating recipe.");
                return new GeneralResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }

        public async Task<GeneralResponse> UpdateRecipeAsync(RecipeCreateDto createDto, Guid recipeId, Stream? photoStream, string? photoName, string? contentType)
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
                var result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
                return result ?? new GeneralResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating recipe {RecipeId}", recipeId);
                return new GeneralResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }

        public async Task<GeneralResponse> DeleteRecipeAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/delete/{id}");
                var result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
                return result ?? new GeneralResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recipe {RecipeId}", id);
                return new GeneralResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }

        public async Task<ToggleFavoriteResponse> ChangeFavoriteAsync(Guid recipeId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl}/toggleFavorite/{recipeId}", null);
                var result = await response.Content.ReadFromJsonAsync<ToggleFavoriteResponse>();
                return result ?? new ToggleFavoriteResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite for recipe {RecipeId}", recipeId);
                return new ToggleFavoriteResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }

        public async Task<CommentPostResponse> PostCommentAsync(CommentCreateDto comment)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/postComment", comment);
                var result = await response.Content.ReadFromJsonAsync<CommentPostResponse>();
                return result ?? new CommentPostResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while posting comment for recipe {RecipeId}", comment.RecipeId);
                return new CommentPostResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }

        public async Task<CommentDeleteResponse> UpdateCommentAsync(CommentCreateDto comment)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/updateComment/{comment.Id}", comment);
                var result = await response.Content.ReadFromJsonAsync<CommentDeleteResponse>();
                return result ?? new CommentDeleteResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating comment {CommentId}", comment.Id);
                return new CommentDeleteResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }

        public async Task<CommentDeleteResponse> DeleteCommentAsync(Guid commentId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/deleteComment/{commentId}");
                var result = await response.Content.ReadFromJsonAsync<CommentDeleteResponse>();
                return result ?? new CommentDeleteResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while removing comment {CommentId}", commentId);
                return new CommentDeleteResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }

        public async Task<List<AdminCommentDto>> GetAdminCommentsAsync()
        {
            try
            {
                var comments = await _httpClient.GetFromJsonAsync<List<AdminCommentDto>>($"{BaseUrl}/admin/allComments");
                return comments ?? new List<AdminCommentDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching admin comments");
                return new List<AdminCommentDto>();
            }
        }

        public async Task<ToggleBoolResponse> AdminToggleRecipeApprovalAsync(Guid recipeId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl}/admin/toggleApproval/{recipeId}", null);
                var result = await response.Content.ReadFromJsonAsync<ToggleBoolResponse>();
                return result ?? new ToggleBoolResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling approval for recipe {RecipeId}", recipeId);
                return new ToggleBoolResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }

        public async Task<ToggleBoolResponse> AdminToggleRecipeCheckedAsync(Guid recipeId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl}/admin/toggleChecked/{recipeId}", null);
                var result = await response.Content.ReadFromJsonAsync<ToggleBoolResponse>();
                return result ?? new ToggleBoolResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling checked status for recipe {RecipeId}", recipeId);
                return new ToggleBoolResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }

        public async Task<GeneralResponse> AdminToggleCommentApprovalAsync(Guid commentId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl}/admin/toggleCommentApproval/{commentId}", null);
                var result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
                return result ?? new GeneralResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling approval for comment {CommentId}", commentId);
                return new GeneralResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }
    }
}