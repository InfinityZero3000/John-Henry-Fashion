using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;

namespace JohnHenryFashionWeb.Services
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(string userId, string permission);
        Task<List<string>> GetUserPermissionsAsync(string userId);
        Task<bool> GrantPermissionAsync(string userId, string permission, string grantedBy, string? notes = null);
        Task<bool> RevokePermissionAsync(string userId, string permission);
        Task<bool> GrantRolePermissionAsync(string roleId, string permission, string grantedBy);
        Task<bool> RevokeRolePermissionAsync(string roleId, string permission);
        Task<List<string>> GetRolePermissionsAsync(string roleId);
    }

    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<PermissionService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Check if user has specific permission (from role or user-specific)
        /// </summary>
        public async Task<bool> HasPermissionAsync(string userId, string permission)
        {
            try
            {
                // Check user-specific permissions first
                var userPermission = await _context.UserPermissions
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.Permission == permission);
                
                if (userPermission != null)
                {
                    return userPermission.IsGranted;
                }

                // Check role-based permissions
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                var roles = await _userManager.GetRolesAsync(user);
                
                foreach (var roleName in roles)
                {
                    var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                    if (role == null) continue;

                    var rolePermission = await _context.RolePermissions
                        .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.Permission == permission);
                    
                    if (rolePermission != null && rolePermission.IsGranted)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permission, userId);
                return false;
            }
        }

        /// <summary>
        /// Get all permissions for a user (from all roles + user-specific)
        /// </summary>
        public async Task<List<string>> GetUserPermissionsAsync(string userId)
        {
            // Effective permissions:
            // - Start with role-granted permissions
            // - Then apply user overrides (IsGranted=true adds, IsGranted=false removes)
            var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Get role-based permissions
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    
                    foreach (var roleName in roles)
                    {
                        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                        if (role == null) continue;

                        var rolePermissions = await _context.RolePermissions
                            .Where(rp => rp.RoleId == role.Id && rp.IsGranted)
                            .Select(rp => rp.Permission)
                            .ToListAsync();
                        
                        foreach (var perm in rolePermissions)
                        {
                            permissions.Add(perm);
                        }
                    }
                }

                // Apply user-specific overrides (grant/deny)
                var userOverrides = await _context.UserPermissions
                    .Where(up => up.UserId == userId)
                    .Select(up => new { up.Permission, up.IsGranted })
                    .ToListAsync();

                foreach (var overridePerm in userOverrides)
                {
                    if (overridePerm.IsGranted)
                    {
                        permissions.Add(overridePerm.Permission);
                    }
                    else
                    {
                        permissions.Remove(overridePerm.Permission);
                    }
                }

                return permissions.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
                return new List<string>();
            }
        }

        /// <summary>
        /// Grant user-specific permission
        /// </summary>
        public async Task<bool> GrantPermissionAsync(string userId, string permission, string grantedBy, string? notes = null)
        {
            try
            {
                var existing = await _context.UserPermissions
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.Permission == permission);

                if (existing != null)
                {
                    existing.IsGranted = true;
                    existing.GrantedBy = grantedBy;
                    existing.Notes = notes;
                }
                else
                {
                    var userPermission = new UserPermission
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Permission = permission,
                        IsGranted = true,
                        GrantedBy = grantedBy,
                        Notes = notes,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.UserPermissions.Add(userPermission);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Granted permission {Permission} to user {UserId} by {GrantedBy}", 
                    permission, userId, grantedBy);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting permission {Permission} to user {UserId}", permission, userId);
                return false;
            }
        }

        /// <summary>
        /// Revoke user-specific permission
        /// </summary>
        public async Task<bool> RevokePermissionAsync(string userId, string permission)
        {
            try
            {
                var existing = await _context.UserPermissions
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.Permission == permission);

                if (existing != null)
                {
                    existing.IsGranted = false;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Revoked permission {Permission} from user {UserId}", permission, userId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking permission {Permission} from user {UserId}", permission, userId);
                return false;
            }
        }

        /// <summary>
        /// Grant permission to entire role
        /// </summary>
        public async Task<bool> GrantRolePermissionAsync(string roleId, string permission, string grantedBy)
        {
            try
            {
                var existing = await _context.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.Permission == permission);

                if (existing != null)
                {
                    existing.IsGranted = true;
                    existing.CreatedBy = grantedBy;
                }
                else
                {
                    var rolePermission = new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = roleId,
                        Permission = permission,
                        IsGranted = true,
                        CreatedBy = grantedBy,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.RolePermissions.Add(rolePermission);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Granted permission {Permission} to role {RoleId}", permission, roleId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting permission {Permission} to role {RoleId}", permission, roleId);
                return false;
            }
        }

        /// <summary>
        /// Revoke permission from role
        /// </summary>
        public async Task<bool> RevokeRolePermissionAsync(string roleId, string permission)
        {
            try
            {
                var existing = await _context.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.Permission == permission);

                if (existing != null)
                {
                    existing.IsGranted = false;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Revoked permission {Permission} from role {RoleId}", permission, roleId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking permission {Permission} from role {RoleId}", permission, roleId);
                return false;
            }
        }

        /// <summary>
        /// Get all permissions for a role
        /// </summary>
        public async Task<List<string>> GetRolePermissionsAsync(string roleId)
        {
            try
            {
                return await _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId && rp.IsGranted)
                    .Select(rp => rp.Permission)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for role {RoleId}", roleId);
                return new List<string>();
            }
        }
    }
}
