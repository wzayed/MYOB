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

public class CustomerStatementModel(IFinancialReportService reports, ApplicationDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public AccountStatementFilter Filter { get; set; } = new();
    public AccountStatementResult? Report { get; private set; }
    public SelectList Customers { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        await LoadCustomersAsync();
        if (Filter.PartyId.HasValue && ValidRange()) Report = await reports.GetCustomerStatementAsync(Filter);
    }

    public async Task<IActionResult> OnGetPdfAsync()
    {
        if (!Filter.PartyId.HasValue || !ValidRange()) return BadRequest("حدد العميل والفترة بصورة صحيحة.");
        var report = await reports.GetCustomerStatementAsync(Filter);
        if (report is null) return NotFound();
        return File(new AccountStatementPdfDocument("كشف حساب عميل", report, Filter).GeneratePdf(), "application/pdf", $"customer-statement-{DateTime.Now:yyyyMMddHHmm}.pdf");
    }

    private bool ValidRange() => Filter.FromDate.HasValue && Filter.ToDate.HasValue && Filter.FromDate <= Filter.ToDate;
    private async Task LoadCustomersAsync() => Customers = new(await db.Customers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
}
