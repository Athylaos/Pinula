using Microsoft.Extensions.Logging;
using Pinula.Shared.Interface;
using Pinula.Shared.DTOs;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Pinula.Shared.Services
{
    public class UserApiClient : IUserApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorage _tokenStorage;
        private readonly ILogger<UserApiClient> _logger;
        private const string BaseUrl = "users";

        public UserApiClient(HttpClient httpClient, ILocalStorage tokenStorage, ILogger<UserApiClient> logger)
        {
            _httpClient = httpClient;
            _tokenStorage = tokenStorage;
            _logger = logger;
        }

        public async Task<GeneralResponse> ChangePasswordAsync(PasswordChangeDto passwordDto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/changePassword", passwordDto);
                var result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
                return result ?? new GeneralResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while changing password");
                return new GeneralResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }

        public async Task<UserDisplayDto?> GetCurrentUserAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/getMe");
                if (response.StatusCode == HttpStatusCode.Unauthorized) return null;
                if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<UserDisplayDto>();
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return null;
            }
        }

        public async Task<UserDisplayDto?> GetUserByIdAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/getUserDisplay/{userId}");
                if (response.StatusCode == HttpStatusCode.NotFound) return null;
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<UserDisplayDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by id");
                return null;
            }
        }

        public async Task<bool> IsUserLoggedInAsync()
        {
            if (string.IsNullOrEmpty(await _tokenStorage.GetTokenAsync())) return false;
            try
            {
                var result = await _httpClient.GetAsync($"{BaseUrl}/verifyToken");
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying auth token");
                return false;
            }
        }

        public async Task<LoginResponse> LoginAsync(UserLoginDto loginDto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/login", loginDto);
                var authResult = await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (authResult != null && authResult.Successful)
                {
                    await _tokenStorage.SaveTokenAsync(authResult.Token);
                    return authResult;
                }
                
                return authResult ?? new LoginResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                return new LoginResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }

        public async void Logout()
        {
            await _tokenStorage.RemoveTokenAsync();
        }

        public async Task<GeneralResponse> RegisterAsync(UserRegistrationDto userDto)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/register", userDto, cts.Token);
                var result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
                return result ?? new GeneralResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration error");
                return new GeneralResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }

        public async Task<GeneralResponse> UpdateUserAsync(UserUpdateDto? userUpdateDto, Stream? photoStream, string? photoName, string? contentType)
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
                var result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
                return result ?? new GeneralResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating user");
                return new GeneralResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }

        public async Task<List<AdminUserDisplayDto>> AdminGetAllUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<AdminUserDisplayDto>>($"{BaseUrl}/admin/all");
                return response ?? new List<AdminUserDisplayDto>();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error while getting users");
                return new List<AdminUserDisplayDto>();
            }
        }

        public async Task<GeneralResponse> AdminChangePasswordAsync(Guid userId, string newPassword)
        {
            try
            {
                var dto = new AdminPasswordChangeDto { UserId = userId, NewPassword = newPassword };
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/admin/changePassword", dto);
                var result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
                return result ?? new GeneralResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while changing user password as admin");
                return new GeneralResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }

        public async Task<ToggleBoolResponse> AdminToggleCommentPermissionAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl}/admin/toggleCommentPermission/{userId}", null);
                var result = await response.Content.ReadFromJsonAsync<ToggleBoolResponse>();
                return result ?? new ToggleBoolResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling comment permission for {UserId}", userId);
                return new ToggleBoolResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }

        public async Task<ToggleBoolResponse> AdminToggleRecipePermissionAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl}/admin/toggleRecipePermission/{userId}", null);
                var result = await response.Content.ReadFromJsonAsync<ToggleBoolResponse>();
                return result ?? new ToggleBoolResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling recipe permission for {UserId}", userId);
                return new ToggleBoolResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }
        
        public async Task<GeneralResponse> SendCodeRequest(VerificationCodeRequestDto requestDto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/sendVerificationCode", requestDto);
                var result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
                return result ?? new GeneralResponse { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendCodeRequest Error");
                return new GeneralResponse { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }

        public async Task<VerificationCodeResponseDto> VerifyCodeRequest(VerificationCodeVerifyDto verifyDto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/verifyCode", verifyDto);
                var result = await response.Content.ReadFromJsonAsync<VerificationCodeResponseDto>();
                return result ?? new VerificationCodeResponseDto { Successful = false, Message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VerifyCodeRequest Error");
                return new VerificationCodeResponseDto { Successful = false, Message = $"Client error: {ex.Message}" };
            }
        }
        
        public async Task<bool> IsEmailAvailable(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var encodedEmail = Uri.EscapeDataString(email.Trim().ToLower());
                var response = await _httpClient.GetAsync($"{BaseUrl}/checkEmail?email={encodedEmail}");
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IsEmailRegistered Error");
                return false; 
            }
        }
    }
}