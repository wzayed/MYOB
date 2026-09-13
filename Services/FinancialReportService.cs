using Microsoft.EntityFrameworkCore;
using MYOB.Data;
using MYOB.DTOs;

namespace MYOB.Services;

public interface IFinancialReportService
{
    Task<AccountStatementResult?> GetSupplierStatementAsync(AccountStatementFilter filter);
    Task<AccountStatementResult?> GetCustomerStatementAsync(AccountStatementFilter filter);
    Task<AccountStatementResult?> GetTotalSupplierStatementAsync(AccountStatementFilter filter);
    Task<AccountStatementResult?> GetTotalCustomerStatementAsync(AccountStatementFilter filter);
    Task<ProfitLossResult> GetProfitLossAsync(ProfitLossFilter filter);
}

public sealed class FinancialReportService(ApplicationDbContext db) : IFinancialReportService
{
    public async Task<AccountStatementResult?> GetSupplierStatementAsync(AccountStatementFilter filter)
    {
        if (!Valid(filter.FromDate, filter.ToDate) || !filter.PartyId.HasValue) return null;
        var supplier = await db.Suppliers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == filter.PartyId.Value);
        if (supplier is null) return null;
        var customer = filter.RelatedPartyId.HasValue
            ? await db.Customers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == filter.RelatedPartyId.Value)
            : null;
        if (filter.RelatedPartyId.HasValue && customer is null) return null;
        var from = filter.FromDate!.Value;
        var to = filter.ToDate!.Value;
        var receiptQuery = db.SupplierReceipts.Where(x => x.SupplierId == supplier.Id);
        var paymentQuery = db.SupplierPayments.Where(x => x.SupplierId == supplier.Id);
        var creditQuery = db.SupplierCredits.Where(x => x.SupplierId == supplier.Id);
        if (customer is not null)
        {
            receiptQuery = receiptQuery.Where(x => x.CustomerDelivery != null && x.CustomerDelivery.CustomerId == customer.Id);
            paymentQuery = paymentQuery.Where(x => x.SupplierReceipt.CustomerDelivery != null && x.SupplierReceipt.CustomerDelivery.CustomerId == customer.Id);
            creditQuery = creditQuery.Where(x => x.SourceSupplierReceipt.CustomerDelivery != null && x.SourceSupplierReceipt.CustomerDelivery.CustomerId == customer.Id);
        }
        var opening = (customer is null ? supplier.OpeningBalance : 0m)
            + (await receiptQuery.Where(x => x.Date < from).SumAsync(x => (decimal?)x.Total) ?? 0m);
        opening -= await paymentQuery.Where(x => x.Date < from).SumAsync(x => (decimal?)(x.Amount - x.CreditAmount)) ?? 0m;
        opening -= await creditQuery.Where(x => x.Date < from).SumAsync(x => (decimal?)x.OriginalAmount) ?? 0m;
        var receipts = await receiptQuery.AsNoTracking().Where(x => x.Date >= from && x.Date <= to)
            .Select(x => new Movement(x.Date, "استلام - " + x.PolicyNumber + " - " + x.Material.Name + " - " + x.Quantity + " × " + x.UnitPrice, x.Total, 0m, x.CreatedAt)).ToListAsync();
        var payments = await paymentQuery.AsNoTracking().Where(x => x.Date >= from && x.Date <= to && x.Amount > x.CreditAmount)
            .Select(x => new Movement(x.Date, "دفع - " + x.SupplierReceipt.PolicyNumber + " - " + x.PaymentMethod.Name + ((filter.ShowPrivateNotes ? x.Comments : x.PublicComments) == null ? "" : " - " + (filter.ShowPrivateNotes ? x.Comments : x.PublicComments)), 0m, x.Amount - x.CreditAmount, x.CreatedAt)).ToListAsync();
        var credits = await creditQuery.AsNoTracking().Where(x => x.Date >= from && x.Date <= to)
            .Select(x => new Movement(x.Date, "رصيد دائن - متبقي من دفعة البوليصة " + x.SourceSupplierReceipt.PolicyNumber + " - " + x.PaymentMethod.Name + ((filter.ShowPrivateNotes ? x.Comments : x.PublicComments) == null ? "" : " - " + (filter.ShowPrivateNotes ? x.Comments : x.PublicComments)), 0m, x.OriginalAmount, x.CreatedAt)).ToListAsync();
        return Build(supplier.Name, customer?.Name, opening, receipts.Concat(payments).Concat(credits));
    }

    public async Task<AccountStatementResult?> GetCustomerStatementAsync(AccountStatementFilter filter)
    {
        if (!Valid(filter.FromDate, filter.ToDate) || !filter.PartyId.HasValue) return null;
        var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == filter.PartyId.Value);
        if (customer is null) return null;
        var supplier = filter.RelatedPartyId.HasValue
            ? await db.Suppliers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == filter.RelatedPartyId.Value)
            : null;
        if (filter.RelatedPartyId.HasValue && supplier is null) return null;
        var from = filter.FromDate!.Value;
        var to = filter.ToDate!.Value;
        var deliveryQuery = db.CustomerDeliveries.Where(x => x.CustomerId == customer.Id);
        var paymentQuery = db.CustomerPayments.Where(x => x.CustomerId == customer.Id);
        if (supplier is not null)
        {
            deliveryQuery = deliveryQuery.Where(x => x.SupplierReceipt.SupplierId == supplier.Id);
            paymentQuery = paymentQuery.Where(x => x.CustomerDelivery.SupplierReceipt.SupplierId == supplier.Id);
        }
        var opening = (supplier is null ? customer.OpeningBalance : 0m)
            + (await deliveryQuery.Where(x => x.Date < from).SumAsync(x => (decimal?)x.Total) ?? 0m);
        opening -= await paymentQuery.Where(x => x.Date < from).SumAsync(x => (decimal?)x.Amount) ?? 0m;
        var deliveries = await deliveryQuery.AsNoTracking().Where(x => x.Date >= from && x.Date <= to)
            .Select(x => new Movement(x.Date, "توريد - " + x.SupplierReceipt.PolicyNumber + " - " + x.SupplierReceipt.Material.Name + " - " + x.SupplierReceipt.Quantity + " × " + x.UnitPrice, x.Total, 0m, x.CreatedAt)).ToListAsync();
        var payments = await paymentQuery.AsNoTracking().Where(x => x.Date >= from && x.Date <= to)
            .Select(x => new Movement(x.Date, "قبض - " + x.CustomerDelivery.SupplierReceipt.PolicyNumber + " - " + x.PaymentMethod.Name + ((filter.ShowPrivateNotes ? x.Comments : x.PublicComments) == null ? "" : " - " + (filter.ShowPrivateNotes ? x.Comments : x.PublicComments)), 0m, x.Amount, x.CreatedAt)).ToListAsync();
        return Build(customer.Name, supplier?.Name, opening, deliveries.Concat(payments));
    }

    public async Task<AccountStatementResult?> GetTotalSupplierStatementAsync(AccountStatementFilter filter)
    {
        if (!Valid(filter.FromDate, filter.ToDate) || !filter.PartyId.HasValue) return null;
        var supplier = await db.Suppliers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == filter.PartyId.Value);
        if (supplier is null) return null;
        var customer = filter.RelatedPartyId.HasValue
            ? await db.Customers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == filter.RelatedPartyId.Value)
            : null;
        if (filter.RelatedPartyId.HasValue && customer is null) return null;
        var from = filter.FromDate!.Value;
        var to = filter.ToDate!.Value;
        var receiptQuery = db.SupplierReceipts.Where(x => x.SupplierId == supplier.Id);
        var paymentQuery = db.SupplierPayments.Where(x => x.SupplierId == supplier.Id);
        var creditQuery = db.SupplierCredits.Where(x => x.SupplierId == supplier.Id);
        if (customer is not null)
        {
            receiptQuery = receiptQuery.Where(x => x.CustomerDelivery != null && x.CustomerDelivery.CustomerId == customer.Id);
            paymentQuery = paymentQuery.Where(x => x.SupplierReceipt.CustomerDelivery != null && x.SupplierReceipt.CustomerDelivery.CustomerId == customer.Id);
            creditQuery = creditQuery.Where(x => x.SourceSupplierReceipt.CustomerDelivery != null && x.SourceSupplierReceipt.CustomerDelivery.CustomerId == customer.Id);
        }
        var opening = (customer is null ? supplier.OpeningBalance : 0m)
            + (await receiptQuery.Where(x => x.Date < from).SumAsync(x => (decimal?)x.Total) ?? 0m)
            - (await paymentQuery.Where(x => x.Date < from).SumAsync(x => (decimal?)(x.Amount - x.CreditAmount)) ?? 0m)
            - (await creditQuery.Where(x => x.Date < from).SumAsync(x => (decimal?)x.OriginalAmount) ?? 0m);
        var receipts = await receiptQuery.AsNoTracking().Where(x => x.Date >= from && x.Date <= to)
            .Select(x => new Movement(x.Date, "استلام - " + x.PolicyNumber + " - " + x.Material.Name + " - " + x.Quantity + " × " + x.UnitPrice, x.Total, 0m, x.CreatedAt)).ToListAsync();
        var paymentData = await paymentQuery.AsNoTracking().Where(x => x.Date >= from && x.Date <= to)
            .Select(x => new { x.Id, x.PaymentGroupId, x.Date, x.Amount, x.CreditAmount, PaymentMethod = x.PaymentMethod.Name, Comments = filter.ShowPrivateNotes ? x.Comments : x.PublicComments, x.CreatedAt }).ToListAsync();
        var generatedCredits = await creditQuery.AsNoTracking().Where(x => x.Date >= from && x.Date <= to)
            .Select(x => new { x.SourcePaymentGroupId, x.OriginalAmount }).ToListAsync();
        var creditByGroup = generatedCredits.GroupBy(x => x.SourcePaymentGroupId).ToDictionary(x => x.Key, x => x.Sum(y => y.OriginalAmount));
        var paymentMovements = new List<Movement>();
        foreach (var group in paymentData.GroupBy(x => x.PaymentGroupId.HasValue ? "G:" + x.PaymentGroupId.Value : "P:" + x.Id))
        {
            var first = group.OrderBy(x => x.CreatedAt).First();
            var generated = first.PaymentGroupId.HasValue ? creditByGroup.GetValueOrDefault(first.PaymentGroupId.Value) : 0m;
            var grossTotal = group.Sum(x => x.Amount) + generated;
            var details = "دفع إجمالي - " + first.PaymentMethod + (string.IsNullOrWhiteSpace(first.Comments) ? "" : " - " + first.Comments);
            paymentMovements.Add(new(first.Date, details, 0m, grossTotal, first.CreatedAt));
            var appliedCredit = group.Sum(x => x.CreditAmount);
            if (appliedCredit > 0)
                paymentMovements.Add(new(first.Date, "تسوية رصيد مرحل مستخدم", appliedCredit, 0m, first.CreatedAt));
        }
        return Build(supplier.Name, customer?.Name, opening, receipts.Concat(paymentMovements));
    }

    public async Task<AccountStatementResult?> GetTotalCustomerStatementAsync(AccountStatementFilter filter)
    {
        if (!Valid(filter.FromDate, filter.ToDate) || !filter.PartyId.HasValue) return null;
        var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == filter.PartyId.Value);
        if (customer is null) return null;
        var supplier = filter.RelatedPartyId.HasValue
            ? await db.Suppliers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == filter.RelatedPartyId.Value)
            : null;
        if (filter.RelatedPartyId.HasValue && supplier is null) return null;
        var from = filter.FromDate!.Value;
        var to = filter.ToDate!.Value;
        var deliveryQuery = db.CustomerDeliveries.Where(x => x.CustomerId == customer.Id);
        var paymentQuery = db.CustomerPayments.Where(x => x.CustomerId == customer.Id);
        if (supplier is not null)
        {
            deliveryQuery = deliveryQuery.Where(x => x.SupplierReceipt.SupplierId == supplier.Id);
            paymentQuery = paymentQuery.Where(x => x.CustomerDelivery.SupplierReceipt.SupplierId == supplier.Id);
        }
        var opening = (supplier is null ? customer.OpeningBalance : 0m)
            + (await deliveryQuery.Where(x => x.Date < from).SumAsync(x => (decimal?)x.Total) ?? 0m)
            - (await paymentQuery.Where(x => x.Date < from).SumAsync(x => (decimal?)x.Amount) ?? 0m);
        var deliveries = await deliveryQuery.AsNoTracking().Where(x => x.Date >= from && x.Date <= to)
            .Select(x => new Movement(x.Date, "توريد - " + x.SupplierReceipt.PolicyNumber + " - " + x.SupplierReceipt.Material.Name + " - " + x.SupplierReceipt.Quantity + " × " + x.UnitPrice, x.Total, 0m, x.CreatedAt)).ToListAsync();
        var paymentData = await paymentQuery.AsNoTracking().Where(x => x.Date >= from && x.Date <= to)
            .Select(x => new { x.Id, x.PaymentGroupId, x.Date, x.Amount, PaymentMethod = x.PaymentMethod.Name, Comments = filter.ShowPrivateNotes ? x.Comments : x.PublicComments, x.CreatedAt }).ToListAsync();
        var payments = paymentData.GroupBy(x => x.PaymentGroupId.HasValue ? "G:" + x.PaymentGroupId.Value : "P:" + x.Id)
            .Select(group =>
            {
                var first = group.OrderBy(x => x.CreatedAt).First();
                var details = "قبض إجمالي - " + first.PaymentMethod + (string.IsNullOrWhiteSpace(first.Comments) ? "" : " - " + first.Comments);
                return new Movement(first.Date, details, 0m, group.Sum(x => x.Amount), first.CreatedAt);
            }).ToList();
        return Build(customer.Name, supplier?.Name, opening, deliveries.Concat(payments));
    }

    public async Task<ProfitLossResult> GetProfitLossAsync(ProfitLossFilter filter)
    {
        if (!Valid(filter.FromDate, filter.ToDate)) return new([], [], []);
        var from = filter.FromDate!.Value;
        var to = filter.ToDate!.Value;
        var query = db.CustomerDeliveries.AsNoTracking().Where(x => x.Date <= to);
        if (filter.SupplierId.HasValue) query = query.Where(x => x.SupplierReceipt.SupplierId == filter.SupplierId.Value);
        if (filter.CustomerId.HasValue) query = query.Where(x => x.CustomerId == filter.CustomerId.Value);
        var policies = await query.OrderBy(x => x.Date).Select(x => new
        {
            SupplierName=x.SupplierReceipt.Supplier.Name,CustomerName=x.Customer.Name,x.SupplierReceipt.PolicyNumber,
            GrossProfit=x.SupplierReceipt.Quantity*(x.UnitPrice-x.SupplierReceipt.UnitPrice),SaleTotal=x.Total,
            CollectedInPeriod=x.Payments.Where(p=>p.Date>=from&&p.Date<=to).Sum(p=>(decimal?)p.Amount)??0m,
            CollectedToDate=x.Payments.Where(p=>p.Date<=to).Sum(p=>(decimal?)p.Amount)??0m
        }).ToListAsync();
        var realized=new List<ProfitLossRow>();var unrealized=new List<ProfitLossRow>();
        foreach(var policy in policies)
        {
            var periodRatio=policy.SaleTotal<=0?0m:Math.Clamp(policy.CollectedInPeriod/policy.SaleTotal,0m,1m);
            var remainingRatio=policy.SaleTotal<=0?0m:Math.Clamp((policy.SaleTotal-policy.CollectedToDate)/policy.SaleTotal,0m,1m);
            var realizedValue=decimal.Round(policy.GrossProfit*periodRatio,2,MidpointRounding.AwayFromZero);
            var unrealizedValue=decimal.Round(policy.GrossProfit*remainingRatio,2,MidpointRounding.AwayFromZero);
            if(policy.CollectedInPeriod>0)realized.Add(new(policy.SupplierName,policy.CustomerName,policy.PolicyNumber,realizedValue,true));
            if(policy.CollectedToDate<policy.SaleTotal)unrealized.Add(new(policy.SupplierName,policy.CustomerName,policy.PolicyNumber,unrealizedValue,false));
        }
        var undelivered=new List<UndeliveredGoodsRow>();
        if(!filter.CustomerId.HasValue)
        {
            var inventoryQuery=db.SupplierReceipts.AsNoTracking().Where(x=>x.Date>=from&&x.Date<=to&&x.CustomerDelivery==null);
            if(filter.SupplierId.HasValue)inventoryQuery=inventoryQuery.Where(x=>x.SupplierId==filter.SupplierId.Value);
            undelivered=await inventoryQuery.OrderBy(x=>x.Date).Select(x=>new UndeliveredGoodsRow(x.Supplier.Name,x.PolicyNumber,x.Material.Name,x.Quantity,x.Total)).ToListAsync();
        }
        return new(realized,unrealized,undelivered);
    }

    private static bool Valid(DateOnly? from, DateOnly? to) => from.HasValue && to.HasValue && from.Value <= to.Value;

    private static AccountStatementResult Build(string name, string? relatedPartyName, decimal opening, IEnumerable<Movement> movements)
    {
        var balance = opening;
        var rows = new List<AccountStatementRow>();
        foreach (var movement in movements.OrderBy(x => x.Date).ThenBy(x => x.CreatedAt))
        {
            balance += movement.Debit - movement.Credit;
            rows.Add(new(movement.Date, movement.Details, movement.Debit, movement.Credit, balance));
        }
        return new(name, relatedPartyName, opening, rows, rows.Sum(x=>x.Debit), rows.Sum(x=>x.Credit), balance);
    }

    private sealed record Movement(DateOnly Date, string Details, decimal Debit, decimal Credit, DateTimeOffset CreatedAt);
}
