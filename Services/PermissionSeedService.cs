using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JohnHenryFashionWeb.Services
{
    public class PermissionSeedService
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<PermissionSeedService> _logger;

        public PermissionSeedService(
            ApplicationDbContext context,
            RoleManager<IdentityRole> roleManager,
            ILogger<PermissionSeedService> logger)
        {
            _context = context;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task SeedDefaultRolePermissionsAsync(string seededBy = "system")
        {
            // Ensure roles exist
            await EnsureRoleExistsAsync(UserRoles.Admin);
            await EnsureRoleExistsAsync(UserRoles.Seller);

            // Seed role permissions
            await SeedRolePermissionsAsync(
                roleName: UserRoles.Seller,
                permissions: Permissions.GetDefaultSellerPermissions(),
                seededBy: seededBy);

            var allPermissions = Permissions.GetAllPermissions()
                .SelectMany(g => g.Value.Select(p => p.Code))
                .ToList();

            await SeedRolePermissionsAsync(
                roleName: UserRoles.Admin,
                permissions: allPermissions,
                seededBy: seededBy);
        }

        private async Task EnsureRoleExistsAsync(string roleName)
        {
            var existing = await _roleManager.FindByNameAsync(roleName);
            if (existing != null) return;

            var role = new IdentityRole(roleName)
            {
                NormalizedName = roleName.ToUpperInvariant()
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                var error = string.Join("; ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to create role {RoleName}: {Error}", roleName, error);
            }
            else
            {
                _logger.LogInformation("Created missing role {RoleName}", roleName);
            }
        }

        private async Task SeedRolePermissionsAsync(string roleName, IEnumerable<string> permissions, string seededBy)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleName} not found; skipping permission seeding", roleName);
                return;
            }

            var permissionList = permissions
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var permission in permissionList)
            {
                var existing = await _context.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.Permission == permission);

                if (existing == null)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = role.Id,
                        Permission = permission,
                        Module = permission.Contains('.') ? permission.Split('.')[0] : null,
                        IsGranted = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = seededBy
                    });
                }
                else if (!existing.IsGranted)
                {
                    existing.IsGranted = true;
                    existing.CreatedBy = seededBy;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} permissions for role {RoleName}", permissionList.Count, roleName);
        }
    }
}
