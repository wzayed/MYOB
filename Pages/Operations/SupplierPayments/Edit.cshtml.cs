using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MYOB.Data;
using MYOB.DTOs;
using MYOB.Services;
using MYOB.Support;

namespace MYOB.Pages.Operations.SupplierPayments;

public class EditModel(ApplicationDbContext db, ITransactionService transactions, IPermissionService permissions) : PageModel
{
    [BindProperty] public SupplierPaymentBatchForm Item { get; set; } = new();
    public SelectList Methods { get; private set; } = null!;
    public IReadOnlyList<PolicyOption> Policies { get; private set; } = [];
    public string SupplierName { get; private set; } = "";
    public SupplierCreditOption? AppliedCredit { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!await permissions.HasAsync(User, ScreenCatalog.SupplierPayments, PermissionAction.Update)) return Forbid();
        var payments = await GetGroupAsync(id);
        if (payments.Count == 0) return NotFound();
        var anchor = payments[0];
        var groupId=anchor.PaymentGroupId;
        var generatedCredit=groupId.HasValue?await db.SupplierCredits.AsNoTracking().SingleOrDefaultAsync(x=>x.SourcePaymentGroupId==groupId):null;
        Item = new SupplierPaymentBatchForm { PaymentGroupId = id, SupplierCreditId=payments.FirstOrDefault(x=>x.SupplierCreditId.HasValue)?.SupplierCreditId, SupplierId = anchor.SupplierId, Date = anchor.Date, TotalAmount = payments.Sum(x => x.Amount)+(generatedCredit?.OriginalAmount??0m), PaymentMethodId = anchor.PaymentMethodId, Comments = anchor.Comments };
        SupplierName = anchor.Supplier.Name;
        await LoadAsync(payments);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        if (!await permissions.HasAsync(User, ScreenCatalog.SupplierPayments, PermissionAction.Update)) return Forbid();
        if (ModelState.IsValid)
        {
            try
            {
                await transactions.UpdateSupplierPaymentsAsync(id, new(Item.SupplierId, Item.Date, Item.TotalAmount, Item.PaymentMethodId, Item.Comments, Item.SupplierCreditId, Item.Allocations.Where(x => x.Amount > 0).Select(x => new SupplierPaymentAllocation(x.SupplierReceiptId, x.Amount)).ToList()));
                TempData["Message"] = "تم تعديل حركة الدفع وجميع توزيعاتها";
                return RedirectToPage("Index");
            }
            catch (InvalidOperationException e) { ModelState.AddModelError(string.Empty, e.Message); }
        }
        var payments = await GetGroupAsync(id);
        SupplierName = await db.Suppliers.Where(x => x.Id == Item.SupplierId).Select(x => x.Name).FirstOrDefaultAsync() ?? "";
        await LoadAsync(payments);
        return Page();
    }

    private async Task<List<Models.SupplierPayment>> GetGroupAsync(Guid id)
    {
        var anchor = await db.SupplierPayments.Include(x => x.Supplier).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (anchor is null) return [];
        return anchor.PaymentGroupId.HasValue
            ? await db.SupplierPayments.Include(x => x.Supplier).AsNoTracking().Where(x => x.PaymentGroupId == anchor.PaymentGroupId).ToListAsync()
            : await db.SupplierPayments.Include(x => x.Supplier).AsNoTracking().Where(x => x.SupplierId == anchor.SupplierId && x.Date == anchor.Date && x.PaymentMethodId == anchor.PaymentMethodId && x.Comments == anchor.Comments && x.CreatedAt == anchor.CreatedAt).ToListAsync();
    }

    private async Task LoadAsync(IReadOnlyCollection<Models.SupplierPayment> payments)
    {
        Methods = new(await db.PaymentMethods.AsNoTracking().OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        Policies = await transactions.GetSupplierPoliciesAsync(Item.SupplierId, payments.Select(x => x.Id).ToArray());
        var entered = Item.Allocations.ToDictionary(x => x.SupplierReceiptId, x => x.Amount);
        if (entered.Count == 0) entered = payments.GroupBy(x=>x.SupplierReceiptId).ToDictionary(x => x.Key, x => x.Sum(p=>p.Amount));
        Item.Allocations = Policies.Select(x => new SupplierPaymentAllocationForm { SupplierReceiptId = x.Id, Amount = entered.GetValueOrDefault(x.Id) }).ToList();
        if(Item.SupplierCreditId.HasValue)
        {
            var credit=await db.SupplierCredits.AsNoTracking().Where(x=>x.Id==Item.SupplierCreditId).Select(x=>new{x.Id,x.RemainingAmount,x.PaymentMethodId,PaymentMethodName=x.PaymentMethod.Name,Policy=x.SourceSupplierReceipt.PolicyNumber,x.Date}).SingleOrDefaultAsync();
            if(credit is not null)AppliedCredit=new(credit.Id,credit.RemainingAmount+payments.Where(x=>x.SupplierCreditId==credit.Id).Sum(x=>x.CreditAmount),credit.PaymentMethodId,credit.PaymentMethodName,credit.Policy,credit.Date,$"متبقي من الدفعة السابقة للبوليصة رقم {credit.Policy} بتاريخ {credit.Date:yyyy/MM/dd}");
        }
    }
}
