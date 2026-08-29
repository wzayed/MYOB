using Microsoft.AspNetCore.Identity;using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using Microsoft.EntityFrameworkCore;using MYOB.Models;using MYOB.Services;using MYOB.Support;
namespace MYOB.Pages.Settings.Users;
public class IndexModel(UserManager<ApplicationUser> users,IPermissionService permissions):PageModel
{
    public IReadOnlyList<ApplicationUser> Items{get;private set;}=[];[BindProperty(SupportsGet=true)]public string? Search{get;set;}[BindProperty(SupportsGet=true)]public int PageNumber{get;set;}=1;public int TotalPages{get;private set;}public bool CanAdd{get;private set;}public bool CanUpdate{get;private set;}
    public async Task OnGetAsync(){const int size=15;var q=users.Users.AsNoTracking();if(!string.IsNullOrWhiteSpace(Search))q=q.Where(x=>x.FullName.Contains(Search)||(x.Email!=null&&x.Email.Contains(Search)));var count=await q.CountAsync();TotalPages=Math.Max(1,(int)Math.Ceiling(count/(double)size));PageNumber=Math.Clamp(PageNumber,1,TotalPages);Items=await q.OrderBy(x=>x.FullName).Skip((PageNumber-1)*size).Take(size).ToListAsync();CanAdd=await permissions.HasAsync(User,ScreenCatalog.Users,PermissionAction.Add);CanUpdate=await permissions.HasAsync(User,ScreenCatalog.Users,PermissionAction.Update);}
}
