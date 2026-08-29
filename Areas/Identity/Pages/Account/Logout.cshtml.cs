using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Identity;using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using MYOB.Models;using MYOB.Services;
namespace MYOB.Areas.Identity.Pages.Account;
[Authorize]public class LogoutModel(SignInManager<ApplicationUser> signInManager,IAuditService audit):PageModel
{
    public async Task<IActionResult> OnPostAsync(string? returnUrl=null){await audit.WriteAsync(HttpContext,"تسجيل خروج","/Account/Logout");await signInManager.SignOutAsync();return LocalRedirect(returnUrl??Url.Content("~/"));}
}
