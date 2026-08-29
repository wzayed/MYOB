using System.ComponentModel.DataAnnotations;using Microsoft.AspNetCore.Identity;using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using MYOB.Models;using MYOB.Services;using MYOB.Support;
namespace MYOB.Pages.Settings.Users;
public class CreateModel(UserManager<ApplicationUser> users,IPermissionService permissions,IAuditService audit):PageModel
{
    [BindProperty]public InputModel Input{get;set;}=new();
    public sealed class InputModel{[Required(ErrorMessage="الاسم مطلوب")]public string FullName{get;set;}="";[Required,EmailAddress(ErrorMessage="البريد غير صحيح")]public string Email{get;set;}="";[Required,MinLength(8,ErrorMessage="كلمة المرور 8 أحرف على الأقل"),DataType(DataType.Password)]public string Password{get;set;}="";public bool IsAdmin{get;set;}}
    public async Task<IActionResult> OnGetAsync()=>await permissions.HasAsync(User,ScreenCatalog.Users,PermissionAction.Add)?Page():Forbid();
    public async Task<IActionResult> OnPostAsync(){if(!await permissions.HasAsync(User,ScreenCatalog.Users,PermissionAction.Add))return Forbid();if(!ModelState.IsValid)return Page();var user=new ApplicationUser{FullName=Input.FullName,Email=Input.Email,UserName=Input.Email,EmailConfirmed=true,IsAdmin=Input.IsAdmin};var result=await users.CreateAsync(user,Input.Password);if(!result.Succeeded){foreach(var e in result.Errors)ModelState.AddModelError(string.Empty,e.Description);return Page();}if(Input.IsAdmin)await users.AddToRoleAsync(user,"Admin");await audit.WriteAsync(HttpContext,"إنشاء","/Settings/Users",entity:"ApplicationUser",recordId:user.Id,newValues:System.Text.Json.JsonSerializer.Serialize(new{user.FullName,user.Email,user.IsAdmin}));TempData["Message"]="تمت إضافة المستخدم";return RedirectToPage("Index");}
}
