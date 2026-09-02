using System.ComponentModel.DataAnnotations;

namespace MYOB.DTOs;

public sealed class AccountStatementFilter
{
    [Required(ErrorMessage = "من تاريخ مطلوب")] public DateOnly? FromDate { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    [Required(ErrorMessage = "إلى تاريخ مطلوب")] public DateOnly? ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Required(ErrorMessage = "الاختيار مطلوب")] public Guid? PartyId { get; set; }
}

public sealed record AccountStatementRow(DateOnly Date, string Details, decimal Debit, decimal Credit, decimal Balance);
public sealed record AccountStatementResult(string PartyName, decimal OpeningBalance, IReadOnlyList<AccountStatementRow> Rows, decimal TotalDebit, decimal TotalCredit, decimal ClosingBalance);

public sealed class ProfitLossFilter
{
    [Required(ErrorMessage = "من تاريخ مطلوب")] public DateOnly? FromDate { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    [Required(ErrorMessage = "إلى تاريخ مطلوب")] public DateOnly? ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public Guid? SupplierId { get; set; }
    public Guid? CustomerId { get; set; }
}

public sealed record ProfitLossRow(string SupplierName, string CustomerName, string PolicyNumber, decimal Value, bool IsRealized);
public sealed record UndeliveredGoodsRow(string SupplierName, string PolicyNumber, string MaterialName, decimal Quantity, decimal PurchaseCost);
public sealed record ProfitLossResult(IReadOnlyList<ProfitLossRow> Realized, IReadOnlyList<ProfitLossRow> Unrealized, IReadOnlyList<UndeliveredGoodsRow> UndeliveredGoods);
