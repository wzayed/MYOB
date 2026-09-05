using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MYOB.Data;
using MYOB.DTOs;
using MYOB.Services;
using MYOB.Support;

namespace MYOB.Pages.Operations.CustomerPayments;

public class EditModel(ApplicationDbContext db, ITransactionService transactions, IPermissionService permissions) : PageModel
{
    [BindProperty] public CustomerPaymentBatchForm Item { get; set; } = new();
    public SelectList Methods { get; private set; } = null!;
    public IReadOnlyList<PolicyOption> Policies { get; private set; } = [];
    public string CustomerName { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!await permissions.HasAsync(User, ScreenCatalog.CustomerPayments, PermissionAction.Update)) return Forbid();
        var payments = await GetGroupAsync(id);
        if (payments.Count == 0) return NotFound();
        var anchor = payments[0];
        Item = new CustomerPaymentBatchForm { PaymentGroupId = id, CustomerId = anchor.CustomerId, Date = anchor.Date, TotalAmount = payments.Sum(x => x.Amount), PaymentMethodId = anchor.PaymentMethodId, Comments = anchor.Comments };
        CustomerName = anchor.Customer.Name;
        await LoadAsync(payments);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        if (!await permissions.HasAsync(User, ScreenCatalog.CustomerPayments, PermissionAction.Update)) return Forbid();
        if (ModelState.IsValid)
        {
            try
            {
                await transactions.UpdateCustomerPaymentsAsync(id, new(Item.CustomerId, Item.Date, Item.PaymentMethodId, Item.Comments, Item.Allocations.Where(x => x.Amount > 0).Select(x => new CustomerPaymentAllocation(x.CustomerDeliveryId, x.Amount)).ToList()));
                TempData["Message"] = "تم تعديل حركة القبض وجميع توزيعاتها";
                return RedirectToPage("Index");
            }
            catch (InvalidOperationException e) { ModelState.AddModelError(string.Empty, e.Message); }
        }
        var payments = await GetGroupAsync(id);
        CustomerName = await db.Customers.Where(x => x.Id == Item.CustomerId).Select(x => x.Name).FirstOrDefaultAsync() ?? "";
        await LoadAsync(payments);
        return Page();
    }

    private async Task<List<Models.CustomerPayment>> GetGroupAsync(Guid id)
    {
        var anchor = await db.CustomerPayments.Include(x => x.Customer).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (anchor is null) return [];
        return anchor.PaymentGroupId.HasValue
            ? await db.CustomerPayments.Include(x => x.Customer).AsNoTracking().Where(x => x.PaymentGroupId == anchor.PaymentGroupId).ToListAsync()
            : await db.CustomerPayments.Include(x => x.Customer).AsNoTracking().Where(x => x.CustomerId == anchor.CustomerId && x.Date == anchor.Date && x.PaymentMethodId == anchor.PaymentMethodId && x.Comments == anchor.Comments && x.CreatedAt == anchor.CreatedAt).ToListAsync();
    }

    private async Task LoadAsync(IReadOnlyCollection<Models.CustomerPayment> payments)
    {
        Methods = new(await db.PaymentMethods.AsNoTracking().OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        Policies = await transactions.GetCustomerPoliciesAsync(Item.CustomerId, payments.Select(x => x.Id).ToArray());
        var entered = Item.Allocations.ToDictionary(x => x.CustomerDeliveryId, x => x.Amount);
        if (entered.Count == 0) entered = payments.ToDictionary(x => x.CustomerDeliveryId, x => x.Amount);
        Item.Allocations = Policies.Select(x => new CustomerPaymentAllocationForm { CustomerDeliveryId = x.Id, Amount = entered.GetValueOrDefault(x.Id) }).ToList();
    }
}
