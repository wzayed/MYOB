using Microsoft.EntityFrameworkCore;
using MYOB.Data;
using MYOB.DTOs;

namespace MYOB.Services;

public interface IFinancialReportService
{
    Task<AccountStatementResult?> GetSupplierStatementAsync(AccountStatementFilter filter);
    Task<AccountStatementResult?> GetCustomerStatementAsync(AccountStatementFilter filter);
    Task<ProfitLossResult> GetProfitLossAsync(ProfitLossFilter filter);
}

public sealed class FinancialReportService(ApplicationDbContext db) : IFinancialReportService
{
    public async Task<AccountStatementResult?> GetSupplierStatementAsync(AccountStatementFilter filter)
    {
        if (!Valid(filter.FromDate, filter.ToDate) || !filter.PartyId.HasValue) return null;
        var supplier = await db.Suppliers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == filter.PartyId.Value);
        if (supplier is null) return null;
        var from = filter.FromDate!.Value;
        var to = filter.ToDate!.Value;
        var opening = supplier.OpeningBalance
            + (await db.SupplierReceipts.Where(x => x.SupplierId == supplier.Id && x.Date < from).SumAsync(x => (decimal?)x.Total) ?? 0m);
        opening -= await db.SupplierPayments.Where(x => x.SupplierId == supplier.Id && x.Date < from).SumAsync(x => (decimal?)x.Amount) ?? 0m;
        var receipts = await db.SupplierReceipts.AsNoTracking().Where(x => x.SupplierId == supplier.Id && x.Date >= from && x.Date <= to)
            .Select(x => new Movement(x.Date, "استلام - " + x.PolicyNumber + " - " + x.Material.Name + " - " + x.Quantity + " × " + x.UnitPrice, x.Total, 0m, x.CreatedAt)).ToListAsync();
        var payments = await db.SupplierPayments.AsNoTracking().Where(x => x.SupplierId == supplier.Id && x.Date >= from && x.Date <= to)
            .Select(x => new Movement(x.Date, "دفع - " + x.SupplierReceipt.PolicyNumber + " - " + x.PaymentMethod.Name + (x.Comments == null ? "" : " - " + x.Comments), 0m, x.Amount, x.CreatedAt)).ToListAsync();
        return Build(supplier.Name, opening, receipts.Concat(payments));
    }

    public async Task<AccountStatementResult?> GetCustomerStatementAsync(AccountStatementFilter filter)
    {
        if (!Valid(filter.FromDate, filter.ToDate) || !filter.PartyId.HasValue) return null;
        var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == filter.PartyId.Value);
        if (customer is null) return null;
        var from = filter.FromDate!.Value;
        var to = filter.ToDate!.Value;
        var opening = customer.OpeningBalance
            + (await db.CustomerDeliveries.Where(x => x.CustomerId == customer.Id && x.Date < from).SumAsync(x => (decimal?)x.Total) ?? 0m);
        opening -= await db.CustomerPayments.Where(x => x.CustomerId == customer.Id && x.Date < from).SumAsync(x => (decimal?)x.Amount) ?? 0m;
        var deliveries = await db.CustomerDeliveries.AsNoTracking().Where(x => x.CustomerId == customer.Id && x.Date >= from && x.Date <= to)
            .Select(x => new Movement(x.Date, "توريد - " + x.SupplierReceipt.PolicyNumber + " - " + x.SupplierReceipt.Material.Name + " - " + x.SupplierReceipt.Quantity + " × " + x.UnitPrice, x.Total, 0m, x.CreatedAt)).ToListAsync();
        var payments = await db.CustomerPayments.AsNoTracking().Where(x => x.CustomerId == customer.Id && x.Date >= from && x.Date <= to)
            .Select(x => new Movement(x.Date, "قبض - " + x.CustomerDelivery.SupplierReceipt.PolicyNumber + " - " + x.PaymentMethod.Name + (x.Comments == null ? "" : " - " + x.Comments), 0m, x.Amount, x.CreatedAt)).ToListAsync();
        return Build(customer.Name, opening, deliveries.Concat(payments));
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

    private static AccountStatementResult Build(string name, decimal opening, IEnumerable<Movement> movements)
    {
        var balance = opening;
        var rows = new List<AccountStatementRow>();
        foreach (var movement in movements.OrderBy(x => x.Date).ThenBy(x => x.CreatedAt))
        {
            balance += movement.Debit - movement.Credit;
            rows.Add(new(movement.Date, movement.Details, movement.Debit, movement.Credit, balance));
        }
        return new(name, opening, rows, rows.Sum(x=>x.Debit), rows.Sum(x=>x.Credit), balance);
    }

    private sealed record Movement(DateOnly Date, string Details, decimal Debit, decimal Credit, DateTimeOffset CreatedAt);
}
