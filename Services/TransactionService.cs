using Microsoft.EntityFrameworkCore;using MYOB.Data;using MYOB.DTOs;using MYOB.Models;
namespace MYOB.Services;
public interface ITransactionService
{
    decimal CalculateTotal(decimal quantity,decimal unitPrice,decimal taxPercent);
    Task CreateSupplierReceiptAsync(SupplierReceiptCommand command);Task CreateSupplierPaymentAsync(SupplierPaymentCommand command);
    Task CreateCustomerDeliveryAsync(CustomerDeliveryCommand command);Task CreateCustomerPaymentAsync(CustomerPaymentCommand command);
    Task UpdateSupplierReceiptAsync(Guid id,SupplierReceiptCommand command);Task UpdateSupplierPaymentAsync(Guid id,SupplierPaymentCommand command);
    Task UpdateCustomerDeliveryAsync(Guid id,CustomerDeliveryCommand command);Task UpdateCustomerPaymentAsync(Guid id,CustomerPaymentCommand command);
    Task<List<PolicyOption>> GetSupplierPoliciesAsync(Guid supplierId,Guid? excludePaymentId=null);Task<List<PolicyOption>> GetUnassignedPoliciesAsync(Guid? includeDeliveryId=null);Task<List<PolicyOption>> GetCustomerPoliciesAsync(Guid customerId,Guid? excludePaymentId=null);
}
public sealed class TransactionService(ApplicationDbContext db):ITransactionService
{
    public decimal CalculateTotal(decimal quantity,decimal unitPrice,decimal taxPercent)
    {
        if(quantity<=0||unitPrice<0||taxPercent<0||taxPercent>100)throw new InvalidOperationException("القيم المالية المدخلة غير صحيحة.");
        return decimal.Round(quantity*unitPrice*(1m+taxPercent/100m),2,MidpointRounding.AwayFromZero);
    }
    public async Task CreateSupplierReceiptAsync(SupplierReceiptCommand c)
    {
        if(!await db.Suppliers.AnyAsync(x=>x.Id==c.SupplierId)||!await db.Materials.AnyAsync(x=>x.Id==c.MaterialId))throw new InvalidOperationException("المورد أو الخامة غير صالح.");
        if(string.IsNullOrWhiteSpace(c.PolicyNumber))throw new InvalidOperationException("رقم البوليصة مطلوب.");
        if(await db.SupplierReceipts.AnyAsync(x=>x.PolicyNumber==c.PolicyNumber.Trim()))throw new InvalidOperationException("رقم البوليصة مسجل مسبقاً.");
        db.SupplierReceipts.Add(new SupplierReceipt{SupplierId=c.SupplierId,MaterialId=c.MaterialId,Date=c.Date,PolicyNumber=c.PolicyNumber.Trim(),Quantity=c.Quantity,UnitPrice=c.UnitPrice,TaxPercent=c.TaxPercent,Total=CalculateTotal(c.Quantity,c.UnitPrice,c.TaxPercent),Details=c.Details});await db.SaveChangesAsync();
    }
    public async Task UpdateSupplierReceiptAsync(Guid id,SupplierReceiptCommand c)
    {
        if(!await db.Suppliers.AnyAsync(x=>x.Id==c.SupplierId)||!await db.Materials.AnyAsync(x=>x.Id==c.MaterialId))throw new InvalidOperationException("المورد أو الخامة غير صالح.");
        var x=await db.SupplierReceipts.Include(x=>x.Payments).Include(x=>x.CustomerDelivery).SingleOrDefaultAsync(x=>x.Id==id)??throw new InvalidOperationException("الحركة غير موجودة.");if(x.Payments.Count>0||x.CustomerDelivery is not null)throw new InvalidOperationException("لا يمكن تعديل استلام مرتبط بدفع أو توريد لعميل.");
        if(await db.SupplierReceipts.AnyAsync(r=>r.Id!=id&&r.PolicyNumber==c.PolicyNumber.Trim()))throw new InvalidOperationException("رقم البوليصة مسجل مسبقاً.");x.SupplierId=c.SupplierId;x.MaterialId=c.MaterialId;x.Date=c.Date;x.PolicyNumber=c.PolicyNumber.Trim();x.Quantity=c.Quantity;x.UnitPrice=c.UnitPrice;x.TaxPercent=c.TaxPercent;x.Total=CalculateTotal(c.Quantity,c.UnitPrice,c.TaxPercent);x.Details=c.Details;await db.SaveChangesAsync();
    }
    public async Task<List<PolicyOption>> GetSupplierPoliciesAsync(Guid supplierId,Guid? excludePaymentId=null)=>await db.SupplierReceipts.AsNoTracking().Where(x=>x.SupplierId==supplierId&&x.Total-x.Payments.Where(p=>!excludePaymentId.HasValue||p.Id!=excludePaymentId.Value).Sum(p=>(decimal?)p.Amount??0)>0)
        .OrderByDescending(x=>x.Date).Select(x=>new PolicyOption(x.Id,x.PolicyNumber,x.Material.Name,x.Material.UnitOfMeasure.Name,x.Quantity,x.Total-x.Payments.Where(p=>!excludePaymentId.HasValue||p.Id!=excludePaymentId.Value).Sum(p=>(decimal?)p.Amount??0),x.UnitPrice,x.TaxPercent,x.Total)).ToListAsync();
    public async Task CreateSupplierPaymentAsync(SupplierPaymentCommand c)
    {
        if(c.Amount<=0)throw new InvalidOperationException("مبلغ الدفع يجب أن يكون أكبر من صفر.");if(!await db.PaymentMethods.AnyAsync(x=>x.Id==c.PaymentMethodId))throw new InvalidOperationException("طريقة الدفع غير صالحة.");
        var receipt=await db.SupplierReceipts.Include(x=>x.Payments).SingleOrDefaultAsync(x=>x.Id==c.SupplierReceiptId&&x.SupplierId==c.SupplierId)??throw new InvalidOperationException("البوليصة لا تخص المورد المحدد.");
        var remaining=receipt.Total-receipt.Payments.Sum(x=>x.Amount);if(c.Amount>remaining)throw new InvalidOperationException($"المبلغ أكبر من المتبقي ({remaining:N2}).");
        db.SupplierPayments.Add(new SupplierPayment{SupplierId=c.SupplierId,SupplierReceiptId=c.SupplierReceiptId,Date=c.Date,Amount=c.Amount,PaymentMethodId=c.PaymentMethodId,Comments=c.Comments});await db.SaveChangesAsync();
    }
    public async Task UpdateSupplierPaymentAsync(Guid id,SupplierPaymentCommand c)
    {
        if(c.Amount<=0)throw new InvalidOperationException("مبلغ الدفع يجب أن يكون أكبر من صفر.");if(!await db.PaymentMethods.AnyAsync(x=>x.Id==c.PaymentMethodId))throw new InvalidOperationException("طريقة الدفع غير صالحة.");var payment=await db.SupplierPayments.SingleOrDefaultAsync(x=>x.Id==id)??throw new InvalidOperationException("حركة الدفع غير موجودة.");var receipt=await db.SupplierReceipts.Include(x=>x.Payments).SingleOrDefaultAsync(x=>x.Id==c.SupplierReceiptId&&x.SupplierId==c.SupplierId)??throw new InvalidOperationException("البوليصة لا تخص المورد المحدد.");var remaining=receipt.Total-receipt.Payments.Where(x=>x.Id!=id).Sum(x=>x.Amount);if(c.Amount>remaining)throw new InvalidOperationException($"المبلغ أكبر من المتبقي ({remaining:N2}).");payment.SupplierId=c.SupplierId;payment.SupplierReceiptId=c.SupplierReceiptId;payment.Date=c.Date;payment.Amount=c.Amount;payment.PaymentMethodId=c.PaymentMethodId;payment.Comments=c.Comments;await db.SaveChangesAsync();
    }
    public async Task<List<PolicyOption>> GetUnassignedPoliciesAsync(Guid? includeDeliveryId=null)=>await db.SupplierReceipts.AsNoTracking().Where(x=>x.CustomerDelivery==null||(includeDeliveryId.HasValue&&x.CustomerDelivery!.Id==includeDeliveryId.Value)).OrderByDescending(x=>x.Date)
        .Select(x=>new PolicyOption(x.Id,x.PolicyNumber,x.Material.Name,x.Material.UnitOfMeasure.Name,x.Quantity,0,x.UnitPrice,x.TaxPercent,x.Total)).ToListAsync();
    public async Task CreateCustomerDeliveryAsync(CustomerDeliveryCommand c)
    {
        if(!await db.Customers.AnyAsync(x=>x.Id==c.CustomerId))throw new InvalidOperationException("العميل غير صالح.");var receipt=await db.SupplierReceipts.Include(x=>x.Material).SingleOrDefaultAsync(x=>x.Id==c.SupplierReceiptId)??throw new InvalidOperationException("البوليصة غير موجودة.");
        if(await db.CustomerDeliveries.AnyAsync(x=>x.SupplierReceiptId==c.SupplierReceiptId))throw new InvalidOperationException("تم تخصيص هذه البوليصة لعميل من قبل.");
        db.CustomerDeliveries.Add(new CustomerDelivery{CustomerId=c.CustomerId,SupplierReceiptId=c.SupplierReceiptId,Date=c.Date,UnitPrice=c.UnitPrice,TaxPercent=c.TaxPercent,Total=CalculateTotal(receipt.Quantity,c.UnitPrice,c.TaxPercent),Details=c.Details});await db.SaveChangesAsync();
    }
    public async Task UpdateCustomerDeliveryAsync(Guid id,CustomerDeliveryCommand c)
    {
        if(!await db.Customers.AnyAsync(x=>x.Id==c.CustomerId))throw new InvalidOperationException("العميل غير صالح.");
        var x=await db.CustomerDeliveries.Include(x=>x.Payments).SingleOrDefaultAsync(x=>x.Id==id)??throw new InvalidOperationException("حركة التوريد غير موجودة.");if(x.Payments.Count>0)throw new InvalidOperationException("لا يمكن تعديل توريد مرتبط بقبض من العميل.");if(await db.CustomerDeliveries.AnyAsync(d=>d.Id!=id&&d.SupplierReceiptId==c.SupplierReceiptId))throw new InvalidOperationException("تم تخصيص هذه البوليصة لعميل من قبل.");var receipt=await db.SupplierReceipts.SingleOrDefaultAsync(r=>r.Id==c.SupplierReceiptId)??throw new InvalidOperationException("البوليصة غير موجودة.");x.CustomerId=c.CustomerId;x.SupplierReceiptId=c.SupplierReceiptId;x.Date=c.Date;x.UnitPrice=c.UnitPrice;x.TaxPercent=c.TaxPercent;x.Total=CalculateTotal(receipt.Quantity,c.UnitPrice,c.TaxPercent);x.Details=c.Details;await db.SaveChangesAsync();
    }
    public async Task<List<PolicyOption>> GetCustomerPoliciesAsync(Guid customerId,Guid? excludePaymentId=null)=>await db.CustomerDeliveries.AsNoTracking().Where(x=>x.CustomerId==customerId&&x.Total-x.Payments.Where(p=>!excludePaymentId.HasValue||p.Id!=excludePaymentId.Value).Sum(p=>(decimal?)p.Amount??0)>0).OrderByDescending(x=>x.Date)
        .Select(x=>new PolicyOption(x.Id,x.SupplierReceipt.PolicyNumber,x.SupplierReceipt.Material.Name,x.SupplierReceipt.Material.UnitOfMeasure.Name,x.SupplierReceipt.Quantity,x.Total-x.Payments.Where(p=>!excludePaymentId.HasValue||p.Id!=excludePaymentId.Value).Sum(p=>(decimal?)p.Amount??0),x.SupplierReceipt.UnitPrice,x.SupplierReceipt.TaxPercent,x.SupplierReceipt.Total)).ToListAsync();
    public async Task CreateCustomerPaymentAsync(CustomerPaymentCommand c)
    {
        if(c.Amount<=0)throw new InvalidOperationException("مبلغ القبض يجب أن يكون أكبر من صفر.");if(!await db.PaymentMethods.AnyAsync(x=>x.Id==c.PaymentMethodId))throw new InvalidOperationException("طريقة الدفع غير صالحة.");
        var delivery=await db.CustomerDeliveries.Include(x=>x.Payments).SingleOrDefaultAsync(x=>x.Id==c.CustomerDeliveryId&&x.CustomerId==c.CustomerId)??throw new InvalidOperationException("البوليصة لا تخص العميل المحدد.");var remaining=delivery.Total-delivery.Payments.Sum(x=>x.Amount);if(c.Amount>remaining)throw new InvalidOperationException($"المبلغ أكبر من المتبقي ({remaining:N2}).");
        db.CustomerPayments.Add(new CustomerPayment{CustomerId=c.CustomerId,CustomerDeliveryId=c.CustomerDeliveryId,Date=c.Date,Amount=c.Amount,PaymentMethodId=c.PaymentMethodId,Comments=c.Comments});await db.SaveChangesAsync();
    }
    public async Task UpdateCustomerPaymentAsync(Guid id,CustomerPaymentCommand c)
    {
        if(c.Amount<=0)throw new InvalidOperationException("مبلغ القبض يجب أن يكون أكبر من صفر.");if(!await db.PaymentMethods.AnyAsync(x=>x.Id==c.PaymentMethodId))throw new InvalidOperationException("طريقة الدفع غير صالحة.");var payment=await db.CustomerPayments.SingleOrDefaultAsync(x=>x.Id==id)??throw new InvalidOperationException("حركة القبض غير موجودة.");var delivery=await db.CustomerDeliveries.Include(x=>x.Payments).SingleOrDefaultAsync(x=>x.Id==c.CustomerDeliveryId&&x.CustomerId==c.CustomerId)??throw new InvalidOperationException("البوليصة لا تخص العميل المحدد.");var remaining=delivery.Total-delivery.Payments.Where(x=>x.Id!=id).Sum(x=>x.Amount);if(c.Amount>remaining)throw new InvalidOperationException($"المبلغ أكبر من المتبقي ({remaining:N2}).");payment.CustomerId=c.CustomerId;payment.CustomerDeliveryId=c.CustomerDeliveryId;payment.Date=c.Date;payment.Amount=c.Amount;payment.PaymentMethodId=c.PaymentMethodId;payment.Comments=c.Comments;await db.SaveChangesAsync();
    }
}
