namespace Plafind.Services
{
    /// <summary>
    /// Permission servisi - Rol bazlı yetkilendirme kontrolü
    /// </summary>
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(string userId, string permission);
        Task<bool> HasAnyPermissionAsync(string userId, params string[] permissions);
        Task<bool> HasAllPermissionsAsync(string userId, params string[] permissions);
        Task<List<string>> GetUserPermissionsAsync(string userId);
        Task<List<string>> GetRolePermissionsAsync(string roleId);
        Task<bool> AssignPermissionToRoleAsync(string roleId, string permission);
        Task<bool> RemovePermissionFromRoleAsync(string roleId, string permission);
    }
}

