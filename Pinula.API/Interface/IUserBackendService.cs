using Pinula.Shared.DTOs;

namespace Pinula.API.Interface;

public interface IUserBackendService
{
    Task<bool> VerifyTokenAsync(Guid userId);
    Task<bool> CheckEmailAvailableAsync(string email);
    Task<GeneralResponse> RegisterAsync(UserRegistrationDto dto);
    Task<LoginResponse> LoginAsync(UserLoginDto dto);
    Task<UserDisplayDto?> GetMeAsync(Guid userId, string baseUrl);
    Task<UserDisplayDto?> GetUserDisplayAsync(Guid userId, string baseUrl);
    Task<GeneralResponse> UpdateUserAsync(Guid userId, UserUpdateDto dto, IFormFile? imageFile);
    Task<GeneralResponse> ChangePasswordAsync(PasswordChangeDto dto);
    Task<List<AdminUserDisplayDto>> AdminGetAllUsersAsync();
    Task<GeneralResponse> AdminChangePasswordAsync(AdminPasswordChangeDto dto);
    Task<ToggleBoolResponse> AdminToggleCommentPermissionAsync(Guid userId);
    Task<ToggleBoolResponse> AdminToggleRecipePermissionAsync(Guid userId);
    Task<GeneralResponse> SendVerificationCodeAsync(VerificationCodeRequestDto dto, string lang);
    Task<VerificationCodeResponseDto> VerifyCodeAsync(VerificationCodeVerifyDto dto);
    
}