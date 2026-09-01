using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MYOB.Data;
using MYOB.DTOs;
using MYOB.PdfDocuments;
using MYOB.Services;
using QuestPDF.Fluent;

namespace MYOB.Pages.Reports;

public class ProfitLossModel(IFinancialReportService reports, ApplicationDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public ProfitLossFilter Filter { get; set; } = new();
    public ProfitLossResult Report { get; private set; } = new([], []);
    public SelectList Suppliers { get; private set; } = null!;
    public SelectList Customers { get; private set; } = null!;
    public string SupplierName { get; private set; } = "الكل";
    public string CustomerName { get; private set; } = "الكل";

    public async Task OnGetAsync()
    {
        await LoadFiltersAsync();
        if (ValidRange()) Report = await reports.GetProfitLossAsync(Filter);
    }

    public async Task<IActionResult> OnGetPdfAsync()
    {
        if (!ValidRange()) return BadRequest("حدد الفترة بصورة صحيحة.");
        await LoadFiltersAsync();
        var report = await reports.GetProfitLossAsync(Filter);
        return File(new ProfitLossPdfDocument(report, Filter, SupplierName, CustomerName).GeneratePdf(), "application/pdf", $"profit-loss-{DateTime.Now:yyyyMMddHHmm}.pdf");
    }

    private bool ValidRange() => Filter.FromDate.HasValue && Filter.ToDate.HasValue && Filter.FromDate <= Filter.ToDate;
    private async Task LoadFiltersAsync()
    {
        var suppliers = await db.Suppliers.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        var customers = await db.Customers.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        Suppliers = new(suppliers, "Id", "Name"); Customers = new(customers, "Id", "Name");
        SupplierName = suppliers.FirstOrDefault(x => x.Id == Filter.SupplierId)?.Name ?? "الكل";
        CustomerName = customers.FirstOrDefault(x => x.Id == Filter.CustomerId)?.Name ?? "الكل";
    }
}
