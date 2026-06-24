using Microsoft.Extensions.Logging;
using Pinula.Shared.Interface;
using Pinula.Shared.DTOs;
using Pinula.Shared.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Pinula.Shared.Services
{
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorage _tokenStorage;
        private readonly ILogger<UserService> _logger;
        private const string BaseUrl = "users";

        public UserService(HttpClient httpClient, ILocalStorage tokenStorage, ILogger<UserService> logger)
        {
            _httpClient = httpClient;
            _tokenStorage = tokenStorage;
            _logger = logger;
        }

        public async Task<bool> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            try
            {
                var dto = new ChangePasswordDto { OldPassword = oldPassword, NewPassword = newPassword};
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/changePassword", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while changing psswd: {ex.Message}");
                return false;
            }
        }

        public async Task<UserDisplayDto?> GetCurrentUserAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/getMe");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogError("User not logged in");
                    return null;
                }

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserDisplayDto>();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Getting current user error: {ex.Message}");
                return null;
            }
        }

        public async Task<UserDisplayDto?> GetUserByIdAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/getUserDisplay/{userId}");

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<UserDisplayDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Getting user error: {ex.Message}");
                return null;
            }
        }

        public Task<bool> IsEmailRegistredAsync(string email)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> IsUserLoggedInAsync()
        {

            if (string.IsNullOrEmpty(await _tokenStorage.GetTokenAsync()))
            {
                return false;
            }
            return true;
        }

        public async Task<User?> LoginAsync(UserLoginDto loginDto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/login", loginDto);

                if (response.IsSuccessStatusCode)
                {
                    var authResult = await response.Content.ReadFromJsonAsync<LoginResponse>();

                    if (authResult != null)
                    {
                        await _tokenStorage.SaveTokenAsync(authResult.Token);

                        return authResult.User;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login Error: {ex.Message}");
                return null;
            }
        }

        public async void Logout()
        {
            await _tokenStorage.RemoveTokenAsync();
        }

        public async Task<bool> RegisterAsync(UserRegistrationDto userDto)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/register", userDto);
                return response.IsSuccessStatusCode;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                return false;
            }
            return false;
        }

        public Task RememberCurrentUserAsync(User user)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> UpdateUserAsync(UserUpdateDto? userUpdateDto, Stream? photoStream, string photoName, string contentType)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                var userJson = JsonSerializer.Serialize(userUpdateDto);
                content.Add(new StringContent(userJson, Encoding.UTF8, "application/json"), "userData");

                if (photoStream is not null && photoName is not null && contentType is not null)
                {
                    var fileContent = new StreamContent(photoStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    content.Add(fileContent, "image", photoName);
                }


                var response = await _httpClient.PutAsync($"{BaseUrl}/update", content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while updating user: {ex.Message}");
                return false;
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<User>>($"{BaseUrl}/admin/all");
                return response ?? new List<User>();
            }
            catch(Exception ex)
            {
                _logger.LogError($"Error while getting users: {ex.Message}");
                return new List<User>();
            }
        }

        public async Task<bool> AdminChangePasswordAsync(Guid userId, string newPassword)
        {
            try
            {
                var dto = new AdminPasswordChangeDto { UserId = userId, NewPassword = newPassword };
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/admin/changePassword", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while changing user psswd: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AdminToggleCommentPermissionAsync(Guid userId)
        {
            var response = await _httpClient.PostAsync($"{BaseUrl}/admin/toggleCommentPermission/{userId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AdminToggleRecipePermissionAsync(Guid userId)
        {
            var response = await _httpClient.PostAsync($"{BaseUrl}/admin/toggleRecipePermission/{userId}", null);
            return response.IsSuccessStatusCode;
        }

    }
}
