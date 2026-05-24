using Pinula.Shared.DTOs;
using Pinula.Shared.Models;

namespace Pinula.Shared.Interface
{
    public interface IUserService
    {
        Task<bool> RegisterAsync(UserRegistrationDto user);
        Task<User?> LoginAsync(UserLoginDto loginDto);
        void Logout();

        Task<UserDisplayDto?> GetCurrentUserAsync();
        Task RememberCurrentUserAsync(User user);
        Task<bool> IsUserLoggedInAsync();

        Task<UserDisplayDto?> GetUserByIdAsync(Guid userId);
        Task<bool> UpdateUserAsync(UserUpdateDto userUpdateDto, Stream? photoStream, string photoName, string contentType);
        Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);

        Task<bool> IsEmailRegistredAsync(string email);

        Task<List<User>> GetAllUsersAsync();
        Task<bool> AdminChangePasswordAsync(Guid userId, string newPassword);
        Task<bool> AdminToggleCommentPermissionAsync(Guid userId);
        Task<bool> AdminToggleRecipePermissionAsync(Guid userId);
    }
}
