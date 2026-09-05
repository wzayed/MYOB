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

public class CustomerStatementTotalModel(IFinancialReportService reports, ApplicationDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public AccountStatementFilter Filter { get; set; } = new();
    public AccountStatementResult? Report { get; private set; }
    public SelectList Customers { get; private set; } = null!;
    public SelectList Suppliers { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        await LoadListsAsync();
        if (Filter.PartyId.HasValue && ValidRange())
            Report = await reports.GetTotalCustomerStatementAsync(Filter);
    }

    public async Task<IActionResult> OnGetPdfAsync()
    {
        if (!Filter.PartyId.HasValue || !ValidRange()) return BadRequest("حدد العميل والفترة بصورة صحيحة.");
        var report = await reports.GetTotalCustomerStatementAsync(Filter);
        if (report is null) return NotFound();
        return File(new AccountStatementPdfDocument("إجمالى كشف حساب عميل", "العميل", "المورد", report, Filter).GeneratePdf(),
            "application/pdf", $"customer-total-statement-{DateTime.Now:yyyyMMddHHmm}.pdf");
    }

    private bool ValidRange() => ModelState.IsValid && Filter.FromDate.HasValue && Filter.ToDate.HasValue
        && Filter.FromDate <= Filter.ToDate && Filter.ToDate <= BusinessDate.Today;

    private async Task LoadListsAsync()
    {
        Customers = new(await db.Customers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        Suppliers = new(await db.Suppliers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
    }
}
