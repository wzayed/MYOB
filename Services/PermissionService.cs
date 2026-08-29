using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MYOB.Data;
using MYOB.Models;
using MYOB.Support;
using System.Security.Claims;
namespace MYOB.Services;
public interface IPermissionService { Task<bool> HasAsync(ClaimsPrincipal user, string screenKey, PermissionAction action = PermissionAction.View); }
public sealed class PermissionService(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : IPermissionService
{
    public async Task<bool> HasAsync(ClaimsPrincipal principal, string screenKey, PermissionAction action = PermissionAction.View)
    {
        if (principal.Identity?.IsAuthenticated != true) return false;
        var user = await userManager.GetUserAsync(principal);
        if (user is null) return false;
        if (user.IsAdmin || await userManager.IsInRoleAsync(user, "Admin")) return true;
        var p = await db.ScreenPermissions.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == user.Id && x.ScreenKey == screenKey);
        return p is not null && action switch
        {
            PermissionAction.View => p.CanView, PermissionAction.Add => p.CanAdd,
            PermissionAction.Update => p.CanUpdate, PermissionAction.Delete => p.CanDelete, _ => false
        };
    }
}
