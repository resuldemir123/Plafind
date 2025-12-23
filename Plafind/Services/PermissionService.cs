using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Plafind.Data;
using Plafind.Models;

namespace Plafind.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PermissionService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<bool> HasPermissionAsync(string userId, string permission)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(permission))
                return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            // Admin her şeyi yapabilir
            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return true;

            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Any())
                return false;

            // Kullanıcının rollerinden birinde bu izin var mı?
            var roleIds = await _context.Roles
                .Where(r => userRoles.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync();

            var hasPermission = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .AnyAsync(rp => roleIds.Contains(rp.RoleId) && rp.Permission.Name == permission);

            return hasPermission;
        }

        public async Task<bool> HasAnyPermissionAsync(string userId, params string[] permissions)
        {
            if (permissions == null || !permissions.Any())
                return false;

            foreach (var permission in permissions)
            {
                if (await HasPermissionAsync(userId, permission))
                    return true;
            }

            return false;
        }

        public async Task<bool> HasAllPermissionsAsync(string userId, params string[] permissions)
        {
            if (permissions == null || !permissions.Any())
                return false;

            foreach (var permission in permissions)
            {
                if (!await HasPermissionAsync(userId, permission))
                    return false;
            }

            return true;
        }

        public async Task<List<string>> GetUserPermissionsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return new List<string>();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new List<string>();

            // Admin her şeyi yapabilir
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return await _context.Permissions.Select(p => p.Name).ToListAsync();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Any())
                return new List<string>();

            var roleIds = await _context.Roles
                .Where(r => userRoles.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync();

            var permissions = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync();

            return permissions;
        }

        public async Task<List<string>> GetRolePermissionsAsync(string roleId)
        {
            if (string.IsNullOrEmpty(roleId))
                return new List<string>();

            return await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission.Name)
                .ToListAsync();
        }

        public async Task<bool> AssignPermissionToRoleAsync(string roleId, string permission)
        {
            if (string.IsNullOrEmpty(roleId) || string.IsNullOrEmpty(permission))
                return false;

            // Permission var mı kontrol et
            var permissionEntity = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Name == permission);

            if (permissionEntity == null)
                return false;

            // Zaten atanmış mı kontrol et
            var exists = await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionEntity.Id);

            if (exists)
                return true; // Zaten var, başarılı say

            var rolePermission = new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionEntity.Id
            };

            _context.RolePermissions.Add(rolePermission);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemovePermissionFromRoleAsync(string roleId, string permission)
        {
            if (string.IsNullOrEmpty(roleId) || string.IsNullOrEmpty(permission))
                return false;

            var permissionEntity = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Name == permission);

            if (permissionEntity == null)
                return false;

            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionEntity.Id);

            if (rolePermission == null)
                return false;

            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}

