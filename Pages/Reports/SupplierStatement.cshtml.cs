using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MYOB.Data;
using MYOB.DTOs;
using MYOB.PdfDocuments;
using MYOB.Services;
using MYOB.Support;
using QuestPDF.Fluent;

namespace MYOB.Pages.Reports;

public class SupplierStatementModel(IFinancialReportService reports, ApplicationDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public AccountStatementFilter Filter { get; set; } = new();
    public AccountStatementResult? Report { get; private set; }
    public SelectList Suppliers { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        await LoadSuppliersAsync();
        if (Filter.PartyId.HasValue && ValidRange()) Report = await reports.GetSupplierStatementAsync(Filter);
    }

    public async Task<IActionResult> OnGetPdfAsync()
    {
        if (!Filter.PartyId.HasValue || !ValidRange()) return BadRequest("حدد المورد والفترة بصورة صحيحة.");
        var report = await reports.GetSupplierStatementAsync(Filter);
        if (report is null) return NotFound();
        return File(new AccountStatementPdfDocument("كشف حساب مورد", report, Filter).GeneratePdf(), "application/pdf", $"supplier-statement-{DateTime.Now:yyyyMMddHHmm}.pdf");
    }

    private bool ValidRange() => ModelState.IsValid && Filter.FromDate.HasValue && Filter.ToDate.HasValue && Filter.FromDate <= Filter.ToDate && Filter.ToDate <= BusinessDate.Today;
    private async Task LoadSuppliersAsync() => Suppliers = new(await db.Suppliers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
}
