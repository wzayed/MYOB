using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MYOB.Data;
using MYOB.Models;
using MYOB.Services;
using MYOB.Support;
namespace MYOB.Pages.Settings.Suppliers;
public class IndexModel(ApplicationDbContext db, IPermissionService permissions) : PageModel
{
    public IReadOnlyList<Supplier> Items { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public int TotalPages { get; private set; }
    public bool CanAdd { get; private set; } public bool CanUpdate { get; private set; } public bool CanDelete { get; private set; }
    public async Task OnGetAsync()
    {
        const int size = 15; PageNumber = Math.Max(1, PageNumber);
        var query = db.Suppliers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(Search)) query = query.Where(x => x.Name.Contains(Search) || (x.Phone != null && x.Phone.Contains(Search)));
        var count = await query.CountAsync(); TotalPages = Math.Max(1, (int)Math.Ceiling(count / (double)size)); PageNumber = Math.Min(PageNumber, TotalPages);
        Items = await query.OrderBy(x => x.Name).Skip((PageNumber - 1) * size).Take(size).ToListAsync();
        CanAdd = await permissions.HasAsync(User, ScreenCatalog.Suppliers, PermissionAction.Add);
        CanUpdate = await permissions.HasAsync(User, ScreenCatalog.Suppliers, PermissionAction.Update);
        CanDelete = await permissions.HasAsync(User, ScreenCatalog.Suppliers, PermissionAction.Delete);
    }
    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (!await permissions.HasAsync(User, ScreenCatalog.Suppliers, PermissionAction.Delete)) return Forbid();
        var item = await db.Suppliers.SingleOrDefaultAsync(x => x.Id == id); if (item is null) return NotFound();
        if(await db.SupplierReceipts.AnyAsync(x=>x.SupplierId==id)||await db.SupplierPayments.AnyAsync(x=>x.SupplierId==id)){TempData["Error"]="لا يمكن حذف مورد مرتبط بحركات.";return RedirectToPage();}
        item.IsDeleted = true; item.DeletedAt = DateTimeOffset.UtcNow; item.DeletedBy = User.Identity?.Name; await db.SaveChangesAsync();
        TempData["Message"] = "تم حذف المورد بنجاح"; return RedirectToPage(new { Search, PageNumber });
    }
}
