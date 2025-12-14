using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.ViewModels;
using System.Linq;

namespace JohnHenryFashionWeb.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public UserManagementService(
            UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager, 
            ApplicationDbContext context,
            IAuditLogService auditLogService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<UserManagementViewModel> GetUsersAsync(UserFilterViewModel filter)
        {
            var query = _userManager.Users.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(u => 
                    (u.FirstName != null && u.FirstName.ToLower().Contains(searchTerm)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(searchTerm)) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchTerm)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm)));
            }

            // Apply role filter
            if (!string.IsNullOrEmpty(filter.Role))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(filter.Role);
                var userIds = usersInRole.Select(u => u.Id).ToList();
                query = query.Where(u => userIds.Contains(u.Id));
            }

            // Apply status filter
            if (filter.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == filter.IsActive.Value);
            }

            // Apply date range filter
            if (filter.CreatedFrom.HasValue)
            {
                query = query.Where(u => u.CreatedAt >= filter.CreatedFrom.Value);
            }
            if (filter.CreatedTo.HasValue)
            {
                query = query.Where(u => u.CreatedAt <= filter.CreatedTo.Value);
            }

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "name" => filter.SortDirection == "desc" ? 
                    query.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName) :
                    query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),
                "email" => filter.SortDirection == "desc" ? 
                    query.OrderByDescending(u => u.Email) : 
                    query.OrderBy(u => u.Email),
                "createdat" => filter.SortDirection == "desc" ? 
                    query.OrderByDescending(u => u.CreatedAt) : 
                    query.OrderBy(u => u.CreatedAt),
                _ => query.OrderByDescending(u => u.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var users = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var userViewModels = new List<UserListItemViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                
                // Get order statistics for this user
                var orderCount = await _context.Orders.CountAsync(o => o.UserId == user.Id);
                var totalSpent = await _context.Orders
                    .Where(o => o.UserId == user.Id && (o.Status == "Completed" || o.Status == "completed" || o.Status == "delivered"))
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
                
                // Check if user is locked out
                var isLockedOut = await _userManager.IsLockedOutAsync(user);
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                
                userViewModels.Add(new UserListItemViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Avatar = user.Avatar, // Thêm avatar
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginDate = user.LastLoginDate, // Đã có
                    Roles = roles.ToList(),
                    OrderCount = orderCount, // Thêm số đơn hàng
                    TotalSpent = totalSpent, // Thêm tổng chi tiêu
                    IsLockedOut = isLockedOut, // Thêm thông tin lockout
                    LockoutEnd = lockoutEnd?.DateTime // Thêm thời gian lockout kết thúc
                });
            }

            // Get statistics
            var totalUsers = await _userManager.Users.CountAsync();
            var activeUsers = await _userManager.Users.CountAsync(u => u.IsActive);
            var newUsersThisMonth = await _userManager.Users
                .CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30));
            
            // Count sellers
            var sellersInRole = await _userManager.GetUsersInRoleAsync("Seller");
            var sellersCount = sellersInRole.Count;

            return new UserManagementViewModel
            {
                Users = userViewModels,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize),
                CurrentPage = filter.Page,
                PageSize = filter.PageSize,
                SearchTerm = filter.SearchTerm,
                RoleFilter = filter.Role,
                StatusFilter = filter.IsActive?.ToString() ?? string.Empty,
                SortBy = filter.SortBy ?? string.Empty,
                Filter = filter,
                AvailableRoles = _roleManager.Roles.Select(r => r.Name).Where(n => n != null).Cast<string>().ToList(),
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                NewUsersThisMonth = newUsersThisMonth,
                SellersCount = sellersCount // Thêm số sellers
            };
        }

        public async Task<UserDetailViewModel> GetUserDetailAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) 
                return new UserDetailViewModel(); // Return empty model instead of null

            var roles = await _userManager.GetRolesAsync(user);
            var recentOrders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();

            var orderCount = await _context.Orders.CountAsync(o => o.UserId == userId);
            var totalSpent = await _context.Orders
                .Where(o => o.UserId == userId && o.PaymentStatus == "paid" && o.Status != "cancelled")
                .SumAsync(o => o.TotalAmount);

            // Check lockout status
            var isLockedOut = await _userManager.IsLockedOutAsync(user);
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            var failedAttempts = await _userManager.GetAccessFailedCountAsync(user);

            return new UserDetailViewModel
            {
                User = user,
                Roles = roles.ToList(),
                RecentOrders = recentOrders,
                OrderCount = orderCount,
                TotalSpent = totalSpent,
                JoinDate = user.CreatedAt,
                LastActivity = user.LastLoginDate,
                IsLockedOut = isLockedOut,
                LockoutEnd = lockoutEnd?.DateTime,
                FailedLoginAttempts = failedAttempts
            };
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<UserEditViewModel> GetUserForEditAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new UserEditViewModel();

            var roles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.ToListAsync();

            return new UserEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                IsActive = user.IsActive,
                SelectedRoles = roles.ToList(),
                AvailableRoles = allRoles.Select(r => r.Name).Where(n => n != null).Cast<string>().ToList()
            };
        }

        public async Task<bool> UpdateUserAsync(string userId, UserEditViewModel model)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                var oldData = $"Email: {user.Email}, Name: {user.FirstName} {user.LastName}, Status: {user.IsActive}";

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.IsActive = model.IsActive;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    var newData = $"Email: {user.Email}, Name: {user.FirstName} {user.LastName}, Status: {user.IsActive}";
                    await _auditLogService.LogAdminActionAsync(
                        "SYSTEM", // In real app, this would be current admin user ID
                        "UPDATE_USER",
                        userId,
                        $"Updated user profile. Old: {oldData}, New: {newData}"
                    );
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ToggleUserStatusAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                var oldStatus = user.IsActive;
                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    await _auditLogService.LogAdminActionAsync(
                        "SYSTEM",
                        "TOGGLE_STATUS",
                        userId,
                        $"Changed user status from {oldStatus} to {user.IsActive}"
                    );
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                var userData = $"Email: {user.Email}, Name: {user.FirstName} {user.LastName}";
                var result = await _userManager.DeleteAsync(user);
                
                if (result.Succeeded)
                {
                    await _auditLogService.LogAdminActionAsync(
                        "SYSTEM",
                        "DELETE_USER",
                        userId,
                        $"Deleted user: {userData}"
                    );
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UnlockUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                // Unlock the account by setting lockout end date to null
                var unlockResult = await _userManager.SetLockoutEndDateAsync(user, null);
                if (!unlockResult.Succeeded) return false;

                // Reset failed access attempts count
                var resetResult = await _userManager.ResetAccessFailedCountAsync(user);
                if (!resetResult.Succeeded) return false;

                // Log the unlock action
                await _auditLogService.LogAdminActionAsync(
                    "SYSTEM",
                    "UNLOCK_USER",
                    userId,
                    $"Unlocked user account: {user.Email}"
                );

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<UserStatsViewModel> GetUserStatsAsync()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var activeUsers = await _userManager.Users.CountAsync(u => u.IsActive);
            var inactiveUsers = totalUsers - activeUsers;
            var newUsersThisMonth = await _userManager.Users
                .CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30));

            return new UserStatsViewModel
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers,
                NewUsersThisMonth = newUsersThisMonth
            };
        }

        public async Task<List<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<string>();

            var roles = await _userManager.GetRolesAsync(user);
            return roles.ToList();
        }

        public async Task<bool> UpdateUserRolesAsync(string userId, List<string> roles)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                var currentRoles = await _userManager.GetRolesAsync(user);
                var oldRoles = string.Join(", ", currentRoles);

                // Remove current roles
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded) return false;

                // Add new roles
                var addResult = await _userManager.AddToRolesAsync(user, roles);
                if (addResult.Succeeded)
                {
                    var newRoles = string.Join(", ", roles);
                    await _auditLogService.LogAdminActionAsync(
                        "SYSTEM",
                        "UPDATE_ROLES",
                        userId,
                        $"Changed user roles from [{oldRoles}] to [{newRoles}]"
                    );
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ResetUserPasswordAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                // Generate a temporary password
                var tempPassword = GenerateTemporaryPassword();
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, resetToken, tempPassword);

                if (result.Succeeded)
                {
                    await _auditLogService.LogAdminActionAsync(
                        "SYSTEM",
                        "RESET_PASSWORD",
                        userId,
                        "Password reset by admin"
                    );
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<string, int>> GetUserStatisticsAsync()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var activeUsers = await _userManager.Users.CountAsync(u => u.IsActive);
            var inactiveUsers = totalUsers - activeUsers;
            var newUsersThisMonth = await _userManager.Users
                .CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30));

            return new Dictionary<string, int>
            {
                ["TotalUsers"] = totalUsers,
                ["ActiveUsers"] = activeUsers,
                ["InactiveUsers"] = inactiveUsers,
                ["NewUsersThisMonth"] = newUsersThisMonth
            };
        }

        public async Task<List<ApplicationUser>> GetRecentUsersAsync(int count = 10)
        {
            return await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        private string GenerateTemporaryPassword()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}