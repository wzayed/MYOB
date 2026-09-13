using Microsoft.EntityFrameworkCore;using MYOB.Data;using MYOB.DTOs;using MYOB.Models;using MYOB.Support;using System.Data;
namespace MYOB.Services;
public interface ITransactionService
{
    decimal CalculateTotal(decimal quantity,decimal unitPrice,decimal taxPercent);
    Task CreateSupplierReceiptAsync(SupplierReceiptCommand command);Task CreateSupplierPaymentAsync(SupplierPaymentCommand command);Task CreateSupplierPaymentsAsync(SupplierPaymentBatchCommand command);
    Task CreateCustomerDeliveryAsync(CustomerDeliveryCommand command);Task CreateCustomerPaymentAsync(CustomerPaymentCommand command);Task CreateCustomerPaymentsAsync(CustomerPaymentBatchCommand command);
    Task UpdateSupplierReceiptAsync(Guid id,SupplierReceiptCommand command);Task UpdateSupplierPaymentAsync(Guid id,SupplierPaymentCommand command);Task UpdateSupplierPaymentsAsync(Guid id,SupplierPaymentBatchCommand command);
    Task DeleteSupplierPaymentsAsync(Guid id,string? userName);
    Task UpdateCustomerDeliveryAsync(Guid id,CustomerDeliveryCommand command);Task UpdateCustomerPaymentAsync(Guid id,CustomerPaymentCommand command);Task UpdateCustomerPaymentsAsync(Guid id,CustomerPaymentBatchCommand command);
    Task<List<PolicyOption>> GetSupplierPoliciesAsync(Guid supplierId,IReadOnlyCollection<Guid>? excludePaymentIds=null);Task<List<PolicyOption>> GetUnassignedPoliciesAsync(Guid? includeDeliveryId=null);Task<List<PolicyOption>> GetCustomerPoliciesAsync(Guid customerId,IReadOnlyCollection<Guid>? excludePaymentIds=null);
    Task<SupplierCreditOption?> GetOldestSupplierCreditAsync(Guid supplierId,Guid? excludeSourcePaymentGroupId=null);
}
public sealed class TransactionService(ApplicationDbContext db):ITransactionService
{
    private static void EnsureNotFuture(DateOnly date)
    {
        if(date>BusinessDate.Today)throw new InvalidOperationException("لا يمكن إدخال تاريخ بعد تاريخ اليوم.");
    }

    public decimal CalculateTotal(decimal quantity,decimal unitPrice,decimal taxPercent)
    {
        if(quantity<=0||unitPrice<0||taxPercent<0||taxPercent>100)throw new InvalidOperationException("القيم المالية المدخلة غير صحيحة.");
        return decimal.Round(quantity*unitPrice*(1m+taxPercent/100m),2,MidpointRounding.AwayFromZero);
    }
    public async Task CreateSupplierReceiptAsync(SupplierReceiptCommand c)
    {
        EnsureNotFuture(c.Date);
        if(!await db.Suppliers.AnyAsync(x=>x.Id==c.SupplierId)||!await db.Materials.AnyAsync(x=>x.Id==c.MaterialId))throw new InvalidOperationException("المورد أو الخامة غير صالح.");
        if(string.IsNullOrWhiteSpace(c.PolicyNumber))throw new InvalidOperationException("رقم البوليصة مطلوب.");
        if(await db.SupplierReceipts.AnyAsync(x=>x.PolicyNumber==c.PolicyNumber.Trim()))throw new InvalidOperationException("رقم البوليصة مسجل مسبقاً.");
        db.SupplierReceipts.Add(new SupplierReceipt{SupplierId=c.SupplierId,MaterialId=c.MaterialId,Date=c.Date,PolicyNumber=c.PolicyNumber.Trim(),Quantity=c.Quantity,UnitPrice=c.UnitPrice,TaxPercent=c.TaxPercent,Total=CalculateTotal(c.Quantity,c.UnitPrice,c.TaxPercent),Details=c.Details});await db.SaveChangesAsync();
    }
    public async Task UpdateSupplierReceiptAsync(Guid id,SupplierReceiptCommand c)
    {
        EnsureNotFuture(c.Date);
        if(!await db.Suppliers.AnyAsync(x=>x.Id==c.SupplierId)||!await db.Materials.AnyAsync(x=>x.Id==c.MaterialId))throw new InvalidOperationException("المورد أو الخامة غير صالح.");
        var x=await db.SupplierReceipts.Include(x=>x.Payments).Include(x=>x.CustomerDelivery).SingleOrDefaultAsync(x=>x.Id==id)??throw new InvalidOperationException("الحركة غير موجودة.");if(x.Payments.Count>0||x.CustomerDelivery is not null)throw new InvalidOperationException("لا يمكن تعديل استلام مرتبط بدفع أو توريد لعميل.");
        if(await db.SupplierReceipts.AnyAsync(r=>r.Id!=id&&r.PolicyNumber==c.PolicyNumber.Trim()))throw new InvalidOperationException("رقم البوليصة مسجل مسبقاً.");x.SupplierId=c.SupplierId;x.MaterialId=c.MaterialId;x.Date=c.Date;x.PolicyNumber=c.PolicyNumber.Trim();x.Quantity=c.Quantity;x.UnitPrice=c.UnitPrice;x.TaxPercent=c.TaxPercent;x.Total=CalculateTotal(c.Quantity,c.UnitPrice,c.TaxPercent);x.Details=c.Details;await db.SaveChangesAsync();
    }
    public async Task<List<PolicyOption>> GetSupplierPoliciesAsync(Guid supplierId,IReadOnlyCollection<Guid>? excludePaymentIds=null)
    {
        var excluded=excludePaymentIds?.ToArray()??[];
        return await db.SupplierReceipts.AsNoTracking().Where(x=>x.SupplierId==supplierId&&x.Total-x.Payments.Where(p=>!excluded.Contains(p.Id)).Sum(p=>(decimal?)p.Amount??0)>0)
            .OrderBy(x=>x.Date).ThenBy(x=>x.PolicyNumber).Select(x=>new PolicyOption(x.Id,x.PolicyNumber,x.Date,x.Material.Name,x.Material.UnitOfMeasure.Name,x.Quantity,x.Total-x.Payments.Where(p=>!excluded.Contains(p.Id)).Sum(p=>(decimal?)p.Amount??0),x.UnitPrice,x.TaxPercent,x.Total)).ToListAsync();
    }
    public async Task CreateSupplierPaymentAsync(SupplierPaymentCommand c)
    {
        await CreateSupplierPaymentsAsync(new(c.SupplierId,c.Date,c.Amount,c.PaymentMethodId,c.Comments,null,[new(c.SupplierReceiptId,c.Amount)]) { PublicComments=c.PublicComments });
    }
    public async Task CreateSupplierPaymentsAsync(SupplierPaymentBatchCommand c)
    {
        EnsureNotFuture(c.Date);
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var allocations=c.Allocations.Where(x=>x.Amount>0).ToList();
        if(allocations.Count==0)throw new InvalidOperationException("أدخل مبلغاً لبوليصة واحدة على الأقل.");
        var allocatedTotal=allocations.Sum(x=>x.Amount);
        if(c.TotalAmount<allocatedTotal)throw new InvalidOperationException($"إجمالي المبلغ أقل من المبلغ الموزع ({allocatedTotal:N2}).");
        if(allocations.GroupBy(x=>x.SupplierReceiptId).Any(x=>x.Count()>1))throw new InvalidOperationException("لا يمكن تكرار البوليصة في نفس حركة الدفع.");
        if(!await db.PaymentMethods.AnyAsync(x=>x.Id==c.PaymentMethodId))throw new InvalidOperationException("طريقة الدفع غير صالحة.");
        var ids=allocations.Select(x=>x.SupplierReceiptId).ToList();
        var receipts=await db.SupplierReceipts.Include(x=>x.Payments).Where(x=>ids.Contains(x.Id)&&x.SupplierId==c.SupplierId).ToDictionaryAsync(x=>x.Id);
        if(receipts.Count!=ids.Count)throw new InvalidOperationException("إحدى البوالص لا تخص المورد المحدد.");
        var credit=await db.SupplierCredits.Include(x=>x.SourceSupplierReceipt).Where(x=>x.SupplierId==c.SupplierId&&x.RemainingAmount>0&&(c.SupplierCreditId==null||x.Id==c.SupplierCreditId)).OrderBy(x=>x.Date).ThenBy(x=>x.CreatedAt).FirstOrDefaultAsync();
        if(c.SupplierCreditId.HasValue&&credit is null)throw new InvalidOperationException("الرصيد المرحل المحدد غير متاح.");
        if(credit is not null&&credit.PaymentMethodId!=c.PaymentMethodId)throw new InvalidOperationException("يجب استخدام طريقة السداد الخاصة بالرصيد المرحل.");
        var comments=credit is null?c.Comments:$"متبقي من الدفعة السابقة للبوليصة رقم {credit.SourceSupplierReceipt.PolicyNumber} بتاريخ {credit.Date:yyyy/MM/dd}";
        var creditToApply=Math.Min(credit?.RemainingAmount??0m,allocatedTotal);
        var unappliedCredit=creditToApply;
        var paymentGroupId=Guid.NewGuid();
        foreach(var allocation in allocations)
        {
            var receipt=receipts[allocation.SupplierReceiptId];var remaining=receipt.Total-receipt.Payments.Sum(x=>x.Amount);
            if(allocation.Amount>remaining)throw new InvalidOperationException($"المبلغ المخصص للبوليصة {receipt.PolicyNumber} أكبر من المتبقي ({remaining:N2}).");
            var creditAmount=Math.Min(unappliedCredit,allocation.Amount);unappliedCredit-=creditAmount;
            db.SupplierPayments.Add(new SupplierPayment{PaymentGroupId=paymentGroupId,SupplierId=c.SupplierId,SupplierReceiptId=allocation.SupplierReceiptId,SupplierCreditId=creditAmount>0?credit!.Id:null,CreditAmount=creditAmount,Date=c.Date,Amount=allocation.Amount,PaymentMethodId=c.PaymentMethodId,Comments=comments,PublicComments=c.PublicComments});
        }
        if(credit is not null)credit.RemainingAmount-=creditToApply;
        var excess=c.TotalAmount-allocatedTotal;
        if(excess>0)
        {
            var sourceReceipt=receipts[allocations[^1].SupplierReceiptId];
            db.SupplierCredits.Add(new SupplierCredit{SupplierId=c.SupplierId,SourceSupplierReceiptId=sourceReceipt.Id,SourcePaymentGroupId=paymentGroupId,Date=c.Date,PaymentMethodId=c.PaymentMethodId,OriginalAmount=excess,RemainingAmount=excess,Comments=comments,PublicComments=c.PublicComments});
        }
        await db.SaveChangesAsync();await transaction.CommitAsync();
    }
    public async Task<SupplierCreditOption?> GetOldestSupplierCreditAsync(Guid supplierId,Guid? excludeSourcePaymentGroupId=null)
    {
        var credit=await db.SupplierCredits.AsNoTracking().Where(x=>x.SupplierId==supplierId&&x.RemainingAmount>0&&(!excludeSourcePaymentGroupId.HasValue||x.SourcePaymentGroupId!=excludeSourcePaymentGroupId.Value)).OrderBy(x=>x.Date).ThenBy(x=>x.CreatedAt)
            .Select(x=>new{x.Id,x.RemainingAmount,x.PaymentMethodId,PaymentMethodName=x.PaymentMethod.Name,SourcePolicyNumber=x.SourceSupplierReceipt.PolicyNumber,SourceDate=x.Date}).FirstOrDefaultAsync();
        return credit is null?null:new SupplierCreditOption(credit.Id,credit.RemainingAmount,credit.PaymentMethodId,credit.PaymentMethodName,credit.SourcePolicyNumber,credit.SourceDate,$"متبقي من الدفعة السابقة للبوليصة رقم {credit.SourcePolicyNumber} بتاريخ {credit.SourceDate:yyyy/MM/dd}");
    }
    public async Task DeleteSupplierPaymentsAsync(Guid id,string? userName)
    {
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var anchor=await db.SupplierPayments.SingleOrDefaultAsync(x=>x.Id==id)??throw new InvalidOperationException("حركة الدفع غير موجودة.");
        var group=anchor.PaymentGroupId.HasValue
            ? await db.SupplierPayments.Where(x=>x.PaymentGroupId==anchor.PaymentGroupId).ToListAsync()
            : await db.SupplierPayments.Where(x=>x.SupplierId==anchor.SupplierId&&x.Date==anchor.Date&&x.PaymentMethodId==anchor.PaymentMethodId&&x.Comments==anchor.Comments&&x.CreatedAt==anchor.CreatedAt).ToListAsync();
        if(anchor.PaymentGroupId.HasValue)
        {
            var generated=await db.SupplierCredits.SingleOrDefaultAsync(x=>x.SourcePaymentGroupId==anchor.PaymentGroupId);
            if(generated is not null)
            {
                var used=generated.OriginalAmount-generated.RemainingAmount;
                if(used>0)throw new InvalidOperationException($"لا يمكن حذف الدفعة لأن مبلغاً قدره {used:N2} من رصيدها استُخدم في بوالص لاحقة.");
                generated.IsDeleted=true;generated.DeletedAt=DateTimeOffset.UtcNow;generated.DeletedBy=userName;
            }
        }
        var creditIds=group.Where(x=>x.SupplierCreditId.HasValue).Select(x=>x.SupplierCreditId!.Value).Distinct().ToArray();
        var credits=await db.SupplierCredits.Where(x=>creditIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id);
        foreach(var payment in group)
        {
            if(payment.SupplierCreditId.HasValue)credits[payment.SupplierCreditId.Value].RemainingAmount+=payment.CreditAmount;
            payment.IsDeleted=true;payment.DeletedAt=DateTimeOffset.UtcNow;payment.DeletedBy=userName;
        }
        await db.SaveChangesAsync();await transaction.CommitAsync();
    }
    public async Task UpdateSupplierPaymentAsync(Guid id,SupplierPaymentCommand c)
    {
        EnsureNotFuture(c.Date);
        if(c.Amount<=0)throw new InvalidOperationException("مبلغ الدفع يجب أن يكون أكبر من صفر.");if(!await db.PaymentMethods.AnyAsync(x=>x.Id==c.PaymentMethodId))throw new InvalidOperationException("طريقة الدفع غير صالحة.");var payment=await db.SupplierPayments.SingleOrDefaultAsync(x=>x.Id==id)??throw new InvalidOperationException("حركة الدفع غير موجودة.");var receipt=await db.SupplierReceipts.Include(x=>x.Payments).SingleOrDefaultAsync(x=>x.Id==c.SupplierReceiptId&&x.SupplierId==c.SupplierId)??throw new InvalidOperationException("البوليصة لا تخص المورد المحدد.");var remaining=receipt.Total-receipt.Payments.Where(x=>x.Id!=id).Sum(x=>x.Amount);if(c.Amount>remaining)throw new InvalidOperationException($"المبلغ أكبر من المتبقي ({remaining:N2}).");payment.SupplierId=c.SupplierId;payment.SupplierReceiptId=c.SupplierReceiptId;payment.Date=c.Date;payment.Amount=c.Amount;payment.PaymentMethodId=c.PaymentMethodId;payment.Comments=c.Comments;payment.PublicComments=c.PublicComments;await db.SaveChangesAsync();
    }
    public async Task UpdateSupplierPaymentsAsync(Guid id,SupplierPaymentBatchCommand c)
    {
        EnsureNotFuture(c.Date);
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var anchor=await db.SupplierPayments.IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.Id==id&&!x.IsDeleted)??throw new InvalidOperationException("حركة الدفع غير موجودة.");
        if(c.SupplierId!=anchor.SupplierId)throw new InvalidOperationException("لا يمكن تغيير المورد المرتبط بحركة الدفع.");
        var group=anchor.PaymentGroupId.HasValue
            ? await db.SupplierPayments.Where(x=>x.PaymentGroupId==anchor.PaymentGroupId).ToListAsync()
            : await db.SupplierPayments.Where(x=>x.SupplierId==anchor.SupplierId&&x.Date==anchor.Date&&x.PaymentMethodId==anchor.PaymentMethodId&&x.Comments==anchor.Comments&&x.CreatedAt==anchor.CreatedAt).ToListAsync();
        var paymentGroupId=anchor.PaymentGroupId??Guid.NewGuid();
        var allocations=c.Allocations.Where(x=>x.Amount>0).ToList();
        if(allocations.Count==0)throw new InvalidOperationException("أدخل مبلغاً لبوليصة واحدة على الأقل.");
        var allocatedTotal=allocations.Sum(x=>x.Amount);
        if(c.TotalAmount<allocatedTotal)throw new InvalidOperationException($"إجمالي المبلغ أقل من المبلغ الموزع ({allocatedTotal:N2}).");
        if(allocations.GroupBy(x=>x.SupplierReceiptId).Any(x=>x.Count()>1))throw new InvalidOperationException("لا يمكن تكرار البوليصة في نفس حركة الدفع.");
        if(!await db.PaymentMethods.AnyAsync(x=>x.Id==c.PaymentMethodId))throw new InvalidOperationException("طريقة الدفع غير صالحة.");
        var ids=allocations.Select(x=>x.SupplierReceiptId).ToList();
        var groupIds=group.Select(x=>x.Id).ToArray();
        var receipts=await db.SupplierReceipts.Include(x=>x.Payments).Where(x=>ids.Contains(x.Id)&&x.SupplierId==c.SupplierId).ToDictionaryAsync(x=>x.Id);
        if(receipts.Count!=ids.Count)throw new InvalidOperationException("إحدى البوالص لا تخص المورد المحدد.");
        foreach(var allocation in allocations)
        {
            var receipt=receipts[allocation.SupplierReceiptId];
            var remaining=receipt.Total-receipt.Payments.Where(x=>!groupIds.Contains(x.Id)).Sum(x=>x.Amount);
            if(allocation.Amount>remaining)throw new InvalidOperationException($"المبلغ المخصص للبوليصة {receipt.PolicyNumber} أكبر من المتبقي ({remaining:N2}).");
        }

        var priorCreditIds=group.Where(x=>x.SupplierCreditId.HasValue).Select(x=>x.SupplierCreditId!.Value).Distinct().ToArray();
        var priorCredits=await db.SupplierCredits.Include(x=>x.SourceSupplierReceipt).Where(x=>priorCreditIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id);
        foreach(var payment in group.Where(x=>x.SupplierCreditId.HasValue))priorCredits[payment.SupplierCreditId!.Value].RemainingAmount+=payment.CreditAmount;
        SupplierCredit? credit=null;
        if(c.SupplierCreditId.HasValue)
        {
            credit=priorCredits.GetValueOrDefault(c.SupplierCreditId.Value)??await db.SupplierCredits.Include(x=>x.SourceSupplierReceipt).SingleOrDefaultAsync(x=>x.Id==c.SupplierCreditId&&x.SupplierId==c.SupplierId&&x.RemainingAmount>0);
            if(credit is null||credit.SourcePaymentGroupId==paymentGroupId)throw new InvalidOperationException("الرصيد المرحل المحدد غير متاح.");
            if(credit.PaymentMethodId!=c.PaymentMethodId)throw new InvalidOperationException("يجب استخدام طريقة السداد الخاصة بالرصيد المرحل.");
        }
        var comments=credit is null?c.Comments:$"متبقي من الدفعة السابقة للبوليصة رقم {credit.SourceSupplierReceipt.PolicyNumber} بتاريخ {credit.Date:yyyy/MM/dd}";
        var creditToApply=Math.Min(credit?.RemainingAmount??0m,allocatedTotal);
        if(credit is not null)credit.RemainingAmount-=creditToApply;

        var excess=c.TotalAmount-allocatedTotal;
        var generatedCredit=await db.SupplierCredits.SingleOrDefaultAsync(x=>x.SourcePaymentGroupId==paymentGroupId);
        if(generatedCredit is not null)
        {
            var alreadyUsed=generatedCredit.OriginalAmount-generatedCredit.RemainingAmount;
            if(excess<alreadyUsed)throw new InvalidOperationException($"لا يمكن خفض الرصيد الدائن عن المبلغ المستخدم منه ({alreadyUsed:N2}).");
            if(alreadyUsed>0&&generatedCredit.PaymentMethodId!=c.PaymentMethodId)throw new InvalidOperationException("لا يمكن تغيير طريقة سداد رصيد دائن استُخدم في بوالص لاحقة.");
            generatedCredit.OriginalAmount=excess;generatedCredit.RemainingAmount=excess-alreadyUsed;generatedCredit.Date=c.Date;generatedCredit.PaymentMethodId=c.PaymentMethodId;generatedCredit.Comments=comments;generatedCredit.PublicComments=c.PublicComments;generatedCredit.SourceSupplierReceiptId=allocations[^1].SupplierReceiptId;
            if(excess==0){generatedCredit.IsDeleted=true;generatedCredit.DeletedAt=DateTimeOffset.UtcNow;}
        }
        else if(excess>0)
        {
            db.SupplierCredits.Add(new SupplierCredit{SupplierId=c.SupplierId,SourceSupplierReceiptId=allocations[^1].SupplierReceiptId,SourcePaymentGroupId=paymentGroupId,Date=c.Date,PaymentMethodId=c.PaymentMethodId,OriginalAmount=excess,RemainingAmount=excess,Comments=comments,PublicComments=c.PublicComments});
        }

        var existing=group.GroupBy(x=>x.SupplierReceiptId).ToDictionary(x=>x.Key,x=>x.First());
        var duplicates=group.GroupBy(x=>x.SupplierReceiptId).SelectMany(x=>x.Skip(1)).ToList();
        var unappliedCredit=creditToApply;
        foreach(var allocation in allocations)
        {
            if(!existing.Remove(allocation.SupplierReceiptId,out var payment))
            {
                payment=new SupplierPayment{PaymentGroupId=paymentGroupId,SupplierId=c.SupplierId,SupplierReceiptId=allocation.SupplierReceiptId};db.SupplierPayments.Add(payment);
            }
            var creditAmount=Math.Min(unappliedCredit,allocation.Amount);unappliedCredit-=creditAmount;
            payment.PaymentGroupId=paymentGroupId;payment.SupplierId=c.SupplierId;payment.Date=c.Date;payment.Amount=allocation.Amount;payment.SupplierCreditId=creditAmount>0?credit!.Id:null;payment.CreditAmount=creditAmount;payment.PaymentMethodId=c.PaymentMethodId;payment.Comments=comments;payment.PublicComments=c.PublicComments;
        }
        foreach(var payment in existing.Values.Concat(duplicates)){payment.IsDeleted=true;payment.DeletedAt=DateTimeOffset.UtcNow;}
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    public async Task<List<PolicyOption>> GetUnassignedPoliciesAsync(Guid? includeDeliveryId=null)=>await db.SupplierReceipts.AsNoTracking().Where(x=>x.CustomerDelivery==null||(includeDeliveryId.HasValue&&x.CustomerDelivery!.Id==includeDeliveryId.Value)).OrderByDescending(x=>x.Date)
        .Select(x=>new PolicyOption(x.Id,x.PolicyNumber,x.Date,x.Material.Name,x.Material.UnitOfMeasure.Name,x.Quantity,0,x.UnitPrice,x.TaxPercent,x.Total)).ToListAsync();
    public async Task CreateCustomerDeliveryAsync(CustomerDeliveryCommand c)
    {
        EnsureNotFuture(c.Date);
        if(!await db.Customers.AnyAsync(x=>x.Id==c.CustomerId))throw new InvalidOperationException("العميل غير صالح.");var receipt=await db.SupplierReceipts.Include(x=>x.Material).SingleOrDefaultAsync(x=>x.Id==c.SupplierReceiptId)??throw new InvalidOperationException("البوليصة غير موجودة.");
        if(await db.CustomerDeliveries.AnyAsync(x=>x.SupplierReceiptId==c.SupplierReceiptId))throw new InvalidOperationException("تم تخصيص هذه البوليصة لعميل من قبل.");
        db.CustomerDeliveries.Add(new CustomerDelivery{CustomerId=c.CustomerId,SupplierReceiptId=c.SupplierReceiptId,Date=c.Date,UnitPrice=c.UnitPrice,TaxPercent=c.TaxPercent,Total=CalculateTotal(receipt.Quantity,c.UnitPrice,c.TaxPercent),Details=c.Details});await db.SaveChangesAsync();
    }
    public async Task UpdateCustomerDeliveryAsync(Guid id,CustomerDeliveryCommand c)
    {
        EnsureNotFuture(c.Date);
        if(!await db.Customers.AnyAsync(x=>x.Id==c.CustomerId))throw new InvalidOperationException("العميل غير صالح.");
        var x=await db.CustomerDeliveries.Include(x=>x.Payments).SingleOrDefaultAsync(x=>x.Id==id)??throw new InvalidOperationException("حركة التوريد غير موجودة.");if(x.Payments.Count>0)throw new InvalidOperationException("لا يمكن تعديل توريد مرتبط بقبض من العميل.");if(await db.CustomerDeliveries.AnyAsync(d=>d.Id!=id&&d.SupplierReceiptId==c.SupplierReceiptId))throw new InvalidOperationException("تم تخصيص هذه البوليصة لعميل من قبل.");var receipt=await db.SupplierReceipts.SingleOrDefaultAsync(r=>r.Id==c.SupplierReceiptId)??throw new InvalidOperationException("البوليصة غير موجودة.");x.CustomerId=c.CustomerId;x.SupplierReceiptId=c.SupplierReceiptId;x.Date=c.Date;x.UnitPrice=c.UnitPrice;x.TaxPercent=c.TaxPercent;x.Total=CalculateTotal(receipt.Quantity,c.UnitPrice,c.TaxPercent);x.Details=c.Details;await db.SaveChangesAsync();
    }
    public async Task<List<PolicyOption>> GetCustomerPoliciesAsync(Guid customerId,IReadOnlyCollection<Guid>? excludePaymentIds=null)
    {
        var excluded=excludePaymentIds?.ToArray()??[];
        return await db.CustomerDeliveries.AsNoTracking().Where(x=>x.CustomerId==customerId&&x.Total-x.Payments.Where(p=>!excluded.Contains(p.Id)).Sum(p=>(decimal?)p.Amount??0)>0).OrderBy(x=>x.SupplierReceipt.Date).ThenBy(x=>x.SupplierReceipt.PolicyNumber)
            .Select(x=>new PolicyOption(x.Id,x.SupplierReceipt.PolicyNumber,x.SupplierReceipt.Date,x.SupplierReceipt.Material.Name,x.SupplierReceipt.Material.UnitOfMeasure.Name,x.SupplierReceipt.Quantity,x.Total-x.Payments.Where(p=>!excluded.Contains(p.Id)).Sum(p=>(decimal?)p.Amount??0),x.SupplierReceipt.UnitPrice,x.SupplierReceipt.TaxPercent,x.SupplierReceipt.Total)).ToListAsync();
    }
    public async Task CreateCustomerPaymentAsync(CustomerPaymentCommand c)
    {
        await CreateCustomerPaymentsAsync(new(c.CustomerId,c.Date,c.PaymentMethodId,c.Comments,[new(c.CustomerDeliveryId,c.Amount)]) { PublicComments=c.PublicComments });
    }
    public async Task CreateCustomerPaymentsAsync(CustomerPaymentBatchCommand c)
    {
        EnsureNotFuture(c.Date);
        var allocations=c.Allocations.Where(x=>x.Amount>0).ToList();
        if(allocations.Count==0)throw new InvalidOperationException("أدخل مبلغاً لبوليصة واحدة على الأقل.");
        if(allocations.GroupBy(x=>x.CustomerDeliveryId).Any(x=>x.Count()>1))throw new InvalidOperationException("لا يمكن تكرار البوليصة في نفس حركة القبض.");
        if(!await db.PaymentMethods.AnyAsync(x=>x.Id==c.PaymentMethodId))throw new InvalidOperationException("طريقة الدفع غير صالحة.");
        var ids=allocations.Select(x=>x.CustomerDeliveryId).ToList();
        var deliveries=await db.CustomerDeliveries.Include(x=>x.Payments).Include(x=>x.SupplierReceipt).Where(x=>ids.Contains(x.Id)&&x.CustomerId==c.CustomerId).ToDictionaryAsync(x=>x.Id);
        if(deliveries.Count!=ids.Count)throw new InvalidOperationException("إحدى البوالص لا تخص العميل المحدد.");
        var paymentGroupId=Guid.NewGuid();
        foreach(var allocation in allocations)
        {
            var delivery=deliveries[allocation.CustomerDeliveryId];var remaining=delivery.Total-delivery.Payments.Sum(x=>x.Amount);
            if(allocation.Amount>remaining)throw new InvalidOperationException($"المبلغ المخصص للبوليصة {delivery.SupplierReceipt.PolicyNumber} أكبر من المتبقي ({remaining:N2}).");
            db.CustomerPayments.Add(new CustomerPayment{PaymentGroupId=paymentGroupId,CustomerId=c.CustomerId,CustomerDeliveryId=allocation.CustomerDeliveryId,Date=c.Date,Amount=allocation.Amount,PaymentMethodId=c.PaymentMethodId,Comments=c.Comments,PublicComments=c.PublicComments});
        }
        await db.SaveChangesAsync();
    }
    public async Task UpdateCustomerPaymentAsync(Guid id,CustomerPaymentCommand c)
    {
        EnsureNotFuture(c.Date);
        if(c.Amount<=0)throw new InvalidOperationException("مبلغ القبض يجب أن يكون أكبر من صفر.");if(!await db.PaymentMethods.AnyAsync(x=>x.Id==c.PaymentMethodId))throw new InvalidOperationException("طريقة الدفع غير صالحة.");var payment=await db.CustomerPayments.SingleOrDefaultAsync(x=>x.Id==id)??throw new InvalidOperationException("حركة القبض غير موجودة.");var delivery=await db.CustomerDeliveries.Include(x=>x.Payments).SingleOrDefaultAsync(x=>x.Id==c.CustomerDeliveryId&&x.CustomerId==c.CustomerId)??throw new InvalidOperationException("البوليصة لا تخص العميل المحدد.");var remaining=delivery.Total-delivery.Payments.Where(x=>x.Id!=id).Sum(x=>x.Amount);if(c.Amount>remaining)throw new InvalidOperationException($"المبلغ أكبر من المتبقي ({remaining:N2}).");payment.CustomerId=c.CustomerId;payment.CustomerDeliveryId=c.CustomerDeliveryId;payment.Date=c.Date;payment.Amount=c.Amount;payment.PaymentMethodId=c.PaymentMethodId;payment.Comments=c.Comments;payment.PublicComments=c.PublicComments;await db.SaveChangesAsync();
    }
    public async Task UpdateCustomerPaymentsAsync(Guid id,CustomerPaymentBatchCommand c)
    {
        EnsureNotFuture(c.Date);
        var anchor=await db.CustomerPayments.IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.Id==id&&!x.IsDeleted)??throw new InvalidOperationException("حركة القبض غير موجودة.");
        if(c.CustomerId!=anchor.CustomerId)throw new InvalidOperationException("لا يمكن تغيير العميل المرتبط بحركة القبض.");
        var group=anchor.PaymentGroupId.HasValue
            ? await db.CustomerPayments.Where(x=>x.PaymentGroupId==anchor.PaymentGroupId).ToListAsync()
            : await db.CustomerPayments.Where(x=>x.CustomerId==anchor.CustomerId&&x.Date==anchor.Date&&x.PaymentMethodId==anchor.PaymentMethodId&&x.Comments==anchor.Comments&&x.CreatedAt==anchor.CreatedAt).ToListAsync();
        var allocations=c.Allocations.Where(x=>x.Amount>0).ToList();
        if(allocations.Count==0)throw new InvalidOperationException("أدخل مبلغاً لبوليصة واحدة على الأقل.");
        if(allocations.GroupBy(x=>x.CustomerDeliveryId).Any(x=>x.Count()>1))throw new InvalidOperationException("لا يمكن تكرار البوليصة في نفس حركة القبض.");
        if(!await db.PaymentMethods.AnyAsync(x=>x.Id==c.PaymentMethodId))throw new InvalidOperationException("طريقة الدفع غير صالحة.");
        var ids=allocations.Select(x=>x.CustomerDeliveryId).ToList();
        var groupIds=group.Select(x=>x.Id).ToArray();
        var deliveries=await db.CustomerDeliveries.Include(x=>x.Payments).Include(x=>x.SupplierReceipt).Where(x=>ids.Contains(x.Id)&&x.CustomerId==c.CustomerId).ToDictionaryAsync(x=>x.Id);
        if(deliveries.Count!=ids.Count)throw new InvalidOperationException("إحدى البوالص لا تخص العميل المحدد.");
        foreach(var allocation in allocations)
        {
            var delivery=deliveries[allocation.CustomerDeliveryId];
            var remaining=delivery.Total-delivery.Payments.Where(x=>!groupIds.Contains(x.Id)).Sum(x=>x.Amount);
            if(allocation.Amount>remaining)throw new InvalidOperationException($"المبلغ المخصص للبوليصة {delivery.SupplierReceipt.PolicyNumber} أكبر من المتبقي ({remaining:N2}).");
        }
        var paymentGroupId=anchor.PaymentGroupId??Guid.NewGuid();
        var existing=group.GroupBy(x=>x.CustomerDeliveryId).ToDictionary(x=>x.Key,x=>x.First());
        foreach(var allocation in allocations)
        {
            if(!existing.Remove(allocation.CustomerDeliveryId,out var payment))
            {
                payment=new CustomerPayment{PaymentGroupId=paymentGroupId,CustomerId=c.CustomerId,CustomerDeliveryId=allocation.CustomerDeliveryId};
                db.CustomerPayments.Add(payment);
            }
            payment.PaymentGroupId=paymentGroupId;payment.CustomerId=c.CustomerId;payment.Date=c.Date;payment.Amount=allocation.Amount;payment.PaymentMethodId=c.PaymentMethodId;payment.Comments=c.Comments;payment.PublicComments=c.PublicComments;
        }
        foreach(var payment in existing.Values){payment.IsDeleted=true;payment.DeletedAt=DateTimeOffset.UtcNow;}
        await db.SaveChangesAsync();
    }
}
