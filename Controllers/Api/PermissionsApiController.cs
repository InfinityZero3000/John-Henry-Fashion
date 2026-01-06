using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;

namespace JohnHenryFashionWeb.Controllers.Api
{
    [ApiController]
    [Route("api/admin/permissions")]
    [Authorize(AuthenticationSchemes = "Identity.Application,Bearer", Roles = "Admin")]
    public class PermissionsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PermissionsApiController> _logger;

        public PermissionsApiController(
            ApplicationDbContext context,
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            ILogger<PermissionsApiController> logger)
        {
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get all roles
        /// </summary>
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _roleManager.Roles
                    .Select(r => new
                    {
                        id = r.Id,
                        name = r.Name,
                        normalizedName = r.NormalizedName
                    })
                    .ToListAsync();

                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles");
                return StatusCode(500, new { error = "Failed to get roles" });
            }
        }

        /// <summary>
        /// Get permissions for a specific role
        /// </summary>
        [HttpGet("roles/{roleName}")]
        public async Task<IActionResult> GetRolePermissions(string roleName)
        {
            try
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role == null)
                    return NotFound(new { error = "Role not found" });

                var permissions = await _context.RolePermissions
                    .Where(p => p.RoleId == role.Id)
                    .Select(p => new PermissionDto
                    {
                        Name = p.Permission,
                        Module = p.Module ?? "",
                        Granted = p.IsGranted
                    })
                    .ToListAsync();

                // If no permissions in database, return default permissions based on role
                if (!permissions.Any())
                {
                    permissions = GetDefaultPermissions(roleName);
                }

                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role permissions for {RoleName}", roleName);
                return StatusCode(500, new { error = "Failed to get permissions" });
            }
        }

        /// <summary>
        /// Search users by email or name
        /// </summary>
        [HttpGet("users/search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                    return BadRequest(new { error = "Search term must be at least 2 characters" });

                var users = await _userManager.Users
                    .Where(u => u.Email!.Contains(term) || 
                               (u.FirstName + " " + u.LastName).Contains(term))
                    .Take(10)
                    .ToListAsync();

                var userDtos = new List<object>();
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userDtos.Add(new
                    {
                        id = user.Id,
                        email = user.Email,
                        name = $"{user.FirstName} {user.LastName}".Trim(),
                        roles = roles
                    });
                }

                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with term {Term}", term);
                return StatusCode(500, new { error = "Search failed" });
            }
        }

        /// <summary>
        /// Update user roles
        /// </summary>
        [HttpPost("users/{userId}/roles")]
        public async Task<IActionResult> UpdateUserRoles(
            string userId, 
            [FromBody] UpdateUserRolesRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { error = "User not found" });

                var currentRoles = await _userManager.GetRolesAsync(user);
                
                // Remove old roles
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    _logger.LogError("Failed to remove roles from user {UserId}: {Errors}", 
                        userId, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    return BadRequest(new { error = "Failed to remove old roles" });
                }

                // Add new roles
                var addResult = await _userManager.AddToRolesAsync(user, request.Roles);
                if (!addResult.Succeeded)
                {
                    _logger.LogError("Failed to add roles to user {UserId}: {Errors}", 
                        userId, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                    return BadRequest(new { error = "Failed to add new roles" });
                }

                _logger.LogInformation("Updated roles for user {UserId} to {Roles}", 
                    userId, string.Join(", ", request.Roles));

                return Ok(new { success = true, message = "User roles updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating roles for user {UserId}", userId);
                return StatusCode(500, new { error = "Failed to update roles" });
            }
        }

        /// <summary>
        /// Get default permissions for a role (when no database records exist)
        /// </summary>
        private List<PermissionDto> GetDefaultPermissions(string roleName)
        {
            return roleName.ToUpper() switch
            {
                "ADMIN" => new List<PermissionDto>
                {
                    new() { Name = "Quản lý toàn bộ hệ thống", Module = "System", Granted = true },
                    new() { Name = "Xem tất cả dữ liệu", Module = "Data", Granted = true },
                    new() { Name = "Sửa đổi cài đặt hệ thống", Module = "Settings", Granted = true },
                    new() { Name = "Quản lý người dùng", Module = "Users", Granted = true },
                    new() { Name = "Xem báo cáo", Module = "Reports", Granted = true },
                    new() { Name = "Quản lý đơn hàng", Module = "Orders", Granted = true },
                    new() { Name = "Quản lý sản phẩm", Module = "Products", Granted = true }
                },
                "SELLER" => new List<PermissionDto>
                {
                    new() { Name = "Xem đơn hàng liên quan", Module = "Orders", Granted = true },
                    new() { Name = "Cập nhật trạng thái đơn hàng", Module = "Orders", Granted = true },
                    new() { Name = "Xem báo cáo bán hàng", Module = "Reports", Granted = true },
                    new() { Name = "Quản lý sản phẩm của mình", Module = "Products", Granted = true },
                    new() { Name = "Quản lý người dùng", Module = "Users", Granted = false }
                },
                "CUSTOMER" => new List<PermissionDto>
                {
                    new() { Name = "Tạo đơn hàng", Module = "Orders", Granted = true },
                    new() { Name = "Xem lịch sử đơn hàng", Module = "Orders", Granted = true },
                    new() { Name = "Quản lý hồ sơ cá nhân", Module = "Profile", Granted = true },
                    new() { Name = "Quản lý wishlist", Module = "Wishlist", Granted = true }
                },
                _ => new List<PermissionDto>()
            };
        }
    }

    public class PermissionDto
    {
        public string Name { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public bool Granted { get; set; }
    }

    public class UpdateUserRolesRequest
    {
        public List<string> Roles { get; set; } = new();
    }
}
