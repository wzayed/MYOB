using Microsoft.AspNetCore.Mvc; using Microsoft.AspNetCore.Mvc.RazorPages; using MYOB.Data; using MYOB.Models; using MYOB.Services; using MYOB.Support;
namespace MYOB.Pages.Settings.Suppliers;
public class CreateModel(ApplicationDbContext db, IPermissionService permissions) : PageModel
{
    [BindProperty] public Supplier Item { get; set; } = new();
    public async Task<IActionResult> OnGetAsync() => await permissions.HasAsync(User, ScreenCatalog.Suppliers, PermissionAction.Add) ? Page() : Forbid();
    public async Task<IActionResult> OnPostAsync(){ if(!await permissions.HasAsync(User,ScreenCatalog.Suppliers,PermissionAction.Add))return Forbid(); if(!ModelState.IsValid)return Page(); db.Suppliers.Add(Item); await db.SaveChangesAsync(); TempData["Message"]="تمت إضافة المورد"; return RedirectToPage("Index"); }
}
