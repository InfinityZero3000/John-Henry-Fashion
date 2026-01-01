using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.ViewModels;

namespace JohnHenryFashionWeb.Services
{
    public interface IUserManagementService
    {
        Task<UserManagementViewModel> GetUsersAsync(UserFilterViewModel filter);
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<bool> ToggleUserStatusAsync(string userId);
        Task<bool> ResetUserPasswordAsync(string userId);
        Task<List<string>> GetUserRolesAsync(string userId);
        Task<bool> UpdateUserRolesAsync(string userId, List<string> roles);
        Task<UserDetailViewModel> GetUserDetailAsync(string userId);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> UnlockUserAsync(string userId);
        Task<Dictionary<string, int>> GetUserStatisticsAsync();
        Task<List<ApplicationUser>> GetRecentUsersAsync(int count = 10);
    }
}