using Pinula.Shared.DTOs;

namespace Pinula.Shared.Interface
{
    public interface IUserService
    {
        Task<bool> RegisterAsync(UserRegistrationDto user);
        Task<bool> LoginAsync(UserLoginDto loginDto);
        void Logout();

        Task<UserDisplayDto?> GetCurrentUserAsync();
        Task<bool> IsUserLoggedInAsync();

        Task<UserDisplayDto?> GetUserByIdAsync(Guid userId);
        Task<bool> UpdateUserAsync(UserUpdateDto userUpdateDto, Stream? photoStream, string photoName, string contentType);
        Task<bool> ChangePasswordAsync(string oldPassword, string newPassword);


        Task<List<AdminUserDisplayDto>> AdminGetAllUsersAsync();
        Task<bool> AdminChangePasswordAsync(Guid userId, string newPassword);
        Task<bool> AdminToggleCommentPermissionAsync(Guid userId);
        Task<bool> AdminToggleRecipePermissionAsync(Guid userId);
    }
}
