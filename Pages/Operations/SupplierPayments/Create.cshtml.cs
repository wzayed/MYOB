using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MYOB.Data;
using MYOB.DTOs;
using MYOB.Services;
using MYOB.Support;

namespace MYOB.Pages.Operations.SupplierPayments;

public class CreateModel(ApplicationDbContext db,ITransactionService transactions,IPermissionService permissions):PageModel
{
    [BindProperty(SupportsGet=true)]public Guid? SupplierId{get;set;}
    [BindProperty]public SupplierPaymentBatchForm Item{get;set;}=new();
    public SelectList Suppliers{get;private set;}=null!;
    public SelectList Methods{get;private set;}=null!;
    public IReadOnlyList<PolicyOption> Policies{get;private set;}=[];
    public SupplierCreditOption? AvailableCredit{get;private set;}

    public async Task<IActionResult> OnGetAsync()
    {
        if(!await permissions.HasAsync(User,ScreenCatalog.SupplierPayments,PermissionAction.Add))return Forbid();
        if(SupplierId.HasValue)Item.SupplierId=SupplierId.Value;
        await LoadAsync();return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SupplierId=Item.SupplierId;
        if(!await permissions.HasAsync(User,ScreenCatalog.SupplierPayments,PermissionAction.Add))return Forbid();
        var allocated=Item.Allocations.Where(x=>x.Amount>0).Sum(x=>x.Amount);
        if(allocated<=0)ModelState.AddModelError(string.Empty,"أدخل مبلغاً لبوليصة واحدة على الأقل.");
        if(Item.TotalAmount>0&&allocated>Item.TotalAmount)ModelState.AddModelError(string.Empty,$"المبلغ الموزع أكبر من إجمالي المبلغ بمقدار: {allocated-Item.TotalAmount:N2}");
        if(ModelState.IsValid)try
        {
            await transactions.CreateSupplierPaymentsAsync(new(Item.SupplierId,Item.Date,Item.TotalAmount,Item.PaymentMethodId,Item.Comments,Item.SupplierCreditId,Item.Allocations.Where(x=>x.Amount>0).Select(x=>new SupplierPaymentAllocation(x.SupplierReceiptId,x.Amount)).ToList()));
            TempData["Message"]="تم تسجيل الدفع وتوزيعه على البوالص المحددة";return RedirectToPage("Index");
        }
        catch(InvalidOperationException e){ModelState.AddModelError(string.Empty,e.Message);}
        await LoadAsync(preserveAmounts:true);return Page();
    }

    private async Task LoadAsync(bool preserveAmounts=false)
    {
        Suppliers=new(await db.Suppliers.AsNoTracking().OrderBy(x=>x.Name).ToListAsync(),"Id","Name",SupplierId);
        Methods=new(await db.PaymentMethods.AsNoTracking().OrderBy(x=>x.Name).ToListAsync(),"Id","Name");
        if(!SupplierId.HasValue)return;
        var amounts=preserveAmounts?Item.Allocations.ToDictionary(x=>x.SupplierReceiptId,x=>x.Amount):new Dictionary<Guid,decimal>();
        Policies=await transactions.GetSupplierPoliciesAsync(SupplierId.Value);
        Item.SupplierId=SupplierId.Value;
        AvailableCredit=await transactions.GetOldestSupplierCreditAsync(SupplierId.Value);
        if(!preserveAmounts&&AvailableCredit is not null)
        {
            Item.SupplierCreditId=AvailableCredit.Id;Item.PaymentMethodId=AvailableCredit.PaymentMethodId;Item.Comments=AvailableCredit.CarryForwardNote;
            var creditLeft=AvailableCredit.RemainingAmount;
            foreach(var policy in Policies)
            {
                var amount=Math.Min(creditLeft,policy.RemainingAmount);amounts[policy.Id]=amount;creditLeft-=amount;if(creditLeft<=0)break;
            }
            Item.TotalAmount=AvailableCredit.RemainingAmount-creditLeft;
        }
        Item.Allocations=Policies.Select(x=>new SupplierPaymentAllocationForm{SupplierReceiptId=x.Id,Amount=amounts.GetValueOrDefault(x.Id)}).ToList();
    }
}
