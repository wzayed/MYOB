using MYOB.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MYOB.PdfDocuments;

public sealed class AccountStatementPdfDocument(string title, AccountStatementResult report, AccountStatementFilter filter) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4); page.Margin(25); page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));
            page.Header().Column(c =>
            {
                c.Item().AlignRight().Text(title).FontSize(18).Bold();
                c.Item().AlignRight().Text($"الاسم: {report.PartyName} | من: {filter.FromDate:yyyy/MM/dd} | إلى: {filter.ToDate:yyyy/MM/dd}").FontColor(Colors.Grey.Darken2);
            });
            page.Content().PaddingVertical(10).Column(column =>
            {
                column.Item().PaddingBottom(8).AlignRight().Text($"الرصيد قبل الفترة: {report.OpeningBalance:N2}").Bold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.ConstantColumn(80); c.RelativeColumn(3); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                    table.Header(h => { Header(h.Cell(), "التاريخ"); Header(h.Cell(), "التفاصيل"); Header(h.Cell(), "عليه"); Header(h.Cell(), "له"); Header(h.Cell(), "الرصيد"); });
                    if (report.Rows.Count == 0) table.Cell().ColumnSpan(5).Padding(15).AlignCenter().Text("لا توجد حركات في الفترة المحددة");
                    foreach (var row in report.Rows)
                    {
                        Cell(table.Cell(), row.Date.ToString("yyyy/MM/dd")); Cell(table.Cell(), row.Details);
                        Cell(table.Cell(), row.Debit == 0 ? "" : row.Debit.ToString("N2")); Cell(table.Cell(), row.Credit == 0 ? "" : row.Credit.ToString("N2")); Cell(table.Cell(), row.Balance.ToString("N2"));
                    }
                });
                column.Item().PaddingTop(8).AlignRight().Text($"الرصيد الختامي: {report.ClosingBalance:N2}").Bold();
            });
            page.Footer().AlignCenter().Text(x => { x.Span("صفحة "); x.CurrentPageNumber(); x.Span(" من "); x.TotalPages(); });
        });
    }
    private static void Header(IContainer c, string text) => c.Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text(text).FontColor(Colors.White).Bold();
    private static void Cell(IContainer c, string text) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(text);
}
