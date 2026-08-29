using Microsoft.AspNetCore.Mvc; using Microsoft.AspNetCore.Mvc.RazorPages; using Microsoft.EntityFrameworkCore; using MYOB.Data; using MYOB.Models; using MYOB.Services; using MYOB.Support;
namespace MYOB.Pages.Settings.Suppliers;
public class EditModel(ApplicationDbContext db, IPermissionService permissions) : PageModel
{
    [BindProperty] public Supplier Item { get; set; } = null!;
    public async Task<IActionResult> OnGetAsync(Guid id){if(!await permissions.HasAsync(User,ScreenCatalog.Suppliers,PermissionAction.Update))return Forbid(); var item=await db.Suppliers.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);if(item is null)return NotFound();Item=item;return Page();}
    public async Task<IActionResult> OnPostAsync(){if(!await permissions.HasAsync(User,ScreenCatalog.Suppliers,PermissionAction.Update))return Forbid(); if(!ModelState.IsValid)return Page(); var current=await db.Suppliers.SingleOrDefaultAsync(x=>x.Id==Item.Id); if(current is null)return NotFound(); current.Name=Item.Name;current.Phone=Item.Phone;current.Address=Item.Address;current.OpeningBalance=Item.OpeningBalance;current.Notes=Item.Notes;await db.SaveChangesAsync();TempData["Message"]="تم تعديل المورد";return RedirectToPage("Index");}
}
