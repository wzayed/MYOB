using MYOB.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
namespace MYOB.PdfDocuments;
public sealed class AuditTrailPdfDocument(IReadOnlyList<AuditReportRow> rows, AuditReportFilter filter) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape()); page.Margin(25); page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));
            page.Header().Column(c => { c.Item().AlignRight().Text("تقرير سجل المراجعة").FontSize(18).Bold(); c.Item().AlignRight().Text(FilterText()).FontColor(Colors.Grey.Darken2); });
            page.Content().PaddingVertical(10).Table(table =>
            {
                table.ColumnsDefinition(c => { c.ConstantColumn(105); c.RelativeColumn(); c.RelativeColumn(); c.ConstantColumn(65); c.RelativeColumn(); c.ConstantColumn(85); c.ConstantColumn(85); });
                table.Header(h => { Header(h.Cell(),"التاريخ");Header(h.Cell(),"المستخدم");Header(h.Cell(),"الشاشة");Header(h.Cell(),"الإجراء");Header(h.Cell(),"الكيان");Header(h.Cell(),"رقم السجل");Header(h.Cell(),"IP"); });
                if (rows.Count == 0) table.Cell().ColumnSpan(7).Padding(15).AlignCenter().Text("لا توجد بيانات مطابقة");
                foreach (var r in rows) { Cell(table.Cell(),r.OccurredAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm"));Cell(table.Cell(),r.UserName);Cell(table.Cell(),r.Screen);Cell(table.Cell(),r.Action);Cell(table.Cell(),r.EntityName);Cell(table.Cell(),r.RecordId);Cell(table.Cell(),r.IpAddress); }
            });
            page.Footer().AlignCenter().Text(x => { x.Span("صفحة "); x.CurrentPageNumber(); x.Span(" من "); x.TotalPages(); });
        });
    }
    private string FilterText() => $"المستخدم: {filter.UserName ?? "الكل"} | الشاشة: {filter.Screen ?? "الكل"} | الإجراء: {filter.Action ?? "الكل"} | من: {filter.FromDate?.ToString() ?? "البداية"} | إلى: {filter.ToDate?.ToString() ?? "النهاية"}";
    private static void Header(IContainer c,string text)=>c.Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text(text).FontColor(Colors.White).Bold();
    private static void Cell(IContainer c,string? text)=>c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(text??"-");
}
