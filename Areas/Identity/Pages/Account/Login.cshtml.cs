using System.ComponentModel.DataAnnotations;using Microsoft.AspNetCore.Authentication;using Microsoft.AspNetCore.Identity;using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using MYOB.Models;using MYOB.Services;
namespace MYOB.Areas.Identity.Pages.Account;
public class LoginModel(SignInManager<ApplicationUser> signInManager,IAuditService audit):PageModel
{
    [BindProperty]public InputModel Input{get;set;}=new();public string? ReturnUrl{get;set;}public sealed class InputModel{[Required,EmailAddress]public string Email{get;set;}="";[Required,DataType(DataType.Password)]public string Password{get;set;}="";public bool RememberMe{get;set;}}
    public async Task OnGetAsync(string? returnUrl=null){ReturnUrl=returnUrl;await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);}
    public async Task<IActionResult> OnPostAsync(string? returnUrl=null){returnUrl??=Url.Content("~/");if(!ModelState.IsValid)return Page();var result=await signInManager.PasswordSignInAsync(Input.Email,Input.Password,Input.RememberMe,lockoutOnFailure:true);if(result.Succeeded){await audit.WriteAsync(HttpContext,"تسجيل دخول","/Account/Login",Input.Email);return LocalRedirect(returnUrl);}if(result.IsLockedOut)ModelState.AddModelError(string.Empty,"تم إيقاف الحساب مؤقتاً بسبب محاولات متكررة.");else ModelState.AddModelError(string.Empty,"بيانات الدخول غير صحيحة.");return Page();}
}
