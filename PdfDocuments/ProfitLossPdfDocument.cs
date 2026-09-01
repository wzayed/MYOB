using MYOB.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MYOB.PdfDocuments;

public sealed class ProfitLossPdfDocument(ProfitLossResult report, ProfitLossFilter filter, string supplierName, string customerName) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4); page.Margin(25); page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));
            page.Header().Column(c =>
            {
                c.Item().AlignRight().Text("تقرير الأرباح والخسائر المحققة وغير المحققة").FontSize(17).Bold();
                c.Item().AlignRight().Text($"من: {filter.FromDate:yyyy/MM/dd} | إلى: {filter.ToDate:yyyy/MM/dd} | المورد: {supplierName} | العميل: {customerName}").FontColor(Colors.Grey.Darken2);
            });
            page.Content().PaddingVertical(10).Column(c =>
            {
                Section(c, "المحقق", report.Realized, "صافي الربح/الخسارة", Colors.Green.Darken2);
                c.Item().PaddingTop(18);
                Section(c, "غير المحقق - تكلفة بضاعة غير موردة", report.Unrealized, "التكلفة غير المحققة", Colors.Orange.Darken2);
            });
            page.Footer().AlignCenter().Text(x => { x.Span("صفحة "); x.CurrentPageNumber(); x.Span(" من "); x.TotalPages(); });
        });
    }
    private static void Section(ColumnDescriptor column, string title, IReadOnlyList<ProfitLossRow> rows, string totalLabel, string color)
    {
        column.Item().AlignRight().Text(title).FontSize(14).Bold().FontColor(color);
        column.Item().PaddingTop(5).Table(table =>
        {
            table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
            table.Header(h => { Header(h.Cell(), "المورد"); Header(h.Cell(), "العميل"); Header(h.Cell(), "رقم البوليصة"); Header(h.Cell(), "القيمة"); });
            if (rows.Count == 0) table.Cell().ColumnSpan(4).Padding(12).AlignCenter().Text("لا توجد بيانات مطابقة");
            foreach (var row in rows) { Cell(table.Cell(), row.SupplierName); Cell(table.Cell(), row.CustomerName); Cell(table.Cell(), row.PolicyNumber); Cell(table.Cell(), row.Value.ToString("N2")); }
        });
        column.Item().PaddingTop(5).AlignRight().Text($"{totalLabel}: {rows.Sum(x => x.Value):N2}").Bold();
    }
    private static void Header(IContainer c, string text) => c.Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text(text).FontColor(Colors.White).Bold();
    private static void Cell(IContainer c, string text) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(text);
}
