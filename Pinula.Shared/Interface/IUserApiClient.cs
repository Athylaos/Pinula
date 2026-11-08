using Pinula.Shared.DTOs;

namespace Pinula.Shared.Interface
{
    public interface IUserApiClient
    {
        Task<GeneralResponse> ChangePasswordAsync(PasswordChangeDto passwordDto);
        Task<UserDisplayDto?> GetCurrentUserAsync();
        Task<UserDisplayDto?> GetUserByIdAsync(Guid userId);
        Task<bool> IsUserLoggedInAsync();
        Task<LoginResponse> LoginAsync(UserLoginDto loginDto);
        void Logout();
        Task<GeneralResponse> RegisterAsync(UserRegistrationDto userDto);
        Task<GeneralResponse> UpdateUserAsync(UserUpdateDto? userUpdateDto, Stream? photoStream, string? photoName, string? contentType);
        Task<List<AdminUserDisplayDto>> AdminGetAllUsersAsync();
        Task<GeneralResponse> AdminChangePasswordAsync(Guid userId, string newPassword);
        Task<ToggleBoolResponse> AdminToggleCommentPermissionAsync(Guid userId);
        Task<ToggleBoolResponse> AdminToggleRecipePermissionAsync(Guid userId);
        Task<GeneralResponse> SendCodeRequest(VerificationCodeRequestDto requestDto);
        Task<VerificationCodeResponseDto> VerifyCodeRequest(VerificationCodeVerifyDto verifyDto);
        Task<bool> IsEmailAvailable(string email);
    }
}
