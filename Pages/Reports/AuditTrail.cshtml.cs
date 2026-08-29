using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using Microsoft.EntityFrameworkCore;using MYOB.Data;using MYOB.DTOs;using MYOB.PdfDocuments;using MYOB.Services;using QuestPDF.Fluent;
namespace MYOB.Pages.Reports;
public class AuditTrailModel(IAuditReportService reports,ApplicationDbContext db):PageModel
{
    [BindProperty(SupportsGet=true)]public AuditReportFilter Filter{get;set;}=new();[BindProperty(SupportsGet=true)]public int PageNumber{get;set;}=1;public IReadOnlyList<AuditReportRow> Rows{get;private set;}=[];public IReadOnlyList<string> Users{get;private set;}=[];public int TotalPages{get;private set;}
    public async Task OnGetAsync(){var all=await reports.GetAsync(Filter);const int size=50;TotalPages=Math.Max(1,(int)Math.Ceiling(all.Count/(double)size));PageNumber=Math.Clamp(PageNumber,1,TotalPages);Rows=all.Skip((PageNumber-1)*size).Take(size).ToList();Users=await db.AuditLogs.AsNoTracking().Select(x=>x.UserName).Distinct().OrderBy(x=>x).ToListAsync();}
    public async Task<IActionResult> OnGetPdfAsync(){var rows=await reports.GetAsync(Filter);var bytes=new AuditTrailPdfDocument(rows,Filter).GeneratePdf();return File(bytes,"application/pdf",$"audit-trail-{DateTime.Now:yyyyMMddHHmm}.pdf");}
}
