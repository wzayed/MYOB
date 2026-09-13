using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MYOB.Data;
using MYOB.DTOs;
using MYOB.Services;
using MYOB.Support;

namespace MYOB.Pages.Operations.CustomerPayments;

public class CreateModel(ApplicationDbContext db,ITransactionService transactions,IPermissionService permissions):PageModel
{
    [BindProperty(SupportsGet=true)]public Guid? CustomerId{get;set;}
    [BindProperty]public CustomerPaymentBatchForm Item{get;set;}=new();
    public SelectList Customers{get;private set;}=null!;
    public SelectList Methods{get;private set;}=null!;
    public IReadOnlyList<PolicyOption> Policies{get;private set;}=[];

    public async Task<IActionResult> OnGetAsync()
    {
        if(!await permissions.HasAsync(User,ScreenCatalog.CustomerPayments,PermissionAction.Add))return Forbid();
        if(CustomerId.HasValue)Item.CustomerId=CustomerId.Value;
        await LoadAsync();return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        CustomerId=Item.CustomerId;
        if(!await permissions.HasAsync(User,ScreenCatalog.CustomerPayments,PermissionAction.Add))return Forbid();
        var allocated=Item.Allocations.Where(x=>x.Amount>0).Sum(x=>x.Amount);
        if(allocated<=0)ModelState.AddModelError(string.Empty,"أدخل مبلغاً لبوليصة واحدة على الأقل.");
        if(Item.TotalAmount>0&&allocated!=Item.TotalAmount)ModelState.AddModelError(string.Empty,$"يجب توزيع إجمالي المبلغ بالكامل. المتبقي للتوزيع: {Item.TotalAmount-allocated:N2}");
        if(ModelState.IsValid)try
        {
            await transactions.CreateCustomerPaymentsAsync(new(Item.CustomerId,Item.Date,Item.PaymentMethodId,Item.Comments,Item.Allocations.Where(x=>x.Amount>0).Select(x=>new CustomerPaymentAllocation(x.CustomerDeliveryId,x.Amount)).ToList()) { PublicComments = Item.PublicComments });
            TempData["Message"]="تم تسجيل القبض وتوزيعه على البوالص المحددة";return RedirectToPage("Index");
        }
        catch(InvalidOperationException e){ModelState.AddModelError(string.Empty,e.Message);}
        await LoadAsync(preserveAmounts:true);return Page();
    }

    private async Task LoadAsync(bool preserveAmounts=false)
    {
        Customers=new(await db.Customers.AsNoTracking().OrderBy(x=>x.Name).ToListAsync(),"Id","Name",CustomerId);
        Methods=new(await db.PaymentMethods.AsNoTracking().OrderBy(x=>x.Name).ToListAsync(),"Id","Name");
        if(!CustomerId.HasValue)return;
        var amounts=preserveAmounts?Item.Allocations.ToDictionary(x=>x.CustomerDeliveryId,x=>x.Amount):new Dictionary<Guid,decimal>();
        Policies=await transactions.GetCustomerPoliciesAsync(CustomerId.Value);
        Item.CustomerId=CustomerId.Value;
        Item.Allocations=Policies.Select(x=>new CustomerPaymentAllocationForm{CustomerDeliveryId=x.Id,Amount=amounts.GetValueOrDefault(x.Id)}).ToList();
    }
}
