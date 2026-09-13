using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MYOB.Data;
using MYOB.Services;
using MYOB.Support;

namespace MYOB.Pages;

[AllowAnonymous]
public class IndexModel(ApplicationDbContext db, IPermissionService permissions, ILogger<IndexModel> logger) : PageModel
{
    public DateOnly Today => BusinessDate.Today;
    public DateTime RefreshedAt { get; private set; }
    public bool Failed { get; private set; }
    public bool SuppliersVisible { get; private set; }
    public bool CustomersVisible { get; private set; }
    public bool GoodsVisible { get; private set; }
    public bool CanPay { get; private set; }
    public bool CanCollect { get; private set; }
    public bool CanDeliver { get; private set; }
    public List<PartyBalance> Suppliers { get; private set; } = [];
    public List<PartyBalance> Customers { get; private set; } = [];
    public List<PartyBalance> Credits { get; private set; } = [];
    public decimal MonthlyPurchases { get; private set; }
    public decimal MonthlySales { get; private set; }
    public decimal MonthlyPaid { get; private set; }
    public decimal MonthlyReceived { get; private set; }
    public decimal GoodsValue { get; private set; }
    public int GoodsCount { get; private set; }
    public int MatchingCount { get; private set; }
    public List<PendingGoods> Goods { get; private set; } = [];
    public List<PendingGoods> OldestGoods { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public Guid? SupplierId { get; set; }
    public Microsoft.AspNetCore.Mvc.Rendering.SelectList VendorOptions { get; private set; } = null!;
    [BindProperty(SupportsGet = true)] public string Sort { get; set; } = "latest";
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(MatchingCount / 5d));

    public async Task OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated != true) return;
        try
        {
            SuppliersVisible = await permissions.HasAsync(User, ScreenCatalog.SupplierStatement);
            CustomersVisible = await permissions.HasAsync(User, ScreenCatalog.CustomerStatement);
            GoodsVisible = await permissions.HasAsync(User, ScreenCatalog.SupplierReceipts);
            CanPay = await permissions.HasAsync(User, ScreenCatalog.SupplierPayments) && await permissions.HasAsync(User, ScreenCatalog.SupplierPayments, PermissionAction.Add);
            CanCollect = await permissions.HasAsync(User, ScreenCatalog.CustomerPayments) && await permissions.HasAsync(User, ScreenCatalog.CustomerPayments, PermissionAction.Add);
            CanDeliver = await permissions.HasAsync(User, ScreenCatalog.CustomerDeliveries) && await permissions.HasAsync(User, ScreenCatalog.CustomerDeliveries, PermissionAction.Add);
            var today = Today;
            var month = new DateOnly(today.Year, today.Month, 1);
            if (SuppliersVisible)
            {
                Suppliers = await db.Suppliers.AsNoTracking().Select(s => new PartyBalance(s.Id, s.Name,
                    s.OpeningBalance
                    + (db.SupplierReceipts.Where(x => x.SupplierId == s.Id && x.Date <= today).Sum(x => (decimal?)x.Total) ?? 0m)
                    - (db.SupplierPayments.Where(x => x.SupplierId == s.Id && x.Date <= today).Sum(x => (decimal?)(x.Amount - x.CreditAmount)) ?? 0m)
                    - (db.SupplierCredits.Where(x => x.SupplierId == s.Id && x.Date <= today).Sum(x => (decimal?)x.OriginalAmount) ?? 0m)))
                    .ToListAsync();
                Suppliers = Suppliers.Where(x => x.Amount > 0).OrderByDescending(x => x.Amount).ToList();
                Credits = await db.SupplierCredits.AsNoTracking().Where(x => x.Date <= today && x.RemainingAmount > 0)
                    .GroupBy(x => new { x.SupplierId, x.Supplier.Name })
                    .Select(x => new PartyBalance(x.Key.SupplierId, x.Key.Name, x.Sum(c => c.RemainingAmount)))
                    .ToListAsync();
                Credits = Credits.OrderByDescending(x => x.Amount).ToList();
                MonthlyPurchases = await db.SupplierReceipts.Where(x => x.Date >= month && x.Date <= today).SumAsync(x => (decimal?)x.Total) ?? 0m;
                MonthlyPaid = (await db.SupplierPayments.Where(x => x.Date >= month && x.Date <= today).SumAsync(x => (decimal?)(x.Amount - x.CreditAmount)) ?? 0m)
                    + (await db.SupplierCredits.Where(x => x.Date >= month && x.Date <= today).SumAsync(x => (decimal?)x.OriginalAmount) ?? 0m);
            }
            if (CustomersVisible)
            {
                Customers = await db.Customers.AsNoTracking().Select(c => new PartyBalance(c.Id, c.Name,
                    c.OpeningBalance
                    + (db.CustomerDeliveries.Where(x => x.CustomerId == c.Id && x.Date <= today).Sum(x => (decimal?)x.Total) ?? 0m)
                    - (db.CustomerPayments.Where(x => x.CustomerId == c.Id && x.Date <= today).Sum(x => (decimal?)x.Amount) ?? 0m)))
                    .ToListAsync();
                Customers = Customers.Where(x => x.Amount > 0).OrderByDescending(x => x.Amount).ToList();
                MonthlySales = await db.CustomerDeliveries.Where(x => x.Date >= month && x.Date <= today).SumAsync(x => (decimal?)x.Total) ?? 0m;
                MonthlyReceived = await db.CustomerPayments.Where(x => x.Date >= month && x.Date <= today).SumAsync(x => (decimal?)x.Amount) ?? 0m;
            }
            if (GoodsVisible)
            {
                VendorOptions = new(await db.Suppliers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
                var query = db.SupplierReceipts.AsNoTracking().Where(x => x.Date <= today && x.CustomerDelivery == null);
                GoodsCount = await query.CountAsync();
                GoodsValue = await query.SumAsync(x => (decimal?)x.Total) ?? 0m;
                OldestGoods = await query.OrderBy(x => x.Date).ThenBy(x => x.Id).Take(3)
                    .Select(x => new PendingGoods(x.Id, x.PolicyNumber, x.Date, x.Supplier.Name, x.Material.Name, x.Quantity,
                        x.Material.UnitOfMeasure.Name, x.UnitPrice, x.TaxPercent, x.Total, x.Details)).ToListAsync();
                if (SupplierId.HasValue)
                    query = query.Where(x => x.SupplierId == SupplierId.Value);
                MatchingCount = await query.CountAsync();
                PageNumber = Math.Clamp(PageNumber, 1, TotalPages);
                var ordered = Sort switch
                {
                    "oldest" => query.OrderBy(x => x.Date).ThenBy(x => x.Id),
                    "cost" => query.OrderByDescending(x => x.Total).ThenBy(x => x.Id),
                    _ => query.OrderByDescending(x => x.Date).ThenBy(x => x.Id)
                };
                Goods = await ordered.Skip((PageNumber - 1) * 5).Take(5)
                    .Select(x => new PendingGoods(x.Id, x.PolicyNumber, x.Date, x.Supplier.Name, x.Material.Name, x.Quantity,
                        x.Material.UnitOfMeasure.Name, x.UnitPrice, x.TaxPercent, x.Total, x.Details)).ToListAsync();
            }
            RefreshedAt = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Egypt Standard Time");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to load business dashboard");
            Failed = true;
        }
    }

    public sealed record PartyBalance(Guid Id, string Name, decimal Amount);
    public sealed record PendingGoods(Guid Id, string Policy, DateOnly Date, string Supplier, string Material,
        decimal Quantity, string Unit, decimal UnitPrice, decimal Tax, decimal Total, string? Notes);
}
