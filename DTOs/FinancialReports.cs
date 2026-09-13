using System.ComponentModel.DataAnnotations;
using MYOB.Support;

namespace MYOB.DTOs;

public sealed class AccountStatementFilter
{
    [Required(ErrorMessage = "من تاريخ مطلوب"), NotFutureDate, DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}", ApplyFormatInEditMode = true)] public DateOnly? FromDate { get; set; } = new(BusinessDate.Today.Year, BusinessDate.Today.Month, 1);
    [Required(ErrorMessage = "إلى تاريخ مطلوب"), NotFutureDate, DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}", ApplyFormatInEditMode = true)] public DateOnly? ToDate { get; set; } = BusinessDate.Today;
    [Required(ErrorMessage = "الاختيار مطلوب")] public Guid? PartyId { get; set; }
    public Guid? RelatedPartyId { get; set; }
    public bool ShowPrivateNotes { get; set; }
}

public sealed record AccountStatementRow(DateOnly Date, string Details, decimal Debit, decimal Credit, decimal Balance);
public sealed record AccountStatementResult(string PartyName, string? RelatedPartyName, decimal OpeningBalance, IReadOnlyList<AccountStatementRow> Rows, decimal TotalDebit, decimal TotalCredit, decimal ClosingBalance);

public sealed class ProfitLossFilter
{
    [Required(ErrorMessage = "من تاريخ مطلوب"), NotFutureDate, DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}", ApplyFormatInEditMode = true)] public DateOnly? FromDate { get; set; } = new(BusinessDate.Today.Year, BusinessDate.Today.Month, 1);
    [Required(ErrorMessage = "إلى تاريخ مطلوب"), NotFutureDate, DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}", ApplyFormatInEditMode = true)] public DateOnly? ToDate { get; set; } = BusinessDate.Today;
    public Guid? SupplierId { get; set; }
    public Guid? CustomerId { get; set; }
}

public sealed record ProfitLossRow(string SupplierName, string CustomerName, string PolicyNumber, decimal Value, bool IsRealized);
public sealed record UndeliveredGoodsRow(string SupplierName, string PolicyNumber, string MaterialName, decimal Quantity, decimal PurchaseCost);
public sealed record ProfitLossResult(IReadOnlyList<ProfitLossRow> Realized, IReadOnlyList<ProfitLossRow> Unrealized, IReadOnlyList<UndeliveredGoodsRow> UndeliveredGoods);
