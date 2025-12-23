using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Plafind.Services;
using System.Security.Claims;

namespace Plafind.Attributes
{
    /// <summary>
    /// Permission bazlı yetkilendirme attribute'u
    /// </summary>
    public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string[] _permissions;
        private readonly bool _requireAll;

        public RequirePermissionAttribute(params string[] permissions)
        {
            _permissions = permissions;
            _requireAll = false;
        }

        public RequirePermissionAttribute(bool requireAll, params string[] permissions)
        {
            _permissions = permissions;
            _requireAll = requireAll;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var permissionService = context.HttpContext.RequestServices.GetService<IPermissionService>();
            if (permissionService == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            bool hasPermission;
            if (_requireAll)
            {
                hasPermission = await permissionService.HasAllPermissionsAsync(userId, _permissions);
            }
            else
            {
                hasPermission = await permissionService.HasAnyPermissionAsync(userId, _permissions);
            }

            if (!hasPermission)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}

